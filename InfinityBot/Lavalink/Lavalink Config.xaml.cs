using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace InfinityBot.Lavalink
{
    /// <summary>
    /// Interaction logic for Lavalink_Config.xaml
    /// </summary>
    public partial class Lavalink_Config : Window
    {
        public Lavalink_Config()
        {
            InitializeComponent();
            try
            {
                Configuration = new Config(System.IO.Directory.GetCurrentDirectory() + @"\Lavalink\application.yml");
                UpdateUI();
            }
            catch { }

            Window[] windows = { };
            Array.Resize(ref windows, Application.Current.Windows.Count);
            Application.Current.Windows.CopyTo(windows, 0);
            MainWindow = windows.ToList().FirstOrDefault(e => e is MainWindow) as MainWindow;

            JREbox.Text = MainWindow?.JREPath;
        }

        private Task UpdateUI()
        {
            var lava = Configuration.Lavalink;
            var server = Configuration.Server;
            var sources = Configuration.Lavalink.Sources;

            // Server tab
            Password.Password = lava.Password;
            BufferDurationMS.Text = lava.BufferDuration.ToString();
            YoutubePlaylistLoadLimit.Text = lava.LoadLimit.ToString();
            GCWarnings.IsChecked = lava.GarbageCollectionWarnings;

            // Networking tab
            IPAddress.Text = server.Address;
            IPAddressPort.Text = server.Port;

            // Sources tab
            SetCheckboxValue(YouTubeSource, sources.YouTube);
            SetCheckboxValue(BandcampSource, sources.Bandcamp);
            SetCheckboxValue(SoundCloudSource, sources.SoundCloud);
            SetCheckboxValue(TwitchSource, sources.Twitch);
            SetCheckboxValue(VimeoSource, sources.Vimeo);
            SetCheckboxValue(MixerSource, sources.Mixer);
            SetCheckboxValue(HTTPSource, sources.HTTP);
            SetCheckboxValue(LocalSource, sources.Local);

            return Task.CompletedTask;
        }
        private MainWindow MainWindow;

        void SettingsUpdated(object sender, RoutedEventArgs e) => SettingsUpdated(sender);
        void SettingsUpdated(object sender, TextChangedEventArgs e) => SettingsUpdated(sender);
        private void SettingsUpdated(object sender)
        {
            if (IsLoaded)
                switch (sender.GetType().Name)
                {
                    case "TextBox":
                        {
                            try
                            {
                                Configuration.Lavalink.BufferDuration = Convert.ToInt32(BufferDurationMS.Text);
                                Configuration.Lavalink.LoadLimit = Convert.ToInt32(YoutubePlaylistLoadLimit.Text);
                            }
                            catch { }
                            Configuration.Server.Address = IPAddress.Text;
                            Configuration.Server.Port = IPAddressPort.Text;
                            MainWindow.JREPath = JREbox.Text;
                            break;
                        }
                    case "PasswordBox":
                        {
                            Configuration.Lavalink.Password = Password.Password;
                            break;
                        }
                    case "CheckBox":
                        {
                            var sources = Configuration.Lavalink.Sources;
                            Configuration.Lavalink.GarbageCollectionWarnings = GCWarnings.IsChecked ?? false;
                            sources.YouTube = YouTubeSource.IsChecked ?? false;
                            sources.Bandcamp = BandcampSource.IsChecked ?? false;
                            sources.SoundCloud = SoundCloudSource.IsChecked ?? false;
                            sources.Twitch = TwitchSource.IsChecked ?? false;
                            sources.Vimeo = VimeoSource.IsChecked ?? false;
                            sources.Mixer = MixerSource.IsChecked ?? false;
                            sources.HTTP = HTTPSource.IsChecked ?? false;
                            sources.Local = LocalSource.IsChecked ?? false;
                            break;
                        }
                    default:
                        {
                            System.IO.File.AppendAllText("settingsUpdated.log", $"{DateTime.Now.ToLocalTime()}: {sender.GetType().Name}" + Environment.NewLine);
                            break;
                        }
                }
        }

        private Config Configuration;

        #region Main Buttons

        void OKButton(object sender, RoutedEventArgs e)
        {
            Configuration.Write();
            this.Close();
        }

        void CancelButton(object sender, RoutedEventArgs e) => this.Close();

        void ApplyButton(object sender, RoutedEventArgs e)
        {
            Configuration.Write();
        }

        void FindJRE(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "Java Executable (*.exe)|*.exe|All files (*.*)|*.*",
                InitialDirectory = @"C:\Program Files\Java",
                RestoreDirectory = true,
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                JREbox.Text = dialog.FileName;
        }

        #endregion


        #region Tools

        private void SetCheckboxValue(CheckBox checkBox, bool value) => checkBox.IsChecked = value;

        #endregion

    }
}
