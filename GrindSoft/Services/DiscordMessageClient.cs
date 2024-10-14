using Flurl.Http;
using GrindSoft.Interface;
using GrindSoft.Settings;
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


        public async Task SendMessageAsync(string accessToken, string userAgent, string channelId, string message, string serverId = "@me")
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

            Task.Run(() => SendTypingAsync(accessToken, userAgent, channelId, serverId));

            await url
                .WithHeaders(_headers)
                .WithHeader("Authorization", $"{accessToken}")
                .WithHeader("Referer", $"https://discord.com/channels/{serverId}/{channelId}")
                .WithHeader("User-Agent", $"{userAgent}")
                .PostJsonAsync(payload);

        }

        public async Task SendTypingAsync(string accessToken, string userAgent, string channelId, string serverId)
        {
            string url = $"{_discordSettings.BaseUrl}/{channelId}/typing";

            await url
                .WithHeaders(_headers)
                .WithHeader("Authorization", $"{accessToken}") 
                .WithHeader("Referer", $"https://discord.com/channels/{serverId}/{channelId}")
                .WithHeader("User-Agent", $"{userAgent}")
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
    }
}