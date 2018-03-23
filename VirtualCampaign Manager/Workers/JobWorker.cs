using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Encoding;
using VirtualCampaign_Manager.Helpers;
using VirtualCampaign_Manager.Rendering;
using VirtualCampaign_Manager.Transfers;

namespace VirtualCampaign_Manager.Workers
{
    public class JobWorker : EventFireBase
    {
        public int MotifTransferCounter;

        //current job
        private Job job;

        private JobRenderProgressMonitor renderProgressMonitor;

        public JobWorker(Job Job)
        {
            this.job = Job;
            this.renderProgressMonitor = new JobRenderProgressMonitor(this.job);
        }

        public void Work()
        {
            if (job.IsActive == false || job.Status == JobStatus.JS_DONE || job.ErrorStatus != JobErrorStatus.JES_NONE)
                return;

            switch (job.Status)
            {
                case JobStatus.JS_IDLE:
                    job.Status = JobStatus.JS_CREATE_DIRECTORIES;
                    goto case JobStatus.JS_CREATE_DIRECTORIES;
                case JobStatus.JS_CREATE_DIRECTORIES:
                    CreateDirectories();
                    break;
                case JobStatus.JS_PREPARE_RESOURCES:
                    DownloadMotifs();
                    break;
                case JobStatus.JS_CREATE_RENDERFILES:
                    PrepareRenderFiles();
                    break;
                case JobStatus.JS_SEND_RENDER_JOB:
                    //DEBUG: SKIP THIS STEP
                    //RenderJob();
                    MonitorRenderStatus();
                    break;
                case JobStatus.JS_GET_JOB_ID:
                    job.Status = JobStatus.JS_RENDER_JOB;
                    break;
                case JobStatus.JS_RENDER_JOB:
                    MonitorRenderStatus();
                    break;
                case JobStatus.JS_SEND_ENCODE_JOB:
                    EncodeJob();
                    break;
                case JobStatus.JS_ENCODINGDONE:
                    CleanUp(reset: false);
                    break;
            }
        }

        private void CreateDirectories()
        {
            job.LogText("Create directory " + JobPathHelper.GetLocalJobDirectory(job));
            job.LogText("Create directory " + JobPathHelper.GetLocalJobRenderOutputDirectory(job));

            bool success = IOHelper.CreateDirectory(JobPathHelper.GetLocalJobDirectory(job));
            success = success && IOHelper.CreateDirectory(JobPathHelper.GetLocalJobRenderOutputDirectory(job));

            if (success == false)
            {
                job.ErrorStatus = JobErrorStatus.JES_CREATE_DIRECTORIES;
                return;
            }

            job.Status = JobStatus.JS_PREPARE_RESOURCES;
            Work();
        }

        private void DownloadMotifs()
        {
            MotifTransferCounter = 0;

            foreach (Motif motif in job.MotifList)
            {
                TransferPacket motifTransferPacket = new TransferPacket(job, motif);
                motifTransferPacket.FailureEvent += OnMotifTransferFailure;
                motifTransferPacket.SuccessEvent += OnMotifTransferSuccess;
                DownloadManager.Instance.AddTransferPacket(motifTransferPacket);
            }
        }

        private void PrepareRenderFiles()
        {
            RenderFilePreparer renderFilePreparer = new RenderFilePreparer(job);
            if (renderFilePreparer.Prepare() == true)
            {
                job.Status = JobStatus.JS_SEND_RENDER_JOB;
                Work();
            }
        }

        private void RenderJob()
        {
            if (DeadlineRenderer.Render(job) == true)
            {
                job.Status = JobStatus.JS_RENDER_JOB;
                Work();
            }
            else
            {
                job.ErrorStatus = JobErrorStatus.JES_DEADLINE_REGISTER_RENDERJOB;
            }
        }

        private void MonitorRenderStatus()
        {
            RenderMonitor renderMonitor = new RenderMonitor(job);
            renderMonitor.FailureEvent += OnRenderFailure;
            renderMonitor.SuccessEvent += OnRenderSuccess;
            renderMonitor.Start();
        }

        private void OnRenderFailure(object sender, EventArgs ea)
        {
            (sender as RenderMonitor).FailureEvent -= OnRenderFailure;
            (sender as RenderMonitor).SuccessEvent -= OnRenderSuccess;
            job.ErrorStatus = JobErrorStatus.JES_DEADLINE_RENDER_JOB;
            FireFailureEvent();
        }

        private void OnRenderSuccess(object sender, EventArgs ea)
        {
            (sender as RenderMonitor).FailureEvent -= OnRenderFailure;
            (sender as RenderMonitor).SuccessEvent -= OnRenderSuccess;
            job.Status = JobStatus.JS_SEND_ENCODE_JOB;
            Work();
        }

        private void EncodeJob()
        {
            bool isZipProduction = job.Production.Film.FilmOutputFormatList.Any(item => item.Name.ToLower().Contains("zip"));
            if (isZipProduction)
            {
                if (ZipFileCreator.Create(job) == false)
                {
                    job.ErrorStatus = JobErrorStatus.JES_CREATE_ZIP;
                    return;
                }
            }

            job.Status = JobStatus.JS_ENCODINGDONE;
            Work();
        }


        private void OnMotifTransferSuccess(object obj, EventArgs ea)
        {
            TransferPacket motifTransferPacket = obj as TransferPacket;
            motifTransferPacket.FailureEvent -= OnMotifTransferFailure;
            motifTransferPacket.SuccessEvent -= OnMotifTransferSuccess;

            Motif motif = motifTransferPacket.Parent as Motif;
            if (motif != null)
            {
                if (MotifTranscoder.Transcode(job, motif) != true)
                {
                    job.ErrorStatus = JobErrorStatus.JES_MODIFY_MOTIF;
                    return;
                }
            }

            MotifTransferCounter += 1;
            if (MotifTransferCounter == job.MotifList.Count)
            {
                job.Status = JobStatus.JS_CREATE_RENDERFILES;
                Work();
            }
        }

        private void OnMotifTransferFailure(object obj, EventArgs ea)
        {
            TransferPacket motifTransferPacket = obj as TransferPacket;
            motifTransferPacket.FailureEvent -= OnMotifTransferFailure;
            motifTransferPacket.SuccessEvent -= OnMotifTransferSuccess;

            job.ErrorStatus = JobErrorStatus.JES_DOWNLOAD_MOTIFS;
            FireFailureEvent();
        }

        private void CleanUp(bool reset)
        {
            DeadlineRenderer.DeleteJob(job);

            if (reset == false)
            {
                job.Status = JobStatus.JS_DONE;
                FireSuccessEvent();
            }
            else
            {
                job.ErrorStatus = JobErrorStatus.JES_NONE;
                job.Status = JobStatus.JS_IDLE;
            }
        }
    }
}