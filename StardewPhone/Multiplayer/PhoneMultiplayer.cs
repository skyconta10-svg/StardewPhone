using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;

namespace StardewPhone.Multiplayer
{
    /// <summary>
    /// Gerencia toda comunicação entre jogadores via SMAPI Multiplayer API.
    /// Suporta: mensagens de chat, transferência de ouro (pix).
    /// </summary>
    public class PhoneMultiplayer
    {
        // Tipos de mensagens (IDs únicos do mod)
        private const string MsgChat   = "StardewPhone.Chat";
        private const string MsgPix    = "StardewPhone.Pix";
        private const string MsgPixAck = "StardewPhone.PixAck";

        private readonly IModHelper _helper;
        private readonly IMonitor   _monitor;

        // Eventos para os apps escutarem
        public static event Action<string, string>? OnChatReceived;  // (sender, text)
        public static event Action<string, int>?    OnPixReceived;   // (sender, amount)

        public PhoneMultiplayer(IModHelper helper, IMonitor monitor)
        {
            _helper  = helper;
            _monitor = monitor;
        }

        // =========================================================
        // Envio de mensagens
        // =========================================================

        /// <summary>Envia mensagem de chat para todos os jogadores online.</summary>
        public void SendChatMessage(string text)
        {
            if (!Context.IsMultiplayer) return;

            _helper.Multiplayer.SendMessage(
                new ChatPayload { Sender = Game1.player.Name, Text = text },
                MsgChat,
                modIDs: new[] { _helper.ModRegistry.ModID }
            );
        }

        /// <summary>Envia pix para um jogador específico.</summary>
        public void SendPix(string targetPlayerName, int amount)
        {
            if (!Context.IsMultiplayer)
            {
                _monitor.Log("Pix só disponível no multiplayer.", LogLevel.Info);
                return;
            }

            // Encontra o peer pelo nome
            long? targetId = FindPeerByName(targetPlayerName);
            if (targetId == null)
            {
                Game1.addHUDMessage(new HUDMessage($"{targetPlayerName} não está online!", HUDMessage.error_type));
                return;
            }

            _helper.Multiplayer.SendMessage(
                new PixPayload { Sender = Game1.player.Name, Amount = amount },
                MsgPix,
                modIDs: new[] { _helper.ModRegistry.ModID },
                playerIDs: new[] { targetId.Value }
            );
        }

        // =========================================================
        // Recebimento de mensagens
        // =========================================================

        public void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != _helper.ModRegistry.ModID) return;

            switch (e.Type)
            {
                case MsgChat:
                    var chat = e.ReadAs<ChatPayload>();
                    // Não exibe se for própria mensagem (host pode receber de volta)
                    if (chat.Sender != Game1.player.Name)
                        OnChatReceived?.Invoke(chat.Sender, chat.Text);
                    break;

                case MsgPix:
                    var pix = e.ReadAs<PixPayload>();
                    OnPixReceived?.Invoke(pix.Sender, pix.Amount);
                    break;
            }
        }

        // =========================================================
        // Utilitários
        // =========================================================

        /// <summary>Retorna lista de nomes dos jogadores online (exceto você).</summary>
        public List<string> GetOnlinePlayers()
        {
            var players = new List<string>();
            if (!Context.IsMultiplayer) return players;

            foreach (var peer in _helper.Multiplayer.GetConnectedPlayers())
            {
                // Busca o Farmer correspondente ao peer
                foreach (var farmer in Game1.getOnlineFarmers())
                {
                    if (farmer.UniqueMultiplayerID == peer.PlayerID
                        && farmer.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
                    {
                        players.Add(farmer.Name);
                        break;
                    }
                }
            }
            return players;
        }

        private long? FindPeerByName(string name)
        {
            foreach (var farmer in Game1.getOnlineFarmers())
            {
                if (farmer.Name == name && farmer.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
                    return farmer.UniqueMultiplayerID;
            }
            return null;
        }

        // =========================================================
        // Payloads (serializados como JSON pelo SMAPI)
        // =========================================================

        private class ChatPayload
        {
            public string Sender { get; set; } = "";
            public string Text   { get; set; } = "";
        }

        private class PixPayload
        {
            public string Sender { get; set; } = "";
            public int    Amount { get; set; } = 0;
        }
    }
}
