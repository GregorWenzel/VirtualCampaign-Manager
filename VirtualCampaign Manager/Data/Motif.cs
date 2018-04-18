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
        public string OriginalExtension { get; set; }
        public string LoaderName { get; set; }
        public string Text { get; set; }
        public int Frames { get; set; }
        public bool IsAvailable { get; set; } = false;
        public Job Job { get; set; }

        public Motif() { }

        public Motif(int ID, string Type, int Position, string Extension, string LoaderName, string Text, Job job)
        {
            this.ID = ID;
            this.Type = Type;
            this.Position = Position;
            this.Extension = Extension;
            this.LoaderName = LoaderName;
            this.Text = Text;
            this.Frames = 0;
            this.Job = job;
        }

        public void Reset()
        {
            if (OriginalExtension != null && OriginalExtension.Length > 0)
            {
                Extension = OriginalExtension;
            }
        }

        public bool IsMovie
        {
            get
            {
                return (Extension == ".avi" || Extension == ".mov" || Extension == ".wmv" || Extension == ".mpg" || Extension == ".mpeg" || Extension == ".mp4");
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