using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

namespace StardewPhone.PhoneUI.Apps
{
    /// <summary>
    /// App de Clima — combina o clima do Stardew com dados reais via Open-Meteo (API gratuita, sem chave).
    /// Open-Meteo: https://open-meteo.com/ — sem registro necessário!
    /// Coordenadas padrão: São Paulo, Brasil (configurável no manifest.json no futuro).
    /// </summary>
    public class WeatherApp : AppBase
    {
        // Dados da API
        private string _temperatura   = "--";
        private string _descricao     = "Carregando...";
        private string _umidade       = "--";
        private string _vento         = "--";
        private bool   _carregando    = false;
        private bool   _erroApi       = false;
        private DateTime _ultimaAtualizacao = DateTime.MinValue;

        // Coordenadas padrão (São Paulo) — pode ser configurado no manifest
        private const double Lat = -23.55;
        private const double Lon = -46.63;

        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        public WeatherApp(IModHelper helper, IMonitor monitor) : base(helper, monitor)
        {
            try { Icon = helper.ModContent.Load<Texture2D>("assets/sprites/icon_weather.png"); }
            catch { }
        }

        public override void OnOpen()
        {
            // Atualiza dados se passaram mais de 10 minutos
            if ((DateTime.Now - _ultimaAtualizacao).TotalMinutes > 10)
                _ = BuscarClimaAsync();
        }

        private async Task BuscarClimaAsync()
        {
            if (_carregando) return;
            _carregando = true;
            _erroApi = false;

            try
            {
                // API gratuita Open-Meteo (sem chave de API!)
                string url = $"https://api.open-meteo.com/v1/forecast" +
                             $"?latitude={Lat}&longitude={Lon}" +
                             $"&current=temperature_2m,relative_humidity_2m,wind_speed_10m,weather_code" +
                             $"&wind_speed_unit=kmh&timezone=America%2FSao_Paulo";

                string json = await Http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);

                var current = doc.RootElement.GetProperty("current");
                double temp  = current.GetProperty("temperature_2m").GetDouble();
                int    hum   = current.GetProperty("relative_humidity_2m").GetInt32();
                double wind  = current.GetProperty("wind_speed_10m").GetDouble();
                int    code  = current.GetProperty("weather_code").GetInt32();

                _temperatura = $"{temp:F1}°C";
                _umidade     = $"{hum}%";
                _vento       = $"{wind:F0} km/h";
                _descricao   = InterpretarWMO(code);
                _ultimaAtualizacao = DateTime.Now;
            }
            catch (Exception ex)
            {
                _descricao = "Sem conexão";
                _erroApi   = true;
                Monitor.Log($"WeatherApp erro: {ex.Message}", LogLevel.Warn);
            }
            finally
            {
                _carregando = false;
            }
        }

