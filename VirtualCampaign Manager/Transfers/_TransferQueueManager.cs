using ComponentPro.IO;
using ComponentPro.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;
using System.Timers;
using System.IO;
using System.Threading;

namespace VirtualCampaign_Manager.Transfers
{
    public sealed class _TransferQueueManager
    {
        private static volatile _TransferQueueManager instance;
        private static object syncRoot = new Object();

        private List<TransferPacket> transferPacketList;

        private Sftp client;
        private int connectionAttempts;
        private TransferQueue queue;
        private LoginData loginData;

        private _TransferQueueManager()
        {
            transferPacketList = new List<TransferPacket>();

            loginData = Settings.MasterLogin;

            //initialize new Sftp connection
            client = new Sftp();
            client.Timeout = -1;            
            client.ReconnectionMaxRetries = Settings.MaxTransferErrorCount;
            client.ReconnectionFailureDelay = 5000;
            
            queue = new TransferQueue(3);
            queue.ReuseRemoteConnection = true;
            queue.ItemProcessed += Queue_ItemProcessed;
            queue.Start();
        }

        private bool Reconnect()
        {
            connectionAttempts = 0;
            while (connectionAttempts <= Settings.MaxTransferErrorCount && client.IsConnected == false)
            {
                Exception clientException = Connectclient();

                bool connected;
                int nativeErrorCode;
                client.GetConnectionState(out connected, out nativeErrorCode);

                string logString = queue.Statistics.FileList.Count.ToString() + " files already in queue.";
                logString += connected ? "Connection still alive" : "Connection closed. Error code: " + nativeErrorCode;

                Log(logString);
                
                if (clientException == null)
                {                    
                    return true;
                }

                Log("Connection to " + loginData.Url + " failed: " + clientException.Message);
                connectionAttempts += 1;
                Log("Retry attempt no. " + connectionAttempts);
            }

            return client.IsConnected;
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
            Log("Adding file transfer: " + Packet.SourcePath + " -> " + Packet.TargetPath);
            if (client.IsConnected == false)
            {
                int connectCounter = 0;
                bool success = Reconnect(); 
                while (success == false && connectCounter < Settings.MaxTransferErrorCount)
                {
                    Log("Connection to " + loginData.Url + " not possible at this moment. Waiting for " + Settings.MinutesToSleepBeforeRetry + " minutes to try again...");
                    connectCounter += 1;
                    Thread.Sleep(Convert.ToInt32(Math.Ceiling(1000 * 60 * Settings.MinutesToSleepBeforeRetry)));
                    success = Reconnect();
                }
                
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

        private void Log(string text)
        {
            string logFilePath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VC Render Manager", "transfers.log");
            File.AppendAllText(logFilePath, string.Format("[{0}-{1}]: {2}\r\n", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString(), text));
        }

        public static _TransferQueueManager Instance
        {
            get
            {
                lock (syncRoot)
                {
                    if (instance == null)
                    {
                        instance = new _TransferQueueManager();
                    }
                }

                return instance;
            }
        }

    }
}
