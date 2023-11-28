using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using SocuciusErgallaBotv3.Services;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SocuciusErgallaBotv3.Module
{
    [SlashCommandGroup("Music", "Commands for playing music in voice channels")]
    public class MusicSlashCommandContainer : ApplicationCommandModule
    {
        private readonly MusicService _musicService;
        private readonly QuoteService _quoteService;
        private readonly ILogger<MusicSlashCommandContainer> _logger;
        private readonly DatabaseService _databaseService;
        private readonly TextInfo _textInfo = new CultureInfo("en-US", false).TextInfo;

        public MusicSlashCommandContainer(MusicService musicService, QuoteService quoteService, ILogger<MusicSlashCommandContainer> logger, DatabaseService databaseService)
        {
            _musicService = musicService;
            _quoteService = quoteService;
            _logger = logger;
            _databaseService = databaseService;
        }

        [SlashCommand("PlayRandom", "Play a random song from the database in voice channel.")]
        public async Task PlayRandomCommand(InteractionContext context, [Option("Next", "Place song first in queue. Has no effect if queue is empty.")] bool playNext = false)
        {
            _logger.LogInformation($"{context.User} used /{_textInfo.ToTitleCase(context.QualifiedName)}.");
            await context.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                IsEphemeral = true
            });

            var result = await _musicService.PlayOrQueueSong(context, playNext);
            await context.EditResponseAsync(
                new DiscordWebhookBuilder()
                    .AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle($"{result.Title}")
                        .WithAuthor($"{result.Author}")
                        .WithDescription($"{result.Message}")
                        .WithThumbnail($"{result.ThumbnailURL}")
                        .WithFooter($"{_quoteService.GetRandomQuote()}")
                        .WithUrl($"{result.URL}")
                        .WithColor(DiscordColor.Green)
                        .AddField($"Duration:", $"{result.Duration}")
                        )
                    );
        }

        [SlashCommand("Play", "Play a song in voice channel.")]
        public async Task PlayCommand(InteractionContext context,
            [Option("Query", "Query or URL for song to play.")] string query,
            [Option("Start", "In seconds, where to start playback.")] long startSeconds = 0,
            [Option("End", "In seconds, where to end playback.")] long endSeconds = 0,
            [Option("Next", "Place song first in queue. Has no effect if queue is empty.")] bool playNext = false)
        {
            _logger.LogInformation($"{context.User} used /{_textInfo.ToTitleCase(context.QualifiedName)}.");
            await context.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                IsEphemeral = true
            });

            if (startSeconds > endSeconds)
            {
                (startSeconds, endSeconds) = (endSeconds, startSeconds);
            }

            //process out time stamp from url
            Regex rx = new(@"[^lis]t=(\d*)\w*$", RegexOptions.Compiled);
            var match = rx.Match(query);
            TimeSpan startTime = TimeSpan.Zero;
            TimeSpan endTime = endSeconds > 0 ? TimeSpan.FromSeconds((int)endSeconds) : TimeSpan.Zero;

            //start time was explicitly given
            if (startSeconds != 0)
            {
                startTime = TimeSpan.FromSeconds((int)startSeconds);
            }
            //start time was captured from input url
            else if (match.Success)
            {
                startTime = TimeSpan.FromSeconds(Convert.ToInt32(match.Groups[1].Value));
                //cleanse any parameters from url
                if (Uri.IsWellFormedUriString(query, UriKind.RelativeOrAbsolute))
                {
                    query = query.Split('?')[0];
                }
            }

            var result = await _musicService.PlayOrQueueSong(context, query, startTime, endTime, playNext);
            var responseEmbed = new DiscordEmbedBuilder()
                        .WithTitle($"{result.Title}")
                        .WithAuthor($"{result.Author}")
                        .WithDescription($"{result.Message}")
                        .WithThumbnail($"{result.ThumbnailURL}")
                        .WithFooter($"{_quoteService.GetRandomQuote()}")
                        .WithUrl($"{result.URL}")
                        .WithColor(DiscordColor.Green);

            if (result.Result != CommandExecutedResult.Failure)
            {
                responseEmbed.AddField($"Duration:", $"{result.Duration}");
            }
            await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(responseEmbed));
        }
        [SlashCommand("PlayVoiceLine","Play random voiceline.")]
        public async Task PlayVoiceLine(InteractionContext context)
        {
            _logger.LogInformation($"{context.User} used /{_textInfo.ToTitleCase(context.QualifiedName)}.");
            await context.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                IsEphemeral = true
            });

            var result = await _musicService.PlayVoiceLine(context);
            var responseEmbed = new DiscordEmbedBuilder()
                        .WithTitle($"{result.Title}")
                        .WithAuthor($"{result.Author}")
                        .WithDescription($"{result.Message}")
                        .WithThumbnail($"{result.ThumbnailURL}")
                        .WithFooter($"{_quoteService.GetRandomQuote()}")
                        .WithUrl($"{result.URL}")
                        .WithColor(DiscordColor.Green);

            if (result.Result != CommandExecutedResult.Failure)
            {
                responseEmbed.AddField($"Duration:", $"{result.Duration}");
            }
            await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(responseEmbed));
        }
        [SlashCommand("Pause", "Pause currently playing song.")]
        public async Task PauseCommand(InteractionContext context)
        {
            _logger.LogInformation($"{context.User} used /{_textInfo.ToTitleCase(context.QualifiedName)}.");
            await context.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                IsEphemeral = true
            });

            //get member voice state and confirm they are in the same channel as bot
            var voiceChannel = GetUserAndBotChannel(context);
            if (voiceChannel == null)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Commands can only be executed when in same channel as bot."));
                return;
            }
            var result = await _musicService.PauseOrResume(context);
            await context.EditResponseAsync(
                result.Result == CommandExecutedResult.Success ?
                new DiscordWebhookBuilder()
                    .AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle($"{result.Title}")
                        .WithAuthor($"{result.Author}")
                        .WithDescription($"{result.Message}")
                        .WithThumbnail($"{result.ThumbnailURL}")
                        .WithFooter($"{_quoteService.GetRandomQuote()}")
                        .WithUrl($"{result.URL}")
                        .WithColor(DiscordColor.Green)
                        .AddField("Position", $"{_musicService.VoiceChannelConnection.CurrentState.PlaybackPosition:g}/{result.Duration:g}")
                        ) :
                new DiscordWebhookBuilder()
                    .AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle($"{result.Result}")
                        .WithDescription($"{result.Message}")
                        .WithColor(DiscordColor.Red)
                        )
                    );
        }

        [SlashCommand("Stop", "Stop currently playing (if any) and leave voice channel.")]
        public async Task StopCommand(InteractionContext context)
        {
            _logger.LogInformation($"{context.User} used /{_textInfo.ToTitleCase(context.QualifiedName)}.");
            await context.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                IsEphemeral = true
            });

            //get member voice state and confirm they are in the same channel as bot
            var voiceChannel = GetUserAndBotChannel(context);
            if (voiceChannel == null)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle($"{CommandExecutedResult.Failure}")
                        .WithDescription($"Bot commands can only be executed when in the same channel as the bot.")
                        .WithColor(DiscordColor.Red)
                        ));
                return;
            }

            var result = await _musicService.Stop(context);
            if (result.Result == Services.CommandExecutedResult.Success)
            {
                await context.EditResponseAsync(
                    new DiscordWebhookBuilder()
                    .AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle($"{result.Result}")
                        .WithDescription($"{result.Message}")
                        .WithColor(DiscordColor.Green)
                        .WithFooter($"{_quoteService.GetRandomQuote()}")
                        )
                    );
            }
            else
            {
                await context.EditResponseAsync(
                    new DiscordWebhookBuilder()
                    .AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle($"{result.Result}")
                        .WithDescription($"{result.Message}")
                        .WithColor(DiscordColor.Red)
                        )
                    );
            }
        }

        [SlashCommand("Skip", "Skip to next track, if available.")]
        public async Task SkipCommand(InteractionContext context)
        {
            _logger.LogInformation($"{context.User} used /{_textInfo.ToTitleCase(context.QualifiedName)}.");
            await context.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                IsEphemeral = true
            });
            //get member voice state and confirm they are in the same channel as bot
            var voiceChannel = GetUserAndBotChannel(context);
            if (voiceChannel == null)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle($"{CommandExecutedResult.Failure}")
                        .WithDescription($"Bot commands can only be executed when in the same channel as the bot.")
                        .WithColor(DiscordColor.Red)
                        ));
                return;
            }
            var result = await _musicService.Skip();
            await context.EditResponseAsync(
                result.Result == CommandExecutedResult.Success ?
                new DiscordWebhookBuilder()
                    .AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle($"{result.Title}")
                        .WithAuthor($"{result.Author}")
                        .WithDescription($"{result.Message}")
                        .WithThumbnail($"{result.ThumbnailURL}")
                        .WithFooter($"{_quoteService.GetRandomQuote()}")
                        .WithUrl($"{result.URL}")
                        .WithColor(DiscordColor.Green)
                        )
                    :
                    new DiscordWebhookBuilder()
                    .AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle($"{result.Result}")
                        .WithDescription($"{result.Message}")
                        .WithColor(DiscordColor.Red)
                        )
                    );
        }

        [SlashCommand("Volume", "Changes music volume for all users")]
        public async Task VolumeCommand(InteractionContext context, [Option("Value", "Value to change volume to. [1-100]%. Default is 15")] long vol)
        {
            _logger.LogInformation($"{context.User} used /{_textInfo.ToTitleCase(context.QualifiedName)} with vol value {vol}.");
            await context.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                IsEphemeral = true
            });
            var convertedVol = (int)vol;
            if (convertedVol < 0 || convertedVol > 100)
            {
                var errorMessage = $"Value must be between 0 and 100 inclusive.";
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                                        .AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle($"{CommandExecutedResult.Failure}")
                        .WithDescription($"{_textInfo.ToTitleCase(context.QualifiedName)} with vol value {vol} failed: {errorMessage}")
                        .WithColor(DiscordColor.Red)
                        ));
                return;
            }
            //get member voice state and confirm they are in the same channel as bot
            var voiceChannel = GetUserAndBotChannel(context);
            if (voiceChannel == null)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle($"{CommandExecutedResult.Failure}")
                        .WithDescription($"Bot commands can only be executed when in the same channel as the bot.")
                        .WithColor(DiscordColor.Red)
                        ));
                return;
            }
            //await connection.SetVolumeAsync(convertedVol);
            var result = await _musicService.SetVolume(convertedVol);
            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle($"{result.Result}")
                    .WithDescription($"{result.Message}")
                    .WithColor(result.Result == CommandExecutedResult.Success ? DiscordColor.Green : DiscordColor.Red)
                    ));
        }

        [SlashCommand("Repeat", "Changes repeat mode (single, all, none)")]
        public async Task RepeatCommand(InteractionContext context, [Choice("None", 0)][Choice("Single", 1)][Choice("All", 2)][Option("Mode", "Track repeating mode.")] long repeatMode)
        {
            _logger.LogInformation($"{context.User} used /{_textInfo.ToTitleCase(context.QualifiedName)}.");
            await context.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                IsEphemeral = true
            });
            _musicService.RepeatModeProperty = (RepeatMode)repeatMode;
            _logger.LogDebug($"RepeatMode set to: {_musicService.RepeatModeProperty}");
            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle($"{CommandExecutedResult.Success}")
                    .WithDescription($"Repeat mode set to: {_musicService.RepeatModeProperty}")
                    .WithColor(DiscordColor.Green)
                    ));
        }

        [SlashCommand("Shuffle", "Changes shuffle mode (none, playlist, endless)")]
        public async Task ShuffleCommand(InteractionContext context, [Choice("None", 0)][Choice("Playlist", 1)][Choice("Endless", 2)][Option("Mode", "Track shuffling mode.")] long shuffleMode)
        {
            _logger.LogInformation($"{context.User} used /{_textInfo.ToTitleCase(context.QualifiedName)}.");
            await context.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                IsEphemeral = true
            });
            _musicService.ShuffleModeProperty = (ShuffleMode)shuffleMode;
            _logger.LogDebug($"ShuffleMode set to: {_musicService.ShuffleModeProperty}");
            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle($"{CommandExecutedResult.Success}")
                    .WithDescription($"Shuffle mode set to: {_musicService.ShuffleModeProperty}")
                    .WithColor(DiscordColor.Green)
                    ));
        }

        [SlashCommand("Remove", "Removes track from queue at a specific index. Use the Queue command to view the list of tracks.")]
        public async Task RemoveCommand(InteractionContext context, [Option("Index", "Index of track to remove.")] long index = 1)
        {
            _logger.LogInformation($"{context.User} used /{_textInfo.ToTitleCase(context.QualifiedName)} with index value {index}.");
            await context.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                IsEphemeral = true
            });

            if (index > _musicService.TrackQueue.Count)
            {
                var errorMessage = $"The selected index is out of range.";
                _logger.LogError($"{_textInfo.ToTitleCase(context.QualifiedName)} with index value {index} failed: {errorMessage}");
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle($"{CommandExecutedResult.Failure}")
                    .WithDescription(errorMessage)
                    .WithColor(DiscordColor.Red)
                    ));
                return;
            }

            var removedTrack = _musicService.TrackQueue[(int)index - 1];
            _musicService.TrackQueue.Remove(removedTrack);
            _logger.LogInformation($"{removedTrack.Track.Title} - {removedTrack.Track.Author} removed from queue.");
            // Construct the thumbnail URL for the track
            string thumbnailUrl = $"https://img.youtube.com/vi/{removedTrack.Track.Identifier}/0.jpg";
            await context.EditResponseAsync(
                new DiscordWebhookBuilder()
                    .AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle($"{removedTrack.Track.Title}")
                        .WithAuthor($"{removedTrack.Track.Author}")
                        .WithDescription($"Removed from queue.")
                        .WithThumbnail($"{thumbnailUrl}")
                        .WithFooter($"{_quoteService.GetRandomQuote()}")
                        .WithUrl($"{removedTrack.Track.Uri}")
                        .WithColor(DiscordColor.Green)
                        ));
        }

        [SlashCommand("Queue", "Displays up to 10 tracks of current queue.")]
        public async Task QueueCommand(InteractionContext context, [Option("Start", "Start for queue display")] long startIndex = 0)
        {
            _logger.LogInformation($"{context.User} used /{_textInfo.ToTitleCase(context.QualifiedName)}.");
            await context.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                IsEphemeral = true
            });

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Player Queue")
                .WithFooter($"{_quoteService.GetRandomQuote()}");

            if (_musicService.NowPlayingTrack != null)
            {
                string thumbnailUrl = $"https://img.youtube.com/vi/{_musicService.NowPlayingTrack.Track.Identifier}/0.jpg";
                embedBuilder.WithThumbnail(thumbnailUrl);
            }
            embedBuilder.AddField("Now Playing:", _musicService.NowPlayingTrack == null ?
                "Nothing." :
                $"{_musicService.NowPlayingTrack.Track.Title} - {_musicService.NowPlayingTrack.Track.Author}\n{_musicService.VoiceChannelConnection.CurrentState.PlaybackPosition:g}/{(_musicService.NowPlayingTrack.EndTime != TimeSpan.Zero ? _musicService.NowPlayingTrack.EndTime : _musicService.NowPlayingTrack.Track.Length):g}");

            string queueString = GetQueueString(startIndex);
            embedBuilder.AddField("Queue:", queueString);

            await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embedBuilder));
        }

        private string GetQueueString(long startIndex)
        {
            StringBuilder queueSB = new StringBuilder();
            var queueCount = _musicService.TrackQueue.Count <= 10 ? _musicService.TrackQueue.Count : 10;
            queueSB.Append(queueCount == 0 ? "Empty." : string.Empty);
            int start = (int)startIndex - 1 >= 0 ? (int)startIndex - 1 : 0;
            int end = start + 10 > _musicService.TrackQueue.Count - 1 ? _musicService.TrackQueue.Count - 1 : start + 10;
            if(start != 0)
            {
                var startingTrack = _musicService.TrackQueue.First();
                queueSB.AppendLine($"1: {startingTrack.Track.Title} - {startingTrack.Track.Author}\n{startingTrack.Track.Length}");
                queueSB.AppendLine(".\n.");
            }
            // end + 1 accounts for 0 based index of queue to 1 based index for display
            for (int i = start; i < end+1; i++)
            {
                var track = _musicService.TrackQueue[i];
                queueSB.AppendLine($"{i + 1}: {track.Track.Title} - {track.Track.Author}\n{track.Track.Length}");
            }
            if (end != _musicService.TrackQueue.Count - 1)
            {
                var endingTrack = _musicService.TrackQueue.Last();
                queueSB.AppendLine(".\n.");
                queueSB.AppendLine($"{_musicService.TrackQueue.Count - 1}: {endingTrack.Track.Title} - {endingTrack.Track.Author}\n{endingTrack.Track.Length}");
            }
            return queueSB.ToString();
        }

        [SlashCommand("Seek", "Seeks to specified timespan in playing track.")]
        public async Task SeekCommand(InteractionContext context, [Option("Seconds", "Location to seek to in track")] long seekPosition)
        {
            _logger.LogInformation($"{context.User} used /{_textInfo.ToTitleCase(context.QualifiedName)}.");
            await context.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                IsEphemeral = true
            });
            var result = await _musicService.Seek(seekPosition);
            await context.EditResponseAsync(
                new DiscordWebhookBuilder()
                    .AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle($"{result.Title}")
                        .WithAuthor($"{result.Author}")
                        .WithDescription($"{result.Message}")
                        .WithThumbnail($"{result.ThumbnailURL}")
                        .WithFooter($"{_quoteService.GetRandomQuote()}")
                        .WithUrl($"{result.URL}")
                        .WithColor(result.Result == CommandExecutedResult.Success ? DiscordColor.Green : DiscordColor.Red)
                        ));
        }

        [SlashCommand("Status", "Display all relevant player data.")]
        public async Task StatusCommand(InteractionContext context)
        {
            _logger.LogInformation($"{context.User} used /{_textInfo.ToTitleCase(context.QualifiedName)}.");
            await context.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                IsEphemeral = true
            });

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Player Status")
                .WithFooter($"{_quoteService.GetRandomQuote()}");

            if (_musicService.NowPlayingTrack != null)
            {
                string thumbnailUrl = $"https://img.youtube.com/vi/{_musicService.NowPlayingTrack.Track.Identifier}/0.jpg";
                embedBuilder.WithThumbnail(thumbnailUrl);
            }
            embedBuilder.AddField("Now Playing:", _musicService.NowPlayingTrack == null ?
                "Nothing." :
                $"{_musicService.NowPlayingTrack.Track.Title} - {_musicService.NowPlayingTrack.Track.Author}\n{_musicService.VoiceChannelConnection.CurrentState.PlaybackPosition:g}/{(_musicService.NowPlayingTrack.EndTime != TimeSpan.Zero ? _musicService.NowPlayingTrack.EndTime : _musicService.NowPlayingTrack.Track.Length):g}");

            embedBuilder.AddField("Repeat:", $"{_musicService.RepeatModeProperty}.", inline: true);
            embedBuilder.AddField("Shuffle:", $"{_musicService.ShuffleModeProperty}.", inline: true);

            string queueString = GetQueueString(0);
            embedBuilder.AddField("Queue:", queueString);
            var trackHistories = await _databaseService.GetTrackHistoriesAsync();
            var topTracks = trackHistories.OrderByDescending(x => x.Plays).Take(5).ToList();
            if (topTracks != null && topTracks.Count() > 0)
            {
                string topTrackString = string.Empty;
                for (int i = 0; i < topTracks.Count; i++)
                {
                    topTrackString += $"{i + 1}: ({topTracks[i].Plays}) {topTracks[i].Title}-{topTracks[i].Author}";
                    if (i + 1 < topTracks.Count)
                    {
                        topTrackString += "\n";
                    }
                }
                embedBuilder.AddField($"Top Tracks (Plays) (Total {trackHistories.Sum(x => x.Plays)}):", topTrackString);
            }

            var users = await _databaseService.GetTopUserAsync();
            if (users != null && users.Count() > 0)
            {
                string topUsersString = string.Empty;
                for (int i = 0; i < users.Count; i++)
                {
                    topUsersString += $"{i + 1}: ({users[i].Item2}) {users[i].Item1.Username}";
                    if (i + 1 < users.Count)
                    {
                        topUsersString += "\n";
                    }
                }
                embedBuilder.AddField("Top Users (Plays):", topUsersString);
            }
            await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embedBuilder));
        }

        //get channel of user and bot, return null if user not in voice channel or bot and user not in same channel
        private DiscordChannel? GetUserAndBotChannel(InteractionContext context)
        {
            return context.Member.VoiceState != null ? context.Guild.Channels.Values.Where(x => x.Type == DSharpPlus.ChannelType.Voice && x.Users.Contains(context.Client.CurrentUser) && x == context.Member.VoiceState.Channel).FirstOrDefault() : null;
        }
    }
}
