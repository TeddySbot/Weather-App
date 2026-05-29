using Newtonsoft.Json.Linq;
using WeatherApp.Models;

namespace WeatherApp.Services
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl     = "https://api.openweathermap.org/data/2.5";
        private const string IconBaseUrl = "https://openweathermap.org/img/wn";

        public WeatherService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<WeatherData> GetCurrentWeatherAsync(string city, string apiKey, string lang = "fr")
        {
            var url = $"{BaseUrl}/weather?q={Uri.EscapeDataString(city)}&appid={apiKey}&units=metric&lang={lang}";
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new CityNotFoundException($"La ville \"{city}\" est introuvable.");

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var obj  = JObject.Parse(json);

            return new WeatherData
            {
                CityName    = obj["name"]!.ToString(),
                Latitude    = obj["coord"]!["lat"]!.Value<double>(),
                Longitude   = obj["coord"]!["lon"]!.Value<double>(),
                Temperature = obj["main"]!["temp"]!.Value<double>(),
                Description = CapitalizeFirst(obj["weather"]![0]!["description"]!.ToString()),
                Humidity    = obj["main"]!["humidity"]!.Value<int>(),
                IconCode    = obj["weather"]![0]!["icon"]!.ToString(),
                DateTime    = DateTime.Now
            };
        }

        public async Task<ForecastData> GetForecastAsync(string city, string apiKey, string lang = "fr")
        {
            var url = $"{BaseUrl}/forecast?q={Uri.EscapeDataString(city)}&appid={apiKey}&units=metric&lang={lang}";
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new CityNotFoundException($"La ville \"{city}\" est introuvable.");

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var obj  = JObject.Parse(json);

            var cityName = obj["city"]!["name"]!.ToString();
            var lat      = obj["city"]!["coord"]!["lat"]!.Value<double>();
            var lon      = obj["city"]!["coord"]!["lon"]!.Value<double>();

            // Prévisions à 12:00 pour les 5 prochains jours
            var forecasts  = new List<WeatherData>();
            var today      = DateTime.Today;
            var targetDays = Enumerable.Range(1, 5).Select(i => today.AddDays(i)).ToList();

            foreach (var item in obj["list"]!.Children())
            {
                var dtTxt = item["dt_txt"]!.ToString();
                var dt    = DateTime.Parse(dtTxt);

                if (dt.Hour == 12 && targetDays.Any(d => d.Date == dt.Date))
                {
                    forecasts.Add(new WeatherData
                    {
                        CityName    = cityName,
                        Latitude    = lat,
                        Longitude   = lon,
                        Temperature = item["main"]!["temp"]!.Value<double>(),
                        Description = CapitalizeFirst(item["weather"]![0]!["description"]!.ToString()),
                        Humidity    = item["main"]!["humidity"]!.Value<int>(),
                        IconCode    = item["weather"]![0]!["icon"]!.ToString(),
                        DateTime    = dt
                    });
                }
            }

            return new ForecastData
            {
                CityName  = cityName,
                Latitude  = lat,
                Longitude = lon,
                Forecasts = forecasts
            };
        }

        /// <summary>Retourne les octets bruts de l'icône météo (format PNG).</summary>
        public async Task<byte[]> GetWeatherIconAsync(string iconCode)
        {
            var url = $"{IconBaseUrl}/{iconCode}@2x.png";
            return await _httpClient.GetByteArrayAsync(url);
        }

        private static string CapitalizeFirst(string s)
            => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
    }

    public class CityNotFoundException : Exception
    {
        public CityNotFoundException(string message) : base(message) { }
    }
}
