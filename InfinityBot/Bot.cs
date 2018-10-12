using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace InfinityBot
{
    class Bot
    {
        public Bot(string token)
        {
            Token = token;
        }

        public event EventHandler<string> TerminalUpdate;

        DiscordSocketClient client = new DiscordSocketClient();
        CommandService commands = new CommandService();
        IServiceProvider services = new ServiceCollection().BuildServiceProvider();
        public string Token;

        public async Task MainAsync()
        {
            client.Log += Log;
            client.MessageReceived += MessageReceived;

            await InstallCommands();

            await client.LoginAsync(TokenType.Bot, Token);
            await client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        public async Task Stop()
        {
            await client.StopAsync();
        }

        #region Console Outputs

        private Task Log(LogMessage msg)
        {
            TerminalUpdate(this, msg.ToString());
            return Task.CompletedTask;
        }

        private Task Log(SocketMessage msg)
        {
            string message = msg.Timestamp.DateTime + ": #" + msg.Channel.Name + "/" + msg.Author + ": " + msg.Content;
            TerminalUpdate(this, message);
            return Task.CompletedTask;
        }

        #endregion

        #region Event Handlers

        private async Task MessageReceived(SocketMessage message)
        {
            await Log(message);
        }

        #endregion

        #region Commands

        public async Task InstallCommands()
        {
            // Hook the MessageReceived Event into our Command Handler
            client.MessageReceived += HandleCommand;
            // Discover all of the commands in this assembly and load them.
            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public async Task HandleCommand(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message is a command, based on if it starts with '!' or a mention prefix
            if (!(message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))) return;
            // Create a Command Context
            var context = new CommandContext(client, message);
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            await message.DeleteAsync();
            var result = await commands.ExecuteAsync(context, argPos, services);
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }

        #endregion
    }
}
