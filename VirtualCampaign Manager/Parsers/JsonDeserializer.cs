using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCampaign_Manager.Parsers
{
    class JsonDeserializer
    {
        public static List<Dictionary<string, string>> Deserialize(string jsonString)
        {
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();
            string[] split1;

            if (jsonString.Contains(@":["))
            {
                split1 = jsonString.Split(new string[] { ":[" }, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                split1 = new string[] { "", jsonString };
            }

            string[] split2 = split1[1].Trim(new char[] { ']', '}', '}' }).Split(new char[] { '{' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string split in split2)
            {
                string[] split3 = split.Split(new char[] { ',', '}' }, StringSplitOptions.RemoveEmptyEntries);

                Dictionary<string, string> splitDict = new Dictionary<string, string>();
                string lastKey = null;
                for (int i = 0; i < split3.Length; i++)
                {
                    string split4 = split3[i];
                    string keyString = "";
                    string valueString = "";
                    string[] keyValueString = split4.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (keyValueString.Length < 2)
                    {
                        if (i > 0 && lastKey != null)
                        {
                            splitDict[lastKey] += "," + keyValueString[0].Trim(new char[] { '\"' });
                        }
                        continue;
                    }

                    keyString = keyValueString[0].Trim(new char[] { '\"' });
                    if (keyValueString.Length > 2)
                    {
                        valueString = string.Join(":", keyValueString, 1, keyValueString.Length - 1).Trim(new char[] { '\"' });
                        //valueString = keyValueString[1].Trim(new char[] { '\"' });
                        //valueString += ":" + keyValueString[2].Trim(new char[] { '\"' });
                    }
                    else
                    {
                        valueString = keyValueString[1].Trim(new char[] { '\"' });
                    }

                    splitDict[keyString] = valueString.Trim(new char[] { '\"' });
                    lastKey = keyString;
                }

                result.Add(splitDict);
            }
            return result;
        }
    }
}