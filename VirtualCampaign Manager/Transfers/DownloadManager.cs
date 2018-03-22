using ComponentPro.IO;
using ComponentPro.Net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Workers;

namespace VirtualCampaign_Manager.Transfers
{
    public sealed class DownloadManager : INotifyPropertyChanged
    {
        public EventHandler<ResultEventArgs> SuccessEvent;
        public EventHandler<ResultEventArgs> FailureEvent;

        private Dictionary<string, Sftp> FtpClientsDict = new Dictionary<string, Sftp>();
        private Dictionary<string, int> FtpTransfersDict = new Dictionary<string, int>();

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
            if (System.IO.File.Exists(packet.TargetPath))
            {
                packet.FireSuccessEvent();
                return;
            }

            List<TransferPacket> buffer = new List<TransferPacket>(transferPacketList);

            if (buffer.Any(item => item.ItemID == packet.ItemID)) return;

            transferPacketList.Add(packet);

            string logText = "Adding transfer packet:\r\n";
            logText += "Source: " + packet.SourcePath;
            logText += "\r\nTarget: " + packet.TargetPath;
            
            if (packet.Parent is Job)
            {
                (packet.Parent as Job).LogText(logText);
            }
            else if (packet.Parent is Production)
            {
                (packet.Parent as Production).LogText(logText);
            }

            Sftp client = GetClient(packet);

            if (client.IsConnected && client.IsAuthenticated)
            {
                FileInfoBase sourceFileInfo = client.CreateFileInfo(packet.SourcePath);
                FileInfoBase targetFileInfo = DiskFileSystem.Default.CreateFileInfo(packet.TargetPath);
                ProgressFileItem progressFileItem = queue.Add(sourceFileInfo, targetFileInfo, false, null, 0);
                progressFileItem.Tag = packet.ItemID;
                queue.Start();                
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
                ftpClient.DownloadFileCompleted += FtpClient_DownloadFileCompleted;
                FtpClientsDict[packet.LoginData.Url + packet.LoginData.Username] = ftpClient;
                FtpTransfersDict[packet.LoginData.Url + packet.LoginData.Username] = 0;
            }
            else
            {
                ftpClient = FtpClientsDict[packet.LoginData.Url + packet.LoginData.Username];
            }

            if (ftpClient.IsConnected == false)
            {
                string serverURL = packet.LoginData.Url;
                if (serverURL.Contains("ftp://"))
                {
                    serverURL = serverURL.Replace("ftp://", "");
                }
                ftpClient.Connect(serverURL);
            }

            if (ftpClient.IsAuthenticated == false)
            {
                string password = packet.LoginData.Password;
                string user = packet.LoginData.Username;
                ftpClient.Authenticate(user, password);
            }

            return ftpClient;
        }

        private void FtpClient_DownloadFileCompleted(object sender, ComponentPro.ExtendedAsyncCompletedEventArgs<long> e)
        {
            Sftp ftpClient = sender as Sftp;
            Object task = e.UserState;
            TransferPacket thisPacket = TransferPacketList.First(item => item.Task == task);
            if (thisPacket != null)
            {
                thisPacket.IsInTransit = false;
                if (e.Error == null)
                {
                    thisPacket.IsSuccessful = true;
                    if (thisPacket.Parent is Motif)
                    {
                        (thisPacket.Parent as Motif).IsAvailable = true;
                    }
                    thisPacket.FireSuccessEvent();
                }
                else
                {
                    thisPacket.TransferErrorCounter += 1;
                    if (thisPacket.TransferErrorCounter > Settings.MaxTransferErrorCount)
                    {
                        thisPacket.IsSuccessful = false;
                        thisPacket.FireFailureEvent();
                    }
                }
            }
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
        private static volatile DownloadManager instance;
        private static object syncRoot = new object();

        private DownloadManager()
        {
            queue = new TransferQueue(Settings.MaxDownloadThreads);
            queue.ItemProcessed += Queue_ItemProcessed;
            queue.BrowsingThreadStateChanged += Queue_BrowsingThreadStateChanged;
            queue.StateChanged += Queue_StateChanged;
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
            TransferPacket packet = TransferPacketList.First(packetItem => packetItem.ItemID == (int)item.Tag);
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

        public static DownloadManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new DownloadManager();
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
