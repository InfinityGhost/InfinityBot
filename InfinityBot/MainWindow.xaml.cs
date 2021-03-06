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
using Clipboard = System.Windows.Clipboard; 
    
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
            // Data Bindings
            {
                ChannelsBox.ItemsSource = ChannelsList;
                ChannelDataGrid.ItemsSource = ChannelsList;
                TabCtrl.DataContext = Settings;
                EnableLoggingItem.DataContext = Settings;
            }

            try
            {
                LoadDefault();
                LoadChannels();
                await Console.Log("Ready to start Bot. Default settings loaded.");
            }
            catch
            {
                await Console.Log("Ready to start Bot.");
            }
            if (System.Diagnostics.Debugger.IsAttached)
                Title += " - Debugging";

            await TrayIcon.Initialize();
            TrayIcon.ShowWindow += TrayIcon_ShowWindow;
        }

        public Bot Bot { private set; get; }
        public bool BotRunning { private set; get; }

        private TrayIcon TrayIcon = new TrayIcon();
        private Settings Settings = new Settings();

        readonly string LogPath = Directory.GetCurrentDirectory() + @"\" + "log.log";
        readonly string ChannelsPath = Directory.GetCurrentDirectory() + @"\" + "channels.txt";
        readonly string DefaultsPath = Directory.GetCurrentDirectory() + @"\" + "defaults.cfg";

        #endregion

        #region Main Bot Controls

        async void StartBot(object sender = null, EventArgs e = null)
        {
            if (!BotRunning)
            {
                BotRunning = !BotRunning;
                Bot = new Bot(Settings.APIToken);
                Bot.Output += Console.Log;
                try
                {
                    await Bot.Start();
                }
                catch(Exception ex)
                {
                    await Console.Log(ex.ToString());
                }
            }
            else
            {
                await Bot.Stop();
                Bot.Output -= Console.Log;
                Bot = null;
                BotRunning = !BotRunning;
            }
        }

        #endregion

        #region Console and Status

        private async void Message(object sender, string message = null)
        {
            try
            {
                if (Bot != null)
                {
                    if (message != null)
                    {
                        string id = (string)(ChannelsBox.SelectedItem as ComboBoxItem).Tag;
                        if (id == "-1")
                            await Bot.ReplyToMessage(message);
                        else
                            await Bot.ServerMessage(message, Bot.GetChannel(Convert.ToUInt64(id)));
                    }
                }
                else
                    await Console.Log("Error: Bot is not started.");
            }
            catch(Discord.Net.HttpException)
            {
                await Console.Log("Error: The bot lacks permission to send messages to this channel, or the channel does not exist.");
            }
        }

        private async void MessageBoxKey(object sender, Key key)
        {
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                var index = ChannelsBox.SelectedIndex;
                switch (key)
                {
                    case Key.Down:
                        if (index++ != ChannelsList.Count - 1)
                            ChannelsBox.SelectedIndex++;
                        break;
                    case Key.Up:
                        if (index-- != 0)
                            ChannelsBox.SelectedIndex--;
                        break;
                }
            }));
        }

        private async void StatusUpdate(object sender, string text)
        {
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                Status.Text = text;
                if (text.Contains("Error") || text.Contains("Exception"))
                {
                    if (StatusBar.Background == DefaultBG)
                    {
                        StatusBar.Background = ErrorBG;
                        Status.Foreground = ErrorText;
                    }
                }
                else
                {
                    if (StatusBar.Background != DefaultBG)
                    {
                        StatusBar.Background = DefaultBG;
                        Status.Foreground = DefaultText;
                    }
                }
            }));
            if (Settings.LoggingEnabled)
                await WriteLog(text);
        }



        public Brush ErrorText = new BrushConverter().ConvertFrom("#FFFFFF") as Brush;
        public Brush ErrorBG = new BrushConverter().ConvertFrom("#E81123") as Brush;

        public Brush DefaultText = new BrushConverter().ConvertFrom("#FFFFFF") as Brush;
        public Brush DefaultBG => SystemParameters.WindowGlassBrush;
        
        #endregion

        #region Settings Management

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
                    await Settings.Save(dialog.FileName);
                }
                catch
                {
                    await Console.Log("Error: An error has occured while saving a setup file.");
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
                    await Settings.Load(dialog.FileName);
                }
                catch
                {
                    await Console.Log("Error: An error has occured while loading a setup file.");
                }
            }
        }

        private async void SaveDefaults() => await Settings.Save(DefaultsPath);
        private async void LoadDefault() => await Settings.Load(DefaultsPath);

        // Channels

        async void SaveChannels(object sender = null, EventArgs e = null)
        {
            try
            {
                List<ComboBoxItem> items = ChannelsList.Skip(1).SkipWhile(item => item == CollectionView.NewItemPlaceholder).ToList();
                List<string> fileContents = items.ConvertAll(item => $"{item.Content},{item.Tag}");
                File.WriteAllLines(Directory.GetCurrentDirectory() + @"\" + "channels.txt", fileContents);
                await Console.Log("Saved all channels.");
            }
            catch (Exception ex)
            {
                await Console.Log("Failed to save channels to file." + Environment.NewLine + ex.ToString());
            }
        }

        private void LoadChannels(object sender = null, EventArgs e = null)
        {
            ChannelsList.Clear();
            ChannelsList.AddFile(ChannelsPath);
            ChannelsBox.SelectedIndex = 0;
        }

        #endregion

        #region Channel Management

        Channels ChannelsList = new Channels();

        private void GetChannels(object sender, ulong e)
        {
            var channels = Bot.GetChannels(e).ToList();
            channels.ForEach(item =>
            {
                if (item != null)
                    ChannelsList.Add(item.Guild.Name + "/#" + item.Name, item.Id);
            });
        }

        async void AddChannelUID(object sender, ulong e)
        {
            var channel = Bot.GetChannel(e);
            ChannelsList.Add(channel);
            ChannelsBox.SelectedIndex = ChannelsList.Count - 1;

            await Console.Log($"Text channel {channel.Guild.Name}/#{channel.Name} added.");
        }

        private void ClearChannels(object sender = null, EventArgs e = null) => ChannelsList.Clear();

        private async void ChannelControlBox_Command(object sender, string e)
        {
            if (Bot != null)
            {
                var vs = e.Split(',');
                switch (vs.First())
                {
                    case "AddChannel":
                        ChannelsList.Add(Bot.GetChannel(Convert.ToUInt64(vs[1])));
                        await Console.Log("Added channel.");
                        break;
                    case "AddGuild":
                        try
                        {
                            ChannelsList.AddRange(Bot.GetChannels(Convert.ToUInt64(vs[1])));
                        }
                        catch
                        {
                            await Console.Log("Error: Invalid ID.");
                        }
                        await Console.Log("Added channels.");
                        break;
                }
            }
            else
                await Console.Log("Error: Bot is not started.");
        }

        #endregion

        #region Menu Buttons

        private void AboutShow(object sender = null, EventArgs e = null) => new AboutBox().ShowDialog();
        async void SaveDefaultsButton(object sender = null, EventArgs e = null)
        {
            SaveDefaults();
            await Console.Log("Saved defaults.");
        }
        void ExitButton(object sender = null, EventArgs e = null) => Close();

        #endregion

        #region Misc.

        private async void GetInvite(object sender = null, EventArgs e = null)
        {
            string link = "https://discordapp.com/oauth2/authorize?client_id=" + Settings.ClientID + @"&scope=bot";
            Clipboard.SetText(link);
            await Console.Log("Copied link to clipboard: " + link);
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
            WindowState = WindowState.Normal;
        }

        #endregion

        #region Logging

        /// <summary>
        /// Writes text to the console's log file.
        /// </summary>
        /// <param name="text">The text to log.</param>
        private Task WriteLog(string text)
        {
            try
            {
                File.AppendAllText(LogPath, Controls.Console.Prefix + text + Environment.NewLine);
            }
            catch
            {
                StatusUpdate(this, "Error: Unable to write to log.");
            }
            return Task.CompletedTask;
        }

        private void OpenLogFile(object sender = null, EventArgs e = null) => System.Diagnostics.Process.Start(LogPath);
        private async void CopyConsole(object sender = null, EventArgs e = null) => await Console.Copy();


        #endregion

    }
}
