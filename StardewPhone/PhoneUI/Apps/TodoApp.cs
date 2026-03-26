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
    /// App de Lembretes — lista de tarefas com persistência no save do jogo.
    /// Suporta lembretes por dia (notificação ao começar o dia).
    /// </summary>
    public class TodoApp : AppBase
    {
        public class TodoItem
        {
            public string Text    { get; set; } = "";
            public bool   Done    { get; set; } = false;
            public int    DiaLembrete { get; set; } = -1; // -1 = sem lembrete
            public string Estacao { get; set; } = "";
        }

        // Lista persistida via SMAPI save data
        public static List<TodoItem> Items { get; private set; } = new();

        private string _novoTexto = "";
        private bool _adicionando = false;
        private int _scroll = 0;

        public TodoApp(IModHelper helper, IMonitor monitor) : base(helper, monitor)
        {
            try { Icon = helper.ModContent.Load<Texture2D>("assets/sprites/icon_todo.png"); }
            catch { }

            LoadItems();
        }

        public void LoadItems()
        {
            try
            {
                var data = Helper.Data.ReadSaveData<List<TodoItem>>("phone_todo");
                if (data != null) Items = data;
            }
            catch { Items = new List<TodoItem>(); }
        }

        private void SaveItems()
        {
            try { Helper.Data.WriteSaveData("phone_todo", Items); }
            catch { }
        }

        public override void OnClose() => SaveItems();

        public override void Draw(SpriteBatch sb, Rectangle screen, Action goBack)
        {
            DrawRect(sb, screen, new Color(15, 10, 5));
            DrawTitleBar(sb, screen, "📋 Lembretes", goBack, new Color(140, 70, 0));

            int titleH = S(26);
            int sy = screen.Y + titleH + S(4);
            int bx = screen.X + S(6);
            int bw = screen.Width - S(12);

            // Botão adicionar
            if (DrawButton(sb, new Rectangle(bx, sy, bw, S(26)), _adicionando ? "✕ Cancelar" : "+ Novo Lembrete",
                _adicionando ? new Color(100, 30, 0) : new Color(160, 80, 0)))
            {
                _adicionando = !_adicionando;
                _novoTexto = "";
            }

            sy += S(30);

            // Área de criação de novo item
            if (_adicionando)
            {
                sy = DrawNewItemForm(sb, screen, bx, bw, sy);
            }

            // Lista de itens
            DrawItemList(sb, screen, bx, bw, sy);
        }

        private int DrawNewItemForm(SpriteBatch sb, Rectangle screen, int bx, int bw, int sy)
        {
            // Campo de texto
            var inputRect = new Rectangle(bx, sy, bw, S(30));
            DrawRoundRect(sb, inputRect, new Color(35, 20, 5));
            string disp = _novoTexto.Length > 0 ? _novoTexto : "Escreva o lembrete...";
            Color textColor = _novoTexto.Length > 0 ? Color.White : Color.Gray;
            DrawText(sb, disp, bx + S(4), sy + S(7), textColor, 0.45f);

            if (WasClicked(inputRect))
            {
                // No mobile, abre teclado virtual do Stardew
                Game1.showTextEntry(new StardewValley.Menus.TextBox(null, null, Game1.smallFont, Color.White));
            }

            sy += S(34);

            // Teclado de texto simplificado (letras A-Z em rows)
            string[] rows = { "qwertyuiop", "asdfghjkl", "zxcvbnm ⌫" };
            int kSpacing = S(2);
            foreach (var row in rows)
            {
                int totalW = row.Length * (S(20) + kSpacing) - kSpacing;
                int startX = bx + (bw - totalW) / 2;
                for (int i = 0; i < row.Length; i++)
                {
                    string ch = row[i].ToString();
                    var kr = new Rectangle(startX + i * (S(20) + kSpacing), sy, S(20), S(22));
                    Color kc = ch == "⌫" ? new Color(100, 30, 0) : new Color(40, 25, 10);
                    if (DrawButton(sb, kr, ch == " " ? "␣" : ch, kc, 0.4f))
                    {
                        if (ch == "⌫" && _novoTexto.Length > 0)
                            _novoTexto = _novoTexto[..^1];
                        else if (ch == " ")
                            _novoTexto += " ";
                        else if (_novoTexto.Length < 40)
                            _novoTexto += ch;
                    }
                }
                sy += S(26);
            }

            // Botão salvar
            if (DrawButton(sb, new Rectangle(bx, sy, bw, S(28)), "✓ Salvar", new Color(0, 120, 60)))
            {
                if (!string.IsNullOrWhiteSpace(_novoTexto))
                {
                    Items.Add(new TodoItem { Text = _novoTexto.Trim() });
                    SaveItems();
                    _novoTexto = "";
                    _adicionando = false;
                }
            }

            sy += S(32);
            return sy;
        }

        private void DrawItemList(SpriteBatch sb, Rectangle screen, int bx, int bw, int startY)
        {
            if (Items.Count == 0)
            {
                DrawCenteredText(sb, "Nenhum lembrete ainda!", screen, Color.Gray, 0.48f);
                return;
            }

            int itemH = S(32);
            int y = startY - _scroll * itemH;

            for (int i = 0; i < Items.Count; i++)
            {
                if (y > screen.Bottom - S(10)) break;
                if (y < startY - itemH) { y += itemH; continue; }

                var item = Items[i];
                var row = new Rectangle(bx, y, bw, itemH - S(2));

                // Fundo da linha
                DrawRoundRect(sb, row, item.Done ? new Color(15, 30, 15) : new Color(30, 18, 5));

                // Checkbox
                var check = new Rectangle(bx + S(4), y + (itemH - S(16)) / 2, S(16), S(16));
                DrawRoundRect(sb, check, item.Done ? new Color(0, 150, 50) : new Color(60, 40, 10));
                if (item.Done) DrawCenteredText(sb, "✓", check, Color.White, 0.5f);

                // Texto do lembrete
                string txt = item.Text.Length > 24 ? item.Text[..24] + "…" : item.Text;
                Color txtColor = item.Done ? Color.Gray : Color.White;
                DrawText(sb, txt, bx + S(24), y + S(8), txtColor, 0.48f);

                // Botão deletar
                var del = new Rectangle(row.Right - S(22), y + (itemH - S(18)) / 2, S(18), S(18));
                if (DrawButton(sb, del, "✕", new Color(100, 20, 10), 0.4f))
                {
                    Items.RemoveAt(i);
                    SaveItems();
                    break;
                }

                // Toggle done ao clicar na linha
                if (WasClicked(new Rectangle(bx, y, bw - S(28), itemH)))
                    item.Done = !item.Done;

                y += itemH;
            }

            // Scroll
            if (Items.Count > 5)
            {
                var upBtn = new Rectangle(screen.Right - S(18), startY, S(14), S(14));
                var dnBtn = new Rectangle(screen.Right - S(18), screen.Bottom - S(18), S(14), S(14));
                if (DrawButton(sb, upBtn, "▲", new Color(50, 30, 5), 0.35f) && _scroll > 0) _scroll--;
                if (DrawButton(sb, dnBtn, "▼", new Color(50, 30, 5), 0.35f)) _scroll++;
            }
        }
    }
}
