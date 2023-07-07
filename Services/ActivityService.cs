using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace SocuciusErgallaBotv3.Services
{
    public class ActivityService
    {
        private DiscordClient _client;
        private readonly ILogger<ActivityService> _logger;
        private readonly System.Timers.Timer _timer;
        private readonly Random _random = new();
        private readonly List<CustomActivity> _activities = new()
        {
            new CustomActivity(){ Label = "Nerevar Rising", Duration = 114000, ActivityType = ActivityType.ListeningTo },
            new CustomActivity(){ Label = "Peaceful Waters", Duration = 185000, ActivityType = ActivityType.ListeningTo },
            new CustomActivity(){ Label = "Knight's Charge", Duration = 124000, ActivityType = ActivityType.ListeningTo },
            new CustomActivity(){ Label = "Over the Next Hill", Duration = 184000, ActivityType = ActivityType.ListeningTo },
            new CustomActivity(){ Label = "Bright Spears Dark Blood", Duration = 126000, ActivityType = ActivityType.ListeningTo },
            new CustomActivity(){ Label = "The Road Most Traveled", Duration = 195000, ActivityType = ActivityType.ListeningTo },
            new CustomActivity(){ Label = "Dance of Swords", Duration = 133000, ActivityType = ActivityType.ListeningTo },
            new CustomActivity(){ Label = "Blessing of Vivec", Duration = 196000, ActivityType = ActivityType.ListeningTo },
            new CustomActivity(){ Label = "Ambush!", Duration = 153000, ActivityType = ActivityType.ListeningTo },
            new CustomActivity(){ Label = "Silt Sunrise", Duration = 191000, ActivityType = ActivityType.ListeningTo },
            new CustomActivity(){ Label = "Hunter's Pursuit", Duration = 137000, ActivityType = ActivityType.ListeningTo },
            new CustomActivity(){ Label = "Shed Your Travails", Duration = 193000, ActivityType = ActivityType.ListeningTo },
            new CustomActivity(){ Label = "Stormclouds on the Battlefield", Duration = 131000, ActivityType = ActivityType.ListeningTo },
            new CustomActivity(){ Label = "Caprice", Duration = 207000, ActivityType = ActivityType.ListeningTo },
            new CustomActivity(){ Label = "Drumbeats of the Dunmer", Duration = 123000, ActivityType = ActivityType.ListeningTo },
            new CustomActivity(){ Label = "Darkened Depths", Duration = 50000, ActivityType = ActivityType.ListeningTo },
            new CustomActivity(){ Label = "The Prophecy Fulfilled", Duration = 71000, ActivityType = ActivityType.ListeningTo },
            new CustomActivity(){ Label = "Triumphant", Duration = 14000, ActivityType = ActivityType.ListeningTo },
            new CustomActivity(){ Label = "Introduction", Duration = 59000, ActivityType = ActivityType.ListeningTo },
            new CustomActivity(){ Label = "Fate's Quickening", Duration = 17000, ActivityType = ActivityType.ListeningTo },
            new CustomActivity(){ Label = "Vivec Arena", Duration = 300000, ActivityType = ActivityType.Competing },
            new CustomActivity(){ Label = "Registration of the Nerevarine", Duration = 300000, ActivityType = ActivityType.Watching },
            new CustomActivity(){ Label = "Fargoth", Duration = 10000, ActivityType = ActivityType.Watching },
            new CustomActivity(){ Label = "The Taxman's Tale", Duration = 10000, ActivityType = ActivityType.Playing },
            new CustomActivity(){ Label = "Death of a Taxman", Duration = 10000, ActivityType = ActivityType.Playing }
        };

        public ActivityService(ILogger<ActivityService> logger)
        {
            _logger = logger;
            _timer = new();
            _timer.Elapsed += _timer_Elapsed;
            _timer.AutoReset = false;
        }

        private async void _timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Stop();
            var newActivity = _activities[_random.Next(_activities.Count)];
            _timer.Interval = newActivity.Duration;
            await _client.UpdateStatusAsync(new DiscordActivity(newActivity.Label, newActivity.ActivityType));
            _timer.Start();
        }

        public void RegisterClient(DiscordClient client)
        {
            _client = client;
        }

        public async Task UpdateActivity(string activityMessage, ActivityType activityType)
        {
            if (_client != null)
            {
                _timer.Stop();
                await _client.UpdateStatusAsync(new DiscordActivity(activityMessage, activityType));
            }
        }

        public async Task SetRandomActivity()
        {
            if (_client != null)
            {
                var newActivity = _activities[_random.Next(_activities.Count)];
                _timer.Interval = newActivity.Duration;
                await _client.UpdateStatusAsync(new DiscordActivity(newActivity.Label, newActivity.ActivityType));
                _timer.Start();
            }
        }
    }

    class CustomActivity
    {
        public string Label { get; set; }
        public double Duration { get; set; }
        public ActivityType ActivityType { get; set; }
    }
}
