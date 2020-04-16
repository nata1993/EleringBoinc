using System;
using System.IO;
using System.Text;
using static System.Console;

//calculate spent electricity and its cost
//calculate total electricity used over time.
//check if BOINC is even installed on computer
//optional: send data to database

namespace BoincElectricity
{
    //Class for setting up essential prerequisites for program to loop, save and read data
    class Setup
    {
        //file names and paths
        private protected string mainPath = @"C:\BoincElectricity\";
        private protected string logFile = @"C:\BoincElectricity\Boinc-Electricity-Log.txt";
        private protected string releaseNotesFile = @"C:\BoincElectricity\Boinc-Electricity-Release-Notes.txt";
        private protected string readMe = @"C:\BoincElectricity\Boinc-Electricity-Read-Me.txt";
        private protected string boincProgram = @"C:\Program Files\BOINC program\boincmgr";
        private protected string[] externalSettings;

        public string LogFile { get { return logFile; } }
        public string BoincProgram { get { return boincProgram; } }
        public string[] ExternalSettings { get { return externalSettings; } }

        public void SetupConsoleWindow()
        {
            //setup console window
            OutputEncoding = Encoding.UTF8;
            InputEncoding = Encoding.UTF8;
            Title = "BoincElectricity";
            BackgroundColor = ConsoleColor.DarkGray;
            ForegroundColor = ConsoleColor.Black;
            SetWindowSize(66, 15);
            BufferWidth = 66;
            CursorVisible = false;
        }
        public void ShowSettingsFile(string path)
        {
            WriteLine(" Previous settings:\n");
            externalSettings = File.ReadAllLines(path);
            for (int i = 0; i < externalSettings.Length; i++)
            {
                string condition = Enum.GetName(typeof(Taxes), i);
                WriteLine($" {Uppercase(condition)}: {externalSettings[i]}");
            }
        }
        private string Uppercase(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            char[] c = s.ToCharArray();
            c[0] = char.ToUpper(c[0]);
            return new string(c);
        }
        public void CreateDirectoriesAndFiles()
        {
            if (!Directory.Exists(mainPath))
            {
                Directory.CreateDirectory(mainPath);
            }
            if (!File.Exists(releaseNotesFile))
            {
                File.Create(releaseNotesFile).Close();
                CreateReleaseNotes(releaseNotesFile);
                CreateProgramIntro(readMe);
            }
        }
        private void CreateProgramIntro(string path)
        {
            string intro =
                " Creator: Bogdan Parubok\n" +
                " Country: Estonia\n" +
                " Contact: bogdan.parubok@hotmail.com\n\n" +
                " ======\n" +
                " BoincElectricity is a console program that is created for single purpose - optimize electricity spending.\n" +
                " It is done by calling local electricity provider API, acquiring data from called API and using that data\n" +
                " to reference to user provided maximum electricity price a person wants to run BOINC program at.\n" +
                " BOINC is a platform for various projects with which a person can contribute home computer or laptop or any\n" +
                " other compactible device computing power for science. One of such projects is WorldCommunityGrid which found\n" +
                " during latest pandemic event, the spread of SARS-COV-2 virus, the virus protein properties for mechanism of\n" +
                " attaching itself to the hosts cells by using special receptors.\n" +
                " Running BOINC software utilizes computer ressources, namely CPU and GPU computing power to solve complex\n" +
                " mathematical calculations for science. In essence combining any BOINC program user computed data, we get world\n" +
                " wide grid of computers which all work for one goal - advancing science through virtual problem solving.\n" +
                " This means that grid of computers basically comes together as one supercomputer.\n However, giving unspent\n" +
                " computer ressources for science spends electricity and electricity has a price tag. Hence this BoincElectricity\n" +
                " program was created.\n" +
                " This program asks during first start-up some data from user which will be used for basic program work. That data\n" +
                " will be saved locally in .txt file format for user to easily read what was asked from user.\n" +
                " For user input there will be used: the maximum electricity price limit, the local excise on electricity and the\n" +
                " local VAT.\n" +
                " The maximum electricity price limit will be used as reference point for starting BOINC program if local electricity\n" +
                " price is below user provided limit as well as shutting down BOINC program when electricity price rises above user\n" +
                " provided price limit.\n" +
                " The local excise on electricity will be used only for calculating total price per kilowatthour (kWh).\n" +
                " The local VAT is same as with excise with only goal for calculating total price per kilowatthour.\n" +
                " This is done by combining local electricity price, excise and VAT into one final pricetag that user will be paying\n" +
                " for each additional kWh that BOINC program will be using for calculating data for science.\n" +
                " BoincElectricity however will be using only local electricity price as comparing value to user provided maximum\n" +
                " electricity price limit. This is done this way because powerplants sell electricity to stockmarket of electricity\n" +
                " as tax-free. But the taxes and excise are added after the electricity has been sold - the usual basic economy stuff.\n" +
                " BoincElectricity will be running automatically after user provided necessary data: first it will call local \n" +
                " electricity provider API, gathered data from API will be shown in console, if price is below user provided limit\n" +
                " BOINC is started, if it is above, BoincElectricity will wait until next o'clock and call API again. This loop \n" +
                " continues until local electricity price falls below limit at which point BOINC program is started.\n" +
                " When BOINC was started, BoincElectricity will wait till next o'clock and then call API. If went above limit, BOINC\n" +
                " is shut down, if price did not rise above limit, BOINC is not shut down and BoincElectricity will again wait till\n" +
                " next o'clock. This loop continues until user closes BoincElectricity program. Closing BoincElectricity will not close\n" +
                " BOINC program by itself hence BOINC will continue running until user closes that program too, otherwise price following\n" +
                " BoincElectricity wont be regulating when BOINC must work and must stop working which in turn will let BOINC to run at\n" +
                " some point at unfavorable electricity price for user.\n" +
                " BoincElectricity is simple and lightweight program written in C# for Windows operating system. At some point in future\n" +
                " BoincElectricity will also support Linux operating system too\n." +
                " This program main auditory is for those users who dont have big money on hand to spend right and left but still want to\n" +
                " contribute somehow to science. BOINC is one of such ways and BoincElectricity was created for this purpose to attempt to\n" +
                " solve such problem.";
            File.WriteAllText(path, intro);
        }
        private void CreateReleaseNotes(string path)
        {
            string releaseNotes =
                " ! - bug\n ? - improvement\n * - update\n" +
                " ======\n v1.5.1\n ______\n" +
                " ? - Small code cleanup and UI polishing.\n" +
                " * - Added Read Me file and text with description about BoincElectricity program.\n" +
                " ======\n v1.5.0\n ______\n" +
                " * - Added user input for local VAT and Excise which are saved to settings file.\n" +
                " * - User input is moved to class.\n" +
                " ? - Additional code refactorings.\n" +
                " * - Created setup class with setup settings for cleaner code.\n" +
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