using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Workers
{
    public class JobWorker
    {
        //Event called after job worker has finished all job-related tasks
        public event EventHandler FinishedEvent;

        //current job
        private Job job;

        private JobRenderProgressMonitor renderProgressMonitor;

        //timer for processing loop
        private Timer timer;

        public JobWorker(Job Job)
        {
            this.job = Job;

            this.timer = new Timer(Settings.WorkerProcessInterval);
            this.timer.Elapsed += Iterate;
            this.renderProgressMonitor = new JobRenderProgressMonitor(this.job);
        }

        private void Iterate(object sender, ElapsedEventArgs e)
        {
            if (job.IsActive == false || job.Status == JobStatus.JS_DONE || job.ErrorStatus != JobErrorStatus.JES_NONE)
                return;

            bool success;

            switch (job.Status)
            {
                case JobStatus.JS_IDLE:
                    job.Status = JobStatus.JS_CREATE_DIRECTORIES;
                    break;
                case JobStatus.JS_CREATE_DIRECTORIES:
                    success = DirectoryWorker.CreateJobDirectories(job);
                    if (success == true)
                    {
                        job.Status = JobStatus.JS_PREPARE_RESOURCES;
                    }
                    else
                    {
                        job.ErrorStatus = JobErrorStatus.JES_CREATE_DIRECTORIES;
                    }
                    break;
                case JobStatus.JS_PREPARE_RESOURCES:
                    success = DownloadWorker.DownloadResources(job);
                    if (success == true)
                    {
                        job.Status = JobStatus.JS_CREATE_RENDERFILES;
                    }
                    else
                    {
                        job.ErrorStatus = JobErrorStatus.JES_DOWNLOAD_MOTIFS;
                    }
                    break;
                case JobStatus.JS_CREATE_RENDERFILES:
                    PrepareRenderfiles();
                    break;
                case JobStatus.JS_SEND_RENDER_JOB:
                    RenderJob();
                    break;
                case JobStatus.JS_GET_JOB_ID:
                    Status = JobStatus.JS_RENDER_JOB;
                    break;
                case JobStatus.JS_RENDER_JOB:
                    CheckRenderStatus();
                    break;
                case JobStatus.JS_SEND_ENCODE_JOB:
                    //Alle außer ZIP normal encoden
                    if (_Production.CodecInfoList[0].Codec.ID != 12)
                    {
                        EncodeJob();
                    }
                    else
                    {
                        string sourceDirectory = Path.Combine(new string[] { this.SourceJobDirectory, "output" });

                        RemoveTrailingDigitsFromOutputFiles(sourceDirectory);

                        string targetFile = Path.Combine(_Production.EncodingProductionDirectory, _Production.Film.UrlHash, "film_" + _Production.FilmID + "_" + _Production.CodecInfoList[0].Codec.ID + _Production.CodecInfoList[0].Codec.Extension);

                        if (!Directory.Exists(Path.Combine(_Production.EncodingProductionDirectory, _Production.Film.UrlHash)))
                        {
                            Directory.CreateDirectory(Path.Combine(_Production.EncodingProductionDirectory, _Production.Film.UrlHash));
                        }

                        if (File.Exists(targetFile))
                            File.Delete(targetFile);

                        //Thread.Sleep(10000);

                        System.IO.Compression.ZipFile.CreateFromDirectory(sourceDirectory, targetFile);
                        this.Status = JobStatus.JS_ENCODINGDONE;
                    }
                    break;
                case JobStatus.JS_ENCODINGDONE:
                    reset = false;
                    CleanUp();
                    break;
            }
        }

        public void Continue()
        {
            timer.Start();
        }

        public void Pause()
        {
            timer.Stop();
        }

        protected virtual void OnFinishedEvent(EventArgs ea)
        {
            FinishedEvent?.Invoke(this, ea);
        }
    }
}
