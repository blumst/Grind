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
        private SessionContext _sessionContext;

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

        public void UpdateData(SessionContext sessionContext) => _sessionContext = sessionContext;

        public async Task<string> FetchUserIdAsync()
        {
            try
            {
                var userInfoUrl = "https://discord.com/api/v9/users/@me";
                var response = await userInfoUrl
                    .WithHeader("Authorization", _sessionContext.AccessToken)
                    .GetStringAsync();

                var jsonResponse = JsonConvert.DeserializeObject<JObject>(response);
                var authorId = jsonResponse["id"].ToString();

                return authorId;
            }
            catch (FlurlHttpException ex)
            {
                Console.WriteLine($"НFailed to retrieve user ID: {ex.Message}");
                return null;
            }
        }

        public async Task SendMessageAsync(string message)
        {
            string url = $"{_discordSettings.BaseUrl}/{_sessionContext.ChannelId}/messages";

            var payload = new
            {
                mobile_network_type = "unknown",
                content = message,
                nonce = GenerateNonce(),
                tts = false,
                flags = 0
            };

            await url
                .WithHeaders(_headers)
                .WithHeader("Authorization", _sessionContext.AccessToken)
                .WithHeader("Referer", $"https://discord.com/channels/{_sessionContext.ServerId}/{_sessionContext.ChannelId}")
                .WithHeader("User-Agent", _sessionContext.UserAgent)
                .PostJsonAsync(payload);
        }

        public async Task SendTypingAsync()
        {
            string url = $"{_discordSettings.BaseUrl}/{_sessionContext.ChannelId}/typing";

            await url
                .WithHeaders(_headers)
                .WithHeader("Authorization", _sessionContext.AccessToken)
                .WithHeader("Referer", $"https://discord.com/channels/{_sessionContext.ServerId}/{_sessionContext.ChannelId}")
                .WithHeader("User-Agent", _sessionContext.UserAgent)
                .PostAsync();

            Random random = new();
            int delay = (int)((random.NextDouble() + random.NextDouble() + random.NextDouble()) * 1000);
            await Task.Delay(delay);
        }

        public async Task<List<(string AuthorId, string Content, string MessageId)>> GetLatestMessagesAsync() 
            => await GetMessageHistory(_sessionContext.AccessToken, _sessionContext.ChannelId);

        private static string GenerateNonce() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();


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
