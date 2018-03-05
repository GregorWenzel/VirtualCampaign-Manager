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
        public static void UpdateFilmDuration(Production Production, Film Film)
        {
            DateTime temp = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
            TimeSpan span = (Production.UpdateDate.ToLocalTime() - temp);

            Dictionary<string, string> param = new Dictionary<string, string>
            {   { "productionID", Production.ID.ToString() },
                { "updateTime", Convert.ToInt64(span.TotalSeconds).ToString() }
            };

            param["duration"] = (IndicativeFrames + AbdicativeFrames + ClipFrames).ToString();
            param["size"] = sizeString;
            RemoteDataManager.UpdateFilm(param);
        }
    }
}