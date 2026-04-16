namespace WeatherApp.Models
{
    public class WeatherData
    {
        public string CityName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Temperature { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Humidity { get; set; }
        public string IconCode { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
    }

    public class ForecastData
    {
        public List<WeatherData> Forecasts { get; set; } = new();
        public string CityName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
