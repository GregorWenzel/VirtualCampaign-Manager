using ComponentPro.IO;
using ComponentPro.Net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Helpers;
using VirtualCampaign_Manager.Workers;

namespace VirtualCampaign_Manager.Transfers
{
    public sealed class TransferManager : INotifyPropertyChanged
    {
        public EventHandler<ResultEventArgs> SuccessEvent;
        public EventHandler<ResultEventArgs> FailureEvent;

        private Dictionary<string, Sftp> FtpClientsDict = new Dictionary<string, Sftp>();
        private Dictionary<Sftp, bool> FtpTransfersDict = new Dictionary<Sftp, bool>();

        private ObservableCollection<TransferPacket> transferPacketList = new ObservableCollection<TransferPacket>();
        public ObservableCollection<TransferPacket> TransferPacketList
        {
            get
            {
                return transferPacketList;
            }
            set
            {
                if (value == transferPacketList) return;

                transferPacketList = value;
                RaisePropertyChangedEvent("TransferPacketList");
            }
        }

        private TransferQueue queue;

        public void AddTransferPacket(TransferPacket packet)
        {
            if ((packet.Type == TransferType.DownloadAnimatedMotif || packet.Type == TransferType.DownloadAudio || packet.Type == TransferType.DownloadMotif)
                && (System.IO.File.Exists(packet.TargetPath)))
            {
                packet.FireSuccessEvent();
                return;
            }

            List<TransferPacket> buffer = new List<TransferPacket>(transferPacketList);

            if (buffer.Any(item => (string)item.ItemID == (string)packet.ItemID))
            {
                packet.FireSuccessEvent();
                return;
            }
            packet.Client = GetClient(packet);
            packet.ClientConnectedEvent += OnClientConnected;

            transferPacketList.Add(packet);

            string logText = "Adding transfer packet:\r\n";
            logText += "Source: " + packet.SourcePath;
            logText += "\r\nTarget: " + packet.TargetPath;

            LogText(packet, logText);

            ConnectClient(packet, packet.Client);
        }

        private void OnClientConnected(object sender, EventArgs ea)
        {
            TransferPacket packet = sender as TransferPacket;
            Sftp client = packet.Client;

            packet.ClientAuthenticatedEvent += OnClientAuthenticated;

            AuthenticateClient(packet, client);
        }

        private void AuthenticateClient(TransferPacket packet, Sftp client)
        {
            string password = packet.LoginData.Password;
            string user = packet.LoginData.Username;
            try
            {
                client.AuthenticateAsync(user, password);
            }
            catch (Exception ex)
            {
                LogText(packet, string.Format("Authentication to server '{0}' with user '{1}' failed.", packet.LoginData.Url, packet.LoginData.Username));
            }
        }

        private void ConnectClient(TransferPacket packet, Sftp client)
        {
            if (FtpTransfersDict[client] == true) return;

            FtpTransfersDict[client] = true;

            string serverURL = packet.LoginData.Url;
            if (serverURL.Contains("ftp://"))
            {
                serverURL = serverURL.Replace("ftp://", "");
            }

            try
            {
                client.ConnectAsync(packet.LoginData.Url, 22);
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("already connecting"))
                {
                    return;
                }
                else
                {
                    LogText(packet, string.Format("Connection to server '{0}' failed. \r\n--Error Message: {1}", packet.LoginData.Url, ex.Message));
                    ConnectClient(packet, client);
                }
            }
        }

        private void OnClientAuthenticated(object sender, EventArgs ea)
        {
            TransferPacket packet = sender as TransferPacket;
            Sftp client = packet.Client;

            packet.ClientAuthenticatedEvent -= OnClientAuthenticated;

            TransferPacket(packet, client);
        }

        private void TransferPacket(TransferPacket packet, Sftp client)
        {
            FileInfoBase sourceFileInfo;
            FileInfoBase targetFileInfo;

            if (packet.IsDownload)
            {
                sourceFileInfo = client.CreateFileInfo(packet.SourcePath);
                targetFileInfo = DiskFileSystem.Default.CreateFileInfo(packet.TargetPath);

                ProgressFileItem progressFileItem = queue.Add(sourceFileInfo, targetFileInfo, false, null, 0);
                progressFileItem.Tag = packet.ItemID;
            }
            else
            {
                if (IOHelper.IsDirectory(packet.SourcePath))
                {
                    AddFilesFromDirectory(client, packet);
                }
                else
                {
                    sourceFileInfo = DiskFileSystem.Default.CreateFileInfo(packet.SourcePath);
                    targetFileInfo = client.CreateFileInfo(packet.TargetPath);

                    ProgressFileItem progressFileItem = queue.Add(sourceFileInfo, targetFileInfo, false, null, 0);
                    progressFileItem.Tag = packet.ItemID;
                }
            }
            queue.Start();
        }


