using System.Collections.Generic;

//save user provided highest electricity price and use it for next program startup
//calculate spent electricity and its cost
//calculate total electricity used over time.
//check if BOINC is even installed on computer
//optional: send data to database
//optinal: create release notes from within program

namespace BoincElectricity
{
    //EleringDataAPI class represents Elering API hierarchy
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
}