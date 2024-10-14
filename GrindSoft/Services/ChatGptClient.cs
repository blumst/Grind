using GrindSoft.Interface;
using GrindSoft.Settings;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Flurl.Http;


namespace GrindSoft.Services
{
    public class ChatGPTClient : IChatGPTClient
    {
        private readonly ChatGptSettings _chatGptSettings;
        private readonly string _apiKey;
        private readonly string _apiUrl;

        public ChatGPTClient(IOptions<ChatGptSettings> chatGptSettings)
        {
            _chatGptSettings = chatGptSettings.Value;
            _apiKey = _chatGptSettings.ApiKey;
            _apiUrl = _chatGptSettings.ApiUrl;
        }

        private readonly List<Dictionary<string, string>> _messages = [];


        public async Task<string> SendMessageAsync(string prompt)
        {
            if (_messages.Count == 0)
            {
                _messages.Add(new Dictionary<string, string>
                {
                    { "role", "system" },
                    { "content", prompt }
                });
            }
            else
            {
                _messages.Add(new Dictionary<string, string>
                {
                    { "role", "user" },
                    { "content", prompt }
                });
            }

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = _messages
            };

            try
            {
                var responseString = await _apiUrl
                    .WithOAuthBearerToken(_apiKey)
                    .WithHeader("Content_Type", "application/json")
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
                        { "role", "assistant" },
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