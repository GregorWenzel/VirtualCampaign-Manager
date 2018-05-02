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
        public static bool Create(Production production)
        {
            //All files for zip package have been rendered into the hash directory
            string sourceDirectory = ProductionPathHelper.GetLocalProductionHashDirectory(production);

            string targetFile = Path.Combine(ProductionPathHelper.GetLocalProductionHashDirectory(production), "film_" + production.FilmID + "_" + production.Film.FilmOutputFormatList[0].ID + production.Film.FilmOutputFormatList[0].Extension);

            if (File.Exists(targetFile))
                File.Delete(targetFile);

            System.IO.Compression.ZipFile.CreateFromDirectory(sourceDirectory, targetFile);

            return (File.Exists(targetFile));
        }        
    }
}
