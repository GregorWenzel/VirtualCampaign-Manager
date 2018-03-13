using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Encoding;
using VirtualCampaign_Manager.Transfers;

namespace VirtualCampaign_Manager.Workers
{
    public class AnimatedMotifWorker
    {
        private AnimatedMotif motif;
        private Thread workerThread;

        public AnimatedMotifWorker(AnimatedMotif motif)
        {
            this.motif = motif;
        }

        public void Start()
        { 
            workerThread = new Thread(Work);
            workerThread.Start();
        }

        public void Work()
        {
            TransferPacket motifDownloadPacket = new TransferPacket(motif);

            //AnimatedMotifDecoder decoder = new AnimatedMotifDecoder(motif);

        }
    }
}
