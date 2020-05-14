using Modbus.Device;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ModbusTCP_UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    { //Communication variables
        TcpClient client;
        ModbusIpMaster master;

        int MaxCycles = 540; //45 MINUTES
        int MaxLogin = 3;
        int PollingCycle = 5000;




        public MainPage()
        {
            this.InitializeComponent();

            for (int login = 0; login < MaxLogin; login++) //login logic, if login does not succeed the first time, it will try to reconnect 3 times.
            {
                try
                {
                    Connect();
                }
                catch (Exception)
                {
                    throw;
                }
            }


            for (int cycles = 0; cycles < MaxCycles; cycles++) //logic for capturing and sending data to cloud service
            {
                UInt16[] modbusint = master.ReadHoldingRegisters(1, 1);

                SqlConnection sqlconn = new SqlConnection("Server = tcp:modbusserver.database.windows.net,1433; Initial Catalog = ModbusDB; Persist Security Info = False; User ID = myadmin; Password =(YourPasswordHere); MultipleActiveResultSets = False; Encrypt = True; TrustServerCertificate = False; Connection Timeout = 30;");
                sqlconn.Open(); //opents the sql connection
                string query = "INSERT INTO SensorData (ApplicationType, PressureData, logtimeapp)"; //specifies the query sent to the database, what columns to target
                query += " VALUES (@ApplicationType, @PressureData, @logtimeapp)";
                SqlCommand cmd = new SqlCommand(query, sqlconn);
                cmd.Parameters.AddWithValue("@logtimeapp", DateTime.Now);
                cmd.Parameters.AddWithValue("@PressureData", Convert.ToInt32(modbusint[0]));
                cmd.Parameters.AddWithValue("@ApplicationType", "RaspberryPi_UWP");

                cmd.ExecuteNonQuery();
                sqlconn.Close();
                System.Threading.Thread.Sleep(PollingCycle);


            }


        }

        private void Connect() //method for connecting to ModbusClient
        {
            //create new tcpclient
            client = new TcpClient();
            client.Client.Connect("192.168.50.164", 502);

            //initialize modbus master, raspberry pi
            master = ModbusIpMaster.CreateIp(client);
            master.Transport.Retries = 0;
            master.Transport.ReadTimeout = 3000;



        }
    }
}
