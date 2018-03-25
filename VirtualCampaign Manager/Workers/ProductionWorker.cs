using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Encoding;
using VirtualCampaign_Manager.Helpers;
using VirtualCampaign_Manager.Repositories;
using VirtualCampaign_Manager.Transfers;

namespace VirtualCampaign_Manager.Workers
{
    public class ProductionWorker
    {
        //Event called after job worker has finished all job-related tasks
        public EventHandler<EventArgs> SuccessEvent;

        //Event called after job worker has finished all job-related tasks
        public EventHandler<EventArgs> FailureEvent;

        public bool IsActive { get; set; }
        public bool IsFinished { get; set; }

        //current production
        private Production production;

        public ProductionWorker(Production Production)
        {
            production = Production;
            IsActive = true;
            IsFinished = false;
        }

        public void Work()
        {
            if (CheckStatusOk() == false) return;

            switch (production.Status)
            {
                case ProductionStatus.PS_READY:
                    CreateDirectories();
                    break;
                case ProductionStatus.PS_START_JOBS:
                    StartJobs();
                    break;
                case ProductionStatus.PS_RENDER_JOBS:
                    break;
                case ProductionStatus.PS_MUX_AUDIO:
                    if (production.IsZipProduction)
                    {
                        production.Status = ProductionStatus.PS_UPLOAD_FILMS;
                        Work();
                    }
                    else
                    {
                        EncodeAudio();
                    }
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
                case ProductionStatus.PS_DONE:
                    break;
            }
        }

        private void CreateDirectories()
        {
            bool success = IOHelper.CreateDirectory(ProductionPathHelper.GetLocalProductionDirectory(production));
            success = success && IOHelper.CreateDirectory(ProductionPathHelper.GetProductionMotifDirectory(production));

            if (success == false)
            {
                production.ErrorStatus = ProductionErrorStatus.PES_CREATE_DIRECTORIES;
                return;
            }

            production.Status = ProductionStatus.PS_START_JOBS;
            Work();            
        }

        private void EncodeAudio()
        {
            AudioEncoder audioEncoder = new AudioEncoder(production);
            audioEncoder.SuccessEvent += OnAudioEncoderSuccess;
            audioEncoder.FailureEvent += OnAudioEncoderFailure;
            audioEncoder.Encode();
        }

        private void OnAudioEncoderSuccess(object sender, EventArgs ea)
        {
            (sender as AudioEncoder).SuccessEvent -= OnAudioEncoderSuccess;
            (sender as AudioEncoder).FailureEvent -= OnAudioEncoderFailure;
            production.Status = ProductionStatus.PS_JOIN_CLIPS;
            Work();
        }

        private void OnAudioEncoderFailure(object sender, ResultEventArgs ea)
        {
            (sender as AudioEncoder).SuccessEvent -= OnAudioEncoderSuccess;
            (sender as AudioEncoder).FailureEvent -= OnAudioEncoderFailure;
            production.ErrorStatus = (ProductionErrorStatus)ea.Result;
            FireFailureEvent();
        }

        private void JoinClips()
        {
            ClipJoiner clipJoiner = new ClipJoiner(production);
            clipJoiner.SuccessEvent += OnClipJoinerSuccess;
            clipJoiner.FailureEvent += OnClipJoinerFailure;
            clipJoiner.Join();
        }

        private void OnClipJoinerSuccess(object sender, EventArgs ea)
        {
            (sender as ClipJoiner).SuccessEvent -= OnClipJoinerSuccess;
            (sender as ClipJoiner).FailureEvent -= OnClipJoinerFailure;
            production.Status = ProductionStatus.PS_ENCODE_FILMS;
            Work();
        }

        private void OnClipJoinerFailure(object sender, ResultEventArgs ea)
        {
            (sender as ClipJoiner).SuccessEvent -= OnClipJoinerSuccess;
            (sender as ClipJoiner).FailureEvent -= OnClipJoinerFailure;
            production.ErrorStatus = (ProductionErrorStatus)ea.Result;
            FireFailureEvent();
        }

        private void EncodeFilms()
        {
            FilmEncoder filmEncoder = new FilmEncoder(production);
            filmEncoder.FailureEvent += OnFilmEncoderFailure;
            filmEncoder.SuccessEvent += OnFilmEncoderSuccess;
            filmEncoder.Encode();
        }

        private void OnFilmEncoderFailure(object sender, ResultEventArgs ea)
        {
            (sender as FilmEncoder).SuccessEvent -= OnFilmEncoderSuccess;
            (sender as FilmEncoder).FailureEvent -= OnFilmEncoderFailure;
            production.ErrorStatus = (ProductionErrorStatus)ea.Result;
            FireFailureEvent();
        }

        private void OnFilmEncoderSuccess(object sender, EventArgs ea)
        {
            (sender as FilmEncoder).SuccessEvent -= OnFilmEncoderSuccess;
            (sender as FilmEncoder).FailureEvent -= OnFilmEncoderFailure;
            production.Status = ProductionStatus.PS_UPLOAD_FILMS;
            Work();
        }

        private void UploadFilms()
        {
            FilmUploader filmUploader = new FilmUploader(production);
            filmUploader.FailureEvent += OnFilmUploaderFailure;
            filmUploader.SuccessEvent += OnFilmUploaderSuccess;
            filmUploader.Upload();
        }

        private void OnFilmUploaderFailure(object sender, ResultEventArgs ea)
        {
            (sender as FilmEncoder).SuccessEvent -= OnFilmUploaderSuccess;
            (sender as FilmEncoder).FailureEvent -= OnFilmUploaderFailure;
            production.ErrorStatus = (ProductionErrorStatus)ea.Result;
            FireFailureEvent();
        }

        private void OnFilmUploaderSuccess(object sender, EventArgs ea)
        {
            (sender as FilmUploader).SuccessEvent -= OnFilmUploaderSuccess;
            (sender as FilmUploader).FailureEvent -= OnFilmUploaderFailure;
            production.Status = ProductionStatus.PS_UPDATE_HISTORY;
            Work();
        }

        private void UpdateHistoryTable()
        {           
            FilmRepository.UpdateHistoryTable(production);
            production.Status = ProductionStatus.PS_DONE;

            FireSuccessEvent();
        }

        public void Delete()
        {
            ProductionRepository.DeleteProduction(production);
            FireSuccessEvent();
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

        private void StartJobs()
        {
            foreach (Job thisJob in production.JobList)
            {                 
                if (thisJob.Status == JobStatus.JS_DONE)
                {
                    continue;
                }

                production.LogText("Starting job ID " + thisJob.ID + " (product ID " + thisJob.ProductID + ")");
                thisJob.SuccessEvent += OnJobSuccess;
                thisJob.StartWorker();
            }

            production.Status = ProductionStatus.PS_RENDER_JOBS;
        }

        private void OnJobSuccess(object sender, EventArgs ea)
        {
            (sender as Job).SuccessEvent -= OnJobSuccess;
            Work();
        }

        private void FireSuccessEvent()
        {
            SuccessEvent?.Invoke(this, new EventArgs());
        }

        private void FireFailureEvent()
        {
            FailureEvent?.Invoke(this, new EventArgs());
        }
    }
}
