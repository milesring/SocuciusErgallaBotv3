using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using DSharpPlus.SlashCommands;
using SocuciusErgallaBotv3.Services;
using SocuciusErgallaBotv3.Module;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using Microsoft.Extensions.DependencyInjection;

namespace SocuciusErgallaBotv3
{
    public class Bot
    {
        private readonly ConfigService _configService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Bot> _logger;
        private readonly MusicTimerService _timerService;
        private readonly ActivityService _activityService;

        public DiscordClient Client { get; private set; }
        public InteractivityExtension Interactivity { get; private set; }
        public CommandsNextExtension Commands { get; private set; }

        public Bot(ConfigService configService, IServiceProvider serviceProvider, ILogger<Bot> logger, MusicTimerService timerService, ActivityService activityService)
        {
            _configService = configService;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _timerService = timerService;
            _activityService = activityService;
        }

        public async Task RunAsync()
        {
            DiscordConfiguration config = new()
            {
                Token = _configService.Config.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true, 
                LoggerFactory = _serviceProvider.GetService<ILoggerFactory>()
            };

            Client = new DiscordClient(config);
            Client.UseInteractivity(new InteractivityConfiguration() { Timeout = TimeSpan.FromMinutes(1) });

            var slashCommands = Client.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = _serviceProvider
            });
            slashCommands.RegisterCommands<MusicSlashCommandContainer>(_configService.Config.GuildId);

            ConnectionEndpoint endpoint = new()
            {
                Hostname = _configService.Config.Hostname,
                Port = _configService.Config.Port
            };

            LavalinkConfiguration lavalinkConfiguration = new()
            {
                Password = _configService.Config.Password,
                RestEndpoint = endpoint,
                SocketEndpoint = endpoint
            };

            var lavaLink = Client.UseLavalink();

            Client.Ready += OnClientReady;

            _timerService.RegisterTimer(Client);
            _activityService.RegisterClient(Client);
            
            await Client.ConnectAsync();

            await lavaLink.ConnectAsync(lavalinkConfiguration);


            await Task.Delay(-1);
        }

        private async Task OnClientReady(DiscordClient client, ReadyEventArgs e)
        {
            client.Logger.LogInformation($"Client Ready");
            await _activityService.SetRandomActivity();
        }
    }
}
