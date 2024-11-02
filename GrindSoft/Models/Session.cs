namespace GrindSoft.Models
{
    public class Session
    {
        public int Id { get; set; }
        public string AccessToken { get; set; }
        public string UserAgent { get; set; }
        public string ServerId { get; set; }
        public string ChannelId { get; set; }
        public string Prompt { get; set; }
        public string Status { get; set; }
        public List<Message> Messages { get; set; } = new List<Message>();
        public string? AuthorId { get; set; }
        public int MessageCount { get; set; }
        public int DelayBetweenMessages { get; set; }
        public string? LastProcessedMessageId { get; set; } = "0";
        public int MessagesSentByBot { get; set; } = -1;
    }
}
