using ComponentPro.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Workers
{
    public class MoitfDownloader
    {
        public bool Success = false;

        Job job;
        Sftp ftpClient;
        List<Motif> motifDownloadList;
        Timer timer;

        public event EventHandler FinishedEvent;

        public MoitfDownloader(Job Job)
        {
            //neither product preview clips nor indicatives/abdicatives nee any resouces
            if (job.Production.IsPreview || job.IsDicative)
            {
                Success = true;
                FinishedEvent?.Invoke(this, new EventArgs());
                return;
            }

            //set up timer for iterative download
            timer = new Timer(Settings.DownloadInterval);
            timer.Elapsed += Timer_ContinueDownload;

            //set up list of motifs to download
            List<Motif> motifDownloadList = new List<Motif>();
            foreach (Motif motif in job.MotifList)
            {
                motifDownloadList.Add(motif);
            }

            //initialize Sftp connection
            Sftp ftpClient = new Sftp();
            ftpClient.Timeout = -1;
            ftpClient.ReconnectionMaxRetries = 10;
            ftpClient.DownloadFileCompleted += FtpClient_DownloadFileCompleted;
        }

        private void Timer_ContinueDownload(object sender, ElapsedEventArgs e)
        {
            //stop parallel downloads as long as it takes to download file
            timer.Stop();

            motifDownloadList.RemoveAt(0);
            if (motifDownloadList.Count > 0)
            {
                DownloadMotif(motifDownloadList[0]);
            }
            else
            {
                Success = true;
                FinishedEvent?.Invoke(this, new EventArgs());
            }
        }
      
        private void FtpClient_DownloadFileCompleted(object sender, ComponentPro.ExtendedAsyncCompletedEventArgs<long> e)
        {
            timer.Start();
        }
        
        private void DownloadMotif(Motif motif)
        {
            string sourceFileName = Settings.FtpUserDirectoryLogin.SubdirectoryPath + "/" + job.AccountID + "/motifs/" + motif.DownloadName;
            string targetFileName = Path.Combine(job.Production.ProductionDirectory, "motifs", motif.DownloadName);

            if (File.Exists(targetFileName))
            {
                timer.Start();
                return;
            }

            if (ftpClient.IsConnected == false)
            {
                string serverURL = Settings.FtpUserDirectoryLogin.Url;
                if (serverURL.Contains("ftp://"))
                {
                    serverURL = serverURL.Replace("ftp://", "");
                }
                ftpClient.Connect(serverURL);
            }

            if (ftpClient.IsAuthenticated == false)
            {
                string password = Settings.FtpUserDirectoryLogin.Password;
                string user = Settings.FtpUserDirectoryLogin.Username;
                ftpClient.Authenticate(user, password);
            }

            Console.WriteLine("FTP: " + Settings.FtpUserDirectoryLogin.FullPath + "/" + sourceFileName + " --> " + targetFileName);

            ftpClient.DownloadFileAsync(sourceFileName, targetFileName);
            
        }
    }
}
