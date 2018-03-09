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
    public class PreviewFilmEncoder : EventFireBase
    {
        List<string[]> mp4PreviewDefinitionList = new List<string[]>
            {
                new string[] { "640x360", "hdpi"},
                new string[] { "320x180", "mdpi" },
                new string[] { "160x90", "ldpi"}
            };

        Production production;

        public PreviewFilmEncoder(Production Production)
        {
            production = Production;
        }

        public void Encode()
        {
            if (production.IsPreview)
            {
                EncodePreviewForProduct();
            }
            else
            {
                EncodePreviewForProduction();
            }
        }

        private void EncodePreviewForProduct()
        {
            string directoryName = ProductionPathHelper.GetLocalProductPreviewProductionDirectory(production.JobList[0].OriginalProductID);
            IOHelper.CreateDirectory(directoryName);

            //find first output format that does not indicate KRPANO
            string sourcePath = production.Film.FilmOutputFormatList.First(item => item.ID != 20).FullFilePath;

            foreach (string[] mp4PreviewDefinition in mp4PreviewDefinitionList)
            {
                string sizeStr = mp4PreviewDefinition[0];
                string fileSuffix = mp4PreviewDefinition[1];
                string targetPath = ProductionPathHelper.GetLocalProductPreviewProductionPath(production.JobList[0].OriginalProductID, fileSuffix);
                string cmd = "-y -loglevel panic -i " + sourcePath + " ";

                cmd += string.Format("-s {0} -threads 8 -c:v libx264 -pix_fmt yuv420p -preset medium -an {1}", sizeStr, targetPath);
                EncodeFilm(cmd);

                if (!File.Exists(targetPath))
                {
                    FireFailureEvent(ProductionErrorStatus.PES_CREATE_MP4PREVIEWS);
                    break;
                }
            }

            FireSuccessEvent();
        }

        private void EncodePreviewForProduction()
        {
            string directoryName = ProductionPathHelper.GetLocalProductionPreviewDirectory(production);
            IOHelper.CreateDirectory(directoryName);

            //find first output format that does not indicate KRPANO
            string sourcePath = production.Film.FilmOutputFormatList.First(item => item.ID != 20).FullFilePath;

            foreach (string[] mp4PreviewDefinition in mp4PreviewDefinitionList)
            {
                string sizeStr = mp4PreviewDefinition[0];
                string fileSuffix = mp4PreviewDefinition[1];
                string targetPath = ProductionPathHelper.GetLocalProductionPreviewPath(production, fileSuffix);
                string cmd = "-y -loglevel panic -i " + sourcePath + " ";

                cmd += string.Format("-s {0} -threads 8 -c:v libx264 -pix_fmt yuv420p -preset medium -an {1}", sizeStr, targetPath);
                EncodeFilm(cmd);

                if (!File.Exists(targetPath))
                {
                    FireFailureEvent(ProductionErrorStatus.PES_CREATE_MP4PREVIEWS);
                    break;
                }
            }

            FireSuccessEvent();
        }

        private void EncodeFilm(string parameters)
        {
            VCProcess process = new VCProcess(production);
            process.StartInfo.FileName = Settings.LocalFfmpegExePath;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = false;
            process.StartInfo.RedirectStandardOutput = false;
            process.Execute();
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            process.WaitForExit();

        }
    }
}