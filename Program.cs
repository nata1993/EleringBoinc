using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using static System.Console;

//save user provided highest electricity price and use it for next program startup
//calculate spent electricity and its cost
//calculate total electricity used over time.
//check if BOINC is even installed on computer
//optional: send data to database
//optinal: create release notes from within program

namespace BoincElectricity
{
    class Elering
    {
        private protected readonly string eleringApiLink = "https://dashboard.elering.ee/api/nps/price";
        private protected string timeFromElering;       //time from elering converted to human readable date and time
        private protected decimal priceFromElering;     //price from elering without taxes
        private protected int timestampFromElering;     //timestamp from elering

        public string TimeFromElering { get { return timeFromElering; }  }
        public decimal PriceFromElering { get { return priceFromElering; }  }
        private string FormatDateandTime(int timeStamp)
        {
            DateTime date = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(timeStamp).ToLocalTime();
            string formatedDate = date.ToString("dd.MM.yyyy HH:mm");
            return formatedDate;
        }
        //Get data from elering
        private void GetApiData()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(eleringApiLink);
            request.Method = "GET";
            var webResponse = request.GetResponse();
            var webResponseStream = webResponse.GetResponseStream();
            using var responseReader = new StreamReader(webResponseStream);
            var response = responseReader.ReadToEnd();
            EleringDataApi.EleringData elering = JsonConvert.DeserializeObject<EleringDataApi.EleringData>(response);
            //add data to object
            timestampFromElering = elering.Data.Ee[^1].Timestamp;
            timeFromElering = FormatDateandTime(timestampFromElering);
            priceFromElering = Convert.ToDecimal(elering.Data.Ee[^1].Price);
        }
        public void PublicGetApiData()
        {
            GetApiData();
        }
        //Calculate how many seconds remain till next o'clock
        private int RemainingSecondsTillNextHour()
        {
            DateTime nextDtElering = new DateTime(1970, 1, 1, 0, 0, 0).AddHours(1).AddSeconds(timestampFromElering).ToLocalTime();  //create time from elering timestamp + 1 hour (next hour)
            DateTime dtNow = DateTime.Now;  //create time at the moment
            TimeSpan result = nextDtElering.Subtract(dtNow); //substract time at the moment from next hour elering timestamp
            return Convert.ToInt32((result.TotalSeconds * 1000) + 10000); //convert substraction result to timestamp in millisecondsand and add 10 000 milliseconds (10 seconds)
        }

