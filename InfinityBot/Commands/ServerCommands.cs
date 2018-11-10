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
                // Object defined methods
                "add_Output",
                "remove_Output",
                "Equals",
                "GetHashCode",
                "GetType",
                "ToString",
                // User defined methods
                "ExecuteCommand",
            };

            ExcludedMethods = new List<MethodInfo>();

            vs.ForEach(e => ExcludedMethods.Add(this.GetType().GetMethod(e)));

            List<MethodInfo> methods = this.GetType().GetMethods().ToList();
            Commands = methods.Except(ExcludedMethods).ToList();
        }

        /// <summary>
        /// Excluded methods for commands.
        /// </summary>
        public List<MethodInfo> ExcludedMethods;

        public List<MethodInfo> Commands;

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

            MethodInfo command = Commands.Find(cmd => cmd.Name == Helper.Capitalize(cmdTitle)) ?? null;
            if (command != null)
                try
                {
                    command.Invoke(this, invokeParams);
                }
                catch
                {
                    try
                    {
                        command.Invoke(this, null);
                    }
                    catch
                    {
                        Output(this, "Command parameters are invalid.");
                    }
                }
            else
                Output(this, "Command invalid.");
            return Task.CompletedTask;
        }

        public async void Game(Bot bot, string gameTitle)
        {
            if (gameTitle == "game")
            {
                await bot.Client.SetGameAsync(string.Empty);
                Output(this, "Game reset.");
            }
            else
            {
                await bot.Client.SetGameAsync(gameTitle);
                Output(this, "Game set to " + gameTitle);
            }
        }

        public void Help()
        {
            string vs = string.Empty;
            List<string> owo = new List<string>
            {
                "Server-side commands",
                "--------------------"
            };
            owo.ForEach(line => vs += Environment.NewLine + line);
            Commands.ForEach(cmd =>
            {
                string args = " ";
                cmd.GetParameters().Skip(1).ToList().ForEach(x => args += "[" + x.ParameterType.Name + "]");
                vs += Environment.NewLine + cmd.Name + args;
            });
            vs += Environment.NewLine + "--------------------";
            Output(this, vs);
        }

    }
}
