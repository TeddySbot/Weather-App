namespace WeatherApp.Models
{
    public class AppSettings
    {
        public string DefaultCity { get; set; } = string.Empty;
        public string Language    { get; set; } = "fr";
        // La clé API est lue depuis le fichier .env (OPENWEATHERMAP_API_KEY), pas stockée ici.
    }
}
