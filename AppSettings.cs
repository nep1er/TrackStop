using System.IO;
using Newtonsoft.Json;

namespace TrackStop
{
    public static class AppSettings
    {
        private static readonly string SettingsPath = Path.Combine(
            System.AppDomain.CurrentDomain.BaseDirectory,
            "settings.json");

        public static Settings CurrentSettings { get; private set; } = new Settings();

        public class Settings
        {
            public string Theme { get; set; } = "Light";
            public string AccentColor { get; set; } = "#FFAB0000";
        }

        public static void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    CurrentSettings = JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
                }
            }
            catch
            {
                CurrentSettings = new Settings();
            }
        }

        public static void SaveSettings()
        {
            try
            {
                // Используем полное имя Formatting для избежания конфликта
                string json = JsonConvert.SerializeObject(CurrentSettings, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // Игнорируем ошибки записи
            }
        }
    }
}