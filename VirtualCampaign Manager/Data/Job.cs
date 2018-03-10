using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        JS_ENCODINGDONE,
        JS_DONE,
        JS_RENDER_DONE,
        JS_HAS_ERRORS
    };

    public class Job : VCObject
    {
        private static readonly Object obj = new Object();

        //PUBLIC fields
        //ID of the account that issued this job
        public int AccountID { get; set; }

        //can the clip's size be adjusted to fit the other clips in the film timeline?
        //older product clips do not fulfill this requirement
        public bool CanReformat { get; set; }

        //determins whether updates can be propagated to the server at this time
        public bool CanUpdateRemoteData { get; set; }

        //This job's error status
        public JobErrorStatus ErrorStatus { get; set; }

        //number of frames for this job's product
        private int frameCount;

        public int FrameCount
        {
            get { return OutFrame - InFrame + 1; }
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
        public List<Motif> MotifList;

        //number of motifs downloaded/available on local file system
        private int motifsAvailableCount;
        public int MotifsAvailableCount
        {
            get
            {
                return MotifList.Count(item => item.IsAvailable == true);
            }
        }

        //index of the last frame to be rendered
        //this is only relevant for subclips
        //for full clips, the last frame is the frame count (Frames)
        public int OutFrame { get; set; }

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
        public int ProductionID { get { return Production.ID; } }

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

        //this job's render progress
        public float Progress { get; set; }

        //ID of render job provided by deadline
        public string RenderJobID { get; set; }

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

                status = value;
                if (IsActive == true)
                {
                    JobRepository.UpdateJob(this, UpdateType.Status);
                }
            }
        }

        //--------------
        //PRIVATE fields
        //path to the directory where this job is being processed
        public string JobDirectory
        {
            get
            {
                return Path.Combine(Production.ProductionDirectory, this.ID.ToString());
            }
        }

        //path to the directory where this job's production is being processed
        public string ProductionDirectory
        {
            get
            {
                return Production.ProductionDirectory;
            }
        }

        private Thread workerThread;

        public void CheckWorker()
        {
            if (worker == null)
            {
                worker = new JobWorker(this);
            }
        }

        public void StartWorker()
        {
            if (workerThread == null)
            {
                workerThread = new Thread(new ThreadStart(worker.Iterate));
                Console.WriteLine("NEW THREAD FOR JOB ID " + this.ID + ": " + workerThread.ManagedThreadId);
                workerThread.Start();
            }
            else
            {
            }
        }

        public Job()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChangedEvent(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
                PropertyChanged(this, e);
            }
        }
    }
}