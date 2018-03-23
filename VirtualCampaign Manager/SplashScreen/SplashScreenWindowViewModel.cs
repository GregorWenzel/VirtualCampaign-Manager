using ComponentPro.IO;
using ComponentPro.Net;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.MainHub;

namespace VirtualCampaign_Manager.SplashScreen
{
    public class SplashScreenWindowViewModel : INotifyPropertyChanged
    {
        public EventHandler<EventArgs> SuccessEvent;
        public EventHandler<EventArgs> FailureEvent;

        private Timer timer;

        public string ErrorString { get; set; } = "";

        public string VersionString
        {
            get
            {
                return "Version " + Settings.Version;
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
            { "Email server settings", "Settings.EmailServerLogin" },
            { "Ghostscript installation", "" },
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

        private int stepCounter = 0;

        public SplashScreenWindowViewModel()
        {
            timer = new Timer(100);
            timer.Elapsed += Timer_Elapsed;

            StatusString = "Checking " + statusStringArr[0, 0] + "...";
        }

        public void Start()
        {
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();
            stepCounter++;

            if (stepCounter < statusStringArr.Length / 2)
            {
                StatusString = statusStringArr[stepCounter, 0] + "...";
                CheckSetting(statusStringArr[stepCounter, 0], statusStringArr[stepCounter, 1]);
            }
            else
            {
                if (ErrorString.Length == 0)
                {
                    SuccessEvent?.Invoke(this, new EventArgs());
                }
                else
                {
                    //DEBUG: Ignore errors for development
                    SuccessEvent?.Invoke(this, new EventArgs());
                    return;

                    MessageBox.Show(ErrorString, "Errors in settings file detected!", MessageBoxButton.OK);
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.ini"));
                        Environment.Exit(-1);
                        //Application.Current.Shutdown();
                        return;
                    });
                }
            }
            timer.Start();
        }
        
        private void CheckSetting(string settingKey, string settingValue)
        {
            string url = "";
            LoginData loginData = null;

            switch (settingKey)
            {
                case "Base path": // Settings.LocalBasePath
                case "Production path": // Settings.LocalProductionPath
                case "FFmpeg path": // Settings.LocalFfmpegExePath
                case "Audio path": //Settings.LocalAudioPath
                case "Fusion plugin path": //Settings.LocalFusionPluginPath
                case "Deadline executable path": //Settings.LocalDeadlineExePath 
                    string extension = Path.GetExtension(settingValue);
                    if (extension.Length == 0)
                    {
                        if (!Directory.Exists(settingValue))
                        {
                            ErrorString += string.Format("- Directory '{0}' does not exist.\r\n", settingValue);
                        }
                    }
                    else
                    {
                        if (!File.Exists(settingValue))
                        {
                            ErrorString += string.Format("- File '{0}' does not exist.\r\n", settingValue);
                        }
                    }
                    break;
                case "Server url"://Settings.ServerUrl
                case "Services url": //Settings.ServicesUrl
                case "Film url": // Settings.ServerUrl
                    url = settingValue;
                    break;
                case "Remote user directory":// "Settings.FtpUserDirectoryLogin"
                    loginData = Settings.FtpUserDirectoryLogin;
                    break;
                case "Remote audio directory"://"Settings.FtpAudioDirectoryLogin"
                    loginData = Settings.FtpAudioDirectoryLogin;
                    break;
                case "Remote HASH directory"://"Settings.FtpHashDirectoryLogin"
                    loginData = Settings.FtpHashDirectoryLogin;
                    break;
                case "Remote product preview directory":// Settings.FtpProductPreviewDirectoryLogin
                    loginData = Settings.FtpProductPreviewDirectoryLogin;
                    break;
                case "SALTED string": //Settings.SALTED
                    if (settingValue.Length == 0)
                    {
                        ErrorString += "No SALTED pass defined.\r\n";
                    }
                    break;
                case "Email server settings": //"Settings.EmailServerLogin"
                    break;
                case "Ghostscript installation":
                    RegistryKey gsKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\GPL Ghostscript", false);
                    if (gsKey == null)
                    {
                        ErrorString += "Ghostscript not installed (required to process PDFs)";
                    }
                    break;
            }

            if (url.Length > 0)
            {
                if (!IsUrlReachable(url))
                {
                    ErrorString += string.Format("- URL '{0}' is not reachable.\r\n", url);
                }
            }
            else if (loginData != null)
            {
                string ftpLoginResult = IsSFtpServerAvailable(loginData);
                if (ftpLoginResult.Length > 0)
                {
                    ErrorString += ftpLoginResult;
                }
            }
        }

        private bool IsUrlReachable(string url)
        {
            if (!url.Contains("http://"))
            {
                url = "http://" + url;
            }
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 15000;
            request.Method = "HEAD"; // As per Lasse's comment
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (WebException ex)
            {
                return false;
            }
        }

        private string IsSFtpServerAvailable(LoginData loginData)
        {
            Sftp ftpClient = new Sftp();
            try
            {
                ftpClient.Connect(loginData.Url);
            }
            catch (Exception ex)
            {
                return string.Format("- Cannot connect to ftp server '{0}'", loginData.Url);
            }

            if (!ftpClient.IsConnected)
            {
                return string.Format("- Cannot connect to ftp server '{0}'", loginData.Url);
            }

            try
            {
                ftpClient.Authenticate(loginData.Username, loginData.Password);
            }
            catch (Exception ex)
            { 
                ftpClient.Disconnect();
                return string.Format("- Invalid credentials for server '{0}', username '{1}'", loginData.Url, loginData.Username);
            }
            ftpClient.Disconnect();
            return "";
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
