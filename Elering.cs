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
    class Elering
    {
        private protected readonly string eleringApiLink = "https://dashboard.elering.ee/api/nps/price";
        private protected string timeFromElering;       //time from elering converted to human readable date and time
        private protected decimal priceFromElering;     //price from elering without taxes
        private protected int timestampFromElering;     //timestamp from elering

        //get used for class external data asking
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
}