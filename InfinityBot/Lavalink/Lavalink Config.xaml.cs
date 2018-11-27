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
        }

        // TODO: add config handler

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
            UpdateCheckbox(YouTubeSource, sources.YouTube);
            UpdateCheckbox(BandcampSource, sources.Bandcamp);
            UpdateCheckbox(SoundCloudSource, sources.SoundCloud);
            UpdateCheckbox(TwitchSource, sources.Twitch);
            UpdateCheckbox(VimeoSource, sources.Vimeo);
            UpdateCheckbox(MixerSource, sources.Mixer);
            UpdateCheckbox(HTTPSource, sources.HTTP);
            UpdateCheckbox(LocalSource, sources.Local);

            return Task.CompletedTask;
        }

        private void SettingsUpdated(object sender)
        {
            switch (sender.GetType().Name)
            {
                default:
                    {
                        System.IO.File.AppendAllText("settingsUpdated.log", $"{DateTime.Now.ToLocalTime()}: {sender.GetType().Name}" + Environment.NewLine);
                        break;
                    }
            }
        }

        void SettingsUpdated(object sender, RoutedEventArgs e) => SettingsUpdated(sender);
        void SettingsUpdated(object sender, TextChangedEventArgs e) => SettingsUpdated(sender);

        private Config Configuration;

        private void UpdateCheckbox(CheckBox checkBox, bool value) => checkBox.IsChecked = value;

    }
}
