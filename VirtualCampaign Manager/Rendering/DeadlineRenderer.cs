using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Helpers;
using VirtualCampaign_Manager.Parsers;

namespace VirtualCampaign_Manager.Rendering
{
    public static class DeadlineRenderer
    {
        public static bool Render(Job job)
        {
            PrepareRendering(job);
            return StartRendering(job);
        }

        private static void PrepareRendering(Job job)
        { 
            job.RenderChunkStatusList = new List<RenderChunkStatus>();
            job.FinishedChunkList = new List<RenderChunkStatus>();
        }

        private static bool StartRendering(Job job)
        {
            string parameters = JobPathHelper.GetDeadlineJobFile(job);
            parameters += " " + Settings.LocalFusionPluginPath;
            parameters += " " + JobPathHelper.GetJobCompPath(job);

            VCProcess process = new VCProcess(job);
            process.StartInfo.FileName = Settings.LocalDeadlineExePath;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.Arguments = parameters;
            process.EnableRaisingEvents = true;
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;

            process.Execute();
            string output = process.StandardOutput.ReadToEnd();
            job.RenderID = RenderIDParser.Parse(output);
            process.WaitForExit();
            process.Close();

            return (job.RenderID != null);
        }

        public static void DeleteJob(Job job)
        {
            VCProcess suspendProcess = new VCProcess(job);

            string arguments = "-deletejob " + job.RenderID;

            suspendProcess.StartInfo.FileName = Settings.LocalDeadlineExePath;
            suspendProcess.StartInfo.CreateNoWindow = true;
            suspendProcess.StartInfo.UseShellExecute = false;
            suspendProcess.StartInfo.RedirectStandardError = false;
            suspendProcess.StartInfo.RedirectStandardOutput = false;
            suspendProcess.StartInfo.Arguments = arguments;
            suspendProcess.EnableRaisingEvents = false;
            suspendProcess.Execute();
        }
    }
}
