using ComponentPro.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Helpers;

namespace VirtualCampaign_Manager.Transfers
{
    public sealed class TransferManager
    {
        private List<TransferPacket> transferList = new List<TransferPacket>();

        private int activePacketCount
        {
            get
            {
                return transferList.Count(item => item.IsInTransit);
            }
        }

        public TransferPacket GetTransferPacket(object itemID)
        {
            if (transferList.Any(item => (string)item.ItemID == (string)itemID))
            {
                return transferList.First(item => (string)item.ItemID == (string)itemID);
            }
            else
            {
                return null;
            }
        }
            
        public void AddTransferPacket(TransferPacket packet)
        {
            packet.IsInTransit = false;

            transferList.Add(packet);

            ProcessQueue();
        }

        private void ProcessQueue()
        {
            List<TransferPacket> idleTransferList = transferList.Where(item => item.IsInTransit == false).ToList();

            int packetsToProcess = Settings.MaxDownloadThreads - idleTransferList.Count;
            for (int i=0; i<packetsToProcess; i++)
            {
                TransferPacket newPacket = idleTransferList[i];
                newPacket.Client = GetFtpClient(newPacket);

                if (newPacket.Client != null)
                {
                    InitiateTransfer(newPacket);
                }
            }
        }

        private void InitiateTransfer(TransferPacket packet)
        {
            packet.IsInTransit = true;

            switch (packet.Type)
            {
                case TransferType.DownloadAnimatedMotif:
                case TransferType.DownloadAudio:
                case TransferType.DownloadMotif:
                    packet.Client.DownloadFileCompleted += Client_DownloadFileCompleted;
                    packet.Client.DownloadFileAsync(packet.SourcePath, packet.TargetPath, packet);
                    break;
                case TransferType.UploadFilmDirectory:
                case TransferType.UploadFilmPreviewDirectory:
                case TransferType.UploadProductDirectory:
                case TransferType.UploadProductPreviewDirectory:
                    packet.Client.UploadFileCompleted += Client_UploadFileCompleted;
                    packet.Client.UploadDirectoryCompleted += Client_UploadDirectoryCompleted;
                    packet.Client.UploadDirectoryAsync(packet.SourcePath, packet.TargetPath, packet);
                    break;
                case TransferType.UploadMotifPreview:
                    packet.Client.UploadFileCompleted += Client_UploadFileCompleted;
                    packet.Client.UploadFileAsync(packet.SourcePath, packet.TargetPath, packet);
                    break;
            }
        }

        private void Client_UploadDirectoryCompleted(object sender, ComponentPro.ExtendedAsyncCompletedEventArgs<ComponentPro.IO.FileSystemTransferStatistics> e)
        {
            ProcessQueue();
        }

        private void Client_UploadFileCompleted(object sender, ComponentPro.ExtendedAsyncCompletedEventArgs<long> e)
        {
            ProcessQueue();
        }

        private void Client_DownloadFileCompleted(object sender, ComponentPro.ExtendedAsyncCompletedEventArgs<long> e)
        {
            Sftp client = sender as Sftp;
            client.DownloadFileCompleted -= Client_DownloadFileCompleted;

            TransferPacket packet = e.UserState as TransferPacket;

            if (packet != null)
            {
                packet.IsInTransit = false;
                if (e.Error == null)
                {
                    packet.FireSuccessEvent();
                }
                else
                {
                    packet.FireFailureEvent();
                }
            }

            ProcessQueue();
        }

        private Sftp GetFtpClient(TransferPacket packet)
        {
            Sftp ftpClient;

            //initialize new Sftp connection
            ftpClient = new Sftp();
            ftpClient.Timeout = -1;
            ftpClient.ReconnectionMaxRetries = 10;

            //connect to client
            try
            {
                ftpClient.Connect(packet.LoginData.Url);
            }
            catch (Exception ex)
            {

            }

            //authenticate user
            try
            {
                ftpClient.Authenticate(packet.LoginData.Username, packet.LoginData.Password);
            }
            catch (Exception ex)
            {
                
            }

            return ftpClient;
        }


        public TransferManager()
        { }

        private static volatile TransferManager instance;
        private static object syncRoot = new object();

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

    }
}
