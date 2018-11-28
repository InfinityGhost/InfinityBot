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
            var x = FetchProperty(property + ": ").ToLower();
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
            var fetch = FetchProperty(property + ": ");
            int lastSpace = 0;
            try
            {
                lastSpace = fetch.IndexOf(" ");
                return Convert.ToInt32(fetch.Remove(lastSpace, fetch.Length - lastSpace));
            }
            catch
            {
                return Convert.ToInt32(fetch);
            }
        }

        private string FetchProperty(string propertyTitle)
        {
            if (Contents.Find(x => x.Contains(propertyTitle)) == null)
                return string.Empty;
            IEnumerable<string> search = Contents.Where(line => line.Contains(propertyTitle));
            if (search.First() == null)
                throw new NullReferenceException();
            var fullLine = search.First().Trim();
            var ret = fullLine.Replace(propertyTitle, string.Empty);
            return ret;
        }

        #endregion
    }
}
