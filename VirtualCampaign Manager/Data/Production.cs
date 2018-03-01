using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        PES_ABDICATIVE_MISSING = 9
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
        private int uploadCounter = 0;

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

                UpdateRemoteValue(UpdateType.Status);
                DoIterate();
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

                UpdateRemoteValue(UpdateType.ErrorCode);
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

                if (JobList != null)
                {
                    foreach (Job job in JobList)
                        JobList[0].ShowPriority(value);
                }

                //UpdateRemoteValue(UpdateType.Priority);
            }
        }


        public string SourceProductionDirectory
        {
            get
            {
                return SettingManager.Instance.TempSourceProductionDirectory(this.ID);
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

        public void Update(Production newProduction)
        {
            CanUpdateRemoteData = false;
            UpdateDate = newProduction.UpdateDate;

            for (int i = 0; i < newProduction.JobList.Count; i++)
                JobList[i].Update(newProduction.JobList[i]);

            Status = newProduction.Status;
            ErrorStatus = newProduction.ErrorStatus;
            if (newProduction.Priority >= 0)
                Priority = newProduction.Priority;
            /*
            _filmCodes = newProduction.FilmCodes;
            FilmID = newProduction.FilmID;
            _account_id = newProduction.AccountID;
            _indicative = newProduction.Indicative;
            _abdicative = newProduction.Abdicative;
            _audio_id = newProduction.AudioID;
            _username = newProduction.Username;
            _name = newProduction.Name;
            */
            CanUpdateRemoteData = true;
            CheckStatus();
        }

        public void Execute()
        {
            IsActive = true;
            Iterate();
        }

        public void Activate()
        {
            IsActive = true;
            foreach (Job job in JobList)
                job.IsActive = true;
        }

        public void Suspend()
        {
            IsActive = false;
            foreach (Job job in JobList)
                job.IsActive = false;
        }

        private void Iterate()
        {
            if (iterateThread == null)
            {
                iterateThread = new Thread(new ThreadStart(DoIterate));
                Console.WriteLine("NEW THREAD FOR PRODUCTION +" + this.ID + ": " + iterateThread.ManagedThreadId);
            }

            try
            {
                iterateThread.Start();
            }
            catch { }
        }

        private void DoIterate()
        {
            if (ErrorStatus == ProductionErrorStatus.PES_UPLOAD)
            {
                ErrorStatus = ProductionErrorStatus.PES_NONE;
                Status = ProductionStatus.PS_UPLOAD_FILMS;
            }

            if (!IsActive || Status == ProductionStatus.PS_DONE || ErrorStatus != ProductionErrorStatus.PES_NONE)
                return;

            switch (Status)
            {
                case ProductionStatus.PS_READY:
                    Status = ProductionStatus.PS_RENDER_JOBS;
                    break;
                case ProductionStatus.PS_RENDER_JOBS:
                    ExecuteJobs();
                    break;
                case ProductionStatus.PS_MUX_AUDIO:
                    if (this.JobList.Count == 0)
                    {
                        break;
                    }

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

        private void ExecuteJobs()
        {
            CheckStatus();

            if (Status != ProductionStatus.PS_JOIN_CLIPS)
            {
                foreach (Job thisJob in JobList)
                {
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

        private void JoinClips()
        {
            EncodingManager encodingManager = new EncodingManager();
            encodingManager.JoinClips(this);
        }

        private void EncodeAudio()
        {
            EncodingManager encodingManager = new EncodingManager();
            encodingManager.EncodeAudio(this);
        }


        private void EncodeIntermediateAudioMPEG()
        {
            EncodingManager encodingManager = new EncodingManager();
            encodingManager.EncodeIntermediateAudioMPEG(this);
        }

        private void EncodeFilms()
        {
            EncodingManager encodingManager = new EncodingManager();
            encodingManager.EncodeFilms(this);
        }

        private void UploadFilms()
        {
            uploadCounter += 1;
            sizeString = Film.UploadFiles(this);

            if (sizeString != null)
            {
                UpdateRemoteValue(UpdateType.Film);

                if (this.Email.Length > 0)
                    EmailManager.Instance.SendMail(this);

                CleanUp();
                Status = ProductionStatus.PS_UPDATE_HISTORY;
            }
            else
            {
                if (uploadCounter >= 3)
                    ErrorStatus = ProductionErrorStatus.PES_UPLOAD;
                else
                    UploadFilms();
            }
        }

        private void CleanUp()
        {
            string path = SourceProductionDirectory;
            if (System.IO.Directory.Exists(path))
            {
                try
                {
                    System.IO.Directory.Delete(path, true);
                }
                catch
                {
                }
            }
        }

        public void Reset()
        {
            CleanUp();
            foreach (Job thisJob in JobList)
            {
                thisJob.Delete();
            }

            uploadCounter = 0;
            this.ErrorStatus = ProductionErrorStatus.PES_NONE;
            this.Status = ProductionStatus.PS_READY;

            this.Execute();
        }

        private void UpdateHistoryTable()
        {
            DateTime temp = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
            TimeSpan span = (this.UpdateDate.ToLocalTime() - temp);

            Dictionary<string, string> param = new Dictionary<string, string>
            {   { "productionID", this.ID.ToString() },
                   { "updateTime", Convert.ToInt64(span.TotalSeconds).ToString() },
            };

            int[] jobIdList = new int[JobList.Count];
            string[] totalMotifIDList = new string[JobList.Count];
            int[] isDicativeList = new int[JobList.Count];

            for (int i = 0; i < JobList.Count; i++)
            {
                jobIdList[i] = JobList[i].ProductID;

                if (JobList[i].IsDicative)
                    isDicativeList[i] = 1;
                else
                    isDicativeList[i] = 0;

                int[] motifIDList = new int[JobList[i].MotifList.Count];

                for (int j = 0; j < JobList[i].MotifList.Count; j++)
                {
                    motifIDList[j] = JobList[i].MotifList[j].ID;
                }

                totalMotifIDList[i] = String.Join(".", motifIDList);
            }

            param["DicativeList"] = String.Join(",", isDicativeList);
            param["JobIDList"] = String.Join(",", jobIdList);
            param["MotifIDList"] = String.Join(",", totalMotifIDList);
            param["FilmID"] = Convert.ToString(this.Film.ID);
            param["AccountID"] = Convert.ToString(this.AccountID);
            param["FilmName"] = this.Name;

            JSONRemoteManager.Instance.UpdateHistory(param);
            this.Status = ProductionStatus.PS_DONE;

            RemoveMyself();
        }

        private void RemoveMyself()
        {
            this.OnFinishedEvent(new EventArgs());
        }

        public void Delete()
        {
            Dictionary<string, string> param = new Dictionary<string, string>
            {   { "productionID", this.ID.ToString() }
            };

            JSONRemoteManager.Instance.DeleteProduction(param);
            this.OnFinishedEvent(new EventArgs());
        }

        private void UpdateRemoteValue(UpdateType Type)
        {
            if (!CanUpdateRemoteData) return;

            DateTime temp = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
            TimeSpan span = (this.UpdateDate.ToLocalTime() - temp);

            Dictionary<string, string> param = new Dictionary<string, string>
            {   { "productionID", this.ID.ToString() },
                { "updateTime", Convert.ToInt64(span.TotalSeconds).ToString() }
            };

            switch (Type)
            {
                case UpdateType.Status:
                    param["status"] = ((int) Status).ToString();
                    JSONRemoteManager.Instance.UpdateProduction(param);
                    break;
                case UpdateType.ErrorCode:
                    param["error_code"] = ((int) ErrorStatus).ToString();
                    JSONRemoteManager.Instance.UpdateProduction(param);
                    break;
                case UpdateType.Film:
                    param["duration"] = (IndicativeFrames + AbdicativeFrames + ClipFrames).ToString();
                    param["size"] = sizeString;
                    JSONRemoteManager.Instance.UpdateFilm(param);
                    break;
                case UpdateType.Priority:
                    param["priority"] = Priority.ToString();
                    JSONRemoteManager.Instance.UpdateProduction(param);
                    break;
            }
        }
    }
}