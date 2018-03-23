using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Helpers;
using VirtualCampaign_Manager.Transfers;

namespace VirtualCampaign_Manager.Encoding
{
    public class AudioEncoder : EventFireBase
    {
        Production production;
        AudioData audioData;

        public AudioEncoder(Production Production)
        {
            production = Production;
        }

        public void Encode()
        {
            if (production.AudioID > 0)
            {
                audioData = new AudioData(production.AudioID);

                if (audioData.Result == false)
                {
                    FireFailureEvent(ProductionErrorStatus.PES_GET_AUDIO);
                    return;
                }

                string filename = audioData.AudioPath;

                if (File.Exists(filename))
                {
                    MuxAudio();
                }
                else
                {
                    DownloadAudio();
                }
            }
        }

        private void DownloadAudio()
        {
            TransferPacket audioTransferPacket = new TransferPacket(production, audioData);
            audioTransferPacket.FailureEvent += OnAudioTransferFailure;
            audioTransferPacket.SuccessEvent += OnAudioTransferSuccess;

            DownloadManager.Instance.AddTransferPacket(audioTransferPacket);
        }

        private void OnAudioTransferFailure(Object obj, EventArgs ea)
        {
            (obj as TransferPacket).FailureEvent -= OnAudioTransferFailure;
            (obj as TransferPacket).SuccessEvent -= OnAudioTransferSuccess;
            FireFailureEvent(ProductionErrorStatus.PES_READ_AUDIOFILE);
        }

        private void OnAudioTransferSuccess(Object obj, EventArgs ea)
        {
            (obj as TransferPacket).FailureEvent -= OnAudioTransferFailure;
            (obj as TransferPacket).SuccessEvent -= OnAudioTransferSuccess;
            MuxAudio();
        }

        private void MuxAudio()
        {
            if (CreateTrimmedMusic() == false) return;

            if (CreateFadedMusic() == false) return;
            
            CreateAudiolistFile();

            if (CreateFinalMusic() == false) return;

            FireSuccessEvent();
        }

        private bool CreateTrimmedMusic()
        {
            int durationInSeconds = 0;

            if (production.HasSpecialIntroMusic)
                durationInSeconds = production.ClipDurationInSeconds;
            else
                durationInSeconds = production.TotalDurationInSeconds;

            if (durationInSeconds == 0)
            {
                FireFailureEvent(ProductionErrorStatus.PES_CALCULATE_DURATION);
                return false;
            }

            string cmd = "-y -loglevel panic -i " + audioData.AudioPath + " -t " + durationInSeconds + " " + ProductionPathHelper.GetTrimmedMusicPath(production);

            VCProcess MusicProcess = new VCProcess(production);
            MusicProcess.StartInfo.FileName = Settings.LocalFfmpegExePath;
            MusicProcess.StartInfo.CreateNoWindow = true;
            MusicProcess.StartInfo.UseShellExecute = false;
            MusicProcess.StartInfo.RedirectStandardError = false;
            MusicProcess.StartInfo.RedirectStandardOutput = false;
            MusicProcess.EnableRaisingEvents = true;
            MusicProcess.StartInfo.Arguments = cmd;
            bool success = MusicProcess.Execute();

            if (success)
            {
                MusicProcess.WaitForExit();
            }

            if (!File.Exists(ProductionPathHelper.GetTrimmedMusicPath(production)))
            {
                FireFailureEvent(ProductionErrorStatus.PES_MUX_AUDIO);
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool CreateFadedMusic()
        {
            int durationInSeconds = 0;

            if (production.HasSpecialIntroMusic)
                durationInSeconds = production.ClipDurationInSeconds;
            else
                durationInSeconds = production.TotalDurationInSeconds;

            if (durationInSeconds == 0)
            {
                FireFailureEvent(ProductionErrorStatus.PES_CALCULATE_DURATION);
                return false;
            }

            string cmd = "-y -loglevel panic -i " + ProductionPathHelper.GetTrimmedMusicPath(production) + " -af \"afade=t=in:st=0:d=2,afade=t=out:st=" + (durationInSeconds - 2).ToString() + ":d=2\" " + ProductionPathHelper.GetFadedMusicPath(production);

            VCProcess MusicProcess = new VCProcess(production);
            MusicProcess.StartInfo.FileName = Settings.LocalFfmpegExePath;
            MusicProcess.StartInfo.CreateNoWindow = true;
            MusicProcess.StartInfo.UseShellExecute = false;
            MusicProcess.StartInfo.RedirectStandardError = false;
            MusicProcess.StartInfo.RedirectStandardOutput = false;
            MusicProcess.EnableRaisingEvents = true;
            MusicProcess.StartInfo.Arguments = cmd;
            bool success = MusicProcess.Execute();
            if (success)
            {
                MusicProcess.WaitForExit();
            }

            if (!File.Exists(ProductionPathHelper.GetTrimmedMusicPath(production)))
            {
                FireFailureEvent(ProductionErrorStatus.PES_MUX_AUDIO);
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool CreateFinalMusic()
        {
            int durationInSeconds = 0;

            if (production.HasSpecialIntroMusic)
                durationInSeconds = production.ClipDurationInSeconds;
            else
                durationInSeconds = production.TotalDurationInSeconds;

            if (durationInSeconds == 0)
            {
                FireFailureEvent(ProductionErrorStatus.PES_CALCULATE_DURATION);
                return false;
            }

            string cmd = "-y -loglevel panic -safe 0 -f concat -i " + ProductionPathHelper.GetAudioListFile(production) + " -c copy " + ProductionPathHelper.GetFinalMusicPath(production);

            VCProcess MusicProcess = new VCProcess(production);
            MusicProcess.StartInfo.FileName = Settings.LocalFfmpegExePath;
            MusicProcess.StartInfo.CreateNoWindow = true;
            MusicProcess.StartInfo.UseShellExecute = false;
            MusicProcess.StartInfo.RedirectStandardError = false;
            MusicProcess.StartInfo.RedirectStandardOutput = false;
            MusicProcess.EnableRaisingEvents = true;
            MusicProcess.StartInfo.Arguments = cmd;
            bool success = MusicProcess.Execute();

            if (success)
            {
                MusicProcess.WaitForExit();
            }

            if (!File.Exists(ProductionPathHelper.GetFinalMusicPath(production)))
            {
                FireFailureEvent(ProductionErrorStatus.PES_MUX_AUDIO);
                return false;
            }
            else
            {
                return true;
            }
        }

        private void CreateAudiolistFile()
        {
            //create cliplist file
            string clipListFilename = ProductionPathHelper.GetAudioListFile(production);
            StreamWriter writer = new StreamWriter(clipListFilename, false);
            writer.WriteLine(@"# audiocliplist");

            if (production.HasSpecialIntroMusic && production.JobList[0].IsDicative)
            {
                string specialProductAudioFilePath = ProductionPathHelper.GetSpecialProductAudioPath(production.JobList[0].ProductID);
                writer.WriteLine(specialProductAudioFilePath);
            }

            writer.WriteLine("file '" + ProductionPathHelper.GetFadedMusicPath(production) + "'");

            if (production.HasSpecialIntroMusic && production.JobList[production.JobList.Count - 1].IsDicative)
            {
                string specialProductAudioFilePath = ProductionPathHelper.GetSpecialProductAudioPath(production.JobList[production.JobList.Count - 1].ProductID);
                writer.WriteLine(specialProductAudioFilePath);
            }
            writer.Close();
        }
    }   
}
