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

        public int CalculateRemainingSecondsTillOClock()
        {
            return RemainingSecondsTillNextHour();
        }
    }
    class Program
    {
        //parameters
        protected int retryCounter = 1;                                                     //counter for elering data aquisition
        protected int secondsTillOClock;
        private protected decimal userProvidedElectricityPrice;                             //user provided maximum electricity price he/she wants to run boinc at
        protected bool savedPrice;
        private protected string allRunningProcesses;
        private protected bool mainLoop = true;
        //file names and paths
        static private protected string mainDirectory = @"C:\BoincElectricity\";            //main directory for log and settings
        static private protected string logFile = "Boinc-Electricity-Log.txt";
        static private protected string settingsFile = "Boinc-Electricity-User-Settings.txt";
        static private protected string releaseNotesFile = "Boinc-Electricity-Release-Notes.txt";
        private protected string boincProgram = @"C:\Program Files\BOINC program\boincmgr";
        static void Main()
        {
            //SETUP
            ProgramSetUp();
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
            //read settings file for saved data - if tryparse fails e.g false, ask user to provide baseline price
            mainProgram.savedPrice = decimal.TryParse(File.ReadAllText(mainDirectory + settingsFile).ToString(), 
                                                      out decimal convertedResult);
            //ASK FOR USER INPUT
            if (!mainProgram.savedPrice)
            {
                CursorVisible = true;
                while (true)
                {
                    try
                    {
                        Write(" Please provide baseline electricity price you want to run\n program in megawatts per hour pricing (e.g 45 as in 45€/MWh): ");
                        mainProgram.userProvidedElectricityPrice = decimal.Parse(ReadLine().Replace(".", ","));
                        if (mainProgram.userProvidedElectricityPrice <= 0)
                        {
                            throw new ArgumentException("Zero or negative number user input!");
                        }
                        File.WriteAllText(mainDirectory + settingsFile, mainProgram.userProvidedElectricityPrice.ToString());
                        //log
                        logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                            $" {DateTime.Now} - User provided electricity price: {mainProgram.userProvidedElectricityPrice}.\n" +
                                            $" {DateTime.Now} - User provided numerical translation of electricity price: {mainProgram.userProvidedElectricityPrice}.\n" +
                                            $" {DateTime.Now} - Saved user provided numerical translation of electricity price to settings file.");
                        break;
                    }
                    catch (FormatException)
                    {
                        Clear();
                        WriteLine(" Please insert valid number.\n");
                        //log
                        logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                            $" {DateTime.Now} - User provided baseline electricity price in incorrect format" +
                                            $" {DateTime.Now} - or there was no input at all.");
                    }
                    catch (ArgumentException)
                    {
                        Clear();
                        WriteLine(" You must provide number that is positive signed number e.g. not zero and not with minus sign.\n");
                        //log
                        logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                            $" {DateTime.Now} - User provided zero or negative electricity price.");
                    }
                }
                CursorVisible = false;                                                      //turn off cursor after user inputed electricity price
            }
            else
            {
                WriteLine(" Using previously saved electricity price level.");
                //log
                logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                    $" {DateTime.Now} - Successfully read user saved setting from file.\n" +
                                    $" {DateTime.Now} - Using previously saved electricity price setting.");
                mainProgram.userProvidedElectricityPrice = convertedResult;                 //asign user provided setting from settings file
                Thread.Sleep(2000);                                                         //wait two seconds for user to read
            }
            logWriter.Flush();                                                              //flush all the log from user input to the log file
            //LOOP FOR PROGRAM
            while (mainProgram.mainLoop)
            {
                //REQUEST FOR ELECTRICITY DATA FROM ELERING
                while (true)
                {
                    try
                    {
                        Clear();
                        WriteLine($" {mainProgram.retryCounter}) Requesting data from Elering\n" +
                                   " =========================\n");
                        //log
                        logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                            $" {DateTime.Now} - Requesting data from Elering.");

                        elering.PublicGetApiData();                                         //getting data from elering
                        mainProgram.secondsTillOClock = elering.CalculateRemainingSecondsTillOClock();
                        break;
                    }
                    catch (WebException)
                    {
                        WriteLine(" Could not get Elering data from internet.\n Please check your internet connection.");
                        //log
                        logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                            $" {DateTime.Now} - Program could not get data from Elering.");
                        mainProgram.retryCounter++;
                        Thread.Sleep(5000);                                                 //retry every five seconds
                    }
                }
                logWriter.Flush();                                                          //flush logs from Elering data request
                //CHECK FOR RUNNING PROCESSES
                WritePriceText(elering.TimeFromElering, elering.PriceFromElering);          //print acquired data from Elering
                WriteLine(" Checking for running boinc processes.\n");
                //log
                logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                    $" {DateTime.Now} - Calculated seconds till next o'clock: {mainProgram.secondsTillOClock / 1000}.\n" +    //calculate milliseconds into seconds
                                    $" {DateTime.Now} - Requested data from Elering: {elering.TimeFromElering} : {elering.PriceFromElering}.\n" +
                                    $" {DateTime.Now} - Checking for running processes.");
                logWriter.Flush();
                mainProgram.retryCounter = 1;                                               //reset retry counter
                try
                {
                    Process[] processList = Process.GetProcessesByName("BOINC");            //Search for running processes by name
                    mainProgram.allRunningProcesses = processList[0].ToString();            //convert acquired process into string for later use
                    Thread.Sleep(2000);                                                     //wait for two seconds for user to read
                }
                catch (IndexOutOfRangeException)
                {
                    mainProgram.allRunningProcesses = "-1";                                 //save index out of range for later use
                    WritePriceText(elering.TimeFromElering, elering.PriceFromElering);      //Show aqcuired data from Elering
                    WriteLine(" Boinc is not crunching numbers.\n");
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                        $" {DateTime.Now} - BOINC processes were not running.");
                    logWriter.Flush();
                }
                //CHECK FOR RUNNING EXTERNAL PROCESS
                //if there is no process running, BOINC process is started
                if (mainProgram.allRunningProcesses == "-1")
                {
                    WritePriceText(elering.TimeFromElering, elering.PriceFromElering);      //Show aqcuired data from Elering
                    WriteLine(" Boinc is not crunching numbers.\n");
                    //START EXTERNAL PROCESS IF PRICE IS GOOD
                    if (elering.PriceFromElering <= mainProgram.userProvidedElectricityPrice)   //if price is below 45€/MWh and got data from elering, proceed to start process
                    {
                        try
                        {
                            //log
                            logWriter.WriteLine($" {DateTime.Now} - Started BOINC.");
                            logWriter.Flush();

                            boinc.Start();                                                  //start process based on previous setup
                            Thread.Sleep(15000);                                            //wait 15 seconds for BOINC program to connect to internet and get data from internet
                            boinc.CloseMainWindow();                                        //close program window automatically to tray

                            mainProgram.secondsTillOClock = elering.CalculateRemainingSecondsTillOClock();     //update remaining seconds till next o'clock

                            WriteLine(" BOINC started crunching numbers!\n" +
                                      " Will check price again at next o'clock.");
                            //log
                            logWriter.WriteLine($" {DateTime.Now} - Closed BOINC to tray.");
                            logWriter.Flush();
                            Thread.Sleep(mainProgram.secondsTillOClock);                    //stop process for one hour inorder to check electricity price again one hour later
                        }
                        catch (Exception e)
                        {
                            WriteLine(" Could not start BOINC for some reason! Please check log for error.");
                            //log
                            logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                                $" {DateTime.Now} - Could not start BOINC program for some reason.\n" +
                                                $" {e}");
                            logWriter.Flush();
                            mainProgram.mainLoop = false;                                   //upon massive error, stop main loop
                        }
                    }
                    //WAIT FOR GOOD PRICE TO START PROCESS
                    else
                    {
                        WriteLine(" Price is still too high for cruncing numbers!\n" +
                                  " Will check price again at next o'clock.");
                        //log
                        logWriter.WriteLine($"{DateTime.Now} - Requested data from Elering was above user specified level. BOINC was not started.");
                        logWriter.Flush();
                        Thread.Sleep(mainProgram.secondsTillOClock);                        //stop process for one hour inorder to check electricity price again one hour later
                    }
                }
                //if BOINC is already running
                else if (mainProgram.allRunningProcesses == "System.Diagnostics.Process (boinc)")
                {
                    //IF PRICE IS GOOD
                    if (elering.PriceFromElering <= mainProgram.userProvidedElectricityPrice)
                    {
                        WriteLine(" BOINC is running.\n" +
                                  " Electricity price is still good!\n" +
                                  " Boinc will continue crunching numbers.");
                        //log
                        logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                            $" {DateTime.Now} - BOINC process is running.\n" +
                                            $" {DateTime.Now} - Requested data from Elering was below user specified level. BOINC processes continued running.");
                        logWriter.Flush();
                        Thread.Sleep(mainProgram.secondsTillOClock);                        //stop process for one hour inorder to check electricity price again one hour later
                    }
                    //IF PRICE IS BAD, TERMINATE BOINC
                    else
                    {
                        try
                        {
                            WriteLine(" Price is too high for cheap number crunching!\n Will check price again at next o'clock.\n" +
                                      " Shutting BOINC down!");
                            //log
                            logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                                $" {DateTime.Now} - Requested data from Elering was above user specified level.");
                            //closing BOINC processes the hard, not the best, way
                            foreach (Process proc in Process.GetProcessesByName("BOINC"))
                            {
                                proc.Kill();
                                boinc.WaitForExit();
                            }
                            //log
                            logWriter.WriteLine($" {DateTime.Now} - Killed brutaly BOINC processes.");
                            logWriter.Flush();
                            Thread.Sleep(mainProgram.secondsTillOClock);                    //stop process for one hour inorder to check electricity price again one hour later
                        }
                        //if could not kill boinc processes, this exception is thrown and program is closing while error is written to log
                        catch (Exception e)
                        {
                            Clear();
                            WriteLine(" Something went wrong! Please check log.");
                            //log
                            logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                                $" {DateTime.Now} - Error in closing BOINC processes.\n" +
                                                $" {DateTime.Now} - {e}");
                            logWriter.Flush();
                            Thread.Sleep(15000);
                        }
                    }
                }
            }
            ReadLine();
        }
        static void ProgramSetUp()
        {
            //setup console window
            OutputEncoding = Encoding.UTF8;
            SetWindowSize(65, 15);
            BufferWidth = 65;
            CursorVisible = false;                                                          //turn off cursor after reading settings file
        }
        static void WritePriceText(string _time, decimal _price)
        {
            Clear();
            WriteLine(" Requested data from Elering!\n" +
                      " Electricity price right now\n" +
                      " =========================\n " +
                     $"{_time} : {_price} €/MWh\n");
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
                " ======\n v1.4.2\n ______\n" +
                " ? - Code clean-up: 1) Removed one unnecessary parameter and its conversion.\n" +
                "                    2) Removed double loging when BOINC was not running.\n" +
                " ? - User can not provide zero or negative signed electricity price as baseline.\n" +
                " ? - Changed when happens flushing of logs to log file.\n" + 
                " ======\n v1.4.1\n ______\n" +
                " ? - Console window size is now fixed to 65 width units and 15 height units.\n" +
                " * - Program compiled with \"ReadyToRun\" function which according to Microsoft, improves program startup.\n" +
                " ======\n v1.4.0\n ______\n" +
                " ? - Complitely rewritten code for more structured code and easier reading.\n" +
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
                " * - Initial release or Console Application.\n";
            File.WriteAllText(path, releaseNotes);
        }
    }
}