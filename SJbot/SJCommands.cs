using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace SJbot
{
    public class SJCommands
    {
        public static bool notifications = true; //notifikationer är aktiverade by default

        [Command("notif")]
        [Description("Enables or disables notifications. Leave argument blank to get current setting.")]
        public async Task Notifications(CommandContext ctx, [Description("on: enable | off: disable |")]string argument = "state")
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

            //"state" är default value för argumentet, används för att kolla den nuvarande inställningen
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
        [Description("Prints a detailed list of a couple of trains.")]
        public async Task TrainList(CommandContext ctx)
        {
            string msg = null;

            var embed = new DiscordEmbedBuilder
            {
                Title = "List of trains which will departure today:",
                Color = DiscordColor.SpringGreen,
            };

            foreach (var train in Program.TrainList)
            {
                msg += $"\n{train.type}: {train.num} - Track: {train.track} - Time: {train.departure}";

                if (train.comment != null && train.newDeparture.TotalMinutes != 0)
                    msg += $" --> New Time: {train.newDeparture} - Info: {train.comment}";

                msg += "\n";
            }

            embed.Description = msg;
            await ctx.RespondAsync(null, false, embed);
        }
    }
}
