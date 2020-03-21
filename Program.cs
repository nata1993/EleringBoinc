using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using static System.Console;

//save user provided highest electricity price and use it for next program startup
//timestamp that follows o'clock timing no matter from what time program was started
//calculate spent electricity and its cost
//calculate total electricity used over time.
//check if BOINC is even installed on computer
//optional: send data to database

namespace BoincElectricity
{
    class EleringDataApi
    {
        public class Ee
        {
            public int Timestamp { get; set; }
            public double Price { get; set; }
        }
        public class Data
        {
            public List<Ee> Ee { get; set; }
        }
        public class EleringData
        {
            public Data Data { get; set; }
            public bool Status { get; set; }
        }
    }
    class CallElering
    {
        private protected readonly string eleringApiLink = "https://dashboard.elering.ee/api/nps/price";
        private protected string timeFromElering;
        private protected decimal priceFromElering;
        private protected int timestampFromElering;

        public string TimeFromElering { get { return timeFromElering; } set { timeFromElering = value; } }
        public decimal PriceFromElering { get { return priceFromElering; } set { priceFromElering = value; } }
        private int TimestampFromElering { get { return timestampFromElering; } set { timestampFromElering = value; } }
        private string FormatDateandTime(int timeStamp)
        {
            DateTime date = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(timeStamp).ToLocalTime();
            string formatedDate = date.ToString("dd.MM.yyyy HH:mm");
            return formatedDate;
        }
        public void GetApiData()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(eleringApiLink);
            request.Method = "GET";
            var webResponse = request.GetResponse();
            var webResponseStream = webResponse.GetResponseStream();
            using var responseReader = new StreamReader(webResponseStream);
            var response = responseReader.ReadToEnd();
            EleringDataApi.EleringData elering = JsonConvert.DeserializeObject<EleringDataApi.EleringData>(response);
            //add data to object
            TimestampFromElering = elering.Data.Ee[^1].Timestamp;
            TimeFromElering = FormatDateandTime(timestampFromElering);
            PriceFromElering = Convert.ToDecimal(elering.Data.Ee[^1].Price);
        }
        public void PublicGetApiData()
        {
            GetApiData();
        }
    }
    class Program
    {
        static void Main()
        {
            //SETUP 
            //console output encoding
            OutputEncoding = Encoding.UTF8;
            //parameters
            int retryCounter = 1;   //counter for elering data aquisition
            string userSpecifiedElectricityPrice; //user provided maximum electricity price he/she wants to run boinc at
            decimal numericalSpecifiedElectricityPrice; //numerical representation of user procided electricity price
            //create directory for program log
            Directory.CreateDirectory("C:\\BoincElectricity\\");
            File.Create("C:\\BoincElectricity\\Boinc-Electricity-Log.txt").Close();
            //objects and writers
            StreamWriter textWriter = new StreamWriter("C:\\BoincElectricity\\Boinc-Electricity-Log.txt", true);    //StreamWritter is adding data to log file, not overwriting
            CallElering elering = new CallElering();    //create object for elering time and price data
            Process boinc = new Process();  //create process of external program to be run

            boinc.StartInfo.UseShellExecute = false;    //start only executables e.g.: .exe
            boinc.StartInfo.FileName = "C:\\Program Files\\BOINC program\\boincmgr";    //program file path

            textWriter.WriteLine($"{DateTime.Now} - Setting up program ressources: Directory, StreamWriter, API object, Process object.");
            textWriter.Flush();

            //MAIN PROCESS LOOP
            //loop for electricity price check and program start up and shut down
            while (true)
            {
                //Ask user for electricity price and convert it to numerical representation
                try
                {
                    Write(" Please provide highest electricity price you want to run \n program in megawats per hour pricing (e.g 45 as in 45€/MWh): ");
                    userSpecifiedElectricityPrice = ReadLine();
                    if (userSpecifiedElectricityPrice.Contains("."))
                    {
                        userSpecifiedElectricityPrice = userSpecifiedElectricityPrice.Replace(".", ",");
                    }
                    numericalSpecifiedElectricityPrice = decimal.Parse(userSpecifiedElectricityPrice);

                    textWriter.WriteLine($"{DateTime.Now} - User provided electricity price: {userSpecifiedElectricityPrice}.");
                    textWriter.WriteLine($"{DateTime.Now} - User provided numerical translation of electricity price: {numericalSpecifiedElectricityPrice}.");
                    textWriter.Flush();

                    try
                    {
                        //ASK FOR ELECTRICITY DATA
                        textWriter.WriteLine($"{DateTime.Now} - Requested data from Elering.");
                        textWriter.Flush();

                        Clear();
                        WriteLine($" {retryCounter}) Getting data from Elering");
                        WriteLine(" =========================\n");
                        elering.PublicGetApiData();   //getting data from elering

                        try
                        {
                            //CHECK FOR ALL RUNNING PROCESSES
                            textWriter.WriteLine($"{DateTime.Now} - Requested data from Elering: {elering.TimeFromElering} : {elering.PriceFromElering}.");
                            textWriter.WriteLine($"{DateTime.Now} - Checking for running processes.");
                            textWriter.Flush();

                            PriceText(elering.TimeFromElering, elering.PriceFromElering);     //Show acquired data from Elering
                            WriteLine(" Checking for running boinc process ");
                            Process[] processList = Process.GetProcessesByName("BOINC");   //Search for running processes by name
                            string allRunningProcesses = processList[0].ToString();   //convert acquired process into string for later use

                            //CHECK FOR RUNNING EXTERNAL PROCESS
                            if (elering.PriceFromElering <= numericalSpecifiedElectricityPrice && allRunningProcesses is "System.Diagnostics.Process (boinc)")    //if process running and price is below specified, continue running boinc
                            {
                                textWriter.WriteLine($"{DateTime.Now} - Requested data from Elering was below user specified level. BOINC processes continued running.");
                                textWriter.Flush();

                                WriteLine(" Electricity price is still good!");
                                WriteLine(" Boinc will continue crunching numbers.");
                                //Thread.Sleep(elering.Timestamp + 3600000);  //stop process for one hour inorder to check electricity price again one hour later

                                Thread.Sleep(10000);
                            }
                            else
                            {
                                try
                                {
                                    textWriter.WriteLine($"{DateTime.Now} - Requested data from Elering was above user specified level.");
                                    textWriter.Flush();

                                    WriteLine(" Price is too high for cheap number crunching!");
                                    WriteLine(" Shutting down Boinc!");
                                    WriteLine(" Will check price again at next o'clock.");
                                    WriteLine(" Shutting BOINC down!");

                                    //closing BOINC processes the hard, not the best, way
                                    foreach (Process proc in Process.GetProcessesByName("BOINC"))
                                    {
                                        proc.Kill();
                                        boinc.WaitForExit();
                                    }
                                    textWriter.WriteLine($"{DateTime.Now} - Killed brutaly BOINC processes.");
                                    textWriter.Flush();
                                }
                                //if could not close boinc processes, this exception is thrown and program is closing while error writing to log
                                catch (Exception e)
                                {
                                    Clear();
                                    WriteLine(e);
                                    textWriter.WriteLine($"{DateTime.Now} - Error in closing BOINC processes.");
                                    textWriter.WriteLine($"{DateTime.Now} - {e}");
                                    textWriter.Flush();
                                    break;
                                }
                                //Thread.Sleep(elering.Timestamp + 3600000);  //stop process for one hour inorder to check electricity price again one hour later

                                Thread.Sleep(10000);
                            }
                        }
                        //EXTERNAL PROCESS IS NOT RUNNING
                        catch (IndexOutOfRangeException)    //if there is no process running, this exception is thrown and process is started
                        {
                            textWriter.WriteLine($"{DateTime.Now} - BOINC processes were not running.");
                            textWriter.Flush();

                            PriceText(elering.TimeFromElering, elering.PriceFromElering);     //Show aqcuired data from Elering
                            WriteLine(" Boinc is not crunching numbers.\n");

                            //START EXTERNAL PROCESS
                            if (elering.PriceFromElering <= numericalSpecifiedElectricityPrice)   //if price is below 45€/MWh and got data from elering, proceed to start process
                            {
                                textWriter.WriteLine($"{DateTime.Now} - Started BOINC.");
                                textWriter.Flush();

                                boinc.Start();  //start process based on previous setup
                                Thread.Sleep(13000);    //wait 13 seconds for BOINC program to connect to internet and get data from internet
                                boinc.CloseMainWindow();    //close program window automatically to tray

                                textWriter.WriteLine($"{DateTime.Now} - Closed BOINC to tray.");
                                textWriter.Flush();

                                WriteLine(" BOINC started crunching numbers!");
                                WriteLine(" Will check price again at next o'clock.\n");
                                //Thread.Sleep(elering.Timestamp + 3600000);  //stop process for one hour inorder to check electricity price again one hour later

                                Thread.Sleep(10000);
                            }
                            //WAIT FOR SUITABLE MOMENT TO START PROCESS
                            else
                            {
                                textWriter.WriteLine($"{DateTime.Now} - Requested data from Elering was above user specified level. BOINC was not started.");
                                textWriter.Flush();

                                WriteLine(" Price is still too high for cruncing numbers!");
                                WriteLine(" Will check price again at next o'clock.\n");
                                //Thread.Sleep(elering.Timestamp + 3600000);  //stop process for one hour inorder to check electricity price again one hour later

                                Thread.Sleep(10000);
                            }
                        }
                    }
                    //IF NO ELECTRICITY DATA, TRY AGAIN
                    catch (WebException)
                    {
                        WriteLine(" Could not get Elering data from internet.\n");
                        retryCounter++;

                        textWriter.WriteLine($"{DateTime.Now} - Program could not get data from Elering.");
                        textWriter.Flush();

                        Thread.Sleep(5000);
                    }
                }
                catch (Exception)
                {
                    textWriter.WriteLine($"{DateTime.Now} - User provided electricity price in incorrect format.");
                    textWriter.Flush();

                    Clear();
                    WriteLine(" Please insert valid number.\n");
                }
            }
            ReadLine();
        }
        static void PriceText(string _time, decimal _price)
        {
            Clear();
            WriteLine(" Got data from Elering!\n");
            WriteLine(" Electricity price right now");
            WriteLine(" =========================\n");
            WriteLine($" {_time} : {_price} €/MWh\n");
        }
    }
}