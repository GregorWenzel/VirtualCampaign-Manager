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
                AnimatedMotif newAnimatedMotif = ParseAnimatedMotif(animatedMotif);
                if (result.Any(item => item. ID == newAnimatedMotif.ID) == false)
                {
                    result.Add(newAnimatedMotif);
                }
            }

            return result;
        }

        private static AnimatedMotif ParseAnimatedMotif(Dictionary<string, string> motifDict)
        {
            AnimatedMotif result = new AnimatedMotif();
            result.ID = Convert.ToInt32(motifDict["ID"]);
            result.AccountID = Convert.ToInt32(motifDict["AccountID"]);
            result.Extension = Convert.ToString(motifDict["Extension"]);
            
            return result;
        }

        public static Motif Parse(Dictionary<string, string> motifDict)
        {
            Motif result = new Motif();
            result.ID = Convert.ToInt32(motifDict["ContentID"]);
            result.Type = Convert.ToString(motifDict["ContentType"]);
            result.Position = Convert.ToInt32(motifDict["ContentPosition"]);
            result.Extension = Convert.ToString(motifDict["ContentExtension"]);
            result.LoaderName = Convert.ToString(motifDict["ContentLoaderName"]);
            result.Text = Convert.ToString(motifDict["ContentText"]);

            return result;
        }
    }
}
