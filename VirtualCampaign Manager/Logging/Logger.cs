using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Helpers;

namespace VirtualCampaign_Manager.Logging
{
    public class Logger : INotifyPropertyChanged
    {
        VCObject parent;
        List<string> logLines = new List<string>();

        private string log;

        public string Log
        {
            get { return string.Join("\r\n", logLines); }
        }

        public Logger(VCObject Parent)
        {
            this.parent = Parent;
        }

        public void ClearLog()
        {
            logLines.Clear();
        }

        //log text to visual log, console and log file
        public void LogText(string text)
        {
            DateTime time = DateTime.Now;
            string logText = logText = string.Format("[{0}]: {1}", time, text);

            logLines.Add(logText);

            Console.WriteLine(logText);

            string logfilePath = null;

            if (parent is Job)
            {
                logfilePath = JobPathHelper.GetLogFilePath(parent as Job);
            }
            else if (parent is Production)
            {
                logfilePath = ProductionPathHelper.GetLogFilePath(parent as Production);
            }
            else if (parent is Motif)
            {
                logfilePath = JobPathHelper.GetLogFilePath((parent as Motif).Job);
            }

            if (logfilePath == null) return;

            IOHelper.CreateDirectory(System.IO.Path.GetDirectoryName(logfilePath));

            if (System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(logfilePath)))
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                    System.IO.File.AppendAllText(logfilePath, string.Format("{0}{1}", logText, Environment.NewLine))
                ));
            }

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
