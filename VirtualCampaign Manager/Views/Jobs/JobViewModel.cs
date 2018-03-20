using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Views.Jobs
{
    public class JobViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Job> JobList
        {
            get { return GlobalValues.JobList; }
            set {
                if (value == GlobalValues.JobList) return;

                GlobalValues.JobList = value;
                RaisePropertyChangedEvent("JobList");
            }
        }

        public JobViewModel()
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
