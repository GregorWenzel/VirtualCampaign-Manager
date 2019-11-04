using ComponentPro.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Transfers
{
    public class TransferManager : EventFireBase
    {
        public TransferPacket Packet;

        private Sftp client;
        private Timer sleepTimer;
        private int connectErrorCount;
        private int sleepTimerMultiplier;

        public TransferManager()
        {
            InitializeClient();
        }

        private void InitializeClient()
        { 
            client = new Sftp();
            client.Timeout = -1;
            client.ReconnectionMaxRetries = Settings.MaxTransferErrorCount;
            client.ReconnectionFailureDelay = 5 * 1000;
            client.ConnectCompleted += Client_ConnectCompleted;
            client.AuthenticateCompleted += Client_AuthenticateCompleted;
        }

        private void Client_AuthenticateCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                Log("Adding file transfer: " + Packet.SourcePath + " -> " + Packet.TargetPath);

                switch (Packet.Type)
                {
                    case TransferType.DownloadAnimatedMotif:
                    case TransferType.DownloadAudio:
                    case TransferType.DownloadMotif:
                        DownloadSingle();
                        break;
                    case TransferType.UploadFilmDirectory:
                    case TransferType.UploadFilmPreviewDirectory:
                    case TransferType.UploadProductDirectory:
                    case TransferType.UploadProductPreviewDirectory:
                        UploadDirectory();
                        break;
                    case TransferType.UploadMotifPreview:
                        UploadSingle();
                        break;
                }
            }
            else
            {
                Packet.FireFailureEvent(e.Error);
            }
        }

        private void DownloadSingle()
        {
            client.DownloadFileCompleted += Client_DownloadFileCompleted;
            client.DownloadFileAsync(Packet.SourcePath, Packet.TargetPath);
        }

        private void Client_DownloadFileCompleted(object sender, ComponentPro.ExtendedAsyncCompletedEventArgs<long> e)
        {
            client.DownloadFileCompleted -= Client_DownloadFileCompleted;
            HandleFileTransferCompleted(e.Error);
        }

        private void UploadDirectory()
        {
            client.UploadDirectoryCompleted += Client_UploadDirectoryCompleted;
            client.UploadDirectoryAsync(Packet.SourcePath, Packet.TargetPath);
        }

        private void Client_UploadDirectoryCompleted(object sender, ComponentPro.ExtendedAsyncCompletedEventArgs<ComponentPro.IO.FileSystemTransferStatistics> e)
        {
            client.UploadDirectoryCompleted -= Client_UploadDirectoryCompleted;
            HandleFileTransferCompleted(e.Error);
        }

        private void UploadSingle()
        {
            client.UploadFileCompleted += Client_UploadFileCompleted;
            client.UploadFileAsync(Packet.SourcePath, Packet.TargetPath);
        }

        private void Client_UploadFileCompleted(object sender, ComponentPro.ExtendedAsyncCompletedEventArgs<long> e)
        {
            client.UploadFileCompleted -= Client_UploadFileCompleted;
            HandleFileTransferCompleted(e.Error);
        }

        private void HandleFileTransferCompleted(Exception e)
        {
            client.ConnectCompleted -= Client_ConnectCompleted;
            client.AuthenticateCompleted -= Client_AuthenticateCompleted;
            if (client.IsConnected)
            {
                client.Disconnect();
            }
            client = null;

            if (e == null)
            {
                Log("Transfer SUCCESS: " + Packet.Parent + " " + Packet.SourcePath + " -> " + Packet.TargetPath);
                Packet.FireSuccessEvent();                        
            }
            else
            {
                Log("Transfer ERROR: " + Packet.Parent + " " + Packet.SourcePath + " -> " + Packet.TargetPath);
                Log(e.Message);
                Packet.FireFailureEvent(e);
            }
        }

        private void Client_ConnectCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                client.AuthenticateAsync(Settings.MasterLogin.Username, Settings.MasterLogin.Password);
            }
            else
            {
                connectErrorCount += 1;
                if (connectErrorCount < Settings.MaxTransferErrorCount)
                {
                    client.ConnectAsync(Settings.MasterLogin.Url, 22);
                }
                else
                {
                    Sleep();
                }
            }
        }

        private void Sleep()
        {
            sleepTimerMultiplier += 1;

            if (sleepTimer == null)
            {
                sleepTimer = new Timer();
                sleepTimer.Elapsed += SleepTimer_Elapsed;
                sleepTimer.Interval = 60 * 1000;
            }

            sleepTimer.Interval *= sleepTimerMultiplier;

            Log("Cannot establish connection to " + Settings.MasterLogin.Url + ". Waiting for " + Math.Ceiling(sleepTimer.Interval / (60 * 1000)) + " minutes to try again...");

            sleepTimer.Start();
        }

        private void SleepTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            connectErrorCount = 0;
            if (client == null)
            {
                InitializeClient();
            }

            client.ConnectAsync(Settings.MasterLogin.Url, 22);
        }

        public void Transfer(TransferPacket packet)
        {
            connectErrorCount = 0;
            sleepTimerMultiplier = 0;
            this.Packet = packet;

            client.ConnectAsync(Settings.MasterLogin.Url, 22);
        }

        private void Log(string text)
        {
            if (Packet.Parent is VCObject)
            {
                (Packet.Parent as VCObject).LogText(text);
            }
            DateTime date = DateTime.Now;

            string logFilename = string.Format("{0:0000}_{1:00}_{2:00}_transfers.log", date.Year, date.Month, date.Day);
            string logFilePath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VC Render Manager", "transfer logs", logFilename);
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (!Directory.Exists(Path.GetDirectoryName(logFilePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
                }
                File.AppendAllText(logFilePath, string.Format("[{0}-{1}]: {2}\r\n", date.ToLongDateString(), date.ToLongTimeString(), text));
                }
            ));
        }

    }
}
