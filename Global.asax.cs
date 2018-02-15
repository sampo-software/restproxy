using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using RestProxy.Net.Repositories;
using RestProxy.Net.Configuration;

namespace RestProxy.Net
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        public static Logger logger;
        public static DBStore dbstore;
        public static RestConfiguration restConfiguration;
        public static MessageRepository messageRepository;

        protected void Application_Start()
        {
            logger = new Logger();
            logger.Log("RestProxyNet starting.");

            restConfiguration = new RestConfiguration();
            logger.Log("RestConfiguration " + restConfiguration.Version);

            dbstore = new DBStore();
            messageRepository = new MessageRepository();

            logger.DebugMode = "0";

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }



}