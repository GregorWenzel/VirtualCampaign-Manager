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

        public static string GetLogFilePath(Job job)
        {
            return Path.Combine(GetLocalJobDirectory(job), "job_" + job.ID + "_log.txt");
        }

        public static string GetJobClipPath(Job Job)
        {
            return Path.Combine(GetLocalJobDirectory(Job), "clip_" + Job.Position + ".mp4");
        }

        public static string GetJobCompPath(Job job)
        {
            string formattedIndex = String.Format("{0:D4}", job.ID);
            return Path.Combine(GetLocalJobDirectory(job), formattedIndex + ".comp");
        }

        public static string GetLocalJobMotifPath(Job job, Motif motif)
        {
            return Path.Combine(new string[]
                {
                    ProductionPathHelper.GetLocalProductionDirectory(job.Production),
                    "motifs",
                    motif.DownloadName
                });
        }

        public static string GetDeadlineJobFile(Job job)
        {
            return Path.Combine(GetLocalJobDirectory(job), job.ID + "_job.txt");
        }

        public static string GetLocalJobRenderOutputDirectory(Job job)
        {
            return Path.Combine(GetLocalJobDirectory(job), "output");
        }
        
        public static string GetLocalJobRenderOutputPathForZip(Job job)
        {
            return Path.Combine(GetLocalJobDirectory(job), "output", "F" + job.OutputExtension);
        }

        public static string GetLocalJobRenderOutputFileMask(Job job)
        {
            return Path.Combine(GetLocalJobRenderOutputDirectory(job), "F%04d" + job.OutputExtension);
        }

        public static string GetRenderChunkPath(RenderChunkStatus renderChunk)
        {
            return Path.Combine(GetLocalJobDirectory(renderChunk.Job), "clip_" + renderChunk.Job.Position + "_chunk_" + string.Format("{0:00000}", renderChunk.StartFrame) + "_" + string.Format("{0:00000}", renderChunk.EndFrame) + ".mp4");
        }

        public static string GetChunkListPath(Job job)
        {
            return Path.Combine(GetJobClipPath(job), "chunklist.txt");
        }

        public static string GetJobMp4Path(Job job)
        {
            return Path.Combine(GetJobClipPath(job), "clip_" + job.Position + ".mp4");
        }

        public static string GetProductCompositionPath(Job job)
        {
            string formattedIndex = String.Format("{0:D4}", job.ProductID);
            return Path.Combine(Settings.LocalProductPath, formattedIndex, formattedIndex+".comp");
        }


    }
}
