using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Helpers
{
    enum LogType
    {
        Job,
        Production
    };

    public class Logger
    {
        Job job;
        Production production;
        LogType LogType;

        public Logger(Job newJob)
        {
            job = newJob;
            LogType = LogType.Job;
        }

        public Logger(Production newProduction)
        {
            production = newProduction;
            LogType = LogType.Production;
        }

        public void Write(string logText)
        {
            switch (this.LogType)
            {
                case LogType.Job:
                    WriteToJobLog(job, logText);
                    break;
                case LogType.Production:
                    WriteToProductionLog(production, logText);
                    break;
            }
        }

        public void WriteLine(string logText)
        {
            switch (this.LogType)
            {
                case LogType.Job:
                    WriteLineToJobLog(job, logText);
                    break;
                case LogType.Production:
                    WriteLineToProductionLog(production, logText);
                    break;
            }
        }

        private void WriteToJobLog(Job job, string logText)
        {
            Console.Write(logText);

            string targetPath = Path.Combine(JobPathHelper.GetLocalJobDirectory(job), job.ID + "_logfile.txt");

            DateTime time = DateTime.Now;
            string dateString = String.Format("[{0:dd/mm/yyyy HH:mm:ss}] ", time);

            try
            {
                StreamWriter writer = new StreamWriter(targetPath, true);
                writer.Write(dateString + logText);
                writer.Close();
            }
            catch
            {
            }
        }

        private void WriteLineToJobLog(Job job, string logText)
        {
            WriteToJobLog(job, logText + "\r\n");
        }

        public void WriteToProductionLog(Production production, string logText)
        {
            string targetPath = Path.Combine(ProductionPathHelper.GetLocalProductionDirectory(production), production.ID + "_logfile.txt");

            if (!File.Exists(targetPath)) return;

            DateTime time = DateTime.Now;
            string dateString = String.Format("[{0:dd/mm/yyyy HH:mm:ss}] ", time);

            StreamWriter writer = new StreamWriter(targetPath, true);

            writer.Write(dateString + logText);
            writer.Close();
        }

        public void WriteLineToProductionLog(Production production, string logText)
        {
            WriteToProductionLog(production, logText + "\r\n");
        }

    }
}