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
using System.Windows.Shapes;
using VirtualCampaign_Manager.MainHub;

namespace VirtualCampaign_Manager.SplashScreen
{
    /// <summary>
    /// Interaction logic for SplashScreenWindow.xaml
    /// </summary>
    public partial class SplashScreenWindow : Window
    {
        SplashScreenWindowViewModel viewModel;

        public SplashScreenWindow()
        {
            bool loaded = false;
            InitializeComponent();

            viewModel = new SplashScreenWindowViewModel();
            this.DataContext = viewModel;
            this.Loaded += SplashScreenWindow_Loaded;
        }

        private void SplashScreenWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PresentationSource presentationSource = PresentationSource.FromVisual((Visual)sender);

            // Subscribe to PresentationSource's ContentRendered event
            presentationSource.ContentRendered += TestUserControl_ContentRendered;
        }

        void TestUserControl_ContentRendered(object sender, EventArgs e)
        {
            // Don't forget to unsubscribe from the event
            ((PresentationSource)sender).ContentRendered -= TestUserControl_ContentRendered;

            viewModel.Start();
        }
    }
}
