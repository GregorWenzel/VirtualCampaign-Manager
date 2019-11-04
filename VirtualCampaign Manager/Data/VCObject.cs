using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Logging;

namespace VirtualCampaign_Manager.Data
{
    public class VCObject: INotifyPropertyChanged
    {
        private int id;

        public int ID
        {
            get { return id; }
            set { id = value; }
        }

        private DateTime creationDate;

        public DateTime CreationDate
        {
            get { return creationDate; }
            set { creationDate = value; }
        }

        public long RenderStartTime { get; set; }

        private DateTime updateDate;

        public DateTime UpdateDate
        {
            get { return updateDate; }
            set
            {
                if (value > updateDate)
                    updateDate = value;
            }
        }

        private Logger logger;

        public string Log
        {
            get
            {
                return logger.Log;
            }
        }

        public VCObject()
        {
            logger = new Logger(this);
        }
        
        public void LogText(string text)
        {
            logger.LogText(text);
            RaisePropertyChangedEvent("Log");
        }

        public void ClearLog()
        {
            logger.ClearLog();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChangedEvent(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
                PropertyChanged(this, e);
            }
        }
    }
}