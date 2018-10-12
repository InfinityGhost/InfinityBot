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
using Discord;
using Discord.WebSocket;
using System.Windows.Forms;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using System.Threading;
using Clipboard = System.Windows.Clipboard;

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
            try
            {
                LoadDefault();
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

        #region UI Code

        void StartBot(object sender, RoutedEventArgs e)
        {
            StartButton.Content = "Stop Bot";
            bot = new Bot(APIToken.Text);
            StartButton.Click += KillBot; // Currently broken
            bot.TerminalUpdate += TerminalUpdate;
            if (bot.MainAsync() == Task.CompletedTask)
            {
                StartButton.Content = "Start Bot";
            }
        }

        private async void KillBot(object sender, RoutedEventArgs e)
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
        }
        void TerminalUpdate(string[] text) => Array.ForEach(text, line => TerminalUpdate(line));
        async void TerminalUpdate(object sender, string e)
        {
            await BotUpdate(e);
        }
        
        Task BotUpdate(string text)
        {
            TerminalUpdate(text);
            return Task.CompletedTask;
        }

        void TerminalClear()
        {
            Terminal.Text = string.Empty;
        }

        void TerminalCommand(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                
            }
        }

        // Context Menu

        void TerminalClear(object sender, RoutedEventArgs e) => TerminalClear();

        void TerminalCopy(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(Terminal.Text);
        }

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

        #endregion

        #region Menu Buttons

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
        }

        #endregion


    }
}
