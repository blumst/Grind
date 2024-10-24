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
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var discordClient = scope.ServiceProvider.GetRequiredService<IDiscordClient>();
                var chatGPTClient = scope.ServiceProvider.GetRequiredService<IChatGPTClient>();

                var sessionContext = new SessionContext
                {
                    AccessToken = session.AccessToken,
                    ChannelId = session.ChannelId,
                    ServerId = session.ServerId,
                    UserAgent = session.UserAgent
                };

                discordClient.UpdateData(sessionContext);

                var authorId = await discordClient.FetchUserIdAsync();
                sessionContext.AuthorId = authorId;

                discordClient.UpdateData(sessionContext);

                session.AuthorId = authorId;

                dbContext.Sessions.Add(session);
                await dbContext.SaveChangesAsync(stoppingToken);
                 
                var gptResponse = await chatGPTClient.SendMessageAsync(session.Prompt);
                await discordClient.SendMessageAsync(gptResponse);

                session.Messages.Add(new Message
                {
                    AuthorId = session.AuthorId,
                    Content = gptResponse,
                    DateTime = DateTime.UtcNow,
                    SessionId = session.Id
                });

                session.Status = "In Progress";

                dbContext.Sessions.Update(session);
                await dbContext.SaveChangesAsync(stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var messages = await discordClient.GetLatestMessagesAsync();
                    var lastMessage = messages.First();

                    if (messages != null && messages.Count > 0)
                    {
                        var (AuthorId, Content, MessageId) = lastMessage;
                        
                        if (AuthorId != session.AuthorId) 
                        {
                            Task.Run(() => discordClient.SendTypingAsync());

                            var response = await chatGPTClient.SendMessageAsync(Content);
                            await discordClient.SendMessageAsync(response);

                            session.Messages.Add(new Message
                            {
                                AuthorId = AuthorId,
                                Content = Content,
                                DateTime = DateTime.UtcNow,
                                SessionId = session.Id
                            });
                        }
               

                        dbContext.Sessions.Update(session);
                        await dbContext.SaveChangesAsync(stoppingToken);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }

                session.Status = "Completed";

                dbContext.Sessions.Update(session);
                await dbContext.SaveChangesAsync(stoppingToken);
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
