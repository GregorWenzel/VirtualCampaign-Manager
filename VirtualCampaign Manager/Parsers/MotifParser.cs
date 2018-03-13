using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Parsers
{
    public static class MotifParser
    {
        public static List<AnimatedMotif> ParseAnimatedMotifList(List<Dictionary<string, string>> motifDict)
        {
            List<AnimatedMotif> result = new List<AnimatedMotif>();

            foreach (Dictionary<string, string> animatedMotif in motifDict)
            {
                AnimatedMotif newAnimatedMotif = Parse(animatedMotif);
                if (result.Any(item => item. ID == newAnimatedMotif.ID) == false)
                {
                    result.Add(newAnimatedMotif);
                }
            }

            return result;
        }

        private static AnimatedMotif Parse(Dictionary<string, string> motifDict)
        {
            AnimatedMotif result = new AnimatedMotif();
            result.ID = Convert.ToInt32(motifDict["ID"]);
            result.AccountID = Convert.ToInt32(motifDict["AccountID"]);
            result.Extension = Convert.ToString(motifDict["Extension"]);
            
            return result;
        }
    }
}
