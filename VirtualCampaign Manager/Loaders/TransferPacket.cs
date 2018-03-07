using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Loaders
{
    public enum TransferType
    {
        RemoteDownload,
        RemoteUpload,
        LocalCopy
    }

    public class TransferPacket
    {
        public EventHandler<EventArgs> SuccessEvent;
        public EventHandler<EventArgs> FailureEvent;

        public Object Task { get; set; } //unique sftp transfer task id
        public Object Parent { get; set; }
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
        public TransferType Type { get; set; }
        public LoginData LoginData { get; set; }
        public bool IsInTransit { get; set; } = false;
        public bool IsSuccessful { get; set; } = false;
        public bool HasError { get; set; } = false;
        public Exception TransferExcetpion { get; set; }
        public int TransferErrorCounter { get; set; } = 0;

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
            TargetPath = Settings.FtpUserDirectoryLogin.SubdirectoryPath + "/" + AccountID + "/motifs/" + Motif.DownloadName;
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
    }
}
