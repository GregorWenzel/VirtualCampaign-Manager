using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCampaign_Manager.Data
{
    public enum DownloadType
    {
        DT_AUDIO,
        DT_MOTIF        
    };

    public class DownloadData
    {
        public DownloadType Type { get; set; }
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
        public LoginData LoginData { get; set; }
        public VCObject RequestingObject { get; set; }
    }
}