        private void AddFilesFromDirectory(Sftp client, TransferPacket parentPacket)
        {
            string[] sourceFileArr = Directory.GetFiles(parentPacket.SourcePath);
            foreach (string sourceFile in sourceFileArr)
            {
                string targetFile = "";
                if (parentPacket.Type == TransferType.UploadFilmPreviewDirectory)
                {
                    targetFile = UriCombine.Uri.Combine(parentPacket.TargetPath, parentPacket.ItemID.ToString(), Path.GetFileName(sourceFile));
                }
                else if (parentPacket.Type == TransferType.UploadFilmDirectory)
                {
                    targetFile = UriCombine.Uri.Combine(parentPacket.TargetPath, (parentPacket.Parent as Production).Film.UrlHash, Path.GetFileName(sourceFile));
                }
                else
                {
                    targetFile = null;
                }
                TransferPacket newPacket = new TransferPacket(parentPacket, sourceFile, targetFile);
                FileInfoBase sourceFileInfo = DiskFileSystem.Default.CreateFileInfo(newPacket.SourcePath);
                FileInfoBase targetFileInfo = client.CreateFileInfo(newPacket.TargetPath);

                ProgressFileItem progressFileItem = queue.Add(sourceFileInfo, targetFileInfo, false, null, 0);
                progressFileItem.Tag = newPacket.ItemID;
            }
        }

        private void LogText(TransferPacket packet, string logText)
        {
            if (packet.Parent is Job)
            {
                (packet.Parent as Job).LogText(logText);
            }
            else if (packet.Parent is Motif)
            {
                (packet.Parent as Motif).Job.LogText(logText);
            }
            else if (packet.Parent is Production)
            {
                (packet.Parent as Production).LogText(logText);
            }
        }

        private Sftp GetClient(TransferPacket packet)
        {
            Sftp ftpClient;

            if (FtpClientsDict.ContainsKey(packet.LoginData.Url+packet.LoginData.Username) == false)
            {
                //initialize new Sftp connection
                ftpClient = new Sftp();
                ftpClient.Timeout = -1;
                ftpClient.ReconnectionMaxRetries = 10;
                FtpClientsDict[packet.LoginData.Url + packet.LoginData.Username] = ftpClient;
                FtpTransfersDict[ftpClient] = false;
            }
            else
            {
                ftpClient = FtpClientsDict[packet.LoginData.Url + packet.LoginData.Username];
            }

            return ftpClient;
        }

        private void FireSuccessEvent(TransferPacket packet)
        {
            EventHandler<ResultEventArgs> successEvent = SuccessEvent;
            if (successEvent != null)
            {
                successEvent(null, new ResultEventArgs(packet));
            }
        }

        private void FireFailureEvent(TransferPacket packet)
        {
            EventHandler<ResultEventArgs> failureEvent = FailureEvent;
            if (failureEvent != null)
            {
                failureEvent(null, new ResultEventArgs(packet));
            }
        }
        private static volatile TransferManager instance;
        private static object syncRoot = new object();

        private TransferManager()
        {
            queue = new TransferQueue(Settings.MaxDownloadThreads);
            queue.ItemProcessed += Queue_ItemProcessed;
            //queue.BrowsingThreadStateChanged += Queue_BrowsingThreadStateChanged;
            //queue.StateChanged += Queue_StateChanged;
            queue.Start();
        }

        private void Queue_StateChanged(object sender, TransferQueueStateChangedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void Queue_BrowsingThreadStateChanged(object sender, TransferThreadStateChangedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void Queue_ItemProcessed(object sender, TransferQueueItemProcessedEventArgs e)
        {
            ProgressFileItem item = e.Item;
            TransferPacket packet;
            bool isSubPacket = false;

            if (item.Tag.ToString().Contains("packet_"))
            {
                string[] buffer = item.Tag.ToString().Split('_');
                packet = TransferPacketList.First(packetItem => (string)packetItem.ItemID == buffer[1]);
                isSubPacket = true;
            }
            else
            {
                packet = TransferPacketList.First(packetItem => (string)packetItem.ItemID == (string)item.Tag);
            }

            if (isSubPacket)
            {
                packet.RemainingSubPackets -= 1;
                if (packet.RemainingSubPackets > 0) return;
            }
            
            TransferPacketList.Remove(packet);
            
            if (e.Item.State == TransferState.FileCopied)
            {
                packet.IsInTransit = false;
                packet.IsSuccessful = true;
                packet.FireSuccessEvent();
            }
            else
            {
                packet.IsInTransit = false;
                packet.IsSuccessful = false;
                packet.FireFailureEvent();
            }
        }

        private TransferPacket IdentifyTransferPacket(ProgressFileItem item)
        {
            return null;
        }

        public static TransferManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new TransferManager();
                        }
                    }
                }

                return instance;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChangedEvent(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
                PropertyChanged(null, e);
            }
        }
    }
}