using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using static System.Console;

//check if BOINC is even installed on computer
//optional: send data to database

namespace BoincElectricity
{
    class Program
    {
        //parameters
        private protected int retryCounter;                                                 //counter for elering data reaquisition
        private protected bool savedPrice;                                                  //parameter for reading external settings file, true if setting was previously saved
        private protected string allRunningProcesses;                                       //parameter for all processes that are currently running
        private protected bool mainLoop = true;                                             //parameter for turning program loop off if critical error

        static void Main()
        {
            //objects and writers
            Setup setup = new Setup();                                                      //Setup is used for creating directories and other main program settings
            StreamWriter logWriter = new StreamWriter(setup.LogFile, true);                 //StreamWritter is adding data to log file, not overwriting
            Program mainProgram = new Program();                                            //mainProgram is used for global variables
            Elering elering = new Elering();                                                //Elering is used for data aqcuisition from Elering API
            UserInput userInput = new UserInput();                                          //UserInput is used for asking user to provide necessary data on start up
            Process boinc = new Process();                                                  //create process of external program to be run
                    boinc.StartInfo.UseShellExecute = false;                                //start only executables e.g.: .exe
                    boinc.StartInfo.FileName = setup.BoincProgram;                          //file path for the program to be started
            //SETUP
            setup.SetupConsoleWindow();
            setup.CreateDirectoriesAndFiles();
            //log
            logWriter.WriteLine($" {DateTime.Now} - ========================== NEW PROGRAM STARTUP ====================================\n" +
                                 "                  Setting up program ressources: Directory, StreamWriter, API object, Process object, etc.\n" +
                                 "                  Reading settings file.");
            logWriter.Flush();
            
            //ASK FOR USER INPUT
            if (!File.Exists(setup.SettingsFile))
            {
                CursorVisible = true;
                userInput.AskElectricityPrice(logWriter);
                userInput.AskVAT(logWriter);
                userInput.AskExcise(logWriter);
                userInput.SaveInputToSettingsFile();
                userInput.ShowUserProvidedData();
                CursorVisible = false;                                                      //turn off cursor after user inputed electricity price
                //log
                logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                     "                  Asked for user input and saved user input to\n" +
                                     "                  settings file.");
            }
            else
            {
                try
                {
                    //read settings file and show its content on the sreen
                    setup.ShowSettingsFile();
                    Task.Delay(2500);
                    //read settings file for saved data - if tryparse fails e.g false e.g exception created e.g settings file is corrupted, ask user to provide baseline price
                    mainProgram.savedPrice = decimal.TryParse(setup.ExternalSettings[0], out decimal convertedResult);
                    WriteLine(" Using previously saved electricity price limit.");
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                         "                  Successfully read user saved setting from file.\n" +
                                         "                  Using previously saved electricity price setting.");
                    userInput.UserProvidedElectricityPrice = convertedResult;                   //asign previously provided setting from settings file
                }
                catch (Exception)
                {
                    CursorVisible = true;
                    userInput.AskElectricityPrice(logWriter);
                    userInput.AskVAT(logWriter);
                    userInput.AskExcise(logWriter);
                    userInput.SaveInputToSettingsFile();
                    userInput.ShowUserProvidedData();
                    CursorVisible = false;
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                         "                  Unsuccessfully read settings file.\n" +
                                         "                  Asked for user input.");
                }
                Task.Delay(2000);                                                         //wait two seconds for user to read
            }
            logWriter.Flush();                                                              //flush all the log from user input to the log file
            //LOOP FOR BOINCELECTRICITY PROGRAM
            while (mainProgram.mainLoop)
            {
                //REQUEST FOR ELECTRICITY DATA FROM ELERING
                mainProgram.retryCounter = 1;
                while (true)
                {
                    try
                    {
                        Clear();
                        WriteLine($" {mainProgram.retryCounter}) Requesting data from Elering\n" +
                                   " =========================\n");
                        //log
                        logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                             "                  Requesting data from Elering.");
                        logWriter.Flush();                                                  //flush logs from Elering data request
                        elering.GetApiData();                                               //getting data from elering
                        elering.CalculateRemainingSecondsTillNextHour();
                        break;
                    }
                    catch (WebException)
                    {
                        WriteLine(" Could not get Elering data from internet.\n Please check your internet connection.");
                        //log
                        logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                             "                  Program could not get data from Elering.");
                        logWriter.Flush();                                                  //flush logs from Elering data request
                        mainProgram.retryCounter++;
                        Task.Delay(5000);                                                   //retry every five seconds
                    }
                }
                //CHECK FOR RUNNING PROCESSES
                PrintCurrentPrice(elering.TimeFromElering, elering.PriceFromElering);          //print acquired data from Elering
                WriteLine(" Checking for running boinc processes.\n");
                //log
                logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                     "                  Calculated seconds till next o'clock: {elering.SecondsTillOClock / 1000}.\n" +    //calculate milliseconds into seconds
                                     "                  Requested data from Elering: {elering.TimeFromElering} : {elering.PriceFromElering}.\n" +
                                     "                  Checking for running processes.");
                logWriter.Flush();
                try
                {
                    Process[] processList = Process.GetProcessesByName("BOINC");            //Search for running processes by name
                    mainProgram.allRunningProcesses = processList[0].ToString();            //convert acquired process into string for later use
                    Task.Delay(2000);                                                     //wait for two seconds for user to read
                }
                catch (IndexOutOfRangeException)
                {
                    mainProgram.allRunningProcesses = "-1";                                 //save index out of range for later use
                    PrintCurrentPrice(elering.TimeFromElering, elering.PriceFromElering);      //Show aqcuired data from Elering
                    WriteLine(" Boinc is not crunching numbers.\n");
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                         "                  BOINC processes were not running.");
                    logWriter.Flush();
                }
                //CHECK FOR RUNNING EXTERNAL PROCESS
                //if there is no process running, BOINC process is started
                if (mainProgram.allRunningProcesses == "-1")
                {
                    PrintCurrentPrice(elering.TimeFromElering, elering.PriceFromElering);   //Show aqcuired data from Elering
                    WriteLine(" Boinc is not crunching numbers.\n");
                    //START EXTERNAL PROCESS IF PRICE IS GOOD
                    if (elering.PriceFromElering <= userInput.UserProvidedElectricityPrice) //if price is below user provided limit and got data from elering, proceed to start process
                    {
                        try
                        {
                            //CombineHourlyPrice(elering.PriceFromElering, setup.ExternalSettings[1], setup.ExternalSettings[2], mainProgram.totalRunningCost);     NOT YET WORKING
                            //log
                            logWriter.WriteLine($" {DateTime.Now} - Starting BOINC.");
                            logWriter.Flush();
                            boinc.Start();                                                  //start BOINC process based on previous setup but only if program is installed on PC
                            Task.Delay(15000);                                            //wait 15 seconds for BOINC program to connect to internet and get data from internet
                            boinc.CloseMainWindow();                                        //close program main window automatically to tray window
                            elering.CalculateRemainingSecondsTillNextHour();                //update remaining seconds till next o'clock
                            WriteLine(" BOINC started crunching numbers!\n" +
                                      " Will check price again at next o'clock.");
                            //log
                            logWriter.WriteLine($" {DateTime.Now} - Started BOINC program.\n" +
                                                 "                  Closed BOINC to tray.");
                            logWriter.Flush();
                            Task.Delay(elering.SecondsTillOClock);                        //stop main program process for one hour inorder to check electricity price again one hour later
                        }
                        catch (Exception e)
                        {
                            WriteLine(" Could not start BOINC for some reason! Please check log for error.");
                            //log
                            logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                                 "                  Could not start BOINC program for some reason.\n" +
                                                $" {e}");
                            logWriter.Flush();
                            mainProgram.mainLoop = false;                                   //upon critical error, stop main loop
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
                        Task.Delay(elering.SecondsTillOClock);                            //stop main program process for one hour inorder to check electricity price again one hour later
                    }
                }
                //if BOINC is already running
                else if (mainProgram.allRunningProcesses == "System.Diagnostics.Process (boinc)")
                {
                    //IF PRICE IS GOOD
                    if (elering.PriceFromElering <= userInput.UserProvidedElectricityPrice)
                    {
                        WriteLine(" BOINC is running.\n" +
                                  " Electricity price is still good!\n" +
                                  " Boinc will continue crunching numbers.");
                        //log
                        logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                             "                  BOINC process is running.\n" +
                                             "                  Requested data from Elering was below user specified level. BOINC processes continued running.");
                        logWriter.Flush();
                        Task.Delay(elering.SecondsTillOClock);                            //stop main program process for one hour inorder to check electricity price again one hour later
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
                                                 "                  Requested data from Elering was above user specified level.");
                            //closing BOINC processes the hard, not the best, way
                            foreach (Process proc in Process.GetProcessesByName("BOINC"))
                            {
                                proc.Kill();
                                boinc.WaitForExit();
                            }
                            //log
                            logWriter.WriteLine($" {DateTime.Now} - Killed brutaly BOINC processes.");
                            logWriter.Flush();
                            Task.Delay(elering.SecondsTillOClock);                        //stop main program process for one hour inorder to check electricity price again one hour later
                        }
                        //if could not kill boinc processes, this exception is thrown and program is closing while error is written to log
                        catch (Exception e)
                        {
                            Clear();
                            WriteLine(" Something went wrong! Please check log.");
                            //log
                            logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                                 "                  Error in closing BOINC processes.\n" +
                                                $" {DateTime.Now} - {e}");
                            logWriter.Flush();
                            Task.Delay(15000);
                        }
                    }
                }
            }
            ReadLine();
        }
        static void PrintCurrentPrice(string time, decimal price)
        {
            Clear();
            WriteLine(" Requested data from Elering!\n" +
                      " Electricity price right now\n" +
                      " =========================\n " +
                     $"{time} : {price} €/MWh\n");
        }
    }
}