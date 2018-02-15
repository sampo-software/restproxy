using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Configuration;

namespace RestProxy.Net.Configuration
{
    public class RestConfiguration
    {
        public string Version { get; set; }
        public string TwilioAccountsid { get; set; }
        public string TwilioToken { get; set; }
        public string Appsid { get; set; }
        public string TwilioBaseUrl { get; set; }
        public string TwilioPhoneNumber { get; set; }

        public RestConfiguration()
        {
            TwilioAccountsid = ConfigurationManager.AppSettings["TwilioAccountsid"];
            TwilioToken = ConfigurationManager.AppSettings["TwilioToken"];
            TwilioPhoneNumber = ConfigurationManager.AppSettings["TwilioPhoneNumber"];
            TwilioBaseUrl = ConfigurationManager.AppSettings["TwilioBaseUrl"];

            Version = "v0.54 / phone +" + TwilioPhoneNumber + " via " + TwilioBaseUrl;
            Appsid = "";
        }
    }
}
