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
                    newProduction.SuccessEvent += OnProductionSuccess;
                    productionList.Add(newProduction);
                    foreach (Job newJob in newProduction.JobList)
                    {
                        jobList.Add(newJob);
                    }
                    newProduction.StartWorker();
                }
            }
        }

        private void OnProductionSuccess(object sender, EventArgs ea)
        {
            Production production = sender as Production;
            production.SuccessEvent -= OnProductionSuccess;
            if (productionList.Any(item => item.ID == production.ID))
            {
                productionList.Remove(production);

                foreach (Job job in production.JobList)
                {
                    if (jobList.Any(item => item.ID == job.ID))
                    {
                        jobList.Remove(job);
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
