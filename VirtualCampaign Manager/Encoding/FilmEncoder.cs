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

            string parameters = GetParameterString();
            
            EncodeFilm()          

            SendEncodeFilm(parameters, false);


            success = success & EncodeMp4Previews();

            if (!success)
            {
                production.ErrorCode = ProductionErrorStatus.PES_ENCODEproduction;
                return;
            }

            //success = success & EncodePreview();

            if (success)
                production.Status = ProductionStatus.PS_UPLOAD_FILMS;
        }

        private void CreateDirectories()
        {
            string directoryName = ProductionPathHelper.GetLocalProductionHashDirectory(production);

            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
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
                parameters += EncodingParameterParser.Parse(production, FilmOutputFormat);
            }

            return parameters;
        }

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

                    newFilmOutputFormat = new FilmOutputFormat();
                    newFilmOutputFormat.Filename = FilmOutputFormat.Filename;
                    newFilmOutputFormat.FullFilePath = FilmOutputFormat.FullFilePath;
                    newFilmOutputFormat.IsPanoChild = true;
                    newFilmOutputFormat.Name = FilmOutputFormat.Name + "_quarter";
                    newFilmOutputFormat.ID = FilmOutputFormat.ID;
                    newFilmOutputFormat.Extension = FilmOutputFormat.Extension;
                    newFilmOutputFormat.Height = (int)Math.Round(FilmOutputFormat.Height / 4d);
                    newFilmOutputFormat.Width = (int)Math.Round(FilmOutputFormat.Width / 4d);
                    newFilmOutputFormat.FfmpegParams = FilmOutputFormat.FfmpegParams.Replace(FilmOutputFormat.Height + "x" + FilmOutputFormat.Width, newFilmOutputFormat.Size);
                    buffer.Add(newFilmOutputFormat);
                }
            }

            return buffer;
        }
    }
}
