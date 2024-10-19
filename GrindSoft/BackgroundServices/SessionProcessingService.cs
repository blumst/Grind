using GrindSoft.Interface;
using GrindSoft.Models;
using GrindSoft.Services;

namespace GrindSoft.BackgroundServices
{
    public class SessionProcessingService(SessionManager sessionManager, IServiceProvider serviceProvider) : BackgroundService
    {
        private readonly SessionManager _sessionManager = sessionManager;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_sessionManager.TryGetNextSession(out var session))
                    {
                        _ = Task.Run(() => ProcessSessionAsync(session, stoppingToken), stoppingToken);
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred in the main loop: {ex.Message}");
                }
            }
        }

        private async Task ProcessSessionAsync(Session session, CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();

                var discordClient = scope.ServiceProvider.GetRequiredService<IDiscordClient>();
                var chatGPTClient = scope.ServiceProvider.GetRequiredService<IChatGPTClient>();

                discordClient.UpdateData(session.AccessToken, session.ChannelId, session.ServerId, session.UserAgent);

                await discordClient.FetchUserIdAsync();

                var gptResponse = await chatGPTClient.SendMessageAsync(session.Prompt);
                await discordClient.SendMessageAsync(gptResponse);

                session.Status = "In Progress";

                while (!stoppingToken.IsCancellationRequested)
                {
                    var messages = await discordClient.GetLatestMessagesAsync();
                    if (messages != null && messages.Count > 0)
                    {
                        var latestMessage = messages.First();

                        if (latestMessage.AuthorId != discordClient.AuthorId)
                        {
                            var response = await chatGPTClient.SendMessageAsync(latestMessage.Content);
                            await discordClient.SendMessageAsync(response);
                        }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }

                session.Status = "Completed";
            }
            catch (OperationCanceledException)
            {
                session.Status = "Cancelled";
                Console.WriteLine($"Session was cancelled.");
            }
            catch (Exception ex)
            {
                session.Status = "Error";
                Console.WriteLine($"An error occurred while processing session: {ex.Message}");
            }
        }
    }
}
