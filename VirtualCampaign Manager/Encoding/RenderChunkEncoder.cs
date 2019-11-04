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
    public static class RenderChunkEncoder
    {
        private const int MAX_ENCODING_TASKS = 1;

        private static int Counter = 0;

        public static bool IsBusy = false;

        public static void EncodeLargestChunk(RenderChunkStatus LargestChunk)
        {
            if (IsBusy) return;

            IsBusy = true;

            bool isZipProduction = LargestChunk.Job.Production.IsZipProduction;

            if (isZipProduction == false)
            {
                for (int i = LargestChunk.StartIndex; i <= LargestChunk.EndIndex; i++)
                {
                    LargestChunk.Job.RenderChunkStatusList.First(item => item.StartIndex == i).Status = 6;
                }

                string cmd = "";
                cmd += "-hwaccel cuvid";
                cmd += " -y -loglevel panic";
                cmd += " -start_number " + LargestChunk.StartFrame;
                cmd += " -f image2";
                cmd += " -i " + JobPathHelper.GetLocalJobRenderOutputFileMask(LargestChunk.Job);
                cmd += " -vframes " + (LargestChunk.EndFrame - LargestChunk.StartFrame + 1);
                cmd += " -pix_fmt yuv420p";

                if (Settings.UseCuda)
                {
                    cmd += " -c:v h264_nvenc";
                }
                else
                {
                    cmd += " -c:v libx264";
                }

                cmd += " -profile:v high -qp 19 -preset fast ";
                cmd += JobPathHelper.GetRenderChunkPath(LargestChunk);

                Encode(LargestChunk.Job, cmd);
            }

            for (int i = LargestChunk.StartIndex; i <= LargestChunk.EndIndex; i++)
            {
                LargestChunk.Job.RenderChunkStatusList.First(item => item.StartIndex == i).Status = 7;
            }

            IsBusy = false;
        }

        public static bool MergeChunks(Job job)
        {
            if (CheckOutputs(job) == false)
            {
                job.ErrorStatus = JobErrorStatus.JES_OUTPUTFILE_COUNT_MISMATCH;
                return false;
            }

            SavePreviewImage(job);

            if (job.Production.IsZipProduction) return true;

            CreateChunklistFile(job);

            string cmd = "-y -loglevel panic -safe 0 -f concat -i " + JobPathHelper.GetChunkListPath(job) + " -c copy ";
            cmd += JobPathHelper.GetJobClipPath(job);

            Encode(job, cmd);

            return true;
        }

        private static bool CheckOutputs(Job job)
        {
            if (job.Production.IsZipProduction) return true;

            int outputCount = Directory.GetFiles(JobPathHelper.GetLocalJobRenderOutputDirectory(job), "*" + job.OutputExtension).Length;
            return (outputCount > 1 && outputCount == job.FrameCount);
        }

        private static void Encode(Job job, string parameters)
        {
            VCProcess ConcatenateProcess = new VCProcess(job);
            ConcatenateProcess.StartInfo.FileName = Settings.LocalFfmpegExePath;
            ConcatenateProcess.StartInfo.CreateNoWindow = true;
            ConcatenateProcess.StartInfo.UseShellExecute = false;
            ConcatenateProcess.StartInfo.RedirectStandardError = false;
            ConcatenateProcess.StartInfo.RedirectStandardOutput = false;
            ConcatenateProcess.StartInfo.Arguments = parameters;
            ConcatenateProcess.Execute();
            ConcatenateProcess.WaitForExit();
            ConcatenateProcess.Close();
        }

        private static bool SavePreviewImage(Job job)
        {
            if (job.IsDicative) return true;

            string directoryName;

            if (job.Production.IsPreview == false)
            {
                directoryName = ProductionPathHelper.GetLocalProductionPreviewDirectory(job.Production);
            }
            else
            {
                directoryName = ProductionPathHelper.GetLocalProductPreviewProductionDirectory(job.Production);
            }
           
            string fileName;

            if (job.Production.IsZipProduction == false)
            {
                job.PreviewFrame = 20;
                fileName = Path.Combine(new string[] { JobPathHelper.GetLocalJobRenderOutputDirectory(job), "F" + String.Format("{0:D4}", (job.PreviewFrame + job.InFrame)) + job.OutputExtension });
            }
            else
            {
                fileName = Directory.GetFiles(JobPathHelper.GetLocalJobRenderOutputPathForZip(job))[0];
            }

            Dictionary<string, string> previewPicDict = new Dictionary<string, string>
            {
                { "hdpi.jpg", "640:360" },
                { "mdpi.jpg", "320:180" },
                { "ldpi.jpg", "160:90" }
            };
 
            string cmd = " -y -i " + fileName;

            foreach (KeyValuePair<string, string> kv in previewPicDict)
            {
                if (job.Production.IsPreview == false)
                {
                    cmd += string.Format(" -vf scale={0} {1}", kv.Value, Path.Combine(directoryName, string.Format("film_{0}_preview_{1}", job.Production.FilmID, kv.Key)));
                }
                else
                {
                    cmd += string.Format(" -vf scale={0} {1}", kv.Value, Path.Combine(directoryName, string.Format("{0:0000}_{1}", job.ProductID, kv.Key)));
                }
            }

            //create previews for krpano productions
            if (job.Production.ContainsPano)
            {
                directoryName = ProductionPathHelper.GetLocalProductionHashDirectory(job.Production);
                IOHelper.CreateDirectory(directoryName);

                FilmOutputFormat outputFormat = job.Production.Film.FilmOutputFormatList.First(item => item.Name.Contains("K360"));

                //full size
                string scale = outputFormat.Width + ":" + outputFormat.Height;
                cmd += string.Format(" -vf scale={0} {1}", scale, Path.Combine(directoryName, string.Format("film_{0}_preview_{1}.jpg", job.Production.FilmID, scale.Replace(":", "x"))));

                //quarter size
                scale = (int)Math.Round(outputFormat.Width / 2d) + ":" + (int)Math.Round(outputFormat.Height / 2d);
                cmd += string.Format(" -vf scale={0} {1}", scale, Path.Combine(directoryName, string.Format("film_{0}_preview_{1}.jpg", job.Production.FilmID, scale.Replace(":", "x"))));
            }

            Encode(job, cmd);

            return true;
        }

        private static void CreateChunklistFile(Job job)
        {
            //get chunk files
            string[] chunkFiles = Directory.GetFiles(JobPathHelper.GetLocalJobDirectory(job), "clip_" + job.Position + "_chunk_*.mp4");
            chunkFiles = chunkFiles.OrderBy(x => x).ToArray();

            //create cliplist file
            string clipListFilename = JobPathHelper.GetChunkListPath(job);
            StreamWriter writer = new StreamWriter(clipListFilename, false);
            writer.WriteLine(@"# chunklist");

            foreach (string chunkFile in chunkFiles)
            {
                writer.WriteLine("file '" + chunkFile + "'");

            }
            writer.Close();
        }
    }
}

