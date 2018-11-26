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

            if (currentDateTime.DayOfWeek == DayOfWeek.Friday 
                || currentDateTime.DayOfWeek == DayOfWeek.Saturday 
                || currentDateTime.DayOfWeek == DayOfWeek.Sunday)
                msg = "Today is not a school day.";

            else if (date == onWayHome.Value)
                msg = "You have already informed us you are on your way home today.";

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
            string msg = "This bot notificates you about the departures for the SJ-trains to save your time." +
                "\nIt will also notificate you if any of the trains get cancelled.";

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder
            {
                Title = "Information about this bot:",
                Color = DiscordColor.SpringGreen
            };
            embed.Description = msg;
            await ctx.RespondAsync(null, false, embed);
        }

        public static int minutes = 20;

        [RequireOwner]
        [Command("mins")]
        [Description("Changes the amount of minutes a notification will be sent before a train will departure.")]
        public async Task Minutes(CommandContext ctx, [Description("1 = 20 mins | 2 = 25 mins | 3 = 30 mins")]int argument = 0)
        {
            string msg = null;

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder
            {
                Title = "Notification settings:",
                Color = DiscordColor.SpringGreen
            };

            KeyValuePair<int, int> setting1 = new KeyValuePair<int, int>(1, 20);
            KeyValuePair<int, int> setting2 = new KeyValuePair<int, int>(2, 25);
            KeyValuePair<int, int> setting3 = new KeyValuePair<int, int>(3, 30);

            string note = "This is your current setting. Setting has not been changed.";

            if (argument == 0) //skriver ut den nuvarande inställningen
                msg = $"Current setting: {minutes} before notification.";

            else if (argument == setting1.Key)
            {
                if (minutes == setting1.Value)
                    msg = note;
                else
                {
                    minutes = setting1.Value;
                    msg = $"Changed to setting: {setting1.Key}\nNotifications will be sended {setting1.Value} minutes before departure.";
                }
            }

            else if (argument == setting2.Key)
            {
                if (minutes == setting2.Value)
                    msg = note;
                else
                {
                    minutes = setting2.Value;
                    msg = $"Changed to setting: {setting2.Key}\nNotifications will be sended {setting2.Value} minutes before departure.";
                }
            }

            else if (argument == setting3.Key)
            {
                if (minutes == setting3.Value)
                    msg = note;
                else
                {
                    minutes = setting3.Value;
                    msg = $"Changed to setting: {setting3.Key}\nNotifications will be sended {setting3.Value} minutes before departure.";
                }
            }
            embed.Description = msg;
            await ctx.RespondAsync(null, false, embed);
        }

        public static bool notifications = true; //notifikationer är aktiverade by default

        [RequireOwner]
        [Command("notif")]
        [Description("[OWNER ONLY] Enables or disables notifications. Leave argument blank to get current setting.")]
        public async Task Notifications(CommandContext ctx, [Description("on: enable | off: disable |")]string argument = "current")
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
            if (argument == "current") //ifall användaren vill kolla den nuvarande inställningen
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

            if (currentDateTime.DayOfWeek == DayOfWeek.Friday
                || currentDateTime.DayOfWeek == DayOfWeek.Saturday
                || currentDateTime.DayOfWeek == DayOfWeek.Sunday)
                msg = "Today is not a school day.";

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
