﻿using System;
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
        public static TimeSpan BeagleAdd { get; private set; } //botten körs på beagleboard som går en timme efter
        private static List<KeyValuePair<DayOfWeek, TimeSpan>> SchoolDays { get; set; }

        private static List<KeyValuePair<DayOfWeek, TimeSpan>> AddSchoolDays()
        {
            List<KeyValuePair<DayOfWeek, TimeSpan>> days = new Dictionary<DayOfWeek, TimeSpan>
            {
                {DayOfWeek.Monday, new TimeSpan(15, 50, 0) },
                {DayOfWeek.Tuesday, new TimeSpan(15, 45, 0) },
                {DayOfWeek.Wednesday, new TimeSpan(15, 20, 0) },
                {DayOfWeek.Thursday, new TimeSpan(12, 20, 0) },
            }.ToList();

            return days;
        }

        private static void Main(string[] args)
        {
            Console.WriteLine("Bot starting...");
            BeagleAdd = new TimeSpan(1, 0, 0); //lägger till en timme för att beaglens klocka ska gå rätt
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

            Me = Discord.GetUserAsync(276068458242768907).Result;
            Bot = Discord.GetUserAsync(491930918500433930).Result;
            ChannelSJ = Discord.GetChannelAsync(502175502983757836).Result;

            await Discord.ConnectAsync();
            Console.WriteLine("Bot is online!");

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
            //ska startas flera gånger, threaden ska bara startas en gång

            List<CanceledTrain> canceledTrainsToday = new List<CanceledTrain>();
            while (true)
            {
                DateTime currentDateTime = DateTime.Now.Add(BeagleAdd);
                string currentDate = currentDateTime.ToShortDateString();

                if (currentDateTime.DayOfWeek != DayOfWeek.Friday 
                    || currentDateTime.DayOfWeek != DayOfWeek.Saturday
                    || currentDateTime.DayOfWeek != DayOfWeek.Sunday)
                {
                    WebRequest request = WebRequest.Create(client.Url);
                    request.Method = client.HttpMethod.ToString();
                    request.Credentials = credential;

                    WebResponse response = await request.GetResponseAsync();

                    Stream stream = response.GetResponseStream();
                    if (stream != null)
                    {
                        StreamReader reader = new StreamReader(stream); //@"C:\PRR2_filhantering\sj\test.json" (bara för testning)
                        string rawData = await reader.ReadToEndAsync();
                        Rootobject data = JsonConvert.DeserializeObject<Rootobject>(rawData);

                        List<SJTrain> tempTrainsList = new List<SJTrain>();

                        foreach (Transfer t in data.station.transfers.transfer)
                        {
                            //när det blir en ny dag ska listan med inställda tåg rensas
                            if (canceledTrainsToday.Count != 0 && currentDate != canceledTrainsToday.Last().date)
                                canceledTrainsToday = new List<CanceledTrain>(); //gör en ny lista med tåg som är inställda

                            DateTime trainDateTime = DateTime.Parse(t.departure);
                            string trainDate = trainDateTime.ToShortDateString();

                            //vill bara visa tågen som går samma dag
                            if (currentDateTime.DayOfWeek != trainDateTime.DayOfWeek)
                                break;

                            //alla tågen jag tar går åker till/förbi Västerås
                            //kollar också om 'destination' har fler än en, därför kollas kommatecknet
                            if (t.destination.Contains("Västerås") && t.destination.Contains(","))
                            {
                                string type = t.type; //SJ regional hela tiden, men ändå, använder typen
                                int num = Convert.ToInt32(t.train); //tågnummer, ska alltid gå att konvertera (säker)

                                if (t.track == "X" || t.track == "x")
                                { //spår tror jag blir "X" om iställt

                                    bool exists = false;
                                    foreach (CanceledTrain ct in canceledTrainsToday)
                                    {
                                        if (trainDate == ct.date)
                                            exists = true;
                                    }
                                    if (exists == false)
                                    {
                                        string msg = $"{Me.Mention}, {t.type}: {t.train} with departure time {t.departure.Split()[1]} has been canceled.";
                                        msg += $"\nReason: {t.comment} --> Check the new list with command: '?trains'.";
                                        await Discord.SendMessageAsync(ChannelSJ, msg);

                                        canceledTrainsToday.Add(new CanceledTrain(trainDate));
                                    }
                                }
                                else
                                {
                                    string track = t.track;

                                    if (t.newDeparture == null)
                                        tempTrainsList.Add(new SJTrain(type, num, track, trainDateTime));

                                    else
                                    {
                                        DateTime newTrainDateTime = DateTime.Parse(t.newDeparture);
                                        string comment = t.comment;
                                        tempTrainsList.Add(new SJTrain(type, num, track, trainDateTime, newTrainDateTime, comment));
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
                DateTime currentDateTime = DateTime.Now.Add(BeagleAdd);
                string currentDate = currentDateTime.ToShortDateString();

                //när det blir en ny dag ska "onWayHome" resettas
                if (SJCommands.onWayHome.Value != currentDate)
                    SJCommands.onWayHome = new KeyValuePair<bool, string>(false, null);

                if (currentDateTime.DayOfWeek == DayOfWeek.Friday 
                    || currentDateTime.DayOfWeek == DayOfWeek.Saturday 
                    || currentDateTime.DayOfWeek == DayOfWeek.Sunday)
                {
                    if (Bot.Presence.Status != UserStatus.DoNotDisturb) //bot status => röd
                        await Discord.UpdateStatusAsync(null, UserStatus.DoNotDisturb);
                }
                else
                {
                    if (SJCommands.notifications == true && SJCommands.onWayHome.Key == false)
                    {
                        if (Bot.Presence.Status != UserStatus.Online) //bot status => grön
                            await Discord.UpdateStatusAsync(null, UserStatus.Online);

                        double currentTime = new TimeSpan(currentDateTime.Hour, currentDateTime.Minute, 0).TotalMinutes;
                        //Console.WriteLine(currentTime + "\n");

                        //denna loop kollar om ett tåg går om 20 min, ska avbrytas när den hittar ett tåg
                        foreach (SJTrain train in TrainList)
                        {
                            double time; //tiden som ska användas för att kolla

                            if (train.newDeparture == DateTime.MinValue)
                            {
                                TimeSpan t = new TimeSpan(train.departure.Hour, train.departure.Minute, 0);
                                //Console.WriteLine(t.TotalMinutes);
                                time = t.TotalMinutes;
                            }
                            else
                            {
                                TimeSpan nt = new TimeSpan(train.newDeparture.Hour, train.newDeparture.Minute, 0);
                                //Console.WriteLine(nt.TotalMinutes);
                                time = nt.TotalMinutes;
                            }

                            TimeSpan schoolEnd;
                            TimeSpan lastNotifTime;
                            foreach (KeyValuePair<DayOfWeek, TimeSpan> schoolDay in SchoolDays)
                            {
                                if (currentDateTime.DayOfWeek == schoolDay.Key)
                                {
                                    schoolEnd = schoolDay.Value.Add(new TimeSpan(-1, 0, 0));
                                    lastNotifTime = schoolEnd.Add(new TimeSpan(3, 0, 0));
                                    break;
                                }
                            }
                            if (currentTime + 20 == time && currentTime > schoolEnd.TotalMinutes && currentTime < lastNotifTime.TotalMinutes)
                            {
                                string msg = $"{Me.Mention}, {train.type}: {train.num} departures in 20 minutes from track {train.track}.";
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
                    int ms = 60000 - currentDateTime.Second * 1000;
                    Thread.Sleep(ms);
                    firstTime = false;
                }
                else
                    Thread.Sleep(60000);
            }
        }
    }
}
