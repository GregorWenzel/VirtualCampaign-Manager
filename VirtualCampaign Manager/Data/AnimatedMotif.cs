using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCampaign_Manager.Data
{
    public class AnimatedMotif : VCObject, INotifyPropertyChanged
    {
        public EventHandler<EventArgs> SuccessEvent;
        public EventHandler<ResultEventArgs> FailureEvent;

        private int accountID;

        public int AccountID
        {
            get { return accountID; }
            set { accountID = value; }
        }

        private string extension;

        public string Extension
        {
            get { return extension; }
            set { extension = value; }
        }

        private int frameCount;

        public int FrameCount
        {
            get { return frameCount; }
            set { frameCount = value; }
        }

        public void StartWorker()
        {

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
