using System.Collections.Generic;

namespace BoincElectricity
{
    //EleringDataAPI class represents Elering API hierarchy for baltic countries and Finland
    //Elering takes its data most likely from NordPoolSpot
    class EleringApi
    {
        //NordPoolSpot day price
        public class NPSPrice
        {
            public class Price
            {
                public bool Success { get; set; }
                public Data Data { get; set; }
            }
            public class Data
            {
                public List<Ee> Ee { get; set; }
                public List<Lv> Lv { get; set; }
                public List<Lt> Lt { get; set; }
                public List<Fi> Fi { get; set; }
            }
            public class Ee
            {
                public int Timestamp { get; set; }
                public double Price { get; set; }
            }
            public class Lv
            {
                public int Timestamp { get; set; }
                public double Price { get; set; }
            }
            public class Lt
            {
                public int Timestamp { get; set; }
                public double Price { get; set; }
            }
            public class Fi
            {
                public int Timestamp { get; set; }
                public double Price { get; set; }
            }
        }
        //Local area grid balance - renewables, crude, bio etc
        public class Balance
        {
            public class Total
            {
                public bool Success { get; set; }
                public List<Data> Data { get; set; }
            }

            public class Data
            {
                public int Timestamp { get; set; }
                public double Input_total { get; set; }
                public double Import_total { get; set; }
                public double Input_local { get; set; }
                public double Renewable_total { get; set; }
                public double Renewable_wind { get; set; }
                public double Renewable_hydro { get; set; }
                public double Renewable_bio { get; set; }
                public double Nonrenewable_total { get; set; }
                public double Output_total { get; set; }
                public double Export_total { get; set; }
                public double Consumption_local_total { get; set; }
            }
        }
    }
}