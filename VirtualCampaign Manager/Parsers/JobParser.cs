using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Parsers
{
    public static class JobParser
    {
        public static List<Job> ParseList(Production Production, List<Dictionary<string, string>> JobDictList)
        {
            List<Job> result = new List<Job>();

            foreach (Dictionary<string, string> jobDict in JobDictList)
            {
                Job newJob = Parse(Production, jobDict);
                if (newJob != null && result.Exists(item => item.ID == newJob.ID) == false)
                {
                    result.Add(newJob);
                }
            }

            return result;
        }

        public static Job Parse(Production Production, Dictionary<string, string> JobDict)
        {
            Job result = new Job();

            //make sure this job won't process changes while being updated
            result.IsActive = false;

            result.Production = Production;
            result.ID = Convert.ToInt32(JobDict["ID"]);
            result.ErrorStatus = (JobErrorStatus)Enum.ToObject(typeof(JobErrorStatus), Convert.ToInt32(JobDict["ErrorCode"]));
            result.Position = Convert.ToInt32(JobDict["Position"]);
            result.ProductID = Convert.ToInt32(JobDict["ProductID"]);
            //DEBUG::: InFrame and OutFrame MUST be available for EVERY product, even indicatives and abdicatives!!
            result.InFrame = Convert.ToInt32(JobDict["InFrame"]);
            result.OutFrame = Convert.ToInt32(JobDict["OutFrame"]);
            result.PreviewFrame = Convert.ToInt32(JobDict["ProductFrames"]);
            result.AccountID = Convert.ToInt32(JobDict["AccountID"]);
            result.IsDicative = (Convert.ToInt32(JobDict["IsDicative"]) == 1);
            result.MotifList = new List<Motif>();

            //required fields that are not provided by the database for indicatives and abdicatives
            if (result.IsDicative == false)
            {
                result.MasterProductID = Convert.ToInt32(JobDict["MasterProductID"]);
                result.CanReformat = (Convert.ToInt32(JobDict["CanReformat"]) == 1);

                //product preview clips do not receive motifs
                if (result.IsPreview == false)
                {
                    result.MotifList.Add(new Motif(Convert.ToInt32(JobDict["ContentID"]), Convert.ToString(JobDict["ContentType"]), Convert.ToInt32(JobDict["ContentPosition"]), Convert.ToString(JobDict["ContentExtension"]), Convert.ToString(JobDict["ContentLoaderName"]), Convert.ToString(JobDict["ContentText"])));

                    JobStatus currentStatus = (JobStatus)Enum.ToObject(typeof(JobStatus), Convert.ToInt32(JobDict["Status"]));
                    
                    //Only accept new job status as such if it isn't currently being rendered and a render id has been saved before,
                    //otherwise: set job to JS_GET_JOB_ID in order to read job id from deadline
                    //and continue from there
                    if (JobDict["RenderID"] != null && JobDict["RenderID"].Length > 0)
                    { 
                        result.RenderJobID = Convert.ToString(JobDict["RenderID"]);
                        if (currentStatus == JobStatus.JS_RENDER_JOB)
                        {
                            result.Status = JobStatus.JS_GET_JOB_ID;
                        }
                        else
                        {
                            result.Status = currentStatus;
                        }
                    }                       
                    
                    //reactivate processing loop
                    result.IsActive = true;                
                }
            }
            else
            {
                result.MasterProductID = -1;
                result.Status = JobStatus.JS_DONE;
            }

            return result;
        }
    }
}
