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
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Clipboard = System.Windows.Clipboard;
using TextBox = System.Windows.Controls.TextBox;
using Discord.WebSocket;

namespace InfinityBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ClearChannels();
            try
            {
                LoadDefault();
                LoadChannels();
                TerminalUpdate(TimePrefix + "Ready. Default settings loaded.");
            }
            catch
            {
                TerminalUpdate(TimePrefix + "Ready.");
            }
            
        }

        string Version = "0.1.1";
        string TimePrefix = DateTime.Now + ": ";

        private Bot bot;

        #region Main Bot Controls

        void StartBot(object sender, RoutedEventArgs e)
        {
            if (StartButton.Content.ToString() == "Start Bot")
            {
                StartButton.Content = "Stop Bot";
                bot = new Bot(APIToken.Text);
                bot.TerminalUpdate += TerminalUpdate;
                bot.AddGuildRequested += GetChannels;
                if (bot.MainAsync() == Task.CompletedTask)
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

        private async void KillBot()
        {
            await bot.Stop();
        }

        #endregion

        #region Terminal

        async void TerminalUpdate(string text)
        {
            string x = null;
            await Dispatcher.BeginInvoke(new Action(() => x = Terminal.Text));
            if (x == string.Empty)
            {
                await Dispatcher.BeginInvoke(new Action(() => Terminal.Text += text));
            }
            else
            {
                await Dispatcher.BeginInvoke(new Action(() => Terminal.Text += Environment.NewLine + text));
            }
            StatusUpdate(text);

            string LoggingText = null;
            await Dispatcher.BeginInvoke(new Action(() => LoggingText = Terminal.Text));

            bool log = false;
            await Dispatcher.BeginInvoke(new Action(() => log = LogFile.IsChecked));
            if (log == true)
            {
                try
                {
                    File.WriteAllText(Directory.GetCurrentDirectory() + @"\" + "log.log", LoggingText);
                }
                catch (Exception ex)
                {
                    StatusUpdate(TimePrefix + "Failed to update log! | " + ex.ToString());
                }
            }
        }
        void TerminalUpdate(string[] text) => Array.ForEach(text, line => TerminalUpdate(line));
        async void TerminalUpdate(object sender, string e)
        {
            await BotUpdate(e);
        }

        public async Task DispatcherInvoke(object uiObject, object newValue)
        {
            await Dispatcher.BeginInvoke(new Action(() => uiObject = newValue));
        }

        Task BotUpdate(string text)
        {
            TerminalUpdate(text);
            return Task.CompletedTask;
        }

        void TerminalClear() => Terminal.Text = string.Empty;

        async void TerminalCommand(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
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
                        catch(Exception ex)
                        {
                            TerminalUpdate(TimePrefix + ex.ToString());
                        }
                    }
                    else
                    {
                        try
                        {
                            await bot.MessageDirect((sender as TextBox).Text, Convert.ToUInt64(selectedItem.Tag));
                        }
                        catch(Exception ex)
                        {
                            TerminalUpdate(TimePrefix + ex.ToString());
                        }
                    }
                    (sender as TextBox).Text = string.Empty;
                }
                catch(Discord.Net.HttpException)
                {
                    TerminalUpdate(TimePrefix + "Error: Failed to send due to lack of permissions.");
                }
            }
        }

        // Context Menu

        void TerminalClear(object sender, RoutedEventArgs e) => TerminalClear();
        void TerminalCopy(object sender, RoutedEventArgs e) => Clipboard.SetText(Terminal.Text);

        #endregion

        #region Status

        async void StatusUpdate(string text)
        {
            await Dispatcher.BeginInvoke(new Action(() => Status.Text = text));
        }

        void StatusClear() => Status.Text = string.Empty;

        #endregion

        #region Settings Management

        void Save(string path)
        {
            File.WriteAllLines(path, new string[]
            {
                "ver:" + Version,
                "apiToken:" + APIToken.Text,
                "clientID:" + ClientID.Text,
                "logToFile:" + LogFile.IsChecked,
            });
        }
        void Load(string path)
        {
            var x = File.ReadAllLines(path);
            if (x[0] == "ver:" + Version)
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
                    TerminalUpdate(TimePrefix + "An error has occured while saving a setup file.");
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
                    TerminalUpdate(TimePrefix + "An error has occured while loading a setup file.");
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
            }
            catch(Exception ex)
            {
                TerminalUpdate(TimePrefix + "Failed to save channels to file." + Environment.NewLine + ex.ToString());
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
                    var x = item.Split(',');
                    AddChannel(x[0], x[1]);
                });
            }
            catch { }
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
            TerminalUpdate(TimePrefix + "Saved defaults.");
            SaveDefault();
        }
        void ExitButton(object sender, RoutedEventArgs e) => Close();

        #endregion

        #region Settings Page Buttons

        private void GetInvite(object sender, RoutedEventArgs e)
        {
            string x = "https://discordapp.com/oauth2/authorize?client_id=" + ClientID.Text + @"&scope=bot";
            Clipboard.SetText(x);
            TerminalUpdate(TimePrefix + "Copied link to clipboard: " + x);
        }

        #endregion

        #region Channels Combobox

        ComboBoxItem[] channelItems = { };

        void UpdateChannelItems()
        {
            Channels.Items.Clear();
            Array.ForEach(channelItems, item => Channels.Items.Add(item));
            Channels.SelectedIndex = 0;
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

        void GetChannels(string guildID)
        {
            var x = bot.GetChannels(Convert.ToUInt64(guildID));
            Array.ForEach(x, item =>
            {
                AddChannel(item.Name, item.Id);
            });
        }

        private void GetChannels(object sender, string e) => GetChannels(e);

        #endregion
    }
}