        public int UpdateRemainingSecondsTillOClock()
        {
            return RemainingSecondsTillNextHour();
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
            int secondsTillNextHour;
            string userSpecifiedElectricityPrice; //user provided maximum electricity price he/she wants to run boinc at
            decimal numericalSpecifiedElectricityPrice; //numerical representation of user procided electricity price
            //create directory for program log
            Directory.CreateDirectory("C:\\BoincElectricity\\");
            //check if file exists, else create one
            if (!File.Exists("C:\\BoincElectricity\\Boinc-Electricity-User-Settings.txt"))
            {
                File.Create("C:\\BoincElectricity\\Boinc-Electricity-User-Settings.txt").Close();
            }
            //objects and writers
            StreamWriter textWriter = new StreamWriter("C:\\BoincElectricity\\Boinc-Electricity-Log.txt", true);    //StreamWritter is adding data to log file, not overwriting
            Elering elering = new Elering();    //create object for elering time and price data
            Process boinc = new Process();  //create process of external program to be run

            boinc.StartInfo.UseShellExecute = false;    //start only executables e.g.: .exe
            boinc.StartInfo.FileName = "C:\\Program Files\\BOINC program\\boincmgr";    //program file path

            //log
            textWriter.WriteLine($"{DateTime.Now} - ========================== NEW PROGRAM STARTUP ====================================");
            textWriter.WriteLine($"{DateTime.Now} - Setting up program ressources: Directory, StreamWriter, API object, Process object.");
            textWriter.Flush();

            //ASK FOR USER INPUT
            while (true)
            {
                try
                {
                    Write(" Please provide highest electricity price you want to run \n program in megawats per hour pricing (e.g 45 as in 45€/MWh): ");
                    userSpecifiedElectricityPrice = ReadLine();
                    if (userSpecifiedElectricityPrice.Contains("."))
                    {
                        userSpecifiedElectricityPrice = userSpecifiedElectricityPrice.Replace(".", ",");
                    }
                    numericalSpecifiedElectricityPrice = decimal.Parse(userSpecifiedElectricityPrice);
                    //log
                    textWriter.WriteLine($"{DateTime.Now} - -----------------------------");
                    textWriter.WriteLine($"{DateTime.Now} - User provided electricity price: {userSpecifiedElectricityPrice}.");
                    textWriter.WriteLine($"{DateTime.Now} - User provided numerical translation of electricity price: {numericalSpecifiedElectricityPrice}.");
                    textWriter.Flush();
                    break;
                }
                catch (Exception)
                {
                    Clear();
                    WriteLine(" Please insert valid number.\n");
                    //log
                    textWriter.WriteLine($"{DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    textWriter.WriteLine($"{DateTime.Now} - User provided electricity price in incorrect format.");
                    textWriter.Flush();
                }
            }

            //MAIN PROCESS LOOP
            //loop for electricity price check and program start up and shut down
            while (true)
            {
                try
                {
                    //ASK FOR ELECTRICITY DATA
                    Clear();
                    WriteLine($" {retryCounter}) Requesting data from Elering");
                    WriteLine(" =========================\n");
                    //log
                    textWriter.WriteLine($"{DateTime.Now} - -----------------------------");
                    textWriter.WriteLine($"{DateTime.Now} - Requesting data from Elering.");
                    textWriter.Flush();

                    elering.PublicGetApiData();   //getting data from elering
                    secondsTillNextHour = elering.UpdateRemainingSecondsTillOClock();

                    try
                    {
                        //CHECK FOR ALL RUNNING PROCESSES
                        PriceText(elering.TimeFromElering, elering.PriceFromElering);     //Show acquired data from Elering
                        WriteLine(" Checking for running boinc process ");
                        //log
                        textWriter.WriteLine($"{DateTime.Now} - -----------------------------");
                        textWriter.WriteLine($"{DateTime.Now} - Calculated seconds till next o'clock: {secondsTillNextHour / 1000}.");
                        textWriter.WriteLine($"{DateTime.Now} - Requested data from Elering: {elering.TimeFromElering} : {elering.PriceFromElering}.");
                        textWriter.WriteLine($"{DateTime.Now} - Checking for running processes.");
                        textWriter.Flush();

                        Process[] processList = Process.GetProcessesByName("BOINC");   //Search for running processes by name
                        string allRunningProcesses = processList[0].ToString();   //convert acquired process into string for later use

                        //CHECK FOR RUNNING EXTERNAL PROCESS
                        if (elering.PriceFromElering <= numericalSpecifiedElectricityPrice && allRunningProcesses is "System.Diagnostics.Process (boinc)")    //if process running and price is below specified, continue running boinc
                        {
                            WriteLine(" Electricity price is still good!");
                            WriteLine(" Boinc will continue crunching numbers.");
                            //log
                            textWriter.WriteLine($"{DateTime.Now} - -----------------------------");
                            textWriter.WriteLine($"{DateTime.Now} - BOINC process is running.");
                            textWriter.WriteLine($"{DateTime.Now} - Requested data from Elering was below user specified level. BOINC processes continued running.");
                            textWriter.Flush();
                            Thread.Sleep(secondsTillNextHour);  //stop process for one hour inorder to check electricity price again one hour later
                        }
                        else
                        {
                            try
                            {
                                WriteLine(" Price is too high for cheap number crunching!");
                                WriteLine(" Will check price again at next o'clock.");
                                WriteLine(" Shutting BOINC down!");
                                //log
                                textWriter.WriteLine($"{DateTime.Now} - -----------------------------");
                                textWriter.WriteLine($"{DateTime.Now} - Requested data from Elering was above user specified level.");
                                textWriter.Flush();

                                //closing BOINC processes the hard, not the best, way
                                foreach (Process proc in Process.GetProcessesByName("BOINC"))
                                {
                                    proc.Kill();
                                    boinc.WaitForExit();
                                }
                                //log
                                textWriter.WriteLine($"{DateTime.Now} - Killed brutaly BOINC processes.");
                                textWriter.Flush();
                                Thread.Sleep(secondsTillNextHour);  //stop process for one hour inorder to check electricity price again one hour later
                            }
                            //if could not close boinc processes, this exception is thrown and program is closing while error writing to log
                            catch (Exception e)
                            {
                                Clear();
                                WriteLine(e);
                                //log
                                textWriter.WriteLine($"{DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                                textWriter.WriteLine($"{DateTime.Now} - Error in closing BOINC processes.");
                                textWriter.WriteLine($"{DateTime.Now} - {e}");
                                textWriter.Flush();
                                break;
                            }
                        }
                    }
                    //EXTERNAL PROCESS IS NOT RUNNING
                    catch (IndexOutOfRangeException)    //if there is no process running, this exception is thrown and process is started
                    {
                        PriceText(elering.TimeFromElering, elering.PriceFromElering);     //Show aqcuired data from Elering
                        WriteLine(" Boinc is not crunching numbers.\n");
                        //log
                        textWriter.WriteLine($"{DateTime.Now} - -----------------------------");
                        textWriter.WriteLine($"{DateTime.Now} - BOINC processes were not running.");
                        textWriter.Flush();

                        //START EXTERNAL PROCESS
                        if (elering.PriceFromElering <= numericalSpecifiedElectricityPrice)   //if price is below 45€/MWh and got data from elering, proceed to start process
                        {
                            //log
                            textWriter.WriteLine($"{DateTime.Now} - Started BOINC.");
                            textWriter.Flush();

                            boinc.Start();  //start process based on previous setup
                            Thread.Sleep(15000);    //wait 15 seconds for BOINC program to connect to internet and get data from internet
                            boinc.CloseMainWindow();    //close program window automatically to tray

                            secondsTillNextHour = elering.UpdateRemainingSecondsTillOClock();     //update remaining seconds till next o'clock
                            
                            WriteLine(" BOINC started crunching numbers!");
                            WriteLine(" Will check price again at next o'clock.");
                            //log
                            textWriter.WriteLine($"{DateTime.Now} - Closed BOINC to tray.");
                            textWriter.Flush();
                            Thread.Sleep(secondsTillNextHour);  //stop process for one hour inorder to check electricity price again one hour later
                        }
                        //WAIT FOR SUITABLE MOMENT TO START PROCESS
                        else
                        {
                            WriteLine(" Price is still too high for cruncing numbers!");
                            WriteLine(" Will check price again at next o'clock.");
                            //log
                            textWriter.WriteLine($"{DateTime.Now} - Requested data from Elering was above user specified level. BOINC was not started.");
                            textWriter.Flush();
                            Thread.Sleep(secondsTillNextHour);  //stop process for one hour inorder to check electricity price again one hour later
                        }
                    }
                    catch (Exception e)
                    {
                        textWriter.WriteLine(e);
                    }
                }
                //IF NO ELECTRICITY DATA, TRY AGAIN
                catch (WebException)
                {
                    WriteLine(" Could not get Elering data from internet.\n");
                    //log
                    textWriter.WriteLine($"{DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    textWriter.WriteLine($"{DateTime.Now} - Program could not get data from Elering.");
                    textWriter.Flush();
                    retryCounter++;
                    Thread.Sleep(5000);
                }
            }
            ReadLine();
        }
        static void PriceText(string _time, decimal _price)
        {
            Clear();
            WriteLine(" Requested data from Elering!\n");
            WriteLine(" Electricity price right now");
            WriteLine(" =========================\n");
            WriteLine($" {_time} : {_price} €/MWh\n");
        }
    }
}