using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Telerik.Windows.Controls;

namespace VirtualCampaign_Manager.MainHub
{
    /// <summary>
    /// Interaction logic for MainHubWindow.xaml
    /// </summary>
    public partial class MainHubWindow
    {
        public MainHubWindow()
        {
            InitializeComponent();
            this.Loaded += MainHubWindow_Loaded;
        }

        private void MainHubWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.ParentOfType<Window>().ShowInTaskbar = true;
            this.Header = Settings.ServerUrl + " (" + GlobalValues.MachineName + ")";
        }
    }
}
