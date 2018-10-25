using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Reflection;

namespace InfinityBot.Commands
{
    class AdminCommands : ModuleBase
    {
        // TODO: port all commands from ServerCommands

        [Command("del"), Summary("Deletes multiple messages.")]
        public async Task Delete([Remainder, Summary("Amount & type")] string prmstring)
        {
            await Context.Message.DeleteAsync(); // delete message sent

            var user = Context.User as SocketGuildUser;
            var permissions = user.GuildPermissions;
            if (permissions.Administrator == true || permissions.ManageMessages == true)
            {
                string[] parameters = prmstring.Split(' ');

                int msgCount = Convert.ToInt32(parameters[0]);
                var channel = Context.Channel as SocketGuildChannel;

                // TODO: make message count allowed over 100
                if (msgCount > 100)
                    msgCount = 100;

                var msgCollection = await (channel as ISocketMessageChannel).GetMessagesAsync(msgCount + 1).FlattenAsync();
                if (msgCollection == null)
                {
                    var errorMsg = await ReplyAsync("Error: No messages to delete.");
                    await Task.Delay(2000);
                    await errorMsg.DeleteAsync();
                    return;
                }

                string reply = string.Empty;
                string text = string.Empty;
                try
                {
                    text = parameters[1];
                }
                catch { }

                if (text != string.Empty)
                {
                    msgCollection = msgCollection.Where(msg => msg.Content.Contains(text));
                    await (channel as SocketTextChannel).DeleteMessagesAsync(msgCollection);
                    reply = $"Deleted {msgCollection.ToArray().Length} messages containing \"{text}\" from {channel.Guild.Name}/#{channel.Name}.";
                }
                else
                {
                    await (channel as SocketTextChannel).DeleteMessagesAsync(msgCollection);
                    reply = $"Deleted {msgCollection.ToArray().Length} messages from {channel.Guild.Name}/#{channel.Name}.";
                }
                var replyMsg = await ReplyAsync(reply);
                await Task.Delay(2000);
                await replyMsg.DeleteAsync();
            }
            else
            {
                var reply = await ReplyAsync("No permission to manage messages.");
                await Task.Delay(5000);
                await reply.DeleteAsync();
            }
        }

        [Command("del"), Summary("Deletes a single message.")]
        public async Task Delete()
        {
            await Context.Message.DeleteAsync();
            var user = Context.User as SocketGuildUser;
            var permissions = user.GuildPermissions;

            if (permissions.Administrator || permissions.ManageMessages)
            {
                var channel = Context.Channel as ISocketMessageChannel;
                var msgCollection = await channel.GetMessagesAsync(1).FlattenAsync();
                var msgArray = msgCollection.ToArray();

                try
                {
                    await (channel as SocketTextChannel).DeleteMessagesAsync(msgCollection);
                }
                catch (IndexOutOfRangeException)
                {
                    var x = await ReplyAsync("No messages to delete.");
                    await Task.Delay(5000);
                    await x.DeleteAsync();
                }
            }
            else
            {
                var reply = await ReplyAsync("No permission to manage messages.");
                await Task.Delay(5000);
                await reply.DeleteAsync();
            }
        }

        [Command("game"), Summary("Sets active game.")]
        public async Task SetGame([Remainder, Summary("Game title")] string game)
        {
            var user = Context.User as SocketGuildUser;
            if (user.Id == (Context.Client as DiscordSocketClient).CurrentUser.Id || user.Id == 193491406386364425)
            {
                await (Context.Client as DiscordSocketClient).SetGameAsync(game);
            }
        }
        
    }
}
