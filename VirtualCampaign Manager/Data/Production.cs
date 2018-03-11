using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Helpers;
using VirtualCampaign_Manager.Managers;
using VirtualCampaign_Manager.Repositories;
using VirtualCampaign_Manager.Workers;

namespace VirtualCampaign_Manager.Data
{
    public enum ProductionErrorStatus
    {
        PES_NONE = 0,
        PES_JOIN_CLIPS = 1,
        PES_ENCODE_PRODUCTION = 2,
        PES_GET_AUDIO = 3,
        PES_READ_AUDIOFILE = 4,
        PES_CALCULATE_DURATION = 5,
        PES_MUX_AUDIO = 6,
        PES_UPLOAD = 7,
        PES_INDICATIVE_MISSING = 8,
        PES_ABDICATIVE_MISSING = 9,
        PES_CREATE_MP4PREVIEWS = 10
    };

    public enum ProductionStatus
    {
        PS_READY = 0,
        PS_RENDER_JOBS = 1,
        PS_MUX_AUDIO = 2,
        PS_JOIN_CLIPS = 3,
        PS_ENCODE_PRODUCTION = 4,
        PS_ENCODE_FILMS = 5,
        PS_UPLOAD_FILMS = 6,
        PS_UPDATE_HISTORY = 7,
        PS_DONE = 8
    };

    public class Production : VCObject
    {
        public EventHandler<EventArgs> SuccessEvent;
        public EventHandler<ResultEventArgs> FailureEvent;

        //list of jobs associated to this production
        public List<Job> JobList;

        //does this production use music for indicative other than the default?
        public bool HasSpecialIntroMusic { get; set; }

        //getters and setters
        //return path to directory where production is being processed
        public string ProductionDirectory
        {
            get
            {
                return Path.Combine(new string[] { Settings.LocalProductionPath, "productions", this.ID.ToString() });
            }
        }

        //returns the number of frames for the indicative
        public int IndicativeFrames
        {
            get
            {
                if (this.JobList[0].IsDicative)
                    return this.JobList[0].FrameCount;
                else
                    return 0;
            }
        }

        private bool IsActive = false;

        private bool CanUpdateRemoteData = false;
        public string creationTime = "";
        public int UploadCounter = 0;

        public string timestamp = "";

        private String sizeString;

        public String VisibleDateTime
        {
            get
            {
                if (creationTime == "")
                    return " - Indicative - ";
                DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
                date = date.AddSeconds(Convert.ToUInt32(creationTime));
                String result = date.ToShortDateString() + ", " + date.ToShortTimeString();
                return result;
            }
        }

        private String _email;
        public String Email
        {
            get { return _email; }
            set { _email = value; }
        }

        //get net number of frames for all clips other than indicatives or abdicatives
        public int ClipsFrameCount
        {
            get
            {
                int result = 0;
                foreach (Job thisJob in this.JobList)
                {
                    result += thisJob.FrameCount;
                }
                return result;
            }
        }

        //true if there is an outputformat with id=20, indicating a KRPANO production
        public bool ContainsPano
        {
            get
            {
                return (Film.FilmOutputFormatList.Any(item => item.ID == 20));
            }
        }

        //get net number of frames for all clips
        public int TotalFrameCount
        {
            get
            {
                int result = 0;
                foreach (Job thisJob in this.JobList)
                {
                    result += thisJob.FrameCount;
                }
                return result;
            }
        }

        //get number of frames for the abdicatives
        public int AbdicativeFrames
        {
            get
            {
                if (JobList[JobList.Count - 1].IsDicative)
                    return JobList[JobList.Count - 1].FrameCount;
                else
                    return 0;
            }
        }

        private ProductionStatus status;
        public ProductionStatus Status
        {
            get { return status; }
            set
            {
                this.UpdateDate = DateTime.Now;

                if ((int) (status) == (int) (value)) return;

                Console.WriteLine("Old Status = " + status + ", new status = " + value);

                if (status != ProductionStatus.PS_RENDER_JOBS)
                {
                    if (value == ProductionStatus.PS_RENDER_JOBS && errorStatus == ProductionErrorStatus.PES_NONE)
                        GlobalValues.RenderQueueCount += 1;
                }
                else
                {
                    if (value != ProductionStatus.PS_RENDER_JOBS)
                        GlobalValues.RenderQueueCount -= 1;
                }

                status = value;

                if (!CanUpdateRemoteData)
                    return;

                ProductionRepository.UpdateRemoteValue(this, UpdateType.Status);
            }
        }

