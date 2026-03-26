using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Collections.Generic;

namespace StardewPhone.Notifications
{
    /// <summary>
    /// Gerencia notificações do StardewPhone.
    /// Exibe popups temporários no canto da tela e controla badges nos ícones.
    /// </summary>
    public class NotificationManager
    {
        // Notificações não lidas por app (para badge nos ícones)
        private readonly Dictionary<string, int> _unread = new();

        // Popups temporários na tela
        private readonly List<NotifPopup> _popups = new();
        private const int MaxPopups = 3;
        private const int PopupDurationMs = 4000; // 4 segundos

        // Pixel 1x1 para retângulos
        private static Texture2D? _pixel;

        public NotificationManager() { }

        /// <summary>Carrega dados de notificações do save (implementar se necessário).</summary>
        public void LoadData() { }

        /// <summary>Verifica notificações de início de dia (lembretes).</summary>
        public void CheckDailyNotifications()
        {
            // Checa lembretes com dia marcado
            foreach (var item in StardewPhone.PhoneUI.Apps.TodoApp.Items)
            {
                if (!item.Done
                    && item.DiaLembrete == Game1.dayOfMonth
                    && item.Estacao == Game1.currentSeason)
                {
                    AddNotification("todo", $"📋 Lembrete: {item.Text}");
                }
            }
        }

        /// <summary>Adiciona uma notificação para um app específico.</summary>
        public void AddNotification(string appId, string text)
        {
            // Incrementa badge
            if (!_unread.ContainsKey(appId))
                _unread[appId] = 0;
            _unread[appId]++;

            // Adiciona popup
            if (_popups.Count >= MaxPopups)
                _popups.RemoveAt(0);

            _popups.Add(new NotifPopup
            {
                AppId     = appId,
                Text      = text.Length > 35 ? text[..35] + "…" : text,
                CreatedAt = DateTime.Now,
                Progress  = 1f
            });
        }

        /// <summary>Retorna quantidade de notificações não lidas para um app.</summary>
        public int GetUnreadCount(string appId)
            => _unread.TryGetValue(appId, out int v) ? v : 0;

        /// <summary>Limpa notificações não lidas de um app (ao abrir o app).</summary>
        public void ClearUnread(string appId)
        {
            if (_unread.ContainsKey(appId))
                _unread[appId] = 0;
        }

        /// <summary>Desenha popups de notificação no canto superior direito.</summary>
        public void DrawPopups(SpriteBatch sb)
        {
            EnsurePixel();

            int screenW = Game1.uiViewport.Width;
            int popupW  = Scale(200);
            int popupH  = Scale(44);
            int spacing = Scale(6);
            int baseX   = screenW - popupW - Scale(8);
            int baseY   = Scale(8);

            var agora = DateTime.Now;
            for (int i = _popups.Count - 1; i >= 0; i--)
            {
                var p = _popups[i];
                double elapsed = (agora - p.CreatedAt).TotalMilliseconds;

                if (elapsed > PopupDurationMs)
                { _popups.RemoveAt(i); continue; }

                // Fade out nos últimos 800ms
                float alpha = elapsed > PopupDurationMs - 800
                    ? 1f - (float)((elapsed - (PopupDurationMs - 800)) / 800.0)
                    : 1f;

                int idx = _popups.Count - 1 - i;
                var rect = new Rectangle(baseX, baseY + idx * (popupH + spacing), popupW, popupH);

                // Fundo do popup
                DrawRect(sb, rect, new Color(15, 20, 40, (int)(220 * alpha)));

                // Barra colorida lateral por app
                Color accent = GetAppAccent(p.AppId);
                DrawRect(sb, new Rectangle(rect.X, rect.Y, Scale(4), rect.Height),
                    accent * alpha);

                // Ícone do app
                DrawRect(sb, new Rectangle(rect.X + Scale(8), rect.Y + Scale(8), Scale(28), Scale(28)),
                    accent * 0.6f * alpha);

                string appLabel = GetAppLabel(p.AppId);
                Vector2 lSize = Game1.smallFont.MeasureString(appLabel) * 0.38f;
                sb.DrawString(Game1.smallFont, appLabel,
                    new Vector2(rect.X + Scale(8) + (Scale(28) - lSize.X) / 2,
                                rect.Y + Scale(8) + (Scale(28) - lSize.Y) / 2),
                    Color.White * alpha, 0f, Vector2.Zero, 0.38f, SpriteEffects.None, 0f);

                // Texto
                sb.DrawString(Game1.smallFont, p.Text,
                    new Vector2(rect.X + Scale(44), rect.Y + Scale(12)),
                    Color.White * alpha, 0f, Vector2.Zero, 0.42f, SpriteEffects.None, 0f);

                // Barra de progresso na base
                float progress = 1f - (float)(elapsed / PopupDurationMs);
                DrawRect(sb, new Rectangle(rect.X, rect.Bottom - Scale(3), (int)(rect.Width * progress), Scale(3)),
                    accent * alpha);
            }
        }

        private Color GetAppAccent(string appId) => appId switch
        {
            "chat"    => new Color(0, 120, 215),
            "pix"     => new Color(0, 180, 100),
            "todo"    => new Color(220, 110, 0),
            "weather" => new Color(50, 140, 220),
            _         => new Color(100, 100, 130),
        };

        private string GetAppLabel(string appId) => appId switch
        {
            "chat"    => "💬",
            "pix"     => "💚",
            "todo"    => "📋",
            "weather" => "🌤",
            _         => "📱",
        };

        private static int Scale(int v)
        {
            float r = Game1.uiViewport.Width / 1280f;
            return (int)Microsoft.Xna.Framework.MathHelper.Clamp(v * r, v * 0.6f, v * 1.5f);
        }

        private void DrawRect(SpriteBatch sb, Rectangle r, Color c)
        {
            EnsurePixel();
            sb.Draw(_pixel!, r, c);
        }

        private static void EnsurePixel()
        {
            if (_pixel == null)
            {
                _pixel = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
            }
        }

        private class NotifPopup
        {
            public string AppId    { get; set; } = "";
            public string Text     { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public float Progress  { get; set; } = 1f;
        }
    }
}
