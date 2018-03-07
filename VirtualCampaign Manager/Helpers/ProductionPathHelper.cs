using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Helpers
{
    public static class ProductionPathHelper
    {
        public static string GetClipListPath(Production production)
        {
            return Path.Combine(GetLocalProductionDirectory(production), "cliplist.txt");
        }

        public static string GetFullMp4Path(Production production)
        {
            return Path.Combine(GetLocalProductionDirectory(production), "full.mp4");
        }

        public static string GetLocalProductionDirectory(Production Production)
        {
            return Path.Combine(Settings.LocalProductionPath, Production.ID.ToString());
        }

        public static string GetLocalProductionHashDirectory(Production Production)
        {
            return Path.Combine(GetLocalProductionDirectory(Production), Production.Film.UrlHash);
        }

        public static string GetLocalAudioPath(AudioData AudioData)
        {
            return Path.Combine(Settings.LocalAudioPath, AudioData.Filename);
        }

        public static string GetPartialFilmFilename(Production Production)
        {
            string targetFilename = "film_" + Production.FilmID;
            return Path.Combine(GetLocalProductionHashDirectory(Production), targetFilename);
        }

        public static string GetTrimmedMusicPath(Production Production)
        {
            return Path.Combine(GetLocalProductionDirectory(Production), "trimmed_music.wav");
        }

        public static string GetFadedMusicPath(Production Production)
        {
            return Path.Combine(GetLocalProductionDirectory(Production), "faded_music.wav");
        }       

        public static string GetFinalMusicPath(Production Production)
        {
            return Path.Combine(GetLocalProductionDirectory(Production), "final_music.wav");
        }

        public static string GetProductMp4PathByOutputFormat(int ProductID, int OutputFormatID)
        {
            string formattedIndex = String.Format("{0:D4}", ProductID);
            return Path.Combine(new string[] { Settings.LocalProductPath, formattedIndex, formattedIndex + "_" + OutputFormatID + ".mp4" });
        }

        public static string GetProductMp4Path(int ProductID)
        {
            string formattedIndex = String.Format("{0:D4}", ProductID);
            return Path.Combine(new string[] { Settings.LocalProductPath, formattedIndex, formattedIndex + ".mp4" });
        }

        public static string GetSpecialProductAudioPath(int ProductID)
        {
            string formattedIndex = String.Format("{0:D4}", ProductID);
            return "file '" + Path.Combine(new string[] { Settings.LocalProductPath, formattedIndex, formattedIndex + ".wav" }) + "'";
        }

        public static string GetAudioListFile(Production Production)
        {
            return Path.Combine(GetLocalProductionDirectory(Production), "audiolist.txt");
        }
    }
}
