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
            }
            catch { }
        }

        // TODO: add config handler

        
        private void SettingsUpdated(object sender)
        {

        }

        void SettingsUpdated(object sender, RoutedEventArgs e) => SettingsUpdated(sender);
        void SettingsUpdated(object sender, TextChangedEventArgs e) => SettingsUpdated(sender);

        private Config Configuration;

    }
}
