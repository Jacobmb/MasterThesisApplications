using EasyModbus;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ModbusTCPIP_Consoleapp
{
    class Program
    {
        static void Main(string[] args)
        {
            ///
            /// Variables ///
            /// 
            // creating an object of the Config class, and accessing the methods in the class
            Config config = new Config();
            int portnumber = config.Portnumber();
            string ipadress = config.IPadress();
            int maxlogin = config.maxlogin();
            int samplingtime = config.Samplingtime();
            int MaxCycles = config.MaxCycles();



            ///
            /// Send all process values from txt tile to Cloud service ///
            /// 
            SqlConnection sqlconnRetry = new SqlConnection(ConfigurationManager.ConnectionStrings["ModbusLibraryConnectionString"].ConnectionString); //retry connection to the database, only used if there exists a pressure.txt file
            if (File.Exists(@"C:/Users/Jacob/Documents/Pressure.txt"))
            {
                using (sqlconnRetry)
                {
                    try
                    {
                        string[] readtext = File.ReadAllLines(@"C:/Users/Jacob/Documents/Pressure.txt"); //reading all lines in pressure.txt
                        foreach (string s in readtext)
                        {
                            sqlconnRetry.Open();
                            string query = "INSERT INTO SensorData (ApplicationType, PressureData, logtimeapp)";//Sql query to be sent to database
                            query += " VALUES (@ApplicationType,@PressureData,@logtimeapp )";
                            SqlCommand cmd = new SqlCommand(query, sqlconnRetry);

                            cmd.Parameters.AddWithValue("@ApplicationType", "ConsoleApp");//adding parameters to the sql command
                            cmd.Parameters.AddWithValue("@PressureData", s);
                            cmd.Parameters.AddWithValue("@logtimeapp", DateTime.Now);
                            cmd.ExecuteNonQuery();//executing sql command
                            sqlconnRetry.Close(); //closing the database connection from sqlconnectionRetry
                        }
                        File.Delete(@"C:/Users/Jacob/Documents/Pressure.txt");//deleting pressure.txt, making room for a new if database connection fails

                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
            }

            Queue<string> Pressure = new Queue<string>(360);//queue structure with 360 lines of data available

            ModbusClient modbusclient = new ModbusClient(ipadress, portnumber); //creating new instance of ModbusClient, with ip-address and port number taken from config.cs


            ///
            /// Login logic to the Modbus server ///
            /// 
            for (int loginTries = 0; ; loginTries++) //login check to modbus server 
            {
                try
                {
                    modbusclient.Connect();
                    break;
                }
                catch (Exception ex)
                {
                    if (loginTries < maxlogin)
                    {
                        int DisplayedTries = Convert.ToInt32(loginTries.ToString()) + 1; //since logintries starts on zero, declaring an ineger and adding 1 to the output of "logintries" for a better visual representation.
                        Console.WriteLine("Retrying " + DisplayedTries + " times..");
                        Thread.Sleep(3000);

                        string error = @"C:/Users/Jacob/Documents/errorFile.txt";
                        using (StreamWriter stwr = new StreamWriter(error, true))//if the error file exists, append text. If not, it will generate a new errorfile
                        {
                            stwr.Write(string.Format("Error Message: {0}{1}StackTrace :{2}{1}Date :{3}{1}-----------------------------------------------------------------------------{1}"
                                , ex.Message, Environment.NewLine, ex.StackTrace, DateTime.Now.ToString()));
                        }
                    }
                    else
                    {
                        Console.WriteLine("Application exiting..");
                        Environment.Exit(1);
                    } 
                }
            }


            ///
            /// login and transmit logic for sending data to cloud service ///
            /// and saving process values to txt-file if broken connection.///
            /// 
            SqlConnection sqlconn = new SqlConnection(ConfigurationManager.ConnectionStrings["ModbusLibraryConnectionString"].ConnectionString);


            for (int DBloginTries = 0; ; DBloginTries++) //login check to database
            {
                try
                {
                    for (int cycles = 0; cycles < MaxCycles; cycles++)
                    {
                        sqlconn.Open();
                        int[] readHoldingResisters = modbusclient.ReadHoldingRegisters(0, 1); // read 1 holding registers to array, starting with adress 1(0). 
                        string query = "INSERT INTO SensorData (ApplicationType, PressureData, logtimeapp)"; //formulation of the SQL query, to be sent to the database
                        query += " VALUES (@ApplicationType, @PressureData,     @logtimeapp)";
                        SqlCommand cmd = new SqlCommand(query, sqlconn);

                        Pressure.Enqueue(Convert.ToString(readHoldingResisters[0]));//enqueuing values to the queue

                        cmd.Parameters.AddWithValue("@ApplicationType", "ConsoleApp"); //adding parameters to the sql command
                        cmd.Parameters.AddWithValue("@PressureData", readHoldingResisters[0]);
                        cmd.Parameters.AddWithValue("@logtimeapp", DateTime.Now);
                        Console.WriteLine(Pressure.Dequeue()); //dequeuing the values in the queue
                        cmd.ExecuteNonQuery();//excetutes sql query to the database
                        sqlconn.Close();
                        Thread.Sleep(samplingtime);//Samplingtime, defined in app.config
                    }
                    //break;
                }
                catch (Exception ex)
                {
                    if (DBloginTries < maxlogin)
                    {
                        int DisplayedTries = Convert.ToInt32(DBloginTries.ToString()) + 1; //since logintries starts on zero, declaring an ineger and adding 1 to the output of "logintries" for a better visual representation.
                        Console.WriteLine("Retrying DB connection " + DisplayedTries + " times..");

                        int[] readHoldingResisters = modbusclient.ReadHoldingRegisters(0, 1); // read 1 holding registers, starting with adress 1
                        Pressure.Enqueue(Convert.ToString(readHoldingResisters[0]));
                        var pre = Pressure.Dequeue();

                        File.AppendAllText(@"C:/Users/Jacob/Documents/Pressure.txt", pre + Environment.NewLine);

                        string error = @"C:/Users/Jacob/Documents/errorFile.txt";
                        using (StreamWriter stwr = new StreamWriter(error, true))//if the error file exists, append text. If not, it will generate a new errorfile
                        {
                            stwr.Write(string.Format("Error Message: {0}{1}StackTrace :{2}{1}Date :{3}{1}-----------------------------------------------------------------------------{1}"
                                , ex.Message, Environment.NewLine, ex.StackTrace, DateTime.Now.ToString()));
                        }
                        Thread.Sleep(3000);
                    }
                    else
                    {
                        Console.WriteLine("Application exiting..");
                        Environment.Exit(1);
                    }
                }
            }

        }
    }
}
