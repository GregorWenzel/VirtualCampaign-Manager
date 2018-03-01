using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
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
                return motifsAvailableCount;
            }
            set
            {
                if (value == motifsAvailableCount) return;

                motifsAvailableCount = value;
                if (motifsAvailableCount >= MotifList.Count)
                {
                    this.Status = JobStatus.JS_CREATE_RENDERFILES;
                    this.worker.Continue();
                }
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
               
        //production this job belongs to
        public Production Production { get; set; }

        //ID of production this job belongs  to
        public int ProductionID { get { return Production.ID; } }

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
                JobRepository.UpdateJob(this, UpdateType.Status);
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

        //Initialize job with its' production and data from database
        public Job(Production Production, Dictionary<string, string> JobDict)
        {
             //make sure this job won't process changes while being updated
            this.IsActive = false;

            this.Production = Production;
            this.ID = Convert.ToInt32(JobDict["ID"]);
            this.ErrorStatus = (JobErrorStatus) Enum.ToObject(typeof(JobErrorStatus), Convert.ToInt32(JobDict["ErrorCode"]));
            this.Position = Convert.ToInt32(JobDict["Position"]);
            this.ProductID = Convert.ToInt32(JobDict["ProductID"]);
            this.Frames = Convert.ToInt32(JobDict["ProductFrames"]);
            this.PreviewFrame = Convert.ToInt32(JobDict["ProductFrames"]);
            this.AccountID = Convert.ToInt32(JobDict["AccountID"]);
            this.IsDicative = (Convert.ToInt32(JobDict["IsDicative"]) == 1);

            MotifList = new List<Motif>();

            //required fields that are not provided by the database for indicatives and abdicatives
            if (this.IsDicative == false)
            {
                MasterProductID = Convert.ToInt32(JobDict["MasterProductID"]);
                InFrame = Convert.ToInt32(JobDict["InFrame"]);
                OutFrame = Convert.ToInt32(JobDict["OutFrame"]);
                CanReformat = (Convert.ToInt32(JobDict["CanReformat"]) == 1);

                //product preview clips do not receive motifs
                if (this.IsPreview == false)
                {
                    MotifList.Add(new Motif(Convert.ToInt32(JobDict["ContentID"]), Convert.ToString(JobDict["ContentType"]), Convert.ToInt32(JobDict["ContentPosition"]), Convert.ToString(JobDict["ContentExtension"]), Convert.ToString(JobDict["ContentLoaderName"]), Convert.ToString(JobDict["ContentText"])));
                }
            }
            else
            {
                MasterProductID = -1;
                InFrame = 0;
                OutFrame = 0;
                Status = JobStatus.JS_DONE;
                return;
            }

            try
            //check if this job already has a render id.
            {
                RenderJobID = Convert.ToString(JobDict["RenderID"]);
            }
            catch
            {
                RenderJobID = "";
            }

            this.Status = AdjustJobStatus((JobStatus) Enum.ToObject(typeof(JobStatus), Convert.ToInt32(JobDict["Status"])));

            //initialize worker
            this.worker = new JobWorker(this);

            //reactivate processing loop
            this.IsActive = true;
        }

        //Only accept new job status as such if it isn't currently being rendered and a render id has been saved before,
        //otherwise: set job to JS_GET_JOB_ID in order to read job id from deadline
        //and continue from there
        private JobStatus AdjustJobStatus(JobStatus currentStatus)
        {
            if (currentStatus == JobStatus.JS_RENDER_JOB && RenderJobID.Length == 0)
                return JobStatus.JS_GET_JOB_ID;
            else
                return currentStatus;
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