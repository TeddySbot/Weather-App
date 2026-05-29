namespace WeatherApp.Services
{
    /// <summary>
    /// Lit les variables d'un fichier .env situé à côté de l'exécutable.
    /// Format supporté : KEY=VALUE (lignes commençant par # ignorées).
    /// </summary>
    public static class EnvService
    {
        private static readonly string EnvPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, ".env");

        public static string Get(string key)
        {
            if (!File.Exists(EnvPath))
                return string.Empty;

            foreach (var line in File.ReadAllLines(EnvPath))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith('#') || !trimmed.Contains('='))
                    continue;

                var idx = trimmed.IndexOf('=');
                var k   = trimmed[..idx].Trim();
                var v   = trimmed[(idx + 1)..].Trim();

                if (k == key)
                    return v;
            }

            return string.Empty;
        }
    }
}
