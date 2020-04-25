using System.Collections.Generic;

namespace BoincElectricity
{
    //EleringDataAPI class represents Elering API hierarchy for baltic countries
    //Elering takes its data most likely from NordPoolStop
    class EleringDataApi
    {
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
        public class Data
        {
            public List<Ee> Ee { get; set; }
            public List<Lv> Lv { get; set; }
            public List<Lt> Lt { get; set; }
        }
        public class EleringData
        {
            public Data Data { get; set; }
            public bool Status { get; set; }
        }
    }
}