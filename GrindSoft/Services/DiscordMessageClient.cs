using Flurl.Http;
using GrindSoft.Interface;

namespace GrindSoft.Services
{
    public class DiscordMessageClient : IDiscordMessageClient
    {
        public async Task SendMessageAsync(string accessToken, string channelId, string message)
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

            await url
                .WithHeader("authority", "discord.com")
                .WithHeader("accept", "/")
                .WithHeader("authorization", $"{accessToken}")
                .WithHeader("origin", "https://discord.com")
                .WithHeader("referer", $"https://discord.com/channels/@me/{channelId}")
                .PostJsonAsync(payload);

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