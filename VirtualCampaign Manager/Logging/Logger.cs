using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Logging
{
    public class Logger : INotifyPropertyChanged
    {
        Production production;
        Job job;
        List<string> logLines = new List<string>();

        private string log;

        public string Log
        {
            get { return string.Join("\r\n", logLines); }
        }

        public Logger(Production production)
        {
            this.production = production;
        }

        public Logger(Job job)
        {
            this.job = job;
        }

        public void LogText(string text)
        {
            DateTime time = DateTime.Now;
            string logText;
            if (job != null)
            {
                logText = string.Format("Job {0} - {1}: {2}", job.ID, time, text);
            }
            else
            {
                logText = string.Format("Job {0} - {1}: {2}", production.ID, time, text);
            }

            Console.WriteLine(logText);
            RaisePropertyChangedEvent("Log");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChangedEvent(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
                PropertyChanged(this, e);
            }
        }
    }
}
