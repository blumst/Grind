using GrindSoft.Interface;
using GrindSoft.Settings;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Flurl.Http;


namespace GrindSoft.Services
{
    public class ChatGPTClient(IOptions<ChatGptSettings> chatGptSettings) : IChatGPTClient
    {
        private readonly ChatGptSettings _chatGptSettings = chatGptSettings.Value;
        private readonly List<Dictionary<string, string>> _messages = [];

        private const string SystemRole = "system";
        private const string UserRole = "user";
        private const string AssistantRole = "assistant";
        private const string ModelName = "gpt-4o-mini";


        public async Task<string> SendMessageAsync(string prompt)
        {
            if (_messages.Count == 0)
            {
                _messages.Add(new Dictionary<string, string>
                {
                    { "role", SystemRole },
                    { "content", prompt }
                });
            }
            else
            {
                _messages.Add(new Dictionary<string, string>
                {
                    { "role", UserRole },
                    { "content", prompt }
                });
            }

            var requestBody = new
            {
                model = ModelName,
                messages = _messages
            };

            try
            {
                var responseString = await _chatGptSettings.ApiUrl
                    .WithOAuthBearerToken(_chatGptSettings.ApiKey)
                    .WithHeader("Content-Type", "application/json")
                    .PostJsonAsync(requestBody)
                    .ReceiveString();

                using JsonDocument doc = JsonDocument.Parse(responseString);

                var root = doc.RootElement;
                var choices = root.GetProperty("choices");

                if (choices.GetArrayLength() > 0)
                {
                    var messageElement = choices[0].GetProperty("message");
                    var contentResponse = messageElement.GetProperty("content").GetString();

                    _messages.Add(new Dictionary<string, string>
                    {
                        { "role", AssistantRole },
                        { "content", contentResponse }
                    });

                    return contentResponse;
                }
                else
                {
                    return "No response from the model.";
                }
            }
            catch (FlurlHttpException ex)
            {
                var error = await ex.GetResponseStringAsync();
                throw new Exception($"API Error: {ex.StatusCode}, {error}");
            }

        }
    }
}
