using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Telerik.Windows.Controls;
using VirtualCampaign_Manager.Logging;
using VirtualCampaign_Manager.Managers;
using VirtualCampaign_Manager.Repositories;
using VirtualCampaign_Manager.Workers;

namespace VirtualCampaign_Manager.Data
{
    public enum JobErrorStatus
    {
        JES_NONE = 0,
        JES_CREATE_DIRECTORIES = 1,
        JES_DOWNLOAD_MOTIFS = 2,
        JES_COPY_COMP = 3,
        JES_COMP_MOD_FOOTAGE_LOADERS = 4,
        JES_COMP_MOD_RESOURCE_LOADERS = 5,
        JES_COMP_MOD_OUTPUTS = 6,
        JES_COMP_MOD_MOTIF_LOADERS = 7,
        JES_COMP_MOD_MOTIF_TEXTS = 8,
        JES_DEADLINE_CREATE_FILES = 9,
        JES_DEADLINE_REGISTER_RENDERJOB = 10,
        JES_DEADLINE_RENDER_JOB = 11,
        JES_OUTPUTFILE_COUNT_MISMATCH = 12,
        JES_DEADLINE_REGISTER_ENCODINGJOB = 13,
        JES_ENCODE_IMAGES = 14,
        JES_PREVIEWFRAME_MISSING = 15,
        JES_EXTRACT_MOTIF = 16,
        JES_COMP_MISSING = 17,
        JES_MODIFY_MOTIF = 18,
        JES_CREATE_ZIP = 19
    };

    public enum JobStatus
    {
        JS_IDLE,
        JS_CREATE_DIRECTORIES,
        JS_PREPARE_RESOURCES,
        JS_CREATE_RENDERFILES,
        JS_SEND_RENDER_JOB,
        JS_GET_JOB_ID,
        JS_RENDER_JOB,
        JS_SEND_ENCODE_JOB,
        JS_ENCODE_JOB,
        JS_ENCODING_DONE,
        JS_DONE,
        JS_RENDER_DONE,
        JS_HAS_ERRORS,
    };

    public class Job : VCObject, INotifyPropertyChanged
    {
        public ICommand DeleteProductionCommand { get; set; }
        public ICommand ResetJobCommand { get; set; }
        public ICommand ResetProductionCommand { get; set; }

        public EventHandler<EventArgs> SuccessEvent;

        private static readonly Object obj = new Object();

        //PUBLIC fields
        //ID of the account that issued this job
        public int AccountID { get; set; }

        //can the clip's size be adjusted to fit the other clips in the film timeline?
        //older product clips do not fulfill this requirement
        public bool CanReformat { get; set; }

        //determins whether updates can be propagated to the server at this time
        public bool CanUpdateRemoteData
        {
            get
            {
                return GlobalValues.IsSimulation == false;
            }
        }

        //This job's error status
        private JobErrorStatus errorStatus;
        public JobErrorStatus ErrorStatus
        {
            get
            {
                return errorStatus;
            }
            set
            {
                UpdateDate = DateTime.Now;

                if (errorStatus == value) return;

                errorStatus = value;

                LogText("Error: " + GlobalValues.JobErrorStatusString[ErrorStatus]);

                RaisePropertyChangedEvent("StatusString");
                RaisePropertyChangedEvent("StatusColor");

                if (!CanUpdateRemoteData)
                    return;

                JobRepository.UpdateJob(this, UpdateType.ErrorCode);
                EmailManager.SendErrorMail(this);
            }
        }
               
        //usually 0, can be >0 for a subclip
        public int InFrame { get; set; }

        //Is this job currently being processed or idle?
        public bool IsActive { get; set; }

        //is the product associated with this clip an indicative or abdicative?
        public bool IsDicative { get; set; }

        //is the job in the list the first clip to be rendered (as opposed to an indicative?)
        public bool IsFirstRealClip { get; set; }

        //is this job rendering a product clip preview (i.e. without any motifs)
        public bool IsPreview { get; set; }

        //if this is a subclip, MasterProductID is >=0, otherwise -1
        public int MasterProductID { get; set; }

        //List of motifs associated with this job
        public List<Motif> MotifList = new List<Motif>();

        //index of the last frame to be rendered
        public int OutFrame { get; set; }

        //net number of frames
        public int FrameCount
        {
            get
            {
                return OutFrame - InFrame + 1;
            }
        }
        private string outputExtension;
        public string OutputExtension
        {
            get { return outputExtension; }
            set
            {
                if (value == outputExtension) return;

                outputExtension = value;
                JobRepository.UpdateJob(this, UpdateType.OutputExtension);
            }
        }
        
        //position index of this job in the clip sequence
        public int Position { get; set; }

        //index of the preview frame used to create thumbnail
        public int PreviewFrame { get; set; }

        //ID of the product for this job
        private int productID;
        public int ProductID
        {
            //if this job's product is a subclip (master id >=0), return its' master ID
            //otherwise, return this job's product id
            get
            {
                if (this.MasterProductID < 0)
                {
                    return productID;
                }
                else
                {
                    return MasterProductID;
                }
            }
            set
            {
                productID = value;
            }
        }
             
        //ID of the product id irrespective of subclip status
        public int OriginalProductID
        {
            get
            {
                return productID;
            }
        }

        //production this job belongs to
        public Production Production { get; set; }

