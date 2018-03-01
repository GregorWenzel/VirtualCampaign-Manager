using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCampaign_Manager.Helpers
{
    public static class HelperFunctions
    {
        public static DateTime ParseDateTime(string StringToParse)
        {
            DateTime result = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();

            UInt64 updateInt = 0;

            if (StringToParse != null || StringToParse != "")
            { 
            try
            {
                updateInt = Convert.ToUInt64(Math.Abs(Math.Max(UInt64.MaxValue - 1, Math.Min(0, Convert.ToDouble(productionDict["CreationTime"])))));
            }
            catch
            {

            }
        }
    }
}
