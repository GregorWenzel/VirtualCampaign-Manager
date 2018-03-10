using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Transfers
{
    public enum TransferType
    {
        RemoteDownload,
        RemoteUpload,
        LocalCopy
    }

    public class TransferPacket : INotifyPropertyChanged
    {
        public EventHandler<EventArgs> SuccessEvent;
        public EventHandler<EventArgs> FailureEvent;

        public Object Task { get; set; } //unique sftp transfer task id
        public Object Parent { get; set; }

        private string sourcePath;

        public string SourcePath
        {
            get { return sourcePath; }
            set {
                if (value == sourcePath) return;
                sourcePath = value;
                RaisePropertyChangedEvent("SourcePath");
                RaisePropertyChangedEvent("Filename");
            }
        }

        public string TargetPath { get; set; }
        public TransferType Type { get; set; }
        public LoginData LoginData { get; set; }
        public Exception TransferExcetpion { get; set; }
        public int TransferErrorCounter { get; set; } = 0;
        public string Filename
        {
            get
            {
                return Path.GetFileName(TargetPath);
            }
        }

        private int progress;

        public int Progress
        {
            get { return progress; }
            set {
                if (value == progress) return;
                progress = value;
                RaisePropertyChangedEvent("Progress");
            }
        }

        private string status;

        public string Status
        {
            get
            {
                if (HasError)
                {
                    return "Error";
                }
                else if (IsInTransit)
                {
                    return "Transfer";
                }
                else if (IsSuccessful)
                {
                    return "Done";
                }
                else
                {
                    return "Idle";
                }
            }            
        }

        private bool hasError;

        public bool HasError
        {
            get { return hasError; }
            set {
                if (value == HasError) return;

                hasError = value;
                RaisePropertyChangedEvent("Status");
            }
        }

        private bool isSuccessful;

        public bool IsSuccessful
        {
            get { return isSuccessful; }
            set
            {
                if (value == isSuccessful) return;

                isSuccessful = value;
                RaisePropertyChangedEvent("Status");
            }
        }

        private bool isInTransit;

        public bool IsInTransit
        {
            get { return isInTransit; }
            set
            {
                if (value == isInTransit) return;

                isInTransit = value;
                RaisePropertyChangedEvent("Status");
            }
        }



        //transfers a motif from remote user account to local filesystem
        public TransferPacket(Job Job, Motif Motif)
        {
            Parent = Job;
            SourcePath = Settings.FtpUserDirectoryLogin.SubdirectoryPath + "/" + Job.AccountID + "/motifs/" + Motif.DownloadName;
            TargetPath = Path.Combine(Job.Production.ProductionDirectory, "motifs", Motif.DownloadName);
            Type = TransferType.RemoteDownload;
            LoginData = Settings.FtpUserDirectoryLogin;
        }

        //uploads a preview pricture for an animated motif from local filesystem to remote user account
        public TransferPacket(int AccountID, Motif Motif)
        {
            Parent = AccountID;
            SourcePath = "";
            TargetPath = Settings.FtpUserDirectoryLogin.SubdirectoryPath + "/" + AccountID + "/motifs/" + Motif.DownloadName;
            Type = TransferType.RemoteUpload;
            LoginData = Settings.FtpUserDirectoryLogin;
        }

        //transfers a film from local file system to remote user account
        public TransferPacket(Film Film, int AccountID, FilmOutputFormat OutputFormat)
        {
            Parent = Film;
            SourcePath = "";
            //TargetPath = Settings.FtpUserDirectoryLogin.SubdirectoryPath + "/" + AccountID + "/motifs/" + Motif.DownloadName;
            Type = TransferType.RemoteUpload;
            LoginData = Settings.FtpUserDirectoryLogin;
        }

        //transfers a user's custom audio file to local file system
        public TransferPacket(AudioData AudioData)
        {
            Parent = AudioData;
            SourcePath = AudioData.Filename;
            TargetPath = AudioData.AudioPath;
            Type = TransferType.RemoteDownload;
            LoginData = Settings.FtpAudioDirectoryLogin;
        }

        public void FireSuccessEvent()
        {
            SuccessEvent?.Invoke(this, new EventArgs());
        }

        public void FireFailureEvent()
        {
            FailureEvent?.Invoke(this, new EventArgs());
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
