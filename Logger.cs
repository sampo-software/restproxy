using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RestProxy.Net
{
    public class Logger
    {
        private bool debug;
        public string DebugMode
        {
            get { return debug.ToString().ToLower(); }
            set
            {
                string valLowStr = value.ToLower();

                if ((valLowStr.ToLower() == "false") || (valLowStr.ToLower() == "off") || (valLowStr.ToLower() == "0"))
                    debug = false;

                if ((valLowStr.ToLower() == "true") || (valLowStr.ToLower() == "on") || (valLowStr.ToLower() == "1"))
                    debug = true;
            }
        }

        //string logFile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\RestProxyNet_log.txt";
        private string logFile = "";
        
        public Logger()
        {
            //logFile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\RestProxyNet_log.txt";    // "MyDocuments" does not resolve on IIS
            logFile = "C:\\RestProxy\\RestProxyNet_log.txt";
            DebugMode = "true";

            System.IO.File.Create(logFile).Close(); // Create empty log file if not exists
        }

        public void Log(string message)
        {
            if (!debug)
                return;

            System.IO.File.AppendAllText(logFile, DateTime.Now + "\t" + message + Environment.NewLine);
        }

    }
}