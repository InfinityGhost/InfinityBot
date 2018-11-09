using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace InfinityBot.Commands
{
    class ServerCommands
    {
        public ServerCommands()
        {
            List<string> vs = new List<string>
            {
                "ExecuteCommand",

            };

            ExcludedMethods = new List<MethodInfo>();

            vs.ForEach(e => ExcludedMethods.Add(this.GetType().GetMethod(e)));
        }

        /// <summary>
        /// Excluded methods for commands.
        /// </summary>
        public List<MethodInfo> ExcludedMethods;

        public event EventHandler<object> Output;

        public Task ExecuteCommand(Bot bot, string commandParam)
        {
            string cmdTitle = commandParam.Split(' ')[0];
            string cmdParams = commandParam.Replace(cmdTitle + " ", string.Empty) ?? string.Empty;
            object[] invokeParams =
            {
                bot,
                cmdParams,
            };

            List<MethodInfo> methods = this.GetType().GetMethods().ToList();
            List<MethodInfo> commands = methods.Except(ExcludedMethods).ToList();

            MethodInfo command = commands.Find(e => e.Name == Helper.Capitalize(cmdTitle)) ?? null;
            if (command != null)
                command.Invoke(this, invokeParams);
            else
                Output(this, "Command invalid.");
            return Task.CompletedTask;
        }

        public async void Game(Bot bot, string gameTitle)
        {
            await bot.Client.SetGameAsync(gameTitle);
            Output(this, "Game set to " + gameTitle);
        }

    }
}
