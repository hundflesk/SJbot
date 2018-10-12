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
        [Command("help")] //skriver ner alla kommandon som kan användas
        public async Task Help(CommandContext ctx)
        {
            await ctx.RespondAsync($"{ctx.User.Mention}, här en lista med kommandon som du kan använda:");

            //skriv ut alla kommandon som kan användas


            await ctx.RespondAsync($"Du kan använda ?{}");
        }

        [Command("command")] //skriver hur ett kommando fungerar
        public async Task CommandHelp(CommandContext ctx, string command)
        {
            await ctx.RespondAsync($"{ctx.User.Mention}, ");
        }

        [Command("tåg")] //skriver vilka tåg som går inom de kommande 60 minuterna
        public async Task NextTrain(CommandContext ctx)
        {
            var currentTime = ;
            var timeInterval = currentTime.Add(new TimeSpan(1, 0, 0));

            //kolla vilka tåg som går mellan 'currentTime' och 'timeInterval' (60 min)
            foreach (var train in Program.trainList)
            {
                if (train.departure > currentTime && train)
                {

                }
            }

            await ctx.RespondAsync($"");
        }

        [Command("notis")] //
        public async Task Notification(CommandContext ctx)
        {

        }
    }

    internal class Program
    {
        private static DiscordClient discord;
        private static CommandsNextModule commands;

        public static List<Tåg> trainList = AddTrains();

        private static void Main(string[] args)
        {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static List<Tåg> AddTrains()
        {
            var trains = new List<Tåg>();

            var timeForFirstTrain = new TimeSpan(9, 14, 0); //första tåget går 08:14
            var timeForLastTrain = new TimeSpan(19, 14, 0); //sista tåget går 19:14
            var timeToAdd = new TimeSpan(0, 30, 0); //tågen går vanligtvis varje halvtimme

            var trainsInfo = new Dictionary<int, int> //TKey = tågnummer, TValue = spår
            {
                {167, 8 }, {724, 13 }, {171, 13 }, {732, 13 },
                {175, 13 }, {740, 13 }, {788, 14 }, {179, 13 },
                {790, 16 }, {748, 11 }, {792, 11 }, {183, 14 },
                {794, 13 }, {756, 14 }, {796, 13 }, {760, 14 }
            }.ToList();

            int lastHourTrainIndex = 6; //är indexet för det sista tåget i 'trainsInfo' som går varje timme
            int index = 0; //är indexet som går igenom alla tåg i 'trainsInfo'

            for (var time = timeForFirstTrain; time <= timeForLastTrain; time = time.Add(timeToAdd))
            {
                trains.Add(new Tåg(trainsInfo[index].Key, trainsInfo[index].Value, time));

                //de första sex tågen går varje timme, därför läggs ytterligare 30 min till
                //från och med det 7:e tåget går resten av tågen var 30:e minut istället
                if (index < lastHourTrainIndex)
                    time = time.Add(timeToAdd);

                index++;
            }

            foreach (var t in trains)
            {
                Console.WriteLine(string.Format("Tåg {0} avgår klockan {01}:{2} från spår {3}",
                    t.num, t.departure.Hours, t.departure.Minutes, t.track));
            }

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
