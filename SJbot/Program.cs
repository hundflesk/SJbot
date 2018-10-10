using System;
using System.Threading.Tasks;
using DSharpPlus;

namespace SJbot
{
    internal class Tåg
    {
        public int num;
        public int track;
        public TimeSpan departure;

        Tåg(int num, int track, TimeSpan departure)
        {
            this.num = num;
            this.track = track;
            this.departure = departure;
        }
    }

    internal class Program
    {
        private static DiscordClient discord;

        private static void Main(string[] args)
        {
            AddTrains();
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static void AddTrains()
        {
            var timeForFirstTrain = new TimeSpan(9, 14, 0); //första tåget går 08:14
            var timeForLastTrain = new TimeSpan(19, 14, 0); //sista tåget går 19:14
            bool hourTrains = false;

            var trainNums = new int[]
            {
                167, 724, 171, 732, 175, 740, 788, 179,
                790, 748, 792, 183, 794, 756, 796, 760
            };

            var trainTracks = new int[]
            {
                8, 13, 13, 13, 13, 13, 14, 13,
                16, 11, 11, 14, 13, 14, 13, 14
            };
        }

        private static async Task MainAsync(string[] args)
        {
            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = "NDkxOTMwOTE4NTAwNDMzOTMw.DoPDsA.-iBZ_CUDa6_O6CiCjPVrIIH0H6k",
                TokenType = TokenType.Bot
            });

            discord.MessageCreated += async x =>
            {
                if (x.Message.Content.ToLower() == "tåg")
                {

                }
            };

            await discord.ConnectAsync();
            
        }
    }
}
