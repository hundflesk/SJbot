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

        public static List<SJTrain> TrainList { get; private set; }
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

            await Discord.ConnectAsync();

            Thread x = new Thread(UpdateTrainList);
            //x.Start();
            Thread y = new Thread(NotificateAsync);
            y.Start();

            await Task.Delay(-1);
        }

        private static void UpdateTrainList()
        {
            RestClient client = new RestClient()
            {
                Url = "http://api.tagtider.net/v1/stations/243/transfers/departures.json",
                UserName = "tagtider",
                UserPassword = "codemocracy"
            };

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(client.Url);
            request.Method = client.HttpMethod.ToString();

            NetworkCredential credential = new NetworkCredential(client.UserName, client.UserPassword);
            request.Credentials = credential;

            while (true)
            {
                var tempTrainsList = new List<SJTrain>();

                string rawData;

                var response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception("error: " + response.StatusCode.ToString());
                }

                var stream = response.GetResponseStream();
                if (stream != null)
                {
                    var reader = new StreamReader(stream);
                    rawData = reader.ReadToEnd();

                    var data = JsonConvert.DeserializeObject<JsonSJTrainArray[]>(rawData);
                    for (int i = 0; i < 10; i++)
                    {

                    }
                    TrainList = tempTrainsList;
                }
                Thread.Sleep(60000);
            }
        }

        private static async void NotificateAsync()
        {
            var channelSJ = Discord.GetChannelAsync(489823743346999331).Result;
            var userMe = Discord.GetUserAsync(276068458242768907).Result;

            string msg = $"{userMe.Mention}, ett tåg går om 20 min. För att hinna med tåget bör du lämna skolan nu.";

            //var testTrain = new TimeSpan(12, 23, 0).TotalMinutes;
            //var testEndTime = new TimeSpan(0, 0, 0).TotalMinutes;

            while (true)
            {
                var currentDay = DateTime.Now;

                if (currentDay.DayOfWeek == DayOfWeek.Saturday || currentDay.DayOfWeek == DayOfWeek.Sunday || currentDay.DayOfWeek == DayOfWeek.Tuesday)
                {
                    if (Bot.Presence.Status != UserStatus.DoNotDisturb)
                    {
                        //ändra bot status till röd
                        await Discord.UpdateStatusAsync(null, UserStatus.DoNotDisturb);
                    }
                }
                else
                {
                    if (SJCommands.notifications == true)
                    {
                        if (Bot.Presence.Status == UserStatus.DoNotDisturb)
                        {
                            //ändra bot status till grön
                            await Discord.UpdateStatusAsync(null, UserStatus.Online);
                        }

                        var currentTime = new TimeSpan(currentDay.Hour, currentDay.Minute, 0).TotalMinutes;

                        //foreach (var train in TrainList)
                        //{
                        //    var t = new TimeSpan(train.departure.Hours, train.departure.Minutes, 0).TotalMinutes;

                        //    if (currentTime == t - 20) // && currentTime > testEndTime
                        //    {
                        //        await Discord.SendMessageAsync(channelSJ, msg);
                        //        break;
                        //    }
                        //}
                    }
                    else
                    {
                        if (Bot.Presence.Status == UserStatus.DoNotDisturb)
                        {
                            //ändra bot status till gul
                            await Discord.UpdateStatusAsync(null, UserStatus.Idle);
                        }
                    }
                }
                Thread.Sleep(60000);
            }
        }
    }
}
