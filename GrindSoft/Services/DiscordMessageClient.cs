using Flurl.Http;
using GrindSoft.Interface;
using Microsoft.Extensions.Options;

namespace GrindSoft.Services
{
    public class DiscordMessageClient(IOptions<DiscordSettings> discordSettings) : IDiscordMessageClient
    {
        private readonly DiscordSettings _discordSettings = discordSettings.Value;

        private readonly Dictionary<string, string> _headers = new()
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

        private readonly List<string> _userAgents =
        [
            "Mozilla/5.0 (Windows NT 10.1; Win64; x64; en-US) AppleWebKit/603.22 (KHTML, like Gecko) Chrome/53.0.1599.252 Safari/602",
            "Mozilla/5.0 (Windows; Windows NT 6.1; x64; en-US) AppleWebKit/600.30 (KHTML, like Gecko) Chrome/51.0.2499.368 Safari/603",
            "Mozilla/5.0 (Windows; Windows NT 6.3; WOW64; en-US) AppleWebKit/537.35 (KHTML, like Gecko) Chrome/51.0.1067.380 Safari/534",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 9_2_8; en-US) AppleWebKit/600.14 (KHTML, like Gecko) Chrome/50.0.2334.114 Safari/534",
            "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 9_1_8) AppleWebKit/601.16 (KHTML, like Gecko) Chrome/48.0.3457.379 Safari/603",
            "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 8_2_2; en-US) Gecko/20100101 Firefox/53.4",
            "Mozilla/5.0 (Windows; U; Windows NT 10.1;; en-US) AppleWebKit/536.9 (KHTML, like Gecko) Chrome/55.0.2545.159 Safari/603.0 Edge/13.87607"
        ];

        public async Task SendMessageAsync(string accessToken, string channelId, string message, string serverId = "@me")
        {
            string url = $"{_discordSettings.BaseUrl}/{channelId}/messages";

            var payload = new
            {
                mobile_network_type = "unknown",
                content = $"{message}",
                nonce = GenerateMessageRequestTime(),
                tts = false,
                flags = 0
            };

            Task.Run(() => SendTypingAsync(accessToken, channelId, serverId));

            await url
                .WithHeaders(_headers)
                .WithHeader("Authorization", $"{accessToken}")
                .WithHeader("Referer", $"https://discord.com/channels/{serverId}/{channelId}")
                .WithHeader("User-Agent", GetRandomUserAgent())
                .PostJsonAsync(payload);

        }

        public async Task SendTypingAsync(string accessToken, string channelId, string serverId)
        {
            string url = $"{_discordSettings.BaseUrl}/{channelId}/typing";

            await url
                .WithHeaders(_headers)
                .WithHeader("Authorization", $"{accessToken}") 
                .WithHeader("Referer", $"https://discord.com/channels/{serverId}/{channelId}")
                .WithHeader("User-Agent", GetRandomUserAgent())
                .PostAsync();

            Random random = new();

            Task.Delay((int)(random.NextDouble() + random.NextDouble() + random.NextDouble()));
        }

        private static string GenerateMessageRequestTime()
        {
            DateTime epoch = new(1970, 1, 1);
            DateTime now = DateTime.Now;
            TimeSpan ts = now - epoch;
            long unixTimeMilliseconds = (long)ts.TotalMilliseconds;

            return ((unixTimeMilliseconds * 1000) - 1420070400000 + 4194304).ToString();
        }

        private string GetRandomUserAgent()
        {
            Random random = new();
            int index = random.Next(_userAgents.Count);
            return _userAgents[index];
        }
    }
}