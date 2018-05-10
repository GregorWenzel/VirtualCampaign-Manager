using HelperFunctions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Repositories
{
    public static class RemoteDataManager
    {
        private static string ServiceCallString(string serviceName)
        {
            string SALTED = Settings.SALTED;
            string pass = Encrypt.GetSHA1Hash(Encrypt.GetSHA1Hash(serviceName) + Settings.SALTED);

            string result = Settings.ServicesUrl + "?pass=" + pass + "&call=" + serviceName;
            return result;
        }

        public static string ExecuteRequest(string serivceName, int retryCounter = 0)
        {
            string responseContent = "";

            string url = ServiceCallString(serivceName);
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
            HttpWebResponse response = null;
            StreamReader reader = null;
            request.KeepAlive = false;

            try
            {
                response = (HttpWebResponse) request.GetResponse();
                reader = new StreamReader(response.GetResponseStream());
                responseContent = reader.ReadToEnd();
                reader.Close();
                response.Close();
            }
            catch (Exception e)
            {
                if (reader != null) reader.Close();
                if (response != null) response.Close();

                Console.WriteLine(string.Format("REQUEST {0} failed. Retry counter = {1}\r\nReason:{2}", serivceName, retryCounter, e.Message));

                return "";
            }

            if (responseContent.ToLower().Contains("error") && !responseContent.ToLower().Contains("errorcode"))
            {
                return "";
            }

            return responseContent;
        }

        private static string GetPostData(Dictionary<string, string> postDict)
        {
            List<string> buffer = new List<string>();

            foreach (KeyValuePair<string, string> item in postDict)
            {
                buffer.Add(item.Key + "=" + item.Value);
            }

            string[] buffer2 = buffer.ToArray();

            return string.Join("&", buffer2);
        }

        public static string ExecuteRequest(string serivceName, Dictionary<string, string> postDict, int retryCounter = 0)
        {
            Console.WriteLine(serivceName);

            /* DEBUG OUTPUT of parameters
            foreach (KeyValuePair<string, string> kvp in postDict)
            {
                Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
            }
            */

            string result = "";
            string postData = GetPostData(postDict);
            string url = ServiceCallString(serivceName);
            byte[] byteArray = System.Text.Encoding.ASCII.GetBytes(postData);

            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;
            request.KeepAlive = false;

            HttpWebResponse response = null;
            StreamReader reader = null;

            try
            {
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                
                response = (HttpWebResponse) request.GetResponse();

                string responseContent;
                reader = new StreamReader(response.GetResponseStream());

                responseContent = reader.ReadToEnd();

                reader.Close();
                response.Close();

                return responseContent;
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                if (response != null) response.Close();

                Console.WriteLine(string.Format("REQUEST {0} failed. Retry counter = {1}\r\nReason:{2}", serivceName, retryCounter, ex.Message));
                if (retryCounter >= 0)
                {
                    return "";
                }
                else
                {
                    return ExecuteRequest(serivceName, postDict, ++retryCounter);
                }
            }
        }

        public static bool UpdateJob(Dictionary<string, string> param)
        {
            string result = ExecuteRequest("updateJob", param);
            return true;
        }

        public static bool UpdateProduction(Dictionary<string, string> param)
        {
            string result = ExecuteRequest("updateProduction", param);
            return true;
        }

        public static void DeleteProduction(Dictionary<string, string> param)
        {
            string result = ExecuteRequest("deleteProduction", param);
        }

        public static bool UpdateFilm(Dictionary<string, string> param)
        {
            string result = ExecuteRequest("updateFilm", param);
            return true;
        }

        public static bool UpdateHistory(Dictionary<string, string> param)
        {
            string result = ExecuteRequest("updateHistory", param);
            return true;
        }

        public static bool UpdateMotif(Motif Motif)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();
            param["motifID"] = Motif.ID.ToString();
            param["frameCount"] = Motif.Frames.ToString();

            string result = ExecuteRequest("updateMotifFrames", param);
            return true;
        }
    }
}
