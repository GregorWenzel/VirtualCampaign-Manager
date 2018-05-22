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
        public static void ParseList(List<Dictionary<string, string>> productionDict)
        {
            foreach (Dictionary<string,string> production in productionDict)
            {
                int newProductionID = Convert.ToInt32(production["ProductionID"]);
                Production thisProduction;
                if (GlobalValues.ProductionList.Any(item => item.ID == newProductionID) == false)
                {
                    thisProduction = Parse(production);
                    GlobalValues.ProductionList.Add(thisProduction);
                }
                else
                {
                    thisProduction = GlobalValues.ProductionList.First(item => item.ID == newProductionID);
                }

                int newJobID = Convert.ToInt32(production["JobID"]);
                Job thisJob;
                if (thisProduction.JobList.Any(item => item.ID == newJobID) == false)
                {
                    thisJob = JobParser.Parse(production);
                    thisJob.Production = thisProduction;
                    thisProduction.JobList.Add(thisJob);
                    GlobalValues.JobList.Add(thisJob);
                }
                else
                {
                    thisJob = thisProduction.JobList.First(item => item.ID == newJobID);
                }

                if (thisJob.IsDicative == false)
                {
                    int newMotifID = Convert.ToInt32(production["ContentID"]);
                    int newMotifPosition = Convert.ToInt32(production["ContentPosition"]);
                    if (thisJob.MotifList.Any(item => item.ID == newMotifID && item.Position == newMotifPosition) == false)
                    {
                        Motif newMotif = MotifParser.Parse(production);
                        thisJob.MotifList.Add(newMotif);
                    }
                }
            }

            foreach (Production production in GlobalValues.ProductionList)
            {
                production.JobList = production.JobList.OrderBy(item => item.Position).ToList();
            }
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
            result.ID = Convert.ToInt32(productionDict["ProductionID"]);
            result.Priority = Convert.ToInt32(productionDict["Priority"]);
            result.Email = Convert.ToString(productionDict["Email"]);
            result.Film.ID = Convert.ToInt32(productionDict["FilmID"]);
            result.Film.UrlHash = Convert.ToString(productionDict["FilmUrlHash"]);
            result.AccountID = Convert.ToInt32(productionDict["AccountID"]);
            result.IndicativeID = Convert.ToInt32(productionDict["IndicativeID"]);
            result.AbdicativeID = Convert.ToInt32(productionDict["AbdicativeID"]);
            result.AudioID = Convert.ToInt32(productionDict["AudioID"]);
            result.Username = Convert.ToString(productionDict["UserName"]);
            result.Name = Convert.ToString(productionDict["FilmName"]);
            result.SetStatus((ProductionStatus)Enum.ToObject(typeof(ProductionStatus), Convert.ToInt32(productionDict["ProductionStatus"])));
            result.SetErrorStatus((ProductionErrorStatus)Enum.ToObject(typeof(ProductionErrorStatus), Convert.ToInt32(productionDict["ProductionErrorCode"])));

            result.JobList = new List<Job>();
            return result;
        }
    }
}
