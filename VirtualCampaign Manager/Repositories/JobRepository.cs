using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Parsers;

namespace VirtualCampaign_Manager.Repositories
{
    public enum UpdateType
    {
        Status,
        ErrorCode,
        OutputExtension,
        Film,
        Priority,
        RenderID
    };

    public static class JobRepository
    {
        public static List<Job> ReadJobs(Production production)
        {
            List<Job> result = new List<Job>();

            Dictionary<string, string> param = new Dictionary<string, string>
            {   { "productionID", production.ID.ToString() },
                { "is_preview", Convert.ToInt32(production.IsPreview).ToString() }
            };

            string productionListString = RemoteDataManager.ExecuteRequest("getJobsByProductionID", param);
            List<Dictionary<string, string>> jobDict = JsonDeserializer.Deserialize(productionListString);
            
            if (jobDict.Count > 0)
            {
                result = JobParser.ParseList(production, jobDict);
            }
                        
            result = new List<Job>(result.OrderBy(item => item.Position));

            //find first clip to be rendered
            foreach (Job job in result)
            {
                job.Production = production;
                if (!job.IsDicative)
                {
                    job.IsFirstRealClip = true;
                    break;
                }
            }

            return result;
        }

        public static void UpdateJob(Job Job, UpdateType Type)
        {
            if (!Job.CanUpdateRemoteData)
                return;

            DateTime temp = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
            TimeSpan span = (Job.UpdateDate.ToLocalTime() - temp);

            Dictionary<string, string> param = new Dictionary<string, string>
            {   { "jobID", Job.ID.ToString() },
                { "updateTime", Math.Max(0, span.TotalSeconds).ToString() }
            };

            switch (Type)
            {
                case UpdateType.Status:
                    param["status"] = ((int) Job.Status).ToString();
                    break;
                case UpdateType.ErrorCode:
                    param["error_code"] = ((int) Job.ErrorStatus).ToString();
                    break;
                case UpdateType.OutputExtension:
                    param["output_extension"] = Job.OutputExtension;
                    break;
                case UpdateType.RenderID:
                    param["render_id"] = Job.RenderID;
                    break;
            }

            RemoteDataManager.UpdateJob(param);
        }
    }
}