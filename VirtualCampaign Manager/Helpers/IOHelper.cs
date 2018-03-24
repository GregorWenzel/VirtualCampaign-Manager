using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Helpers
{
    public static class IOHelper
    {
        public static bool CreateDirectory(string Path)
        {
            if (!Directory.Exists(Path))
            {
                try
                {
                    Directory.CreateDirectory(Path);
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }

        public static void DeleteDirectory(string Path)
        {
            if (Directory.Exists(Path))
            {
                try
                {
                    Directory.Delete(Path, true);
                }
                catch { }
            }
        }

        public static string GetFilmFileSizeString(Production production)
        {
            string[] buffer = new string[production.Film.FilmOutputFormatList.Count];
           
            for (int i = 0; i < production.Film.FilmOutputFormatList.Count; i++)
            {
                FilmOutputFormat filmOutputFormat = production.Film.FilmOutputFormatList[i];

                string filePath = FilmPathHelper.GetFilmHashPath(production, filmOutputFormat);

                if (File.Exists(filePath))
                {
                    FileInfo fileInfo = new FileInfo(filePath);

                    buffer[i] = fileInfo.Length.ToString();
                }
                else
                {
                    buffer[i] = "0";
                }
            }

            return String.Join(".", buffer);
        }
    }
}