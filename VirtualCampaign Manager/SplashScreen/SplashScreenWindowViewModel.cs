using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using VirtualCampaign_Manager.MainHub;

namespace VirtualCampaign_Manager.SplashScreen
{
    public class SplashScreenWindowViewModel : INotifyPropertyChanged
    {
        public EventHandler<EventArgs> SuccessEvent;
        public EventHandler<EventArgs> FailureEvent;

        public string VersionString
        {
            get
            {
                return Settings.Version;
            }
        }

        string[,] statusStringArr = new string[,]
        {
            { "Checking prerequisits", "" },
            { "Base path", Settings.LocalBasePath },
            { "Production path", Settings.LocalProductionPath },
            { "FFmpeg path", Settings.LocalFfmpegExePath },
            { "Audio path", Settings.LocalAudioPath },
            { "Fusion plugin path", Settings.LocalFusionPluginPath },
            { "Deadline executable path", Settings.LocalDeadlineExePath },
            { "Server url", Settings.ServerUrl },
            { "Services url", Settings.ServicesUrl },
            { "Film url", Settings.ServerUrl },
            { "Remote user directory", "Settings.FtpUserDirectoryLogin" },
            { "Remote audio directory", "Settings.FtpAudioDirectoryLogin" },
            { "Remote HASH directory", "Settings.FtpHashDirectoryLogin" },
            { "Remote product preview directory", "Settings.FtpProductPreviewDirectoryLogin" },
            { "SALTED string", Settings.SALTED },
            { "Email server settings", "Settings.EmailServerLogin" }
        };

        private string statusString;

        public string StatusString
        {
            get { return statusString; }
            set {
                statusString = value;
                RaisePropertyChangedEvent("StatusString");
            }
        }

        private Timer timer;
        private int stepCounter = 0;

        public SplashScreenWindowViewModel()
        {
            StatusString = "Checking " + statusStringArr[0, 0] + "...";

            timer = new Timer(100);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            stepCounter += 1;

            if (stepCounter < statusStringArr.Length/2)
            {
                StatusString = statusStringArr[stepCounter, 0] + "...";
            }
            else
            {
                SuccessEvent?.Invoke(this, new EventArgs());
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChangedEvent(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
                PropertyChanged(this, e);
            }
        }
    }
}