        private ProductionErrorStatus errorStatus;
        public ProductionErrorStatus ErrorStatus
        {
            get { return errorStatus; }
            set
            {
                this.UpdateDate = DateTime.Now;

                if (errorStatus == value) return;

                //ERROR present
                if (errorStatus != ProductionErrorStatus.PES_NONE)
                {
                    //ERROR REMOVED
                    if (value == ProductionErrorStatus.PES_NONE && status == ProductionStatus.PS_RENDER_JOBS)
                        GlobalValues.RenderQueueCount += 1;
                }
                //NO ERROR PRESENT
                else
                {
                    //ERROR OCCURED
                    if (value != ProductionErrorStatus.PES_NONE && status == ProductionStatus.PS_RENDER_JOBS)
                        GlobalValues.RenderQueueCount -= 1;
                }

                errorStatus = value;

                JobList[0].ProductionErrorStatus = errorStatus;

                if (!CanUpdateRemoteData)
                    return;

                ProductionRepository.UpdateRemoteValue(this, UpdateType.ErrorCode);
                EmailManager.SendErrorMail(this);
            }
        }

        private Film film;
        public Film Film
        {
            get
            {
                return film;
            }
            set
            {
                film = value;
            }
        }

        public int FilmID
        {
            get { return film.ID; }
        }

        private int _account_id;

        public int AccountID
        {
            get { return _account_id; }
            set { _account_id = value; }
        }

        private int _audio_id;

        public int AudioID
        {
            get { return _audio_id; }
            set { _audio_id = value; }
        }

        private int indicativeID;

        public int IndicativeID
        {
            get { return indicativeID; }
            set { indicativeID = value; }
        }

        private int abdicativeID;

        public int AbdicativeID
        {
            get { return abdicativeID; }
            set { abdicativeID = value; }
        }

        private string _filmCodes;

        public string FilmCodes
        {
            get { return _filmCodes; }
            set { _filmCodes = value; }
        }

        private string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private string _username;

        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }

        private int _priority = -1;
        public int Priority
        {
            get
            {
                return _priority;
            }
            set
            {
                bool propagate = (_priority != -1);

                if (value == _priority) return;

                _priority = value;
            }
        }

        public int ClipDurationInSeconds
        {
            get
            {
                return Convert.ToInt32(Math.Round(ClipsFrameCount / 25f));
            }
        }

        public int TotalDurationInSeconds
        {
            get
            {
                return Convert.ToInt32(Math.Round(TotalFrameCount / 25f));
            }
        }

        private bool isPreview;

        public bool IsPreview { get; set; }

        private ProductionWorker worker;

        //empty constructor
        public Production()
        {
            Film = new Film();
        }

        public void SetPriority()
        {
            if (Priority < 0)
                Priority = GlobalValues.GetNextRenderQueueSlot();
        }

        public void CheckStatus()
        {
            if (JobList == null) return;

            int jobsDoneCount = 0;
            foreach (Job job in JobList)
                if (job.Status == JobStatus.JS_DONE)
                    jobsDoneCount += 1;

            if (jobsDoneCount == JobList.Count && Status < ProductionStatus.PS_MUX_AUDIO)
                Status = ProductionStatus.PS_MUX_AUDIO;
        }

        public void SetStatus(ProductionStatus Satus)
        {
            status = Status;
        }

        public void SetErrorStatus(ProductionErrorStatus ErrorStatus)
        {
            errorStatus = ErrorStatus;
        }

        private Thread workerThread;

        public void StartWorker()
        {
            IsActive = true;
            worker = new ProductionWorker(this);
            workerThread = new Thread(worker.Work);
            workerThread.Start();
        }
        
        public void Reset()
        {
            foreach (Job thisJob in JobList)
            {
                thisJob.Reset();
            }

            IOHelper.DeleteDirectory(ProductionPathHelper.GetLocalProductionDirectory(this));
            
            ErrorStatus = ProductionErrorStatus.PES_NONE;
            Status = ProductionStatus.PS_READY;
        }

        private void UpdateHistoryTable()
        {
            FilmRepository.UpdateHistoryTable(this);
            Status = ProductionStatus.PS_DONE;
            FireSuccessEvent();
        }

        public void Delete()
        {
            ProductionRepository.DeleteProduction(this);
            FireSuccessEvent();
        }

        protected void FireSuccessEvent()
        {
            SuccessEvent?.Invoke(null, new EventArgs());
        }

        protected void FireFailureEvent(object ResultObject = null)
        {
            FailureEvent?.Invoke(null, new ResultEventArgs(ResultObject));
        }

    }
}