using Flurl.Http;
using Newtonsoft.Json.Linq;

namespace GrindSoft.Services
{
    public class MessageProcessor
    {
        static async Task<string> DownloadAccountPageAsync(string url, string authToken)
        {
            try
            {
                var responseBody = await url
                    .WithHeader("authorization", authToken)
                    .GetStringAsync();

                return responseBody;
            }
            catch (FlurlHttpException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return null!;
            }
        }

        static List<(string, string, string)> ParseJson(string jsonResponse)
        {
            JArray messages = JArray.Parse(jsonResponse);
            List<(string, string, string)> messagesList = [];

            foreach (var message in messages)
            {
                string content = message["content"].ToString();
                string authorId = message["author"]["id"].ToString();
                string messageId = message["id"].ToString();

                messagesList.Add((authorId, content, messageId));    
        
            }

            return messagesList;
        }

        public async Task<List<(string, string, string)>> GetMessageHistory(string token, string channelId)
        { 
            string accountUrl = $"https://discord.com/api/v9/channels/{channelId}/messages";
            string pageContent = await DownloadAccountPageAsync(accountUrl, token);

            if (pageContent != null)
            {
                return ParseJson(pageContent);
            }
            else
            {
                Console.WriteLine("Unable to retrieve page content.");
                return null!;
            }
        }
    }
}

