using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace InfinityBot.Commands
{
    public class UserCommands : ModuleBase
    {
        [Command("say"), Summary("Echos a message")]
        public async Task Say([Remainder, Summary("The text to echo")] string echo)
        {
            await Context.Message.DeleteAsync();
            await ReplyAsync(echo);
        }

        [Command("announce"), Summary("Creates an announcement in the current channel")]
        public async Task Announce([Remainder, Summary("The text to announce")] string text)
        {
            await Context.Message.DeleteAsync();
            var channel = Context.Channel;
            string replyText = text;
            bool IsVote;
            try
            {
                string owo = text.Substring(text.IndexOf("vote: "));
                replyText = owo;
                IsVote = true;
            }
            catch
            {
                IsVote = false;
            }
            string x = "**Announcement** @everyone " + Environment.NewLine + replyText;
            var reply = await ReplyAsync(x);
            if (IsVote == true)
            {
                Emoji[] reactions =
                {
                    new Emoji("👍"),
                    new Emoji("👎"),
                };
                Array.ForEach(reactions, async reaction =>
                {
                    await reply.AddReactionAsync(reaction);
                });
            }
        }

        [Command("ping"), Summary("Pong!")]
        public async Task Ping()
        {
            await ReplyAsync("Pong!");
        }

        [Command("despacito")]
        public async Task Despacito()
        {
            await Context.Message.DeleteAsync();
            await ReplyAsync("https://www.youtube.com/watch?v=kJQP7kiw5Fk");
        }
    }
}
