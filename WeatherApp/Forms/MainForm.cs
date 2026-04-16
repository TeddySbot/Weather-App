using WeatherApp.Models;
using WeatherApp.Services;

namespace WeatherApp.Forms
{
    public partial class MainForm : Form
    {
        private readonly WeatherService _weatherService = new();
        private readonly SettingsService _settingsService = new();
        private AppSettings _settings = new();

        // --- Controls ---
        private TabControl tabControl = null!;

        // Tab 1 – Search
        private TabPage tabSearch = null!;
        private TextBox txtSearchCity = null!;
        private Button btnSearch = null!;
        private Label lblSearchStatus = null!;
        private Panel pnlSearchResult = null!;
        private Label lblCityName = null!;
        private Label lblLatLon = null!;
        private Label lblTemp = null!;
        private Label lblDesc = null!;
        private Label lblHumidity = null!;
        private PictureBox pbWeatherIcon = null!;

        // Tab 2 – Forecast
        private TabPage tabForecast = null!;
        private TextBox txtForecastCity = null!;
        private Button btnForecast = null!;
        private Label lblForecastStatus = null!;
        private Panel pnlForecastColumns = null!;

        // Tab 3 – Settings
        private TabPage tabSettings = null!;
        private TextBox txtDefaultCity = null!;
        private ComboBox cboLanguage = null!;
        private TextBox txtApiKey = null!;
        private Button btnSaveSettings = null!;
        private Label lblSettingsSaved = null!;

        public MainForm()
        {
            InitializeComponent();
            _settings = _settingsService.Load();
            PopulateSettingsTab();
            PreFillFromSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "Weather App";
            this.Size = new Size(1000, 650);
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5f);
            this.BackColor = Color.FromArgb(240, 244, 250);

            tabControl = new TabControl { Dock = DockStyle.Fill };
            tabControl.Font = new Font("Segoe UI", 10f, FontStyle.Bold);

            BuildSearchTab();
            BuildForecastTab();
            BuildSettingsTab();

            tabControl.TabPages.AddRange(new[] { tabSearch, tabForecast, tabSettings });
            this.Controls.Add(tabControl);
        }

        // =====================================================================
        // TAB 1 – SEARCH
        // =====================================================================
        private void BuildSearchTab()
        {
            tabSearch = new TabPage("🔍  Recherche");

            var topPanel = new Panel
            {
                Height = 60,
                Dock = DockStyle.Top,
                Padding = new Padding(16, 12, 16, 0)
            };

            txtSearchCity = new TextBox
            {
                PlaceholderText = "Nom de la ville…",
                Width = 280,
                Height = 32,
                Location = new Point(16, 14),
                Font = new Font("Segoe UI", 10f)
            };
            txtSearchCity.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnSearch.PerformClick(); };

            btnSearch = CreateButton("Rechercher", new Point(308, 13));
            btnSearch.Click += BtnSearch_Click;

            lblSearchStatus = new Label
            {
                AutoSize = true,
                Location = new Point(16, 45),
                ForeColor = Color.Firebrick,
                Font = new Font("Segoe UI", 9f)
            };

            topPanel.Controls.AddRange(new Control[] { txtSearchCity, btnSearch, lblSearchStatus });

