using System.Threading.Tasks;
using System.IO;
using static InfinityBot.Tools.ReadHelper;

namespace InfinityBot
{
    public class Settings
    {
        public Settings() { }
        public Settings(string path) => Load(path);

        public string Path { private set; get; }

        public string APIToken { set; get; }
        public string ClientID { set; get; }
        public bool LoggingEnabled { set; get; }

        public readonly char Splitter = ':';

        #region Methods

        public async void Save() => await Save(Path);
        public Task Save(string path)
        {
            File.WriteAllLines(path, new string[]
            {
                "apiToken" + Splitter + APIToken,
                "clientID" + Splitter + ClientID,
                "logToFile" + Splitter + LoggingEnabled,
            });

            Path = path;
            return Task.CompletedTask;
        }

        public async void Load() => await Load(Path);
        public Task Load(string path)
        {
            var contents = File.ReadAllLines(path);
            APIToken = contents.GetProperty("apiToken");
            ClientID = contents.GetProperty("clientID");
            LoggingEnabled = contents.GetProperty("logToFile").ToBool();

            Path = path;
            return Task.CompletedTask;
        }

        #endregion
    }
}
