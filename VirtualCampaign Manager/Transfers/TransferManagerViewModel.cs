using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCampaign_Manager.Transfers
{
    public class TransferManagerViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<TransferPacket> DownloadList
        {
            get
            {
                return DownloadManager.Instance.TransferPacketList;
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChangedEvent(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
                PropertyChanged(null, e);
            }
        }
    }
}
