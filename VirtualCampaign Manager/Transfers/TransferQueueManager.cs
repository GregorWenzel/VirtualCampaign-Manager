using ComponentPro.IO;
using ComponentPro.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;
using System.Timers;

namespace VirtualCampaign_Manager.Transfers
{
    public sealed class TransferQueueManager
    {
        private static volatile TransferQueueManager instance;
        private static object syncRoot = new Object();

        private List<TransferPacket> transferPacketList;

        private Sftp client;
        private int connectionAttempts;
        private TransferQueue queue;
        private LoginData loginData;

        private Timer reconnectTimer;

        private TransferQueueManager()
        {
            transferPacketList = new List<TransferPacket>();

            loginData = Settings.MasterLogin;

            //initialize new Sftp connection
            client = new Sftp();
            client.Timeout = -1;
            client.ReconnectionMaxRetries = 10;

            //initialize reconnect timer in event of persistent connection failure
            reconnectTimer = new Timer();
            reconnectTimer.Interval = 1000 * 60 * 5;
            reconnectTimer.Elapsed += ReconnectTimer_Elapsed;

            Reconnect();

            queue = new TransferQueue(3);
            queue.ItemProcessed += Queue_ItemProcessed;
            queue.Start();
        }

        private void ReconnectTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            reconnectTimer.Stop();

        }

        private bool Reconnect()
        {
            connectionAttempts = 0;
            while (connectionAttempts <= Settings.MaxTransferErrorCount)
            {
                Exception clientException = Connectclient();

                if (clientException == null)
                {
                    return true;
                }

                Console.WriteLine("Connection to " + loginData.Url + " failed: " + clientException.Message);
                connectionAttempts += 1;
                Console.WriteLine("Retry attempt no. " + connectionAttempts);
            }

            Console.WriteLine("Connection to " + loginData.Url + " not possible at this moment. Waiting for 5 minutes to try again...");
            return false;
        }

        private Exception Connectclient()
        {
            //connect to client
            try
            {
                client.Connect(loginData.Url);
            }
            catch (Exception ex)
            {
                client.Disconnect();
                return ex;
            }

            //authenticate user
            try
            {
                client.Authenticate(loginData.Username, loginData.Password);
            }
            catch (Exception ex)
            {
                client.Disconnect();
                return ex;
            }

            client.ReconnectionMaxRetries = Settings.MaxTransferErrorCount;
            client.ReconnectionFailureDelay = 5000;

            return null;
        }

        private void Queue_ItemProcessed(object sender, TransferQueueItemProcessedEventArgs e)
        {
            TransferPacket currentPacket;

            if (transferPacketList.Any(item => item.FileItem == e.Item))
            {
                currentPacket = transferPacketList.First(item => item.FileItem == e.Item);
                if (e.Item.Error == null)
                {
                    if (currentPacket.Type == TransferType.UploadSubPacket)
                    {
                        TransferPacket parentPacket = currentPacket.Parent as TransferPacket;
                        parentPacket.RemainingSubPackets -= 1;
                        if (parentPacket.RemainingSubPackets == 0)
                        {
                            parentPacket.FireSuccessEvent();
                        }
                    }
                    else
                    {
                        currentPacket.FireSuccessEvent();
                    }
                }
                else
                {
                    currentPacket.FireFailureEvent(e.Item.Error);
                }
                transferPacketList.Remove(currentPacket);
            }
            else
            {

            }
        }               

        public void AddTransferPacket(TransferPacket Packet)
        {
            if (client.IsConnected == false)
            {
                bool success = Reconnect();

                if (success == false)
                {
                    if (Packet.Parent is Production)
                    {
                        (Packet.Parent as Production).ErrorStatus = ProductionErrorStatus.PES_UPLOAD;
                    }
                    else if (Packet.Parent is Motif)
                    {
                        if ((Packet.Parent as Motif).Job != null)
                        {
                            (Packet.Parent as Motif).Job.ErrorStatus = JobErrorStatus.JES_DOWNLOAD_MOTIFS;
                        }
                    }

                    return;
                }
            }

            switch (Packet.Type)
            {
                case TransferType.DownloadAnimatedMotif:
                case TransferType.DownloadAudio:
                case TransferType.DownloadMotif:
                    AddDownloadSingle(Packet);
                    break;
                case TransferType.UploadFilmDirectory:
                case TransferType.UploadFilmPreviewDirectory:
                case TransferType.UploadProductDirectory:
                case TransferType.UploadProductPreviewDirectory:
                    AddUploadDirectory(Packet);
                    break;
                case TransferType.UploadMotifPreview:
                    AddUploadSingle(Packet);
                    break;
            }
                        
            if (queue.State != TransferQueueState.Processing)
            {
                queue.Start();
            }
        }

        private void AddUploadDirectory(TransferPacket packet)
        {
            FileInfoBase sourceFileInfo = DiskFileSystem.Default.CreateFileInfo(packet.SourcePath);

            if (sourceFileInfo.IsDirectory)
            {
                string[] filenames = System.IO.Directory.GetFiles(packet.SourcePath);
                foreach (string filename in filenames)
                {
                    TransferPacket newPacket = new TransferPacket(packet, filename);
                    AddUploadSingle(newPacket);
                }
            }
        }
        
        private void AddDownloadSingle(TransferPacket packet)
        {
            FileInfoBase sourceFileInfo;
            try
            {
                sourceFileInfo = client.CreateFileInfo(packet.SourcePath);
            }
            catch(Exception ex)
            {
                packet.FireFailureEvent(ex.Message);
                return;
            }

            FileInfoBase targetFileInfo = DiskFileSystem.Default.CreateFileInfo(packet.TargetPath);

            ProgressFileItem pfi = queue.Add(sourceFileInfo, targetFileInfo, false, FileExistsAction.Overwrite, 1);
            packet.FileItem = pfi;

            transferPacketList.Add(packet);
        }

        private void AddUploadSingle(TransferPacket packet)
        {
            FileInfoBase sourceFileInfo = DiskFileSystem.Default.CreateFileInfo(packet.SourcePath);
            FileInfoBase targetFileInfo;
            try
            {
                targetFileInfo = client.CreateFileInfo(packet.TargetPath);
            }
            catch (Exception ex)
            {
                packet.FireFailureEvent(ex.Message);
                return;
            }

            ProgressFileItem pfi = queue.Add(sourceFileInfo, targetFileInfo, false, FileExistsAction.Overwrite, 1);
            packet.FileItem = pfi;

            transferPacketList.Add(packet);
        }

        public static TransferQueueManager Instance
        {
            get
            {
                lock (syncRoot)
                {
                    if (instance == null)
                    {
                        instance = new TransferQueueManager();
                    }
                }

                return instance;
            }
        }

    }
}
