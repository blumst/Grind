namespace GrindSoft.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string AuthorId { get; set; }
        public string Content { get; set; }
        public int SessionId { get; set; }
        public DateTime DateTime { get; set; }
    }
}
