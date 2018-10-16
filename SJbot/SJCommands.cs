using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace SJbot
{
    public class SJCommands
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
                if (Program.Bot.Presence.Status != UserStatus.DoNotDisturb)
                    await Program.Discord.UpdateStatusAsync(null, UserStatus.Idle);

                msg = $"Notifications have been {setting0[1]}.";
            }
            else if (argument == setting1[0] && notifications == false)
            { //ifall användaren vill sätta på notifikationer
                notifications = true;

                //ändra bot status till grön
                if (Program.Bot.Presence.Status != UserStatus.DoNotDisturb)
                    await Program.Discord.UpdateStatusAsync(null, UserStatus.Online);

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
        [Description("Prints a detailed list of the ten following trains.")]
        public async Task TrainList(CommandContext ctx)
        {
            string msg = null;

            var embed = new DiscordEmbedBuilder
            {
                Title = "List of the ten trains which:",
                Color = DiscordColor.SpringGreen,
            };

            foreach (var train in Program.TrainList)
                msg += $"\nTrain: {train.num} - Track: {train.track} - Time: {train.departure}\n";

            embed.Description = msg;
            await ctx.RespondAsync(null, false, embed);
        }
    }
}
