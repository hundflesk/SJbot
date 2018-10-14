using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace SJbot
{
    internal class SJTrain
    {
        public int num;
        public int track;
        public TimeSpan departure;

        public SJTrain(int num, int track, TimeSpan departure)
        {
            this.num = num;
            this.track = track;
            this.departure = departure;
        }
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
                Title = "Trains The Coming Hour",
                Color = DiscordColor.SpringGreen,
            };

            var currentDay = DateTime.Now;
            if (currentDay.DayOfWeek == DayOfWeek.Saturday || currentDay.DayOfWeek == DayOfWeek.Sunday)
                msg = $"The trains will not run on weekends.";
            else
            {
                var currentTime = new TimeSpan(currentDay.Hour, currentDay.Minute, 0);
                var timeInterval = currentTime.Add(new TimeSpan(1, 0, 0));

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
                else if (trainQuantity == 1)
                    msg += " It seems like it is only hourtrains which runs right now.";
            }
            embed.Description = msg;
            await ctx.RespondAsync(null, false, embed);
        }


        [Command("notif")]
        [Description("Deactivates or activates notifications. Notifications are activated by default.")]
        public async Task Notifications(CommandContext ctx, string argument)
        {
            string msg = null;

            var embed = new DiscordEmbedBuilder
            {
                Title = "Notifications",
                Color = DiscordColor.SpringGreen,
            };

            string state = "state";
            var setting0 = new string[] { "off", "deactivated" };
            var setting1 = new string[] { "on", "activated" };

            string note = "Setting has not been changed";

            if (argument == state) //ifall användaren vill kolla den nuvarande inställningen
            {
                if (notifications == false)
                    msg = $"Notifications are currently {setting0[1]}.";

                else if (notifications == true)
                    msg = $"Notifications are currently {setting1[1]}.";
            }

            else if (argument == setting0[0] && notifications == true)
            { //ifall användaren vill stänga av notifikationer
                notifications = false;
                msg = $"Notifications have been {setting0[1]}.";
            }
            else if (argument == setting1[0] && notifications == false)
            { //ifall användaren vill sätta på notifikationer
                notifications = true;
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
        [Description("Prints the list of all the trains.")]
        public async Task TrainList(CommandContext ctx)
        {
            string msg = null;

            var embed = new DiscordEmbedBuilder
            {
                Title = "Train List:",
                Color = DiscordColor.SpringGreen,
            };

            foreach (var train in Program.TrainList)
            {
                msg += $"\nTrain: {train.num} - Track: {train.track} - Time: {train.departure}\n";
            }

            embed.Description = msg;
            await ctx.RespondAsync(null, false, embed);
        }
    }

    internal class Program
    {
        private static DiscordClient Discord { get; set; }
        private static CommandsNextModule Commands { get; set; }

        public static List<SJTrain> TrainList { get; private set; }

        private static void Main(string[] args)
        {
            TrainList = AddTrains();
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static List<SJTrain> AddTrains()
        {
            var trains = new List<SJTrain>(); //lista med alla tåg som avgår på en dag

            var timeForFirstTrain = new TimeSpan(9, 14, 0); //första tåget går 09:14
            var timeForLastTrain = new TimeSpan(19, 14, 0); //sista tåget går 19:14
            var timeToAdd = new TimeSpan(0, 30, 0); //tågen går vanligtvis varje halvtimme

            var trainsInfo = new Dictionary<int, int> //TKey = tågnummer, TValue = spår
            {
                {167, 8 }, {724, 13 }, {171, 13 }, {732, 13 },
                {175, 13 }, {740, 13 }, {788, 14 }, {179, 13 },
                {790, 16 }, {748, 11 }, {792, 11 }, {183, 14 },
                {794, 13 }, {756, 14 }, {796, 13 }, {760, 14 }
            }.ToList();

            int lastHourTrainIndex = 5; //är indexet för det sista tåget i 'trainsInfo' som går varje timme
            int index = 0; //är indexet som går igenom alla tåg i 'trainsInfo'

            for (var time = timeForFirstTrain; time <= timeForLastTrain; time = time.Add(timeToAdd))
            {
                trains.Add(new SJTrain(trainsInfo[index].Key, trainsInfo[index].Value, time));

                //de första 5 tågen går varje timme, därför läggs ytterligare 30 min till
                //från och med det 6:e tåget går resten av tågen var 30:e minut istället
                if (index < lastHourTrainIndex)
                    time = time.Add(timeToAdd);

                index++;
            }

            foreach (var t in trains)
            {
                Console.WriteLine(string.Format("Tåg {0} avgår klockan {1:00}:{2:00} från spår {3}",
                    t.num, t.departure.Hours, t.departure.Minutes, t.track));
            }

            if (index == trainsInfo.Count)
                Console.WriteLine("\nSuccess: alla tågen har lagts till i listan.");
            else
                Console.WriteLine("\nError: listan saknar tåg.");

            return trains;
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
            await Task.Run(NotificateAsync);
        }

        private static async Task NotificateAsync()
        {
            var channelSJ = Discord.GetChannelAsync(489823743346999331).Result;
            var userMe = Discord.GetUserAsync(276068458242768907).Result;

            var startActiveTime = new TimeSpan(8, 44, 0).TotalMinutes;
            var endActiveTime = TrainList[TrainList.Count - 1].departure.TotalMinutes;

            string msg = $"{userMe.Mention}, ett tåg går om 20 min. För att hinna med tåget bör du lämna skolan nu.";

            //var testTrain = new TimeSpan(23, 20, 0).TotalMinutes;

            while (true)
            {
                var currentDay = DateTime.Now;

                if (currentDay.DayOfWeek == DayOfWeek.Saturday || currentDay.DayOfWeek == DayOfWeek.Sunday)
                {
                    //ändra bot status till röd

                }
                else
                {
                    var currentTime = new TimeSpan(currentDay.Hour, currentDay.Minute, 0).TotalMinutes;

                    if (currentTime < startActiveTime || currentTime > endActiveTime)
                    {
                        //ändra bot status till gul

                    }
                    else
                    {
                        //ändra bot status till grön


                        if (SJCommands.notifications == true)
                        {
                            foreach (var train in TrainList)
                            {
                                var t = new TimeSpan(train.departure.Hours, train.departure.Minutes, 0).TotalMinutes;

                                if (currentTime == t - 20)
                                {
                                    await Discord.SendMessageAsync(channelSJ, msg);
                                    break;
                                }
                            }
                        }
                    }
                }
                Thread.Sleep(60000);
            }
        }
    }
}
