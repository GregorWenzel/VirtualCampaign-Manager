using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCampaign_Manager.Data
{
    public class Motif : VCObject
    {
        public string Type { get; set; }
        public int Position { get; set; }
        public string Extension { get; set; }
        public string LoaderName { get; set; }
        public string Text { get; set; }
        public int Frames { get; set; }

        public Motif(int ID, string Type, int Position, string Extension, string LoaderName, string Text)
        {
            this.ID = ID;
            this.Type = Type;
            this.Position = Position;
            this.Extension = Extension;
            this.LoaderName = LoaderName;
            this.Text = Text;
            this.Frames = 0;
        }

        /*
        public bool IsMovie
        {
            get
            {
                return (Extension == ".avi" || Extension == ".mov" || Extension == ".wmv" || Extension == ".mpg" || Extension == ".mpeg" || Extension == ".mp4");
            }
        }
        */

        public string Id
        {
            get
            {
                return this.ID.ToString();
            }
        }

        public string DownloadName
        {
            get
            {
                return ID + Extension;
            }
        }
    }
}