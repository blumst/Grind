using Flurl.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using GrindSoft.Interface;
using Timer = System.Timers.Timer;

namespace GrindSoft.Services
{
    public class DiscordService
    {
        private readonly IDiscordMessageClient _discordMessageClient;
        private readonly IChatGPTClient _chatGPTClient;
        private readonly MessageProcessor _messageProcessor;
        private readonly Timer _timer;

        private string _lastMessageId;
        private string _authorId;

        private string _accessToken;
        private string _channelId;
        private string _userAgent;
        private string _serverId;

        public DiscordService(IDiscordMessageClient discordMessageClient, IChatGPTClient chatGPTClient)
        {
            _discordMessageClient = discordMessageClient;
            _chatGPTClient = chatGPTClient;
            _messageProcessor = new MessageProcessor();

            _timer = new Timer(5000);
            _timer.Elapsed += TimerElapsedAsync;
            _timer.AutoReset = true;
        }



        public void StartMonitoring()
        {
            _timer.Start();
        }

        public async void TimerElapsedAsync(object? sender, System.Timers.ElapsedEventArgs e)
        {
            var messages = await _messageProcessor.GetMessageHistory(_accessToken, _channelId);

            if (messages != null && messages.Count > 0)
            {
                var latestMessage = messages.First();

                if (latestMessage.Item3 != _lastMessageId && latestMessage.Item1 != _authorId)
                {
                    _lastMessageId = latestMessage.Item3;

                    var chatGptResponse = await _chatGPTClient.SendMessageAsync(latestMessage.Item2);

                    await _discordMessageClient.SendMessageAsync(_accessToken, _userAgent, _channelId, chatGptResponse, _serverId);
                }
            }
        }

        public async Task FetchUserIdAsync()
        {
            try
            {
                var userInfoUrl = "https://discord.com/api/v9/users/@me";
                var response = await userInfoUrl
                    .WithHeader("Authorization", _accessToken)
                    .GetStringAsync();

                var jsonResponse = JsonConvert.DeserializeObject<JObject>(response);
                _authorId = jsonResponse["id"].ToString();
            }
            catch (FlurlHttpException ex)
            {
                Console.WriteLine($"Unable to get the user ID: {ex.Message}");
            }
        }

        public void UpdateData(string accessToken, string channelId, string serverId, string userAgent)
        {
            _accessToken = accessToken;
            _channelId = channelId;
            _serverId = serverId;
            _userAgent = userAgent;
        }
    }
}
