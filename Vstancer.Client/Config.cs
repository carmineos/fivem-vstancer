using System;
using System.Collections.Generic;
using System.Linq;

namespace Vstancer.Client
{
    public class Config
    {
        protected Dictionary<string, string> Entries { get; set; }

        public Config(string content)
        {
            Entries = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(content))
                return;

            var splitted = content
                .Split('\n')
                .Where((line) => !line.Trim().StartsWith("#"))
                .Select((line) => line.Trim().Split('='))
                .Where((line) => line.Length == 2);

            foreach (var tuple in splitted)
            {
                Entries.Add(tuple[0], tuple[1]);
            }
        }

        public string Get(string key, string fallback = null)
        {
            if (!string.IsNullOrEmpty(key) && Entries.TryGetValue(key, out string value))
                return value;
            else
                return fallback;
        }

        public int GetIntValue(string key, int fallback)
        {
            if (!string.IsNullOrEmpty(key) && Entries.TryGetValue(key, out string value))
            {
                if (int.TryParse(value, out int tmp))
                    return tmp;
            }
            return fallback;
        }

        public float GetFloatValue(string key, float fallback)
        {
            if (!string.IsNullOrEmpty(key) && Entries.TryGetValue(key, out string value))
            {
                if (float.TryParse(value, out float tmp))
                    return tmp;
            }
            return fallback;
        }

        public bool GetBoolValue(string key, bool fallback)
        {
            if (!string.IsNullOrEmpty(key) && Entries.TryGetValue(key, out string value))
            {
                if (bool.TryParse(value, out bool tmp))
                    return tmp;
            }
            return fallback;
        }

        public long GetLongValue(string key, long fallback)
        {
            if (!string.IsNullOrEmpty(key) && Entries.TryGetValue(key, out string value))
            {
                if (long.TryParse(value, out long tmp))
                    return tmp;
            }
            return fallback;
        }
    }
}
