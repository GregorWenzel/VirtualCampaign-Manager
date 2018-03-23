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
    public static class ZipFileCreator
    {
        public static bool Create(Job job)
        {
            string sourceDirectory = JobPathHelper.GetLocalJobRenderOutputDirectory(job);

            RemoveTrailingDigitsFromOutputFiles(sourceDirectory);

            string targetFile = Path.Combine(ProductionPathHelper.GetLocalProductionHashDirectory(job.Production), "film_" + job.Production.FilmID + "_" + job.Production.Film.FilmOutputFormatList[0].ID + job.Production.Film.FilmOutputFormatList[0].Extension);

            IOHelper.CreateDirectory(ProductionPathHelper.GetLocalProductionHashDirectory(job.Production));

            if (File.Exists(targetFile))
                File.Delete(targetFile);

            System.IO.Compression.ZipFile.CreateFromDirectory(sourceDirectory, targetFile);

            return (File.Exists(targetFile));
        }

        private static void RemoveTrailingDigitsFromOutputFiles(string sourceDirectory)
        {
            if (!Directory.Exists(sourceDirectory)) return;

            DirectoryInfo dirInfo = new DirectoryInfo(sourceDirectory);
            FileInfo[] fileInfos = dirInfo.GetFiles();

            foreach (FileInfo fileInfo in fileInfos)
            {
                string sourceFullName = fileInfo.FullName;
                string extension = Path.GetExtension(sourceFullName);
                string dir = Path.GetDirectoryName(sourceFullName);
                string sourceFileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFullName);
                int len = sourceFileNameWithoutExtension.Length;
                if (char.IsDigit(sourceFileNameWithoutExtension[len - 4]) && char.IsDigit(sourceFileNameWithoutExtension[len - 1]))
                {
                    string targetNameWithoutExtension = sourceFileNameWithoutExtension.Substring(0, len - 4);
                    string targetName = Path.Combine(dir, targetNameWithoutExtension + extension);
                    File.Move(sourceFullName, targetName);
                }
            }
        }
    }
}
