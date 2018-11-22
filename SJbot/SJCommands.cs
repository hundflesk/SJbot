using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace SJbot
{
    public class SJCommands
    {
        public static KeyValuePair<bool, string> onWayHome = new KeyValuePair<bool, string>(false, null);

        [RequireOwner]
        [Command("done")]
        [Description("[OWNER ONLY] Tells us you are on your way home from the train station.")]
        public async Task Done(CommandContext ctx)
        {
            string msg = null;
            DateTime currentDateTime = DateTime.Now.Add(Program.BeagleAdd);
            string date = currentDateTime.ToShortDateString();

            if (currentDateTime.DayOfWeek == DayOfWeek.Saturday || currentDateTime.DayOfWeek == DayOfWeek.Sunday)
                msg = "Error. Trains do not run on weekends.";
            else if (date == onWayHome.Value)
                msg = "You have already informed us you are on your way home.";
            else
            {
                msg = "Understood. You will no longer recieve notifications about departures for today.";
                onWayHome = new KeyValuePair<bool, string>(true, date);
            }
            await ctx.RespondAsync(msg);
        }

        [Command("info")]
        [Description("Explains what this bot does.")]
        public async Task Information(CommandContext ctx)
        {
            string msg = "This bot notificates you about the departures for the SJ-trains to save your time.";

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder
            {
                Title = "Information about this bot:",
                Color = DiscordColor.SpringGreen
            };

            embed.Description = msg;
            await ctx.RespondAsync(null, false, embed);
        }

        public static bool notifications = true; //notifikationer är aktiverade by default

        [RequireOwner]
        [Command("notif")]
        [Description("[OWNER ONLY] Enables or disables notifications. Leave argument blank to get current setting.")]
        public async Task Notifications(CommandContext ctx, [Description("on: enable | off: disable |")]string argument = "state")
        {
            string msg = null;

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder
            {
                Title = "Notifications:",
                Color = DiscordColor.SpringGreen
            };

            KeyValuePair<string, bool> disabled = new KeyValuePair<string, bool>("off", false);
            KeyValuePair<string, bool> enabled = new KeyValuePair<string, bool>("on", true);

            string text = "Notifications are already";
            string note = "Setting has not been changed";
            string change = "Notifications have been";

            //"state" är default value för argumentet, används för att kolla den nuvarande inställningen
            if (argument == "state") //ifall användaren vill kolla den nuvarande inställningen
            {
                msg = "Notifications are currently ";

                if (notifications == false)
                    msg += "disabled";

                else if (notifications == true)
                    msg += "enabled.";
            }
            else if (argument == disabled.Key)
            {
                if (notifications == disabled.Value)
                    msg = $"{text} disabled. {note}";

                else
                {
                    notifications = disabled.Value;
                    msg = $"{change} disabled.";
                }
            }
            else if (argument == enabled.Key)
            {
                if (notifications == enabled.Value)
                    msg = $"{text} enabled. {note}";

                else
                {
                    notifications = enabled.Value;
                    msg = $"{change} enabled.";
                }
            }
            embed.Description = msg;
            await ctx.RespondAsync(null, false, embed);
        }

        [Command("trains")]
        [Description("Prints a detailed list of a couple of trains which will departure this day.")]
        public async Task TrainList(CommandContext ctx)
        {
            string msg = null;

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder
            {
                Title = "No list for today.",
                Color = DiscordColor.SpringGreen
            };

            DateTime currentDateTime = DateTime.Now.Add(Program.BeagleAdd);

            if (currentDateTime.DayOfWeek == DayOfWeek.Saturday || currentDateTime.DayOfWeek == DayOfWeek.Sunday)
                msg = "The trains do not run on weekends.";

            else if (Program.TrainList.Count == 0)
                msg = "All the trains have departured for today.";

            else
            {
                embed.Title = "List of trains which will departure today:";
                foreach (SJTrain train in Program.TrainList)
                {
                    msg += $"\n{train.type}: {train.num} - Track: {train.track} - Time: {train.departure.ToLongTimeString()}";

                    if (train.newDeparture != DateTime.MinValue)
                        msg += $"\nNew Time: {train.newDeparture.ToLongTimeString()} - Info: {train.comment}";

                    msg += "\n";
                }
            }
            embed.Description = msg;
            await ctx.RespondAsync(null, false, embed);
        }
    }
}
