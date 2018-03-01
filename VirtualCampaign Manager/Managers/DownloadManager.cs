using ComponentPro.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Managers
{
    public class DownloadManager
    {
        private List<DownloadData> DownloadList;
        private Dictionary<string, Sftp> ftpClientDict;
        private int downloadCounter;

        private DownloadManager()
        {
            ftpClientDict = new Dictionary<string, Sftp>();
            DownloadList = new List<DownloadData>();
            downloadCounter = 0;
        }

        public void DownloadMotifs(Job Job)
        {
            Job.MotifsAvailableCount = 0;
            foreach (Motif motif in Job.MotifList)
            {
                string sourceFileName = Settings.FtpUserDirectoryLogin.SubdirectoryPath + "/" + Job.AccountID + "/motifs/" + motif.DownloadName;
                string targetFileName = Path.Combine(Job.Production.ProductionDirectory, "motifs", motif.DownloadName);

                if (File.Exists(targetFileName))
                {
                    Job.MotifsAvailableCount += 1;
                    if (Job.MotifsAvailableCount >= Job.MotifList.Count)
                    {
                        return;
                    }
                    else
                    {
                        continue;
                    }
                }

                DownloadData newDownload = new DownloadData();
                newDownload.RequestingObject = Job;
                newDownload.Type = DownloadType.DT_MOTIF;
                newDownload.LoginData = Settings.FtpUserDirectoryLogin;
                newDownload.SourcePath = sourceFileName;
                newDownload.TargetPath = targetFileName;
                DownloadList.Add(newDownload);
            }            
        }

        private void FtpClient_DownloadFileCompleted(object sender, ComponentPro.ExtendedAsyncCompletedEventArgs<long> e)
        {
            DownloadData downloadData = e.UserState as DownloadData;
            if (e.Error == null)
            {
                if (DownloadList.Contains(downloadData))
                {
                    DownloadList.Remove(downloadData);
                    (downloadData.RequestingObject as Job).MotifsAvailableCount += 1;
                }
            }
            else
            {
                if (downloadData.Type == DownloadType.DT_MOTIF)
                {
                    (downloadData.RequestingObject as Job).ErrorStatus = JobErrorStatus.JES_DOWNLOAD_MOTIFS;
                }
            }

            downloadCounter -= 1;
            ContinueDownload();
        }

        private void ContinueDownload()
        {
            int freeSlotCount = Settings.MaxDownloadCount - downloadCounter;

            for (int i = 0; i < Math.Max(freeSlotCount, DownloadList.Count); i++)
            {
                DownloadData downloadData = DownloadList[i];                
                if (File.Exists(downloadData.TargetPath) == false)
                {
                    Sftp ftpClient = GetFtpClient(downloadData);
                    ftpClient.DownloadFileAsync(downloadData.SourcePath, downloadData.TargetPath, downloadData);
                    downloadCounter += 1;
                }
            }
        }

        private Sftp GetFtpClient(DownloadData downloadData)
        {
            Sftp result;
            if (ftpClientDict.ContainsKey(downloadData.LoginData.Username))
            {
                result = ftpClientDict[downloadData.LoginData.Username];
            }
            else
            {
                result = new Sftp();
                result.Timeout = -1;
                result.ReconnectionMaxRetries = 10;
                result.DownloadFileCompleted += FtpClient_DownloadFileCompleted;
                ftpClientDict[downloadData.LoginData.Username] = result;
            }

            if (result.IsConnected == false)
            {
                string serverURL = downloadData.LoginData.Url;
                if (serverURL.Contains("ftp://"))
                {
                    serverURL = serverURL.Replace("ftp://", "");
                }
                result.Connect(serverURL);
            }

            if (result.IsAuthenticated == false)
            {
                string password = downloadData.LoginData.Password;
                string user = downloadData.LoginData.Username;
                result.Authenticate(user, password);
            }

            return result;
        }

        //Singleton
        private static volatile DownloadManager instance;
        private static object syncRoot = new Object();

        public static DownloadManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new DownloadManager();
                    }
                }

                return instance;
            }
        }
    }
}
