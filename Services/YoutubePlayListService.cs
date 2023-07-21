using Google.Apis.Services;
using Google.Apis.YouTube.v3;
namespace SocuciusErgallaBotv3.Services
{
    public class YoutubePlayListService
    {
        private readonly ConfigService _configService;
        private readonly YouTubeService _youTubeService;
        public bool YoutubeServiceEnabled { get; private set; }

        public YoutubePlayListService(ConfigService configService)
        {
            _configService = configService;
            if(!string.IsNullOrEmpty(_configService.Config.ApiKey) && !string.IsNullOrEmpty(_configService.Config.ApplicationName))
            {
                YoutubeServiceEnabled = true;
            }
            _youTubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = _configService.Config.ApiKey,
                ApplicationName = _configService.Config.ApplicationName
            });

        }

        public List<string> GetPlayListURLS(string playlistUrl)
        {
            if (!YoutubeServiceEnabled)
                return new List<string>();
            string playlistId = ExtractPlaylistIdFromUrl(playlistUrl);

            var playlistItemsRequest = _youTubeService.PlaylistItems.List("snippet");
            playlistItemsRequest.PlaylistId = playlistId;
            playlistItemsRequest.MaxResults = 50;

            string nextPageToken = "";
            List<string> songURLs = new();
            do
            {
                playlistItemsRequest.PageToken = nextPageToken;
                var playlistItemsResponse = playlistItemsRequest.Execute();

                foreach (var playlistItem in playlistItemsResponse.Items)
                {
                    string videoUrl = "https://www.youtube.com/watch?v=" + playlistItem.Snippet.ResourceId.VideoId;
                    songURLs.Add(videoUrl);
                }

                nextPageToken = playlistItemsResponse.NextPageToken;

            } while (nextPageToken != null);
            return songURLs;
        }

        private string ExtractPlaylistIdFromUrl(string playlistUrl)
        {
            // Extract the playlist ID from the URL
            string playlistId = string.Empty;
            Uri uri = new Uri(playlistUrl);
            // Get query and remove '?'
            string query = uri.Query.Remove(0,1);

            if (!string.IsNullOrEmpty(query))
            {
                string[] queryParams = query.Split('&');
                foreach (string param in queryParams)
                {
                    if (param.StartsWith("list="))
                    {
                        playlistId = param.Substring(5);
                        break;
                    }
                }
            }

            return playlistId;
        }
    }
}
