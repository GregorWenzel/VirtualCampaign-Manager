using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCampaign_Manager.Data
{
    public class LoginData
    {
        public string Url;
        public string Username;
        public string Password;
        public string SubdirectoryPath;

        public string FullPath
        {
            get
            {
                return Url + SubdirectoryPath;
            }
        }

        public LoginData()
        {

        }

        public LoginData(string Url, string Username, string Password, string SubdirectoryPath = "")
        {
            this.Url = Url;
            this.Username = Username;
            this.Password = Password;
            this.SubdirectoryPath = SubdirectoryPath;
        }

    }
}
