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
using System.Windows.Forms;
using System.Threading;

namespace InfinityBot
{
    class Bot
    {
        public Bot(string token)
        {
            Token = token;
        }
        private readonly string Token;

        public event EventHandler<string> TerminalUpdate;

        DiscordSocketClient client = new DiscordSocketClient();
        CommandService userCommands = new CommandService();
        CommandService adminCommands = new CommandService();
        readonly IServiceProvider services = new ServiceCollection().BuildServiceProvider();
        
        #region Main

        public async Task Start()
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
            TerminalUpdate(this, "Username: " + client.CurrentUser.Username);
        }

        public async Task Stop()
        {
            await client.StopAsync();
            client = null;
            TerminalUpdate(this, "Bot has stopped.");
        }

        #endregion

        #region Console Outputs

        SocketMessage lastMessage;

        private Task Log(LogMessage msg)
        {
            if (msg.Message != null)
            {
                TerminalUpdate(this, msg.Message);
            }
            return Task.CompletedTask;
        }

        private Task Log(SocketMessage msg)
        {
            lastMessage = msg;
            string message = string.Empty;
            try
            {
                if (msg.Channel is SocketGuildChannel channel)
                {
                    message = channel.Guild.Name + "/#" + channel.Name + "/" + msg.Author + ": ";
                }
                else if (msg.Channel is SocketDMChannel dm)
                {
                    var users = dm.Users.ToArray();
                    string userstring = "DM " + "{";
                    for (int i = 0; i < users.Length; i++)
                    {
                        if (i < users.Length - 1)
                        {
                            userstring += users[i].Username + ", ";
                        }
                        else
                        {
                            userstring += users[i].Username;
                        }
                    }
                    message = userstring + "}" + ": ";
                }

                if (msg.Content.Contains("\n"))
                {
                    message += Environment.NewLine + msg.Content;
                }
                else
                {
                    message += msg.Content;
                }

                if (msg.Attachments.ToArray() is Attachment[] attachments)
                {
                    for (int i = 0; i < attachments.Length; i++)
                    {
                        message += "{" + attachments[i].Filename + "}";

                        if (i < attachments.Length - 1)
                        {
                            message += ", ";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                message += ex.ToString();
            }
            
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
            if (message.Content != null)
            {
                await Log(message);
            }
        }

        private Task MessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage msg, ISocketMessageChannel arg3)
        {
            if (msg != null)
            {
                var channel = msg.Channel as SocketGuildChannel;
                string message = "Message Updated | " + channel.Guild.Name + "/#" + channel.Name + "/" + msg.Author + ": " + msg.Content;
                TerminalUpdate(this, message);
            }
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
            // User commands
            client.MessageReceived += HandleUserCommand;
            await userCommands.AddModuleAsync(typeof(Commands.UserCommands), services);

            // Admin Commands
            client.MessageReceived += HandleAdminCommand;
            await adminCommands.AddModuleAsync(typeof(Commands.AdminCommands), services);
        }

        private async Task HandleAdminCommand(SocketMessage messageParam)
        {
            if (!(messageParam is SocketUserMessage message))
                return;
            int argPos = 0;
            if (!message.HasCharPrefix('&', ref argPos))
                return;

            var context = new CommandContext(client, message);
            var result = await adminCommands.ExecuteAsync(context, argPos, services);
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }

        private async Task HandleUserCommand(SocketMessage messageParam)
        {
            if (!(messageParam is SocketUserMessage message))
            {
                return;
            }
            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message is a command, based on if it starts with '!' or a mention prefix
            if (!message.HasCharPrefix('$', ref argPos))
            {
                return;
            }
            // Create a Command Context
            var context = new CommandContext(client, message);
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = await userCommands.ExecuteAsync(context, argPos, services);
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }

        public async Task ServerCommand(string cmd, SocketTextChannel channel)
        {
            string[] command = cmd.Split(' ');
            string parameters = string.Empty;
            try
            {
                parameters = cmd.Substring(command[0].Length + 1);
            }
            catch { }

            switch (command[0])
            {
                case "!addguild":
                    {
                        AddGuildCommand(parameters);
                        break;
                    }
                case "!addchannel":
                    {
                        AddChannelRequested(this, Convert.ToUInt64(parameters));
                        break;
                    }
                case "!getchannelid":
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
                        break;
                    }
                case "!getguildid":
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
                        break;
                    }
                case "!game":
                    {
                        await client.SetGameAsync(parameters);
                        break;
                    }
                case "!del":
                    {
                        if (channel == null)
                        {
                            TerminalUpdate(this, "No recent channel.");
                            return;
                        }
                        if (parameters == "!del")
                        {
                            parameters = string.Empty;
                        }
                        if (parameters == null || parameters == string.Empty)
                        {
                            var msgCollection = await (channel as ISocketMessageChannel).GetMessagesAsync(1).FlattenAsync();
                            var msgArray = msgCollection.ToArray();

                            try
                            {
                                await msgArray[0].DeleteAsync();
                                TerminalUpdate(this, $"Deleted last message from {channel.Guild.Name}/#{channel.Name}.");
                            }
                            catch (IndexOutOfRangeException)
                            {
                                TerminalUpdate(this, "No messages to delete.");
                            }
                        }
                        else
                        {
                            int messageCount = Convert.ToInt32(parameters.Split(' ')[0]);


                            if (messageCount > 100)
                            {
                                messageCount = 100;
                                // TODO: make message count allowed over 100
                            }

                            var msgCollection = await (channel as ISocketMessageChannel).GetMessagesAsync(messageCount).FlattenAsync();

                            try
                            {
                                string update = string.Empty;
                                string text = string.Empty;
                                try
                                {
                                    text = parameters.Substring(messageCount.ToString().Length + 1);
                                }
                                catch { }
                                if (text != string.Empty)
                                {
                                    msgCollection = msgCollection.Where(msg => msg.Content.Contains(text));
                                    await channel.DeleteMessagesAsync(msgCollection);
                                    update = $"Deleted {msgCollection.ToArray().Length} messages containing \"{text}\" from {channel.Guild.Name}/#{channel.Name}.";
                                }
                                else
                                {
                                    await channel.DeleteMessagesAsync(msgCollection);
                                    update = $"Deleted {msgCollection.ToArray().Length} messages from {channel.Guild.Name}/#{channel.Name}.";
                                }
                                TerminalUpdate(this, update);
                            }
                            catch (IndexOutOfRangeException)
                            {
                                TerminalUpdate(this, "No messages to delete.");
                            }
                        }
                        break;
                    }
                case "!edit":
                    {
                        // TODO: add edit command
                        throw new NotImplementedException();
                    }
                default:
                    {
                        TerminalUpdate(this, "Not a command.");
                        break;
                    }
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
