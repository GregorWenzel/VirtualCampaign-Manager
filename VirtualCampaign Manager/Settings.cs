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
        public static int MainUpdateInterval = 5000;
        //interval for polling new animated motifs to generate preview frame for
        public static int MotifUpdateInterval = 10000;
        //max number of parallel download threads
        public static int MaxDownloadThreads = 3;
        //max number of failed transfers before giving up
        public static int MaxTransferErrorCount = 10;

        //Paths to local file system     
        //Base path to local files (data, tools, ressources)
        public static string LocalBasePath;
        //Variable used in Fusion comp files to replace with base path
        public static string BasePathVariable;
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
        public static LoginData MasterLogin;
        public static string FtpUserSubdirectory;
        public static string FtpAudioSubdirectory;
        public static string FtpHashSubdirectory;
        public static string FtpProductPreviewSubdirectory;

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

        //MongoDB
        public static string MongoServerURL;
        public static string MongoPort;

        static Settings()
        {
            string defaultIniFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.ini");
            string iniFilePath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VC Render Manager", "Settings.ini");

            if (!File.Exists(iniFilePath))
            {
                Directory.CreateDirectory(Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VC Render Manager"));
                File.Copy(defaultIniFilePath, iniFilePath);
            }

            RenderChunkSize = Convert.ToInt32(IniFileHelper.ReadValue("Constants", "RenderChunkSize", iniFilePath));
            MaxRenderIdleCount = Convert.ToInt32(IniFileHelper.ReadValue("Constants", "MaxRenderIdleCount", iniFilePath));

            BasePathVariable = IniFileHelper.ReadValue("LocalPaths", "BasePathVariable", iniFilePath);
            LocalBasePath = IniFileHelper.ReadValue("LocalPaths", "BasePath", iniFilePath);
            LocalProductPath = IniFileHelper.ReadValue("LocalPaths", "ProductPath", iniFilePath);
            LocalAudioPath = IniFileHelper.ReadValue("LocalPaths", "AudioPath", iniFilePath);
            LocalFusionPluginPath = IniFileHelper.ReadValue("LocalPaths", "FusionPluginPath", iniFilePath);
            LocalDeadlineExePath = IniFileHelper.ReadValue("LocalPaths", "DeadlinePath", iniFilePath);
            LocalFfmpegExePath = IniFileHelper.ReadValue("LocalPaths", "FFMpegPath", iniFilePath);
            LocalProductionPath = IniFileHelper.ReadValue("LocalPaths", "ProductionPath", iniFilePath);

            ServerUrl = IniFileHelper.ReadValue("ExternalPaths", "ServerUrl", iniFilePath);
            ServicesUrl = IniFileHelper.ReadValue("ExternalPaths", "ServicesUrl", iniFilePath);
            FilmUrl = IniFileHelper.ReadValue("ExternalPaths", "FilmUrl", iniFilePath);
            
            MasterLogin = new LoginData(
                Url: IniFileHelper.ReadValue("Ftp", "MasterFtpURL", iniFilePath),
                Username: IniFileHelper.ReadValue("Ftp", "MasterFtpLogin", iniFilePath),
                Password: IniFileHelper.ReadValue("Ftp", "MasterFtpPassword", iniFilePath));

            FtpUserSubdirectory = IniFileHelper.ReadValue("Ftp", "UserFtpSubdirectory", iniFilePath);
            FtpAudioSubdirectory = IniFileHelper.ReadValue("Ftp", "AudioFtpSubdirectory", iniFilePath);
            FtpHashSubdirectory = IniFileHelper.ReadValue("Ftp", "HashFtpSubdirectory", iniFilePath);
            FtpProductPreviewSubdirectory = IniFileHelper.ReadValue("Ftp", "ProductPreviewFtpSubdirectory", iniFilePath);

            EmailServerLogin = new LoginData(
                Url: IniFileHelper.ReadValue("Email", "EmailHost", iniFilePath),
                Username: IniFileHelper.ReadValue("Email", "EmailLogin", iniFilePath),
                Password: IniFileHelper.ReadValue("Email", "EmailPassword", iniFilePath));

            EmailSender = IniFileHelper.ReadValue("Email", "EmailAddress", iniFilePath);

            SALTED = IniFileHelper.ReadValue("RemoteService", "SALTED", iniFilePath);

            MongoServerURL = IniFileHelper.ReadValue("MongoDB", "MongoServer", iniFilePath);
            MongoPort = IniFileHelper.ReadValue("MongoDB", "MongoPort", iniFilePath);
        }
    }
}
