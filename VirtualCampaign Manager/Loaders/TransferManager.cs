using ComponentPro.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Workers;

namespace VirtualCampaign_Manager.Loaders
{
    public class ResultEventArgs: EventArgs
    {
        public Object Result { get; set; }

        public ResultEventArgs(Object Result)
        {
            this.Result = Result;
        }
    }

    public static class TransferManager
    {
        public static EventHandler<ResultEventArgs> SuccessEvent;
        public static EventHandler<ResultEventArgs> FailureEvent;

        private static Dictionary<string, Sftp> FtpClientsDict = new Dictionary<string, Sftp>();
        private static Dictionary<string, int> FtpTransfersDict = new Dictionary<string, int>();

        private static List<TransferPacket> TransferPacketList = new List<TransferPacket>();

        public static void AddTransferPacket(TransferPacket Packet)
        {
            if (TransferPacketList.Any(item => item.SourcePath == Packet.SourcePath && item.TargetPath == Packet.TargetPath) == false)
            {
                TransferPacketList.Add(Packet);
                CheckClient(Packet);
                Continue();
            }
        }

        private static void CheckClient(TransferPacket packet)
        {
            if (FtpClientsDict.ContainsKey(packet.LoginData.Url+packet.LoginData.Username) == false)
            {
                //initialize new Sftp connection
                Sftp ftpClient = new Sftp();
                ftpClient.Timeout = -1;
                ftpClient.ReconnectionMaxRetries = 10;
                ftpClient.DownloadFileCompleted += FtpClient_DownloadFileCompleted;
                FtpClientsDict[packet.LoginData.Url + packet.LoginData.Username] = ftpClient;
                FtpTransfersDict[packet.LoginData.Url + packet.LoginData.Username] = 0;
            }
        }

        private static void FtpClient_DownloadFileCompleted(object sender, ComponentPro.ExtendedAsyncCompletedEventArgs<long> e)
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
            Continue();
        }

        private static void Continue()
        {
            if (TransferPacketList.Count == 0) return;

            //get ftp connections with free slots
            List<KeyValuePair<string, int>> freeFtpList = FtpTransfersDict.Where(item => item.Value < Settings.MaxDownloadCount).ToList();

            //find files that will use any of the free ftp connections
            foreach (KeyValuePair<string, int> kvp in freeFtpList)
            {
                List<TransferPacket> packetList = TransferPacketList.Where(item =>(item.IsInTransit == false) && (item.LoginData.Url + item.LoginData.Username == kvp.Key)).ToList();
                if (packetList.Count == 0) continue;

                int packetsToTransfer = Math.Min(kvp.Value, packetList.Count);

                for (int i=0; i<packetsToTransfer; i++)
                {
                    TransferPacket thisPacket = packetList[i];
                    thisPacket.IsInTransit = true;

                    Sftp ftpClient = GetClientForTransfer(kvp.Key, thisPacket);
                    if (ftpClient.IsAuthenticated == true)
                    {
                        switch (thisPacket.Type)
                        {
                            case TransferType.RemoteDownload:
                                thisPacket.Task = ftpClient.DownloadFileAsync(thisPacket.SourcePath, thisPacket.TargetPath);
                                break;
                            case TransferType.RemoteUpload:
                                thisPacket.Task = ftpClient.UploadFileAsync(thisPacket.SourcePath, thisPacket.TargetPath);
                                break;
                        }
                        FtpTransfersDict[kvp.Key] += 1;
                    }
                }
            }
        }

        private static Sftp GetClientForTransfer(string clientKey, TransferPacket packet)
        {
            Sftp ftpClient = FtpClientsDict[clientKey];

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

        private static void FireSuccessEvent(TransferPacket packet)
        {
            EventHandler<ResultEventArgs> successEvent = SuccessEvent;
            if (successEvent != null)
            {
                successEvent(null, new ResultEventArgs(packet));
            }
        }

        private static void FireFailureEvent(TransferPacket packet)
        {
            EventHandler<ResultEventArgs> failureEvent = FailureEvent;
            if (failureEvent != null)
            {
                failureEvent(null, new ResultEventArgs(packet));
            }
        }
    }
}
