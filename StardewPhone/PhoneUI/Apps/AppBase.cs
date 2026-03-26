using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;

namespace StardewPhone.PhoneUI.Apps
{
    /// <summary>
    /// Classe base para todos os apps do StardewPhone.
    /// Fornece helpers de desenho e estrutura padronizada.
    /// </summary>
    public abstract class AppBase
    {
        protected IModHelper Helper;
        protected IMonitor Monitor;

        // Ícone do app (carregado da pasta assets/sprites/)
        public Texture2D? Icon { get; protected set; }

        // Controle de clique único (evita múltiplos eventos por frame)
        private bool _wasMouseDown = false;

        // Pixel 1x1 para desenhar retângulos
        private static Texture2D? _pixel;

        protected AppBase(IModHelper helper, IMonitor monitor)
        {
            Helper  = helper;
            Monitor = monitor;
        }

        /// <summary>Chamado quando o app é aberto.</summary>
        public virtual void OnOpen() { }

        /// <summary>Chamado quando o app é fechado.</summary>
        public virtual void OnClose() { }

        /// <summary>Atualiza lógica do app (chamado todo frame).</summary>
        public virtual void Update(UpdateTickedEventArgs e) { }

        /// <summary>
        /// Desenha o app dentro de 'screen'.
        /// 'goBack' deve ser chamado para voltar à home.
        /// </summary>
        public abstract void Draw(SpriteBatch sb, Rectangle screen, Action goBack);

        // =========================================================
        // Helpers de UI usados pelos apps filhos
        // =========================================================

        /// <summary>Desenha barra de título padrão do app.</summary>
        protected void DrawTitleBar(SpriteBatch sb, Rectangle screen, string title, Action goBack, Color? barColor = null)
        {
            int h = PhoneOverlay.Scale(26);
            var bar = new Rectangle(screen.X, screen.Y, screen.Width, h);
            DrawRect(sb, bar, barColor ?? new Color(20, 25, 50));

            // Botão voltar "<"
            int btnW = PhoneOverlay.Scale(22);
            var backBtn = new Rectangle(screen.X + PhoneOverlay.Scale(2), screen.Y + PhoneOverlay.Scale(2), btnW, h - PhoneOverlay.Scale(4));
            DrawRect(sb, backBtn, Color.DimGray);
            DrawCenteredText(sb, "<", backBtn, Color.White, 0.55f);

            if (WasClicked(backBtn)) goBack();

            // Título
            DrawCenteredText(sb, title, bar, Color.White, 0.55f);
        }

        /// <summary>Desenha um botão e retorna se foi clicado.</summary>
        protected bool DrawButton(SpriteBatch sb, Rectangle rect, string label, Color? color = null, float textScale = 0.55f)
        {
            bool hovered = rect.Contains(Game1.getMousePosition());
            Color bg = color ?? new Color(0, 100, 200);
            if (hovered) bg = Color.Lerp(bg, Color.White, 0.2f);

            DrawRoundRect(sb, rect, bg);
            DrawCenteredText(sb, label, rect, Color.White, textScale);

            return WasClicked(rect);
        }

        /// <summary>Desenha texto centralizado dentro de um retângulo.</summary>
        protected void DrawCenteredText(SpriteBatch sb, string text, Rectangle bounds, Color color, float scale = 0.6f)
        {
            Vector2 size = Game1.smallFont.MeasureString(text) * scale;
            sb.DrawString(Game1.smallFont, text,
                new Vector2(bounds.X + (bounds.Width - size.X) / 2f,
                            bounds.Y + (bounds.Height - size.Y) / 2f),
                color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        /// <summary>Desenha texto alinhado à esquerda.</summary>
        protected void DrawText(SpriteBatch sb, string text, int x, int y, Color color, float scale = 0.55f)
        {
            sb.DrawString(Game1.smallFont, text, new Vector2(x, y),
                color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        /// <summary>Verifica clique único (não segura) em um retângulo.</summary>
        protected bool WasClicked(Rectangle rect)
        {
            bool mouseDown = Game1.input.GetMouseState().LeftButton ==
                             Microsoft.Xna.Framework.Input.ButtonState.Pressed;
            bool justClicked = mouseDown && !_wasMouseDown && rect.Contains(Game1.getMousePosition());
            _wasMouseDown = mouseDown;
            return justClicked;
        }

        protected void DrawRect(SpriteBatch sb, Rectangle r, Color c)
        {
            EnsurePixel();
            sb.Draw(_pixel!, r, c);
        }

        protected void DrawRoundRect(SpriteBatch sb, Rectangle r, Color c)
        {
            int rad = PhoneOverlay.Scale(6);
            DrawRect(sb, new Rectangle(r.X + rad, r.Y, r.Width - rad * 2, r.Height), c);
            DrawRect(sb, new Rectangle(r.X, r.Y + rad, r.Width, r.Height - rad * 2), c);
        }

        private static void EnsurePixel()
        {
            if (_pixel == null)
            {
                _pixel = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
            }
        }

        protected int S(int v) => PhoneOverlay.Scale(v);
    }
}
