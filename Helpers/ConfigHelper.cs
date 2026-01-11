using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using EFT;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Utilities;
using static GClass2175;

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
            var configPath = Path.Combine(pluginPath, "config\\config.jsonc");

            if (!File.Exists(configPath))
            {
                Plugin.LogSource.LogWarning("Config file not found!");
                return new ClientConfig();
            }

            var json = File.ReadAllText(configPath);

            var settings = new JsonLoadSettings
            {
                CommentHandling = CommentHandling.Ignore,
                LineInfoHandling = LineInfoHandling.Ignore
            };


            var parsed = JObject.Parse(json, settings);

            var config = parsed.ToObject<ClientConfig>() ?? new ClientConfig();

            Plugin.LogSource.LogInfo(
                $"Loaded {config.DeviceTypeOverride.Count} device overrides"
            );

            return config;
        }
    }
}
