using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;

//calculate spent electricity and its cost
//calculate total electricity used over time.
//check if BOINC is even installed on computer
//optional: send data to database

namespace BoincElectricity
{
    //Elering class is used for acquiring data from Elering and data reacquisition timing
    class Elering
    {
        private protected readonly string eleringApiLink = "https://dashboard.elering.ee/api/nps/price";
        private protected string timeFromElering;       //time from elering converted to human readable date and time
        private protected double priceFromElering;     //price from elering without taxes
        private protected int timestampFromElering;     //timestamp from elering
        private protected int secondsTillOClock;

        //get used for class external data asking
        string EleringApiLink { get { return eleringApiLink; } }
        public string TimeFromElering { get { return timeFromElering; }  }
        public double PriceFromElering { get { return priceFromElering; }  }
        public int SecondsTillOClock { get { return secondsTillOClock; } }

        private string FormatDateandTime(int timeStamp)
        {
            DateTime date = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(timeStamp).ToLocalTime();
            string formatedDate = date.ToString("dd.MM.yyyy HH:mm");
            return formatedDate;
        }
        //Get data from elering
        public  void GetApiData()
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
            priceFromElering = elering.Data.Ee[^1].Price;
        }
        //Calculate how many seconds remain till next o'clock
        public  void CalculateRemainingSecondsTillNextHour()
        {
            DateTime dtNow = DateTime.Now;  //time at the moment
            DateTime nextDtElering = new DateTime(1970, 1, 1, 0, 0, 0).AddHours(1).AddSeconds(timestampFromElering).ToLocalTime();  // time from elering timestamp + 1 hour (next hour)
            TimeSpan result = nextDtElering.Subtract(dtNow); //substract current time from next hour elering timestamp
            secondsTillOClock = Convert.ToInt32((result.TotalSeconds * 1000) + 10000); //convert substraction result to timestamp in milliseconds and and add 10 000 milliseconds (10 seconds)
        }
    }
}