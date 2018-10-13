using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace SJbot
{
    internal class Tåg
    {
        public int num;
        public int track;
        public TimeSpan departure;

        public Tåg(int num, int track, TimeSpan departure)
        {
            this.num = num;
            this.track = track;
            this.departure = departure;
        }
    }

    internal class SJCommands
    {
        public static bool notifications = true; //notiser är aktiverade by default

        [Command("notis")]
        public async Task Notifications(CommandContext ctx, string setting)
        {
            string msg = null;

            var setting0 = new string[] { "off", "deactivated"};
            var setting1 = new string[] { "on", "activated" };
            string setting2 = "state";

            string note = "Setting has not been changed";

            if (setting == setting0[0] && notifications == true)
            {
                notifications = false;
                msg = $"Notifications have been {setting0[1]}.";
            }
            else if (setting == setting1[0] && notifications == false)
            {
                notifications = true;
                msg = $"Notifications have been {setting1[1]}.";
            }

            else if (setting == setting0[0] && notifications == false)
                msg = $"Notifications are already {setting0[1]}. {note}.";

            else if (setting == setting1[0] && notifications == true)
                msg = $"Notifications are already {setting1[1]}. {note}.";

            else if (setting == setting2)
            {
                if (notifications == false)
                    msg = $"Notifications are currently {setting0[1]}.";
                else if (notifications == true)
                    msg = $"Notifications are currently {setting1[1]}.";
            }
            await ctx.RespondAsync(msg);
        }

        [Command("tåg")]
        public async Task Train(CommandContext ctx)
        {
            string msg = null;

            var currentDay = DateTime.Now;
            if (currentDay.DayOfWeek == DayOfWeek.Saturday || currentDay.DayOfWeek == DayOfWeek.Sunday)
                msg = $"{ctx.User.Mention}, the trains will not run on weekends.";
            else
            {
                var currentTime = new TimeSpan(currentDay.Hour, currentDay.Minute, currentDay.Second);
                var timeInterval = currentTime.Add(new TimeSpan(1, 0, 0));

                msg = $"{ctx.User.Mention}, in this coming hour will the following run:";
                int trainQuantity = 0;

                //kolla vilka tåg som går mellan 'currentTime' och 'timeInterval' (60 min)
                foreach (var train in Program.trainList)
                {
                    if (train.departure > currentTime && train.departure < timeInterval)
                    {
                        msg += $" Train {train.num} at {train.departure} from track {train.track}.";
                        trainQuantity++;
                    }
                }
                if (trainQuantity == 0)
                    msg = $"{ctx.User.Mention}, there is no trains which runs this coming hour.";
                else if (trainQuantity == 1)
                    msg += " It seems like it is only hourtrains which runs right now.";
            }
            await ctx.RespondAsync(msg);
        }
    }

    internal class Program
    {
        private static DiscordClient discord;
        private static CommandsNextModule commands;

        public static List<Tåg> trainList;

        private static void Main(string[] args)
        {
            trainList = AddTrains();
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static List<Tåg> AddTrains()
        {
            var trains = new List<Tåg>(); //lista med alla tåg som avgår på en dag

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
                trains.Add(new Tåg(trainsInfo[index].Key, trainsInfo[index].Value, time));

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
            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = "NDkxOTMwOTE4NTAwNDMzOTMw.DoPDsA.-iBZ_CUDa6_O6CiCjPVrIIH0H6k",
                TokenType = TokenType.Bot,
            });

            commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefix = "?"
            });

            commands.RegisterCommands<SJCommands>();

            await discord.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}
