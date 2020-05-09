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
        static private protected int retryCounter;                                          //Counter for elering data reaquisition
        static private protected bool savedPrice;                                           //Parameter for reading external settings file, true if setting was previously saved
        static private protected string allRunningProcesses;                                //Parameter for all processes that are currently running
        static private protected bool programLoop = true;                                   //Parameter for turning program loop off if critical error happened
        static private protected double clientPrice;

        static void Main()
        {
            //CREATE OBJETS AND WRITERS
            Setup setup = new Setup();                                                      //Setup is used for creating directories and setting up BoincElectricity
                  setup.SetupConsoleWindow();
                  setup.CreateDirectoriesAndFiles();
                  setup.CheckIfBoincIsInstalled();                                          //Checking if BOINC is installed in default installation folder like in WIN10
            StreamWriter logWriter = new StreamWriter(Setup.LogFile, true);                 //StreamWritter is adding data to log file, not overwriting
            Elering elering = new Elering();                                                //Elering is used for data aqcuisition from Elering API
            UserInput userInput = new UserInput();                                          //UserInput is used for asking user to provide necessary data on start up
            Process boinc = new Process();                                                  //Create process of external program to be run
                    boinc.StartInfo.UseShellExecute = false;                                //Start only executables e.g.: .exe
                    boinc.StartInfo.FileName = Setup.BoincInstallationPath + setup.BoincProgram;    //File path for the program to be started
            //log
            logWriter.WriteLine($" {DateTime.Now} - ========================== NEW PROGRAM STARTUP =========================================\n" +
                                 "                       Set up program ressources: Directory, StreamWriter, API object, Process object, etc.\n" +
                                 "                       Reading settings file.");
            logWriter.Flush();
            
            //ASK FOR USER INPUT
            if (!File.Exists(Setup.SettingsFile))
            {
                CursorVisible = true;
                userInput.AskUserInput(logWriter);
                CursorVisible = false;
                setup.ReadSettingsFile();                                                   //Turn off cursor after user inputed electricity price
                //log
                logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                     "                       Asked for user input and saved user input to\n" +
                                     "                       settings file. Showed saved settings to user.");
            }
            else
            {
                try
                {
                    setup.ReadSettingsFile();                                               //Read settings file and show its content on the screen
                    //Read settings file for saved data - if tryparse fails e.g false e.g exception created e.g settings file is corrupted, ask user to provide baseline price
                    savedPrice = double.TryParse(setup.SettingsFromSettingsFile[1], out double convertedResult);
                    WriteLine(" Using previously saved electricity price limit.");
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - --------------------------\n" +
                                         "                       Successfully read user saved setting from file.\n" +
                                         "                       Using previously saved electricity price setting.");
                    userInput.UserProvidedElectricityPrice = convertedResult;               //Asign previously provided setting from settings file
                }
                catch (Exception)
                {
                    CursorVisible = true;
                    userInput.AskUserInput(logWriter);
                    CursorVisible = false;
                    setup.ReadSettingsFile();
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                         "                       Unsuccessfully read settings file.\n" +
                                         "                       Asked for user input again.");
                }
            }
            logWriter.Flush();                                                              //Flush all the log from user input to the log file

            //LOOP FOR BOINCELECTRICITY PROGRAM
            while (programLoop)
            {
                //REQUEST FOR ELECTRICITY DATA FROM ELERING
                retryCounter = 1;
                clientPrice = 0;
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
                        logWriter.Flush();                                                  
                        elering.GetNPSPriceData();                                          //Getting data from elering
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
                        logWriter.Flush();                                                  
                        retryCounter++;
                        Task.Delay(10000).Wait();                                            //Retry every ten seconds
                    }
                }

                //CHECK FOR RUNNING PROCESSES
                PrintCurrentPrice(elering.DatetimeFromElering, elering.PriceFromElering);   //Print acquired data from Elering
                WriteLine(" Checking for running boinc processes.\n");
                //log
                logWriter.WriteLine($" {DateTime.Now} - --------------------------\n" +
                                    $"                       Requested data from Elering: {elering.DatetimeFromElering} : {elering.PriceFromElering}.\n" +
                                    $"                       Calculated seconds till next o'clock: {elering.RemainingSecondsTillOClock / 1000}.\n" +    //Calculate milliseconds into seconds" +
                                     "                       Checking for running processes.");
                logWriter.Flush();
                Task.Delay(2000).Wait();                                                    //Wait for two seconds for user to read
                //CHECK FOR RUNNING EXTERNAL PROCESS
                try
                {
                    Process[] processList = Process.GetProcessesByName("BOINC");            //Search for running processes by name
                    allRunningProcesses = processList[0].ToString();                        //Convert acquired process into string for later use
                }
                catch (IndexOutOfRangeException)
                {
                    allRunningProcesses = "-1";                                             //Save index out of range for later use
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - --------------------------\n" +
                                         "                       BOINC processes were not running.");
                    logWriter.Flush();
                }

                //If there is no BOINC process running, BOINC process is started
                if (allRunningProcesses == "-1")
                {
                    WriteLine(" Boinc is not crunching numbers.\n");
                    //START EXTERNAL PROCESS IF PRICE IS GOOD
                    if (elering.PriceFromElering <= userInput.UserProvidedElectricityPrice) //If price is below user provided limit, proceed to start BOINC process
                    {
                        try
                        {
                            //log
                            logWriter.WriteLine($" {DateTime.Now} - Starting BOINC.");
                            logWriter.Flush();
                            boinc.Start();                                                  //Start BOINC process based on previous setup
                            Task.Delay(5000).Wait();                                        //Wait 5 seconds for BOINC program to connect to internet and get data from internet
                            boinc.CloseMainWindow();                                        //Close program main window automatically to tray window
                            elering.CalculateRemainingSecondsTillNextHour();                //Update remaining seconds till next o'clock
                            WriteLine(" BOINC started crunching numbers!\n" +
                                      " Will check price again at next o'clock.");
                            logWriter.WriteLine($" {DateTime.Now} - Started BOINC program.\n" +
                                                 "                       Closed BOINC to tray.");
                            logWriter.Flush();
                            Task.Delay(elering.RemainingSecondsTillOClock).Wait();          //Stop main program process inorder to check electricity price again at next o'clock
                        }
                        catch (Exception e)
                        {
                            WriteLine(" Could not start BOINC for some reason! Please check log for error.");
                            //log
                            logWriter.WriteLine($" {DateTime.Now} - --------------------------\n" +
                                                 "                       Could not start BOINC program for some reason.\n" +
                                                $" {e}");
                            logWriter.Flush();
                            programLoop = false;                                            //Upon critical error, stop main loop
                        }
                    }
                    //WAIT FOR GOOD PRICE TO START PROCESS
                    else
                    {
                        WriteLine(" Price is still too high for cruncing numbers!\n" +
                                  " Will check price again at next o'clock.");
                        elering.CalculateRemainingSecondsTillNextHour();
                        //log
                        logWriter.WriteLine($" {DateTime.Now} - Requested data from Elering was above user specified level. BOINC was not started.");
                        logWriter.Flush();
                        Task.Delay(elering.RemainingSecondsTillOClock).Wait();              //Stop main program process inorder to check electricity price again at next o'clock
                    }
                }
                //If BOINC was started by BoincElectricity and is already running
                else if (allRunningProcesses == "System.Diagnostics.Process (boinc)")
                {
                    //IF PRICE IS GOOD
                    if (elering.PriceFromElering <= userInput.UserProvidedElectricityPrice)
                    {
                        WriteLine(" BOINC is running.\n" +
                                  " Electricity price is still good!\n" +
                                  " Boinc will continue crunching numbers.");
                        elering.CalculateRemainingSecondsTillNextHour();
                        //log
                        logWriter.WriteLine($" {DateTime.Now} - --------------------------\n" +
                                             "                       BOINC process is running.\n" +
                                             "                       Requested data from Elering was below user specified level. BOINC processes continued running.");
                        logWriter.Flush();
                        Task.Delay(elering.RemainingSecondsTillOClock).Wait();              //Stop main program process inorder to check electricity price again at next o'clock
                    }
                    //IF PRICE IS BAD, TERMINATE BOINC
                    else
                    {
                        try
                        {
                            WriteLine(" Price is too high for relatively cheap number crunching!\n" +
                                      " Will check price again at next o'clock.\n" +
                                      " Shutting BOINC down!");
                            //log
                            logWriter.WriteLine($" {DateTime.Now} - --------------------------\n" +
                                                 "                       Requested data from Elering was above user specified level.");
                            //Closing BOINC processes the easy, not the best, way
                            foreach (Process proc in Process.GetProcessesByName("BOINC"))
                            {
                                proc.Kill();
                                boinc.WaitForExit();
                            }
                            elering.CalculateRemainingSecondsTillNextHour();
                            logWriter.WriteLine($" {DateTime.Now} - Killed brutaly BOINC processes.");
                            logWriter.Flush();
                            Task.Delay(elering.RemainingSecondsTillOClock).Wait();          //Stop main program process inorder to check electricity price again at next o'clock
                        }
                        //If could not kill boinc processes, this exception is thrown
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
                            break;
                        }
                    }
                }
            }
            ReadLine();
        }
        static void PrintCurrentPrice(string time, double price)
        {
            Clear();
            WriteLine(" Electricity price right now\n" +
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