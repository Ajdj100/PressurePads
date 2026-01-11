using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using EFT;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Utilities;

namespace PressurePads.Helpers
{
    public class ClientConfig
    {
        public Dictionary<string, int> DeviceTypeOverride { get; set; } = new();
    }

    public static class ConfigLoader
    {
        public static ClientConfig Load(string pluginPath)
        {
            var configPath = Path.Combine(pluginPath, "config.jsonc");

            if (!File.Exists(configPath))
            {
                return new ClientConfig();
            }

            var json = File.ReadAllText(configPath);

            // JSONC support (strip comments)
            var parsed = JObject.Parse(json);

            return parsed.ToObject<ClientConfig>() ?? new ClientConfig();
        }
    }
}
