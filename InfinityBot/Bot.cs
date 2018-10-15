﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        readonly IServiceProvider services = new ServiceCollection().BuildServiceProvider();
        private string Token;
        readonly string TimePrefix = DateTime.Now + ": ";

        public async Task MainAsync()
        {
            client.Log += Log;
            client.MessageReceived += MessageReceived;
            client.MessageUpdated += MessageUpdated;
            client.Ready += StartupCommands;

            await client.LoginAsync(TokenType.Bot, Token);
            await client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private async Task StartupCommands()
        {
            await InstallCommands();
        }

        public async Task Stop()
        {
            await client.StopAsync();
            TerminalUpdate(this, "Client has stopped.");
        }

        #region Console Outputs

        SocketMessage lastMessage;

        private Task Log(LogMessage msg)
        {
            TerminalUpdate(this, msg.Message);
            return Task.CompletedTask;
        }

        private Task Log(SocketMessage msg)
        {
            lastMessage = msg;
            var channel = msg.Channel as SocketGuildChannel;
            string message = channel.Guild.Name + "/#" + msg.Channel.Name + "/" + msg.Author + ": " + msg.Content;
            TerminalUpdate(this, message);
            return Task.CompletedTask;
        }

        #endregion

        #region Messaging through UI

        public async Task ReplyToMessage(string text)
        {
            if (text.StartsWith("!"))
            {
                if(lastMessage != null)
                {
                    await ServerCommand(text, lastMessage.Channel as SocketTextChannel);
                }
                else
                {
                    await ServerCommand(text, null);
                }
            }
            else
            {
                try
                {
                    await lastMessage.Channel.SendMessageAsync(text);
                }
                catch
                {
                    TerminalUpdate(this, "Error: Message failed to send; No channel to reply to.");
                    return;
                }
            }
        }

        public async Task MessageDirect(string text, ulong channelID)
        {
            if (client.GetChannel(channelID) is SocketTextChannel channel)
            {
                if (text.StartsWith("!"))
                {
                    await ServerCommand(text, channel);
                }
                else
                {
                    await channel.SendMessageAsync(text);
                }
            }
            else
            {
                TerminalUpdate(this, "Error: Channel ID is either invalid or channel is not a text channel.");
            }
        }

        #endregion

        #region Event Handlers


        private async Task MessageReceived(SocketMessage message)
        {
            await Log(message);
        }

        private Task MessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
        {
            // Add textbox updating code
            return Task.CompletedTask;
        }


        #endregion

        #region Channels

        public SocketGuildChannel[] GetChannels(ulong guildID)
        {
            var guild = client.GetGuild(guildID);
            var textChannels = guild.Channels.ToArray() as SocketChannel[];
            SocketTextChannel[] channels = { };
            Array.ForEach(textChannels, item =>
            {
                if (item is SocketTextChannel x)
                {
                    Array.Resize(ref channels, channels.Length + 1);
                    channels[channels.Length - 1] = x;
                }
            });
            return channels;

            //throw new NotImplementedException();
        }
        public event EventHandler<ulong> AddGuildRequested;

        public SocketGuildChannel GetChannel(ulong channelID)
        {
            if(client.GetChannel(channelID) is SocketGuildChannel x)
            {
                return x;
            }
            else
            {
                throw new ArgumentException();
            }
        }
        public event EventHandler<ulong> AddChannelRequested;

        #endregion

        #region Commands

        private async Task InstallCommands()
        {
            // Hook the MessageReceived Event into our Command Handler
            client.MessageReceived += HandleCommand;
            // Discover all of the commands in this assembly and load them.
            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        private async Task HandleCommand(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            var message = messageParam as SocketUserMessage;
            if (message == null)
            {
                return;
            }
            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message is a command, based on if it starts with '!' or a mention prefix
            if (!(message.HasCharPrefix('$', ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos)))
            {
                return;
            }
            // Create a Command Context
            var context = new CommandContext(client, message);
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = await commands.ExecuteAsync(context, argPos, services);
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }

        public async Task ServerCommand(string cmd, SocketTextChannel channel)
        {
            string parameters = string.Empty;
            try
            {
                parameters = cmd.Substring(cmd.IndexOf(' ') + 1);
            }
            catch { }
            if (cmd.StartsWith("!addguild"))
            {
                AddGuildCommand(parameters);
            }
            else if (cmd.StartsWith("!addchannel"))
            {
                AddChannelRequested(this, Convert.ToUInt64(parameters));
            }
            else if (cmd.StartsWith("!getchannelid"))
            {
                if (channel != null)
                {
                    TerminalUpdate(this, channel.Guild.Name + "/#" + channel.Name + ".ID:" + channel.Id);
                    Clipboard.SetText(channel.Id.ToString());
                }
                else
                {
                    TerminalUpdate(this, "Error: No recent channel.");
                }
            }
            else if (cmd.StartsWith("!getguildid"))
            {
                if (channel != null)
                {
                    TerminalUpdate(this, channel.Guild.Name + ".ID:" + channel.Guild.Id);
                    Clipboard.SetText(channel.Guild.Id.ToString());
                }
                else
                {
                    TerminalUpdate(this, "Error: No recent guild.");
                }
            }
            else if (cmd.StartsWith("!game"))
            {
                await client.SetGameAsync(parameters);
            }
            else
            {
                TerminalUpdate(this, "Not a command!");
            }
        }

        private void AddGuildCommand(string parameters)
        {
            var guildID = Convert.ToUInt64(parameters);
            var guildInfo = client.GetGuild(guildID);
            TerminalUpdate(this, "Added all text channels from guild " + "\"" + guildInfo.Name + "\".");
            AddGuildRequested(this, guildID);
            
        }

        #endregion
    }
}
