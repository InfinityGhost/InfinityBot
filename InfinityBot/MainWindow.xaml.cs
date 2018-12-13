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
using System.Threading;
using System.Reflection;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Clipboard = System.Windows.Clipboard;
using TextBox = System.Windows.Controls.TextBox;
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

        async void Window_Loaded(object sender = null, EventArgs e = null)
        {
            try
            {
                LoadDefault();
                LoadChannels();
                await TerminalUpdate("Ready to start bot. Default settings loaded.");
            }
            catch
            {
                await TerminalUpdate("Ready to start bot.");
            }
            if (System.Diagnostics.Debugger.IsAttached)
                Title += " - Debugging";

            ChannelsBox.ItemsSource = ChannelsList;
            ChannelDataGrid.ItemsSource = ChannelsList;
            await TrayIcon.Initialize();
            TrayIcon.ShowWindow += TrayIcon_ShowWindow;

        }

        private Bot bot;
        private TrayIcon TrayIcon = new TrayIcon();

        readonly string SettingsVersion = "0.1.1";
        string TimePrefix => DateTime.Now.ToLocalTime() + ": ";

        readonly string logFile = Directory.GetCurrentDirectory() + @"\" + "log.log";
        readonly string channelsFile = Directory.GetCurrentDirectory() + @"\" + "channels.txt";
        readonly string defaultsFile = Directory.GetCurrentDirectory() + @"\" + "defaults.cfg";

        #endregion

        #region Main Bot Controls

        async void StartBot(object sender = null, EventArgs e = null)
        {
            if ((string)StartButton.Content == "Start Bot")
            {
                StartButton.Content = "Stop Bot";
                bot = new Bot(APIToken.Password);

                // Event handlers
                {
                    bot.Output += TerminalUpdate;
                }

                try
                {
                    await bot.Start();
                }
                catch(Exception ex)
                {
                    await TerminalUpdate(ex.ToString());
                }
            }
            else
            {
                await bot.Stop();
                bot = null;
                StartButton.Content = "Start Bot";
            }
        }

        #endregion

        #region Terminal & StatusBar

        // Terminal
        async void TerminalCommand(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (bot == null)
                {
                    await TerminalUpdate("Error: Bot must be running in order to send messages or commands.");
                    return;
                }
                if ((sender as TextBox).Text != string.Empty)
                {
                    try
                    {
                        var selectedItem = ChannelsBox.SelectedItem as ComboBoxItem;
                        if ((string)selectedItem.Tag == "-1")
                        {
                            try
                            {
                                await bot.ReplyToMessage((sender as TextBox).Text);
                            }
                            catch (Exception ex)
                            {
                                await TerminalUpdate(ex.ToString());
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
                                await TerminalUpdate("Error: Unable to send message due to lack of permissions.");
                            }
                            catch (Exception ex)
                            {
                                await TerminalUpdate(ex.ToString());
                            }
                        }
                        (sender as TextBox).Text = string.Empty;
                    }
                    catch (Discord.Net.HttpException)
                    {
                        await TerminalUpdate("Error: Failed to send due to lack of permissions.");
                    }
                }
            }
        }

        async Task TerminalUpdate(string text)
        {
            try
            {
                await Dispatcher.BeginInvoke(new Action(async () =>
                {
                    if (Terminal.Text == string.Empty)
                    {
                        Terminal.Text += TimePrefix + text;
                    }
                    else
                    {
                        Terminal.Text += Environment.NewLine + TimePrefix + text;
                    }
                    await StatusUpdate(text);

                    if (LogFile.IsChecked == true)
                    {
                        await Log(text);
                    }
                }));
            }
            catch (Exception ex)
            {
                await Log(ex.ToString());
            }
        }
        void TerminalUpdate(string[] text) => Array.ForEach(text, async line => await TerminalUpdate(line));
        async void TerminalUpdate(object sender, string e) => await TerminalUpdate(e);

        void TerminalClear() => Terminal.Text = string.Empty;

        // Context Menu
        void TerminalClear(object sender = null, EventArgs e = null) => TerminalClear();
        void TerminalCopy(object sender = null, EventArgs e = null) => Clipboard.SetText(Terminal.Text);
        async void OpenLogButton(object sender = null, EventArgs e = null)
        {
            try
            {
                System.Diagnostics.Process.Start(logFile);
            }
            catch
            {
                await TerminalUpdate("Error: No log file to open.");
            }
        }

        // Logging
        async Task Log(string text)
        {
            try
            {
                File.AppendAllText(logFile, Environment.NewLine + TimePrefix + text);
            }
            catch (Exception ex)
            {
                await StatusUpdate("Error: Failed to update log! " + ex.ToString());
            }
        }
        void Log(string[] text) => Array.ForEach(text, async line => await Log(line));

        // Status
        async Task StatusUpdate(string text)
        {
            if (text != null)
            {
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    Status.Text = text.Replace(TimePrefix, string.Empty);
                }));
            }
            if (text.Contains("Error") || text.Contains("Exception"))
            {
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (StatusBar.Background == DefaultBG)
                    {
                        StatusBar.Background = ErrorBG;
                        Status.Foreground = ErrorText;
                    }
                }));
            }
            else
            {
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (StatusBar.Background != DefaultBG)
                    {
                        StatusBar.Background = DefaultBG;
                        Status.Foreground = DefaultText;
                    }
                }));
            }
        }
        void StatusClear() => Status.Text = string.Empty;

        public Brush ErrorText = new BrushConverter().ConvertFrom("#FFFFFF") as Brush;
        public Brush ErrorBG = new BrushConverter().ConvertFrom("#E81123") as Brush;

        public Brush DefaultText = new BrushConverter().ConvertFrom("#FFFFFF") as Brush;
        public Brush DefaultBG => SystemParameters.WindowGlassBrush;
        
        #endregion

        #region Settings Management

        private Task Save(string path)
        {
            File.WriteAllLines(path, new string[]
            {
                "ver:" + SettingsVersion,
                "apiToken:" + APIToken.Password,
                "clientID:" + ClientID.Password,
                "logToFile:" + LogFile.IsChecked,
            });
            return Task.CompletedTask;
        }
        private Task Load(string path)
        {
            var x = File.ReadAllLines(path);
            if (x[0] == "ver:" + SettingsVersion)
            {
                APIToken.Password = x[1].Replace("apiToken:", string.Empty);
                ClientID.Password = x[2].Replace("clientID:", string.Empty);
                LogFile.IsChecked = Convert.ToBoolean(x[3].Replace("logToFile:", string.Empty));
            }
            else
            {
                switch (x[0])
                {
                    case "ver:0.1":
                        {
                            APIToken.Password = x[1].Replace("apiToken:", string.Empty);
                            ClientID.Password = x[2].Replace("clientID:", string.Empty);
                            break;
                        }
                    case "ver:0.1.1":
                        {
                            APIToken.Password = x[1].Replace("apiToken:", string.Empty);
                            ClientID.Password = x[2].Replace("clientID:", string.Empty);
                            LogFile.IsChecked = Convert.ToBoolean(x[3].Replace("logToFile:", string.Empty));
                            break;
                        }
                }
            }
            return Task.CompletedTask;
        }

        private async void SaveDialog(object sender = null, EventArgs e = null)
        {
            var dialog = new System.Windows.Forms.SaveFileDialog
            {
                Filter = "Bot configuration files (*.cfg)|*.cfg|All files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                RestoreDirectory = true,
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    await Save(dialog.FileName);
                }
                catch
                {
                    await TerminalUpdate("Error: An error has occured while saving a setup file.");
                }
            }
        }
        private async void LoadDialog(object sender = null, EventArgs e = null)
        {
            var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "Bot configuration files (*.cfg)|*.cfg|All files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                RestoreDirectory = true,
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    await Load(dialog.FileName);
                }
                catch
                {
                    await TerminalUpdate("Error: An error has occured while loading a setup file.");
                }
            }
        }

        private async void SaveDefaults() => await Save(defaultsFile);
        private async void LoadDefault() => await Load(defaultsFile);

        // Channels

        async void SaveChannels(object sender = null, EventArgs e = null)
        {
            try
            {
                List<string> fileContents = ChannelsList.Skip(1).ToList().ConvertAll(item => $"{item.Content},{item.Tag}");
                File.WriteAllLines(Directory.GetCurrentDirectory() + @"\" + "channels.txt", fileContents);
                await TerminalUpdate("Saved all channels.");
            }
            catch (Exception ex)
            {
                await TerminalUpdate("Failed to save channels to file." + Environment.NewLine + ex.ToString());
            }
        }

        private void LoadChannels(object sender = null, EventArgs e = null)
        {
            ChannelsList.Clear();
            ChannelsList.AddFile(channelsFile);
            ChannelsBox.SelectedIndex = 0;
        }

        #endregion

        #region Channel Management

        Channels ChannelsList = new Channels();

        private void GetChannels(object sender, ulong e)
        {
            var channels = bot.GetChannels(e).ToList();
            channels.ForEach(item =>
            {
                if (item != null)
                    ChannelsList.Add(item.Guild.Name + "/#" + item.Name, item.Id);
            });
        }

        async void AddChannelUID(object sender, ulong e)
        {
            var channel = bot.GetChannel(e);
            ChannelsList.Add(channel);
            ChannelsBox.SelectedIndex = ChannelsList.Count - 1;

            await TerminalUpdate($"Text channel {channel.Guild.Name}/#{channel.Name} added.");
        }

        private void ClearChannels(object sender = null, EventArgs e = null) => ChannelsList.Clear();

        #endregion

        #region Menu Buttons

        private void AboutShow(object sender = null, EventArgs e = null) => new AboutBox().ShowDialog();
        async void SaveDefaultsButton(object sender = null, EventArgs e = null)
        {
            SaveDefaults();
            await TerminalUpdate("Saved defaults.");
        }
        void ExitButton(object sender = null, EventArgs e = null) => Close();

        #endregion

        #region Misc.

        private async void GetInvite(object sender = null, EventArgs e = null)
        {
            string link = "https://discordapp.com/oauth2/authorize?client_id=" + ClientID.Password + @"&scope=bot";
            Clipboard.SetText(link);
            await TerminalUpdate("Copied link to clipboard: " + link);
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            switch (WindowState)
            {
                case WindowState.Minimized:
                    {
                        ShowInTaskbar = false;
                        TrayIcon.Visible = true;
                        Hide();
                        break;
                    }
                case WindowState.Normal:
                    {
                        ShowInTaskbar = true;
                        TrayIcon.Visible = false;
                        break;
                    }
            }
        }

        private void TrayIcon_ShowWindow(object sender, EventArgs e)
        {
            Show();
        }

        #endregion
    }
}
