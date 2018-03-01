using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCampaign_Manager.Data
{
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

        

        public bool HasSpecialIntroMusic { get; set; }
        public int ClipFrames { get; set; }
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

        public int CorrectClipFrames
        {
            get
            {
                int result = 0;
                foreach (Job thisJob in this.JobList)
                {
                    //if (thisJob.IsDicative == false)
                    result += thisJob.FrameCount;
                }
                return result;
            }
        }

        public int AbdicativeFrames
        {
            get
            {
                if (this.JobList[this.JobList.Count - 1].IsDicative)
                    return this.JobList[this.JobList.Count - 1].Frames;
                else
                    return 0;
            }
        }

        private ProductionStatus _status;
        public ProductionStatus Status
        {
            get { return _status; }
            set
            {
                this.UpdateDate = DateTime.Now;

                if ((int) (_status) == (int) (value)) return;

                Console.WriteLine("Old Status = " + _status + ", new status = " + value);

                if (_status != ProductionStatus.PS_RENDER_JOBS)
                {
                    if (value == ProductionStatus.PS_RENDER_JOBS && _errorCode == ProductionErrorStatus.PES_NONE)
                        GlobalValues.Instance.RenderQueueCount += 1;
                }
                else
                {
                    if (value != ProductionStatus.PS_RENDER_JOBS)
                        GlobalValues.Instance.RenderQueueCount -= 1;
                }

                _status = value;
                if (JobList.Count > 0)
                {
                    JobList[0].ProductionStatus = _status;
                }

                if (!CanUpdateRemoteData)
                    return;

                UpdateRemoteValue(UpdateType.Status);
                DoIterate();

            }
        }

        private ProductionErrorStatus _errorCode;
        public ProductionErrorStatus ErrorCode
        {
            get { return _errorCode; }
            set
            {
                this.UpdateDate = DateTime.Now;

                if (_errorCode == value) return;

                //ERROR present
                if (_errorCode != ProductionErrorStatus.PES_NONE)
                {
                    //ERROR REMOVED
                    if (value == ProductionErrorStatus.PES_NONE && _status == ProductionStatus.PS_RENDER_JOBS)
                        GlobalValues.Instance.RenderQueueCount += 1;
                }
                //NO ERROR PRESENT
                else
                {
                    //ERROR OCCURED
                    if (value != ProductionErrorStatus.PES_NONE && _status == ProductionStatus.PS_RENDER_JOBS)
                        GlobalValues.Instance.RenderQueueCount -= 1;
                }

                _errorCode = value;

                JobList[0].ProductionErrorStatus = _errorCode;

                if (!CanUpdateRemoteData)
                    return;

                UpdateRemoteValue(UpdateType.ErrorCode);
                EmailManager.Instance.SendErrorMail(this);
            }
        }

        private Film _film;
        public Film Film
        {
            get
            {
                return _film;
            }
            set
            {
                _film = value;
            }
        }

        public int FilmID
        {
            get { return _film.ID; }
            set
            {
                _film = new Film(value, FilmCodes);
            }
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

        private int _indicative;

        public int Indicative
        {
            get { return _indicative; }
            set { _indicative = value; }
        }

        private int _abdicative;

        public int Abdicative
        {
            get { return _abdicative; }
            set { _abdicative = value; }
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
                return Convert.ToInt32(Math.Round(ClipFrames / 25f));
            }
        }

        public int TotalDurationInSeconds
        {
            get
            {
                return Convert.ToInt32(Math.Round(CorrectClipFrames / 25f));
                //return Convert.ToInt32(Math.Round(((IndicativeFrames+AbdicativeFrames+CorrectClipFrames) / 25f)));
            }
        }

        private bool isPreview;

        public bool IsPreview { get; set; }

        public CodecInfo LargestCodec
        {
            get
            {
                CodecInfo result = CodecInfoList[0];

                if (CodecInfoList.Count > 1)
                {
                    for (int i = 1; i < CodecInfoList.Count; i++)
                    {
                        if (CodecInfoList[i].Codec.Width * CodecInfoList[i].Codec.Height > result.Codec.Width * result.Codec.Height)
                        {
                            result = CodecInfoList[i];
                        }
                    }
                }

                return result;
            }
        }

        public List<CodecInfo> CodecInfoList { get; set; }

        public Production()
        {
        }

        public Production(Dictionary<string, string> productionDict)
        {
            UpdateDate = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();

            UInt64 updateInt = 0;

            try
            {
                if (productionDict["UpdateTime"] != "")
                    updateInt = Convert.ToUInt64(Math.Abs(Math.Max(UInt64.MaxValue - 1, Math.Min(0, Convert.ToDouble(productionDict["UpdateTime"])))));
                else
                    updateInt = Convert.ToUInt64(Math.Abs(Math.Max(UInt64.MaxValue - 1, Math.Min(0, Convert.ToDouble(productionDict["CreationTime"])))));
            }
            catch
            {

            }

            creationTime = Convert.ToString(productionDict["CreationTime"]);

            CodecInfoList = new List<CodecInfo>();

            string[] formatBuffer = productionDict["Formats"].Split(new char[] { ',' });

            foreach (string format in formatBuffer)
            {
                int formatId = Convert.ToInt32(format);
                CodecInfo codecInfo = new CodecInfo();
                codecInfo.Codec = SettingManager.Instance.CodecDict[formatId];
                CodecInfoList.Add(codecInfo);
            }

            UpdateDate = UpdateDate.AddSeconds(updateInt + 1);
            HasSpecialIntroMusic = Convert.ToString(productionDict["SpecialIntroMusic"]) == "1";
            ClipFrames = Convert.ToInt32(productionDict["ClipFrameCount"]);
            //IndicativeFrames = Convert.ToInt32(_ProductionRow["IndicativeFrameCount"]);
            //AbdicativeFrames = Convert.ToInt32(_ProductionRow["AbdicativeFrameCount"]);
            IsPreview = Convert.ToInt32(productionDict["IsPreview"]) == 1;
            ID = Convert.ToInt32(productionDict["ID"]);
            Priority = Convert.ToInt32(productionDict["Priority"]);
            Email = Convert.ToString(productionDict["Email"]);
            JobList = ReadJobs();
            FindFirstRealClip();
            DatabaseManager.Instance.CanUpdate = false;
            _status = (ProductionStatus) Enum.ToObject(typeof(ProductionStatus), Convert.ToInt32(productionDict["Status"]));
            _errorCode = (ProductionErrorStatus) Enum.ToObject(typeof(ProductionErrorStatus), Convert.ToInt32(productionDict["ErrorCode"]));
            //_filmCodes = Convert.ToString(productionDict["FilmCodes"]);
            FilmID = Convert.ToInt32(productionDict["FilmID"]);
            _film.CodecSizes = this.CodecInfoList;
            _film.UrlHash = Convert.ToString(productionDict["FilmUrlHash"]);
            _account_id = Convert.ToInt32(productionDict["AccountID"]);
            _indicative = Convert.ToInt32(productionDict["IndicativeID"]);
            _abdicative = Convert.ToInt32(productionDict["AbdicativeID"]);
            _audio_id = Convert.ToInt32(productionDict["AudioID"]);
            _username = Convert.ToString(productionDict["UserName"]);
            _name = Convert.ToString(productionDict["Name"]);
            CanUpdateRemoteData = true;
            CheckStatus();
        }

        public void SetPriority()
        {
            if (Priority < 0)
                Priority = GlobalValues.Instance.GetNextRenderQueueSlot();
        }

        private void FindFirstRealClip()
        {
            foreach (Job job in JobList)
                if (!job.IsDicative)
                {
                    job.IsFirstRealClip = true;
                    return;
                }
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

        public void Update(Production newProduction)
        {
            CanUpdateRemoteData = false;
            UpdateDate = newProduction.UpdateDate;

            for (int i = 0; i < newProduction.JobList.Count; i++)
                JobList[i].Update(newProduction.JobList[i]);

            Status = newProduction.Status;
            ErrorCode = newProduction.ErrorCode;
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
            if (ErrorCode == ProductionErrorStatus.PES_UPLOAD)
            {
                ErrorCode = ProductionErrorStatus.PES_NONE;
                Status = ProductionStatus.PS_UPLOAD_FILMS;
            }

            if (!IsActive || Status == ProductionStatus.PS_DONE || ErrorCode != ProductionErrorStatus.PES_NONE)
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

        private ObservableCollection<Job> ReadJobs()
        {
            ObservableCollection<Job> result = new ObservableCollection<Job>();

            Dictionary<string, string> param = new Dictionary<string, string>
            {   { "productionID", this.ID.ToString() },
                { "is_preview", Convert.ToInt32(this.IsPreview).ToString() }
            };

            string productionListString = JSONRemoteManager.Instance.ExecuteRequest("getJobsByProductionID", param);
            List<Dictionary<string, string>> jobDict = JSONDeserializer.Deserialize(productionListString);

            //XmlDocument productionList = RemoteManager.Instance.ExecuteRequest("getJobsByProductionID", param);
            //DataSet data = new DataSet();

            /*
            using (XmlReader reader = new XmlNodeReader(productionList))
            {
                data.ReadXml(reader);
                reader.Close();
            }
            */

            if (jobDict.Count > 0)
            {
                result = ParseJobDictList(jobDict);
            }

            /*
            if (data.Tables.Count > 0)
            {
                result = ParseJobs(data.Tables[0].Rows);
            }
            */
            result = new ObservableCollection<Job>(result.OrderBy(item => item.Position));
            return result;
        }

        private ObservableCollection<Job> ParseJobs(DataRowCollection JobRows)
        {
            ObservableCollection<Job> result = new ObservableCollection<Job>();

            foreach (DataRow JobRow in JobRows)
            {
                Job newJob = new Job(this, JobRow);

                Job oldJob = HasSameID(result, newJob.ID);

                if (oldJob != null)
                {
                    oldJob.MotifList.Add(newJob.MotifList[0]);
                }
                else
                    result.Add(newJob);
            }
            return result;
        }

        private ObservableCollection<Job> ParseJobDictList(List<Dictionary<string, string>> jobDictList)
        {
            ObservableCollection<Job> result = new ObservableCollection<Job>();

            foreach (Dictionary<string, string> jobDict in jobDictList)
            {
                Job newJob = new Job(this, jobDict);

                Job oldJob = HasSameID(result, newJob.ID);

                if (oldJob != null)
                {
                    oldJob.MotifList.Add(newJob.MotifList[0]);
                }
                else
                    result.Add(newJob);
            }
            return result;
        }


        private Job HasSameID(ObservableCollection<Job> list, int id)
        {
            foreach (Job listObject in list)
                if (listObject.ID == id)
                    return listObject;

            return null;
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
                    ErrorCode = ProductionErrorStatus.PES_UPLOAD;
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
            this.ErrorCode = ProductionErrorStatus.PES_NONE;
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
                    param["error_code"] = ((int) ErrorCode).ToString();
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