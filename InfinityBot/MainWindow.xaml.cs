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
            // Data Bindings
            {
                ChannelsBox.ItemsSource = ChannelsList;
                ChannelDataGrid.ItemsSource = ChannelsList;
            }

            try
            {
                LoadDefault();
                LoadChannels();
                await Console.Log("Ready to start bot. Default settings loaded.");
            }
            catch
            {
                await Console.Log("Ready to start bot.");
            }
            if (System.Diagnostics.Debugger.IsAttached)
                Title += " - Debugging";

            await TrayIcon.Initialize();
            TrayIcon.ShowWindow += TrayIcon_ShowWindow;
        }

        private Bot bot;
        private TrayIcon TrayIcon = new TrayIcon();

        readonly string SettingsVersion = "0.1.1";
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
                    bot.Output += Console.Log;
                }

                try
                {
                    await bot.Start();
                }
                catch(Exception ex)
                {
                    await Console.Log(ex.ToString());
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

        #region Console and Status

        private async void Message(object sender, string message = null)
        {
            if (message != null)
            {
                string id = (string)(ChannelsBox.SelectedItem as ComboBoxItem).Tag;
                if (id == "-1")
                    await bot.ReplyToMessage(message);
                else
                    await bot.ServerMessage(message, bot.GetChannel(Convert.ToUInt64(id)));
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
        }

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
                "logToFile:" + Console.LoggingEnabled,
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
                Console.LoggingEnabled = Convert.ToBoolean(x[3].Replace("logToFile:", string.Empty));
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
                            Console.LoggingEnabled = Convert.ToBoolean(x[3].Replace("logToFile:", string.Empty));
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
                    await Load(dialog.FileName);
                }
                catch
                {
                    await Console.Log("Error: An error has occured while loading a setup file.");
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

            await Console.Log($"Text channel {channel.Guild.Name}/#{channel.Name} added.");
        }

        private void ClearChannels(object sender = null, EventArgs e = null) => ChannelsList.Clear();

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
            string link = "https://discordapp.com/oauth2/authorize?client_id=" + ClientID.Password + @"&scope=bot";
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

        private void TrayIcon_ShowWindow(object sender, EventArgs e) => Show();

        #endregion
    }
}
