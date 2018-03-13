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
        public static List<Production> ReadProductions()
        {
            List<Production> result = new List<Production>();

            string productionListString = RemoteDataManager.ExecuteRequest("getOpenProductions");

            List<Dictionary<string, string>> productionDict = JsonDeserializer.Deserialize(productionListString);

            if (productionDict.Count > 0)
            {
                result = ProductionParser.ParseList(productionDict);
            }

            return result;
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
                case UpdateType.Film:
                    param["duration"] = production.TotalFrameCount.ToString();
                    param["size"] = sizeString;
                    RemoteDataManager.UpdateFilm(param);
                    break;
                case UpdateType.Priority:
                    param["priority"] = production.Priority.ToString();
                    RemoteDataManager.UpdateProduction(param);
                    break;
            }
        }
    }
}
