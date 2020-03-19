using static System.Console;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Text;
using System.Threading;

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
        private readonly string eleringApiLink = "https://dashboard.elering.ee/api/nps/price";
        private string time;
        private double price;
        private int timestamp;

        public string Time { get { return time; } set { time = value; } }
        public double Price { get { return price; } set { price = value; } }
        public int Timestamp { get { return timestamp; } set { timestamp = value; } }
        private string FormatDateandTime(int ts)
        {
            DateTime date = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(ts).ToLocalTime();
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
            Timestamp = elering.Data.Ee[^1].Timestamp;
            Time = FormatDateandTime(timestamp);
            Price = elering.Data.Ee[^1].Price;
        }
    }
    class Program
    {
        static void Main()
        {
            OutputEncoding = Encoding.UTF8; //make console show utf-8 characters
            int retryCounter = 1;   //counter for elering data aquisition
            CallElering elering = new CallElering();    //create object for elering time and price data
            Process boinc = new Process();  //create process of programm to be run
            boinc.StartInfo.UseShellExecute = false;    //start only executables e.g.: .exe
            boinc.StartInfo.FileName = "C:\\Program Files\\BOINC program\\boincmgr";    //program file path
            //boinc.StartInfo.CreateNoWindow = true;  //!NB! doesnt work for some reason

            int boincID;
            int boincHandle;

            //loop for electricity price check and program start up and shut down
            while (true)
            {
                try
                {
                    WriteLine("Enter price");
                    int price = int.Parse(ReadLine());
                    WriteLine($"{retryCounter}) Getting data from Elering");
                    WriteLine("=========================\n");
                    elering.GetApiData();   //getting data from elering

                    try
                    {
                        PriceText(elering.Time, elering.Price);     //Show aqcuired data from Elering
                        Process[] processList = Process.GetProcessesByName("BOINC");   //check running processes by searchable process name

                        string localAllWord = processList[0].ToString();   //convert acquired process into string for later use

                        if (price < 10 && localAllWord == "System.Diagnostics.Process (boinc)")    //if process running and price is below specified, continue running boinc
                        {
                            boincID = boinc.Id; //gets process id
                            boincHandle = boinc.Handle.ToInt32();   //gets process handle
                            WriteLine($"Process ID: {boincID}; Process Handle: {boincHandle}");
                            WriteLine("Electricity price is still good!");
                            WriteLine("Boinc will continue crunching numbers.");
                            //Thread.Sleep(elering.Timestamp + 3600000);  //stop process for one hour inorder to check electricity price again one hour later

                            Thread.Sleep(10000);
                        }
                        else
                        {
                            WriteLine("Price is too high for cheap number crunching!");
                            WriteLine("Shutting down Boinc!");
                            WriteLine("Will check price again at next o'clock.");
                            //boinc.Refresh();
                            boincID = boinc.Id; //gets process id
                            boincHandle = boinc.Handle.ToInt32();   //gets process handle
                            WriteLine($"Process ID: {boincID}; Process Handle: {boincHandle}");
                            boinc.Kill();   //not very elegant way for stopping process...... NOT WORKING!
                            boinc.WaitForExit();
                            boinc.Dispose();
                            //Thread.Sleep(elering.Timestamp + 3600000);  //stop process for one hour inorder to check electricity price again one hour later

                            Thread.Sleep(10000);
                        }
                    }
                    catch (IndexOutOfRangeException)    //if there is no process running, this exception is thrown and process is started
                    {
                        PriceText(elering.Time, elering.Price);     //Show aqcuired data from Elering
                        WriteLine("Boinc is not crunching numbers.\n");

                        if (price < 10)   //if price is below 45€/MWh and got data from elering, proceed to start process
                        {
                            boinc.Start();  //start process based on previous setup
                            boincID = boinc.Id; //gets process id
                            boincHandle = boinc.Handle.ToInt32();   //gets process handle
                            WriteLine($"Process ID: {boincID}; Process Handle: {boincHandle}");
                            Thread.Sleep(13000);    //wait 13 seconds for program to connect to internet and get data from internet
                            boinc.CloseMainWindow();    //close program window automatically

                            WriteLine($"BOINC started crunching numbers!");
                            WriteLine("Will check price again at next o'clock.\n");
                            //Thread.Sleep(elering.Timestamp + 3600000);  //stop process for one hour inorder to check electricity price again one hour later

                            Thread.Sleep(10000);
                        }
                        else
                        {
                            WriteLine($"Price is still too high for cruncing numbers!");
                            WriteLine("Will check price again at next o'clock.\n");
                            //Thread.Sleep(elering.Timestamp + 3600000);  //stop process for one hour inorder to check electricity price again one hour later

                            Thread.Sleep(10000);
                        }
                    }
                }
                catch (WebException)
                {
                    WriteLine("Could not get Elering data from internet.\n");
                    retryCounter++;
                    Thread.Sleep(5000);
                }
            }
        }

        static void PriceText(string _time, double _price)
        {
            Clear();
            WriteLine("Got data from Elering!\n");
            WriteLine("Electricity price right now");
            WriteLine("=========================\n");
            WriteLine($"{_time} : {_price} MWh/€\n");
        }
    }
}