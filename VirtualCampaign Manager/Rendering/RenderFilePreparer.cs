using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Rendering
{
    public class RenderFilePreparer : EventFireBase
    {
        Job job;

        public RenderFilePreparer(Job job)
        {
            this.job = job;
        }

        public bool Prepare()
        {
            if (PrepareComposition() == false) return false;
            if (PrepareDeadlineFiles() == false) return false;

            return true;
        }

        private bool PrepareComposition()
        {
            bool result;



            return result;
        }

        private bool PrepareDeadlineFiles()
        {
            bool result;



            return result;
        }
    }
}
