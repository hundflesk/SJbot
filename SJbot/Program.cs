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
            ChannelSJ = Discord.GetChannelAsync(489823743346999331).Result;
            Me = Discord.GetUserAsync(276068458242768907).Result;

            await Discord.ConnectAsync();

            Thread x = new Thread(CallAPI);
            x.Start();
            Thread y = new Thread(NotificateAsync);
            y.Start();

            await Task.Delay(-1);
        }

        private static void CallAPI()
        {
            RestClient client = new RestClient();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(client.Url);
            request.Method = client.HttpMethod.ToString();

            NetworkCredential credential = new NetworkCredential(client.UserName, client.UserPassword);
            request.Credentials = credential;

            while (true)
            {
                var response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception("error: " + response.StatusCode.ToString());
                }

                var stream = response.GetResponseStream();
                if (stream != null)
                {
                    var reader = new StreamReader(stream);
                    string rawData = reader.ReadToEnd();

                    var data = JsonConvert.DeserializeObject<Rootobject>(rawData);

                    Trains.UpdateTrainList(data);
                }
                Thread.Sleep(60000);
            }
        }

        private static async void NotificateAsync()
        {
            string msg = $"{Me.Mention}, ett tåg går om 20 min. För att hinna med tåget bör du lämna skolan nu.";

            while (true)
            {
                var currentDay = DateTime.Now;

                if (currentDay.DayOfWeek == DayOfWeek.Saturday || currentDay.DayOfWeek == DayOfWeek.Sunday)
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

                        foreach (var train in TrainList)
                        {
                            var t = new TimeSpan(train.departure.Hours, train.departure.Minutes, 0).TotalMinutes;

                            if (currentTime == t - 20) // && currentTime > testEndTime
                            {
                                await Discord.SendMessageAsync(ChannelSJ, msg);
                                break;
                            }
                        }
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
