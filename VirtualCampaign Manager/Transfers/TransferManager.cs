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
    public sealed class _TransferManager : EventFireBase
    {
        private Sftp client;
        public TransferPacket Packet;
        private int connectionAttempts = 0;

        public _TransferManager(TransferPacket packet)
        {
            this.Packet = packet;
        }

        public void Transfer(bool addEventHandlers = true)
        {
            while (connectionAttempts <= Settings.MaxTransferErrorCount)
            {
                Exception clientException = Connectclient();

                if (clientException == null)
                {
                    if (addEventHandlers)
                    {
                        AddEventHandlers();
                    }
                    InitiateTransfer();
                    break;
                }
                
                Console.WriteLine("Connection to " + Packet.LoginData.Url + " failed: " + clientException.Message);

                connectionAttempts += 1;

                Console.WriteLine("Retry attempt no. " + connectionAttempts);
                
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
            client.Disconnect();
            HandleFinishedTransfer(e.Error);
        }

        private void Client_UploadFileCompleted(object sender, ComponentPro.ExtendedAsyncCompletedEventArgs<long> e)
        {
            client.Disconnect();
            HandleFinishedTransfer(e.Error);
        }

        private void Client_DownloadFileCompleted(object sender, ComponentPro.ExtendedAsyncCompletedEventArgs<long> e)
        {
            client.Disconnect();
            HandleFinishedTransfer(e.Error);
        }

        private void HandleFinishedTransfer(Exception error)
        {
            Packet.IsInTransit = false;

            if (error == null)
            {
                RemoveEventHandlers();
                FireSuccessEvent();
            }
            else
            {
                Console.WriteLine("Transfer Error: " + error.Message + " for packet " + Packet.SourcePath + " -> " + Packet.TargetPath);
                Packet.TransferErrorCounter += 1;
                if (Packet.TransferErrorCounter > Settings.MaxTransferErrorCount)
                {
                    RemoveEventHandlers();
                    FireFailureEvent();
                    return;
                }
                Transfer(false);
            }
        }

        private Exception Connectclient()
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
                client.Disconnect();
                return ex;
            }

            //authenticate user
            try
            {
                client.Authenticate(Packet.LoginData.Username, Packet.LoginData.Password);
            }
            catch (Exception ex)
            {
                client.Disconnect();
                return ex;
            }

            return null;
        }
    }
}
