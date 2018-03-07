using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Loaders;

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

        public void Iterate()
        {
            if (job.IsActive == false || job.Status == JobStatus.JS_DONE || job.ErrorStatus != JobErrorStatus.JES_NONE)
                return;

            bool success = false;

            switch (job.Status)
            {
                case JobStatus.JS_IDLE:
                    job.Status = JobStatus.JS_CREATE_DIRECTORIES;
                    goto case JobStatus.JS_CREATE_DIRECTORIES;                     
                case JobStatus.JS_CREATE_DIRECTORIES:
                    DirectoryWorker.SuccessEvent += OnCreateDirectorySuccess;
                    DirectoryWorker.FailureEvent += OnCreateDirectoryFailure;
                    DirectoryWorker.CreateJobDirectories(job);
                    break;
                case JobStatus.JS_PREPARE_RESOURCES:
                    DownloadMotifs();
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
     
        private void DownloadMotifs()
        {
            MotifTransferCounter = 0;

            foreach (Motif motif in job.MotifList)
            {
                TransferPacket motifTransferPacket = new TransferPacket(job, motif);
                motifTransferPacket.FailureEvent += OnMotifTransferFailure;
                motifTransferPacket.SuccessEvent += OnMotifTransferSuccess;
                TransferManager.AddTransferPacket(motifTransferPacket);
            }
        }
        
        private void OnCreateDirectorySuccess(object obj, EventArgs ea)
        {
            DirectoryWorker.SuccessEvent -= OnCreateDirectorySuccess;
            DirectoryWorker.FailureEvent -= OnCreateDirectoryFailure;
            job.Status = JobStatus.JS_PREPARE_RESOURCES;
            Iterate();
        }

        private void OnCreateDirectoryFailure(object obj, ResultEventArgs ea)
        {
            DirectoryWorker.SuccessEvent -= OnCreateDirectorySuccess;
            DirectoryWorker.FailureEvent -= OnCreateDirectoryFailure;
            job.ErrorStatus = JobErrorStatus.JES_CREATE_DIRECTORIES;
            FailureEvent?.Invoke(this, ea);
        }

        private void OnMotifTransferSuccess(object obj, EventArgs ea)
        {
            TransferPacket motifTransferPacket = obj as TransferPacket;
            motifTransferPacket.FailureEvent -= OnMotifTransferFailure;
            motifTransferPacket.SuccessEvent -= OnMotifTransferSuccess;

            MotifTransferCounter += 1;
            if (MotifTransferCounter == job.MotifList.Count)
            {
                job.Status = JobStatus.JS_CREATE_RENDERFILES;
                Iterate();
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
    }
}
