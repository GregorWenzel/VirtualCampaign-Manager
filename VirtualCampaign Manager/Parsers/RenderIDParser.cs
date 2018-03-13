using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCampaign_Manager.Parsers
{
    public static class RenderIDParser
    {
        public static string Parse(string deadline_string)
        {
            string[] lines = deadline_string.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            string[] resultArr = new string[2];
            string[] renderIDArr = new string[2];

            foreach (string line in lines)
            {
                if (line.Contains("Result="))
                {
                    resultArr = line.Split('=');
                }
                else if (line.Contains("JobID="))
                {
                    renderIDArr = line.Split('=');
                }
            }

            if (resultArr[1] != "Success")
                return null;
            else
                return renderIDArr[1];
        }
    }
}
