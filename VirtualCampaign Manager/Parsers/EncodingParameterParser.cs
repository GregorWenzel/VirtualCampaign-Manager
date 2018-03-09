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
        //Returns 
        public static string Parse(Production Production, FilmOutputFormat Format)
        {
            string result;

            if (Production.AudioID > 0)
            {
                result = " -map 0:v -map 1:a \"";
            }
            
            string filmPath = FilmPathHelper.GetFilmHashPath(Production, Format);                      

            /*only create encoding line if the current codec info id does not indicate Pano
            * Pano is only a flag that indicates the need for additional encoding for 360° movies
             */
            if (Format.ID != 20)
            {               
                result = Format.FfmpegParams + " \"" + filmPath + "\" ";
            }

            return result;
        }
    }
}
