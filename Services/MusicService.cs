using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using SocuciusErgallaBotv3.Model;

namespace SocuciusErgallaBotv3.Services
{
    public class MusicService
    {
        private readonly DatabaseService _databaseService;
        private readonly ILogger<MusicService> _logger;
        private readonly ActivityService _activityService;
        private readonly Random _random;
        public const int DefaultVolume = 15;
        public bool InChannel { get; set; } = false;
        public bool IsPlaying { get; set; } = false;
        public LavalinkGuildConnection VoiceChannelConnection { get; set; }
        public List<QueuedTrack> TrackQueue { get; set; } = new();
        public QueuedTrack NowPlayingTrack;
        public RepeatMode RepeatModeProperty { get; set; } = RepeatMode.None;
        public ShuffleMode ShuffleModeProperty { get; set; } = ShuffleMode.None;

        public MusicService(DatabaseService databaseService, ILogger<MusicService> logger, ActivityService activityService)
        {
            _databaseService = databaseService;
            _logger = logger;
            _activityService = activityService;
            _random = new();
        }

        private LavalinkNodeConnection GetLavalinkNodeConnection(InteractionContext context)
        {
            return context.Client.GetLavalink().ConnectedNodes.Values.First();
        }

        private async Task<PlayResult> JoinChannel(InteractionContext context)
        {
            string errorMessage = string.Empty;
            var lavaLink = context.Client.GetLavalink();
            if (!lavaLink.ConnectedNodes.Any())
            {
                //return lavalink not connected error
                errorMessage = $"Error joining channel. Lavalink connection not established.";
                _logger.LogError(errorMessage);
                return new PlayResult() { Result = CommandExecutedResult.Failure, Message = errorMessage };
            }

            //get connected node
            var node = GetLavalinkNodeConnection(context);

            //set track starting and ending events
            node.PlaybackStarted += Node_PlaybackStarted;
            node.PlaybackFinished += Node_PlaybackFinished;

            //get user voice channel
            var voiceChannel = context.Member.VoiceState != null ? context.Member.VoiceState.Channel : null;
            if (voiceChannel != null)
            {
                //connect to channel
                var conn = await node.ConnectAsync(voiceChannel);

                if (conn != null)
                {
                    //return voice channel connection not available
                    InChannel = true;
                    VoiceChannelConnection = conn;
                    //set default volume since first join
                    await VoiceChannelConnection.SetVolumeAsync(DefaultVolume);
                    _logger.LogDebug($"Successfully joined channel: {voiceChannel.Name}");
                    return new PlayResult() { Result = CommandExecutedResult.Success };
                }
            }
            errorMessage = $"Cannot join voice channel. {context.User.Username} not in voice channel.";
            _logger.LogError(errorMessage);
            return new PlayResult() { Result = CommandExecutedResult.Failure, Message = errorMessage };
        }

