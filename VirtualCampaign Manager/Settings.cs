using HelperFunctions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager
{
    public static class Settings
    {
        //manual definitions
        //Version
        public static string Version = "4.0b";
        //interval for polling new productions in milliseconds
        public static int MainUpdateInterval = 2000;
        //max number of parallel downloads
        public static int MaxDownloadCount = 3;
        //max number of failed transfers before giving up
        public static int MaxTransferErrorCount = 3;

        //Paths to local file system     
        //Path to product clips
        public static string LocalProductPath;
        //Path to audio clips
        public static string LocalAudioPath;
        //Path to fusion plugin file
        public static string LocalFusionPluginPath;
        //Path to deadline executable
        public static string LocalDeadlineExePath;
        //Path to FFMPEG executable
        public static string LocalFfmpegExePath;
        //Path to render productions into
        public static string LocalProductionPath;       

        //External urls
        //Base URL to server
        public static string ServerUrl;
        //Absolute path to php service calls
        public static string ServicesUrl;
        //URL for users to download films from
        public static string FilmUrl;
        //Path to motifs relative to ServerUrl
        public static string ExternalMotifPath;
        //Path to account folder relatvie to ServerUrl
        public static string ExternalUserPath;

        //Ftp logins
        //ftp for upload of previews into user directories
        public static LoginData FtpUserDirectoryLogin;
        //ftp for download of audio clips
        public static LoginData FtpAudioDirectoryLogin;
        //ftp for upload of films into hash directory
        public static LoginData FtpHashDirectoryLogin;
        //ftp for upload of product preview clips (only from client admins)
        public static LoginData FtpProductPreviewDirectoryLogin;
        //email server for messages to users upon production completion
        public static LoginData EmailServerLogin;

        //Email sender address
        public static string EmailSender;

        //SALTED string
        public static string SALTED;

        //CONSTANTS
        //Chunk size for rendering
        public static int RenderChunkSize;
        //Idle Counter for attempts to get render status
        public static int MaxRenderIdleCount;

        static Settings()
        {
            string localAppPath = AppDomain.CurrentDomain.BaseDirectory;
            string iniFilePath = Path.Combine(localAppPath, "Settings.ini");

            RenderChunkSize = Convert.ToInt32(IniFileHelper.ReadValue("Constants", "RenderChunkSize", iniFilePath));
            MaxRenderIdleCount = Convert.ToInt32(IniFileHelper.ReadValue("Constants", "MaxRenderIdleCount", iniFilePath));

            LocalProductPath = IniFileHelper.ReadValue("LocalPaths", "ProductPath", iniFilePath);
            LocalAudioPath = IniFileHelper.ReadValue("LocalPaths", "AudioPath", iniFilePath);
            LocalFusionPluginPath = IniFileHelper.ReadValue("LocalPaths", "FusionPluginPath", iniFilePath);
            LocalDeadlineExePath = IniFileHelper.ReadValue("LocalPaths", "DeadlinePath", iniFilePath);
            LocalFfmpegExePath = IniFileHelper.ReadValue("LocalPaths", "FFMpegPath", iniFilePath);
            LocalProductionPath = IniFileHelper.ReadValue("LocalPaths", "ProductionPath", iniFilePath);

            ServerUrl = IniFileHelper.ReadValue("ExternalPaths", "ServerUrl", iniFilePath);
            ServicesUrl = IniFileHelper.ReadValue("ExternalPaths", "ServerUrl", iniFilePath);
            FilmUrl = IniFileHelper.ReadValue("ExternalPaths", "FilmUrl", iniFilePath);
            
            FtpUserDirectoryLogin = new LoginData(
                Url: IniFileHelper.ReadValue("Ftp", "UserFtpURL", iniFilePath),
                Username: IniFileHelper.ReadValue("Ftp", "UserFtpLogin", iniFilePath),
                Password: IniFileHelper.ReadValue("Ftp", "UserFtpPassword", iniFilePath));

            FtpAudioDirectoryLogin = new LoginData(
                Url: IniFileHelper.ReadValue("Ftp", "AudioFtpURL", iniFilePath),
                Username: IniFileHelper.ReadValue("Ftp", "AudioFtpLogin", iniFilePath),
                Password: IniFileHelper.ReadValue("Ftp", "AudioFtpPassword", iniFilePath),
                SubdirectoryPath: IniFileHelper.ReadValue("Ftp", "AudioFtpSubdirectory", iniFilePath));

            FtpHashDirectoryLogin = new LoginData(
                Url: IniFileHelper.ReadValue("Ftp", "HashFtpURL", iniFilePath),
                Username: IniFileHelper.ReadValue("Ftp", "HashFtpLogin", iniFilePath),
                Password: IniFileHelper.ReadValue("Ftp", "HashFtpPassword", iniFilePath),
                SubdirectoryPath: IniFileHelper.ReadValue("Ftp", "HashFtpSubdirectory", iniFilePath));

            FtpProductPreviewDirectoryLogin = new LoginData(
                Url: IniFileHelper.ReadValue("Ftp", "ProductPreviewFtpURL", iniFilePath),
                Username: IniFileHelper.ReadValue("Ftp", "ProductPreviewFtpLogin", iniFilePath),
                Password: IniFileHelper.ReadValue("Ftp", "ProductPreviewFtpPassword", iniFilePath),
                SubdirectoryPath: IniFileHelper.ReadValue("Ftp", "ProductPreviewFtpSubdirectory", iniFilePath));

            EmailServerLogin = new LoginData(
                Url: IniFileHelper.ReadValue("Email", "EmailHost", iniFilePath),
                Username: IniFileHelper.ReadValue("Email", "EmailLogin", iniFilePath),
                Password: IniFileHelper.ReadValue("Email", "EmailPassword", iniFilePath));

            EmailSender = IniFileHelper.ReadValue("Email", "EmailAddress", iniFilePath);
        }
    }
}
