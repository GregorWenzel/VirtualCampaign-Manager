using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Helpers;

namespace VirtualCampaign_Manager.Rendering
{
    public static class DeadlineFileWriter
    {
        public static bool WriteJobFile(Job job)
        {
            string renderFilename = JobPathHelper.GetDeadlineJobFile(job);
            StreamWriter jobFile = new StreamWriter(renderFilename);

            jobFile.WriteLine("Plugin=FusionCmd");
            jobFile.WriteLine("Frames={0}-{1}", job.InFrame, job.OutFrame);
            job.RenderStartTime = DateTime.Now.Ticks;
            jobFile.WriteLine(string.Format("Name={0} [{1}]", job.ID, job.RenderStartTime));
            jobFile.WriteLine("UserName=virtualcampaign");

            if (job.Production.Is4K)
            {
                jobFile.WriteLine("Group=vc_360");
            }
            else
            {
                jobFile.WriteLine("Group=vc");
            }

            jobFile.WriteLine("ArchiveOnComplete=false");
            jobFile.WriteLine("OnJobComplete=Nothing");
            jobFile.WriteLine("Priority=" + Convert.ToString(job.Production.Priority));
            jobFile.WriteLine("Comment=#" + job.FrameCount.ToString());
            if (job.FrameCount <= 1)
                jobFile.WriteLine("ChunkSize=1");
            else
                jobFile.WriteLine("ChunkSize=" + Settings.RenderChunkSize);
            jobFile.WriteLine("ExtraInfo1=" + job.ProductID);
            jobFile.WriteLine("OverrideJobFailureDetection=true");
            jobFile.WriteLine("FailureDetectionJobErrors=10");

            jobFile.Close();

            return File.Exists(renderFilename);
        }
    }
}
