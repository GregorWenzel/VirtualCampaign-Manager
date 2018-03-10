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

        public static void UpdateRemoteValue(Production Prodcution, UpdateType Type)
        {
            if (!CanUpdateRemoteData) return;

            DateTime temp = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
            TimeSpan span = (Prodcution.UpdateDate.ToLocalTime() - temp);

            Dictionary<string, string> param = new Dictionary<string, string>
            {   { "productionID", Prodcution.ID.ToString() },
                { "updateTime", Convert.ToInt64(span.TotalSeconds).ToString() }
            };

            switch (Type)
            {
                case UpdateType.Status:
                    param["status"] = ((int)Prodcution.Status).ToString();
                    RemoteDataManager.UpdateProduction(param);
                    break;
                case UpdateType.ErrorCode:
                    param["error_code"] = ((int)Prodcution.ErrorStatus).ToString();
                    RemoteDataManager.UpdateProduction(param);
                    break;
                case UpdateType.Film:
                    param["duration"] = (IndicativeFrames + AbdicativeFrames + ClipFrames).ToString();
                    param["size"] = sizeString;
                    JSONRemoteManager.Instance.UpdateFilm(param);
                    break;
                case UpdateType.Priority:
                    param["priority"] = Prodcution.Priority.ToString();
                    RemoteDataManager.UpdateProduction(param);
                    break;
            }
        }
    }
}
