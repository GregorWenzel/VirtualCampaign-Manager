using ComponentPro.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VirtualCampaign_Manager.Helpers;

namespace VirtualCampaign_Manager.Transfers
{
    public sealed class TransferManager
    {
        private Sftp client;

        public void Transfer(TransferPacket packet)
        {
            packet.IsInTransit = false;

            transferList.Add(packet);
        }

        private void ProcessQueue()
        {
            List<TransferPacket> idleTransferList = transferList.Where(item => item.IsInTransit == false).ToList();

            List<TransferPacket> packetsInProcessList = transferList.Where(item => item.IsInTransit == true).ToList();
            int packetsToProcess = Math.Min(idleTransferList.Count, Settings.MaxDownloadThreads - packetsInProcessList.Count);

            for (int i=0; i<packetsToProcess; i++)
            {
                TransferPacket newPacket = idleTransferList[i];

                if (packetsInProcessList.Any(item => item.ItemID == newPacket.ItemID)) continue;

                newPacket.Client = GetFtpClient(newPacket);

                if (newPacket.Client != null)
                {
                    InitiateTransfer(newPacket);
                }
            }
        }

        private void AddEventHandlers(TransferPacket packet)
        {
            switch (packet.Type)
            {
                case TransferType.DownloadAnimatedMotif:
                case TransferType.DownloadAudio:
                case TransferType.DownloadMotif:
                    packet.Client.DownloadFileCompleted += Client_DownloadFileCompleted;
                    break;
                case TransferType.UploadFilmDirectory:
                case TransferType.UploadFilmPreviewDirectory:
                case TransferType.UploadProductDirectory:
                case TransferType.UploadProductPreviewDirectory:
                    packet.Client.UploadFileCompleted += Client_UploadFileCompleted;
                    packet.Client.UploadDirectoryCompleted += Client_UploadDirectoryCompleted;
                    break;
                case TransferType.UploadMotifPreview:
                    packet.Client.UploadFileCompleted += Client_UploadFileCompleted;
                    break;
            }
        }

        private void InitiateTransfer(TransferPacket packet)
        {
            AddEventHandlers(packet);
            packet.IsInTransit = true;

            switch (packet.Type)
            {
                case TransferType.DownloadAnimatedMotif:
                case TransferType.DownloadAudio:
                case TransferType.DownloadMotif:
                    packet.Client.DownloadFileAsync(packet.SourcePath, packet.TargetPath, packet);
                    break;
                case TransferType.UploadFilmDirectory:
                case TransferType.UploadFilmPreviewDirectory:
                case TransferType.UploadProductDirectory:
                case TransferType.UploadProductPreviewDirectory:
                    packet.Client.UploadDirectoryAsync(packet.SourcePath, packet.TargetPath, packet);
                    break;
                case TransferType.UploadMotifPreview:
                    packet.Client.UploadFileAsync(packet.SourcePath, packet.TargetPath, packet);
                    break;
            }
        }

        private void Client_UploadDirectoryCompleted(object sender, ComponentPro.ExtendedAsyncCompletedEventArgs<ComponentPro.IO.FileSystemTransferStatistics> e)
        {
            Sftp client = sender as Sftp;
            client.UploadDirectoryCompleted -= Client_UploadDirectoryCompleted;
            client.Disconnect();

            HandleFinishedTransfer(e.UserState as TransferPacket, e.Error);
        }

        private void Client_UploadFileCompleted(object sender, ComponentPro.ExtendedAsyncCompletedEventArgs<long> e)
        {
            Sftp client = sender as Sftp;
            client.UploadFileCompleted -= Client_UploadFileCompleted;
            client.Disconnect();

            HandleFinishedTransfer(e.UserState as TransferPacket, e.Error);
        }

        private void Client_DownloadFileCompleted(object sender, ComponentPro.ExtendedAsyncCompletedEventArgs<long> e)
        {
            Sftp client = sender as Sftp;
            client.DownloadFileCompleted -= Client_DownloadFileCompleted;
            client.Disconnect();

            HandleFinishedTransfer(e.UserState as TransferPacket, e.Error);
        }

        private void HandleFinishedTransfer(TransferPacket packet, Exception error)
        { 
            if (packet != null)
            {
                packet.IsInTransit = false;

                if (error == null)
                {
                    transferList.Remove(packet);
                    packet.RaiseSuccessEvent();
                }
                else
                {
                    packet.TransferErrorCounter += 1;
                    if (packet.TransferErrorCounter > Settings.MaxTransferErrorCount)
                    {
                        transferList.Remove(packet);
                        packet.RaiseFailureEvent();
                    }
                    else
                    {

                    }
                }
            }

            timer.Start();
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
        { 
            timer = new Timer();
            timer.Interval = 1000;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();
            ProcessQueue();
            timer.Start();
        }
    }
}
