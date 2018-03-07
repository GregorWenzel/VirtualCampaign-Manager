using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Helpers;

namespace VirtualCampaign_Manager.Parsers
{
    public static class EncodingParameterParser
    {
        public static string Parse(Production Production, FilmOutputFormat Format)
        {
            string result = "";

            string partialTargetPath1 = ProductionPathHelper.GetPartialFilmFilename(Production);
            string partialTargetPath2 = "";

            if (Production.AudioID > 0)
            {
                partialTargetPath2 = " -map 0:v -map 1:a \"" + partialTargetPath1;
            }

            /*only create encoding line if the current codec info id does not indicate Pano
            * Pano is only a flag that indicates the need for additional encoding for 360° movies
             */
            if (Format.ID != 20)
            {
                string fullTargetPath = "";
                if (Format.IsPanoChild)
                {
                    fullTargetPath = partialTargetPath2 + "_" + Format.ID + "_" + Format.Size + Format.Extension + "\"";
                }
                else
                {
                    fullTargetPath = partialTargetPath2 + "_" + Format.ID + Format.Extension + "\"";
                }
                result = Format.FfmpegParams + " " + fullTargetPath + " ";
            }

            return result;
        }
    }
}
