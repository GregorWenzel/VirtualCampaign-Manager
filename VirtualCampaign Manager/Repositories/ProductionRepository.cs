using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Parsers;

namespace VirtualCampaign_Manager.Repositories
{
    public static class ProductionRepository
    {
        public static List<AnimatedMotif> ReadAnimatedMotifs()
        {
            List<AnimatedMotif> result = new List<AnimatedMotif>();

            string animatedMotifListString = RemoteDataManager.ExecuteRequest("getOpenMotifList");

            List<Dictionary<string, string>> motifDict = JsonDeserializer.Deserialize(animatedMotifListString);

            if (motifDict.Count > 0)
            {
                result = MotifParser.ParseAnimatedMotifList(motifDict);
            }

            return result;
        }

        public static void ReadProductions()
        {
            string productionListString = RemoteDataManager.ExecuteRequest("getOpenProductionList");

            List<Dictionary<string, string>> productionDict = JsonDeserializer.Deserialize(productionListString);

            if (productionDict.Count > 0)
            {
                ProductionParser.ParseList(productionDict);
            }
        }

        public static void DeleteProduction(Production Production)
        {
            Dictionary<string, string> param = new Dictionary<string, string>
            {   { "productionID", Production.ID.ToString() }
            };

            RemoteDataManager.DeleteProduction(param);
        }

        public static void UpdateRemoteValue(Production production, UpdateType type)
        {
            DateTime temp = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
            TimeSpan span = (production.UpdateDate.ToLocalTime() - temp);

            Dictionary<string, string> param = new Dictionary<string, string>
            {   { "productionID", production.ID.ToString() },
                { "updateTime", Convert.ToInt64(span.TotalSeconds).ToString() }
            };

            switch (type)
            {
                case UpdateType.Status:
                    param["status"] = ((int)production.Status).ToString();
                    RemoteDataManager.UpdateProduction(param);
                    break;
                case UpdateType.ErrorCode:
                    param["error_code"] = ((int)production.ErrorStatus).ToString();
                    RemoteDataManager.UpdateProduction(param);
                    break;
                case UpdateType.Priority:
                    param["priority"] = production.Priority.ToString();
                    RemoteDataManager.UpdateProduction(param);
                    break;
            }
        }
    }
}
