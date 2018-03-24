using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Logging;

namespace VirtualCampaign_Manager.Views.Jobs
{
    public class JobViewModel : INotifyPropertyChanged
    {
        public ICommand GridDoubleClickCommand { get; set; }        

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
            GridDoubleClickCommand = new DelegateCommand(OnGridDoubleClicked);

        }

        private void OnGridDoubleClicked(object obj)
        {
            MouseButtonEventArgs eventArgs = (obj as MouseButtonEventArgs);
            var senderEement = eventArgs.OriginalSource as FrameworkElement;
            var parentRow = senderEement.ParentOfType<GridViewRow>();
            var parentRowGroup = senderEement.ParentOfType<GridViewGroupRow>();

            if (parentRow == null && parentRowGroup != null && parentRowGroup.ItemsSource != null)
            {
                //production double clicked
                Production selectedProduction = ((parentRowGroup.ItemsSource as System.Collections.ObjectModel.ReadOnlyObservableCollection<object>)[0] as Job).Production;
                ShowLog(selectedProduction);
            }
            else if (parentRowGroup.ItemsSource != null)
            {
                //job double clicked
                Job selectedJob = (parentRowGroup.ItemsSource as System.Collections.ObjectModel.ReadOnlyObservableCollection<object>)[0] as Job;
                ShowLog(selectedJob);
            }
        }

        private void ShowLog(VCObject selectedObject)
        {            
            LogView logView = (GlobalValues.LogWindow.Content as Grid).FindChildByType<LogView>();
            if (selectedObject is Job)
            {
                GlobalValues.LogWindow.Header = "Job " + selectedObject.ID;
                logView.DataContext = selectedObject as Job;
            }
            else
            {
                GlobalValues.LogWindow.Header = "Production " + selectedObject.ID;
                logView.DataContext = selectedObject as Production;
            }

            GlobalValues.LogWindow.Show();
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
