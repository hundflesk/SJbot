using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Newtonsoft.Json;

namespace SJbot
{
    internal class SJTrain
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

    internal struct SJTrainJson
    {
        [JsonProperty("type")]
        public string Type { get; private set; }

        [JsonProperty("train")]
        public string Num { get; private set; }

        [JsonProperty("track")]
        public string Track { get; private set; }

        [JsonProperty("departure")]
        public string Departure { get; private set; }

        [JsonProperty("newDeparture")]
        public string NewDeparture { get; private set; }

        [JsonProperty("comment")]
        public string Comment { get; private set; }
    }

    internal class SJCommands
    {
        public static bool notifications = true; //notifikationer är aktiverade by default

        [Command("hour")]
        [Description("Tells which trains will run the coming hour. Note: Will not work on weekends.")]
        public async Task ComingHour(CommandContext ctx)
        {
            string msg = null;

            var embed = new DiscordEmbedBuilder
            {
                Title = "Trains the coming hour:",
                Color = DiscordColor.SpringGreen,
            };

            var currentDay = DateTime.Now;

            if (currentDay.DayOfWeek == DayOfWeek.Saturday || currentDay.DayOfWeek == DayOfWeek.Sunday)
                msg = $"The trains will not run on weekends.";
            else
            {
                var currentTime = new TimeSpan(currentDay.Hour, currentDay.Minute, 0);
                var timeInterval = currentTime.Add(new TimeSpan(1, 0, 0));

                //används för att kolla om tågen den kommande timmen är dem sista
                var scndLastTrain = Program.TrainList[Program.TrainList.Count - 2].departure.TotalMinutes;

                msg = $"In this coming hour will the following run:\n\n";
                int trainQuantity = 0;

                //kolla vilka tåg som går mellan 'currentTime' och 'timeInterval' (60 min)
                foreach (var train in Program.TrainList)
                {
                    if (train.departure > currentTime && train.departure < timeInterval)
                    {
                        msg += $" Train {train.num} at {train.departure} from track {train.track}.\n";
                        trainQuantity++;
                    }
                }
                if (trainQuantity == 0)
                    msg = $"There is no trains which runs this coming hour.";

                //här används variabeln för att kolla ifall det är timtåg som går den kommande timmen
                //botten ska inte säga att det är timtåg som går om det bara är det sista tåget som går
                else if (trainQuantity == 1 && currentTime.TotalMinutes > scndLastTrain)
                    msg += "\nIt seems like it is only hourtrains which runs right now.";
            }
            embed.Description = msg;
            await ctx.RespondAsync(null, false, embed);
        }


        [Command("notif")]
        [Description("Enables or disables notifications. Notifications are enabled by default.")]
        public async Task Notifications(CommandContext ctx, string argument = "state")
        {
            string msg = null;

            var embed = new DiscordEmbedBuilder
            {
                Title = "Notifications:",
                Color = DiscordColor.SpringGreen,
            };

            var setting0 = new string[] { "off", "disabled" };
            var setting1 = new string[] { "on", "enabled" };

            string note = "Setting has not been changed";

            //"state" är default value för argumentet
            if (argument == "state") //ifall användaren vill kolla den nuvarande inställningen
            {
                if (notifications == false)
                    msg = $"Notifications are currently {setting0[1]}.";

                else if (notifications == true)
                    msg = $"Notifications are currently {setting1[1]}.";
            }

            else if (argument == setting0[0] && notifications == true)
            { //ifall användaren vill stänga av notifikationer
                notifications = false;

                //ändra bot status till gul

                msg = $"Notifications have been {setting0[1]}.";
            }
            else if (argument == setting1[0] && notifications == false)
            { //ifall användaren vill sätta på notifikationer
                notifications = true;

                //ändra bot status till grön

                msg = $"Notifications have been {setting1[1]}.";
            }

            //ifall när användaren försöker stänga av notifikationer men de är redan på
            else if (argument == setting0[0] && notifications == false)
                msg = $"Notifications are already {setting0[1]}. {note}.";

            //ifall när användaren försöker sätta på notifikationer men de är redan av
            else if (argument == setting1[0] && notifications == true)
                msg = $"Notifications are already {setting1[1]}. {note}.";

            embed.Description = msg;
            await ctx.RespondAsync(null, false, embed);
        }

        [Command("trains")]
        [Description("Prints a detailed list of all the trains which runs this day.")]
        public async Task TrainList(CommandContext ctx)
        {
            string msg = null;

            var embed = new DiscordEmbedBuilder
            {
                Title = "List of all the trains which runs today:",
                Color = DiscordColor.SpringGreen,
            };

            foreach (var train in Program.TrainList)
                msg += $"\nTrain: {train.num} - Track: {train.track} - Time: {train.departure}\n";

            embed.Description = msg;
            await ctx.RespondAsync(null, false, embed);
        }
    }

    internal class Program
    {
        private static DiscordClient Discord { get; set; }
        private static CommandsNextModule Commands { get; set; }

        public static List<SJTrain> TrainList { get; private set; }
        public static List<KeyValuePair<DayOfWeek, TimeSpan>> SchoolDays { get; private set; }

        private static void Main(string[] args)
        {
            SchoolDays = AddSchoolDays();
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

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

            await Discord.ConnectAsync();
            Thread x = new Thread(UpdateTrainsList);
            x.Start();
            Thread y = new Thread(NotificateAsync);
            y.Start();
            await Task.Delay(-1);
        }

        private static void UpdateTrainsList()
        {
            var tempTrainsList = new List<SJTrain>();

            string json;
            using (var wc = new WebClient())
            {
                json = wc.DownloadString("http://api.tagtider.net/v1/stations/243/transfers/departures.json");
            }
            var trainsJson = JsonConvert.DeserializeObject<SJTrainJson>(json);


            TrainList = tempTrainsList;
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

                if (currentDay.DayOfWeek == DayOfWeek.Saturday || currentDay.DayOfWeek == DayOfWeek.Sunday)
                {
                    if (userMe.Presence.Status != UserStatus.DoNotDisturb)
                    {
                        //ändra bot status till röd
                    }
                }
                else
                {
                    if (SJCommands.notifications == true)
                    {
                        //if (userMe.Presence.Status == UserStatus.DoNotDisturb)
                        //{
                        //    //ändra bot status till grön
                        //}

                        var currentTime = new TimeSpan(currentDay.Hour, currentDay.Minute, 0).TotalMinutes;

                        foreach (var train in TrainList)
                        {
                            var t = new TimeSpan(train.departure.Hours, train.departure.Minutes, 0).TotalMinutes;

                            if (currentTime == t - 20) // && currentTime > testEndTime
                            {
                                await Discord.SendMessageAsync(channelSJ, msg);
                                break;
                            }
                        }
                    }
                    else
                    {
                        //if (userMe.Presence.Status == UserStatus.DoNotDisturb)
                        //{
                        //    //ändra bot status till gul
                        //}
                    }
                }
                Thread.Sleep(60000);
            }
        }
    }
}
