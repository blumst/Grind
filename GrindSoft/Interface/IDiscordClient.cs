using GrindSoft.Models;

namespace GrindSoft.Interface
{
    public interface IDiscordClient
    {
        void UpdateData(SessionContext sessionContext);
        Task<string> FetchUserIdAsync();
        Task<List<(string AuthorId, string Content, string MessageId)>> GetLatestMessagesAsync();
        Task SendMessageAsync(string message);
        Task SendTypingAsync();
    }
}
