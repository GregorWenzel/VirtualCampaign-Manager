using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Repositories;

namespace VirtualCampaign_Manager.Parsers
{
    public static class ProductionParser
    {
        public static List<Production> ParseList(List<Dictionary<string, string>> productionDict)
        {
            List<Production> result = new List<Production>();

            foreach (Dictionary<string,string> production in productionDict)
            {
                Production newProduction = Parse(production);
                if (result.Any(item => item.ID == newProduction.ID) == false)
                {
                    result.Add(newProduction);
                }
            }

            return result;
        }

        public static Production Parse(Dictionary<string, string> productionDict)
        {
            Production result = new Production();

            if (productionDict["UpdateTime"] != "")
                result.UpdateDate = Helpers.HelperFunctions.ParseDateTime(productionDict["UpdateTime"]);
            else
                result.UpdateDate = Helpers.HelperFunctions.ParseDateTime(productionDict["CreationTime"]);

            result.creationTime = Convert.ToString(productionDict["CreationTime"]);
            result.Film = new Film();

            string[] formatBuffer = productionDict["Formats"].Split(new char[] { ',' });

            foreach (string format in formatBuffer)
            {
                int formatId = Convert.ToInt32(format);
                FilmOutputFormat filmOutputFormat = GlobalValues.CodecDict[formatId];
                result.Film.FilmOutputFormatList.Add(filmOutputFormat);
            }

            result.HasSpecialIntroMusic = Convert.ToString(productionDict["SpecialIntroMusic"]) == "1";
            result.IsPreview = Convert.ToInt32(productionDict["IsPreview"]) == 1;
            result.ID = Convert.ToInt32(productionDict["ID"]);
            result.Priority = Convert.ToInt32(productionDict["Priority"]);
            result.Email = Convert.ToString(productionDict["Email"]);
            result.Film.ID = Convert.ToInt32(productionDict["FilmID"]);
            result.Film.UrlHash = Convert.ToString(productionDict["FilmUrlHash"]);
            result.AccountID = Convert.ToInt32(productionDict["AccountID"]);
            result.IndicativeID = Convert.ToInt32(productionDict["IndicativeID"]);
            result.AbdicativeID = Convert.ToInt32(productionDict["AbdicativeID"]);
            result.AudioID = Convert.ToInt32(productionDict["AudioID"]);
            result.Username = Convert.ToString(productionDict["UserName"]);
            result.Name = Convert.ToString(productionDict["Name"]);
            result.SetStatus((ProductionStatus)Enum.ToObject(typeof(ProductionStatus), Convert.ToInt32(productionDict["Status"])));
            result.SetErrorStatus((ProductionErrorStatus)Enum.ToObject(typeof(ProductionErrorStatus), Convert.ToInt32(productionDict["ErrorCode"])));
            result.JobList = JobRepository.ReadJobs(result);

            return result;
        }
    }
}
