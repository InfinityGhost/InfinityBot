﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Threading;
using System.Reflection;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Clipboard = System.Windows.Clipboard;
using TextBox = System.Windows.Controls.TextBox;
using Discord.WebSocket;
using System.Collections.ObjectModel;

namespace InfinityBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Initialization & Variables

        public MainWindow()
        {
            InitializeComponent();
        }
        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ClearChannels();
            try
            {
                LoadDefault();
                LoadChannels();
                TerminalUpdate("Ready to start bot. Default settings loaded.");
            }
            catch
            {
                TerminalUpdate("Ready to start bot.");
            }
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Title += " - Debugging";
            }
        }

        private Bot bot;

        string SettingsVersion = "0.1.1";
        string TimePrefix = DateTime.Now + ": ";
        string logfile = Directory.GetCurrentDirectory() + @"\" + "log.log";

        #endregion

        #region Main Bot Controls

        void StartBot(object sender, RoutedEventArgs e)
        {
            if (StartButton.Content.ToString() == "Start Bot")
            {
                StartButton.Content = "Stop Bot";
                bot = new Bot(APIToken.Text);
                
                // Event handlers
                {
                    bot.TerminalUpdate += TerminalUpdate;
                    bot.AddGuildRequested += GetChannels;
                    bot.AddChannelRequested += AddChannelRequested;
                }

                if (bot.Start() == Task.CompletedTask)
                {
                    StartButton.Content = "Start Bot";
                }
            }
            else
            {
                KillBot();
                StartButton.Content = "Start Bot";
            }
        }
        private async void KillBot() => await bot.Stop();

        #endregion

        #region Terminal & StatusBar

        // Terminal
        async void TerminalCommand(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if ((sender as TextBox).Text != string.Empty)
                {
                    try
                    {
                        var selectedItem = Channels.SelectedItem as ComboBoxItem;
                        if (selectedItem.Tag.ToString() == "-1")
                        {
                            try
                            {
                                await bot.ReplyToMessage((sender as TextBox).Text);
                            }
                            catch (Exception ex)
                            {
                                TerminalUpdate(ex.ToString());
                            }
                        }
                        else
                        {
                            try
                            {
                                await bot.MessageDirect((sender as TextBox).Text, Convert.ToUInt64(selectedItem.Tag));
                            }
                            catch (Discord.Net.HttpException)
                            {
                                TerminalUpdate("Error: Unable to send message due to lack of permissions.");
                            }
                            catch (NullReferenceException)
                            {
                                TerminalUpdate("Error: Bot must be running in order to send messages or commands.");
                            }
                            catch (Exception ex)
                            {
                                TerminalUpdate(ex.ToString());
                            }
                        }
                        (sender as TextBox).Text = string.Empty;
                    }
                    catch (Discord.Net.HttpException)
                    {
                        TerminalUpdate("Error: Failed to send due to lack of permissions.");
                    }
                }
            }
        }

        async void TerminalUpdate(string text)
        {
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                string x = Terminal.Text;
                if (x == string.Empty)
                {
                    Terminal.Text += TimePrefix + text;
                }
                else
                {
                    Terminal.Text += Environment.NewLine + TimePrefix + text;
                }
                StatusUpdate(text);

                if (LogFile.IsChecked == true)
                {
                    try
                    {
                        string log = File.ReadAllText(logfile);
                        log += Environment.NewLine + TimePrefix + text;
                        File.WriteAllText(logfile, log);
                    }
                    catch (Exception ex)
                    {
                        StatusUpdate("Error: Failed to update log! " + ex.ToString());
                    }
                }
            }));
        }

        void TerminalUpdate(string[] text) => Array.ForEach(text, line => TerminalUpdate(line));
        void TerminalUpdate(object sender, string e) => TerminalUpdate(e);
        void TerminalClear() => Terminal.Text = string.Empty;

        // Context Menu
        void TerminalClear(object sender, RoutedEventArgs e) => TerminalClear();
        void TerminalCopy(object sender, RoutedEventArgs e) => Clipboard.SetText(Terminal.Text);

        // Status
        async void StatusUpdate(string text)
        {
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                Status.Text = text.Replace(TimePrefix, string.Empty);
            }));
        }
        void StatusClear() => Status.Text = string.Empty;

        #endregion

        #region Settings Management

        void Save(string path)
        {
            File.WriteAllLines(path, new string[]
            {
                "ver:" + SettingsVersion,
                "apiToken:" + APIToken.Text,
                "clientID:" + ClientID.Text,
                "logToFile:" + LogFile.IsChecked,
            });
        }
        void Load(string path)
        {
            var x = File.ReadAllLines(path);
            if (x[0] == "ver:" + SettingsVersion)
            {
                APIToken.Text = x[1].Replace("apiToken:", string.Empty);
                ClientID.Text = x[2].Replace("clientID:", string.Empty);
                LogFile.IsChecked = Convert.ToBoolean(x[3].Replace("logToFile:", string.Empty));
            }
            else
            {
                switch (x[0])
                {
                    case "ver:0.1":
                        {
                            APIToken.Text = x[1].Replace("apiToken:", string.Empty);
                            ClientID.Text = x[2].Replace("clientID:", string.Empty);
                            return;
                        }
                    case "ver:0.1.1":
                        {
                            APIToken.Text = x[1].Replace("apiToken:", string.Empty);
                            ClientID.Text = x[2].Replace("clientID:", string.Empty);
                            LogFile.IsChecked = Convert.ToBoolean(x[3].Replace("logToFile:", string.Empty));
                            return;
                        }
                }
            }
        }

        void SaveFile()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Bot configuration files (*.cfg)|*.cfg|All files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                RestoreDirectory = true,
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    Save(dialog.FileName);
                }
                catch
                {
                    TerminalUpdate("Error: An error has occured while saving a setup file.");
                }
            }
        }
        void LoadFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Bot configuration files (*.cfg)|*.cfg|All files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                RestoreDirectory = true,
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    Load(dialog.FileName);
                }
                catch
                {
                    TerminalUpdate("Error: An error has occured while loading a setup file.");
                }
            }
        }

        void SaveDefault() => Save(Directory.GetCurrentDirectory() + @"\" + "defaults.cfg");
        void LoadDefault() => Load(Directory.GetCurrentDirectory() + @"\" + "defaults.cfg");

        // Channels

        void SaveChannels(object sender, RoutedEventArgs e) => SaveChannels();
        void SaveChannels()
        {
            string[] fileContents = { };
            try
            {
                for (int i = 0; i < channelItems.Length - 1; i++)
                {
                    Array.Resize(ref fileContents, fileContents.Length + 1);
                    fileContents[fileContents.Length - 1] = channelItems[i + 1].Content.ToString() + ',' + channelItems[i + 1].Tag.ToString();
                }
                File.WriteAllLines(Directory.GetCurrentDirectory() + @"\" + "channels.txt", fileContents);
                TerminalUpdate("Saved all channels.");
            }
            catch(Exception ex)
            {
                TerminalUpdate("Failed to save channels to file." + Environment.NewLine + ex.ToString());
            }
        }
        
        void LoadChannels(object sender, RoutedEventArgs e) => LoadChannels();
        void LoadChannels()
        {
            ClearChannels();
            string[] fileContents = File.ReadAllLines(Directory.GetCurrentDirectory() + @"\" + "channels.txt");
            try
            {
                Array.ForEach(fileContents, item =>
                {
                    if (item != string.Empty)
                    {
                        var x = item.Split(',');
                        AddChannel(x[0], x[1]);
                    }
                });
            }
            catch { }
        }

        #endregion

        #region Channel Management

        ComboBoxItem[] channelItems = { };

        void UpdateChannelItems()
        {
            Channels.Items.Clear();
            Array.ForEach(channelItems, item => Channels.Items.Add(item)); 
            Channels.SelectedIndex = 0;

            // ChannelDataGrid
            var channelsCollection = new ObservableCollection<ComboBoxItem>();
            for(int i = 1; i < channelItems.Length; i++)
            {
                channelsCollection.Add(channelItems[i]);
            }

            ChannelDataGrid.ItemsSource = channelsCollection;
        }


        void AddChannel(string name, string id)
        {
            Array.Resize(ref channelItems, channelItems.Length + 1);
            channelItems[channelItems.Length - 1] = new ComboBoxItem
            {
                Content = name,
                Tag = id,
            };
            UpdateChannelItems();
        }
        void AddChannel(string name, ulong id) => AddChannel(name, id.ToString());

        void ClearChannels(object sender, RoutedEventArgs e) => ClearChannels();
        void ClearChannels()
        {
            ComboBoxItem[] x =
            {
                new ComboBoxItem
                {
                    Content = "- Reply in recent msg channel -",
                    Tag = "-1",
                },
            };
            channelItems = x;
            UpdateChannelItems();
        }

        private void GetChannels(object sender, ulong e)
        {
            var x = bot.GetChannels(e);
            var guild = x[0].Guild;
            Array.ForEach(x, item =>
            {
                if (item != null)
                {
                    AddChannel(item.Guild.Name + "/#" + item.Name, item.Id);
                }
            });
        }
        private void AddChannelRequested(object sender, ulong e)
        {
            var channel = bot.GetChannel(e);
            var guild = channel.Guild;
            AddChannel(guild.Name + "/#" + channel.Name, channel.Id);
            TerminalUpdate($"Text channel {guild.Name}/#{channel.Name} added.");
            Channels.SelectedIndex = channelItems.Length - 1;
        }

        #endregion

        #region Menu Buttons

        private void AboutShow(object sender, RoutedEventArgs e)
        {
            var x = new AboutBox();
            x.ShowDialog();
        }
        void SaveButton(object sender, RoutedEventArgs e) => SaveFile();
        void LoadButton(object sender, RoutedEventArgs e) => LoadFile();
        void SaveDefaultsButton(object sender, RoutedEventArgs e)
        {
            TerminalUpdate("Saved defaults.");
            SaveDefault();
        }
        void ExitButton(object sender, RoutedEventArgs e) => Close();

        #endregion

        #region Misc.

        private void GetInvite(object sender, RoutedEventArgs e)
        {
            string x = "https://discordapp.com/oauth2/authorize?client_id=" + ClientID.Text + @"&scope=bot";
            Clipboard.SetText(x);
            TerminalUpdate("Copied link to clipboard: " + x);
        }

        #endregion
    }
}
