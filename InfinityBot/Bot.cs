using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace InfinityBot
{
    public class Bot
    {
        public Bot(string token)
        {
            Token = token;
        }

        #region Variables

        public readonly string Token;

        public event EventHandler<string> Output;

        public DiscordSocketClient Client = new DiscordSocketClient();
        CommandService UserCommands = new CommandService();
        CommandService AdminCommands = new CommandService();
        Commands.ServerCommands ServerCommands;
        IServiceProvider Services = new ServiceCollection().BuildServiceProvider();

        public SocketMessage ReplyMessage;
        public SocketChannel Channel;

        #endregion

        #region Main

        public async Task Start()
        {
            Client.Log += Log;
            Client.MessageReceived += Log;
            Client.Ready += Client_Ready;

            await Client.LoginAsync(TokenType.Bot, Token);
            await Client.StartAsync();

            await Task.Delay(-1);
        }

        public async Task Stop()
        {
            await Client.StopAsync();
            Output(this, "Bot has been stopped.");
        }

        private async Task Client_Ready()
        {
            await InstallCommands();
            Output(this, "Username: " + Client.CurrentUser.Username);
        }

        #endregion

        #region Tasks

        public async Task ServerMessage(string text, SocketChannel socketChannel)
        {
            if (!await HandleServerCommand(text))
            {
                if (socketChannel is SocketTextChannel channel)
                {
                    if (text != string.Empty || text != null)
                        await channel.SendMessageAsync(text);
                    Channel = channel;
                }
            }
        }

        public async Task ReplyToMessage(string text)
        {
            if(!await HandleServerCommand(text))
            {
                if (ReplyMessage != null)
                {
                    await ServerMessage(text, ReplyMessage.Channel as SocketChannel);
                    Channel = (ReplyMessage.Channel as SocketChannel);
                }
                else
                    Output(ReplyMessage, "Message is null.");
            }
        }

        public async Task MessageDirect(string text, ulong channelID)
        {
            var channel = Client.GetChannel(channelID);
            await ServerMessage(text, channel);
        }

        public SocketGuildChannel GetChannel(ulong channelID)
        {
            return Client.GetChannel(channelID) as SocketGuildChannel ?? null;
        }

        public SocketGuildChannel[] GetChannels(ulong guildID)
        {
            SocketGuild guild = Client.GetGuild(guildID) ?? null;

            if (guild != null)
                return guild.Channels.Where(channel => channel is SocketTextChannel).ToArray();
            else
                throw new ArgumentException();
        }

        #endregion

        #region Event Handlers

        private Task Log(string msg)
        {
            Output(this, "Log: " + msg);
            return Task.CompletedTask;
        }

        private Task Log(LogMessage msg)
        {
            if (msg.Message != null)
                Output(this, "Log: " + msg.Message);
            return Task.CompletedTask;
        }

        private Task Log(SocketMessage msg)
        {
            if (msg.Content == null)
                return Task.CompletedTask;

            ReplyMessage = msg ?? ReplyMessage;

            string message = string.Empty;
            if (msg.Channel is SocketGuildChannel guildChannel)
            {
                message = guildChannel.Guild.Name + "/#" + guildChannel.Name + "/" + msg.Author + ": ";
            }
            else if (msg.Channel is SocketDMChannel dmChannel)
            {
                var users = dmChannel.Users;
                int count = users.ToArray().Length;

                message = "DM ";
                if (count > 2)
                {
                    message += "{";
                    users.Skip(1).Take(count - 1).ToList().ForEach(e =>
                    {
                        message += $"{e.Username}, ";
                    });
                    message += users.Last().Username + "}" + ": ";
                }
                else
                {
                    message += users.Last().Username + ": ";
                }
            }

            if (msg.Content.Contains("\n"))
                message += Environment.NewLine + msg.Content;
            else
                message += msg.Content;

            if (msg.Attachments.ToList() != new List<Attachment> { })
            {
                var attachments = msg.Attachments.ToList();
                int count = attachments.ToArray().Length;
                if (count != 0)
                {
                    message += " {";
                    attachments.Except(attachments.Skip(count - 1)).ToList().ForEach(attachment =>
                    {
                        message += attachment.Filename + ", ";
                    });
                    message += attachments.Last().Filename + "}";
                }
            }
            
            Output(this, message);

            return Task.CompletedTask;
        }

        #endregion

        #region Commands

        private async Task InstallCommands()
        {
            Client.MessageReceived += HandleUserCommand;
            await UserCommands.AddModuleAsync(typeof(Commands.UserCommands), Services);
            Output(UserCommands, "UserCommands installed.");

            Client.MessageReceived += HandleAdminCommand;
            await AdminCommands.AddModuleAsync(typeof(Commands.AdminCommands), Services);
            Output(AdminCommands, "AdminCommands installed.");

            ServerCommands = new Commands.ServerCommands();
            ServerCommands.Output += ServerCommands_Output;
            Output(ServerCommands, "ServerCommands installed.");
        }

        private void ServerCommands_Output(object sender, object e)
        {
            if (e is string x)
                Output(sender, x);
        }

        private async Task HandleUserCommand(SocketMessage messageParam)
        {
            if (!(messageParam is SocketUserMessage message))
                return;
            int argPos = 0;
            if (!message.HasCharPrefix('$', ref argPos))
                return;
            var context = new CommandContext(Client, message);
            var result = await UserCommands.ExecuteAsync(context, argPos, Services);
            if (!result.IsSuccess)
            {
                await messageParam.DeleteAsync();
                var reply = await context.Channel.SendMessageAsync("Error: " + result.ErrorReason);
                await Task.Delay(Commands.UserCommands.Delay);
                await reply.DeleteAsync();
            }
        }

        private async Task HandleAdminCommand(SocketMessage messageParam)
        {
            if (!(messageParam is SocketUserMessage message))
                return;
            int argPos = 0;
            if (!message.HasCharPrefix('&', ref argPos))
                return;
            var context = new CommandContext(Client, message);
            var result = await AdminCommands.ExecuteAsync(context, argPos, Services);
            if (!result.IsSuccess)
            {
                await messageParam.DeleteAsync();
                var reply = await context.Channel.SendMessageAsync("Error: " + result.ErrorReason);
                await Task.Delay(Commands.UserCommands.Delay);
                await reply.DeleteAsync();
            }
        }

        private async Task<bool> HandleServerCommand(string messageParam)
        {
            if (messageParam.StartsWith("/"))
            {
                string command = messageParam.TrimStart('/');
                await ServerCommands.ExecuteCommand(this, command);
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion
    }
}
