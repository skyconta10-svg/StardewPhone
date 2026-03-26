using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewPhone.PhoneUI.Apps;
using System;
using System.Collections.Generic;

namespace StardewPhone.PhoneUI
{
    /// <summary>
    /// Tela inicial do celular com grade de ícones de apps.
    /// </summary>
    public class HomeScreen
    {
        private readonly Dictionary<string, AppBase> _apps;

        // IDs e labels dos apps na ordem de exibição
        private readonly (string id, string label)[] _appOrder = new[]
        {
            ("chat",    "Chat"),
            ("pix",     "Pix"),
            ("calc",    "Calc"),
            ("todo",    "Lembretes"),
            ("weather", "Clima"),
        };

        // Informações de hora/data
        private string ClockText => $"{Game1.timeOfDay / 100:D2}:{Game1.timeOfDay % 100:D2}";
        private string DateText  => $"Dia {Game1.dayOfMonth} - {GetSeasonName()}";

        public HomeScreen(Dictionary<string, AppBase> apps)
        {
            _apps = apps;
        }

        public void Reset() { }

        /// <summary>
        /// Desenha a home screen dentro da área 'screen'.
        /// Chama 'openApp' ao clicar em um ícone.
        /// </summary>
        public void Draw(SpriteBatch sb, Rectangle screen, Action<string> openApp)
        {
            // --- Status bar: hora e data ---
            DrawStatusBar(sb, screen);

            // --- Grade de ícones ---
            DrawAppGrid(sb, screen, openApp);
        }

        private void DrawStatusBar(SpriteBatch sb, Rectangle screen)
        {
            // Fundo da status bar
            var barRect = new Rectangle(screen.X, screen.Y, screen.Width, PhoneOverlay.Scale(24));
            DrawRect(sb, barRect, new Color(10, 12, 30, 200));

            // Hora
            sb.DrawString(Game1.smallFont, ClockText,
                new Vector2(screen.X + PhoneOverlay.Scale(4), screen.Y + PhoneOverlay.Scale(2)),
                Color.White, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);

            // Data
            Vector2 dateSize = Game1.smallFont.MeasureString(DateText) * 0.5f;
            sb.DrawString(Game1.smallFont, DateText,
                new Vector2(screen.Right - dateSize.X - PhoneOverlay.Scale(4), screen.Y + PhoneOverlay.Scale(2)),
                Color.LightGray, 0f, Vector2.Zero, 0.50f, SpriteEffects.None, 0f);
        }

        private void DrawAppGrid(SpriteBatch sb, Rectangle screen, Action<string> openApp)
        {
            int cols      = 2;
            int padding   = PhoneOverlay.Scale(10);
            int iconSize  = (screen.Width - padding * (cols + 1)) / cols;
            iconSize      = Math.Min(iconSize, PhoneOverlay.Scale(64));

            int startY = screen.Y + PhoneOverlay.Scale(30);
            var mousePos = Game1.getMousePosition();
            bool mouseDown = Game1.input.GetMouseState().LeftButton ==
                             Microsoft.Xna.Framework.Input.ButtonState.Pressed;

            for (int i = 0; i < _appOrder.Length; i++)
            {
                int col = i % cols;
                int row = i / cols;

                int x = screen.X + padding + col * (iconSize + padding);
                int y = startY + row * (iconSize + PhoneOverlay.Scale(28));

                var iconRect = new Rectangle(x, y, iconSize, iconSize);

                // Fundo do ícone (cor por app)
                bool hovered = iconRect.Contains(mousePos);
                Color bgColor = GetAppColor(_appOrder[i].id);
                if (hovered) bgColor = Color.Lerp(bgColor, Color.White, 0.25f);

                DrawRoundRect(sb, iconRect, bgColor);

                // Textura do ícone (se disponível)
                var texture = _apps[_appOrder[i].id].Icon;
                if (texture != null)
                {
                    int pad = iconSize / 5;
                    sb.Draw(texture, new Rectangle(x + pad, y + pad, iconSize - pad * 2, iconSize - pad * 2), Color.White);
                }
                else
                {
                    // Letra inicial como fallback
                    string letter = _appOrder[i].label[0].ToString();
                    Vector2 lSize = Game1.dialogueFont.MeasureString(letter) * 0.6f;
                    sb.DrawString(Game1.dialogueFont, letter,
                        new Vector2(x + (iconSize - lSize.X) / 2, y + (iconSize - lSize.Y) / 2),
                        Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                }

                // Notificação badge
                int notifCount = ModEntry.Notifications.GetUnreadCount(_appOrder[i].id);
                if (notifCount > 0)
                    DrawBadge(sb, new Rectangle(x + iconSize - PhoneOverlay.Scale(14), y, PhoneOverlay.Scale(16), PhoneOverlay.Scale(16)), notifCount);

                // Label abaixo do ícone
                Vector2 labelSize = Game1.smallFont.MeasureString(_appOrder[i].label) * 0.5f;
                sb.DrawString(Game1.smallFont, _appOrder[i].label,
                    new Vector2(x + (iconSize - labelSize.X) / 2, y + iconSize + PhoneOverlay.Scale(3)),
                    Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);

                // Clique
                if (hovered && mouseDown)
                    openApp(_appOrder[i].id);
            }
        }

        private void DrawBadge(SpriteBatch sb, Rectangle rect, int count)
        {
            DrawRect(sb, rect, Color.Red);
            string txt = count > 9 ? "9+" : count.ToString();
            Vector2 ts = Game1.smallFont.MeasureString(txt) * 0.4f;
            sb.DrawString(Game1.smallFont, txt,
                new Vector2(rect.X + (rect.Width - ts.X) / 2, rect.Y + (rect.Height - ts.Y) / 2),
                Color.White, 0f, Vector2.Zero, 0.4f, SpriteEffects.None, 0f);
        }

        private Color GetAppColor(string appId) => appId switch
        {
            "chat"    => new Color(0, 120, 215),
            "pix"     => new Color(0, 150, 100),
            "calc"    => new Color(100, 50, 150),
            "todo"    => new Color(200, 100, 0),
            "weather" => new Color(30, 100, 180),
            _         => new Color(60, 60, 80),
        };

        private string GetSeasonName() => Game1.currentSeason switch
        {
            "spring" => "Primavera",
            "summer" => "Verão",
            "fall"   => "Outono",
            "winter" => "Inverno",
            _        => Game1.currentSeason
        };

        private static Texture2D? _pixel;
        private void DrawRect(SpriteBatch sb, Rectangle r, Color c)
        {
            if (_pixel == null)
            {
                _pixel = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
            }
            sb.Draw(_pixel, r, c);
        }

        // Simula bordas arredondadas usando 3 retângulos sobrepostos
        private void DrawRoundRect(SpriteBatch sb, Rectangle r, Color c)
        {
            int rad = PhoneOverlay.Scale(8);
            DrawRect(sb, new Rectangle(r.X + rad, r.Y, r.Width - rad * 2, r.Height), c);
            DrawRect(sb, new Rectangle(r.X, r.Y + rad, r.Width, r.Height - rad * 2), c);
        }
    }
}
