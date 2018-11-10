using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
            MethodInfo command = Commands.Find(cmd => cmd.Name == Helper.Capitalize(cmdTitle)) ?? null;
            
            List<ParameterInfo> parameters = command.GetParameters().ToList() ?? new List<ParameterInfo>();

            string cmdParams = commandParam.Replace(cmdTitle + " ", string.Empty) ?? string.Empty;
            List<object> invokeParams = new List<object>
            {
                bot,
            };

            if (parameters.ToArray().Length > 1)
                invokeParams.Add(TypeConverter.ConvertParameter(parameters[1], cmdParams));

            if (command != null)
                try
                {
                    command.Invoke(this, invokeParams.ToArray());
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
                Output(this, "Error: Invalid command. Do /help to get a list of commands.");
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

        public void Getguildid(Bot bot)
        {
            var channel = bot.Channel as SocketGuildChannel;
            Output(this, channel.Guild.Name + ".ID:" + channel.Guild.Id);
            Clipboard.SetText(channel.Guild.Id.ToString());
        }

        public void Getchannelid(Bot bot)
        {
            var channel = bot.Channel as SocketGuildChannel;
            Output(this, channel.Guild.Name + "/#" + channel.Name + ".ID:" + channel.Id);
        }

        public void Del(Bot bot, string parameters)
        {
            if (bot.Channel is SocketTextChannel textChannel)
                textChannel.SendMessageAsync($"&del {parameters}");
        }
    }
}
