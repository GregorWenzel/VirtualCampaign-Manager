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
        private Timer productionsTimer;
        private Timer animatedMotifsTimer;

        private ObservableCollection<AnimatedMotif> animatedMotifList;

        public ObservableCollection<AnimatedMotif> AnimatedMotifList
        {
            get { return animatedMotifList; }
            set { animatedMotifList = value; }
        }
        
        private List<Production> productionList;

        public MainHubViewModel()
        {
            productionList = new List<Production>();

            productionsTimer = new Timer();
            productionsTimer.Interval = Settings.MainUpdateInterval;
            productionsTimer.Elapsed += Timer_Elapsed;

            animatedMotifsTimer = new Timer();
            animatedMotifsTimer.Interval = Settings.MotifUpdateInterval;
            animatedMotifsTimer.Elapsed += AnimatedMotifsTimer_Elapsed;
        }

        public void Start()
        {
            productionsTimer.Start();
        }

        private void AnimatedMotifsTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            List<AnimatedMotif> animatedMotifListBuffer = ProductionRepository.ReadAnimatedMotifs();

            foreach (AnimatedMotif newAnimatedMotif in animatedMotifListBuffer)
            {
                if (animatedMotifList.Any(item => item.ID == newAnimatedMotif.ID) == false)
                {
                    animatedMotifList.Add(newAnimatedMotif);
                    newAnimatedMotif.StartWorker();
                }
            }
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
                        GlobalValues.JobList.Add(newJob);
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
                    if (GlobalValues.JobList.Any(item => item.ID == job.ID))
                    {
                        GlobalValues.JobList.Remove(job);
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
