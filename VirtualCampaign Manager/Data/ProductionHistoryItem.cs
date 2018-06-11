using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCampaign_Manager.Data
{
    public class ProductionHistoryItem
    {
        public string Name { get; set; }
        public int FilmID { get; set; }
        public int ID { get; set; }
        public string Username { get; set; }
        public int AccountID { get; set; }
        public DateTime UpdateDate { get; set; }

        public ProductionHistoryItem(Production production)
        {
            Name = production.Name;
            FilmID = production.FilmID;
            ID = production.ID;
            Username = production.Username;
            AccountID = production.AccountID;
            UpdateDate = new DateTime(production.UpdateDate.Ticks);
        }
    }
}
