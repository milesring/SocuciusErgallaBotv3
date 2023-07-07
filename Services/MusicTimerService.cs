using DSharpPlus;
using Microsoft.Extensions.Logging;

namespace SocuciusErgallaBotv3.Services
{
    public class MusicTimerService
    {
        private readonly ILogger<MusicTimerService> _logger;
        private readonly MusicService _musicService;
        private readonly System.Timers.Timer _timer;
        private bool _timerRunning;
        private readonly double _timerDuration = 10000;

        public MusicTimerService(ILogger<MusicTimerService> logger, MusicService musicService)
        {
            _logger = logger;
            _musicService = musicService;
            _timer = new(_timerDuration);
            _timer.Elapsed += _timer_Elapsed;
            _timer.AutoReset = false;
        }

        private async void _timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            _timerRunning = false;
            await _musicService.StopAndLeaveChannel();
            _logger.LogDebug($"Leave timer elapsed.");
        }

        public void RegisterTimer(DiscordClient client)
        {
            client.VoiceStateUpdated += Client_VoiceStateUpdated;
            _logger.LogInformation($"Client VoiceStateUpdated registered.");
        }

        private Task Client_VoiceStateUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.VoiceStateUpdateEventArgs args)
        {
            if (!args.User.IsBot 
                && _musicService.InChannel)
            {
                if(args.Before != null && args.Before.Channel == _musicService.VoiceChannelConnection.Channel && args.Before.Channel.Users.Count == 1)
                {
                    _logger.LogDebug($"User left bot channel.");
                    //start leave timer
                    _timer.Interval = _timerDuration;
                    _timer.Start();
                    _timerRunning = true;
                    _logger.LogDebug($"Leave timer started.");
                }else if(args.After != null && args.After.Channel == _musicService.VoiceChannelConnection.Channel && args.After.Channel.Users.Count > 1)
                {
                    //stop leave timer
                    _logger.LogDebug($"User joined bot channel.");
                    if (_timerRunning)
                    {
                        _timer.Stop();
                        _timerRunning = false;
                        _logger.LogDebug($"Leave timer stopped.");
                    }
                }
            }
            return Task.CompletedTask;
        }
    }
}
