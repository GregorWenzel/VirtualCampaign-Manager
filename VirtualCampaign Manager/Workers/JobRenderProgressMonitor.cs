using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Workers
{
    class JobRenderProgressMonitor
    {
        private Job currentJob;
        private Timer timer;
        private MongoClient client;
        private IMongoDatabase database;

        public JobRenderProgressMonitor(Job job)
        {
            currentJob = job;
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
            var collection = database.GetCollection<BsonDocument>("Jobs");
            BsonDocument jobDoc = null;

            try
            {
                jobDoc = collection.Find(Builders<BsonDocument>.Filter.AnyEq("Props.Name", string.Format("{0}.comp", currentJob.ID))).SortByDescending(bson => bson["LastWriteTime"]).First();

                int status = Convert.ToInt32(jobDoc["Stat"]);
                double tasks = Convert.ToDouble(jobDoc["Props"]["Tasks"]);
                double completedTasks = Convert.ToDouble(jobDoc["CompletedChunks"]);

                currentJob.Progress = (float) Math.Round(100 * completedTasks / tasks);

                switch (status)
                {
                    case 3:
                        CompleteJob();
                        break;
                    case 4:
                        FailJob();
                        break;
                    default:
                        timer.Start();
                        break;
                }
            }
            catch (Exception ex)
            {
                timer.Start();
            }
        }

        private void CompleteJob()
        {
            timer.Stop();
            currentJob.Status = JobStatus.JS_SEND_ENCODE_JOB;
        }

        private void FailJob()
        {
            timer.Stop();
            currentJob.ErrorStatus = JobErrorStatus.JES_DEADLINE_RENDER_JOB;
        }
    }
}