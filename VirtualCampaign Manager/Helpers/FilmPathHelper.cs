using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Helpers
{
    public static class FilmPathHelper
    {
        public static string GetFilmHashPath(Production Production, FilmOutputFormat Format)
        {
            return Path.Combine(ProductionPathHelper.GetLocalProductionHashDirectory(Production), GetFilmFilename(Production, Format));
        }

        public static string GetFilmFilename(Production Production, FilmOutputFormat Format)
        {
            string result = "film_" + Production.FilmID;

            if (Format.IsPanoChild)
            {
                result += "_" + Format.ID + "_" + Format.Size + Format.Extension;
            }
            else
            {
                result += "_" + Format.ID + Format.Extension;
            }

            return result;
        }
    }
}
