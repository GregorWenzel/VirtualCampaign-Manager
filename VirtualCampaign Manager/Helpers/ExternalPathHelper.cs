using UriCombine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Helpers
{
    public static class ExternalPathHelper
    {
        public static string GetServerUrl()
        {
            return Settings.ServerUrl;
        }

        public static string GetAccountFtpUrl()
        {
            return Uri.Combine(Settings.FtpUserDirectoryLogin.Url, Settings.FtpUserDirectoryLogin.SubdirectoryPath);
        }

        public static string GetMotifFtpUrl()
        {
            return Uri.Combine(GetAccountFtpUrl(), Settings.ExternalMotifPath);
        }

        public static string GetProductionPreviewDirectory(Production production)
        {
            return Uri.Combine(production.AccountID.ToString(), "productions");
        }
    }
}
