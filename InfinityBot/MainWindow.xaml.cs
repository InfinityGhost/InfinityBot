using System;
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
using DataGrid = System.Windows.Controls.DataGrid;
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

        async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await ClearChannels();
            try
            {
                LoadDefault();
                await LoadChannels();
                await TerminalUpdate("Ready to start bot. Default settings loaded.");
            }
            catch
            {
                await TerminalUpdate("Ready to start bot.");
            }
            if (System.Diagnostics.Debugger.IsAttached)
                Title += " - Debugging";

            await NotifyIconStartup();
        }

        private Bot bot;
        private Assembly assembly = Assembly.GetExecutingAssembly();

        readonly string SettingsVersion = "0.1.1";
        string TimePrefix
        {
            get
            {
                return DateTime.Now + ": ";
            }
        }
        readonly string logfile = Directory.GetCurrentDirectory() + @"\" + "log.log";

        #endregion

        #region Main Bot Controls

        async void StartBot(object sender, RoutedEventArgs e)
        {
            if (StartButton.Content.ToString() == "Start Bot")
            {
                StartButton.Content = "Stop Bot";
                bot = new Bot(APIToken.Text);

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
                    string[] output =
                    {
                        ex.Message,
                        ex.ToString(),
                        ex.InnerException.ToString(),
                        ex.Data.ToString(),
                    };
                    try
                    {
                        TerminalUpdate(output);
                    }
                    catch
                    {
                        Log(output);
                    }
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
                if (bot == null)
                {
                    await TerminalUpdate("Error: Bot must be running in order to send messages or commands.");
                    return;
                }
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
                    string x = Terminal.Text;
                    if (x == string.Empty)
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
        void TerminalClear(object sender, RoutedEventArgs e) => TerminalClear();
        void TerminalCopy(object sender, RoutedEventArgs e) => Clipboard.SetText(Terminal.Text);
        async void OpenLogButton(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(logfile);
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
                string log = File.ReadAllText(logfile);
                log += Environment.NewLine + TimePrefix + text;
                File.WriteAllText(logfile, log);
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
        public Brush DefaultBG
        {
            get
            {
                return SystemParameters.WindowGlassBrush;
            }
        }



        #endregion

        #region Settings Management

        Task Save(string path)
        {
            File.WriteAllLines(path, new string[]
            {
                "ver:" + SettingsVersion,
                "apiToken:" + APIToken.Text,
                "clientID:" + ClientID.Text,
                "logToFile:" + LogFile.IsChecked,
            });
            return Task.CompletedTask;
        }
        Task Load(string path)
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
                            break;
                        }
                    case "ver:0.1.1":
                        {
                            APIToken.Text = x[1].Replace("apiToken:", string.Empty);
                            ClientID.Text = x[2].Replace("clientID:", string.Empty);
                            LogFile.IsChecked = Convert.ToBoolean(x[3].Replace("logToFile:", string.Empty));
                            break;
                        }
                }
            }
            return Task.CompletedTask;
        }

        async void SaveFile()
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
                    await Save(dialog.FileName);
                }
                catch
                {
                    await TerminalUpdate("Error: An error has occured while saving a setup file.");
                }
            }
        }
        async void LoadFile()
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
                    await Load(dialog.FileName);
                }
                catch
                {
                    await TerminalUpdate("Error: An error has occured while loading a setup file.");
                }
            }
        }

        async void SaveDefault() => await Save(Directory.GetCurrentDirectory() + @"\" + "defaults.cfg");
        async void LoadDefault() => await Load(Directory.GetCurrentDirectory() + @"\" + "defaults.cfg");

        // Channels

        async void SaveChannels(object sender, RoutedEventArgs e) => await SaveChannels();
        async Task SaveChannels()
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
                await TerminalUpdate("Saved all channels.");
            }
            catch (Exception ex)
            {
                await TerminalUpdate("Failed to save channels to file." + Environment.NewLine + ex.ToString());
            }
        }

        async void LoadChannels(object sender, RoutedEventArgs e) => await LoadChannels();
        Task LoadChannels()
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
            return Task.CompletedTask;
        }

        #endregion

        #region Channel Management

        ComboBoxItem[] channelItems = { };

        ObservableCollection<ComboBoxItem> channelsCollection = new ObservableCollection<ComboBoxItem>();

        Task UpdateChannelItems()
        {
            Channels.Items.Clear();
            Array.ForEach(channelItems, item => Channels.Items.Add(item));
            Channels.SelectedIndex = 0;

            // ChannelDataGrid
            channelsCollection = new ObservableCollection<ComboBoxItem>();
            for (int i = 1; i < channelItems.Length; i++)
            {
                channelsCollection.Add(channelItems[i]);
            }

            ChannelDataGrid.ItemsSource = channelsCollection;
            return Task.CompletedTask;
        }

        async void AddChannel(string name, ulong id) => await AddChannel(name, id.ToString());
        Task AddChannel(string name, string id)
        {
            Array.Resize(ref channelItems, channelItems.Length + 1);
            channelItems[channelItems.Length - 1] = new ComboBoxItem
            {
                Content = name,
                Tag = id,
            };
            UpdateChannelItems();
            return Task.CompletedTask;
        }

        async void ClearChannels(object sender, RoutedEventArgs e) => await ClearChannels();
        Task ClearChannels()
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
            return Task.CompletedTask;
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

        async void AddChannelRequested(object sender, ulong e)
        {
            var channel = bot.GetChannel(e);
            var guild = channel.Guild;
            AddChannel(guild.Name + "/#" + channel.Name, channel.Id);
            await TerminalUpdate($"Text channel {guild.Name}/#{channel.Name} added.");
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
        async void SaveDefaultsButton(object sender, RoutedEventArgs e)
        {
            await TerminalUpdate("Saved defaults.");
            SaveDefault();
        }
        void ExitButton(object sender, RoutedEventArgs e) => Close();

        #endregion

        #region Tray / Notification icon

        NotifyIcon NotifyIcon = new NotifyIcon();

        private Task NotifyIconStartup()
        {
            string icon = @"InfinityBot.infinitybot.ico";
            NotifyIcon.Icon = new System.Drawing.Icon(assembly.GetManifestResourceStream(icon));
            NotifyIcon.MouseClick += NotifyIcon_Click;
            NotifyIcon.Text = "InfinityBot";

            return Task.CompletedTask;
        }

        private void NotifyIcon_Click(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            switch (WindowState)
            {
                case WindowState.Minimized:
                    {
                        ShowInTaskbar = false;
                        NotifyIcon.Visible = true;
                        Hide();
                        break;
                    }
                case WindowState.Normal:
                    {
                        ShowInTaskbar = true;
                        NotifyIcon.Visible = false;
                        break;
                    }
            }
        }

        #endregion

        #region Misc.

        private async void GetInvite(object sender, RoutedEventArgs e)
        {
            string x = "https://discordapp.com/oauth2/authorize?client_id=" + ClientID.Text + @"&scope=bot";
            Clipboard.SetText(x);
            await TerminalUpdate("Copied link to clipboard: " + x);
        }

        #endregion
    }
}
