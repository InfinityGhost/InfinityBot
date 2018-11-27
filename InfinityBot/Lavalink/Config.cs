using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfinityBot.Lavalink
{
    public class Config
    {
        public Config() { }

        public Config(string path)
        {
            // TODO: add lavalink config loader

            var cfg = new YMLHandler(path);

            try
            {
                Server = new ServerProperties
                {
                    Address = cfg.GetString("address"),
                    Port = cfg.GetString("port"),
                };
                Lavalink = new LavalinkProperties
                {
                    Password = cfg.GetString("password"),
                    Sources = new LavalinkProperties.SourceProperties
                    {
                        YouTube = cfg.GetBool("youtube"),
                        Bandcamp = cfg.GetBool("bandcamp"),
                        SoundCloud = cfg.GetBool("soundcloud"),
                        Twitch = cfg.GetBool("twitch"),
                        Vimeo = cfg.GetBool("vimeo"),
                        Mixer = cfg.GetBool("mixer"),
                        HTTP = cfg.GetBool("http"),
                        Local = cfg.GetBool("local"),
                    },
                    BufferDuration = cfg.GetInt("bufferDurationMs"),
                    LoadLimit = cfg.GetInt("youtubePlaylistLoadLimit"),
                    GarbageCollectionWarnings = cfg.GetBool("gc-warnings"),
                };
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("errorDump.log", $"{DateTime.Now.ToLocalTime()}: {ex}");
            }
        }

        public ServerProperties Server;
        public LavalinkProperties Lavalink;

        #region Classes

        public class ServerProperties
        {
            public string Address;
            public string Port;
        }

        public class LavalinkProperties
        {
            public string Password;
            public SourceProperties Sources;
            public int BufferDuration;
            public int LoadLimit;
            public bool GarbageCollectionWarnings;
            
            public class SourceProperties
            {
                public bool YouTube;
                public bool Bandcamp;
                public bool SoundCloud;
                public bool Twitch;
                public bool Vimeo;
                public bool Mixer;
                public bool HTTP;
                public bool Local;
            }
        }

        #endregion
    }
}
