namespace GrindSoft.Interface
{
    public interface IDiscordMessageClient
    {
        public Task SendMessageAsync(string accessToken, string channelId, string message);
        public string Nonce();
    }
}
