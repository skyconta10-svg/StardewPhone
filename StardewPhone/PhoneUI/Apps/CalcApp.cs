using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System;

namespace StardewPhone.PhoneUI.Apps
{
    /// <summary>
    /// App Calculadora — operações básicas com interface de teclado numérico.
    /// </summary>
    public class CalcApp : AppBase
    {
        private string _display = "0";
        private double _valorA = 0;
        private string _operador = "";
        private bool _novaEntrada = false;

        public CalcApp(IModHelper helper, IMonitor monitor) : base(helper, monitor)
        {
            try { Icon = helper.ModContent.Load<Texture2D>("assets/sprites/icon_calc.png"); }
            catch { }
        }

        public override void OnOpen()
        {
            _display = "0";
            _valorA = 0;
            _operador = "";
            _novaEntrada = false;
        }

        public override void Draw(SpriteBatch sb, Rectangle screen, Action goBack)
        {
            DrawRect(sb, screen, new Color(12, 12, 20));
            DrawTitleBar(sb, screen, "🔢 Calculadora", goBack, new Color(50, 30, 100));

            int titleH = S(26);
            int displayH = S(50);
            int padX = S(6);
            int padY = S(4);

            // --- Display ---
            var displayRect = new Rectangle(screen.X + padX, screen.Y + titleH + padY,
                                            screen.Width - padX * 2, displayH);
            DrawRoundRect(sb, displayRect, new Color(20, 20, 35));

            // Alinha número à direita
            string disp = _display.Length > 12 ? _display[..12] : _display;
            float scale = disp.Length > 8 ? 0.55f : 0.75f;
            Vector2 dSize = Game1.smallFont.MeasureString(disp) * scale;
            sb.DrawString(Game1.smallFont, disp,
                new Vector2(displayRect.Right - dSize.X - S(6),
                            displayRect.Y + (displayRect.Height - dSize.Y) / 2f),
                Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            // Operador ativo
            if (!string.IsNullOrEmpty(_operador))
                DrawText(sb, _operador, displayRect.X + S(4), displayRect.Y + S(6), Color.LightBlue, 0.55f);

            // --- Grade de botões ---
            int gridY = screen.Y + titleH + displayH + padY * 2;
            int cols = 4;
            int rows = 5;
            int spacing = S(3);
            int btnW = (screen.Width - padX * 2 - spacing * (cols - 1)) / cols;
            int btnH = (screen.Bottom - gridY - padY - spacing * (rows - 1)) / rows;

            // Layout da calculadora
            string[,] layout = {
                { "C", "±", "%", "÷" },
                { "7", "8", "9", "×" },
                { "4", "5", "6", "−" },
                { "1", "2", "3", "+" },
                { "0", "0", ".", "=" },
            };

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    // "0" ocupa 2 colunas na última linha
                    if (r == 4 && c == 1) continue;
                    if (r == 4 && c == 0)
                    {
                        var bigBtn = new Rectangle(
                            screen.X + padX + c * (btnW + spacing),
                            gridY + r * (btnH + spacing),
                            btnW * 2 + spacing, btnH);
                        if (DrawButton(sb, bigBtn, "0", GetButtonColor("0")))
                            HandleInput("0");
                        continue;
                    }

                    string key = layout[r, c];
                    var btn = new Rectangle(
                        screen.X + padX + c * (btnW + spacing),
                        gridY + r * (btnH + spacing),
                        btnW, btnH);

                    if (DrawButton(sb, btn, key, GetButtonColor(key)))
                        HandleInput(key);
                }
            }
        }

        private Color GetButtonColor(string key) => key switch
        {
            "=" => new Color(255, 140, 0),
            "÷" or "×" or "−" or "+" => new Color(60, 60, 120),
            "C" or "±" or "%" => new Color(50, 50, 70),
            _ => new Color(30, 35, 55),
        };

        private void HandleInput(string key)
        {
            switch (key)
            {
                case "C":
                    _display = "0"; _valorA = 0; _operador = ""; _novaEntrada = false;
                    break;

                case "±":
                    if (double.TryParse(_display, out double v))
                        _display = (-v).ToString("G10");
                    break;

                case "%":
                    if (double.TryParse(_display, out double pct))
                        _display = (pct / 100).ToString("G10");
                    break;

                case "÷": case "×": case "−": case "+":
                    if (double.TryParse(_display, out double a))
                        _valorA = a;
                    _operador = key;
                    _novaEntrada = true;
                    break;

                case "=":
                    if (!string.IsNullOrEmpty(_operador) && double.TryParse(_display, out double b))
                    {
                        double resultado = _operador switch
                        {
                            "+" => _valorA + b,
                            "−" => _valorA - b,
                            "×" => _valorA * b,
                            "÷" => b != 0 ? _valorA / b : double.NaN,
                            _ => b
                        };
                        _display = double.IsNaN(resultado) ? "Erro" : resultado.ToString("G10");
                        _operador = "";
                        _novaEntrada = false;
                    }
                    break;

                case ".":
                    if (_novaEntrada) { _display = "0."; _novaEntrada = false; }
                    else if (!_display.Contains('.')) _display += ".";
                    break;

                default: // dígitos
                    if (_novaEntrada || _display == "0")
                    { _display = key; _novaEntrada = false; }
                    else if (_display.Length < 12)
                        _display += key;
                    break;
            }
        }
    }
}
