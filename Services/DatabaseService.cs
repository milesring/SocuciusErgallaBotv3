using Microsoft.Extensions.Logging;
using SocuciusErgallaBotv3.Model;
using System.Data.SQLite;

namespace SocuciusErgallaBotv3.Services
{
    public class DatabaseService
    {
        private static SQLiteConnection _connection;
        private static string _trackTableName = "tracks";
        private static string _userTableName = "users";
        private static string _userPlaysTableName = "user_plays";
        private readonly ConfigService _configService;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(ConfigService configService, ILogger<DatabaseService> logger)
        {
            Task.Run(CreateTables);
            _configService = configService;
            _logger = logger;
        }
        private async Task CreateTables()
        {
            using (_connection = new SQLiteConnection(_configService.Config.HistoryDatabase))
            {
                await _connection.OpenAsync();
                var command = _connection.CreateCommand();
                command.CommandText = $"CREATE TABLE IF NOT EXISTS {_trackTableName}" +
                    $"(id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                    $"{nameof(TrackHistory.Title).ToLower()} varchar(255), " +
                    $"{nameof(TrackHistory.Author).ToLower()} varchar(255), " +
                    $"{nameof(TrackHistory.URL).ToLower()} varchar(255))";
                await command.ExecuteNonQueryAsync();

                command.CommandText = $"CREATE TABLE IF NOT EXISTS {_userTableName} " +
                    $"(id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                    $"{nameof(User.DiscordId).ToLower()} varchar(255), " +
                    $"{nameof(User.Username).ToLower()} varchar(255))";
                await command.ExecuteNonQueryAsync();

                command.CommandText = $"CREATE TABLE IF NOT EXISTS {_userPlaysTableName} " +
                    $"(id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                    $"{nameof(User).ToLower()}_{nameof(User.Id).ToLower()} int, " +
                    $"{nameof(TrackHistory).ToLower()}_{nameof(TrackHistory.Id).ToLower()} int, " +
                    $"play_date varchar(255), " +
                    $"FOREIGN KEY ({nameof(User).ToLower()}_{nameof(User.Id).ToLower()}) REFERENCES {_userTableName}(id), " +
                    $"FOREIGN KEY ({nameof(TrackHistory).ToLower()}_{nameof(TrackHistory.Id).ToLower()}) REFERENCES {_trackTableName}(id))";
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<TrackHistory>> GetTrackHistoriesAsync()
        {
            using (_connection = new SQLiteConnection(_configService.Config.HistoryDatabase))
            {
                await _connection.OpenAsync();
                var command = _connection.CreateCommand();
                command.CommandText = $"SELECT {nameof(TrackHistory.Id).ToLower()}, {nameof(TrackHistory.Title).ToLower()}, {nameof(TrackHistory.Author).ToLower()}, {nameof(TrackHistory.URL).ToLower()} FROM {_trackTableName}";
                List<TrackHistory> trackHistories = new();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var trackHistory = new TrackHistory();
                        trackHistory.Id = reader.GetInt32(reader.GetOrdinal($"{nameof(TrackHistory.Id).ToLower()}"));
                        trackHistory.Title = reader.GetString(reader.GetOrdinal($"{nameof(TrackHistory.Title).ToLower()}"));
                        trackHistory.Author = reader.GetString(reader.GetOrdinal($"{nameof(TrackHistory.Author).ToLower()}"));
                        trackHistory.URL = reader.GetString(reader.GetOrdinal($"{nameof(TrackHistory.URL).ToLower()}"));
                        trackHistories.Add(trackHistory);
                    }
                }

                command.CommandText = $"SELECT " +
                    $"{nameof(TrackHistory).ToLower()}_{nameof(TrackHistory.Id).ToLower()}, " +
                    $"COUNT({nameof(TrackHistory).ToLower()}_{nameof(TrackHistory.Id).ToLower()}) AS {nameof(TrackHistory.Plays)} " +
                    $"FROM {_userPlaysTableName} " +
                    $"GROUP BY " +
                    $"{nameof(TrackHistory).ToLower()}_{nameof(TrackHistory.Id).ToLower()}";
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int id = reader.GetInt32(reader.GetOrdinal($"{nameof(TrackHistory).ToLower()}_{nameof(TrackHistory.Id).ToLower()}"));
                        int plays = reader.GetInt32(reader.GetOrdinal(nameof(TrackHistory.Plays)));
                        trackHistories.Where(x => x.Id == id).First().Plays = plays;
                    }
                }
                await _connection.CloseAsync();
                return trackHistories;
            }
        }

        public async Task<TrackHistory> GetTrackHistoryAsync(TrackHistory trackHistory)
        {
            using (_connection = new SQLiteConnection(_configService.Config.HistoryDatabase))
            {
                await _connection.OpenAsync();
                var command = _connection.CreateCommand();
                //Get previous track info based on url
                command.CommandText = $"SELECT * FROM {_trackTableName} WHERE {nameof(TrackHistory.URL).ToLower()} = $url";
                command.Parameters.AddWithValue("$url", trackHistory.URL);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        await AssignTrackInfo(trackHistory, reader);
                        await reader.CloseAsync();
                        return trackHistory;
                    }
                }
                //Get previous track info based on title and author. In the case where user submitted a shortened youtube link vs full
                command.CommandText = $"SELECT * FROM {_trackTableName} WHERE {nameof(TrackHistory.Title).ToLower()} = $title AND {nameof(TrackHistory.Author).ToLower()} = $author";
                command.Parameters.AddWithValue("$title", trackHistory.Title);
                command.Parameters.AddWithValue("$author", trackHistory.Author);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        await AssignTrackInfo(trackHistory, reader);
                        await reader.CloseAsync();
                        return trackHistory;
                    }
                }
                return trackHistory;
            }

        }

        public async Task<User> GetUserHistoryAsync(User user)
        {
            using (_connection = new SQLiteConnection(_configService.Config.HistoryDatabase))
            {
                await _connection.OpenAsync();
                var command = _connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {_userTableName} WHERE {nameof(User.DiscordId).ToLower()} = $discordid";
                command.Parameters.AddWithValue("$discordid", user.DiscordId);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        await reader.ReadAsync();
                        user.Id = reader.GetInt32(reader.GetOrdinal(nameof(User.Id).ToLower()));
                    }
                }
            }
            return user;
        }

        public async Task<List<(User, int)>> GetTopUserAsync()
        {
            List<(int, int)> userPlays = new();
            List<(User, int)> users = new();
            using (_connection = new SQLiteConnection(_configService.Config.HistoryDatabase))
            {
                await _connection.OpenAsync();
                var command = _connection.CreateCommand();
                command.CommandText = $"SELECT " +
                    $"{nameof(User).ToLower()}_{nameof(User.Id).ToLower()}, " +
                    $"COUNT({nameof(User).ToLower()}_{nameof(User.Id).ToLower()}) AS {nameof(TrackHistory.Plays)} " +
                    $"FROM {_userPlaysTableName} " +
                    $"GROUP BY " +
                    $"{nameof(User).ToLower()}_{nameof(User.Id).ToLower()}";
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int id = reader.GetInt32(reader.GetOrdinal($"{nameof(User).ToLower()}_{nameof(User.Id).ToLower()}"));
                            int plays = reader.GetInt32(reader.GetOrdinal(nameof(TrackHistory.Plays)));
                            //trackHistories.Where(x => x.Id == id).First().Plays = plays;
                            userPlays.Add((id, plays));
                        }
                    }
                
                foreach(var userPlay in userPlays)
                {
                    command.CommandText = $"SELECT * FROM {_userTableName} WHERE {nameof(User.Id).ToLower()} = $id";
                    command.Parameters.AddWithValue("$id", userPlay.Item1);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            
                            int id = reader.GetInt32(reader.GetOrdinal(nameof(User.Id).ToLower()));
                            string discordId = reader.GetString(reader.GetOrdinal(nameof(User.DiscordId).ToLower()));
                            string username = reader.GetString(reader.GetOrdinal(nameof(User.Username).ToLower()));
                            users.Add((new User() { Id = id, DiscordId = discordId, Username = username }, userPlay.Item2));
                        }
                    }
                }
                await _connection.CloseAsync();
            }
            return users.OrderByDescending(x=>x.Item2).Take(5).ToList();
        }

        public async Task InsertTrackPlayAsync(TrackHistory trackHistory)
        {
            //User Info
            var user = await GetUserHistoryAsync(trackHistory.User);
            if (user.Id == 0)
            {
                using (_connection = new SQLiteConnection(_configService.Config.HistoryDatabase))
                {
                    await _connection.OpenAsync();
                    var command = _connection.CreateCommand();
                    command.CommandText = $"INSERT INTO {_userTableName}({nameof(User.DiscordId).ToLower()}, {nameof(User.Username).ToLower()}) VALUES ($discordid, $username)";
                    command.Parameters.AddWithValue("$discordid", trackHistory.User.DiscordId);
                    command.Parameters.AddWithValue("$username", trackHistory.User.Username);
                    await command.ExecuteNonQueryAsync();
                    trackHistory.User.Id = Convert.ToInt32(_connection.LastInsertRowId);
                }
            }
            else
            {
                trackHistory.User.Id = user.Id;
            }

            //Track Info
            await GetTrackHistoryAsync(trackHistory);
            if (trackHistory.Id == 0)
            {
                using (_connection = new SQLiteConnection(_configService.Config.HistoryDatabase))
                {
                    await _connection.OpenAsync();
                    var command = _connection.CreateCommand();
                    command.CommandText = $"INSERT INTO {_trackTableName}({nameof(TrackHistory.Title).ToLower()}, {nameof(TrackHistory.Author).ToLower()}, {nameof(TrackHistory.URL).ToLower()}) VALUES ($title, $author, $url)";
                    command.Parameters.AddWithValue("$title", trackHistory.Title);
                    command.Parameters.AddWithValue("$author", trackHistory.Author);
                    command.Parameters.AddWithValue("$url", trackHistory.URL);
                    await command.ExecuteNonQueryAsync();
                    trackHistory.Id = Convert.ToInt32(_connection.LastInsertRowId);
                }
            }

            //Play History
            using (_connection = new SQLiteConnection(_configService.Config.HistoryDatabase))
            {
                await _connection.OpenAsync();
                var command = _connection.CreateCommand();
                command.CommandText = $"INSERT INTO {_userPlaysTableName}({nameof(User).ToLower()}_{nameof(User.Id).ToLower()}, {nameof(TrackHistory).ToLower()}_{nameof(TrackHistory.Id).ToLower()}, play_date) VALUES ($userid, $trackid, $date)";
                command.Parameters.AddWithValue("$userid", trackHistory.User.Id);
                command.Parameters.AddWithValue("$trackid", trackHistory.Id);
                command.Parameters.AddWithValue("$date", DateTime.Now);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task AssignTrackInfo(TrackHistory trackHistory, System.Data.Common.DbDataReader reader)
        {
            while (await reader.ReadAsync())
            {
                trackHistory.Id = reader.GetInt32(reader.GetOrdinal($"{nameof(TrackHistory.Id).ToLower()}"));
                trackHistory.Author = trackHistory.Author.Equals(reader.GetString(reader.GetOrdinal(nameof(TrackHistory.Author).ToLower()))) ? trackHistory.Author : reader.GetString(reader.GetOrdinal(nameof(TrackHistory.Author).ToLower()));
                trackHistory.Title = trackHistory.Title.Equals(reader.GetString(reader.GetOrdinal(nameof(TrackHistory.Title).ToLower()))) ? trackHistory.Title : reader.GetString(reader.GetOrdinal(nameof(TrackHistory.Title).ToLower()));
            }
        }


    }
}
