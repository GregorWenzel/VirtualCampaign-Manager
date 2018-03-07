using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Helpers
{
    public static class JobPathHelper
    {
        public static string GetLocalJobDirectory(Job Job)
        {
            return Path.Combine(ProductionPathHelper.GetLocalProductionDirectory(Job.Production), Job.ID.ToString());
        }

        public static string GetJobClipPath(Job Job)
        {
            return Path.Combine(GetLocalJobDirectory(Job), "clip_" + Job.Position + ".mp4");
        }

        public static string GetLocalJobMotifPath(Job Job, Motif Motif)
        {
            return Path.Combine(new string[]
                {
                    GetLocalJobDirectory(Job),
                    "motifs",
                    Motif.DownloadName
                });
        }

        public static string GetLocalJobRenderOutputDirectory(Job Job)
        {
            return Path.Combine(GetLocalJobDirectory(Job), "output");
        }
    }
}
