using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WebApi.Utils
{
    public class Auth
    {
        public static void Init() {
            try
            {
                string computerCode = FingerPrint.Value();
                string authCode = ConfigurationSettings.AppSettings["AuthKey"].ConvertToString();
                var request = (HttpWebRequest)WebRequest.Create($"http://www.keduoduo.online/Api/Auth.ashx?authCode={authCode}&computerCode={computerCode}");
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex) { }
        }
    }
}
