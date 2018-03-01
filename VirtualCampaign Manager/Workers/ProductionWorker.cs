using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Workers
{
    public class ProductionWorker
    {
        //Event called after production worker has finished all production tasks
        public event EventHandler FinishedEvent;     

        //current production
        private Production production;


        public ProductionWorker(Production Production)
        {
            this.production = Production;
        }

        protected virtual void OnFinishedEvent(EventArgs ea)
        {
            FinishedEvent?.Invoke(this, ea);
        }
    }
}
