using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SocuciusErgallaBotv3.Services
{
    public class ConfigService
    {
        private string _configFolder = "Resources";
        private string _configFile = "config.json";
        private readonly ILogger<ConfigService> _logger;

        public BotConfig Config { get; private set; }

        public ConfigService(ILogger<ConfigService> logger)
        {
            _logger = logger;
            if (!Directory.Exists(_configFolder))
            {
                Directory.CreateDirectory(_configFolder);
                _logger.LogInformation($"Config folder created: {_configFolder}");
            }
            var configPath = Path.Combine(_configFolder, _configFile);
            if (!File.Exists(configPath))
            {
                Config = new BotConfig();
                var json = JsonConvert.SerializeObject(Config, Formatting.Indented);
                File.WriteAllText(configPath, json);
                _logger.LogInformation($"Config file created: {_configFile}, please fill in correct info and restart");
            }
            else
            {
                var json = File.ReadAllText(configPath);
                Config = JsonConvert.DeserializeObject<BotConfig>(json);
                _logger.LogInformation($"Config file loaded: {_configFile}");
            }
        }
    }

    public class BotConfig
    {
        //bot token for authentication from Discord
        [JsonProperty("token")]
        public string Token { get; private set; }
        //name of database
        [JsonProperty("historydatabase")]
        public string HistoryDatabase { get; private set; }
        
        //hostname for lavalink
        [JsonProperty("hostname")]
        public string Hostname { get; private set; }

        //port for lavalink
        [JsonProperty("port")]
        public int Port { get; private set; }
        //password for lavalink
        [JsonProperty("password")]
        public string Password { get; private set; }
        [JsonProperty("guildid")]
        public ulong GuildId { get; private set; }
        [JsonProperty("googleapikey")]
        public string ApiKey { get; private set; }
        [JsonProperty("googleapplicationname")]
        public string ApplicationName { get;private set; }
    }
}
