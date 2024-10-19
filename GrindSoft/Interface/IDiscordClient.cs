namespace GrindSoft.Interface
{
    public interface IDiscordClient
    {
        string AuthorId { get; }

        void UpdateData(string accessToken, string channelId, string serverId, string userAgent);
        Task FetchUserIdAsync();
        Task<List<(string AuthorId, string Content, string MessageId)>> GetLatestMessagesAsync();
        Task SendMessageAsync(string message);
        Task SendTypingAsync();
    }
}
