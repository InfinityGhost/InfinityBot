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
            Path = path;
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
                System.IO.File.AppendAllText("errorDump.log", $"{DateTime.Now.ToLocalTime()}: {ex}" + Environment.NewLine);
            }
        }

        public string Path;

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

        #region Writing

        public void Write() => Write(Path);
        public Task Write(string path)
        {
            List<string> vs = new List<string>
            {
                "server:",
                "  port: " + Server.Port,
                "  address: " + Server.Address,
                "spring:",
                "  main:",
                "    banner-mode: log",
                "lavalink:",
                "  server:",
               $"    password: \"{Lavalink.Password}\"",
                "    sources:",
                "      youtube: " + Lavalink.Sources.YouTube.ToString().ToLower(),
                "      bandcamp: " + Lavalink.Sources.Bandcamp.ToString().ToLower(),
                "      soundcloud: " + Lavalink.Sources.SoundCloud.ToString().ToLower(),
                "      twitch: " + Lavalink.Sources.Twitch.ToString().ToLower(),
                "      vimeo: " + Lavalink.Sources.Vimeo.ToString().ToLower(),
                "      mixer: " + Lavalink.Sources.Mixer.ToString().ToLower(),
                "      http: " + Lavalink.Sources.HTTP.ToString().ToLower(),
                "      local: " + Lavalink.Sources.Local.ToString().ToLower(),
                "    bufferDurationMs: " + Lavalink.BufferDuration,
                "    youtubePlaylistLoadLimit: " + Lavalink.LoadLimit,
                "    gc-warnings: " + Lavalink.GarbageCollectionWarnings.ToString().ToLower(),
                string.Empty,
                "metrics:",
                "  prometheus:",
                "    enabled: false",
                "    endpoint: /metrics",
                string.Empty,
                "sentry:",
                "  dsn: \"\"",
                string.Empty,
                "logging:",
                "  file:",
                "    max-history: 30",
                "    max-size: 1GB",
                "  path: ./logs/",
                string.Empty,
                "  level:",
                "    root: INFO",
                "    lavalink: INFO"
            };
            System.IO.File.WriteAllLines(path, vs.ToArray());
            return Task.CompletedTask;
        }

        #endregion
    }
}
