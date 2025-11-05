using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NetworkWatcher
{
    public class SettingsStore
    {
        public List<string> IgnoredIds { get; set; } = new List<string>();

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
                return JsonSerializer.Deserialize<SettingsStore>(json) ?? new SettingsStore();
            }
            catch
            {
                return new SettingsStore();
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
