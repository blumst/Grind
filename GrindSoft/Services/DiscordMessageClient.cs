using Flurl;
using Flurl.Http;
using GrindSoft.Interface;
using System;

namespace GrindSoft.Services
{
    public class DiscordMessageClient : IDiscordMessageClient
    {
        public async Task SendMessageAsync(string accessToken, string channelId, string message, string serverId = "@me")
        {
            string url = $"https://discord.com/api/v9/channels/{channelId}/messages";

            var payload = new
            {
                mobile_network_type = "unknown",
                content = $"{message}",
                nonce = Nonce(),
                tts = false,
                flags = 0
            };

            Task.Run(() => SendTypingAsync(accessToken, channelId, serverId));

            await url
                .WithHeader("authority", "discord.com")
                .WithHeader("accept", "/")
                .WithHeader("authorization", $"{accessToken}")
                .WithHeader("origin", "https://discord.com")
                .WithHeader("referer", $"https://discord.com/channels/{serverId}/{channelId}")
                .PostJsonAsync(payload);

        }

        public async Task SendTypingAsync(string accessToken, string channelId, string serverId)
        {
            string url = $"https://discord.com/api/v9/channels/{channelId}/typing";

            await url
                .WithHeader("Authority", "discord.com")
                .WithHeader("Accept", "*/*")
                .WithHeader("Accept-Language", "en-US,en;q=0.9,ru;q=0.8")
                .WithHeader("Authorization", $"{accessToken}") 
                .WithHeader("DNT", "1")
                .WithHeader("Origin", "https://discord.com")
                .WithHeader("Priority", "u=1, i")
                .WithHeader("Referer", $"https://discord.com/channels/{serverId}/{channelId}")
                .WithHeader("Sec-CH-UA", "\"Not/A)Brand\";v=\"8\", \"Chromium\";v=\"126\", \"Google Chrome\";v=\"126\"")
                .WithHeader("Sec-CH-UA-Platform", "\"Windows\"")
                .WithHeader("Sec-Fetch-Dest", "empty")
                .WithHeader("Sec-Fetch-Mode", "cors")
                .WithHeader("Sec-Fetch-Site", "same-origin")
                .WithHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Unique/96.7.5796.97")
                .WithHeader("X-Discord-Locale", "en-US")
                .WithHeader("X-Discord-Timezone", "Europe/Kyiv")
                .PostAsync();

            Random random = new();
    
            Thread.Sleep((int)(random.NextDouble() + random.NextDouble() + random.NextDouble()));

        }

        public string Nonce()
        {
            DateTime epoch = new(1970, 1, 1);
            DateTime now = DateTime.Now;
            TimeSpan ts = now - epoch;
            long unixTimeMilliseconds = (long)ts.TotalMilliseconds;

            return ((unixTimeMilliseconds * 1000) - 1420070400000 + 4194304).ToString();
        }
    }
}