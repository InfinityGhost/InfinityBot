using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

namespace InfinityBot.Lavalink
{
    public class YMLHandler
    {
        public YMLHandler(string path)
        {
            Path = path;
            Contents = File.ReadAllLines(path).ToList();
        }

        public readonly string Path;
        private List<string> Contents;

        #region Methods

        public string GetString(string property)
        {
            var x = FetchProperty(property + ": ");
            return x.Replace("\"", string.Empty);
        }

        public bool GetBool(string property)
        {
            var x = FetchProperty(property + ": ");
            if (x.Contains("true"))
            {
                return true;
            }
            else if(x.Contains("false"))
            {
                return false;
            }
            else
            {
                return false;
            }
        }

        public int GetInt(string property)
        {
            return Convert.ToInt32(FetchProperty(property + ": "));
        }

        private string FetchProperty(string propertyTitle)
        {
            if (Contents.Find(x => x.StartsWith(propertyTitle)) == null)
                return string.Empty;
            IEnumerable<string> search = Contents.Where(line => line.StartsWith(propertyTitle));
            if (search.First() == null)
                return string.Empty;
            return search.First().Replace(propertyTitle, string.Empty);
        }

        #endregion
    }
}
