using Flurl.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Options;
using GrindSoft.Interface;
using GrindSoft.Settings;
using GrindSoft.Models;
using System.Net.WebSockets;
using System.Threading.Tasks.Dataflow;
using System.Text;

namespace GrindSoft.Services
{
    public class DiscordClient : IDiscordClient
    {
        private readonly DiscordSettings _discordSettings;
        private readonly Dictionary<string, string> _headers;
        private SessionContext _sessionContext;

        private ClientWebSocket _webSocket;
        private CancellationTokenSource _webSocketCts;
        private readonly BufferBlock<string> _messageQueue = new();

        public DiscordClient(IOptions<DiscordSettings> discordSettings)
        {
            _discordSettings = discordSettings.Value;

            _headers = new Dictionary<string, string>
            {
                { "Authority", "discord.com" },
                { "Accept",  "*/*" },
                { "Accept-Language",  "en-US,en;q=0.9,uk;q=0.8,ru;q=0.7"},
                { "DNT", "1" },
                { "Origin", "https://discord.com" },
                { "Priority", "u=1, i" },
                { "Sec-CH-UA", "\"Not/A)Brand\";v=\"8\", \"Chromium\";v=\"126\", \"Google Chrome\";v=\"126\"" },
                { "Sec-CH-UA-Platform", "\"Windows\"" },
                { "Sec-Fetch-Dest", "empty" },
                { "Sec-Fetch-Mode", "cors" },
                { "Sec-Fetch-Site", "same-origin" },
                { "X-Discord-Locale", "en-US" },
                { "X-Discord-Timezone", "Europe/Kyiv" }
            };
        }

        public void UpdateData(SessionContext sessionContext)
        {
            _sessionContext = sessionContext;
        }
        public void InitializeDeletionMechanism()
        {
            InitializeWebSocket();
            StartDeletionTask();
        }

        private void InitializeWebSocket()
        {
            _webSocketCts?.Cancel();
            _webSocketCts = new CancellationTokenSource();
            _webSocket = new ClientWebSocket();
            _webSocket.Options.SetRequestHeader("Authorization", _sessionContext.AccessToken);
            _webSocket.Options.SetRequestHeader("User-Agent", _sessionContext.UserAgent);

            Task.Run(() => ConnectWebSocketAsync(_webSocketCts.Token));
        }

        private async Task ConnectWebSocketAsync(CancellationToken cancellationToken)
        {
            try
            {
                var uri = new Uri("wss://gateway.discord.gg/?v=9&encoding=json");
                await _webSocket.ConnectAsync(uri, cancellationToken);

                var identifyPayload = new
                {
                    op = 2,
                    d = new
                    {
                        token = _sessionContext.AccessToken,
                        properties = new
                        {
                            os = Environment.OSVersion.Platform.ToString(),
                            browser = "DiscordClient",
                            device = "DiscordClient"
                        }
                    }
                };

                await SendWebSocketMessageAsync(identifyPayload, cancellationToken);

                await ReceiveWebSocketMessagesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка підключення WebSocket: {ex.Message}");
            }
        }

        private async Task SendWebSocketMessageAsync(object payload, CancellationToken cancellationToken)
        {
            var jsonPayload = JsonConvert.SerializeObject(payload);
            var bytes = Encoding.UTF8.GetBytes(jsonPayload);
            var segment = new ArraySegment<byte>(bytes);

            await _webSocket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
        }

        private async Task ReceiveWebSocketMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];

