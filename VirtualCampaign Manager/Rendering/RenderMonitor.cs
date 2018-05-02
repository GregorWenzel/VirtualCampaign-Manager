using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Encoding;

namespace VirtualCampaign_Manager.Rendering
{
    public class RenderMonitor : EventFireBase
    {
        private Job job;
        private Timer timer;
        private MongoClient client;
        private IMongoDatabase database;

        public RenderMonitor(Job job)
        {
            this.job = job;
            timer = new Timer(2000);
            timer.Elapsed += Timer_Elapsed;
        }

        public void Start()
        {
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();

            client = new MongoClient(@"mongodb://SERVER-A:27080");
            database = client.GetDatabase("deadline8db");
            var jobCollection = database.GetCollection<BsonDocument>("Jobs");
            var taskCollection = database.GetCollection<BsonDocument>("JobTasks");
            BsonDocument jobDoc = null;

            try
            {
                string jobName = string.Format("{0} [{1}]", job.ID, job.RenderStartTime);
                jobDoc = jobCollection.Find(Builders<BsonDocument>.Filter.AnyEq("Props.Name", jobName)).SortByDescending(bson => bson["LastWriteTime"]).First();

                int status = Convert.ToInt32(jobDoc["Stat"]);
                double tasks = Convert.ToDouble(jobDoc["Props"]["Tasks"]);
                double completedTasks = Convert.ToDouble(jobDoc["CompletedChunks"]);
                string mongoJobId = Convert.ToString(jobDoc["_id"]);

                var taskResults = taskCollection.Find(Builders<BsonDocument>.Filter.Eq("JobID", mongoJobId)).SortByDescending(bson => bson["TaskID"]);
                var taskList = taskResults.ToList();

                bool createNewTasks = (job.RenderChunkStatusList.Count == 0);

                //get all render chunk statuses
                foreach (BsonDocument taskDoc in taskList)
                {
                    int taskID = Convert.ToInt32(taskDoc["TaskID"]);

                    RenderChunkStatus currentChunk;

                    if (createNewTasks)
                    {
                        currentChunk = new RenderChunkStatus();
                        currentChunk.StartIndex = taskID;
                        currentChunk.Job = job;                       
                    }
                    else
                    {
                        currentChunk = job.RenderChunkStatusList.First(item => item.StartIndex == taskID);
                    }


                    currentChunk.SlaveName = Convert.ToString(taskDoc["Slave"]);
                    currentChunk.Status = Math.Max(currentChunk.Status, Convert.ToInt32(taskDoc["Stat"]));
                    string[] frameString = Convert.ToString(taskDoc["Frames"]).Split(new char[] { '-' });
                    currentChunk.StartFrame = Convert.ToInt32(frameString[0]);
                    if (frameString.Length > 1)
                    {
                        currentChunk.EndFrame = Convert.ToInt32(frameString[1]);
                    }
                    else
                    {
                        currentChunk.EndFrame = currentChunk.StartFrame;
                    }

                    var slaveCollection = database.GetCollection<BsonDocument>("SlaveInfo");
                    BsonDocument slaveInfo = slaveCollection.Find(Builders<BsonDocument>.Filter.AnyEq("_id", currentChunk.SlaveName.ToLower())).First();

                    currentChunk.Processors = Convert.ToInt32(slaveInfo["Procs"]);
                    currentChunk.ProcessorSpeed = Convert.ToInt32(slaveInfo["ProcSpd"]);

                    Dictionary<string, object> taskVal = (Dictionary<string, object>)BsonTypeMapper.MapToDotNetValue(taskDoc);

                    currentChunk.RenderStartDate = (DateTime)taskVal["StartRen"];
                    currentChunk.RenderEndDate = (DateTime)taskVal["Comp"];
                    currentChunk.Status = Math.Max(currentChunk.Status, Convert.ToInt32(taskDoc["Stat"]));

                    if (createNewTasks)
                    {
                        job.RenderChunkStatusList.Add(currentChunk);
                    }
                }

                if (createNewTasks)
                {
                    job.RenderChunkStatusList = job.RenderChunkStatusList.OrderBy(item => item.StartIndex).ToList();
                }

                RenderChunkStatus largestChunk = GetLargestChunkRendered(job);
                if (largestChunk != null)
                {
                    RenderChunkEncoder.EncodeLargestChunk(largestChunk);
                }

                int finishedChunks = job.RenderChunkStatusList.Count(item => item.Status == 7);

                job.Progress = (float)Math.Round(100f * finishedChunks / job.RenderChunkStatusList.Count);

                if (finishedChunks == taskList.Count)
                {
                    if (RenderChunkEncoder.MergeChunks(job) == true)
                    {
                        CompleteJob();
                    }
                    else
                    {
                        FireFailureEvent(job.ErrorStatus);
                    }
                }
                else
                {
                    if (status == 4)
                    {
                        FailJob();
                    }
                    else
                    {
                        timer.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                timer.Start();
            }
        }

        private RenderChunkStatus GetLargestChunkRendered(Job job)
        {
            RenderChunkStatus result = null;
            List<RenderChunkStatus> renderedChunkList = job.RenderChunkStatusList.Where(item => item.Status == 5).ToList();
            int lastIndex;

            if (renderedChunkList.Count > 0)
            {
                result = renderedChunkList[0];
                result.EndIndex = result.StartIndex;
                lastIndex = result.StartIndex;
            }
            else
            {
                return null;
            }

            if (renderedChunkList.Count > 1)
            {
                for (int i = 1; i < renderedChunkList.Count; i++)
                {
                    RenderChunkStatus thisChunk = renderedChunkList[i];

                    if (thisChunk.StartIndex == lastIndex + 1)
                    {
                        result.EndFrame = thisChunk.EndFrame;
                        result.EndIndex = thisChunk.StartIndex;
                    }
                    else
                    {
                        result = thisChunk;
                        result.EndIndex = result.StartIndex;
                    }

                    lastIndex = thisChunk.StartIndex;
                }
            }

            return result;
        }

        private void CompleteJob()
        {
            timer.Stop();
            FireSuccessEvent();
        }

        private void FailJob()
        {
            timer.Stop();
            FireFailureEvent(JobErrorStatus.JES_DEADLINE_RENDER_JOB);
        }
    }
}
