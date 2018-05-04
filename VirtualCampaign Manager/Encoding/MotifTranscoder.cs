using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;
using ImageMagick;
using VirtualCampaign_Manager.Helpers;
using System.IO;

namespace VirtualCampaign_Manager.Encoding
{
    public static class MotifTranscoder
    {
        public static bool Transcode(Job job, Motif motif)
        {
            bool result = true;

            MagickReadSettings settings = new MagickReadSettings();
            // Settings the density to 300 dpi will create an image with a better quality
            settings.Density = new Density(300, 300);

            string motifPath = JobPathHelper.GetLocalJobMotifPath(job, motif);

            MagickImage image = new MagickImage();
            try
            {
                image.Read(motifPath, settings);
            }
            catch (Exception ex)
            {
                job.LogText("Ghostscript not installed.");
                return false;
            }

            if ((image.Format != MagickFormat.Jpg && image.Format != MagickFormat.Jpeg) || image.ColorSpace != ColorSpace.sRGB
                || image.Width > 2000 || image.Height > 2000)
            {
                job.LogText(string.Format("Transcoding image from format {0}, colorspace {1} to Jpg and sRGB", image.Format, image.ColorSpace));

                image.Format = MagickFormat.Jpg;
                image.ColorSpace = ColorSpace.sRGB;
                motif.OriginalExtension = motif.Extension;
                motif.Extension = ".jpg";

                if (image.Width > 2000 || image.Height > 2000)
                {
                    job.LogText(string.Format("Resizing image from {0}x{1} to 1024x{2}", image.Width, image.Height, (1024f / 2000f) * image.Height));
                    image.Resize(1024, 0);

                }

                string motifOutputPath = JobPathHelper.GetLocalJobMotifPath(job, motif);
                image.Write(motifOutputPath);
                return (System.IO.File.Exists(motifOutputPath));
            }

            return result;
        }

        public static bool Extract(Job job, Motif motif)
        {
            IOHelper.CreateDirectory(JobPathHelper.GetLocalJobAnimatedMotifDiretory(job.Production, motif));

            string sourcePath = JobPathHelper.GetLocalJobMotifPath(job, motif);
            string targetPath = Path.Combine(JobPathHelper.GetLocalJobAnimatedMotifDiretory(job.Production, motif), @"motif_F%04d.tga");

            string parameters = "-i " + sourcePath + " " + targetPath;

            VCProcess process = new VCProcess(job);
            process.StartInfo.FileName = Settings.LocalFfmpegExePath;
            process.StartInfo.Arguments = parameters;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = false;
            process.StartInfo.RedirectStandardOutput = false;
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            process.Execute();
            process.WaitForExit();

            motif.Frames = Directory.GetFiles(JobPathHelper.GetLocalJobAnimatedMotifDiretory(job.Production, motif), "*.tga").Length;
            motif.Extension = ".tga";

            return motif.Frames > 0;
        }
    }
}