            pnlSearchResult = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16),
                Visible = false
            };

            var card = CreateCard(new Point(0, 0), new Size(500, 280));

            pbWeatherIcon = new PictureBox
            {
                Size = new Size(80, 80),
                Location = new Point(380, 20),
                SizeMode = PictureBoxSizeMode.Zoom
            };

            lblCityName = CreateResultLabel(bold: true, fontSize: 16);
            lblCityName.Location = new Point(16, 16);

            lblLatLon   = CreateResultLabel(); lblLatLon.Location   = new Point(16, 56);
            lblTemp     = CreateResultLabel(fontSize: 22, bold: true); lblTemp.Location     = new Point(16, 90);
            lblDesc     = CreateResultLabel(); lblDesc.Location     = new Point(16, 130);
            lblHumidity = CreateResultLabel(); lblHumidity.Location = new Point(16, 162);

            card.Controls.AddRange(new Control[] { pbWeatherIcon, lblCityName, lblLatLon, lblTemp, lblDesc, lblHumidity });
            pnlSearchResult.Controls.Add(card);

            tabSearch.Controls.AddRange(new Control[] { pnlSearchResult, topPanel });
        }

        private async void BtnSearch_Click(object? sender, EventArgs e)
        {
            var city = txtSearchCity.Text.Trim();
            if (string.IsNullOrEmpty(city)) { lblSearchStatus.Text = "Veuillez entrer un nom de ville."; return; }
            if (string.IsNullOrEmpty(_settings.ApiKey)) { lblSearchStatus.Text = "Clé API manquante. Configurez-la dans les paramètres."; return; }

            lblSearchStatus.Text = "Chargement…";
            lblSearchStatus.ForeColor = Color.Gray;
            btnSearch.Enabled = false;
            pnlSearchResult.Visible = false;

            try
            {
                var data = await _weatherService.GetCurrentWeatherAsync(city, _settings.ApiKey, _settings.Language);
                lblCityName.Text = data.CityName;
                lblLatLon.Text   = $"📍 Lat : {data.Latitude:F4}  |  Lon : {data.Longitude:F4}";
                lblTemp.Text     = $"{data.Temperature:F1} °C";
                lblDesc.Text     = $"☁  {data.Description}";
                lblHumidity.Text = $"💧 Humidité : {data.Humidity} %";

                lblSearchStatus.Text = string.Empty;
                pnlSearchResult.Visible = true;

                _ = LoadIconAsync(data.IconCode, pbWeatherIcon);
            }
            catch (CityNotFoundException ex) { ShowError(lblSearchStatus, ex.Message); }
            catch (HttpRequestException) { ShowError(lblSearchStatus, "Impossible de contacter le serveur. Vérifiez votre connexion internet."); }
            catch (TaskCanceledException) { ShowError(lblSearchStatus, "La requête a expiré. Vérifiez votre connexion internet."); }
            catch (Exception ex) { ShowError(lblSearchStatus, $"Erreur inattendue : {ex.Message}"); }
            finally { btnSearch.Enabled = true; }
        }

        // =====================================================================
        // TAB 2 – FORECAST
        // =====================================================================
        private void BuildForecastTab()
        {
            tabForecast = new TabPage("📅  Prévisions");

            var topPanel = new Panel
            {
                Height = 60,
                Dock = DockStyle.Top,
                Padding = new Padding(16, 12, 16, 0)
            };

            txtForecastCity = new TextBox
            {
                PlaceholderText = "Nom de la ville…",
                Width = 280,
                Height = 32,
                Location = new Point(16, 14),
                Font = new Font("Segoe UI", 10f)
            };
            txtForecastCity.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnForecast.PerformClick(); };

            btnForecast = CreateButton("Voir les prévisions", new Point(308, 13));
            btnForecast.Click += BtnForecast_Click;

            lblForecastStatus = new Label
            {
                AutoSize = true,
                Location = new Point(16, 45),
                ForeColor = Color.Firebrick,
                Font = new Font("Segoe UI", 9f)
            };

            topPanel.Controls.AddRange(new Control[] { txtForecastCity, btnForecast, lblForecastStatus });

            pnlForecastColumns = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            tabForecast.Controls.AddRange(new Control[] { pnlForecastColumns, topPanel });
        }

        private async void BtnForecast_Click(object? sender, EventArgs e)
        {
            var city = txtForecastCity.Text.Trim();
            if (string.IsNullOrEmpty(city)) { lblForecastStatus.Text = "Veuillez entrer un nom de ville."; return; }
            if (string.IsNullOrEmpty(_settings.ApiKey)) { lblForecastStatus.Text = "Clé API manquante. Configurez-la dans les paramètres."; return; }

            lblForecastStatus.Text = "Chargement…";
            lblForecastStatus.ForeColor = Color.Gray;
            btnForecast.Enabled = false;
            pnlForecastColumns.Controls.Clear();

            try
            {
                var forecast = await _weatherService.GetForecastAsync(city, _settings.ApiKey, _settings.Language);

                if (forecast.Forecasts.Count == 0)
                {
                    ShowError(lblForecastStatus, "Aucune prévision disponible pour les prochains jours à 12h00.");
                    return;
                }

                lblForecastStatus.Text = $"{forecast.CityName}  —  📍 {forecast.Latitude:F4}, {forecast.Longitude:F4}";
                lblForecastStatus.ForeColor = Color.FromArgb(50, 80, 120);

                int colWidth = 170;
                int colHeight = 310;
                int startX = 16;
                int startY = 12;

                for (int i = 0; i < forecast.Forecasts.Count && i < 5; i++)
                {
                    var day = forecast.Forecasts[i];
                    var col = CreateForecastColumn(day, new Point(startX + i * (colWidth + 12), startY), new Size(colWidth, colHeight));
                    pnlForecastColumns.Controls.Add(col);
                }
            }
            catch (CityNotFoundException ex) { ShowError(lblForecastStatus, ex.Message); }
            catch (HttpRequestException) { ShowError(lblForecastStatus, "Impossible de contacter le serveur. Vérifiez votre connexion internet."); }
            catch (TaskCanceledException) { ShowError(lblForecastStatus, "La requête a expiré. Vérifiez votre connexion internet."); }
            catch (Exception ex) { ShowError(lblForecastStatus, $"Erreur inattendue : {ex.Message}"); }
            finally { btnForecast.Enabled = true; }
        }

        private Panel CreateForecastColumn(WeatherData data, Point location, Size size)
        {
            var card = CreateCard(location, size);

            var dateLabel = new Label
            {
                Text = data.DateTime.ToString("ddd dd MMM", new System.Globalization.CultureInfo("fr-FR")),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 60, 110),
                AutoSize = false,
                Width = size.Width - 16,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(8, 8),
                Height = 22
            };

            var timeLabel = new Label
            {
                Text = "12:00",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.Gray,
                AutoSize = false,
                Width = size.Width - 16,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(8, 30),
                Height = 18
            };

            var pb = new PictureBox
            {
                Size = new Size(64, 64),
                Location = new Point((size.Width - 64) / 2, 52),
                SizeMode = PictureBoxSizeMode.Zoom
            };

            var tempLabel = new Label
            {
                Text = $"{data.Temperature:F1} °C",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = TempColor(data.Temperature),
                AutoSize = false,
                Width = size.Width - 16,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(8, 122),
                Height = 28
            };

            var descLabel = new Label
            {
                Text = data.Description,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(60, 80, 100),
                AutoSize = false,
                Width = size.Width - 16,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(8, 152),
                Height = 40
            };

            var humLabel = new Label
            {
                Text = $"💧 {data.Humidity} %",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.SteelBlue,
                AutoSize = false,
                Width = size.Width - 16,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(8, 196),
                Height = 22
            };

            card.Controls.AddRange(new Control[] { dateLabel, timeLabel, pb, tempLabel, descLabel, humLabel });

            _ = LoadIconAsync(data.IconCode, pb);

            return card;
        }

        // =====================================================================
        // TAB 3 – SETTINGS
        // =====================================================================
        private void BuildSettingsTab()
        {
            tabSettings = new TabPage("⚙  Paramètres");

            var card = CreateCard(new Point(20, 20), new Size(480, 340));
            card.Anchor = AnchorStyles.None;

            var title = new Label
            {
                Text = "Paramètres de l'application",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 60, 110),
                AutoSize = true,
                Location = new Point(16, 16)
            };

            var lblApi = CreateSettingsLabel("Clé API OpenWeatherMap :", new Point(16, 60));
            txtApiKey = new TextBox
            {
                Location = new Point(16, 82),
                Width = 420,
                Font = new Font("Segoe UI", 10f),
                UseSystemPasswordChar = false
            };

            var lblCity = CreateSettingsLabel("Ville par défaut :", new Point(16, 122));
            txtDefaultCity = new TextBox
            {
                Location = new Point(16, 144),
                Width = 420,
                Font = new Font("Segoe UI", 10f)
            };

            var lblLang = CreateSettingsLabel("Langue des descriptions météo :", new Point(16, 184));
            cboLanguage = new ComboBox
            {
                Location = new Point(16, 206),
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10f)
            };
            PopulateLanguages();

            btnSaveSettings = CreateButton("💾  Enregistrer", new Point(16, 262));
            btnSaveSettings.Click += BtnSaveSettings_Click;

            lblSettingsSaved = new Label
            {
                AutoSize = true,
                Location = new Point(170, 268),
                ForeColor = Color.SeaGreen,
                Font = new Font("Segoe UI", 9f)
            };

            card.Controls.AddRange(new Control[]
            {
                title, lblApi, txtApiKey,
                lblCity, txtDefaultCity,
                lblLang, cboLanguage,
                btnSaveSettings, lblSettingsSaved
            });

            tabSettings.Controls.Add(card);
        }

        private void PopulateLanguages()
        {
            var languages = new[]
            {
                ("af", "Afrikaans"), ("al", "Albanian"), ("ar", "Arabic"),
                ("az", "Azerbaijani"), ("bg", "Bulgarian"), ("ca", "Catalan"),
                ("cz", "Czech"), ("da", "Danish"), ("de", "German"),
                ("el", "Greek"), ("en", "English"), ("eu", "Basque"),
                ("fa", "Persian"), ("fi", "Finnish"), ("fr", "French"),
                ("gl", "Galician"), ("he", "Hebrew"), ("hi", "Hindi"),
                ("hr", "Croatian"), ("hu", "Hungarian"), ("id", "Indonesian"),
                ("it", "Italian"), ("ja", "Japanese"), ("kr", "Korean"),
                ("la", "Latvian"), ("lt", "Lithuanian"), ("mk", "Macedonian"),
                ("no", "Norwegian"), ("nl", "Dutch"), ("pl", "Polish"),
                ("pt", "Portuguese"), ("pt_br", "Português Brasil"),
                ("ro", "Romanian"), ("ru", "Russian"), ("sv", "Swedish"),
                ("sk", "Slovak"), ("sl", "Slovenian"), ("sp", "Spanish"),
                ("sr", "Serbian"), ("th", "Thai"), ("tr", "Turkish"),
                ("ua", "Ukrainian"), ("vi", "Vietnamese"), ("zh_cn", "Chinese Simplified"),
                ("zh_tw", "Chinese Traditional"), ("zu", "Zulu")
            };

            cboLanguage.DisplayMember = "Item2";
            cboLanguage.ValueMember = "Item1";
            foreach (var (code, name) in languages)
                cboLanguage.Items.Add(new { Item1 = code, Item2 = $"{name} ({code})" });
        }

        private void PopulateSettingsTab()
        {
            txtApiKey.Text = _settings.ApiKey;
            txtDefaultCity.Text = _settings.DefaultCity;

            for (int i = 0; i < cboLanguage.Items.Count; i++)
            {
                dynamic item = cboLanguage.Items[i]!;
                if (item.Item1 == _settings.Language)
                {
                    cboLanguage.SelectedIndex = i;
                    break;
                }
            }
            if (cboLanguage.SelectedIndex < 0) cboLanguage.SelectedIndex = 14; // fr
        }

        private void PreFillFromSettings()
        {
            if (!string.IsNullOrEmpty(_settings.DefaultCity))
            {
                txtSearchCity.Text = _settings.DefaultCity;
                txtForecastCity.Text = _settings.DefaultCity;
            }
        }

        private void BtnSaveSettings_Click(object? sender, EventArgs e)
        {
            dynamic? selected = cboLanguage.SelectedItem;
            _settings.ApiKey = txtApiKey.Text.Trim();
            _settings.DefaultCity = txtDefaultCity.Text.Trim();
            _settings.Language = selected?.Item1 ?? "fr";
            _settingsService.Save(_settings);

            PreFillFromSettings();
            lblSettingsSaved.Text = "✔ Paramètres enregistrés.";
            Task.Delay(2500).ContinueWith(_ => lblSettingsSaved.Invoke(() => lblSettingsSaved.Text = ""));
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        private async Task LoadIconAsync(string iconCode, PictureBox pb)
        {
            try
            {
                var img = await _weatherService.GetWeatherIconAsync(iconCode);
                if (!pb.IsDisposed)
                    pb.Invoke(() => pb.Image = img);
            }
            catch { /* icon non critique */ }
        }

        private static Panel CreateCard(Point location, Size size)
        {
            return new Panel
            {
                Location = location,
                Size = size,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(8)
            };
        }

        private static Button CreateButton(string text, Point location)
        {
            return new Button
            {
                Text = text,
                Location = location,
                Height = 32,
                AutoSize = true,
                Padding = new Padding(10, 0, 10, 0),
                BackColor = Color.FromArgb(30, 100, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
        }

        private static Label CreateResultLabel(bool bold = false, float fontSize = 10.5f)
        {
            return new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", fontSize, bold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 50, 80)
            };
        }

        private static Label CreateSettingsLabel(string text, Point location)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Location = location,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 80, 100)
            };
        }

        private static void ShowError(Label lbl, string message)
        {
            lbl.ForeColor = Color.Firebrick;
            lbl.Text = message;
        }

        private static Color TempColor(double temp) => temp switch
        {
            < 0   => Color.DeepSkyBlue,
            < 10  => Color.CornflowerBlue,
            < 20  => Color.ForestGreen,
            < 28  => Color.DarkOrange,
            _     => Color.Crimson
        };
    }
}
