using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Workers
{
    public static class DirectoryWorker
    {
        //Event called after job worker has finished all job-related tasks
        public static EventHandler<EventArgs> SuccessEvent;

        //Event called after job worker has finished all job-related tasks
        public static EventHandler<ResultEventArgs> FailureEvent;

        public static void CreateJobDirectories(Job Job)
        {
            try
            {
                Directory.CreateDirectory(Job.JobDirectory);
                Directory.CreateDirectory(Path.Combine(Job.JobDirectory, "output"));
                FireSuccessEvent();
            }
            catch
            {
                FireFailureEvent();
            }
        }

        public static void CreateProductionDirectories(Production Production)
        {
            try
            { 
                Directory.CreateDirectory(Production.ProductionDirectory);
                Directory.CreateDirectory(Path.Combine(Production.ProductionDirectory, "motifs"));
                FireSuccessEvent();
            }
            catch
            {
                FireFailureEvent();
            }
        }

        public static void DeleteProductionDirectories(Production Production)
        {
            Directory.Delete(Production.ProductionDirectory);
        }

        private static void FireSuccessEvent()
        {
            EventHandler<EventArgs> successEvent = SuccessEvent;
            if (successEvent != null)
            {
                successEvent(null, new EventArgs());
            }
        }

        private static void FireFailureEvent()
        {
            EventHandler<ResultEventArgs> failureEvent = FailureEvent;
            if (failureEvent != null)
            {
                failureEvent(null, new ResultEventArgs(null));
            }
        }
    }
}
