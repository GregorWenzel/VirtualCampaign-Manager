using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using VirtualCampaign_Manager.MainHub;
using VirtualCampaign_Manager.SplashScreen;

namespace VirtualCampaign_Manager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        SplashScreenWindow splash;
        MainHubWindow mainHub;

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            GlobalValues.ReadOutputFormats();

            //DEBUG: Skip Splash
            //splash = new SplashScreenWindow();
            //(splash.DataContext as SplashScreenWindowViewModel).SuccessEvent += OnSplashSucccess;
            //splash.ShowDialog();

            OnSplashSucccess(this, null);
            base.OnStartup(e);
        }      

        private void OnSplashSucccess(object sender, EventArgs ea)
        {
            Dispatcher.Invoke((Action)delegate
            {
                if (splash != null)
                    splash.Visibility = Visibility.Hidden;

                mainHub = new MainHubWindow();
                mainHub.ShowDialog();
            });
        }

    }
}
