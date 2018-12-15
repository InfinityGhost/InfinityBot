using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            DataContext = this;
        }

        /// <summary>
        /// The status output from the console.
        /// </summary>
        public event EventHandler<string> Status;

        /// <summary>
        /// Message sent through the message box.
        /// </summary>
        public event EventHandler<string> Message;

        /// <summary>
        /// The last key pressed in the message box.
        /// </summary>
        public event EventHandler<Key> BoxKey;

        /// <summary>
        /// The prefix for console message output.
        /// </summary>
        private static string Prefix => DateTime.Now.ToLocalTime() + ": ";

        /// <summary>
        /// The path for the log file.
        /// </summary>
        private static string LogPath => Directory.GetCurrentDirectory() + @"\" + "log.log";
        
        #region Properties

        /// <summary>
        /// Returns whether the console is empty.
        /// </summary>
        public bool IsEmpty => TextBlock.Text == string.Empty || TextBlock.Text == null;

        /// <summary>
        /// Controls the visibility of the horizontal scroll bar.
        /// </summary>
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

        /// <summary>
        /// Controls the visibility of the vertical scroll bar.
        /// </summary>
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

        /// <summary>
        /// Controls if the console saves to the log.
        /// </summary>
        [Bindable(true), Category("Common")]
        public bool LoggingEnabled
        {
            get => (bool)GetValue(LoggingEnabledProperty);
            set => SetValue(LoggingEnabledProperty, value);
        }

        [Bindable(true), Category("Common")]
        public string SelectedChannel
        {
            get => (string)GetValue(SelectedChannelProperty);
            set => SetValue(SelectedChannelProperty, value);
        }
        
        #endregion

        #region Public Methods

        /// <summary>
        /// Writes text to the console buffer.
        /// </summary>
        /// <param name="text">The text to write to the console.</param>
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

        /// <summary>
        /// Clears the console buffer.
        /// </summary>
        public async Task Clear()
        {
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                TextBlock.Text = string.Empty;
                
            }));
        }

        /// <summary>
        /// Copies all of the text from the console buffer.
        /// </summary>
        public Task Copy()
        {
            Clipboard.SetText(TextBlock.Text);
            return Task.CompletedTask; 
        }

        /// <summary>
        /// Writes text to the console's log file.
        /// </summary>
        /// <param name="text">The text to log.</param>
        private Task WriteLog(string text)
        {
            File.AppendAllText(LogPath, text + Environment.NewLine);
            return Task.CompletedTask;
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

        #region Dependency Properties

        public static readonly DependencyProperty LoggingEnabledProperty = DependencyProperty.Register(
            "LoggingEnabled", typeof(bool), typeof(Console));

        public static readonly DependencyProperty SelectedChannelProperty = DependencyProperty.Register(
            "SelectedChannel", typeof(string), typeof(Console));

        #endregion
    }
}
