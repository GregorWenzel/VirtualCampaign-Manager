using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCampaign_Manager.Data
{
    public class VCObject
    {
        private int id;

        public int ID
        {
            get { return id; }
            set { id = value; }
        }

        private DateTime creationDate;

        public DateTime CreationDate
        {
            get { return creationDate; }
            set { creationDate = value; }
        }

        private DateTime updateDate;

        public DateTime UpdateDate
        {
            get { return updateDate; }
            set
            {
                if (value > updateDate)
                    updateDate = value;
            }
        }
    }
}