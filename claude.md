# Documentation technique — Weather App

## Table des matières

1. [Vue d'ensemble du projet](#1-vue-densemble-du-projet)
2. [Structure des fichiers](#2-structure-des-fichiers)
3. [Fichier de projet — `WeatherApp.csproj`](#3-fichier-de-projet--weatherappcsproj)
4. [Point d'entrée — `Program.cs` + `App.axaml`](#4-point-dentrée--programcs--appaxaml)
5. [Modèles — dossier `Models/`](#5-modèles--dossier-models)
   - [AppSettings.cs](#appsettingscs)
   - [WeatherData.cs](#weatherdatacs)
6. [Services — dossier `Services/`](#6-services--dossier-services)
   - [EnvService.cs](#envservicecs)
   - [SettingsService.cs](#settingsservicecs)
   - [WeatherService.cs](#weatherservicecs)
7. [Interface graphique — `Views/MainWindow`](#7-interface-graphique--viewsmainwindow)
   - [MainWindow.axaml — structure XAML](#mainwindowaxaml--structure-xaml)
   - [MainWindow.axaml.cs — code-behind](#mainwindowaxamlcs--code-behind)
   - [Onglet Recherche](#onglet-recherche)
   - [Onglet Prévisions](#onglet-prévisions)
   - [Onglet Paramètres](#onglet-paramètres)
   - [Méthodes utilitaires](#méthodes-utilitaires)
8. [Fichiers de configuration](#8-fichiers-de-configuration)
   - [.env — clé API](#env--clé-api)
   - [options.json — préférences utilisateur](#optionsjson--préférences-utilisateur)
9. [Gestion des erreurs](#9-gestion-des-erreurs)
10. [Flux de données complet](#10-flux-de-données-complet)
11. [Dépendances externes](#11-dépendances-externes)

---

## 1. Vue d'ensemble du projet

**Weather App** est une application de bureau multiplateforme développée en **C# .NET 8** avec **Avalonia UI**. Elle permet de consulter la météo en temps réel et les prévisions sur 5 jours en interrogeant l'API publique **OpenWeatherMap**.

L'application est organisée en **trois couches** distinctes :

- **Models** : structures de données pures, sans logique.
- **Services** : logique métier (appels API, lecture/écriture fichier, lecture `.env`).
- **Views** : interface graphique Avalonia (XAML + code-behind).

La clé API n'est **jamais** stockée dans `options.json` ni commitée : elle est lue exclusivement depuis un fichier `.env` local.

---

## 2. Structure des fichiers

```
WeatherApp/
├── WeatherApp.sln              ← Fichier solution Visual Studio
├── .gitignore                  ← Exclusions git (options.json, *.env, bin/, obj/)
├── README.md                   ← Guide d'installation rapide
└── WeatherApp/
    ├── WeatherApp.csproj       ← Fichier de projet .NET (Avalonia)
    ├── Program.cs              ← Point d'entrée Avalonia
    ├── App.axaml               ← Déclaration de l'application et du thème
    ├── App.axaml.cs            ← Classe partielle App
    ├── .env                    ← Clé API (NON commité — ignoré par *.env dans .gitignore)
    ├── Models/
    │   ├── AppSettings.cs      ← Modèle des préférences (ville, langue)
    │   └── WeatherData.cs      ← Modèles des données météo
    ├── Services/
    │   ├── EnvService.cs       ← Lecture du fichier .env
    │   ├── SettingsService.cs  ← Lecture/écriture de options.json
    │   └── WeatherService.cs   ← Appels à l'API OpenWeatherMap
    ├── Views/
    │   ├── MainWindow.axaml    ← Fenêtre principale (3 onglets, XAML Avalonia)
    │   └── MainWindow.axaml.cs ← Code-behind (logique UI)
    └── Forms/
        └── MainForm.cs         ← Vide — conservé pour l'historique git
```

---

## 3. Fichier de projet — `WeatherApp.csproj`

```xml
<OutputType>WinExe</OutputType>
<TargetFramework>net8.0</TargetFramework>
```

- **`TargetFramework = net8.0`** : sans suffixe `-windows`, ce qui rend l'application compilable sur Windows, macOS et Linux grâce à Avalonia.
- **`WinExe`** : supprime la fenêtre de console sur Windows.
- **Copie de `.env`** : une entrée `<None>` avec `CopyToOutputDirectory="PreserveNewest"` copie automatiquement le `.env` dans le dossier de build à chaque compilation.
- **`Newtonsoft.Json` (v13)** : sérialisation de `options.json` et parsing des réponses API.
- **Avalonia (v11.2)** : framework UI multiplateforme (`Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`).

---

## 4. Point d'entrée — `Program.cs` + `App.axaml`

```csharp
[STAThread]
public static void Main(string[] args) => BuildAvaloniaApp()
    .StartWithClassicDesktopLifetime(args);

public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
```

- **`AppBuilder.Configure<App>()`** : lie le builder à la classe `App` définie dans `App.axaml.cs`.
- **`UsePlatformDetect()`** : détecte automatiquement le backend de rendu (Direct2D sur Windows, Metal sur macOS, X11/Wayland sur Linux).
- **`WithInterFont()`** : enregistre la police Inter (incluse via `Avalonia.Fonts.Inter`) comme police par défaut.
- **`StartWithClassicDesktopLifetime(args)`** : démarre la boucle d'événements et crée la `MainWindow` définie dans `App.axaml.cs`.

`App.axaml` déclare le thème Fluent (style visuel moderne) et la variante claire (`RequestedThemeVariant="Light"`).

---

## 5. Modèles — dossier `Models/`

### `AppSettings.cs`

```csharp
public class AppSettings
{
    public string DefaultCity { get; set; } = string.Empty;
    public string Language    { get; set; } = "fr";
    // La clé API est lue depuis .env, pas stockée ici.
}
```

Représente le contenu de `options.json`. La clé API en a été retirée : elle transite désormais par `EnvService`.

| Propriété | Type | Valeur par défaut | Description |
|-----------|------|-------------------|-------------|
| `DefaultCity` | `string` | `""` | Ville pré-remplie au démarrage |
| `Language` | `string` | `"fr"` | Code langue pour les descriptions météo |

---

### `WeatherData.cs`

Deux classes :

**`WeatherData`** — données météo pour un instant précis :

| Propriété | Source API | Description |
|-----------|-----------|-------------|
| `CityName` | `name` | Nom de la ville |
| `Latitude` | `coord.lat` | Latitude géographique |
| `Longitude` | `coord.lon` | Longitude géographique |
| `Temperature` | `main.temp` | Température en °C (`units=metric`) |
| `Description` | `weather[0].description` | Texte court (ex : "Ciel dégagé") |
| `Humidity` | `main.humidity` | Humidité relative en % |
| `IconCode` | `weather[0].icon` | Code d'icône (ex : `"01d"`) |
| `DateTime` | — | Heure actuelle (recherche) ou horodatage de prévision |

**`ForecastData`** — enveloppe pour les prévisions sur 5 jours : sépare les informations de la ville de la liste des 5 prévisions pour éviter la duplication.

---

## 6. Services — dossier `Services/`

### `EnvService.cs`

Lit un fichier `.env` situé à côté de l'exécutable et expose ses variables via `EnvService.Get(key)`.

```csharp
public static string Get(string key)
{
    // Lit .env ligne par ligne, parse KEY=VALUE, ignore les commentaires (#)
}
```

- Le chemin est construit avec `AppDomain.CurrentDomain.BaseDirectory` (même stratégie que `SettingsService`).
- Si le fichier est absent ou si la clé n'existe pas, retourne `string.Empty`.
- Utilisé dans `MainWindow` pour charger `OPENWEATHERMAP_API_KEY` au démarrage.

---

### `SettingsService.cs`

Gère la persistance de `options.json` (ville par défaut et langue uniquement).

- **`Load()`** : crée le fichier avec les valeurs par défaut s'il n'existe pas ; désérialise sinon. Un `catch` global retourne des paramètres par défaut en cas de fichier corrompu.
- **`Save(AppSettings)`** : sérialise en JSON indenté (`Formatting.Indented`) et écrase le fichier.

---

### `WeatherService.cs`

Responsable de tous les appels réseau vers OpenWeatherMap.

- **`HttpClient`** instancié une seule fois (bonne pratique), timeout 10 s.
- **`GetCurrentWeatherAsync`** : endpoint `/weather`, retourne un `WeatherData`.
- **`GetForecastAsync`** : endpoint `/forecast`, filtre les entrées à `12:00` pour les 5 prochains jours, retourne un `ForecastData`.
- **`GetWeatherIconAsync`** : télécharge l'icône `{code}@2x.png` et retourne les octets bruts (`byte[]`). La conversion en `Avalonia.Media.Imaging.Bitmap` est faite dans le code-behind.
- **`CityNotFoundException`** : exception personnalisée levée sur HTTP 404, permet de distinguer "ville introuvable" des autres erreurs réseau dans les blocs `catch` de l'UI.
- **`CapitalizeFirst`** : met en majuscule la première lettre de la description météo.

---

## 7. Interface graphique — `Views/MainWindow`

### `MainWindow.axaml` — structure XAML

La fenêtre est un `Window` Avalonia contenant un `TabControl` avec trois `TabItem`.

Chaque onglet utilise un `DockPanel` (ou `ScrollViewer`) pour organiser une barre de contrôles en haut et la zone de résultat en bas. Les contrôles nommés (`x:Name`) sont accessibles directement dans le code-behind grâce à la génération de code partielle d'Avalonia.

---

### `MainWindow.axaml.cs` — code-behind

```csharp
private readonly string _apiKey = EnvService.Get("OPENWEATHERMAP_API_KEY");
```

La clé API est chargée une seule fois au démarrage depuis `.env` et stockée dans un champ `readonly`. Elle n'est jamais écrite dans `options.json`.

Ordre d'initialisation dans le constructeur :
1. `InitializeComponent()` — génère et attache les contrôles XAML.
2. `CboLanguage.ItemsSource = _languages` — peuple la liste des langues.
3. `_settingsService.Load()` — charge `options.json`.
4. `PopulateSettingsTab()` — remplit les champs de l'onglet Paramètres.
5. `PreFillFromSettings()` — pré-remplit les champs ville si une valeur par défaut existe.

---

### Onglet Recherche

- `TxtSearchCity_KeyDown` : déclenche la recherche sur `Key.Enter`.
- `BtnSearch_Click` (`async void`) : désactive le bouton, appelle `GetCurrentWeatherAsync`, met à jour les `TextBlock`, rend `PnlSearchResult` visible, lance `LoadIconAsync` en fire-and-forget (`_ =`).
- Le bloc `finally` réactive toujours le bouton, même en cas d'erreur.

---

### Onglet Prévisions

- Même structure de barre de recherche que l'onglet Recherche.
- `BtnForecast_Click` : vide `PnlForecastColumns.Children`, appelle `GetForecastAsync`, crée dynamiquement en code-behind jusqu'à 5 `Border` (colonnes) et les ajoute au `StackPanel` horizontal.
- `CreateForecastColumn` : construit un `Border > StackPanel` avec date (`"ddd dd MMM"` en `fr-FR`), heure fixe `"12:00"`, icône, température colorée, description, humidité.

---

### Onglet Paramètres

Contient uniquement la ville par défaut et la langue (la clé API n'y est plus affichée).

- La ComboBox des langues utilise `ItemsSource` avec une liste de `LanguageItem` (record avec `Code` et `DisplayName`). `ToString()` est surchargé pour l'affichage.
- `BtnSaveSettings_Click` (`async void`) : sauvegarde `options.json`, appelle `PreFillFromSettings()`, affiche un message de confirmation pendant 2,5 secondes via `await Task.Delay(2500)`.

---

### Méthodes utilitaires

#### `LoadIconAsync(string iconCode, Image imgControl)`

```csharp
var bytes = await _weatherService.GetWeatherIconAsync(iconCode);
using var ms = new MemoryStream(bytes);
imgControl.Source = new Bitmap(ms);
```

`Bitmap` est `Avalonia.Media.Imaging.Bitmap`. L'`await` garantit que l'assignation se fait sur le thread UI. Le `catch` vide est intentionnel (l'icône est non critique).

#### `ShowError(TextBlock lbl, string message)`

Passe le `TextBlock` en rouge (`Brushes.Firebrick`) et y inscrit le message. Centralisé pour éviter la duplication.

#### `TempColor(double temp) → Color`

Expression switch C# 8+ qui associe une couleur Avalonia (`Colors.DeepSkyBlue` → `Colors.Crimson`) à une plage de températures.

---

## 8. Fichiers de configuration

### `.env` — clé API

```
OPENWEATHERMAP_API_KEY=<votre_clé>
```

- Situé dans `WeatherApp/` (projet), copié automatiquement dans `bin/Debug/net8.0/` à la compilation grâce à l'entrée `<None CopyToOutputDirectory="PreserveNewest">` dans le `.csproj`.
- **Jamais commité** : couvert par `*.env` dans `.gitignore`.
- Lu par `EnvService.Get("OPENWEATHERMAP_API_KEY")` au démarrage de la fenêtre.

### `options.json` — préférences utilisateur

```json
{
  "DefaultCity": "Paris",
  "Language": "fr"
}
```

- Créé automatiquement au premier lancement s'il n'existe pas.
- **Jamais commité** : couvert par `options.json` et `**/options.json` dans `.gitignore`.
- Ne contient plus la clé API.

---

## 9. Gestion des erreurs

| Situation | Exception / contrôle | Message affiché |
|-----------|----------------------|-----------------|
| Ville introuvable | `CityNotFoundException` | "La ville '...' est introuvable." |
| Pas de connexion | `HttpRequestException` | "Impossible de contacter le serveur…" |
| Timeout (10 s) | `TaskCanceledException` | "La requête a expiré…" |
| `.env` absent ou clé vide | Vérification manuelle | "Clé API manquante. Vérifiez votre fichier .env…" |
| Champ ville vide | Vérification manuelle | "Veuillez entrer un nom de ville." |
| Aucune prévision à 12h | Vérification manuelle | "Aucune prévision disponible…" |
| Erreur inattendue | `Exception` (générique) | "Erreur inattendue : {message}" |
| Icône non chargeable | `Exception` (silencieux) | *(aucun message — non critique)* |
| `options.json` corrompu | `Exception` dans `Load()` | Paramètres par défaut utilisés |

---

## 10. Flux de données complet

### Recherche météo

```
Utilisateur saisit une ville → clique "Rechercher" (ou Entrée)
        ↓
BtnSearch_Click (MainWindow)
        ↓
EnvService.Get("OPENWEATHERMAP_API_KEY")  [chargé au démarrage]
        ↓
WeatherService.GetCurrentWeatherAsync(city, apiKey, lang)
        ↓
HTTP GET → https://api.openweathermap.org/data/2.5/weather?q=...&units=metric&lang=fr
        ↓
Réponse JSON parsée → WeatherData
        ↓
TextBlocks mis à jour + PnlSearchResult.IsVisible = true
        ↓
WeatherService.GetWeatherIconAsync(iconCode)  [fire-and-forget]
        ↓
HTTP GET → https://openweathermap.org/img/wn/01d@2x.png  → byte[]
        ↓
new Avalonia.Media.Imaging.Bitmap(ms) → ImgWeatherIcon.Source
```

### Sauvegarde des paramètres

```
Utilisateur modifie ville/langue → clique "Enregistrer"
        ↓
BtnSaveSettings_Click (MainWindow)
        ↓
_settings.DefaultCity / Language mis à jour
        ↓
SettingsService.Save(_settings)
        ↓
JsonConvert.SerializeObject → JSON indenté
        ↓
File.WriteAllText → options.json
        ↓
PreFillFromSettings() → TxtSearchCity.Text / TxtForecastCity.Text mis à jour
        ↓
"✔ Paramètres enregistrés." affiché 2,5 s (await Task.Delay)
```

---

## 11. Dépendances externes

| Dépendance | Version | Usage |
|------------|---------|-------|
| `Avalonia` | 11.2.1 | Framework UI multiplateforme (core) |
| `Avalonia.Desktop` | 11.2.1 | Cycle de vie application desktop |
| `Avalonia.Themes.Fluent` | 11.2.1 | Thème visuel Fluent Design |
| `Avalonia.Fonts.Inter` | 11.2.1 | Police Inter intégrée |
| `Newtonsoft.Json` | 13.0.3 | Sérialisation de `options.json` + parsing des réponses API |
| `System.Net.Http.HttpClient` | Intégré .NET | Requêtes HTTP vers OpenWeatherMap |
| API OpenWeatherMap `/weather` | v2.5 | Météo actuelle |
| API OpenWeatherMap `/forecast` | v2.5 | Prévisions 5 jours / 3 heures |
| OpenWeatherMap icons CDN | — | Images météo `img/wn/{code}@2x.png` |
