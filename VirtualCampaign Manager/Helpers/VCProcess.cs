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
        VCObject sender;

        public VCProcess(Job job)
        {
            sender = job;
        }

        public VCProcess(Production production)
        {
            sender = production;
        }

        public bool Execute()
        {
            string logString = this.StartInfo.FileName + " " + this.StartInfo.Arguments;
            if (sender is Job)
            {
                (sender as Job).LogText(logString);
            }
            else if (sender is Production)
            {
                (sender as Production).LogText(logString);
            }
            return base.Start();
        }
    }
}