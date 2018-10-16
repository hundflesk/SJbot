using System;
using System.Collections.Generic;
using DSharpPlus;

namespace SJbot
{
    public class Trains
    {
        public static async void UpdateTrainList(Rootobject data)
        {
            var tempTrainsList = new List<SJTrain>();

            int trainsQuantity = 0;
            int maxTrainsInList = 10;
            foreach (var t in data.station.transfers.transfer)
            {
                DateTime currentDay = DateTime.Now;

                string[] arr = t.departure.Split();
                DateTime trainDay = DateTime.Parse(arr[0]);

                //vill inte att listan ska innehålla tåg som går nästa dag
                if (currentDay.DayOfWeek != trainDay.DayOfWeek)
                    break;

                //vill inte att listan ska bli för lång, max antal tåg i listan = 10
                else if (trainsQuantity == maxTrainsInList)
                    break;

                //alla tågen jag tar går åker till/förbi Västerås
                if (t.destination.Contains("Västerås"))
                {
                    try
                    {
                        string type = t.type; //SJ regional hela tiden, men ändå, använder typen
                        int num = Convert.ToInt32(t.train); //tågnummer
                        int track = Convert.ToInt32(t.track); //spår, tror jag blir "X" om iställt

                        string[] temp = arr[1].Split(":");
                        TimeSpan time = new TimeSpan(Convert.ToInt32(temp[0], Convert.ToInt32(temp[1], Convert.ToInt32(temp[2]))));

                        time = TimeSpan.Parse(arr[1]);

                        if (t.newDeparture == null && t.comment == null)
                            tempTrainsList.Add(new SJTrain(type, num, track, time));
                        else
                        {
                            TimeSpan nd;
                            string[] nArr = t.newDeparture.Split();

                            string c = t.comment;

                            tempTrainsList.Add(new SJTrain(type, num, track, time, nd, c));
                        }

                        trainsQuantity++;
                    }
                    catch
                    {
                        if (t.track == "X" || t.track == "x")
                        {
                            string msg = $"{t.type}: {t.train} with departure time {t.departure} has been canceled.";
                            msg += " ";
                            await Program.Discord.SendMessageAsync(Program.ChannelSJ, msg);
                        }
                    }
                }
            }
            Program.TrainList = tempTrainsList;
        }
    }

    public class SJTrain
    {
        public string type;
        public int num;
        public int track;
        public TimeSpan departure;
        public TimeSpan newDeparture;
        public string comment;

        public SJTrain(string type, int num, int track, TimeSpan d)
        {
            this.type = type;
            this.num = num;
            this.track = track;
            departure = d;
        }

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
