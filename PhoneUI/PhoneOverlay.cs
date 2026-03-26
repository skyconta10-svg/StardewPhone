using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewPhone.PhoneUI.Apps;
using System.Collections.Generic;

namespace StardewPhone.PhoneUI
{
    /// <summary>
    /// Controla toda a interface visual do celular.
    /// Responsiva: adapta tamanho à resolução (funciona no Stardew Mobile).
    /// </summary>
    public class PhoneOverlay
    {
        // --- Texturas ---
        private Texture2D? _phoneBodyTexture;
        private Texture2D? _phoneScreenTexture;
        private Texture2D? _pixel; // textura 1x1 para desenhar retângulos coloridos

        // --- Estado ---
        private bool _isOpen = false;
        private AppBase? _currentApp = null;
        private HomeScreen _homeScreen;

        // --- Layout responsivo ---
        // O celular ocupa ~35% da largura da tela, centralizado verticalmente
        private Rectangle PhoneBounds => CalculatePhoneBounds();
        private Rectangle ScreenBounds => CalculateScreenBounds();

        // --- Apps disponíveis ---
        private readonly Dictionary<string, AppBase> _apps;

        private IModHelper _helper;
        private IMonitor _monitor;

        public PhoneOverlay(IModHelper helper, IMonitor monitor)
        {
            _helper = helper;
            _monitor = monitor;

            // Registra todos os apps
            _apps = new Dictionary<string, AppBase>
            {
                ["chat"]    = new ChatApp(helper, monitor),
                ["pix"]     = new PixApp(helper, monitor),
                ["calc"]    = new CalcApp(helper, monitor),
                ["todo"]    = new TodoApp(helper, monitor),
                ["weather"] = new WeatherApp(helper, monitor),
            };

            _homeScreen = new HomeScreen(_apps);

            // Carrega texturas após construção (chamado no GameLaunched)
            LoadTextures();
        }

        private void LoadTextures()
        {
            try
            {
                _phoneBodyTexture   = _helper.ModContent.Load<Texture2D>("assets/sprites/phone_body.png");
                _phoneScreenTexture = _helper.ModContent.Load<Texture2D>("assets/sprites/phone_screen.png");
            }
            catch
            {
                _monitor.Log("Sprites não encontrados — usando fallback colorido.", LogLevel.Warn);
            }

            // Cria pixel 1x1 para fallback/retângulos coloridos
            _pixel = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        public void TogglePhone()
        {
            _isOpen = !_isOpen;
            if (!_isOpen)
            {
                // Fecha app atual ao fechar o celular
                _currentApp = null;
                _homeScreen.Reset();
            }
        }

        public void Update(UpdateTickedEventArgs e)
        {
            if (!_isOpen) return;
            _currentApp?.Update(e);
        }

        public void Draw(SpriteBatch sb)
        {
            if (!_isOpen) return;

            var body   = PhoneBounds;
            var screen = ScreenBounds;

            // --- Sombra ---
            DrawRect(sb, new Rectangle(body.X + 6, body.Y + 6, body.Width, body.Height), Color.Black * 0.35f);

            // --- Corpo do celular ---
            if (_phoneBodyTexture != null)
                sb.Draw(_phoneBodyTexture, body, Color.White);
            else
                DrawRect(sb, body, new Color(30, 30, 35));

            // --- Tela ---
            if (_phoneScreenTexture != null)
                sb.Draw(_phoneScreenTexture, screen, Color.White);
            else
                DrawRect(sb, screen, new Color(15, 20, 40));

            // --- Conteúdo: home ou app aberto ---
            if (_currentApp == null)
                _homeScreen.Draw(sb, screen, OpenApp);
            else
                _currentApp.Draw(sb, screen, CloseApp);

            // --- Botão fechar (X) no canto superior direito do corpo ---
            DrawCloseButton(sb, body);
        }

        /// <summary>Abre um app pelo ID.</summary>
        private void OpenApp(string appId)
        {
            if (_apps.TryGetValue(appId, out var app))
            {
                _currentApp = app;
                app.OnOpen();
            }
        }

        /// <summary>Volta para a home screen.</summary>
        private void CloseApp()
        {
            _currentApp?.OnClose();
            _currentApp = null;
        }

        private void DrawCloseButton(SpriteBatch sb, Rectangle body)
        {
            int btnSize = Scale(18);
            var btn = new Rectangle(body.Right - btnSize - Scale(4), body.Y + Scale(4), btnSize, btnSize);
            DrawRect(sb, btn, Color.Red * 0.85f);

            // "X" com fonte do jogo
            string x = "X";
            Vector2 size = Game1.smallFont.MeasureString(x);
            sb.DrawString(Game1.smallFont, x,
                new Vector2(btn.X + (btn.Width - size.X) / 2, btn.Y + (btn.Height - size.Y) / 2),
                Color.White);

            // Detecta clique no X
            if (Game1.input.GetMouseState().LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed
                && btn.Contains(Game1.getMousePosition()))
            {
                TogglePhone();
            }
        }

        // --- Layout responsivo ---

        /// <summary>
        /// Calcula bounds do corpo do celular com base na resolução atual.
        /// No mobile, a tela é menor — o celular se ajusta automaticamente.
        /// </summary>
        private Rectangle CalculatePhoneBounds()
        {
            int screenW = Game1.uiViewport.Width;
            int screenH = Game1.uiViewport.Height;

            // Largura = 28% da tela, mínimo 180px, máximo 320px
            int w = (int)MathHelper.Clamp(screenW * 0.28f, 180, 320);
            // Proporção do corpo: ~9:16 (estilo smartphone)
            int h = (int)(w * 1.78f);
            h = (int)MathHelper.Clamp(h, 300, screenH - 80);

            // Posição: lado direito da tela com margem
            int x = screenW - w - Scale(24);
            int y = (screenH - h) / 2;

            return new Rectangle(x, y, w, h);
        }

        /// <summary>Área interna da tela do celular (dentro do corpo).</summary>
        private Rectangle CalculateScreenBounds()
        {
            var b = PhoneBounds;
            int pad = Scale(10);
            int topBar = Scale(28); // espaço para câmera / status bar
            return new Rectangle(
                b.X + pad,
                b.Y + topBar,
                b.Width - pad * 2,
                b.Height - topBar - Scale(40) // espaço para botão home
            );
        }

        /// <summary>Escala um valor baseado na resolução (responsividade).</summary>
        public static int Scale(int baseValue)
        {
            float ratio = Game1.uiViewport.Width / 1280f;
            return (int)MathHelper.Clamp(baseValue * ratio, baseValue * 0.6f, baseValue * 1.5f);
        }

        private void DrawRect(SpriteBatch sb, Rectangle rect, Color color)
        {
            if (_pixel != null)
                sb.Draw(_pixel, rect, color);
        }
    }
}
