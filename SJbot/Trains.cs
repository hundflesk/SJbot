using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SJbot
{
    public class SJTrain
    {
        public string type;
        public int num;
        public int track;
        public TimeSpan departure;
        public TimeSpan newDeparture;
        public string comment;

        public SJTrain(string type, int num, int track, TimeSpan d, TimeSpan nd, string c)
        {
            this.type = type;
            this.num = num;
            this.track = track;
            departure = d;
            newDeparture = nd;
            comment = c;
        }
    }

    public class JsonSJTrainArray
    {

    }
}
