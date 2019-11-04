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
        public static void ManageHeartbeat()
        {
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                { "MachineName", GlobalValues.LocalRenderMachine.Name },
                { "CurrentTime", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") },
                { "Message", "none" },
                { "IsActive", GlobalValues.IsActive.ToString() },
                { "LicenseKey", Settings.LicenseKey }
            };

            string heartBeatString = RemoteDataManager.ExecuteRequest("sendHeartbeat", param);

            if (heartBeatString.Length == 0)
            {
                GlobalValues.ActiveRenderMachine.Name = GlobalValues.LocalRenderMachine.Name;
                GlobalValues.ActiveRenderMachine.Id = GlobalValues.LocalRenderMachine.Id;
                GlobalValues.HasLicense = false;
            }
            else
            {
                try
                {
                    GlobalValues.HasLicense = heartBeatString.Split(new char[] { ':' })[1].Substring(0, 1) == "1";
                }
                catch
                {
                    GlobalValues.HasLicense = false;
                }

                List<Dictionary<string, string>> heartbeatDict = JsonDeserializer.Deserialize(heartBeatString);

                //set local machine data
                if (heartbeatDict.Any(item => item["machinename"] == GlobalValues.LocalRenderMachine.Name))
                {
                    if (heartbeatDict.First(item => item["machinename"] == GlobalValues.LocalRenderMachine.Name)["status"] == "offline")
                    {
                        GlobalValues.ActiveRenderMachine.Name = GlobalValues.LocalRenderMachine.Name;
                        GlobalValues.ActiveRenderMachine.Id = GlobalValues.LocalRenderMachine.Id;
                        return;
                    }
                    else
                    {
                        GlobalValues.LocalRenderMachine.Id = Convert.ToInt32(heartbeatDict.First(item => item["machinename"] == GlobalValues.LocalRenderMachine.Name)["id"]);
                    }
                }

                Dictionary<string, string> activeMachineDict;
                if (heartbeatDict.Any(item => item["force_active"] == "1"))
                {
                    activeMachineDict = heartbeatDict.Where(item => item["force_active"] == "1").OrderBy(item => item["priority"]).First();
                }
                else
                {
                    activeMachineDict = heartbeatDict.OrderBy(item => item["priority"]).First();
                }

                GlobalValues.ActiveRenderMachine.Name = activeMachineDict["machinename"];
                GlobalValues.ActiveRenderMachine.Id = Convert.ToInt32(activeMachineDict["id"]);
            }
        }

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
            string productionListString = RemoteDataManager.ExecuteRequest("getOpenJobs");

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
                { "updateTime", Convert.ToInt64(span.TotalSeconds).ToString() },
                { "rendermachine_id", GlobalValues.LocalRenderMachine.Id.ToString() }
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
