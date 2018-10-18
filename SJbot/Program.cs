using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using Newtonsoft.Json;

namespace SJbot
{
    internal class Program
    {
        public static DiscordClient Discord { get; private set; }
        private static CommandsNextModule Commands { get; set; }

        public static DiscordUser Bot { get; private set; }
        public static DiscordUser Me { get; private set; }
        public static DiscordChannel ChannelSJ { get; private set; }

        public static List<SJTrain> TrainList { get; set; }
        private static List<KeyValuePair<DayOfWeek, TimeSpan>> SchoolDays { get; set; }

        private static List<KeyValuePair<DayOfWeek, TimeSpan>> AddSchoolDays()
        {
            var days = new Dictionary<DayOfWeek, TimeSpan>
            {
                {DayOfWeek.Monday, new TimeSpan(15, 50, 0) },
                {DayOfWeek.Tuesday, new TimeSpan(15, 40, 0) },
                {DayOfWeek.Wednesday, new TimeSpan(15, 20, 0) },
                {DayOfWeek.Thursday, new TimeSpan(12, 20, 0) },
                {DayOfWeek.Friday, new TimeSpan(12, 20, 0) }
            }.ToList();

            return days;
        }

        private static void Main(string[] args)
        {
            SchoolDays = AddSchoolDays();
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            Discord = new DiscordClient(new DiscordConfiguration
            {
                Token = "NDkxOTMwOTE4NTAwNDMzOTMw.DoPDsA.-iBZ_CUDa6_O6CiCjPVrIIH0H6k",
                TokenType = TokenType.Bot,
            });

            Commands = Discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefix = "?"
            });

            Commands.RegisterCommands<SJCommands>();

            Bot = Discord.GetUserAsync(491930918500433930).Result;
            ChannelSJ = Discord.GetChannelAsync(502175502983757836).Result;
            Me = Discord.GetUserAsync(276068458242768907).Result;

            await Discord.ConnectAsync();

            Thread x = new Thread(CallAPI);
            x.Start();

