using System;

namespace SJbot
{
    public class SJTrain
    {
        public string type;
        public int num;
        public string track;
        public TimeSpan departure;
        public TimeSpan newDeparture;
        public string comment;

        public SJTrain(TimeSpan d)
        {
            departure = d;
        }

        public SJTrain(string type, int num, string track, TimeSpan d)
        {
            this.type = type;
            this.num = num;
            this.track = track;
            departure = d;
        }

        public SJTrain(string type, int num, string track, TimeSpan d, TimeSpan nd, string c)
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
        public DayOfWeek day { get; set; }
        public TimeSpan time { get; set; }
        public CanceledTrain(DayOfWeek day, TimeSpan time)
        {
            this.day = day;
            this.time = time;
        }
    }

    public class Rootobject
    {
        public Station station { get; set; }
    }

    public class Station
    {
        public string name { get; set; }
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
