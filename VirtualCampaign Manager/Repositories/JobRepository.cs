using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

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
                    param["render_id"] = Job.RenderJobID;
                    break;
            }

            RemoteDataManager.UpdateJob(param);
        }
    }
}