        private async Task Node_PlaybackFinished(LavalinkGuildConnection sender, DSharpPlus.Lavalink.EventArgs.TrackFinishEventArgs args)
        {
            _logger.LogDebug($"Playback finished. Reason:{args.Reason}");
            //track ended and queue has tracks remaining or repeat is set
            if (args.Reason == DSharpPlus.Lavalink.EventArgs.TrackEndReason.Finished && (TrackQueue.Count > 0 || (RepeatModeProperty != RepeatMode.None && NowPlayingTrack != null)))
            {
                switch (RepeatModeProperty)
                {
                    case RepeatMode.None:
                        break;
                    case RepeatMode.Single:
                        TrackQueue.Insert(0, NowPlayingTrack);
                        break;
                    case RepeatMode.All:
                        TrackQueue.Add(NowPlayingTrack);
                        break;
                    default:
                        break;
                }

                //remove next track
                var removedTrack = TrackQueue.First();
                TrackQueue.Remove(removedTrack);
                NowPlayingTrack = removedTrack;
                if (removedTrack.StartTime != TimeSpan.Zero || removedTrack.EndTime != TimeSpan.Zero)
                {
                    await sender.PlayPartialAsync(removedTrack.Track, removedTrack.StartTime, removedTrack.EndTime);
                    _logger.LogInformation($"Track playing from queue from {removedTrack.StartTime:g}-{removedTrack.EndTime:g}: {removedTrack.Track.Title} - {removedTrack.Track.Author}.");
                }
                else
                {
                    await sender.PlayAsync(removedTrack.Track);
                    _logger.LogInformation($"Track playing from queue: {removedTrack.Track.Title} - {removedTrack.Track.Author}");
                }
                await SaveTrackInformationToDatabase(NowPlayingTrack);
                IsPlaying = true;
            }
            //queue is empty, but shuffle is set to endless mode
            else if (args.Reason == DSharpPlus.Lavalink.EventArgs.TrackEndReason.Finished && TrackQueue.Count == 0 && ShuffleModeProperty == ShuffleMode.Endless)
            {
                NowPlayingTrack = null;
                IsPlaying = false;
                _logger.LogDebug($"Track queue empty, but shuffle mode is endless. Shuffling in new tracks.");
                await QueueRandomTracks();
                if (NowPlayingTrack != null)
                {
                    await SaveTrackInformationToDatabase(NowPlayingTrack);
            }
            }
            else if (args.Reason == DSharpPlus.Lavalink.EventArgs.TrackEndReason.Replaced)
            {
                IsPlaying = true;
            }
            else
            {
                NowPlayingTrack = null;
                IsPlaying = false;
                await _activityService.SetRandomActivity();
            }
        }

        private async Task SaveTrackInformationToDatabase(QueuedTrack track)
        {
            await _databaseService.InsertTrackPlayAsync(new TrackHistory()
            {
                Title = track.Track.Title,
                Author = track.Track.Author,
                URL = track.Track.Uri.ToString(),
                User = track.User
            });
        }
        private async Task Node_PlaybackStarted(LavalinkGuildConnection sender, DSharpPlus.Lavalink.EventArgs.TrackStartEventArgs args)
        {
            await _activityService.UpdateActivity($"{NowPlayingTrack.Track.Title}-{NowPlayingTrack.Track.Author}", ActivityType.ListeningTo);
        }

        //plays random track from database
        public async Task<PlayResult> PlayOrQueueSong(InteractionContext context, bool next)
        {
            if (!InChannel)
            {
                var result = await JoinChannel(context);
                if (result.Result == CommandExecutedResult.Failure)
                {
                    return result;
                }
            }
            var allTracks = await _databaseService.GetTrackHistoriesAsync();
            TrackHistory randomTrackFromHistory;
            LavalinkLoadResult loadResult;
            do
            {
                randomTrackFromHistory = allTracks.OrderBy(x => _random.Next()).First();
                loadResult = await GetLavalinkNodeConnection(context).Rest.GetTracksAsync(randomTrackFromHistory.URL, LavalinkSearchType.Plain);

                switch (loadResult.LoadResultType)
                {
                    case LavalinkLoadResultType.LoadFailed:
                        return new PlayResult() { Result = CommandExecutedResult.Failure, Message = "Lavalink loading track failure." }; ;
                    case LavalinkLoadResultType.NoMatches:
                        _logger.LogError($"History: {randomTrackFromHistory.Title} - {randomTrackFromHistory.Author}: {randomTrackFromHistory.URL} search returned no results.");
                        break;
                    default:
                        //all other results are successes
                        break;
                }
                //search is from history, can not return a no match search, redo
            } while (loadResult.LoadResultType == LavalinkLoadResultType.NoMatches);

            var track = loadResult.Tracks.First();

            if (NowPlayingTrack == null || NowPlayingTrack.Track == null)
            {
                await VoiceChannelConnection.PlayAsync(track);
                NowPlayingTrack = new QueuedTrack()
                {
                    Track = track,
                    StartTime = TimeSpan.Zero,
                    EndTime = TimeSpan.Zero
                };
                IsPlaying = true;
            }
            else
            {
                //queue
                QueuedTrack trackToQueue = new()
                {
                    Track = track,
                    StartTime = TimeSpan.Zero,
                    EndTime = TimeSpan.Zero
                };
                if (next)
                {
                    TrackQueue.Insert(0, trackToQueue);
                }
                else
                {
                    TrackQueue.Add(trackToQueue);
                }
            }

            //search database and insert track play
            await _databaseService.InsertTrackPlayAsync(new TrackHistory()
            {
                Title = randomTrackFromHistory.Title,
                Author = randomTrackFromHistory.Author,
                URL = randomTrackFromHistory.URL,
                User = new User()
                {
                    Username = context.User.Username,
                    DiscordId = context.User.Id.ToString()
                }
            });
            string trackAction = NowPlayingTrack.Track == track ? "Track playing" : "Track queued";
            _logger.LogInformation($"{trackAction}: {track.Title} - {track.Author}");
            return new PlayResult()
            {
                Result = CommandExecutedResult.Success,
                Message = $"{trackAction}.",
                Title = track.Title,
                Author = track.Author,
                URL = track.Uri.ToString(),
                Duration = track.Length,
                ThumbnailURL = $"https://img.youtube.com/vi/{track.Identifier}/0.jpg"
            };
        }

