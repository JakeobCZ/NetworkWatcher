using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NetworkWatcher
{
    public class AdapterRule
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool MatchByName { get; set; }
    }

    public class SettingsStore
    {
        public List<AdapterRule> IgnoredAdapters { get; set; } = new List<AdapterRule>();
        // Legacy fields are retained so existing settings.json files migrate automatically.
        public List<string> IgnoredIds { get; set; } = new List<string>();
        public List<string> IgnoredNames { get; set; } = new List<string>();
        public bool MatchByName { get; set; }

        static string GetFolder()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NetworkWatcher");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }

        static string SettingsPath() => Path.Combine(GetFolder(), "settings.json");

        public static SettingsStore Load()
        {
            var path = SettingsPath();
            if (!File.Exists(path)) return new SettingsStore();
            try
            {
                var json = File.ReadAllText(path);
                var settings = JsonSerializer.Deserialize<SettingsStore>(json) ?? new SettingsStore();
                settings.MigrateLegacyRules();
                return settings;
            }
            catch
            {
                return new SettingsStore();
            }
        }

        private void MigrateLegacyRules()
        {
            if (IgnoredAdapters.Count > 0) return;

            if (MatchByName)
            {
                IgnoredAdapters.AddRange(IgnoredNames.ConvertAll(name => new AdapterRule
                {
                    Name = name,
                    MatchByName = true
                }));
            }
            else
            {
                IgnoredAdapters.AddRange(IgnoredIds.ConvertAll(id => new AdapterRule
                {
                    Id = id,
                    MatchByName = false
                }));
            }
        }

        public void Save()
        {
            var path = SettingsPath();
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}
