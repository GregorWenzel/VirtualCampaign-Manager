using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Views.History
{
    public class HistoryWindowViewModel : INotifyPropertyChanged
    {

        private ObservableCollection<Production> productionHistoryList = new ObservableCollection<Production>();

        public ObservableCollection<Production> ProductionHistoryList
        {
            get { return GlobalValues.ProductionHistoryList; }
            set
            {
                if (value == GlobalValues.ProductionHistoryList) return;
                GlobalValues.ProductionHistoryList = value;
                RaisePropertyChangedEvent("ProductionHistoryList");
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