            while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Закриття з'єднання", cancellationToken);
                }
                else
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    HandleWebSocketMessage(message);
                }
            }
        }

        private void HandleWebSocketMessage(string message)
        {
            var json = JObject.Parse(message);

            if (json["t"] != null && json["t"].ToString() == "MESSAGE_CREATE")
            {
                var messageData = json["d"];
                var authorId = messageData["author"]["id"].ToString();
                var messageId = messageData["id"].ToString();
                var channelId = messageData["channel_id"].ToString();

                if (authorId == _sessionContext.AuthorId && channelId == _sessionContext.ChannelId)
                {
                    _messageQueue.Post(messageId);
                }
            }
        }

        public async Task<string> FetchUserIdAsync()
        {
            try
            {
                var userInfoUrl = "https://discord.com/api/v9/users/@me";
                var response = await userInfoUrl
                    .WithHeader("Authorization", _sessionContext.AccessToken)
                    .GetStringAsync();

                var jsonResponse = JsonConvert.DeserializeObject<JObject>(response);
                var authorId = jsonResponse["id"].ToString();

                return authorId;
            }
            catch (FlurlHttpException ex)
            {
                Console.WriteLine($"НFailed to retrieve user ID: {ex.Message}");
                return null;
            }
        }

        public async Task SendMessageAsync(string message, string replyToMessageId = null)
        {
            string url = $"{_discordSettings.BaseUrl}/{_sessionContext.ChannelId}/messages";

            var payload = new
            {
                mobile_network_type = "unknown",
                content = message,
                nonce = GenerateNonce(),
                tts = false,
                flags = 0,
                message_reference = replyToMessageId == null ? null : new
                {
                    channel_id = _sessionContext.ChannelId,
                    message_id = replyToMessageId
                }
            };

            await url
                .WithHeaders(_headers)
                .WithHeader("Authorization", _sessionContext.AccessToken)
                .WithHeader("Referer", $"https://discord.com/channels/{_sessionContext.ServerId}/{_sessionContext.ChannelId}")
                .WithHeader("User-Agent", _sessionContext.UserAgent)
                .PostJsonAsync(payload);
        }

        public async Task SendTypingAsync()
        {
            string url = $"{_discordSettings.BaseUrl}/{_sessionContext.ChannelId}/typing";

            await url
                .WithHeaders(_headers)
                .WithHeader("Authorization", _sessionContext.AccessToken)
                .WithHeader("Referer", $"https://discord.com/channels/{_sessionContext.ServerId}/{_sessionContext.ChannelId}")
                .WithHeader("User-Agent", _sessionContext.UserAgent)
                .PostAsync();

            Random random = new();
            int delay = (int)((random.NextDouble() + random.NextDouble() + random.NextDouble()) * 1000);
            await Task.Delay(delay);
        }

        public async Task<List<MessageRecord>> GetLatestMessagesAsync()
        {
            var messages = await GetMessageHistory(_sessionContext.AccessToken, _sessionContext.ChannelId);
            
            return messages.Select(message => new MessageRecord(
                AuthorId: message.AuthorId,
                Content: message.Content,
                MessageId: message.MessageId,
                Timestamp: message.Timestamp
            )).ToList();
        }

        private static string GenerateNonce() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();


        private static async Task<string> DownloadAccountPageAsync(string url, string authToken)
        {
            try
            {
                var responseBody = await url
                    .WithHeader("Authorization", authToken)
                    .GetStringAsync();

                return responseBody;
            }
            catch (FlurlHttpException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return null;
            }
        }

        private static List<MessageRecord> ParseJson(string jsonResponse)
        {
            var messagesList = new List<MessageRecord>();

            var settings = new JsonSerializerSettings
            {
                DateParseHandling = DateParseHandling.None
            };

            var messagesArray = JsonConvert.DeserializeObject<JArray>(jsonResponse, settings);

            foreach (var message in messagesArray)
            {
                string content = message["content"]!.ToString();
                string authorId = message["author"]!["id"]!.ToString();
                string messageId = message["id"]!.ToString();
                string timestampString = message["timestamp"]!.ToString();

                DateTimeOffset dto = DateTimeOffset.Parse(timestampString);
                DateTime timestamp = dto.UtcDateTime;

                messagesList.Add(new MessageRecord(authorId, content, messageId, timestamp));
            }

            return messagesList;
        }

        private async Task<List<MessageRecord>> GetMessageHistory(string token, string channelId)
        {
            string accountUrl = $"{_discordSettings.BaseUrl}/{channelId}/messages";
            string pageContent = await DownloadAccountPageAsync(accountUrl, token);

            if (pageContent != null)
                return ParseJson(pageContent);
            else
            {
                Console.WriteLine("Unable to retrieve page content.");
                return null;
            }
        }

        public async Task<string> SendMessageAndGetIdAsync(string message)
        {
            string url = $"{_discordSettings.BaseUrl}/{_sessionContext.ChannelId}/messages";

            var payload = new
            {
                content = message,
                nonce = GenerateNonce(),
                tts = false,
            };

            var response = await url
                .WithHeaders(_headers)
                .WithHeader("Authorization", _sessionContext.AccessToken)
                .WithHeader("Referer", $"https://discord.com/channels/{_sessionContext.ServerId}/{_sessionContext.ChannelId}")
                .WithHeader("User-Agent", _sessionContext.UserAgent)
                .PostJsonAsync(payload)
                .ReceiveString();

            var jsonResponse = JsonConvert.DeserializeObject<JObject>(response);
            string messageId = jsonResponse["id"].ToString();

            _messageQueue.Post(messageId);

            return messageId;
        }

        public async Task DeleteMessageAsync(string messageId)
        {
            string url = $"{_discordSettings.BaseUrl}/{_sessionContext.ChannelId}/messages/{messageId}";

            await url
                .WithHeaders(_headers)
                .WithHeader("Authorization", _sessionContext.AccessToken)
                .WithHeader("Referer", $"https://discord.com/channels/{_sessionContext.ServerId}/{_sessionContext.ChannelId}")
                .WithHeader("User-Agent", _sessionContext.UserAgent)
                .DeleteAsync();
        }

        private void StartDeletionTask()
        {
            Task.Run(async () =>
            {
                while (!_webSocketCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var messageId = await _messageQueue.ReceiveAsync(_webSocketCts.Token);

                        if (!string.IsNullOrEmpty(messageId))
                        {
                            await DeleteMessageAsync(messageId);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting a message: {ex.Message}");
                    }
                }
            }, _webSocketCts.Token);
        }

         public void Dispose()
        {
            _webSocketCts?.Cancel();
            _webSocket?.Dispose();
        }
    }
}
