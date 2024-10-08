namespace GrindSoft.Interface
{
    public interface IDiscordMessageClient
    {
        public Task SendMessageAsync(string accessToken, string userAgent, string channelId, string message, string serverId);

        public Task SendTypingAsync(string accessToken, string userAgent, string channelId, string serverId);
    }
}
