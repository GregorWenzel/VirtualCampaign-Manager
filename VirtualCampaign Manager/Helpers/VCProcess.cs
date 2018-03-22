using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Helpers
{
    public class VCProcess : Process
    {
        private Logger logger;

        public VCProcess(Job job)
        {
            logger = new Logger(job);
        }

        public VCProcess(Production production)
        {
            logger = new Logger(production);
        }

        public bool Execute()
        {
            Console.WriteLine(this.StartInfo.FileName + " " + this.StartInfo.Arguments);
            logger.WriteLine(this.StartInfo.FileName + " " + this.StartInfo.Arguments);
            return true;
            //return base.Start();
        }
    }
}