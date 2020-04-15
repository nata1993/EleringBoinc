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
        private protected string boincProgram = @"C:\Program Files\BOINC program\boincmgr";
        private protected string[] externalSettings;

        public string LogFile { get { return logFile; } }
        public string BoincProgram { get { return boincProgram; } }
        public string[] ExternalSettings { get { return externalSettings; } }

        public void SetupConsoleWindow()
        {
            //setup console window
            OutputEncoding = Encoding.UTF8;
            SetWindowSize(65, 15);
            BufferWidth = 65;
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
        string Uppercase(string s)
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
            }
        }
        private void CreateReleaseNotes(string path)
        {
            string releaseNotes =
                " ! - bug\n ? - improvement\n * - update\n" +
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