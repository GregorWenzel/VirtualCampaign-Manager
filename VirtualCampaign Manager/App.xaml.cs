using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using VirtualCampaign_Manager.MainHub;

namespace VirtualCampaign_Manager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            GlobalValues.ReadOutputFormats();

            MainHubWindow mainHubWindow = new MainHubWindow();

            mainHubWindow.Show();
            base.OnStartup(e);
        }
    }
}
