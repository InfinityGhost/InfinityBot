using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InfinityBot
{
    public class TrayIcon
    {
        NotifyIcon NotifyIcon = new NotifyIcon();

        public Task Initialize()
        {
            string icon = @"InfinityBot.infinitybot.ico";
            Assembly assembly = Assembly.GetExecutingAssembly();

            NotifyIcon.Icon = new System.Drawing.Icon(assembly.GetManifestResourceStream(icon));
            NotifyIcon.MouseClick += NotifyIcon_Click;
            NotifyIcon.Text = "InfinityBot " + $"v{Information.AssemblyVersion}";

            return Task.CompletedTask;
        }

        public event EventHandler ShowWindow;

        public bool Visible
        {
            set => NotifyIcon.Visible = value;
            get => NotifyIcon.Visible;
        }

        private void NotifyIcon_Click(object sender, MouseEventArgs e)
        {
            ShowWindow?.Invoke(this, null);
        }
        
    }
}
