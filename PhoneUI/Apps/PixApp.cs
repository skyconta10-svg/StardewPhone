using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;

namespace StardewPhone.PhoneUI.Apps
{
    /// <summary>
    /// App de Pix — transferência instantânea de ouro entre jogadores.
    /// No singleplayer mostra o extrato local.
    /// </summary>
    public class PixApp : AppBase
    {
        private enum PixScreen { Home, Send, Extrato }
        private PixScreen _screen = PixScreen.Home;

        // Transferência
        private string _amountInput = "";
        private string _selectedPlayer = "";
        private List<string> _onlinePlayers = new();

        // Extrato (histórico de transações)
        public static readonly List<(string tipo, string nome, int valor, string hora)> Extrato = new();

        public PixApp(IModHelper helper, IMonitor monitor) : base(helper, monitor)
        {
            try { Icon = helper.ModContent.Load<Texture2D>("assets/sprites/icon_pix.png"); }
            catch { }

            // Escuta confirmações de pix recebido
            ModEntry.PhoneMultiplayer.OnPixReceived += OnPixReceived;
        }

        private void OnPixReceived(string sender, int amount)
        {
            Game1.player.Money += amount;
            string hora = $"{Game1.timeOfDay / 100:D2}:{Game1.timeOfDay % 100:D2}";
            Extrato.Insert(0, ("recebido", sender, amount, hora));
            ModEntry.Notifications.AddNotification("pix", $"Pix recebido de {sender}: {amount}g");
            Game1.addHUDMessage(new HUDMessage($"💸 Pix recebido de {sender}: {amount}g", HUDMessage.newQuest_type));
        }

        public override void OnOpen()
        {
            _screen = PixScreen.Home;
            _onlinePlayers = ModEntry.PhoneMultiplayer.GetOnlinePlayers();
        }

        public override void Draw(SpriteBatch sb, Rectangle screen, Action goBack)
        {
            DrawRect(sb, screen, new Color(0, 18, 12));

            switch (_screen)
            {
                case PixScreen.Home:    DrawHome(sb, screen, goBack); break;
                case PixScreen.Send:    DrawSend(sb, screen);        break;
                case PixScreen.Extrato: DrawExtrato(sb, screen);     break;
            }
        }

        private void DrawHome(SpriteBatch sb, Rectangle screen, Action goBack)
        {
            DrawTitleBar(sb, screen, "💚 Pix", goBack, new Color(0, 100, 60));

            int sy = screen.Y + S(34);
            int bw = screen.Width - S(16);
            int bx = screen.X + S(8);

            // Saldo
            var saldoRect = new Rectangle(bx, sy, bw, S(48));
            DrawRoundRect(sb, saldoRect, new Color(0, 60, 40));
            DrawCenteredText(sb, "Saldo Atual", saldoRect with { Height = S(20) }, Color.LightGreen, 0.45f);
            DrawCenteredText(sb, $"{Game1.player.Money:N0} ouro",
                saldoRect with { Y = saldoRect.Y + S(22), Height = S(26) }, Color.White, 0.7f);

            sy += S(56);

            // Botão Enviar Pix
            if (DrawButton(sb, new Rectangle(bx, sy, bw, S(34)), "➤  Enviar Pix", new Color(0, 150, 90)))
            {
                _screen = PixScreen.Send;
                _amountInput = "";
                _selectedPlayer = _onlinePlayers.Count > 0 ? _onlinePlayers[0] : "";
            }

            sy += S(42);

            // Botão Extrato
            if (DrawButton(sb, new Rectangle(bx, sy, bw, S(34)), "📋  Extrato", new Color(0, 80, 55)))
                _screen = PixScreen.Extrato;
        }

