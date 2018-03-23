using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Helpers;

namespace VirtualCampaign_Manager.Encoding
{
    public class ClipJoiner : EventFireBase
    {
        private Production production;

        public ClipJoiner(Production Production)
        {
            production = Production;
        }

        public void Join()
        {
            CreateCliplistFile();
            if (JoinClips() == false) return;
            CheckJoining();
        }

        private void CreateCliplistFile()
        {
            //create cliplist file
            StreamWriter writer = new StreamWriter(ProductionPathHelper.GetClipListPath(production), false);
            writer.WriteLine(@"# cliplist");

            FilmOutputFormat largestOutputFormat = production.Film.LargestFilmOutputFormat;

            //clips
            for (int i = 0; i < production.JobList.Count; i++)
            {
                Job job = production.JobList[i];

                if (job.IsDicative)
                {
                    VerifyDicativeSize(job, largestOutputFormat);
                    writer.WriteLine(CreateDicativeLine(job, largestOutputFormat));
                }
                else
                    writer.WriteLine("file '" + JobPathHelper.GetJobClipPath(job) + "'");
            }

            writer.Close();
        }

        private void VerifyDicativeSize(Job thisJob, FilmOutputFormat largestOutputFormat)
        {          
            string indicativePath = ProductionPathHelper.GetProductMp4PathByOutputFormat(thisJob.ProductID, largestOutputFormat.ID);
            if (!File.Exists(indicativePath))
            {
                ResizeDicative(thisJob, largestOutputFormat);
            }
        }

        private void ResizeDicative(Job thisJob, FilmOutputFormat codecFormat)
        {
            string formattedIndex = String.Format("{0:D4}", thisJob.ProductID);
            string sourcePath = ProductionPathHelper.GetProductMp4Path(thisJob.ProductID);
            string targetPath = ProductionPathHelper.GetProductMp4PathByOutputFormat(thisJob.ProductID, codecFormat.ID);

            string cmd = "-y -loglevel panic -i " + sourcePath + " -s " + codecFormat.Width + "x" + codecFormat.Height + " -c:v libx264 -pix_fmt yuv420p -preset ultrafast -qp 19 " + targetPath;

            VCProcess ConcatenateProcess = new VCProcess(production);
            ConcatenateProcess.StartInfo.FileName = Settings.LocalFfmpegExePath;
            ConcatenateProcess.StartInfo.CreateNoWindow = true;
            ConcatenateProcess.StartInfo.UseShellExecute = false;
            ConcatenateProcess.StartInfo.RedirectStandardError = false;
            ConcatenateProcess.StartInfo.RedirectStandardOutput = false;
            ConcatenateProcess.StartInfo.Arguments = cmd;
            ConcatenateProcess.Execute();
            ConcatenateProcess.WaitForExit();
        }

        private string CreateDicativeLine(Job job, FilmOutputFormat largestOutputFormat)
        {
            string result = "";

            string formattedIndex = String.Format("{0:D4}", job.ProductID);
            string indicativePath = ProductionPathHelper.GetProductMp4PathByOutputFormat(job.ProductID, largestOutputFormat.ID);

            if (!File.Exists(indicativePath))
            {
                FireFailureEvent(ProductionErrorStatus.PES_INDICATIVE_MISSING);
                return "";
            }
            else
                result = "file '" + indicativePath + "'";

            return result;
        }

        private bool JoinClips()
        { 
            string cmd = "-y -loglevel panic -safe 0 -f concat -i " + ProductionPathHelper.GetClipListPath(production) + " -c copy ";
            cmd += ProductionPathHelper.GetFullMp4Path(production);

            VCProcess ConcatenateProcess = new VCProcess(production);
            ConcatenateProcess.StartInfo.FileName = Settings.LocalFfmpegExePath;
            ConcatenateProcess.StartInfo.CreateNoWindow = true;
            ConcatenateProcess.StartInfo.UseShellExecute = false;
            ConcatenateProcess.StartInfo.RedirectStandardError = false;
            ConcatenateProcess.StartInfo.RedirectStandardOutput = false;
            ConcatenateProcess.StartInfo.Arguments = cmd;
            bool success = ConcatenateProcess.Execute();

            if (success)
            {
                ConcatenateProcess.WaitForExit();
            }

            if (!File.Exists(ProductionPathHelper.GetFullMp4Path(production)))
            {
                FireFailureEvent(ProductionErrorStatus.PES_JOIN_CLIPS);
                return false;
            }
            else
            {
                return true;
            }

        }

        private void CheckJoining()
        {
            if (!File.Exists(ProductionPathHelper.GetFullMp4Path(production)))
            {
                FireFailureEvent(ProductionErrorStatus.PES_JOIN_CLIPS);
            }
            else
            {
                FireSuccessEvent();
            }
        }
    }
}
