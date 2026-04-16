using Newtonsoft.Json;
using WeatherApp.Models;

namespace WeatherApp.Services
{
    public class SettingsService
    {
        private static readonly string SettingsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "options.json");

        public AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    var defaults = new AppSettings();
                    Save(defaults);
                    return defaults;
                }
                var json = File.ReadAllText(SettingsPath);
                return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(SettingsPath, json);
        }
    }
}
