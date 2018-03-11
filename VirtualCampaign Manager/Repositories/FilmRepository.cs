using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Repositories
{
    public static class FilmRepository
    {
        public static void UpdateFilmDuration(Production Production)
        {
            DateTime temp = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
            TimeSpan span = (Production.UpdateDate.ToLocalTime() - temp);

            Dictionary<string, string> param = new Dictionary<string, string>
            {   { "productionID", Production.ID.ToString() },
                { "updateTime", Convert.ToInt64(span.TotalSeconds).ToString() }
            };

            param["duration"] =  Production.TotalFrameCount.ToString();
            param["size"] = sizeString;
            RemoteDataManager.UpdateFilm(param);
        }

        public static void UpdateHistoryTable(Production production)
        {
            DateTime temp = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
            TimeSpan span = (production.UpdateDate.ToLocalTime() - temp);

            Dictionary<string, string> param = new Dictionary<string, string>
            {   { "productionID", production.ID.ToString() },
                   { "updateTime", Convert.ToInt64(span.TotalSeconds).ToString() },
            };

            int[] jobIdList = new int[production.JobList.Count];
            string[] totalMotifIDList = new string[production.JobList.Count];
            int[] isDicativeList = new int[production.JobList.Count];

            for (int i = 0; i < production.JobList.Count; i++)
            {
                jobIdList[i] = production.JobList[i].ProductID;

                if (production.JobList[i].IsDicative)
                    isDicativeList[i] = 1;
                else
                    isDicativeList[i] = 0;

                int[] motifIDList = new int[production.JobList[i].MotifList.Count];

                for (int j = 0; j < production.JobList[i].MotifList.Count; j++)
                {
                    motifIDList[j] = production.JobList[i].MotifList[j].ID;
                }

                totalMotifIDList[i] = String.Join(".", motifIDList);
            }

            param["DicativeList"] = String.Join(",", isDicativeList);
            param["JobIDList"] = String.Join(",", jobIdList);
            param["MotifIDList"] = String.Join(",", totalMotifIDList);
            param["FilmID"] = Convert.ToString(production.Film.ID);
            param["AccountID"] = Convert.ToString(production.AccountID);
            param["FilmName"] = production.Name;

            RemoteDataManager.UpdateHistory(param);
        }
    }
}