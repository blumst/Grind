namespace GrindSoft.Interface
{
    public interface IChatGPTClient
    {
        public Task<string> SendMessageAsync(string prompt);
    }
}