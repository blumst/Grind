using Flurl.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Options;
using GrindSoft.Interface;
using GrindSoft.Settings; 

namespace GrindSoft.Services
{
    public class DiscordClient : IDiscordClient
    {
        private readonly DiscordSettings _discordSettings;

        private readonly Dictionary<string, string> _headers;

        public string AuthorId { get; private set; }
        private string _accessToken;
        private string _channelId;
        private string _userAgent;
        private string _serverId;

        public DiscordClient(IOptions<DiscordSettings> discordSettings)
        {
            _discordSettings = discordSettings.Value;

            _headers = new Dictionary<string, string>
            {
                { "Authority", "discord.com" },
                { "Accept",  "*/*" },
                { "Accept-Language",  "en-US,en;q=0.9,uk;q=0.8,ru;q=0.7"},
                { "DNT", "1" },
                { "Origin", "https://discord.com" },
                { "Priority", "u=1, i" },
                { "Sec-CH-UA", "\"Not/A)Brand\";v=\"8\", \"Chromium\";v=\"126\", \"Google Chrome\";v=\"126\"" },
                { "Sec-CH-UA-Platform", "\"Windows\"" },
                { "Sec-Fetch-Dest", "empty" },
                { "Sec-Fetch-Mode", "cors" },
                { "Sec-Fetch-Site", "same-origin" },
                { "X-Discord-Locale", "en-US" },
                { "X-Discord-Timezone", "Europe/Kyiv" }
            };
        }

        public void UpdateData(string accessToken, string channelId, string serverId, string userAgent)
        {
            _accessToken = accessToken;
            _channelId = channelId;
            _serverId = serverId;
            _userAgent = userAgent;
        }

        public async Task FetchUserIdAsync()
        {
            try
            {
                var userInfoUrl = "https://discord.com/api/v9/users/@me";
                var response = await userInfoUrl
                    .WithHeader("Authorization", _accessToken)
                    .GetStringAsync();

                var jsonResponse = JsonConvert.DeserializeObject<JObject>(response);
                AuthorId = jsonResponse["id"].ToString();
            }
            catch (FlurlHttpException ex)
            {
                Console.WriteLine($"Не вдалося отримати ідентифікатор користувача: {ex.Message}");
            }
        }

        public async Task SendMessageAsync(string message)
        {
            string url = $"{_discordSettings.BaseUrl}/{_channelId}/messages";

            var payload = new
            {
                mobile_network_type = "unknown",
                content = message,
                nonce = GenerateNonce(),
                tts = false,
                flags = 0
            };

            _ = SendTypingAsync();

            await url
                .WithHeaders(_headers)
                .WithHeader("Authorization", _accessToken)
                .WithHeader("Referer", $"https://discord.com/channels/{_serverId}/{_channelId}")
                .WithHeader("User-Agent", _userAgent)
                .PostJsonAsync(payload);
        }

        public async Task SendTypingAsync()
        {
            string url = $"{_discordSettings.BaseUrl}/{_channelId}/typing";

            await url
                .WithHeaders(_headers)
                .WithHeader("Authorization", _accessToken)
                .WithHeader("Referer", $"https://discord.com/channels/{_serverId}/{_channelId}")
                .WithHeader("User-Agent", _userAgent)
                .PostAsync();

            Random random = new();
            int delay = (int)((random.NextDouble() + random.NextDouble() + random.NextDouble()) * 1000);
            await Task.Delay(delay);
        }

        public async Task<List<(string AuthorId, string Content, string MessageId)>> GetLatestMessagesAsync()
        {
            var messages = await GetMessageHistory(_accessToken, _channelId);
            return messages;
        }

        private static string GenerateNonce()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        }

 
        private static async Task<string> DownloadAccountPageAsync(string url, string authToken)
        {
            try
            {
                var responseBody = await url
                    .WithHeader("Authorization", authToken)
                    .GetStringAsync();

                return responseBody;
            }
            catch (FlurlHttpException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return null;
            }
        }

        private static List<(string AuthorId, string Content, string MessageId)> ParseJson(string jsonResponse)
        {
            JArray messages = JArray.Parse(jsonResponse);
            var messagesList = new List<(string, string, string)>();

            foreach (var message in messages)
            {
                string content = message["content"].ToString();
                string authorId = message["author"]["id"].ToString();
                string messageId = message["id"].ToString();

                messagesList.Add((authorId, content, messageId));
            }

            return messagesList;
        }

        private async Task<List<(string AuthorId, string Content, string MessageId)>> GetMessageHistory(string token, string channelId)
        {
            string accountUrl = $"{_discordSettings.BaseUrl}/{channelId}/messages";
            string pageContent = await DownloadAccountPageAsync(accountUrl, token);

            if (pageContent != null)
            {
                return ParseJson(pageContent);
            }
            else
            {
                Console.WriteLine("Unable to retrieve page content.");
                return null;
            }
        }
    }
}
