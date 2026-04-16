# Weather App – C# .NET 8 Windows Forms

Application météo Windows Forms utilisant l'API [OpenWeatherMap](https://openweathermap.org/api).

## Fonctionnalités

| Onglet | Description |
|--------|-------------|
| 🔍 Recherche | Météo actuelle d'une ville (température, description, humidité, coordonnées, icône) |
| 📅 Prévisions | Prévisions sur 5 jours à 12h00 affichées en colonnes |
| ⚙ Paramètres | Clé API, ville par défaut, langue des descriptions |

## Prérequis

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows (Windows Forms)
- Une clé API gratuite sur [openweathermap.org](https://openweathermap.org/appid)

## Installation & lancement

```bash
git clone <votre-repo>
cd WeatherApp
dotnet restore
dotnet run --project WeatherApp/WeatherApp.csproj
```

## Configuration

Au premier lancement, allez dans l'onglet **⚙ Paramètres** et renseignez :
- Votre **clé API** OpenWeatherMap
- (Optionnel) Une **ville par défaut**
- La **langue** des descriptions météo

Les paramètres sont sauvegardés dans `options.json` (exclu du dépôt git).

## Structure du projet

```
WeatherApp/
├── Models/
│   ├── AppSettings.cs       # Modèle des paramètres
│   └── WeatherData.cs       # Modèles météo / prévisions
├── Services/
│   ├── SettingsService.cs   # Lecture/écriture options.json
│   └── WeatherService.cs    # Appels API OpenWeatherMap
├── Forms/
│   └── MainForm.cs          # Interface principale (3 onglets)
└── Program.cs               # Point d'entrée
```
