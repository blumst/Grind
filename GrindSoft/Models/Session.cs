namespace GrindSoft.Models
{
    public class Session
    {
        public string AccessToken { get; set; }
        public string UserAgent { get; set; }
        public string ServerId { get; set; }
        public string ChannelId { get; set; }
        public string Prompt { get; set; }
        public string Status { get; set; }
    }
}
