using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VirtualCampaign_Manager.MainHub
{
    /// <summary>
    /// Interaktionslogik für MainHubView.xaml
    /// </summary>
    public partial class MainHubView : UserControl
    {
        public MainHubView()
        {
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            (this.DataContext as MainHubViewModel).Start();
            base.OnInitialized(e);
        }

    }
}
