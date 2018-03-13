using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Encoding
{
    public class AnimatedMotifDecoder : EventFireBase
    {
        AnimatedMotif motif;

        public AnimatedMotifDecoder(AnimatedMotif motif)
        {
            this.motif = motif;
        }

        public void Decode()
        {

        }
    }
}
