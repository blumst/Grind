using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

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
        public List<Message> Messages { get; set; } = [];
        public string? AuthorId { get; set; }
        public int MessageCount { get; set; }
        public int DelayBetweenMessages { get; set; }
        public string? LastProcessedMessageId { get; set; } = "0";
        public DateTime? LastProcessedMessageTimestamp { get; set; }
        public int MessagesSentByBot { get; set; } = 0;
        public int ModeType { get; set; }
        public string TargetUserId { get; set; }
        public DateTime StartTime { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
