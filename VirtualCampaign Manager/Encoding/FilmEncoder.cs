using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Helpers;
using VirtualCampaign_Manager.Parsers;

namespace VirtualCampaign_Manager.Encoding
{
    public class FilmEncoder : EventFireBase
    {
        private Production production;

        public FilmEncoder(Production Production)
        {
            production = Production;
        }

        public void Encode()
        {
            CreateDirectories();
           
            if (EncodeFilms() == false) return;

            EncodeMp4Previews();
        }

        private void CreateDirectories()
        {
            string directoryName = ProductionPathHelper.GetLocalProductionHashDirectory(production);

            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
        }

        private bool EncodeFilms()
        {
            bool result = true;

            VCProcess process = new VCProcess(production);
            process.StartInfo.FileName = Settings.LocalFfmpegExePath;
            process.StartInfo.Arguments = GetParameterString();
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = false;
            process.StartInfo.RedirectStandardOutput = false;

            bool success = process.Execute();
            if (success)
            {
                process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                process.WaitForExit();
            }

            //check if all films have been created
            foreach (FilmOutputFormat format in production.Film.FilmOutputFormatList)
            {
                if (format.ID != 20)
                {
                    string filepath = FilmPathHelper.GetFilmHashPath(production, format);
                    if (!File.Exists(filepath))
                    {
                        FireFailureEvent(ProductionErrorStatus.PES_ENCODE_PRODUCTION);
                        result = false;
                        break;
                    }
                }
            }

            return result;
        }

        private bool EncodeMp4Previews()
        {
            bool result = false;

            PreviewFilmEncoder previewEncoder = new PreviewFilmEncoder(production);
            previewEncoder.FailureEvent += OnPreviewEncoderFailure;
            previewEncoder.SuccessEvent += OnPreviewEncoderSuccess;
            previewEncoder.Encode();

            return result;
        }

        private void OnPreviewEncoderFailure(object sender, ResultEventArgs rea)
        {
            (sender as PreviewFilmEncoder).FailureEvent -= OnPreviewEncoderFailure;
            (sender as PreviewFilmEncoder).SuccessEvent -= OnPreviewEncoderSuccess;

            FireFailureEvent(rea.Result);
        }

        private void OnPreviewEncoderSuccess(object sender, EventArgs rea)
        {
            (sender as PreviewFilmEncoder).FailureEvent -= OnPreviewEncoderFailure;
            (sender as PreviewFilmEncoder).SuccessEvent -= OnPreviewEncoderSuccess;
            FireSuccessEvent();
        }

        private string GetParameterString()
        {
            if (production.ContainsPano)
            {
                production.Film.FilmOutputFormatList.AddRange(AddPanoToCodecList(production.Film.FilmOutputFormatList));
            }

            string parameters = "-y -i " + ProductionPathHelper.GetFullMp4Path(production) + " ";

            if (production.AudioID > 0)
            {
                if (production.Film.FilmOutputFormatList.Count(item => item.Size == "4K360") == 0)
                {
                    parameters += "-i " + ProductionPathHelper.GetFinalMusicPath(production) + " ";
                }
            }

            foreach (FilmOutputFormat FilmOutputFormat in production.Film.FilmOutputFormatList)
            {
                if (FilmOutputFormat.ID != 20)
                {
                    FilmOutputFormat.FullFilePath = FilmPathHelper.GetFilmHashPath(production, FilmOutputFormat);
                    parameters += FilmOutputFormat.FfmpegParams + " " + FilmOutputFormat.FullFilePath + " ";
                }
            }

            return parameters;
        }

        //Adds a half-sized output format to the list of required output formats to 360° films
        private List<FilmOutputFormat> AddPanoToCodecList(List<FilmOutputFormat> FilmOutputFormatList)
        {
            List<FilmOutputFormat> buffer = new List<FilmOutputFormat>();
            foreach (FilmOutputFormat FilmOutputFormat in FilmOutputFormatList)
            {
                if (FilmOutputFormat.Name.Contains("K360"))
                {
                    FilmOutputFormat newFilmOutputFormat = new FilmOutputFormat();
                    newFilmOutputFormat.Filename = FilmOutputFormat.Filename;
                    newFilmOutputFormat.FullFilePath = FilmOutputFormat.FullFilePath;
                    newFilmOutputFormat.IsPanoChild = true;
                    newFilmOutputFormat.Name = FilmOutputFormat.Name + "_half";
                    newFilmOutputFormat.ID = FilmOutputFormat.ID;
                    newFilmOutputFormat.Extension = FilmOutputFormat.Extension;
                    newFilmOutputFormat.Height = (int)Math.Round(FilmOutputFormat.Height / 2d);
                    newFilmOutputFormat.Width = (int)Math.Round(FilmOutputFormat.Width / 2d);
                    newFilmOutputFormat.FfmpegParams = FilmOutputFormat.FfmpegParams.Replace(FilmOutputFormat.Height + "x" + FilmOutputFormat.Width, newFilmOutputFormat.Size);
                    buffer.Add(newFilmOutputFormat);
                }
            }

            return buffer;
        }
    }
}
