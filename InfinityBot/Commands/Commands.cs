using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord.Commands;

namespace InfinityBot.Commands
{
    public class Commands : ModuleBase
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
            string x = "**Announcement** @everyone " + Environment.NewLine + text;
            await ReplyAsync(x);
        }

        [Command("ping"), Summary("Pong!")]
        public async Task Ping()
        {
            await ReplyAsync("Pong!");
        }
    }
}