        private void DrawSend(SpriteBatch sb, Rectangle screen)
        {
            DrawTitleBar(sb, screen, "Enviar Pix", () => _screen = PixScreen.Home, new Color(0, 100, 60));

            int sy = screen.Y + S(34);
            int bx = screen.X + S(8);
            int bw = screen.Width - S(16);

            // Selecionar destinatário
            DrawText(sb, "Para:", bx, sy, Color.LightGreen, 0.48f);
            sy += S(16);

            if (_onlinePlayers.Count == 0)
            {
                DrawText(sb, "Nenhum jogador online.", bx, sy, Color.Gray, 0.45f);
                sy += S(18);
            }
            else
            {
                foreach (var player in _onlinePlayers)
                {
                    bool selected = player == _selectedPlayer;
                    var pr = new Rectangle(bx, sy, bw, S(28));
                    DrawRoundRect(sb, pr, selected ? new Color(0, 120, 80) : new Color(20, 40, 30));
                    DrawText(sb, (selected ? "✓ " : "  ") + player, bx + S(6), sy + S(6), Color.White, 0.5f);
                    if (WasClicked(pr)) _selectedPlayer = player;
                    sy += S(32);
                }
            }

            sy += S(8);

            // Campo valor
            DrawText(sb, "Valor (ouro):", bx, sy, Color.LightGreen, 0.48f);
            sy += S(16);
            var inputR = new Rectangle(bx, sy, bw, S(30));
            DrawRoundRect(sb, inputR, new Color(15, 35, 25));
            DrawText(sb, _amountInput.Length > 0 ? _amountInput : "0", bx + S(6), sy + S(7), Color.White, 0.55f);
            sy += S(36);

            // Teclado numérico simples
            string[] keys = { "1","2","3","4","5","6","7","8","9","⌫","0","OK" };
            int kCols = 3;
            int kSize = (bw - S(4) * (kCols - 1)) / kCols;
            for (int i = 0; i < keys.Length; i++)
            {
                int c = i % kCols, r = i / kCols;
                var kr = new Rectangle(bx + c * (kSize + S(4)), sy + r * (S(28) + S(4)), kSize, S(28));
                Color kColor = keys[i] == "OK" ? new Color(0, 150, 90) :
                               keys[i] == "⌫" ? new Color(120, 30, 30) :
                               new Color(20, 50, 35);
                if (DrawButton(sb, kr, keys[i], kColor))
                {
                    if (keys[i] == "⌫" && _amountInput.Length > 0)
                        _amountInput = _amountInput[..^1];
                    else if (keys[i] == "OK")
                        ConfirmPix();
                    else if (_amountInput.Length < 7)
                        _amountInput += keys[i];
                }
            }
        }

        private void ConfirmPix()
        {
            if (!int.TryParse(_amountInput, out int amount) || amount <= 0)
            {
                Game1.addHUDMessage(new HUDMessage("Valor inválido!", HUDMessage.error_type));
                return;
            }
            if (amount > Game1.player.Money)
            {
                Game1.addHUDMessage(new HUDMessage("Saldo insuficiente!", HUDMessage.error_type));
                return;
            }
            if (string.IsNullOrEmpty(_selectedPlayer))
            {
                Game1.addHUDMessage(new HUDMessage("Selecione um destinatário!", HUDMessage.error_type));
                return;
            }

            Game1.player.Money -= amount;
            ModEntry.PhoneMultiplayer.SendPix(_selectedPlayer, amount);

            string hora = $"{Game1.timeOfDay / 100:D2}:{Game1.timeOfDay % 100:D2}";
            Extrato.Insert(0, ("enviado", _selectedPlayer, amount, hora));

            Game1.addHUDMessage(new HUDMessage($"💸 Pix enviado para {_selectedPlayer}: {amount}g", HUDMessage.newQuest_type));
            _screen = PixScreen.Home;
        }

        private void DrawExtrato(SpriteBatch sb, Rectangle screen)
        {
            DrawTitleBar(sb, screen, "Extrato", () => _screen = PixScreen.Home, new Color(0, 100, 60));

            int sy = screen.Y + S(30);
            int bx = screen.X + S(6);
            int bw = screen.Width - S(12);

            if (Extrato.Count == 0)
            {
                DrawCenteredText(sb, "Nenhuma transação.", screen, Color.Gray, 0.5f);
                return;
            }

            foreach (var (tipo, nome, valor, hora) in Extrato)
            {
                if (sy > screen.Bottom - S(20)) break;
                bool recebido = tipo == "recebido";
                var row = new Rectangle(bx, sy, bw, S(30));
                DrawRoundRect(sb, row, new Color(10, 25, 18));
                Color valorColor = recebido ? Color.LightGreen : Color.OrangeRed;
                string sinal = recebido ? "+" : "-";
                DrawText(sb, $"{(recebido ? "⬇" : "⬆")} {nome}", bx + S(4), sy + S(4), Color.White, 0.45f);
                DrawText(sb, $"{sinal}{valor}g", bx + bw - S(52), sy + S(4), valorColor, 0.48f);
                DrawText(sb, hora, bx + S(4), sy + S(16), Color.Gray, 0.38f);
                sy += S(34);
            }
        }
    }
}
