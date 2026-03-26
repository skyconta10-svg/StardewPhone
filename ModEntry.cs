using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewPhone.PhoneUI;
using StardewPhone.Notifications;
using StardewPhone.Multiplayer;

namespace StardewPhone
{
    /// <summary>
    /// Ponto de entrada do mod StardewPhone.
    /// Registra eventos e inicializa os sistemas principais.
    /// </summary>
    public class ModEntry : Mod
    {
        // Instância do overlay da tela do celular
        private PhoneOverlay? _phoneOverlay;

        // Gerenciador de notificações
        public static NotificationManager Notifications = null!;

        // Gerenciador de multiplayer
        public static PhoneMultiplayer PhoneMultiplayer = null!;

        // Referência estática ao helper do SMAPI (usado por outras classes)
        public static IModHelper ModHelper = null!;
        public static IMonitor ModMonitor = null!;

        public override void Entry(IModHelper helper)
        {
            ModHelper = helper;
            ModMonitor = Monitor;

            // Inicializa os sistemas
            Notifications = new NotificationManager();
            PhoneMultiplayer = new PhoneMultiplayer(helper, Monitor);

            // Registra eventos
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Display.RenderedHud += OnRenderedHud;
            helper.Events.Multiplayer.ModMessageReceived += PhoneMultiplayer.OnModMessageReceived;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;

            Monitor.Log("StardewPhone carregado com sucesso!", LogLevel.Info);
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // Inicializa overlay após o jogo carregar (texturas disponíveis)
            _phoneOverlay = new PhoneOverlay(Helper, Monitor);
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            // Recarrega dados salvos ao carregar save
            Notifications.LoadData();
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            // Dispara notificações do dia (lembretes, aniversários, etc.)
            Notifications.CheckDailyNotifications();
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            _phoneOverlay?.Update(e);
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // Abre o celular ao pressionar Tab (ou botão de ombro no mobile)
            if (!Context.IsPlayerFree) return;

            if (e.Button == SButton.Tab || e.Button == SButton.LeftShoulder)
            {
                _phoneOverlay?.TogglePhone();
            }
        }

        private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
        {
            // Desenha o celular sobre o HUD do jogo
            _phoneOverlay?.Draw(e.SpriteBatch);

            // Desenha notificações popup (ex: mensagem recebida)
            Notifications.DrawPopups(e.SpriteBatch);
        }
    }
}
