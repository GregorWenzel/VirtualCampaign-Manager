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
        public static bool CreateJobDirectories(Job Job)
        {
            try
            {
                Directory.CreateDirectory(Job.JobDirectory);
                Directory.CreateDirectory(Path.Combine(Job.JobDirectory, "output"));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool CreateProductionDirectories(Production Production)
        {
            try
            { 
                Directory.CreateDirectory(Production.ProductionDirectory);
                Directory.CreateDirectory(Path.Combine(Production.ProductionDirectory, "motifs"));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void DeleteProductionDirectories(Production Production)
        {
            Directory.Delete(Production.ProductionDirectory);
        }
    }
}
