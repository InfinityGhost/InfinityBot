using System.Windows;
using System.Reflection;
using System.Windows.Controls;
using System.Diagnostics;

namespace InfinityBot
{
    /// <summary>
    /// Interaction logic for AboutBox.xaml
    /// </summary>
    public partial class AboutBox : Window
    {
        public AboutBox()
        {
            InitializeComponent();
            GetDetails();
        }

        public void GetDetails()
        {
            DiscordTag.Content = "InfinityGhost#7843";
            Version.Content = AssemblyVersion;
            Website.Content = string.Empty;
        }

        
        #region Assembly Information

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        #endregion

        #region Menu Buttons

        void CloseButton(object sender, RoutedEventArgs e) => Close();

        #endregion

        #region Discord Tag Context Menu

        private void DiscordTag_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var label = (sender as Label);
            label.ContextMenu.IsOpen = true;
        }

        void CopyTagButton(object sender, RoutedEventArgs e) => Clipboard.SetText((string)DiscordTag.Content);

        void OpenDevDiscord(object sender, RoutedEventArgs e) => Process.Start("https://discord.gg/aQSZ2WC");

        #endregion

        #region Website

        void OpenWebsite(object sender, RoutedEventArgs e)
        {
            var label = sender as Label;
            if ((string)label.Content != string.Empty || (string)label.Content != null)
                Process.Start((string)label.Content);
        }

        #endregion



    }
}
