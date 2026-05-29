using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.Globalization;
using System.Net.Http;
using WeatherApp.Models;
using WeatherApp.Services;

namespace WeatherApp.Views;

public partial class MainWindow : Window
{
    private readonly WeatherService  _weatherService  = new();
    private readonly SettingsService _settingsService = new();
    private AppSettings _settings = new();

    // Clé API chargée depuis .env (jamais stockée dans options.json)
    private readonly string _apiKey = EnvService.Get("OPENWEATHERMAP_API_KEY");

    private List<LanguageItem> _languages = new();

    public MainWindow()
    {
        InitializeComponent();

        _languages = BuildLanguageList();
        CboLanguage.ItemsSource = _languages;

        _settings = _settingsService.Load();
        PopulateSettingsTab();
        PreFillFromSettings();
    }

    // =====================================================================
    // ONGLET 1 – RECHERCHE
    // =====================================================================
    private void TxtSearchCity_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) BtnSearch_Click(sender, e);
    }

    private async void BtnSearch_Click(object? sender, RoutedEventArgs e)
    {
        var city = TxtSearchCity.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(city))    { ShowError(LblSearchStatus, "Veuillez entrer un nom de ville."); return; }
        if (string.IsNullOrEmpty(_apiKey)) { ShowError(LblSearchStatus, "Clé API manquante. Vérifiez votre fichier .env (OPENWEATHERMAP_API_KEY)."); return; }

        LblSearchStatus.Text       = "Chargement…";
        LblSearchStatus.Foreground = Brushes.Gray;
        BtnSearch.IsEnabled        = false;
        PnlSearchResult.IsVisible  = false;

        try
        {
            var data = await _weatherService.GetCurrentWeatherAsync(city, _apiKey, _settings.Language);

            LblCityName.Text  = data.CityName;
            LblLatLon.Text    = $"📍 Lat : {data.Latitude:F4}  |  Lon : {data.Longitude:F4}";
            LblTemp.Text      = $"{data.Temperature:F1} °C";
            LblTemp.Foreground = new SolidColorBrush(TempColor(data.Temperature));
            LblDesc.Text      = $"☁  {data.Description}";
            LblHumidity.Text  = $"💧 Humidité : {data.Humidity} %";

            LblSearchStatus.Text      = string.Empty;
            PnlSearchResult.IsVisible = true;

            _ = LoadIconAsync(data.IconCode, ImgWeatherIcon);
        }
        catch (CityNotFoundException ex) { ShowError(LblSearchStatus, ex.Message); }
        catch (HttpRequestException)     { ShowError(LblSearchStatus, "Impossible de contacter le serveur. Vérifiez votre connexion internet."); }
        catch (TaskCanceledException)    { ShowError(LblSearchStatus, "La requête a expiré. Vérifiez votre connexion internet."); }
        catch (Exception ex)             { ShowError(LblSearchStatus, $"Erreur inattendue : {ex.Message}"); }
        finally { BtnSearch.IsEnabled = true; }
    }

    // =====================================================================
    // ONGLET 2 – PRÉVISIONS
    // =====================================================================
    private void TxtForecastCity_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) BtnForecast_Click(sender, e);
    }

    private async void BtnForecast_Click(object? sender, RoutedEventArgs e)
    {
        var city = TxtForecastCity.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(city))    { ShowError(LblForecastStatus, "Veuillez entrer un nom de ville."); return; }
        if (string.IsNullOrEmpty(_apiKey)) { ShowError(LblForecastStatus, "Clé API manquante. Vérifiez votre fichier .env (OPENWEATHERMAP_API_KEY)."); return; }

        LblForecastStatus.Text       = "Chargement…";
        LblForecastStatus.Foreground = Brushes.Gray;
        BtnForecast.IsEnabled        = false;
        PnlForecastColumns.Children.Clear();

        try
        {
            var forecast = await _weatherService.GetForecastAsync(city, _apiKey, _settings.Language);

            if (forecast.Forecasts.Count == 0)
            {
                ShowError(LblForecastStatus, "Aucune prévision disponible pour les prochains jours à 12h00.");
                return;
            }

            LblForecastStatus.Text       = $"{forecast.CityName}  —  📍 {forecast.Latitude:F4}, {forecast.Longitude:F4}";
            LblForecastStatus.Foreground = new SolidColorBrush(Color.FromRgb(50, 80, 120));

            for (int i = 0; i < forecast.Forecasts.Count && i < 5; i++)
                PnlForecastColumns.Children.Add(CreateForecastColumn(forecast.Forecasts[i]));
        }
        catch (CityNotFoundException ex) { ShowError(LblForecastStatus, ex.Message); }
        catch (HttpRequestException)     { ShowError(LblForecastStatus, "Impossible de contacter le serveur. Vérifiez votre connexion internet."); }
        catch (TaskCanceledException)    { ShowError(LblForecastStatus, "La requête a expiré. Vérifiez votre connexion internet."); }
        catch (Exception ex)             { ShowError(LblForecastStatus, $"Erreur inattendue : {ex.Message}"); }
        finally { BtnForecast.IsEnabled = true; }
    }

    private Border CreateForecastColumn(WeatherData data)
    {
        var imgIcon = new Image { Width = 64, Height = 64, Stretch = Stretch.Uniform };
        _ = LoadIconAsync(data.IconCode, imgIcon);

        var stack = new StackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = data.DateTime.ToString("ddd dd MMM", new CultureInfo("fr-FR")),
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 60, 110)),
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                new TextBlock
                {
                    Text = "12:00",
                    Foreground = Brushes.Gray,
                    FontSize = 12,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                new Border
                {
                    Child = imgIcon,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                new TextBlock
                {
                    Text = $"{data.Temperature:F1} °C",
                    FontSize = 16,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(TempColor(data.Temperature)),
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                new TextBlock
                {
                    Text = data.Description,
                    FontStyle = FontStyle.Italic,
                    Foreground = new SolidColorBrush(Color.FromRgb(60, 80, 100)),
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 140,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                new TextBlock
                {
                    Text = $"💧 {data.Humidity} %",
                    Foreground = Brushes.SteelBlue,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            }
        };

        return new Border
        {
            Background  = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Padding     = new Thickness(12),
            Width       = 170,
            Child       = stack
        };
    }

    // =====================================================================
    // ONGLET 3 – PARAMÈTRES
    // =====================================================================
    private void PopulateSettingsTab()
    {
        TxtDefaultCity.Text = _settings.DefaultCity;

        CboLanguage.SelectedItem = _languages.FirstOrDefault(l => l.Code == _settings.Language)
                                ?? _languages.FirstOrDefault(l => l.Code == "fr");
    }

    private void PreFillFromSettings()
    {
        if (!string.IsNullOrEmpty(_settings.DefaultCity))
        {
            TxtSearchCity.Text   = _settings.DefaultCity;
            TxtForecastCity.Text = _settings.DefaultCity;
        }
    }

    private async void BtnSaveSettings_Click(object? sender, RoutedEventArgs e)
    {
        _settings.DefaultCity = TxtDefaultCity.Text?.Trim() ?? "";
        _settings.Language    = (CboLanguage.SelectedItem as LanguageItem)?.Code ?? "fr";
        _settingsService.Save(_settings);

        PreFillFromSettings();
        LblSettingsSaved.Text = "✔ Paramètres enregistrés.";
        await Task.Delay(2500);
        LblSettingsSaved.Text = "";
    }

    // =====================================================================
    // HELPERS
    // =====================================================================
    private async Task LoadIconAsync(string iconCode, Image imgControl)
    {
        try
        {
            var bytes = await _weatherService.GetWeatherIconAsync(iconCode);
            using var ms = new System.IO.MemoryStream(bytes);
            imgControl.Source = new Bitmap(ms);
        }
        catch { /* icône non critique */ }
    }

    private static void ShowError(TextBlock lbl, string message)
    {
        lbl.Foreground = Brushes.Firebrick;
        lbl.Text       = message;
    }

    private static Color TempColor(double temp) => temp switch
    {
        < 0  => Colors.DeepSkyBlue,
        < 10 => Colors.CornflowerBlue,
        < 20 => Colors.ForestGreen,
        < 28 => Colors.DarkOrange,
        _    => Colors.Crimson
    };

    private static List<LanguageItem> BuildLanguageList() =>
        new[]
        {
            ("af", "Afrikaans"),    ("al", "Albanian"),    ("ar", "Arabic"),
            ("az", "Azerbaijani"), ("bg", "Bulgarian"),   ("ca", "Catalan"),
            ("cz", "Czech"),       ("da", "Danish"),      ("de", "German"),
            ("el", "Greek"),       ("en", "English"),     ("eu", "Basque"),
            ("fa", "Persian"),     ("fi", "Finnish"),     ("fr", "French"),
            ("gl", "Galician"),    ("he", "Hebrew"),      ("hi", "Hindi"),
            ("hr", "Croatian"),    ("hu", "Hungarian"),   ("id", "Indonesian"),
            ("it", "Italian"),     ("ja", "Japanese"),    ("kr", "Korean"),
            ("la", "Latvian"),     ("lt", "Lithuanian"),  ("mk", "Macedonian"),
            ("no", "Norwegian"),   ("nl", "Dutch"),       ("pl", "Polish"),
            ("pt", "Portuguese"),  ("pt_br", "Português Brasil"),
            ("ro", "Romanian"),    ("ru", "Russian"),     ("sv", "Swedish"),
            ("sk", "Slovak"),      ("sl", "Slovenian"),   ("sp", "Spanish"),
            ("sr", "Serbian"),     ("th", "Thai"),        ("tr", "Turkish"),
            ("ua", "Ukrainian"),   ("vi", "Vietnamese"),  ("zh_cn", "Chinese Simplified"),
            ("zh_tw", "Chinese Traditional"), ("zu", "Zulu")
        }
        .Select(l => new LanguageItem(l.Item1, $"{l.Item2} ({l.Item1})"))
        .ToList();
}

/// <summary>Représente une langue disponible dans la ComboBox paramètres.</summary>
public record LanguageItem(string Code, string DisplayName)
{
    public override string ToString() => DisplayName;
}
