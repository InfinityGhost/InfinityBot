using System;
using System.Collections.Generic;
using System.Linq;

namespace InfinityBot.Tools
{
    public static class ReadExtensions
    {
        public static string GetProperty(this IEnumerable<string> contents, string objectName, string separator = ":")
        {
            var find = contents.Where(value => value.Contains(objectName));
            return find.FirstOrDefault().Replace(objectName + separator, string.Empty) ?? string.Empty;
        }

        public static bool ToBool(this string property)
        {
            switch(property.ToLower())
            {
                case "true":
                case "1":
                    return true;
                case "false":
                case "0":
                    return false;
                default:
                    throw new InvalidCastException();
            }
        }
    }
}
