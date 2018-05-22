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
    public sealed class TransferManager : EventFireBase
    {
        private Sftp client;
        public TransferPacket Packet;

        public TransferManager(TransferPacket packet)
        {
            this.Packet = packet;
        }

        public void Transfer()
        {
            bool connected = Connectclient();

            if (connected)
            {
                AddEventHandlers();
                InitiateTransfer();
            }
            else
            {

            }
        }
        
        private void AddEventHandlers()
        {
            switch (Packet.Type)
            {
                case TransferType.DownloadAnimatedMotif:
                case TransferType.DownloadAudio:
                case TransferType.DownloadMotif:
                    client.DownloadFileCompleted += Client_DownloadFileCompleted;
                    break;
                case TransferType.UploadFilmDirectory:
                case TransferType.UploadFilmPreviewDirectory:
                case TransferType.UploadProductDirectory:
                case TransferType.UploadProductPreviewDirectory:
                    client.UploadFileCompleted += Client_UploadFileCompleted;
                    client.UploadDirectoryCompleted += Client_UploadDirectoryCompleted;
                    break;
                case TransferType.UploadMotifPreview:
                    client.UploadFileCompleted += Client_UploadFileCompleted;
                    break;
            }
        }

        private void RemoveEventHandlers()
        {
            switch (Packet.Type)
            {
                case TransferType.DownloadAnimatedMotif:
                case TransferType.DownloadAudio:
                case TransferType.DownloadMotif:
                    client.DownloadFileCompleted -= Client_DownloadFileCompleted;
                    break;
                case TransferType.UploadFilmDirectory:
                case TransferType.UploadFilmPreviewDirectory:
                case TransferType.UploadProductDirectory:
                case TransferType.UploadProductPreviewDirectory:
                    client.UploadFileCompleted -= Client_UploadFileCompleted;
                    client.UploadDirectoryCompleted -= Client_UploadDirectoryCompleted;
                    break;
                case TransferType.UploadMotifPreview:
                    client.UploadFileCompleted -= Client_UploadFileCompleted;
                    break;
            }
        }

        private void InitiateTransfer()
        {
            Packet.IsInTransit = true;

            switch (Packet.Type)
            {
                case TransferType.DownloadAnimatedMotif:
                case TransferType.DownloadAudio:
                case TransferType.DownloadMotif:
                    client.DownloadFileAsync(Packet.SourcePath, Packet.TargetPath, Packet);
                    break;
                case TransferType.UploadFilmDirectory:
                case TransferType.UploadFilmPreviewDirectory:
                case TransferType.UploadProductDirectory:
                case TransferType.UploadProductPreviewDirectory:
                    client.UploadDirectoryAsync(Packet.SourcePath, Packet.TargetPath, Packet);
                    break;
                case TransferType.UploadMotifPreview:
                    client.UploadFileAsync(Packet.SourcePath, Packet.TargetPath, Packet);
                    break;
            }
        }

        private void Client_UploadDirectoryCompleted(object sender, ComponentPro.ExtendedAsyncCompletedEventArgs<ComponentPro.IO.FileSystemTransferStatistics> e)
        {
            client.UploadDirectoryCompleted -= Client_UploadDirectoryCompleted;
            client.Disconnect();

            HandleFinishedTransfer(e.Error);
        }

        private void Client_UploadFileCompleted(object sender, ComponentPro.ExtendedAsyncCompletedEventArgs<long> e)
        {
            Sftp client = sender as Sftp;
            client.UploadFileCompleted -= Client_UploadFileCompleted;
            client.Disconnect();

            HandleFinishedTransfer(e.Error);
        }

        private void Client_DownloadFileCompleted(object sender, ComponentPro.ExtendedAsyncCompletedEventArgs<long> e)
        {
            Sftp client = sender as Sftp;
            client.DownloadFileCompleted -= Client_DownloadFileCompleted;
            client.Disconnect();

            HandleFinishedTransfer(e.Error);
        }

        private void HandleFinishedTransfer(Exception error)
        { 
            if (Packet != null)
            {
                Packet.IsInTransit = false;

                if (error == null)
                {
                    RemoveEventHandlers();
                    FireSuccessEvent();
                }
                else
                {
                    Packet.TransferErrorCounter += 1;
                    if (Packet.TransferErrorCounter > Settings.MaxTransferErrorCount)
                    {
                        RemoveEventHandlers();
                        FireFailureEvent();
                    }
                }
            }
        }

        private bool Connectclient()
        {
            //initialize new Sftp connection
            client = new Sftp();
            client.Timeout = -1;
            client.ReconnectionMaxRetries = 10;

            //connect to client
            try
            {
                client.Connect(Packet.LoginData.Url);
            }
            catch (Exception ex)
            {
                return false;
            }

            //authenticate user
            try
            {
                client.Authenticate(Packet.LoginData.Username, Packet.LoginData.Password);
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }
    }
}
