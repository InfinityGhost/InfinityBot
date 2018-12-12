﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using Discord;
using Discord.WebSocket;

namespace InfinityBot
{
    public class Channels : List<ComboBoxItem>
    {
        #region Class Methods

        public void Add(SocketGuildChannel channel) => base.Add(ConvertChannel(channel));

        public void Add(SocketChannel channel)
        {
            if (channel is SocketGuildChannel guildChannel)
                Add(guildChannel);
        }

        public void Add(string name, ulong id)
        {
            base.Add(new ComboBoxItem
            {
                Content = name,
                Tag = id.ToString(),
            });
        }

        public void AddFile(string path)
        {
            var file = File.ReadAllLines(path).ToList();
            file.ForEach(item =>
            {
                Add(item.Split(',')[0], Convert.ToUInt64(item.Split(',')[1]));
            });
        }

        #endregion

        #region Static Methods

        public static ComboBoxItem ConvertChannel(SocketGuildChannel channel)
        {
            return new ComboBoxItem
            {
                Content = channel.Name,
                Tag = channel.Id,
            };
        }

        public static string GetName(SocketGuildChannel channel) => $"{channel.Guild.Name}/#{channel.Name}";

        #endregion
    }
}
