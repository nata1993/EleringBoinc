using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using static System.Console;

//calculate spent electricity and its cost
//calculate total electricity used over time.
//check if BOINC is even installed on computer
//optional: send data to database

namespace BoincElectricity
{
    class Program
    {
        //parameters
        private protected int retryCounter = 1;                                             //counter for elering data reaquisition
        private protected bool savedPrice;
        private protected string allRunningProcesses;
        private protected bool mainLoop = true;

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
                    boinc.StartInfo.FileName = setup.BoincProgram;                          //program to be started file path
            //SETUP
            setup.SetupConsoleWindow();
            setup.CreateDirectoriesAndFiles();
            //log
            logWriter.WriteLine($" {DateTime.Now} - ========================== NEW PROGRAM STARTUP ====================================\n" +
                                $" {DateTime.Now} - Setting up program ressources: Directory, StreamWriter, API object, Process object, etc.\n" +
                                $" {DateTime.Now} - Reading settings file.");
            logWriter.Flush();
            
            //ASK FOR USER INPUT
            if (!File.Exists(userInput.SettingsFile))
            {
                CursorVisible = true;
                userInput.AskElectricityPrice(logWriter);
                userInput.AskExcise(logWriter);
                userInput.AskVAT(logWriter);
                userInput.SaveInputToSettingsFile();
                userInput.ShowUserProvidedData();
                CursorVisible = false;                                                      //turn off cursor after user inputed electricity price
            }
            else
            {
                //read settings file and show its content on the sreen
                setup.ShowSettingsFile(userInput.SettingsFile);
                Thread.Sleep(2500);
                //read settings file for saved data - if tryparse fails e.g false, ask user to provide baseline price
                mainProgram.savedPrice = decimal.TryParse(setup.ExternalSettings[0], out decimal convertedResult);
                WriteLine(" Using previously saved electricity price level.");
                //log
                logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                    $" {DateTime.Now} - Successfully read user saved setting from file.\n" +
                                    $" {DateTime.Now} - Using previously saved electricity price setting.");
                userInput.UserProvidedElectricityPrice = convertedResult;                   //asign user provided setting from settings file
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
                        logWriter.Flush();                                                  //flush logs from Elering data request
                        elering.PublicGetApiData();                                         //getting data from elering
                        elering.CalculateRemainingSecondsTillOClock();
                        break;
                    }
                    catch (WebException)
                    {
                        WriteLine(" Could not get Elering data from internet.\n Please check your internet connection.");
                        //log
                        logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                            $" {DateTime.Now} - Program could not get data from Elering.");
                        logWriter.Flush();                                                  //flush logs from Elering data request
                        mainProgram.retryCounter++;
                        Thread.Sleep(5000);                                                 //retry every five seconds
                    }
                }
                mainProgram.retryCounter = 1;                                               //reset retry counter
                //CHECK FOR RUNNING PROCESSES
                WritePriceText(elering.TimeFromElering, elering.PriceFromElering);          //print acquired data from Elering
                WriteLine(" Checking for running boinc processes.\n");
                //log
                logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                    $" {DateTime.Now} - Calculated seconds till next o'clock: {elering.SecondsTillOClock / 1000}.\n" +    //calculate milliseconds into seconds
                                    $" {DateTime.Now} - Requested data from Elering: {elering.TimeFromElering} : {elering.PriceFromElering}.\n" +
                                    $" {DateTime.Now} - Checking for running processes.");
                logWriter.Flush();
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
                    if (elering.PriceFromElering <= userInput.UserProvidedElectricityPrice) //if price is below 45€/MWh and got data from elering, proceed to start process
                    {
                        try
                        {
                            //log
                            logWriter.WriteLine($" {DateTime.Now} - Starting BOINC.");
                            logWriter.Flush();

                            boinc.Start();                                                  //start BOINC process based on previous setup but only if program is installed on PC
                            Thread.Sleep(15000);                                            //wait 15 seconds for BOINC program to connect to internet and get data from internet
                            boinc.CloseMainWindow();                                        //close program window automatically to tray
                            elering.CalculateRemainingSecondsTillOClock();                  //update remaining seconds till next o'clock
                            WriteLine(" BOINC started crunching numbers!\n" +
                                      " Will check price again at next o'clock.");
                            //log
                            logWriter.WriteLine($" {DateTime.Now} - Started BOINC program.\n" +
                                                $" {DateTime.Now} - Closed BOINC to tray.");
                            logWriter.Flush();
                            Thread.Sleep(elering.SecondsTillOClock);                        //stop main program process for one hour inorder to check electricity price again one hour later
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
                        Thread.Sleep(elering.SecondsTillOClock);                            //stop main program process for one hour inorder to check electricity price again one hour later
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
                                            $" {DateTime.Now} - BOINC process is running.\n" +
                                            $" {DateTime.Now} - Requested data from Elering was below user specified level. BOINC processes continued running.");
                        logWriter.Flush();
                        Thread.Sleep(elering.SecondsTillOClock);                            //stop main program process for one hour inorder to check electricity price again one hour later
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
                            Thread.Sleep(elering.SecondsTillOClock);                        //stop main program process for one hour inorder to check electricity price again one hour later
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
        static void WritePriceText(string time, decimal price)
        {
            Clear();
            WriteLine(" Requested data from Elering!\n" +
                      " Electricity price right now\n" +
                      " =========================\n " +
                     $"{time} : {price} €/MWh\n");
        }
    }
}