        public override void Draw(SpriteBatch sb, Rectangle screen, Action goBack)
        {
            // Gradiente de fundo azul/noite
            DrawRect(sb, screen, new Color(10, 25, 60));
            DrawTitleBar(sb, screen, "🌤 Clima", goBack, new Color(15, 45, 100));

            int titleH = S(26);
            int cx = screen.X + screen.Width / 2;
            int sy = screen.Y + titleH + S(12);

            // --- Clima do Stardew (sempre disponível) ---
            DrawText(sb, "☘ Pelican Town", screen.X + S(8), sy, Color.LightSkyBlue, 0.5f);
            sy += S(18);

            var sdCard = new Rectangle(screen.X + S(6), sy, screen.Width - S(12), S(56));
            DrawRoundRect(sb, sdCard, new Color(20, 40, 90));

            string sdClima = GetStardewWeather();
            string sdEmoji = GetStardewEmoji();
            DrawCenteredText(sb, sdEmoji, sdCard with { Height = S(30) }, Color.White, 0.85f);
            DrawCenteredText(sb, sdClima, sdCard with { Y = sdCard.Y + S(30), Height = S(26) }, Color.LightBlue, 0.5f);

            // Clima amanhã
            string amanha = GetStardewWeatherAmanha();
            DrawText(sb, $"Amanhã: {amanha}", screen.X + S(8), sy + S(60), Color.Gray, 0.42f);

            sy += S(82);

            // Divisor
            DrawRect(sb, new Rectangle(screen.X + S(8), sy, screen.Width - S(16), 1), Color.DimGray);
            sy += S(6);

            // --- Clima Real (Open-Meteo) ---
            DrawText(sb, "🌎 Tempo Real", screen.X + S(8), sy, Color.LightGreen, 0.5f);
            sy += S(18);

            if (_carregando)
            {
                DrawCenteredText(sb, "Carregando...", screen with { Y = sy, Height = S(40) }, Color.Gray, 0.5f);
            }
            else if (_erroApi)
            {
                DrawCenteredText(sb, "Offline", screen with { Y = sy, Height = S(20) }, Color.OrangeRed, 0.5f);
                sy += S(24);
                if (DrawButton(sb, new Rectangle(screen.X + S(20), sy, screen.Width - S(40), S(26)), "↻ Tentar novamente", new Color(30, 60, 120)))
                    _ = BuscarClimaAsync();
            }
            else
            {
                var realCard = new Rectangle(screen.X + S(6), sy, screen.Width - S(12), S(64));
                DrawRoundRect(sb, realCard, new Color(15, 30, 70));

                // Temperatura em destaque
                float tempScale = 0.85f;
                Vector2 tempSize = Game1.smallFont.MeasureString(_temperatura) * tempScale;
                sb.DrawString(Game1.smallFont, _temperatura,
                    new Vector2(realCard.X + S(8), realCard.Y + S(8)),
                    Color.White, 0f, Vector2.Zero, tempScale, SpriteEffects.None, 0f);

                // Descrição
                DrawText(sb, _descricao, realCard.X + S(8), realCard.Y + S(36), Color.LightBlue, 0.45f);

                // Detalhes laterais
                DrawText(sb, $"💧 {_umidade}", realCard.Right - S(72), realCard.Y + S(8),  Color.DeepSkyBlue, 0.45f);
                DrawText(sb, $"💨 {_vento}",  realCard.Right - S(72), realCard.Y + S(28), Color.SkyBlue, 0.45f);

                // Horário da última atualização
                sy += S(68);
                DrawText(sb, $"Atualizado: {_ultimaAtualizacao:HH:mm}", screen.X + S(8), sy, Color.DimGray, 0.38f);
                sy += S(18);

                // Botão atualizar
                if (DrawButton(sb, new Rectangle(screen.X + S(20), sy, screen.Width - S(40), S(24)), "↻ Atualizar", new Color(20, 50, 100), 0.45f))
                    _ = BuscarClimaAsync();
            }
        }

        private string GetStardewWeather()
        {
            if (Game1.isRaining) return Game1.isLightning ? "Tempestade" : "Chovendo";
            if (Game1.isSnowing) return "Nevando";
            if (Game1.isDebrisWeather) return "Ventando";
            return "Ensolarado";
        }

        private string GetStardewEmoji()
        {
            if (Game1.isRaining) return Game1.isLightning ? "⛈" : "🌧";
            if (Game1.isSnowing) return "❄️";
            if (Game1.isDebrisWeather) return "💨";
            return "☀️";
        }

        private string GetStardewWeatherAmanha()
        {
            return Game1.weatherForTomorrow switch
            {
                Game1.weather_rain      => "🌧 Chuva",
                Game1.weather_lightning => "⛈ Tempestade",
                Game1.weather_snow      => "❄️ Neve",
                Game1.weather_debris    => "💨 Vento",
                _                       => "☀️ Sol"
            };
        }

        // Interpreta código WMO da API Open-Meteo
        private string InterpretarWMO(int code) => code switch
        {
            0                   => "☀️ Céu limpo",
            1 or 2 or 3         => "⛅ Parcialmente nublado",
            45 or 48            => "🌫 Neblina",
            51 or 53 or 55      => "🌦 Chuvisco",
            61 or 63 or 65      => "🌧 Chuva",
            71 or 73 or 75      => "❄️ Neve",
            80 or 81 or 82      => "🌧 Pancadas",
            95                  => "⛈ Tempestade",
            96 or 99            => "⛈ Tempestade com granizo",
            _                   => "🌤 Variável"
        };
    }
}
