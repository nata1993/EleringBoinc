using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using static System.Console;

namespace BoincElectricity
{
    class BoincElectricity
    {
        //parameters
        static private protected int retryCounter;                                          //counter for elering data reaquisition
        static private protected bool savedPrice;                                           //parameter for reading external settings file, true if setting was previously saved
        static private protected string allRunningProcesses;                                //parameter for all processes that are currently running
        static private protected bool programLoop = true;                                   //parameter for turning program loop off if critical error happened
        static private protected double clientPrice;

        static void Main()
        {
            //objects and writers
            Setup setup = new Setup();                                                      //Setup is used for creating directories and other BoincElectricity program settings
            StreamWriter logWriter = new StreamWriter(setup.LogFile, true);                 //StreamWritter is adding data to log file, not overwriting
            Elering elering = new Elering();                                                //Elering is used for data aqcuisition from Elering API
            UserInput userInput = new UserInput();                                          //UserInput is used for asking user to provide necessary data on start up
            Process boinc = new Process();                                                  //create process of external program to be run

            //SETUP
            setup.SetupConsoleWindow();
            setup.CreateDirectoriesAndFiles();
            setup.CheckIfBoincIsInstalled();
            boinc.StartInfo.UseShellExecute = false;                                        //start only executables e.g.: .exe
            boinc.StartInfo.FileName = Setup.BoincInstallationPath + Setup.BoincProgram;    //file path for the program to be started
            //log
            logWriter.WriteLine($" {DateTime.Now} - ========================== NEW PROGRAM STARTUP =========================================\n" +
                                 "                       Setting up program ressources: Directory, StreamWriter, API object, Process object, etc.\n" +
                                 "                       Reading settings file.");
            logWriter.Flush();
            
            //ASK FOR USER INPUT
            if (!File.Exists(setup.SettingsFile))
            {
                CursorVisible = true;
                userInput.AskElectricityPrice(logWriter);
                userInput.AskVAT(logWriter, "VAT");
                userInput.AskExcise(logWriter, "Excise");
                userInput.SaveInputToSettingsFile();
                setup.ReadSettingsFile();
                CursorVisible = false;                                                      //turn off cursor after user inputed electricity price
                //log
                logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                     "                       Asked for user input and saved user input to\n" +
                                     "                       settings file.");
                Task.Delay(4000).Wait();
            }
            else
            {
                try
                {
                    setup.ReadSettingsFile();                                               //read settings file and show its content on the sreen
                    Task.Delay(4000).Wait();
                    //read settings file for saved data - if tryparse fails e.g false e.g exception created e.g settings file is corrupted, ask user to provide baseline price
                    savedPrice = double.TryParse(setup.SettingsFromSettingsFile[1], out double convertedResult);
                    WriteLine(" Using previously saved electricity price limit.");
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - --------------------------\n" +
                                         "                       Successfully read user saved setting from file.\n" +
                                         "                       Using previously saved electricity price setting.");
                    userInput.UserProvidedElectricityPrice = convertedResult;               //asign previously provided setting from settings file
                }
                catch (Exception)
                {
                    CursorVisible = true;
                    userInput.AskElectricityPrice(logWriter);
                    userInput.AskVAT(logWriter, "VAT");
                    userInput.AskExcise(logWriter, "Excise");
                    userInput.SaveInputToSettingsFile();
                    setup.ReadSettingsFile();
                    CursorVisible = false;
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                         "                       Unsuccessfully read settings file.\n" +
                                         "                       Asked for user input.");
                }
                Task.Delay(4000).Wait();                                                    //wait two seconds for user to read
            }
            logWriter.Flush();                                                              //flush all the log from user input to the log file

            //LOOP FOR BOINCELECTRICITY PROGRAM
            while (programLoop)
            {
                //REQUEST FOR ELECTRICITY DATA FROM ELERING
                retryCounter = 1;
                while (true)
                {
                    try
                    {
                        Clear();
                        WriteLine($" {retryCounter}) Requesting data from Elering\n" +
                                   " =========================\n");
                        //log
                        logWriter.WriteLine($" {DateTime.Now} - --------------------------\n" +
                                             "                       Requesting data from Elering.");
                        logWriter.Flush();                                                  //flush logs from Elering data request
                        elering.GetApiData();                                               //getting data from elering
                        elering.CalculateRemainingSecondsTillNextHour();
                        CalculateClientElectricityPrice(elering.PriceFromElering, 
                                                        setup.SettingsFromSettingsFile[2], 
                                                        setup.SettingsFromSettingsFile[3], 
                                                        setup.SettingsFromSettingsFile[4], 
                                                        setup.SettingsFromSettingsFile[5]);
                        break;
                    }
                    catch (WebException)
                    {
                        WriteLine(" Could not get Elering data from internet.\n Please check your internet connection.");
                        //log
                        logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                             "                       Program could not get data from Elering.");
                        logWriter.Flush();                                                  //flush logs from Elering data request
                        retryCounter++;
                        Task.Delay(5000).Wait();                                            //retry every five seconds
                    }
                }

                //CHECK FOR RUNNING PROCESSES
                PrintCurrentPrice(elering.DatetimeFromElering, elering.PriceFromElering);   //print acquired data from Elering
                WriteLine(" Checking for running boinc processes.\n");
                //log
                logWriter.WriteLine($" {DateTime.Now} - --------------------------\n" +
                                    $"                       Calculated seconds till next o'clock: {elering.SecondsTillOClock / 1000}.\n" +    //calculate milliseconds into seconds
                                    $"                       Requested data from Elering: {elering.DatetimeFromElering} : {elering.PriceFromElering}.\n" +
                                     "                       Checking for running processes.");
                logWriter.Flush();
                try
                {
                    Process[] processList = Process.GetProcessesByName("BOINC");            //Search for running processes by name
                    allRunningProcesses = processList[0].ToString();                        //convert acquired process into string for later use
                    Task.Delay(2000).Wait();                                                //wait for two seconds for user to read
                }
                catch (IndexOutOfRangeException)
                {
                    allRunningProcesses = "-1";                                             //save index out of range for later use
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - --------------------------\n" +
                                         "                       BOINC processes were not running.");
                    logWriter.Flush();
                }

                //CHECK FOR RUNNING EXTERNAL PROCESS
                //if there is no process running, BOINC process is started
                if (allRunningProcesses == "-1")
                {
                    WriteLine(" Boinc is not crunching numbers.\n");
                    //START EXTERNAL PROCESS IF PRICE IS GOOD
                    if (elering.PriceFromElering <= userInput.UserProvidedElectricityPrice) //if price is below user provided limit and got data from elering, proceed to start process
                    {
                        try
                        {
                            //log
                            logWriter.WriteLine($" {DateTime.Now} - Starting BOINC.");
                            logWriter.Flush();
                            boinc.Start();                                                  //start BOINC process based on previous setup but only if program is installed on PC
                            Task.Delay(16000).Wait();                                       //wait 15 seconds for BOINC program to connect to internet and get data from internet
                            boinc.CloseMainWindow();                                        //close program main window automatically to tray window
                            elering.CalculateRemainingSecondsTillNextHour();                //update remaining seconds till next o'clock
                            WriteLine(" BOINC started crunching numbers!\n" +
                                      " Will check price again at next o'clock.");
                            logWriter.WriteLine($" {DateTime.Now} - Started BOINC program.\n" +
                                                 "                       Closed BOINC to tray.");
                            logWriter.Flush();
                            Task.Delay(elering.SecondsTillOClock).Wait();                   //stop main program process for one hour inorder to check electricity price again one hour later
                        }
                        catch (Exception e)
                        {
                            WriteLine(" Could not start BOINC for some reason! Please check log for error.");
                            //log
                            logWriter.WriteLine($" {DateTime.Now} - --------------------------\n" +
                                                 "                       Could not start BOINC program for some reason.\n" +
                                                $" {e}");
                            logWriter.Flush();
                            programLoop = false;                                            //upon critical error, stop main loop
                        }
                    }
                    //WAIT FOR GOOD PRICE TO START PROCESS
                    else
                    {
                        WriteLine(" Price is still too high for cruncing numbers!\n" +
                                  " Will check price again at next o'clock.");
                        //log
                        logWriter.WriteLine($" {DateTime.Now} - Requested data from Elering was above user specified level. BOINC was not started.");
                        logWriter.Flush();
                        Task.Delay(elering.SecondsTillOClock).Wait();                       //stop main program process for one hour inorder to check electricity price again one hour later
                    }
                }
                //if BOINC is already running
                else if (allRunningProcesses == "System.Diagnostics.Process (boinc)")
                {
                    //IF PRICE IS GOOD
                    if (elering.PriceFromElering <= userInput.UserProvidedElectricityPrice)
                    {
                        WriteLine(" BOINC is running.\n" +
                                  " Electricity price is still good!\n" +
                                  " Boinc will continue crunching numbers.");
                        //log
                        logWriter.WriteLine($" {DateTime.Now} - --------------------------\n" +
                                             "                       BOINC process is running.\n" +
                                             "                       Requested data from Elering was below user specified level. BOINC processes continued running.");
                        logWriter.Flush();
                        Task.Delay(elering.SecondsTillOClock).Wait();                       //stop main program process for one hour inorder to check electricity price again one hour later
                    }
                    //IF PRICE IS BAD, TERMINATE BOINC
                    else
                    {
                        try
                        {
                            WriteLine(" Price is too high for cheap number crunching!\n Will check price again at next o'clock.\n" +
                                      " Shutting BOINC down!");
                            //log
                            logWriter.WriteLine($" {DateTime.Now} - --------------------------\n" +
                                                 "                       Requested data from Elering was above user specified level.");
                            //closing BOINC processes the hard, not the best, way
                            foreach (Process proc in Process.GetProcessesByName("BOINC"))
                            {
                                proc.Kill();
                                boinc.WaitForExit();
                            }
                            logWriter.WriteLine($" {DateTime.Now} - Killed brutaly BOINC processes.");
                            logWriter.Flush();
                            Task.Delay(elering.SecondsTillOClock).Wait();                   //stop main program process for one hour inorder to check electricity price again one hour later
                        }
                        //if could not kill boinc processes, this exception is thrown and program is closing while error is written to log
                        catch (Exception e)
                        {
                            Clear();
                            WriteLine(" Something went wrong! Please check log.");
                            //log
                            logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                                 "                       Error in closing BOINC processes.\n" +
                                                $" {DateTime.Now} - {e}");
                            logWriter.Flush();
                            Task.Delay(15000).Wait();
                        }
                    }
                }
            }
            ReadLine();
        }
        static void PrintCurrentPrice(string time, double price)
        {
            Clear();
            WriteLine(" Requested data from Elering!\n" +
                      " Electricity price right now\n" +
                      " =========================\n " +
                     $"{time} : {price} €/MWh\n" +
                     $" Final price for user: {Math.Round(clientPrice, 2)} €/MWh\n");
        }
        static void CalculateClientElectricityPrice(double eleringPrice, string VATprice, string VATtype, string excisePrice, string exciseType)
        {
            clientPrice += eleringPrice;
            if (bool.Parse(VATtype) == false)
            {
                clientPrice += double.Parse(VATprice);
            }
            else
            {
                clientPrice += eleringPrice * (double.Parse(VATprice) / 100);
            }
            if (bool.Parse(exciseType) == false)
            {
                clientPrice += double.Parse(excisePrice);
            }
            else
            {
                clientPrice += eleringPrice * (double.Parse(excisePrice) / 100);
            }
        }
    }
}