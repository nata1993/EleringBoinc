using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;

namespace BoincElectricity
{
    //Elering class is used for acquiring data from Elering and data reacquisition timing
    class Elering
    {
        private protected readonly string eleringApiLink = "https://dashboard.elering.ee/api/nps/price";
        private protected string datetimeFromElering;                                           //time from elering converted to human readable date and time
        private protected double priceFromElering;                                              //price from elering without taxes
        private protected int timestampFromElering;                                             //timestamp from elering
        private protected int remainingSecondsTillOClock;

        public string DatetimeFromElering { get { return datetimeFromElering; }  }
        public double PriceFromElering { get { return priceFromElering; }  }
        public int RemainingSecondsTillOClock { get { return remainingSecondsTillOClock; } }

        //public methods
        public void GetNPSPriceData()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(eleringApiLink);
                           request.Method = "GET";
            var webResponse = request.GetResponse();
            var webResponseStream = webResponse.GetResponseStream();
            using var responseReader = new StreamReader(webResponseStream);
            var response = responseReader.ReadToEnd();
            EleringApi.NPSPrice.Price elering = JsonConvert.DeserializeObject<EleringApi.NPSPrice.Price>(response);
            //add data to object
            timestampFromElering = elering.Data.Ee[^1].Timestamp;
            datetimeFromElering = FormatDateandTimeFromEleringTimestamp(timestampFromElering);
            priceFromElering = elering.Data.Ee[^1].Price;
        }
        public void CalculateRemainingSecondsTillNextHour()
        {
            DateTime dtNow = DateTime.Now;                                                      //time at the moment
            DateTime nextDtElering = new DateTime(1970, 1, 1, 0, 0, 0).AddHours(1).AddSeconds(timestampFromElering).ToLocalTime();  // time from elering timestamp + 1 hour (next hour)
            TimeSpan result = nextDtElering.Subtract(dtNow);                                    //substract current time from next hour elering timestamp
            remainingSecondsTillOClock = Convert.ToInt32((result.TotalSeconds * 1000) + 10000); //convert substraction result to timestamp in milliseconds and and add 10 000 milliseconds (10 seconds)
        }

        //private methods
        private string FormatDateandTimeFromEleringTimestamp(int timeStamp)
        {
            DateTime date = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(timeStamp).ToLocalTime();
            string formatedDate = date.ToString("dd.MM.yyyy HH:mm");
            return formatedDate;
        }
    }
}