using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Helpers;
using VirtualCampaign_Manager.Repositories;

namespace VirtualCampaign_Manager.Data
{
    public class AudioData : VCObject
    {
        public bool Result;

        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private string extension;
        public string Extension
        {
            get { return extension; }
            set { extension = value; }
        }

        public string Filename
        {
            get
            {
                return ("audio_" + ID + Extension);
            }
        }

        public string AudioPath
        {
            get
            {
                return ProductionPathHelper.GetLocalAudioPath(this);
            }
        }

        public AudioData(int id)
        {
            ID = id;

            Dictionary<string, string> AudioRow = AudioRepository.GetRemoteAudio(this);

            if (AudioRow != null)
            {
                name = Convert.ToString(AudioRow["FileName"]);
                extension = Convert.ToString(AudioRow["FileExtension"]);
                Result = true;
                return;
            }
            Result = false;
        }        
    }
}
