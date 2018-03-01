using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCampaign_Manager.Data
{
    public class FilmOutputFormat
    {
        public int ID { get; set; }
        public string Extension { get; set; }
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Filename { get; set; }
        public string FullFilePath { get; set; }
        public bool IsPanoChild { get; set; }
        public string FfmpegParams { get; set; }
        public string Size
        {
            get
            {
                return Width + "x" + Height;
            }
        }
        public int Area
        {
            get
            {
                return Width * Height;
            }
        }

        public FilmOutputFormat()
        {

        }

        public FilmOutputFormat(Dictionary<string, string> codec)
        {
            ID = Convert.ToInt32(codec["codec_type_id"]);
            Extension = codec["extension"];
            Name = codec["name"];
            Width = Convert.ToInt32(codec["width"]);
            Height = Convert.ToInt32(codec["height"]);

            if (codec["ffmpeg_param"] != null)
                FfmpegParams = codec["ffmpeg_param"];
            else
                FfmpegParams = "";
        }
    }
}
