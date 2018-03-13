using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Model
{
    public class AnimatedMotif : VCObject
    {
        public int AccountID;
        public string Extension;
        public int FrameCount;

        private bool working = false;
        public bool done = false;

        public void Decode()
        {
            if (working)
                return;

            working = true;

            if (DownloadMotifMovie())
            {
                GetFrameCount();
                if (FrameCount > 0)
                {
                    ExtractPreviewFrame();
                    UploadPreviewFrame();
                }
                Directory.Delete(Path.Combine(new string[] { SettingManager.Instance.TempEncodingDirectory, this.ID.ToString() }), true);
            }

            JSONRemoteManager.Instance.UpdateMotif(this);
            done = true;
        }

        private void GetFrameCount()
        {
            Process process = new Process();

            string arguments = "-i " + Path.Combine(new string[] { SettingManager.Instance.TempEncodingDirectory, this.ID.ToString(), (this.ID.ToString() + this.Extension) }) + " -f null /dev/null";

            process.StartInfo.FileName = SettingManager.Instance.FFMpegPath;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.Arguments = arguments;
            process.EnableRaisingEvents = true;
            process.Start();
            StreamReader reader = process.StandardError;
            string output = reader.ReadToEnd();

            process.WaitForExit();
            process.Close();

            FrameCount = ParseFrames(output);
        }

        private int ParseFrames(string text)
        {
            //Bei fehlerhaften Motiven steht in text der stdout vom ffmpeg => abfangen
            text = text.Substring(text.LastIndexOf("frame=") + 6, 5);
            try
            {
                return Convert.ToInt32(text);
            }
            catch
            {
                return 0;
            }
        }

        private void ExtractPreviewFrame()
        {
            EncodingManager encodingManager = new EncodingManager();
            encodingManager.ExtractMotif(this);
        }

        private void UploadPreviewFrame()
        {
            string targetFileName;
            string sourceFileName = Path.Combine(new string[] { SettingManager.Instance.TempEncodingDirectory, this.ID.ToString(), this.ID.ToString() + "_thumb.jpg" });

            FtpManager ftp = new FtpManager(SettingManager.Instance.FtpServerURL, SettingManager.Instance.FtpLogin, SettingManager.Instance.FtpPassword);

            targetFileName = this.AccountID + "/motifs/" + this.ID + "_thumb.jpg";

            ftp.upload(targetFileName, sourceFileName);
        }

        private bool DownloadMotifMovie()
        {
            string sourceFileName;
            string targetFileName;
            string motifName;

            FtpManager ftp = new FtpManager(SettingManager.Instance.FtpServerURL, SettingManager.Instance.FtpLogin, SettingManager.Instance.FtpPassword);

            motifName = this.ID.ToString() + this.Extension;
            sourceFileName = this.AccountID + "/motifs/" + motifName;

            targetFileName = Path.Combine(SettingManager.Instance.TempEncodingDirectory, this.ID.ToString());
            if (!Directory.Exists(targetFileName))
                Directory.CreateDirectory(targetFileName);

            targetFileName = Path.Combine(targetFileName, motifName);

            ftp.download(sourceFileName, targetFileName);

            if (!File.Exists(targetFileName))
            {
                return false;
            }
            return true;
        }
    }
}