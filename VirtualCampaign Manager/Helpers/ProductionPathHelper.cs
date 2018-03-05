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
        public static string GetLocalProductionDirectory(Production Production)
        {
            return Path.Combine(Settings.LocalProductionPath, Production.ID.ToString());
        }
    }
}
