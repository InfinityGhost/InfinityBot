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

namespace InfinityBot.Controls
{
    /// <summary>
    /// Interaction logic for Console.xaml
    /// </summary>
    public partial class Console : UserControl
    {
        public Console()
        {
            InitializeComponent();
        }

        public event EventHandler<string> Status;

        public event EventHandler<string> Message;
        public event EventHandler<Key> BoxKey;

        private static string Prefix => DateTime.Now.ToLocalTime() + ": ";
        private static string LogPath => Directory.GetCurrentDirectory() + @"\" + "log.log";
        
        private static object WriteLocker = new object();

        #region Properties

        public bool IsEmpty => TextBlock.Text == string.Empty || TextBlock.Text == null;

        public bool HorizontalScrollbarEnabled
        {
            set
            {
                switch (value)
                {
                    case true:
                        ScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                        break;
                    case false:
                        ScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                        break;
                }
            }
            get => ScrollViewer.HorizontalScrollBarVisibility == ScrollBarVisibility.Auto;
        }

        public bool VerticalScrollbarEnabled
        {
            set
            {
                switch (value)
                {
                    case true:
                        ScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                        break;
                    case false:
                        ScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                        break;
                }
            }
            get => ScrollViewer.VerticalScrollBarVisibility == ScrollBarVisibility.Auto;
        }

        public bool LoggingEnabled
        {
            get => LogBox.IsChecked;
            set => LogBox.IsChecked = value;
        }

        #endregion

        #region Public Methods

        public async Task Log(string text)
        {
            await Dispatcher.BeginInvoke(new Action( async () =>
            {
                if (IsEmpty)
                    TextBlock.Text += Prefix + text;
                else
                    TextBlock.Text += Environment.NewLine + Prefix + text;

                if (LoggingEnabled)
                    await WriteLog(Prefix + text);
            }));

            Status?.Invoke(this, text);
        }

        public async void Log(object sender, string text) => await Log(text);

        public async Task Clear()
        {
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                TextBlock.Text = string.Empty;
            }));
        }

        public Task Copy()
        {
            Clipboard.SetText(TextBlock.Text);
            return Task.CompletedTask; 
        }

        public Task WriteLog(string text)
        {
            lock (WriteLocker)
            {
                using (FileStream file = new FileStream(LogPath, FileMode.Append, FileAccess.Write, FileShare.Read))
                using (StreamWriter writer = new StreamWriter(file, Encoding.Unicode))
                {
                    writer.Write(text);
                }
            }
            return Task.CompletedTask;
            //await File.AppendText(LogPath).WriteLineAsync(text);
        }

        #endregion

        #region Message Box

        private void MessageBox_KeyDown(object sender, KeyEventArgs e)
        {
            var key = e.Key;
            if (key == Key.Enter)
            {
                var box = sender as TextBox;
                Message?.Invoke(this, box.Text);
                box.Text = string.Empty;
            }
            BoxKey?.Invoke(this, key);
        }

        #endregion

        #region Context Menu

        private async void Clear(object sender, EventArgs e) => await Clear();
        private async void Copy(object sender, EventArgs e) => await Copy();
        private void OpenLogFile(object sender, EventArgs e) => System.Diagnostics.Process.Start(LogPath);

        #endregion
    }
}
