using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using static System.Console;

//calculate spent electricity and its cost
//calculate total electricity used over time.
//check if BOINC is even installed on computer
//optional: send data to database

namespace BoincElectricity
{
    class Elering
    {
        private protected readonly string eleringApiLink = "https://dashboard.elering.ee/api/nps/price";
        private protected string timeFromElering;       //time from elering converted to human readable date and time
        private protected decimal priceFromElering;     //price from elering without taxes
        private protected int timestampFromElering;     //timestamp from elering

        string EleringApiLink { get { return eleringApiLink; } }
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
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(EleringApiLink);
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
        //parameters
        protected int retryCounter = 1;                                                     //counter for elering data aquisition
        protected int secondsTillNextHour;
        private protected string userProvidedElectricityPrice;                             //user provided maximum electricity price he/she wants to run boinc at
        private protected decimal numericalUserProvidedElectricityPrice;                   //numerical representation of user procided electricity price
        protected bool savedPrice;
        //file names and paths
        static private protected string mainDirectory = @"C:\BoincElectricity\";           //main directory for log and settings
        static private protected string logFile = "Boinc-Electricity-Log.txt";
        static private protected string settingsFile = "Boinc-Electricity-User-Settings.txt";
        static private protected string releaseNotesFile = "Boinc-Electricity-Release-Notes.txt";
        private protected string boincProgram = @"C:\Program Files\BOINC program\boincmgr";
        static void Main()
        {
            //SETUP 
            OutputEncoding = Encoding.UTF8;
            CreateDirectories();
            //objects and writers
            StreamWriter logWriter = new StreamWriter(mainDirectory + logFile, true);       //StreamWritter is adding data to log file, not overwriting
            Program mainProgram = new Program();                                            //Create object of main program
            Elering elering = new Elering();                                                //create object for elering time and price data
            Process boinc = new Process();                                                  //create process of external program to be run
                    boinc.StartInfo.UseShellExecute = false;                                //start only executables e.g.: .exe
                    boinc.StartInfo.FileName = mainProgram.boincProgram;                    //program to be started file path
            //log
            logWriter.WriteLine($" {DateTime.Now} - ========================== NEW PROGRAM STARTUP ====================================\n" +
                                $" {DateTime.Now} - Setting up program ressources: Directory, StreamWriter, API object, Process object.");
            logWriter.Flush();
            //read settings file for saved data
            mainProgram.savedPrice = decimal.TryParse(File.ReadAllText(mainDirectory + settingsFile).ToString(), 
                                                      out decimal convertedResult);
            if (!mainProgram.savedPrice)
            {
                //ASK FOR USER INPUT
                while (true)
                {
                    try
                    {
                        Write(" Please provide highest electricity price you want to run\n program in megawats per hour pricing (e.g 45 as in 45€/MWh): ");
                        mainProgram.userProvidedElectricityPrice = ReadLine();
                        mainProgram.numericalUserProvidedElectricityPrice = decimal.Parse(mainProgram.userProvidedElectricityPrice.Replace(".", ","));
                        File.WriteAllText(mainDirectory + settingsFile, mainProgram.userProvidedElectricityPrice);
                        //log
                        logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                            $" {DateTime.Now} - User provided electricity price: {mainProgram.userProvidedElectricityPrice}.\n" +
                                            $" {DateTime.Now} - User provided numerical translation of electricity price: {mainProgram.numericalUserProvidedElectricityPrice}.\n" +
                                            $" {DateTime.Now} - Saved user provided numerical translation of electricity price to settings file.");
                        logWriter.Flush();
                        break;
                    }
                    catch (Exception)
                    {
                        Clear();
                        WriteLine(" Please insert valid number.\n");
                        //log
                        logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                            $" {DateTime.Now} - User provided electricity price in incorrect format.");
                        logWriter.Flush();
                    }
                }
            }
            else
            {
                WriteLine("Using previously saved electricity price level.");
                //log
                logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                    $" {DateTime.Now} - Sucsessfully read user saved setting from file.\n" +
                                    $" {DateTime.Now} - Using previously saved electricity price setting.");
                logWriter.Flush();
                mainProgram.numericalUserProvidedElectricityPrice = convertedResult;
                Thread.Sleep(2000);                                                         //wait two seconds for user to read
            }
            //MAIN PROCESS LOOP
            //loop for electricity price check and program start up and shut down
            while (true)
            {
                try
                {
                    //ASK FOR ELECTRICITY DATA
                    Clear();
                    WriteLine($" {mainProgram.retryCounter}) Requesting data from Elering\n" +
                               " =========================\n");
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                        $" {DateTime.Now} - Requesting data from Elering.");
                    logWriter.Flush();

                    elering.PublicGetApiData();                                             //getting data from elering
                    mainProgram.secondsTillNextHour = elering.UpdateRemainingSecondsTillOClock();

                    try
                    {
                        //CHECK FOR ALL RUNNING PROCESSES
                        mainProgram.retryCounter = 1;
                        WritePriceText(elering.TimeFromElering, elering.PriceFromElering);  //print acquired data from Elering
                        WriteLine(" Checking for running boinc process ");
                        //log
                        logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                            $" {DateTime.Now} - Calculated seconds till next o'clock: {mainProgram.secondsTillNextHour / 1000}.\n" +    //calculate milliseconds into seconds
                                            $" {DateTime.Now} - Requested data from Elering: {elering.TimeFromElering} : {elering.PriceFromElering}.\n" +
                                            $" {DateTime.Now} - Checking for running processes.");
                        logWriter.Flush();

                        Process[] processList = Process.GetProcessesByName("BOINC");        //Search for running processes by name
                        string allRunningProcesses = processList[0].ToString();             //convert acquired process into string for later use
                        Thread.Sleep(2000);                                                 //wait for two seconds for user to read

                        //CHECK FOR RUNNING EXTERNAL PROCESS
                        //if price is below specified and process is running, continue running boinc
                        if (elering.PriceFromElering <= mainProgram.numericalUserProvidedElectricityPrice && 
                            allRunningProcesses is "System.Diagnostics.Process (boinc)")
                        {
                            WriteLine(" Electricity price is still good!\n" +
                                      " Boinc will continue crunching numbers.");
                            //log
                            logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                                $" {DateTime.Now} - BOINC process is running.\n" +
                                                $" {DateTime.Now} - Requested data from Elering was below user specified level. BOINC processes continued running.");
                            logWriter.Flush();
                            Thread.Sleep(mainProgram.secondsTillNextHour);                  //stop process for one hour inorder to check electricity price again one hour later
                        }
                        else
                        {
                            try
                            {
                                WriteLine(" Price is too high for cheap number crunching!\n Will check price again at next o'clock.\n" +
                                          " Shutting BOINC down!");
                                //log
                                logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                                    $" {DateTime.Now} - Requested data from Elering was above user specified level.");
                                logWriter.Flush();
                                //closing BOINC processes the hard, not the best, way
                                foreach (Process proc in Process.GetProcessesByName("BOINC"))
                                {
                                    proc.Kill();
                                    boinc.WaitForExit();
                                }
                                //log
                                logWriter.WriteLine($" {DateTime.Now} - Killed brutaly BOINC processes.");
                                logWriter.Flush();
                                Thread.Sleep(mainProgram.secondsTillNextHour);              //stop process for one hour inorder to check electricity price again one hour later
                            }
                            //if could not close boinc processes, this exception is thrown and program is closing while error is written to log
                            catch (Exception e)
                            {
                                Clear();
                                WriteLine("Something went wrong!");
                                //log
                                logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                                    $" {DateTime.Now} - Error in closing BOINC processes.\n" +
                                                    $" {DateTime.Now} - {e}");
                                logWriter.Flush();
                                Thread.Sleep(15000);
                                break;
                            }
                        }
                    }
                    //EXTERNAL PROCESS IS NOT RUNNING
                    catch (IndexOutOfRangeException)    //if there is no process running, this exception is thrown and process is started
                    {
                        WritePriceText(elering.TimeFromElering, elering.PriceFromElering);     //Show aqcuired data from Elering
                        WriteLine(" Boinc is not crunching numbers.\n");
                        //log
                        logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                             $" {DateTime.Now} - BOINC processes were not running.");
                        logWriter.Flush();

                        //START EXTERNAL PROCESS
                        if (elering.PriceFromElering <= mainProgram.numericalUserProvidedElectricityPrice)   //if price is below 45€/MWh and got data from elering, proceed to start process
                        {
                            //log
                            logWriter.WriteLine($"{DateTime.Now} - Started BOINC.");
                            logWriter.Flush();

                            boinc.Start();  //start process based on previous setup
                            Thread.Sleep(15000);    //wait 15 seconds for BOINC program to connect to internet and get data from internet
                            boinc.CloseMainWindow();    //close program window automatically to tray

                            mainProgram.secondsTillNextHour = elering.UpdateRemainingSecondsTillOClock();     //update remaining seconds till next o'clock
                            
                            WriteLine(" BOINC started crunching numbers!\n" +
                                      " Will check price again at next o'clock.");
                            //log
                            logWriter.WriteLine($"{DateTime.Now} - Closed BOINC to tray.");
                            logWriter.Flush();
                            Thread.Sleep(mainProgram.secondsTillNextHour);  //stop process for one hour inorder to check electricity price again one hour later
                        }
                        //WAIT FOR SUITABLE MOMENT TO START PROCESS
                        else
                        {
                            WriteLine(" Price is still too high for cruncing numbers!\n" +
                                      " Will check price again at next o'clock.");
                            //log
                            logWriter.WriteLine($"{DateTime.Now} - Requested data from Elering was above user specified level. BOINC was not started.");
                            logWriter.Flush();
                            Thread.Sleep(mainProgram.secondsTillNextHour);  //stop process for one hour inorder to check electricity price again one hour later
                        }
                    }
                    catch (Exception e)
                    {
                        logWriter.WriteLine(e);
                    }
                }
                //IF NO ELECTRICITY DATA, TRY AGAIN
                catch (WebException)
                {
                    WriteLine(" Could not get Elering data from internet.\n");
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                        $" {DateTime.Now} - Program could not get data from Elering.");
                    logWriter.Flush();
                    mainProgram.retryCounter++;
                    Thread.Sleep(5000);                                                     //retry every five seconds
                }
            }
            ReadLine();
        }
        static void WritePriceText(string _time, decimal _price)
        {
            Clear();
            WriteLine(" Requested data from Elering!\n Electricity price right now\n" +
                     $" =========================\n {_time} : {_price} €/MWh\n");
        }
        static void CreateDirectories()
        {
            if (!Directory.Exists(mainDirectory))
            {
                Directory.CreateDirectory(mainDirectory);
            }
            if (!File.Exists(mainDirectory + settingsFile) ||
                !File.Exists(mainDirectory + releaseNotesFile))
            {
                File.Create(mainDirectory + settingsFile).Close();
                File.Create(mainDirectory + releaseNotesFile).Close();
                CreateReleaseNotes(mainDirectory + releaseNotesFile);
            }
        }
        static void CreateReleaseNotes(string path)
        {
            string releaseNotes =
                " ! - bug\n ? - improvement\n * - update\n" +
                " ======\n v1.3.2\n ______\n" +
                " ! - Fixed bug where release notes file was not created.\n" +
                " * - Main program is now object as well as main method parameters are hidden.\n" +
                "   - Removed extra method for checking if user inputed decimal number with comma or dot and made such checking\n" +
                "     easier in more suitable place as one liner code.\n" +
                " ? - Rewritten code for easier maintainability and reading.\n" +
                " ======\n v1.3.1\n ______\n" +
                " ? - Updated code for easier maintainability.\n" +
                " ======\n v1.3.0\n ______\n" +
                " * - Created method for checking if user previously provided electricity price level.If not, ask\n" +
                "     user to provide such data and save it to file for later use by program.\n" +
                " ? - Rewritten code little bit for easier maintainability and testing aswell as bug tracking.\n" +
                " ======\n v1.2.2\n ______\n" +
                " ? - Minor improvements.\n" +
                " ======\n v1.2.1\n ______\n" +
                " ! - Fixed bug where program crashed when o'clock happened and program asked from Elering electricity\n" +
                "     price but was unable to acquire data.Implemented 10 seconds buffer time for such occurence.\n" +
                " ======\n v1.2.0\n ______\n" +
                " ! - Fixed Elering data setters bug where Elering data could be modified in the main program method.\n" +
                " * - Added method for calculating remaining seconds till next o'clock with additional 5 seconds for \n" +
                "     confirmed data acquisition from Elering.\n" +
                " ? - Rewritten code for easier reading of program text and log text.\n" +
                " ======\n v1.1.1\n ______\n" +
                " ? - Moved method of user input for electricity price level for asking only once when program starts.\n" +
                " ======\n v1.1.0\n ______\n" +
                " * - Added mechanism for writing working and error log of program.\n" +
                " ======\n v1.0.3\n ______\n" +
                " * - Added method that distinguishes between user inputed value whenever it is written with comma or dot.\n" +
                "     Method also replaces input with dot to input with comma.\n" +
                " ======\n v1.0.2\n ______\n" +
                " * - Added method for user to provide electricity price level from which program will turn BOINC on or off.\n" +
                " ======\n v1.0.1\n ______\n" +
                " ? - Minor language mistakes corrected.\n" +
                " ======\n v1.0.0\n ______\n" +
                " * -Initial release or Console Application.\n";
            File.WriteAllText(path, releaseNotes);
        }
    }
}