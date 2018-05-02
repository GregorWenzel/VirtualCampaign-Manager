using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Encoding;
using VirtualCampaign_Manager.Helpers;
using VirtualCampaign_Manager.Rendering;
using VirtualCampaign_Manager.Repositories;
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
            if (job.Status == JobStatus.JS_DONE || job.ErrorStatus != JobErrorStatus.JES_NONE)
                return;

            switch (job.Status)
            {
                case JobStatus.JS_IDLE:
                    job.Status = JobStatus.JS_PREPARE_RESOURCES;
                    goto case JobStatus.JS_PREPARE_RESOURCES;
                case JobStatus.JS_PREPARE_RESOURCES:
                    DownloadMotifs();
                    break;
                case JobStatus.JS_CREATE_RENDERFILES:
                    PrepareRenderFiles();
                    break;
                case JobStatus.JS_SEND_RENDER_JOB:
                    RenderJob();
                    break;
                case JobStatus.JS_GET_JOB_ID:
                    job.Status = JobStatus.JS_RENDER_JOB;
                    break;
                case JobStatus.JS_RENDER_JOB:
                    MonitorRenderStatus();
                    break;
                case JobStatus.JS_SEND_ENCODE_JOB:
                    PrepareJobForZip();
                    break;
                case JobStatus.JS_ENCODING_DONE:
                    WriteStatistics();
                    CleanUp(reset: false);
                    break;
            }
        }

        //create job statistics data and send to server
        private void WriteStatistics()
        {
            if (job.FrameCount == 0) return;

            float totalSeconds = 0;
            int totalFrames = 0;
            float totalProcessorFactor = 0;

            foreach (RenderChunkStatus renderChunk in job.RenderChunkStatusList)
            {
                TimeSpan renderTimeSpan = renderChunk.RenderEndDate - renderChunk.RenderStartDate;
                float seconds = (float)Math.Round(renderTimeSpan.TotalSeconds, 2);
                totalSeconds += seconds;
                int frames = (renderChunk.EndFrame - renderChunk.StartFrame + 1);
                totalFrames += frames;

                totalProcessorFactor += ((float)frames / (float)job.FrameCount) * renderChunk.Processors * renderChunk.ProcessorSpeed;
            }

            float totalStandardizedTime = (float)Math.Round(totalSeconds * totalProcessorFactor / (float)job.FrameCount, 2);

            string filename = JobPathHelper.GetJobClipPath(job);
            FileInfo fileInfo = new FileInfo(filename);

            Dictionary<string, string> param = new Dictionary<string, string>
            {   { "productID", job.ProductID.ToString() },
                { "seconds", ((decimal)totalSeconds).ToString().Replace(",",".") },
                { "processorFactor", ((decimal)totalProcessorFactor).ToString().Replace(",",".") },
                { "standardizedComplexity",  ((decimal)totalStandardizedTime).ToString().Replace(",",".") },
                { "filesize", fileInfo.Length.ToString() }
            };

            JobRepository.UpdateProductStatistics(param);

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

                //check if the same transfer is already in progress (other job with the same motif id)
                TransferPacket inTransitPacket = TransferManager.Instance.GetTransferPacket(motifTransferPacket.ItemID);

                //if transfer already in proggress just subscribe to finished/failure events of that transfer
                if (inTransitPacket != null)
                {
                    inTransitPacket.FailureEvent += OnMotifTransferFailure;
                    inTransitPacket.SuccessEvent += OnMotifTransferSuccess;
                }
                else
                {
                    motifTransferPacket.FailureEvent += OnMotifTransferFailure;
                    motifTransferPacket.SuccessEvent += OnMotifTransferSuccess;
                    TransferManager.Instance.AddTransferPacket(motifTransferPacket);
                }
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

            if (job.IsZip)
            {
                job.Status = JobStatus.JS_ENCODE_JOB;
            }
            else
            {
                job.Status = JobStatus.JS_ENCODING_DONE;
            }
            Work();
        }

        //Pack Zip File
        private void PrepareJobForZip()
        {
            //Remove trailing digits from output pic (e.g. pic0000.jpg -> pic.jpg)
            //CAVE! four trailing digits are expected!
            string sourceDirectory = JobPathHelper.GetLocalJobRenderOutputPathForZip(job);
            if (!Directory.Exists(sourceDirectory)) return;

            DirectoryInfo dirInfo = new DirectoryInfo(sourceDirectory);
            FileInfo[] fileInfos = dirInfo.GetFiles();

            foreach (FileInfo fileInfo in fileInfos)
            {
                string sourceFullName = fileInfo.FullName;
                string extension = Path.GetExtension(sourceFullName);
                string dir = Path.GetDirectoryName(sourceFullName);
                string sourceFileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFullName);
                int len = sourceFileNameWithoutExtension.Length;
                if (char.IsDigit(sourceFileNameWithoutExtension[len - 4]) && char.IsDigit(sourceFileNameWithoutExtension[len - 1]))
                {
                    string targetNameWithoutExtension = sourceFileNameWithoutExtension.Substring(0, len - 4);
                    string targetName = Path.Combine(dir, targetNameWithoutExtension + extension);
                    File.Move(sourceFullName, targetName);
                }
            }
 
            job.Status = JobStatus.JS_ENCODING_DONE;
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
                if (motif.IsMovie)
                {
                    if (MotifTranscoder.Extract(job, motif) != true)
                    {
                        job.ErrorStatus = JobErrorStatus.JES_EXTRACT_MOTIF;
                        return;
                    }
                }
                else if (MotifTranscoder.Transcode(job, motif) != true)
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

        public void CleanUp(bool reset)
        {
            if (reset == false)
            {
                job.Status = JobStatus.JS_DONE;
                FireSuccessEvent();
            }
            else
            {
                DeadlineRenderer.DeleteJob(job);
                job.ErrorStatus = JobErrorStatus.JES_NONE;
                job.Status = JobStatus.JS_IDLE;
            }
        }
    }
}