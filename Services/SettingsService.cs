using System;
using System.IO;

namespace SpiceChecker.Services
{
    internal static class SettingsService
    {
        private static readonly string AppFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SpiceChecker");
        private static readonly string SettingsPath = Path.Combine(AppFolder, "settings.json");

        public class AppSettings
        {
            public string LastTheme { get; set; } = "FluentLight";
        }

        public static AppSettings Load()
        {
            try
            {
                if (!Directory.Exists(AppFolder))
                    Directory.CreateDirectory(AppFolder);

                if (!File.Exists(SettingsPath))
                    return new AppSettings();

                var json = File.ReadAllText(SettingsPath);
                return System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json)
                       ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                if (!Directory.Exists(AppFolder))
                    Directory.CreateDirectory(AppFolder);

                var json = System.Text.Json.JsonSerializer.Serialize(settings,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // silencieux, on ne fait pas planter l'appli pour ça
            }
        }
    }
}
