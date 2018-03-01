using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Workers
{
    public class ProductionWorker
    {
        private static object syncRoot = new Object();

        public bool IsActive { get; set; }
        public bool IsFinished { get; set; }

        //Event called after production worker has finished all production tasks
        public event EventHandler FinishedEvent;

        //current production
        private Production production;

        public ProductionWorker(Production Production)
        {
            production = Production;
            IsActive = false;
            IsFinished = false;
        }

        protected virtual void OnFinishedEvent(EventArgs ea)
        {
            FinishedEvent?.Invoke(this, ea);
        }

        public void Work()
        {
            lock (syncRoot)
            {
                if (CheckStatusOk() == false) return;

                switch (production.Status)
                {
                    case ProductionStatus.PS_READY:
                        production.Status = ProductionStatus.PS_RENDER_JOBS;
                        break;
                    case ProductionStatus.PS_RENDER_JOBS:
                        ExecuteJobs();
                        break;
                    case ProductionStatus.PS_MUX_AUDIO:
                        //Zip format?
                        if (this.CodecInfoList[0].Codec.ID == 12)
                            UploadFilms();
                        else
                            EncodeAudio();
                        break;
                    case ProductionStatus.PS_JOIN_CLIPS:
                        JoinClips();
                        break;
                    case ProductionStatus.PS_ENCODE_FILMS:
                        EncodeFilms();
                        break;
                    case ProductionStatus.PS_UPLOAD_FILMS:
                        UploadFilms();
                        break;
                    case ProductionStatus.PS_UPDATE_HISTORY:
                        UpdateHistoryTable();
                        break;
                }
            }
        }

        private bool CheckStatusOk()
        {
            if (production.JobList == null || production.JobList.Count == 0)
            {
                IsActive = false;
                IsFinished = true;
                return false;
            }

            if (IsFinished == true) return false;

            if (!IsActive || production.Status == ProductionStatus.PS_DONE || production.ErrorStatus != ProductionErrorStatus.PES_NONE)
                return false;

            int jobsDoneCount = 0;
            foreach (Job job in production.JobList)
                if (job.Status == JobStatus.JS_DONE)
                    jobsDoneCount += 1;

            if (jobsDoneCount == production.JobList.Count && production.Status < ProductionStatus.PS_MUX_AUDIO)
            {
                production.Status = ProductionStatus.PS_MUX_AUDIO;
            }

            return true;
        }

        private void ExecuteJobs()
        {
            foreach (Job thisJob in production.JobList)
            {
                if (thisJob.Worker == null)
                {
                    thisJob.InitializeWorker();
                }
                if (thisJob.Status == JobStatus.JS_DONE)
                {
                    continue;
                }
                thisJob.IsActive = true;
                Thread jobThread = new Thread(new ThreadStart(thisJob.Execute));
                Console.WriteLine("NEW THREAD FOR JOB ID " + thisJob.ID + ": " + jobThread.ManagedThreadId);
                jobThread.Start();
            }
        }
    }
}