            await Task.Delay(-1);
        }

        private static async void CallAPI()
        {
            RestClient client = new RestClient();

            //inloggning för att komma åt json-filen på hemsidan
            NetworkCredential credential = new NetworkCredential(client.UserName, client.UserPassword);

            bool cheapWay = false; //denna legendariska metod används för att notificateasync inte
            //ska köras innan listan har hämtats från API:n, ska threaden ska bara köras en gång

            var canceledTrainsToday = new List<CanceledTrain>();
            while (true)
            {
                while (true)
                {
                    WebRequest request = WebRequest.Create(client.Url);
                    request.Method = client.HttpMethod.ToString();
                    request.Credentials = credential;

                    WebResponse response = await request.GetResponseAsync();

                    var stream = response.GetResponseStream();
                    if (stream != null)
                    {
                        var reader = new StreamReader(stream); //@"C:\PRR2_filhantering\sj\test.json"
                        string rawData = await reader.ReadToEndAsync();
                        var data = JsonConvert.DeserializeObject<Rootobject>(rawData);

                        var tempTrainsList = new List<SJTrain>();

                        foreach (var t in data.station.transfers.transfer)
                        {
                            DateTime currentDay = DateTime.Now;

                            //när det blir en ny dag ska listan med inställda tåg rensas
                            if (canceledTrainsToday.Count != 0 && currentDay.DayOfWeek != canceledTrainsToday[canceledTrainsToday.Count - 1].day)
                                canceledTrainsToday = new List<CanceledTrain>(); //gör en ny lista med tåg som är inställda

                            string[] dayAndTime = t.departure.Split();
                            DateTime trainDay = DateTime.Parse(dayAndTime[0]);

                            //vill inte att listan ska innehålla tåg som går nästa dag
                            if (currentDay.DayOfWeek != trainDay.DayOfWeek)
                                break;

                            //alla tågen jag tar går åker till/förbi Västerås
                            //kollar också om 'destination' har fler än en, därför kollas kommatecknet
                            if (t.destination.Contains("Västerås") && t.destination.Contains(","))
                            {
                                string type = t.type; //SJ regional hela tiden, men ändå, använder typen
                                int num = Convert.ToInt32(t.train); //tågnummer, ska alltid gå att konvertera

                                string[] timeArr = dayAndTime[1].Split(":");
                                TimeSpan time = new TimeSpan(Convert.ToInt32(timeArr[0]), Convert.ToInt32(timeArr[1]), Convert.ToInt32(timeArr[2]));

                                if (t.track == "X" || t.track == "x")
                                {
                                    bool exists = false;
                                    foreach (var canceled in canceledTrainsToday)
                                    {
                                        if (time == canceled.time)
                                            exists = true;
                                    }
                                    if (exists == false)
                                    {
                                        string msg = $"{Me.Mention}, {t.type}: {t.train} with departure time {t.departure} has been canceled.";
                                        msg += $"\nReason: {t.comment} --> Check the new list with command: '?trains'.";
                                        await Discord.SendMessageAsync(ChannelSJ, msg);

                                        canceledTrainsToday.Add(new CanceledTrain(trainDay.DayOfWeek, time));
                                    }
                                }
                                else
                                {
                                    string track = t.track; //spår, tror jag blir "X" om iställt

                                    if (t.newDeparture == null || t.comment == null)
                                        tempTrainsList.Add(new SJTrain(type, num, track, time));

                                    else
                                    {
                                        string[] nDayTime = t.newDeparture.Split();
                                        string[] nTimeArr = nDayTime[1].Split(":");
                                        TimeSpan nTime = new TimeSpan(Convert.ToInt32(nTimeArr[0]), Convert.ToInt32(nTimeArr[1]), Convert.ToInt32(nTimeArr[2]));

                                        string c = t.comment;

                                        tempTrainsList.Add(new SJTrain(type, num, track, time, nTime, c));
                                    }
                                }
                            }
                        }
                        TrainList = tempTrainsList;

                        if (cheapWay == false)
                        {
                            Thread y = new Thread(NotificateAsync);
                            y.Start();
                            cheapWay = true;
                        }
                    }
                    Thread.Sleep(10000);
                }
            }
        }

        private static async void NotificateAsync()
        {
            bool firstTime = true; //används för att notifikationerna ska köras så fort det blir en ny minut
            //annars om man startar programmet ex. 10 sekunder innan det blir en ny minut, kommer en notifikation
            //skickas 50 sekunder efter i den minut då botten ska egentligen skicka så fort den minuten blev till
            //enkelt sagt: botten ska skicka ett meddelande så fort det blir en ny minut

            while (true)
            {
                var currentDay = DateTime.Now;

                if (currentDay.DayOfWeek == DayOfWeek.Saturday || currentDay.DayOfWeek == DayOfWeek.Sunday)
                {
                    if (Bot.Presence.Status != UserStatus.DoNotDisturb) //bot status => röd
                        await Discord.UpdateStatusAsync(null, UserStatus.DoNotDisturb);
                }
                else
                {
                    if (SJCommands.notifications == true)
                    {
                        if (Bot.Presence.Status != UserStatus.Online) //bot status => grön
                            await Discord.UpdateStatusAsync(null, UserStatus.Online);

                        var currentTime = new TimeSpan(currentDay.Hour, currentDay.Minute, 0).TotalMinutes;

                        foreach (var train in TrainList)
                        {
                            var t = new TimeSpan(train.departure.Hours, train.departure.Minutes, 0).TotalMinutes;
                            TimeSpan endTime;

                            foreach (var schoolDay in SchoolDays)
                            {
                                if (currentDay.DayOfWeek == schoolDay.Key)
                                {
                                    endTime = schoolDay.Value;
                                    break;
                                }
                            }
                            if (currentTime == t - 20 && currentTime > endTime.TotalMinutes)
                            {
                                string msg = $"{Me.Mention}, {train.type}: {train.num} departures from track: {train.track} in 20 minutes.";
                                msg += "\nYou should leave school now to get to the train in time.";

                                await Discord.SendMessageAsync(ChannelSJ, msg);
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (Bot.Presence.Status != UserStatus.Idle) //bot status => gul
                            await Discord.UpdateStatusAsync(null, UserStatus.Idle);
                    }
                }
                if (firstTime == true)
                { //gör att botten väntar med att kolla koden tills den sekund det blir en ny minut
                    int ms = 60000 - currentDay.Second * 1000;
                    Thread.Sleep(ms);
                    firstTime = false;
                }
                else
                    Thread.Sleep(60000);
            }
        }
    }
}