        public async Task<PlayResult> PlayOrQueueSong(InteractionContext context, string query, TimeSpan startTime, TimeSpan endTime, bool next)
        {
            if (!InChannel)
            {
                var result = await JoinChannel(context);
                if (result.Result == CommandExecutedResult.Failure)
                {
                    return result;
                }
            }
            LavalinkSearchType searchType = LavalinkSearchType.Youtube;
            Uri uriResult;
            bool isQueryURLResult = Uri.TryCreate(query, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (isQueryURLResult)
            {
                searchType = LavalinkSearchType.Plain;
            }
            var loadResult = await GetLavalinkNodeConnection(context).Rest.GetTracksAsync(query, searchType);
            switch (loadResult.LoadResultType)
            {
                case LavalinkLoadResultType.LoadFailed:
                    return new PlayResult() { Result = CommandExecutedResult.Failure, Message = "Lavalink loading track failure." }; ;
                case LavalinkLoadResultType.NoMatches:
                    return new PlayResult() { Result = CommandExecutedResult.Failure, Message = "Track search returned no matches." }; ;
                default:
                    //all other results are successes
                    break;
            }
                tracks.Add(loadResult.Tracks.First());
            }
            User user = new User()
            {
                Username = context.User.Username,
                DiscordId = context.User.Id.ToString()
            };
            foreach (var track in tracks)
            {
            if (NowPlayingTrack == null)
            {
                TimeSpan start = startTime != TimeSpan.Zero ? startTime : TimeSpan.Zero;
                TimeSpan end = endTime != TimeSpan.Zero ? (endTime > track.Length ? track.Length : endTime) : track.Length;

                await VoiceChannelConnection.PlayPartialAsync(track, start, end);
                IsPlaying = true;
                NowPlayingTrack = new QueuedTrack()
                {
                    Track = track,
                    StartTime = startTime,
                        EndTime = endTime,
                        User = user
                };
            }
            else
            {
                QueuedTrack trackToQueue = new()
                {
                    Track = track,
                    StartTime = startTime,
                        EndTime = endTime,
                        User = user
                };
                if (next)
                {
                    TrackQueue.Insert(0, trackToQueue);
                }
                else
                {
                    TrackQueue.Add(trackToQueue);
                }
            }

            //search database and insert track play
            await _databaseService.InsertTrackPlayAsync(new TrackHistory()
            {
                Title = track.Title,
                Author = track.Author,
                URL = track.Uri.ToString(),
                User = new User()
                {
                    Username = context.User.Username,
                    DiscordId = context.User.Id.ToString()
                }
            });
            string trackAction = NowPlayingTrack.Track == track ? "Track playing" : "Track queued";
            string timeString = startTime != TimeSpan.Zero || endTime != TimeSpan.Zero ? $" from {startTime:g}-{endTime:g}" : string.Empty;
            _logger.LogInformation($"{trackAction}{timeString}: {track.Title} - {track.Author}");
            return new PlayResult()
            {
                Result = CommandExecutedResult.Success,
                Message = $"{trackAction}{timeString}.",
                Title = track.Title,
                Author = track.Author,
                URL = track.Uri.ToString(),
                Duration = track.Length,
                ThumbnailURL = $"https://img.youtube.com/vi/{track.Identifier}/0.jpg"
            };
        }

        private async Task PlayOrQueueSong(TrackHistory trackHistory)
        {
            if (!InChannel)
            {
                return;
            }

            LavalinkLoadResult loadResult = await VoiceChannelConnection.Node.Rest.GetTracksAsync(trackHistory.URL, LavalinkSearchType.Plain);
            switch (loadResult.LoadResultType)
            {
                case LavalinkLoadResultType.LoadFailed:
                    _logger.LogError($"History: {trackHistory.Title} - {trackHistory.Author}: {trackHistory.URL} returned an error connecting to Lavalink.");
                    return;
                case LavalinkLoadResultType.NoMatches:
                    _logger.LogError($"History: {trackHistory.Title} - {trackHistory.Author}: {trackHistory.URL} search returned no results.");
                    return;
                default:
                    //all other results are successes
                    break;
            }

            var track = loadResult.Tracks.First();
            User user = new User()
            {
                Username = VoiceChannelConnection.Guild.CurrentMember.Username,
                DiscordId = VoiceChannelConnection.Guild.CurrentMember.Id.ToString()
            };
            if (NowPlayingTrack == null || NowPlayingTrack.Track == null)
            {
                await VoiceChannelConnection.PlayAsync(track);
                NowPlayingTrack = new QueuedTrack()
                {
                    Track = track,
                    StartTime = TimeSpan.Zero,
                    EndTime = TimeSpan.Zero,
                    User = user
                };
                IsPlaying = true;
            }
            else
            {
                //queue
                QueuedTrack trackToQueue = new()
                {
                    Track = track,
                    StartTime = TimeSpan.Zero,
                    EndTime = TimeSpan.Zero,
                    User = user
                };
                TrackQueue.Add(trackToQueue);
            }

            //search database and insert track play
            await _databaseService.InsertTrackPlayAsync(new TrackHistory()
            {
                Title = trackHistory.Title,
                Author = trackHistory.Author,
                URL = trackHistory.URL,
                User = new User()
                {
                    Username = VoiceChannelConnection.Guild.CurrentMember.Username,
                    DiscordId = VoiceChannelConnection.Guild.CurrentMember.Id.ToString()
                }
            });
            string trackAction = NowPlayingTrack.Track == track ? "Track playing" : "Track queued";
            _logger.LogInformation($"{trackAction}: {track.Title} - {track.Author}");
        }

        public async Task<CommandResult> Stop(InteractionContext context)
        {
            if (InChannel)
            {
                await VoiceChannelConnection.DisconnectAsync();
                var node = GetLavalinkNodeConnection(context);
                //clear track starting and ending events
                node.PlaybackStarted -= Node_PlaybackStarted;
                node.PlaybackFinished -= Node_PlaybackFinished;
                InChannel = false;
                NowPlayingTrack = null;
                RepeatModeProperty = RepeatMode.None;
                ShuffleModeProperty = ShuffleMode.None;
                TrackQueue.Clear();
                await _activityService.SetRandomActivity();
                return new CommandResult() { Result = CommandExecutedResult.Success, Message = $"Successfully left channel." };
            }
            return new CommandResult() { Result = CommandExecutedResult.Failure, Message = $"Bot is not currently in a channel." };
        }

        public async Task StopAndLeaveChannel()
        {
            if (InChannel)
            {
                await VoiceChannelConnection.DisconnectAsync();
                VoiceChannelConnection.Node.PlaybackStarted -= Node_PlaybackStarted;
                VoiceChannelConnection.Node.PlaybackFinished -= Node_PlaybackFinished;
                InChannel = false;
                NowPlayingTrack = null;
                RepeatModeProperty = RepeatMode.None;
                ShuffleModeProperty = ShuffleMode.None;
                TrackQueue.Clear();
                await _activityService.SetRandomActivity();
            }
        }

        public async Task<PlayResult> PauseOrResume(InteractionContext context)
        {
            if (InChannel && NowPlayingTrack != null)
            {
                if (IsPlaying)
                {
                    await VoiceChannelConnection.PauseAsync();
                    _logger.LogInformation("Playback paused.");
                    IsPlaying = false;
                    return new PlayResult()
                    {
                        Result = CommandExecutedResult.Success,
                        Message = $"Paused playback.",
                        Title = NowPlayingTrack.Track.Title,
                        Author = NowPlayingTrack.Track.Author,
                        URL = NowPlayingTrack.Track.Uri.ToString(),
                        Duration = NowPlayingTrack.EndTime != TimeSpan.Zero ? NowPlayingTrack.EndTime : NowPlayingTrack.Track.Length,
                        ThumbnailURL = $"https://img.youtube.com/vi/{NowPlayingTrack.Track.Identifier}/0.jpg"
                    };
                }
                else
                {
                    await VoiceChannelConnection.ResumeAsync();
                    _logger.LogInformation("Playback resumed.");
                    IsPlaying = true;
                    return new PlayResult()
                    {
                        Result = CommandExecutedResult.Success,
                        Message = $"Resumed playback.",
                        Title = NowPlayingTrack.Track.Title,
                        Author = NowPlayingTrack.Track.Author,
                        URL = NowPlayingTrack.Track.Uri.ToString(),
                        Duration = NowPlayingTrack.EndTime != TimeSpan.Zero ? NowPlayingTrack.EndTime : NowPlayingTrack.Track.Length,
                        ThumbnailURL = $"https://img.youtube.com/vi/{NowPlayingTrack.Track.Identifier}/0.jpg"
                    };
                }
            }
            return new PlayResult()
            {
                Result = CommandExecutedResult.Failure,
                Message = $"Bot not in channel or nothing currently playing"
            };
        }

        public async Task<PlayResult> Skip()
        {
            if (InChannel && NowPlayingTrack != null && TrackQueue.Count > 0)
            {
                var newTrack = TrackQueue.First();
                TrackQueue.Remove(newTrack);
                TimeSpan start = newTrack.StartTime != TimeSpan.Zero ? newTrack.StartTime : TimeSpan.Zero;
                TimeSpan end = newTrack.EndTime != TimeSpan.Zero ? newTrack.EndTime : newTrack.Track.Length;
                await VoiceChannelConnection.PlayPartialAsync(newTrack.Track, start, end);
                string timeString = newTrack.StartTime != TimeSpan.Zero || newTrack.EndTime != newTrack.EndTime ? $" from {start:g}-{end:g}" : string.Empty;
                _logger.LogInformation($"Skipped to track{timeString}: {newTrack.Track.Title} - {newTrack.Track.Author}");
                NowPlayingTrack = newTrack;
                IsPlaying = true;
                return new PlayResult()
                {
                    Result = CommandExecutedResult.Success,
                    Message = $"Skipped to track{timeString}.",
                    Title = newTrack.Track.Title,
                    Author = newTrack.Track.Author,
                    URL = newTrack.Track.Uri.ToString(),
                    Duration = newTrack.Track.Length,
                    ThumbnailURL = $"https://img.youtube.com/vi/{newTrack.Track.Identifier}/0.jpg"
                };
            }
            else if (InChannel && NowPlayingTrack != null && TrackQueue.Count == 0 && ShuffleModeProperty == ShuffleMode.Endless)
            {
                await QueueRandomTracks();
                var newTrack = TrackQueue.First();
                TrackQueue.Remove(newTrack);
                await VoiceChannelConnection.PlayAsync(newTrack.Track);
                _logger.LogDebug($"Skipped track, but queue was empty. Shuffled in new songs.");
                _logger.LogDebug($"Skipped to track {newTrack.Track.Title} - {newTrack.Track.Author}");
                NowPlayingTrack = newTrack;
                IsPlaying = true;
                return new PlayResult()
                {
                    Result = CommandExecutedResult.Success,
                    Message = $"Skipped to track.",
                    Title = newTrack.Track.Title,
                    Author = newTrack.Track.Author,
                    URL = newTrack.Track.Uri.ToString(),
                    Duration = newTrack.Track.Length,
                    ThumbnailURL = $"https://img.youtube.com/vi/{newTrack.Track.Identifier}/0.jpg"
                };
            }
            return new PlayResult()
            {
                Result = CommandExecutedResult.Failure,
                Message = "Nothing playing or queue is empty"
            };
        }

        public async Task<CommandResult> SetVolume(int volume = DefaultVolume)
        {
            if (InChannel)
            {
                await VoiceChannelConnection.SetVolumeAsync(volume);
                _logger.LogInformation($"Volume set to {volume}.");
                return new CommandResult()
                {
                    Result = CommandExecutedResult.Success,
                    Message = $"Volume set to {volume}."
                };
            }
            return new CommandResult()
            {
                Result = CommandExecutedResult.Failure,
                Message = "Bot not in voice channel."
            };
        }

        public async Task<PlayResult> Seek(long position)
        {
            if (InChannel && IsPlaying)
            {
                TimeSpan newPosition = TimeSpan.FromSeconds(position);
                if (newPosition > NowPlayingTrack.Track.Length)
                {
                    return new PlayResult() { Result = CommandExecutedResult.Failure, Message = $"{newPosition:g} exceeds current track length of {NowPlayingTrack.Track.Length:g}." };
                }
                await VoiceChannelConnection.SeekAsync(newPosition);
                return new PlayResult()
                {
                    Result = CommandExecutedResult.Success,
                    Message = $"Set position to {newPosition:g}.",
                    Title = NowPlayingTrack.Track.Title,
                    Author = NowPlayingTrack.Track.Author,
                    URL = NowPlayingTrack.Track.Uri.ToString(),
                    Duration = NowPlayingTrack.Track.Length,
                    ThumbnailURL = $"https://img.youtube.com/vi/{NowPlayingTrack.Track.Identifier}/0.jpg"
                };
            }
            return new PlayResult() { Result = CommandExecutedResult.Failure, Message = "Nothing currently playing." };
        }

        public async Task<CommandResult> Shuffle()
        {

            switch (ShuffleModeProperty)
            {
                case (ShuffleMode.None):
                    //do nothing
                    break;
                case (ShuffleMode.Playlist):
                    //shuffle current playlist
                    TrackQueue = TrackQueue.OrderBy(x => _random.Next()).ToList();
                    break;
                case (ShuffleMode.Endless):
                    //shuffle current playlist and set property for future.
                    TrackQueue = TrackQueue.OrderBy(x => _random.Next()).ToList();
                    break;
                default:
                    break;
            }
            return new CommandResult() { Result = CommandExecutedResult.Failure, Message = $"Changing shuffle mode to {ShuffleModeProperty} failed." };
        }

        private async Task QueueRandomTracks(int amount = 1)
        {

            var tracks = (await _databaseService.GetTrackHistoriesAsync()).ToList();
            HashSet<int> generatedIndices = new HashSet<int>();
            for (int i = 0; i < amount; i++)
            {
                int randomIndex;
                do
                {
                    randomIndex = _random.Next(tracks.Count);
                } while (generatedIndices.Contains(randomIndex));
                
                generatedIndices.Add(randomIndex);
                await PlayOrQueueSong(tracks[randomIndex]);
            }
        }
    }

    public class CommandResult
    {
        public string Message { get; set; }
        public CommandExecutedResult Result { get; set; }
    }

    public class PlayResult : CommandResult
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string URL { get; set; }
        public string ThumbnailURL { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public enum CommandExecutedResult
    {
        Success,
        Failure
    }

    public enum RepeatMode
    {
        None,
        Single,
        All
    }

    public enum ShuffleMode
    {
        None,
        Playlist,
        Endless
    }

    public class QueuedTrack
    {
        public LavalinkTrack Track { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public User User { get; set; }

    }

}
