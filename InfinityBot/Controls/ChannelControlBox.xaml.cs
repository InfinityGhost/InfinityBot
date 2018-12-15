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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace InfinityBot.Controls
{
    /// <summary>
    /// Interaction logic for ObjectInsertBox.xaml
    /// </summary>
    public partial class ChannelControlBox : UserControl
    {
        public ChannelControlBox()
        {
            InitializeComponent();
        }

        public event EventHandler<String> Command;

        private void KeyPress(object sender, KeyEventArgs e)
        {
            var key = e.Key;
            switch(key)
            {
                case Key.Enter:
                    {
                        ulong? id = (sender as TextBox).Text.ToULong();
                        string commandName = (string)(ComboBox.SelectedItem as ComboBoxItem).Tag;
                        (sender as TextBox).Text = string.Empty;
                        Command?.Invoke(sender, $"{commandName},{id}");
                        break;
                    }
                case Key.Down:
                    {
                        if (ComboBox.SelectedIndex != ComboBox.Items.Count)
                            ComboBox.SelectedIndex++;
                        break;
                    }
                case Key.Up:
                    {
                        if (ComboBox.SelectedIndex != 0)
                            ComboBox.SelectedIndex--;
                        break;
                    }
            }
        }
    }

    static class Extensions
    {
        public static ulong? ToULong(this string content)
        {
            try
            {
                return Convert.ToUInt64(content);
            }
            catch
            {
                return null;
            }
        }
    }
}
