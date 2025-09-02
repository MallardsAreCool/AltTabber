using System;
using System.Collections.Generic;
using System.IO;

namespace AltTabber
{
    class Config
    {
        const string CONFIG_PATH = "hotkeys.cfg";

        public static Dictionary<string, char> LoadMappingConfig()
        {

            Dictionary<string, char> _processMap = [];
            try
            {
                var lines = File.ReadAllLines(CONFIG_PATH);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            foreach (var line in File.ReadAllLines(CONFIG_PATH))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) // allow comments
                    continue;

                var parts = line.Split('=', 2);
                if (parts.Length != 2 || string.IsNullOrEmpty(parts[1]) || parts[1].Length != 1)
                    continue;

                _processMap[parts[0].Trim()] = parts[1][0];
            }

            return _processMap;
        }
    }
}
