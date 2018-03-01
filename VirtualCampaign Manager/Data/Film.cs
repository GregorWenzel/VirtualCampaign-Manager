using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCampaign_Manager.Data
{
    public class Film : VCObject
    {
        public List<FilmOutputFormat> FilmOutputFormatList { get; set; }
        public string UrlHash { get; set; }

        public FilmOutputFormat LargestFilmOutputFormat
        {
            get
            {
                return FilmOutputFormatList.OrderByDescending(item => item.Area).First();
            }
        }
        
        public Film()
        {
            FilmOutputFormatList = new List<FilmOutputFormat>();
        }

        public Film(int id, string filmCodes)
        {
            ID = id;
            FilmOutputFormatList = new List<FilmOutputFormat>();
        }
    }
}