        //ID of production this job belongs  to
        public int ProductionID
        {
            get
            {
                if (Production != null)
                {
                    return Production.ID;
                }
                else
                {
                    return -1;
                }
            }
        }

        public string IDString
        {
            get
            {
                return Production.Name + " (" + Production.Film.ID + ")";
            }
        }

        public string UserIDString
        {
            get
            {
                return Production.Username + " (" + Production.AccountID + ")";
            }
        }

        private ProductionStatus _productionStatus;
        public ProductionStatus ProductionStatus
        {
            get { return _productionStatus; }
            set
            {
                if (_productionStatus == value) return;

                _productionStatus = value;
                RaisePropertyChangedEvent("ProductionStatusString");
            }
        }

        private ProductionErrorStatus _productionErrorStatus;
        public ProductionErrorStatus ProductionErrorStatus
        {
            get { return _productionErrorStatus; }
            set
            {
                _productionErrorStatus = value;
                RaisePropertyChangedEvent("ProductionStatusString");
                RaisePropertyChangedEvent("ProductionStatusColor");
            }
        }

        public Brush ProductionStatusColor
        {
            get
            {
                if (Production.ErrorStatus == ProductionErrorStatus.PES_NONE)
                {
                    return new SolidColorBrush(Colors.Black);
                }
                else
                {
                    return new SolidColorBrush(Colors.Red);

                }
            }
        }

        public string ProductionStatusString
        {
            get
            {
                if (Position == 1)
                {
                    if (Production.ErrorStatus == ProductionErrorStatus.PES_NONE)
                        return GlobalValues.ProductionStatusString[Production.Status];
                    else
                        return "ERROR: " + GlobalValues.ProductionErrorStatusString[Production.ErrorStatus];
                }
                else
                    return "";
            }
        }

        private float productionProgress;

        public float ProductionProgress
        {
            get { return productionProgress; }
            set {
                if (value == productionProgress) return;

                productionProgress = value;

                RaisePropertyChangedEvent("ProductionProgress");
            }
        }

        //this job's render progress
        private float progress;

        public float Progress
        {
            get { return progress; }
            set
            {
                if (value == progress) return;
                progress = value;
                RaisePropertyChangedEvent("Progress");
            }
        }

        //ID of render job provided by deadline
        public string RenderID { get; set; }

        public List<RenderChunkStatus> RenderChunkStatusList { get; set; }
        public List<RenderChunkStatus> FinishedChunkList { get; set; }

        //process queue worker for rendering, encoding, etc.
        private JobWorker worker;

        //This job's status
        private JobStatus status;
        public JobStatus Status
        {
            get
            {
                return status;
            }
            set
            {
                if (value == status) return;

                if (IsDicative && !IsPreview)
                {
                    status = JobStatus.JS_DONE;
                }
                else
                {
                    status = value;

                    if (IsActive == true)
                    {
                        JobRepository.UpdateJob(this, UpdateType.Status);
                    }
                }

                RaisePropertyChangedEvent("StatusString");
                RaisePropertyChangedEvent("StatusColor");
            }
        }

        public string StatusString
        {
            get
            {
                if (ErrorStatus == JobErrorStatus.JES_NONE)
                    return GlobalValues.JobStatusString[Status];
                else
                    return "ERROR: " + GlobalValues.JobErrorStatusString[ErrorStatus];
            }
        }

        public Brush StatusColor
        {
            get
            {
                if (ErrorStatus == JobErrorStatus.JES_NONE)
                {
                    return new SolidColorBrush(Colors.Black);
                }
                else
                {
                    return new SolidColorBrush(Colors.Red);

                }
            }
        }

        //--------------
        //PRIVATE fields

        public void Reset()
        {
            if (worker != null)
            {
                worker.CleanUp(reset: true);

                if (workerThread != null)
                {
                    workerThread.Abort();
                }
            }
        }

        private Logger logger;

        public string Log
        {
            get
            {
                return logger.Log;
            }
        }

        private Thread workerThread;

        public void LogText(string text)
        {
            logger.LogText(text);
            RaisePropertyChangedEvent("Log");
        }

        public void StartWorker()
        {
            if (GlobalValues.IsSimulation)
            {
                this.ErrorStatus = JobErrorStatus.JES_NONE;
            }

            IsActive = true;
            worker = new JobWorker(this);
            worker.SuccessEvent += OnWorkerSuccess;
            workerThread = new Thread(new ThreadStart(worker.Work));
            LogText("NEW THREAD ID: " + workerThread.ManagedThreadId);
            workerThread.Start();
        }

        private void OnWorkerSuccess(object sender, EventArgs ea)
        {
            worker.SuccessEvent -= OnWorkerSuccess;

            FireSuccessEvent();
        }

        public Job()
        {
            logger = new Logger(this);

            ResetProductionCommand = new DelegateCommand(OnResetProduction);
            ResetJobCommand = new DelegateCommand(OnResetJob);
            DeleteProductionCommand = new DelegateCommand(OnDeleteProduction);
        }

        private void OnResetProduction(object obj)
        {
            if (Production != null)
            {
                Production.Reset();
            }
        }

        private void OnDeleteProduction(object obj)
        {

        }

        private void OnResetJob(object obj)
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChangedEvent(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
                PropertyChanged(this, e);
            }
        }

        private void FireSuccessEvent()
        {
            SuccessEvent?.Invoke(this, new EventArgs());
        }
    }
}