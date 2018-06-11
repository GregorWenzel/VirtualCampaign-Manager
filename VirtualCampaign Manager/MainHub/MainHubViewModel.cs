using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Repositories;
using VirtualCampaign_Manager.Views.History;

namespace VirtualCampaign_Manager.MainHub
{
    public class MainHubViewModel : INotifyPropertyChanged
    {
        private Timer productionsTimer;
        private Timer animatedMotifsTimer;

        private HistoryWindow historyWindow;
        private HistoryWindowViewModel historyWindowViewModel;

        public ICommand ShowHistoryCommand { get; set; }

        public string LocalMachineName
        {
            get
            {
                return GlobalValues.LocalMachineName;
            }
        }

        public string ActiveMachineName
        {
            get
            {
                return GlobalValues.ActiveMachineName;
            }
        }

        public SolidColorBrush MachineNameColor
        {
            get
            {
                if (GlobalValues.IsActive == 1)
                {
                    return Brushes.Green;
                }
                else
                {
                    return Brushes.Black;
                }
            }
        }

        private ObservableCollection<AnimatedMotif> animatedMotifList;

        public ObservableCollection<AnimatedMotif> AnimatedMotifList
        {
            get { return animatedMotifList; }
            set { animatedMotifList = value; }
        }

        private DateTime currentTime = DateTime.Now;
        public DateTime CurrentTime
        {
            get
            {
                return currentTime;
            }
            set
            {
                currentTime = value;
                RaisePropertyChangedEvent("CurrentTime");
            }
        }

        private DateTime lastUpdateTime;

        public DateTime LastUpdateTime
        {
            get { return lastUpdateTime; }
            set {
                lastUpdateTime = value;
                RaisePropertyChangedEvent("LastUpdateTime");
            }
        }

        public MainHubViewModel()
        {
            Timer clockTimer = new Timer();
            clockTimer.Interval = 1000;
            clockTimer.Elapsed += ClockTimer_Elapsed;
            clockTimer.Start();

            productionsTimer = new Timer();
            productionsTimer.Interval = Settings.MainUpdateInterval;
            productionsTimer.Elapsed += Timer_Elapsed;

            animatedMotifsTimer = new Timer();
            animatedMotifsTimer.Interval = Settings.MotifUpdateInterval;
            animatedMotifsTimer.Elapsed += AnimatedMotifsTimer_Elapsed;

            ShowHistoryCommand = new DelegateCommand(OnShowHistory);
        }

        private void OnShowHistory(object obj)
        {
            if (historyWindow == null)
            {
                historyWindow = new HistoryWindow();
                historyWindowViewModel = new HistoryWindowViewModel();
                historyWindow.DataContext = historyWindowViewModel;
            }

            historyWindow.Show();
        }

        private void ClockTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            CurrentTime = DateTime.Now;
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
            productionsTimer.Stop();

            GlobalValues.ActiveMachineName = ProductionRepository.ManageHeartbeat();
            RaisePropertyChangedEvent("ActiveMachineName");
            RaisePropertyChangedEvent("MachineNameColor");

            if (GlobalValues.ActiveMachineName == GlobalValues.LocalMachineName)
            {              
                GlobalValues.IsActive = 1;

                ProductionRepository.ReadProductions();

                foreach (Production newProduction in GlobalValues.ProductionList)
                {
                    if (newProduction.HasStarted) continue;

                    newProduction.SuccessEvent += OnProductionSuccess;
                    foreach (Job newJob in newProduction.JobList)
                    {
                        if (GlobalValues.JobList.Any(item => item.ID == newJob.ID) == false)
                        {
                            GlobalValues.JobList.Add(newJob);
                        }
                    }
                    newProduction.StartWorker();
                }

                LastUpdateTime = DateTime.Now;
            }
            else
            {
                GlobalValues.IsActive = 0;

                //clear productions after turning off the current manager 
                GlobalValues.ProductionList.Clear();
                
            }

            productionsTimer.Start();
        }

        private void OnProductionSuccess(object sender, EventArgs ea)
        {
            Production production = sender as Production;
            production.SuccessEvent -= OnProductionSuccess;

            if (GlobalValues.ProductionList.Any(item => item.ID == production.ID))
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    GlobalValues.ProductionHistoryList.Add(new ProductionHistoryItem(production));
                });

                GlobalValues.ProductionList.Remove(production);

                for (int i=0; i<production.JobList.Count; i++)
                {
                    Job job = production.JobList[i];
                    if (GlobalValues.JobList.Any(item => item.ID == job.ID))
                    {
                        GlobalValues.JobList.Remove(job);
                        job = null;
                    }
                }

                production = null;
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
