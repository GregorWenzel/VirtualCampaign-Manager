using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Parsers;

namespace VirtualCampaign_Manager.Repositories
{
    public static class AudioRepository
    {
        public static Dictionary<string, string> GetRemoteAudio(AudioData AudioData)
        {
            Dictionary<string, string> param = new Dictionary<string, string>
            {   { "audioID", AudioData.ID.ToString() },
            };
            string audioResultString = RemoteDataManager.ExecuteRequest("getAudioById", param);

            List<Dictionary<string, string>> audioDictString = JsonDeserializer.Deserialize(audioResultString);

            if (audioDictString.Count == 0)
                return null;
            else
                //DEBUG: Check indexing
                return audioDictString[audioDictString.Count - 1];
        }
    }
}
