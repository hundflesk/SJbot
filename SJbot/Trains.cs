using System;

namespace SJbot
{
    public class SJTrain
    {
        public string type;
        public int num;
        public string track;
        public DateTime departure;
        public DateTime newDeparture;
        public string comment;

        public SJTrain(string type, int num, string track, DateTime d)
        {
            this.type = type;
            this.num = num;
            this.track = track;
            departure = d;
        }

        public SJTrain(string type, int num, string track, DateTime d, DateTime nd, string c)
        {
            this.type = type;
            this.num = num;
            this.track = track;
            departure = d;
            newDeparture = nd;
            comment = c;
        }
    }

    public class CanceledTrain
    {
        public string date { get; set; }
        public CanceledTrain(string date) { this.date = date; }
    }

    public class Rootobject
    {
        public Station station { get; set; }
    }

    public class Station
    {
        public Transfers transfers { get; set; }
    }

    public class Transfers
    {
        public Transfer[] transfer { get; set; }
    }

    public class Transfer
    {
        public string departure { get; set; }
        public string newDeparture { get; set; }
        public string destination { get; set; }
        public string track { get; set; }
        public string train { get; set; }
        public string type { get; set; }
        public string comment { get; set; }
    }
}
