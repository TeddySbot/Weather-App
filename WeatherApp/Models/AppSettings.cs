namespace WeatherApp.Models
{
    public class AppSettings
    {
        public string DefaultCity { get; set; } = string.Empty;
        public string Language { get; set; } = "fr";
        public string ApiKey { get; set; } = string.Empty;
    }
}
