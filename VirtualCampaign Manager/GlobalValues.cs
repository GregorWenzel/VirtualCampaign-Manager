using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Windows.Controls;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Logging;
using VirtualCampaign_Manager.Parsers;
using VirtualCampaign_Manager.Repositories;

namespace VirtualCampaign_Manager
{
    public static class GlobalValues
    {
        public static bool IsSimulation = true;

        public static LogWindow LogWindow = new LogWindow();

        private static ObservableCollection<Production> productionList = new ObservableCollection<Production>();

        public static ObservableCollection<Production> ProductionList
        {
            get { return productionList; }
            set {
                if (value == productionList) return;

                productionList = value;
            }
        }

        private static ObservableCollection<Job> jobList = new ObservableCollection<Job>();

        public static ObservableCollection<Job> JobList
        {
            get { return jobList; }
            set { jobList = value; }
        }

        public static Dictionary<ProductionStatus, string> ProductionStatusString = new Dictionary<ProductionStatus, string>()
        {
            {ProductionStatus.PS_ENCODE_FILMS, "Encoding Films"},
            {ProductionStatus.PS_ENCODE_PRODUCTION, "Encoding Production"},
            {ProductionStatus.PS_JOIN_CLIPS, "Joining Job Clips"},
            {ProductionStatus.PS_MUX_AUDIO, "Muxing Audio"},
            {ProductionStatus.PS_READY, "Ready"},
            {ProductionStatus.PS_RENDER_JOBS, "Rendering Jobs"},
            {ProductionStatus.PS_UPLOAD_FILMS, "Uploading Films"},
            {ProductionStatus.PS_UPDATE_HISTORY, "Writing Statistic Data"},
            {ProductionStatus.PS_DONE, "DONE"},
        };

        public static Dictionary<JobStatus, string> JobStatusString = new Dictionary<JobStatus, string>()
        {
            {JobStatus.JS_IDLE, "Idle..."},
            {JobStatus.JS_CREATE_DIRECTORIES, "Creating Diretories"},
            {JobStatus.JS_PREPARE_RESOURCES, "Preparing Resources"},
            {JobStatus.JS_CREATE_RENDERFILES, "Creating Renderfiles"},
            {JobStatus.JS_SEND_RENDER_JOB, "Sending to Deadline"},
            {JobStatus.JS_GET_JOB_ID, "Getting Render ID"},
            {JobStatus.JS_RENDER_JOB, "Rendering"},
            {JobStatus.JS_SEND_ENCODE_JOB, "Encoding MPEG"},
            {JobStatus.JS_ENCODINGDONE, "Cleaning up"},
            {JobStatus.JS_DONE, "DONE"},
            {JobStatus.JS_RENDER_DONE, "Render done"},
            {JobStatus.JS_HAS_ERRORS, "Errors"},
        };

        public static Dictionary<ProductionErrorStatus, string> ProductionErrorStatusString = new Dictionary<ProductionErrorStatus, string>()
        {
            {ProductionErrorStatus.PES_NONE, "OK"},
            {ProductionErrorStatus.PES_CALCULATE_DURATION, "Calculate Duration"},
            {ProductionErrorStatus.PES_ENCODE_PRODUCTION, "Encode Production"},
            {ProductionErrorStatus.PES_GET_AUDIO, "Get Audio Info"},
            {ProductionErrorStatus.PES_JOIN_CLIPS, "Join Clips"},
            {ProductionErrorStatus.PES_MUX_AUDIO, "Mux Audio"},
            {ProductionErrorStatus.PES_READ_AUDIOFILE, "Copy Audio Resource"},
            {ProductionErrorStatus.PES_UPLOAD, "Upload Films"},
            {ProductionErrorStatus.PES_INDICATIVE_MISSING, "Missing indicative file"},
            {ProductionErrorStatus.PES_ABDICATIVE_MISSING, "Missing abdicative file"},
            {ProductionErrorStatus.PES_CREATE_MP4PREVIEWS, "Create MP4 previews" },
            {ProductionErrorStatus.PES_CREATE_DIRECTORIES, "Create directories" }
        };

        public static Dictionary<JobErrorStatus, string> JobErrorStatusString = new Dictionary<JobErrorStatus, string>()
        {
            {JobErrorStatus.JES_NONE, "OK"},
            {JobErrorStatus.JES_COMP_MOD_FOOTAGE_LOADERS, "Modify Footage Loaders"},
            {JobErrorStatus.JES_COMP_MOD_MOTIF_LOADERS, "Modify Motif Loaders"},
            {JobErrorStatus.JES_COMP_MOD_MOTIF_TEXTS, "Modify Text Tools"},
            {JobErrorStatus.JES_COMP_MOD_OUTPUTS, "Modify Output Tools"},
            {JobErrorStatus.JES_COMP_MOD_RESOURCE_LOADERS, "Modify Resource Loaders"},
            {JobErrorStatus.JES_COPY_COMP, "Copy Fusion flow"},
            {JobErrorStatus.JES_CREATE_DIRECTORIES, "Create output directories"},
            {JobErrorStatus.JES_DEADLINE_CREATE_FILES, "Create deadline files"},
            {JobErrorStatus.JES_DEADLINE_REGISTER_RENDERJOB, "Enqueue render job"},
            {JobErrorStatus.JES_DEADLINE_RENDER_JOB, "Render job"},
            {JobErrorStatus.JES_DOWNLOAD_MOTIFS, "Download motifs"},
            {JobErrorStatus.JES_ENCODE_IMAGES, "Encode MPEG"},
            {JobErrorStatus.JES_OUTPUTFILE_COUNT_MISMATCH, "Output count mismatch"},
            {JobErrorStatus.JES_PREVIEWFRAME_MISSING, "Preview frame missing" },
            {JobErrorStatus.JES_COMP_MISSING, "Comp file missing" },
            {JobErrorStatus.JES_MODIFY_MOTIF, "Modify motif" }
        };

        public static Dictionary<int, FilmOutputFormat> CodecDict = new Dictionary<int, FilmOutputFormat>();

        private static int renderQueueCount = 0;
        public static int RenderQueueCount { get => renderQueueCount; set => renderQueueCount = value; }

        public static void ReadOutputFormats()
        {
            string result = RemoteDataManager.ExecuteRequest("getCodecTypes");
            List<Dictionary<string, string>> codecList = JsonDeserializer.Deserialize(result);

            foreach (Dictionary<string, string> codec in codecList)
            {
                FilmOutputFormat codecFormat = new FilmOutputFormat(codec);
                CodecDict[codecFormat.ID] = codecFormat;
            }
        }

        public static int GetNextRenderQueueSlot()
        {
            RenderQueueCount = RenderQueueCount - 1;

            if (RenderQueueCount < 1)
                RenderQueueCount = 99;

            return RenderQueueCount;
        }


    }
}