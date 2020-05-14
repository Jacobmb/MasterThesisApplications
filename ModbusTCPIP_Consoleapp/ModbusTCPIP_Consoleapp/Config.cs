using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusTCPIP_Consoleapp
{
    class Config
    {
        public string IPadress() //method for returning the IP-address for the application to connect to. Changable in app.config file
        {
            string s;
            s = ConfigurationManager.AppSettings["IPadress"];
            return s;

        }

        public int Portnumber() //method for returning the port number for the application to connect to. Changable in app.config file
        {
            int port;
            port = Convert.ToInt32(ConfigurationManager.AppSettings["PortNumber"]);
            return port;
        }

        public int maxlogin() //returning the max amount of login tries. Changable in app.config file
        {
            int maxlogin;
            maxlogin = int.Parse(ConfigurationManager.AppSettings["MaxLogin"]);
            return maxlogin;
        }


        public int Samplingtime() //returning the samplingtime. set to 5 seconds as in requirements in the Thesis. Changable in app.config file
        {
            int samplingtime;
            samplingtime = int.Parse(ConfigurationManager.AppSettings["Samplingtime"]);
            return samplingtime;
        }

        public int MaxCycles() //returning max amounts of cycles. Changable in app.config file
        {
            int maxCycles;
            maxCycles = Convert.ToInt32(ConfigurationManager.AppSettings["PortNumber"]);
            return maxCycles;
        }

    }
}
