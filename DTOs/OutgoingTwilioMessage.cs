using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestProxy.Net.DTO
{
    public class OutgoingTwilioMessage
    {
        public int id { get; set; }
        public DateTime Timestamp { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Action { get; set; }
        public string Method { get; set; }
        public string Body { get; set; }
        public string MediaURLs { get; set; }       // Tab-delimited string of URLs pointing to MMS content media files
        public string Client { get; set; }          // Arbitrary client identifier for possible distinction of several recipients via the same twilio phone gateway

        public override string ToString()
        {
            return $"{Client}, {From}, {To}, {Body}, {MediaURLs} ,{Action}, {Method}";
        }
    }
}
