using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewPhone.Multiplayer;
using System;
using System.Collections.Generic;

namespace StardewPhone.PhoneUI.Apps
{
    /// <summary>
    /// App de Chat — permite enviar/receber mensagens entre jogadores no multiplayer.
    /// No singleplayer, funciona como diário de mensagens locais.
    /// </summary>
    public class ChatApp : AppBase
    {
        // Histórico de mensagens: (remetente, texto, cor)
        public static readonly List<(string sender, string text, Color color)> Messages = new();
        private const int MaxMessages = 50;

        // Campo de texto simples (simulado com teclas)
        private string _inputText = "";
        private bool _inputFocused = false;

        // Scroll
        private int _scrollOffset = 0;
        private const int MsgHeight = 28;

        // Cores dos jogadores
        private static readonly Color[] PlayerColors =
        {
            new Color(100, 180, 255),
            new Color(255, 160, 100),
            new Color(100, 255, 140),
            new Color(255, 100, 180),
        };

        public ChatApp(IModHelper helper, IMonitor monitor) : base(helper, monitor)
        {
            try { Icon = helper.ModContent.Load<Texture2D>("assets/sprites/icon_chat.png"); }
            catch { /* usa fallback */ }

            // Escuta mensagens recebidas do multiplayer
            PhoneMultiplayer.OnChatReceived += OnChatReceived;
        }

        private void OnChatReceived(string sender, string text)
        {
            AddMessage(sender, text, PlayerColors[sender.GetHashCode() % PlayerColors.Length]);
            ModEntry.Notifications.AddNotification("chat", $"{sender}: {text}");
        }

        public override void OnOpen()
        {
            _inputFocused = true;
            _scrollOffset = 0;
        }

        public override void OnClose()
        {
            _inputFocused = false;
            // Marca mensagens como lidas
            ModEntry.Notifications.ClearUnread("chat");
        }

        public override void Update(UpdateTickedEventArgs e)
        {
            if (!_inputFocused) return;

            // Captura input de teclado para o campo de texto
            // O Stardew Mobile usa teclado virtual — Game1.keyboardDispatcher
            var kb = Microsoft.Xna.Framework.Input.Keyboard.GetState();

            foreach (var key in kb.GetPressedKeys())
            {
                if (key == Microsoft.Xna.Framework.Input.Keys.Back && _inputText.Length > 0)
                    _inputText = _inputText[..^1];
                else if (key == Microsoft.Xna.Framework.Input.Keys.Enter)
                    SendMessage();
                else if (key == Microsoft.Xna.Framework.Input.Keys.Space)
                    _inputText += " ";
                else if (key >= Microsoft.Xna.Framework.Input.Keys.A &&
                         key <= Microsoft.Xna.Framework.Input.Keys.Z)
                    _inputText += key.ToString().ToLower();
                else if (key >= Microsoft.Xna.Framework.Input.Keys.D0 &&
                         key <= Microsoft.Xna.Framework.Input.Keys.D9)
                    _inputText += ((int)key - 48).ToString();
            }
        }

        public override void Draw(SpriteBatch sb, Rectangle screen, Action goBack)
        {
            // Fundo escuro
            DrawRect(sb, screen, new Color(12, 15, 30));

            // Título
            DrawTitleBar(sb, screen, "💬 Chat", goBack, new Color(0, 90, 180));

            int titleH = S(26);
            int inputH = S(32);
            int listH  = screen.Height - titleH - inputH - S(6);

            var listArea  = new Rectangle(screen.X, screen.Y + titleH, screen.Width, listH);
            var inputArea = new Rectangle(screen.X, screen.Bottom - inputH, screen.Width, inputH);

            DrawMessageList(sb, listArea);
            DrawInputBar(sb, inputArea);
        }

        private void DrawMessageList(SpriteBatch sb, Rectangle area)
        {
            DrawRect(sb, area, new Color(8, 10, 22));

            int y = area.Y + S(4) - _scrollOffset * MsgHeight;
            int maxVisible = (area.Height / S(MsgHeight)) + 1;
            int start = Math.Max(0, Messages.Count - maxVisible - _scrollOffset);

            for (int i = start; i < Messages.Count; i++)
            {
                var (sender, text, color) = Messages[i];
                if (y > area.Bottom) break;
                if (y < area.Y - S(MsgHeight)) { y += S(MsgHeight); continue; }

                // Balão de mensagem
                bool isMine = sender == Game1.player.Name;
                int balloonW = (int)(area.Width * 0.75f);
                int bx = isMine ? area.Right - balloonW - S(4) : area.X + S(4);

                var balloon = new Rectangle(bx, y, balloonW, S(MsgHeight - 2));
                DrawRoundRect(sb, balloon, isMine ? new Color(0, 100, 200) : new Color(35, 40, 60));

                // Remetente (apenas se não for eu)
                if (!isMine)
                    DrawText(sb, sender, bx + S(4), y + S(2), color, 0.4f);

                // Texto da mensagem
                string display = text.Length > 28 ? text[..28] + "…" : text;
                DrawText(sb, display, bx + S(4), y + (isMine ? S(8) : S(14)), Color.White, 0.48f);

                y += S(MsgHeight);
            }

            // Botões de scroll se houver muitas mensagens
            if (Messages.Count > 6)
            {
                var upBtn = new Rectangle(area.Right - S(18), area.Y + S(2), S(16), S(16));
                var dnBtn = new Rectangle(area.Right - S(18), area.Bottom - S(18), S(16), S(16));
                if (DrawButton(sb, upBtn, "▲", new Color(40, 40, 60), 0.4f) && _scrollOffset > 0)
                    _scrollOffset--;
                if (DrawButton(sb, dnBtn, "▼", new Color(40, 40, 60), 0.4f))
                    _scrollOffset++;
            }
        }

        private void DrawInputBar(SpriteBatch sb, Rectangle area)
        {
            DrawRect(sb, area, new Color(20, 25, 50));

            int sendW = S(44);
            var inputRect = new Rectangle(area.X + S(2), area.Y + S(4), area.Width - sendW - S(6), area.Height - S(8));
            var sendRect  = new Rectangle(area.Right - sendW - S(2), area.Y + S(4), sendW, area.Height - S(8));

            // Campo de input
            DrawRoundRect(sb, inputRect, _inputFocused ? new Color(40, 50, 80) : new Color(25, 30, 55));
            string display = _inputText + (_inputFocused ? "|" : "");
            if (_inputText.Length == 0 && !_inputFocused)
                DrawText(sb, "Mensagem...", inputRect.X + S(4), inputRect.Y + S(6), Color.Gray, 0.45f);
            else
                DrawText(sb, display, inputRect.X + S(4), inputRect.Y + S(6), Color.White, 0.45f);

            // Foca ao clicar no input
            if (WasClicked(inputRect)) _inputFocused = true;

            // Botão enviar
            if (DrawButton(sb, sendRect, "➤", new Color(0, 130, 80)))
                SendMessage();
        }

        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(_inputText)) return;

            string msg = _inputText.Trim();
            _inputText = "";

            // Adiciona localmente
            AddMessage(Game1.player.Name, msg, PlayerColors[0]);

            // Envia para outros jogadores
            ModEntry.PhoneMultiplayer.SendChatMessage(msg);
        }

        private static void AddMessage(string sender, string text, Color color)
        {
            Messages.Add((sender, text, color));
            if (Messages.Count > MaxMessages)
                Messages.RemoveAt(0);
        }
    }
}
