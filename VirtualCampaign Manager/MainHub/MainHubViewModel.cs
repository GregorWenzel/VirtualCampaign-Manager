using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Repositories;

namespace VirtualCampaign_Manager.MainHub
{
    public class MainHubViewModel : INotifyPropertyChanged
    {
        private Timer timer;

        private ObservableCollection<Job> jobList;

        public ObservableCollection<Job> JobList
        {
            get { return jobList; }
            set { jobList = value; }
        }

        private List<Production> productionList;

        public MainHubViewModel()
        {
            productionList = new List<Production>();

            timer = new Timer();
            timer.Interval = Settings.MainUpdateInterval;
            timer.Elapsed += Timer_Elapsed;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            List<Production> productionBuffer = ProductionRepository.ReadProductions();
            
            foreach (Production newProduction in productionBuffer)
            {
                if (productionList.Any(item => item.ID == newProduction.ID) == false)
                {
                    productionList.Add(newProduction);
                    foreach (Job newJob in newProduction.JobList)
                    {
                        jobList.Add(newJob);
                    }
                }
            }
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
