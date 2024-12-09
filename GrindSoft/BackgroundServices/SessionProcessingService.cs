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
            Task.Run(async() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        if (_sessionManager.TryGetNextSession(out var session))
                            await ProcessSessionAsync(session, stoppingToken);
                        else
                            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred in the main loop: {ex.Message}");
                    }
                }
            }, stoppingToken);
        }

        private async Task ProcessSessionAsync(Session session, CancellationToken stoppingToken)
        {
            IDiscordClient discordClient = null;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                discordClient = scope.ServiceProvider.GetRequiredService<IDiscordClient>();
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
                session.AuthorId = authorId;

                dbContext.Sessions.Add(session);
                await dbContext.SaveChangesAsync(stoppingToken);

                var initialMessages = await discordClient.GetLatestMessagesAsync();
                session.LastProcessedMessageId = initialMessages?.FirstOrDefault().MessageId ?? "0";
                session.LastProcessedMessageTimestamp = initialMessages?.FirstOrDefault()?.Timestamp;
                session.Status = "In Progress";

                dbContext.Sessions.Update(session);
                await dbContext.SaveChangesAsync(stoppingToken);

                var messageQueue = new Queue<MessageRecord>();
                var random = new Random();

                CancellationTokenSource initialPingCts = null;
                Task initialPingTask = Task.CompletedTask;


                if (session.ModeType == 1)
                { 
                    await AutoSendBotMessageAsync(session, discordClient, chatGPTClient, dbContext, session.Prompt, stoppingToken);

                    dbContext.Sessions.Update(session);
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
                else if (session.ModeType == 2)
                {
                    initialPingCts = new CancellationTokenSource();

                    initialPingTask = Task.Run(async () =>
                    {
                            int delay = random.Next(15, 31);
                            await Task.Delay(TimeSpan.FromSeconds(delay), initialPingCts.Token);

                            if (!initialPingCts.Token.IsCancellationRequested && session.MessagesSentByBot == 0)
                            {
                                await AutoSendBotMessageAsync(session, discordClient, chatGPTClient, dbContext, session.Prompt, stoppingToken);
                                dbContext.Sessions.Update(session);
                                await dbContext.SaveChangesAsync(stoppingToken);
                            }  
                    }, stoppingToken);
                }
                else if (session.ModeType == 3)
                {
                    discordClient.InitializeDeletionMechanism();

                    await HandleMode3Async(session, discordClient, dbContext, stoppingToken);
                }

                while (!stoppingToken.IsCancellationRequested)
                {
                    if (session.MessagesSentByBot >= session.MessageCount)
                        break;

                    var messages = await discordClient.GetLatestMessagesAsync();

                    if (messages != null && messages.Count > 0)
                    {
                        var newMessages = messages
                            .Where(m => session.LastProcessedMessageTimestamp == null || m.Timestamp > session.LastProcessedMessageTimestamp)
                            .OrderBy(m => m.Timestamp);

                        foreach (var message in newMessages)
                        {
                            if (message.AuthorId != session.AuthorId)
                            {
                                if (session.ModeType == 2)
                                {
                                    bool isTargetUser = new Random().Next(0, 101) <= 90;

                                    string targetAuthorId = isTargetUser ? session.TargetUserId : messages[new Random().Next(0, messages.Count)].AuthorId;

                                    if (targetAuthorId == null || targetAuthorId != message.AuthorId) continue;

                                    initialPingCts?.Cancel();

                                    await ProcessAndSendResponseAsync(session, discordClient, chatGPTClient, dbContext, message.Content, message.MessageId, message.AuthorId, stoppingToken);
                                    await Task.Delay(TimeSpan.FromSeconds(session.DelayBetweenMessages), stoppingToken);
                                }

                                messageQueue.Enqueue(message);
                                session.LastProcessedMessageTimestamp = message.Timestamp;
                            }
                        }

                        dbContext.Sessions.Update(session);
                        await dbContext.SaveChangesAsync(stoppingToken);
                    }

                    if (session.ModeType == 1 && messageQueue.Count > 0)
                    {
                        while (messageQueue.Count > 0)
                        {
                            var currentMessage = messageQueue.Dequeue(); 

                            if (string.IsNullOrEmpty(currentMessage.MessageId))  continue; 

                            await ProcessAndSendResponseAsync(session, discordClient, chatGPTClient, dbContext, currentMessage.Content, currentMessage.MessageId, currentMessage.AuthorId, stoppingToken);

                            await Task.Delay(TimeSpan.FromSeconds(session.DelayBetweenMessages), stoppingToken);
                        }
                    }
                    else
                        await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
                }

                if (session.ModeType == 2)
                {
                    try
                    {
                        await initialPingTask;
                    }
                    catch (TaskCanceledException) { }
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
                session.Status = "Failed";
                session.ErrorMessage = ex.Message;
                Console.WriteLine($"An error occurred while processing session: {ex.Message}");

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Sessions.Update(session);
                await dbContext.SaveChangesAsync(stoppingToken);
            }
            finally
            {
                if (discordClient is IDisposable disposableClient)
                    disposableClient.Dispose();
            }
        }

        private static async Task AutoSendBotMessageAsync(Session session, IDiscordClient discordClient, IChatGPTClient chatGPTClient, AppDbContext dbContext, string prompt, CancellationToken stoppingToken)
        {
            if (session.MessagesSentByBot >= session.MessageCount)
                return;

            Task.Run(() => discordClient.SendTypingAsync(), stoppingToken);

            string response = await chatGPTClient.SendMessageAsync(prompt);

            if (session.ModeType == 2)
                response = $"<@{session.TargetUserId}> {response}";

            await discordClient.SendMessageAsync(response);

            session.Messages.Add(new Message
            {
                AuthorId = session.AuthorId,
                Content = response,
                DateTime = DateTime.UtcNow,
                SessionId = session.Id
            });

            session.MessagesSentByBot++;

            dbContext.Sessions.Update(session);
            await dbContext.SaveChangesAsync(stoppingToken);

            await Task.Delay(TimeSpan.FromSeconds(session.DelayBetweenMessages), stoppingToken);
        }
        private static async Task ProcessAndSendResponseAsync(Session session, IDiscordClient discordClient, IChatGPTClient chatGPTClient, AppDbContext dbContext, string content, string messageId, string authorId, CancellationToken stoppingToken)
        {
            if (session.MessagesSentByBot >= session.MessageCount)
                return;

            if (string.IsNullOrEmpty(messageId))
                return;

            try
            { 
                await discordClient.SendTypingAsync();
            }
            catch
            {
                return;
            }

            var response = await chatGPTClient.SendMessageAsync(content);

            bool isTargetChance = new Random().Next(0, 101) <= 90;

            if (session.ModeType == 2 && !isTargetChance)
            {
                var words = response.Split(' ');
                int midPoint = words.Length / 2;

                var firstPart = string.Join(" ", words.Take(midPoint));
                var secondPart = string.Join(" ", words.Skip(midPoint));

                try
                {
                    await discordClient.SendMessageAsync(response, messageId);
                }
                catch 
                {
                    return;
                }

                session.Messages.Add(new Message
                {
                    AuthorId = session.AuthorId,
                    Content = firstPart,
                    DateTime = DateTime.UtcNow,
                    SessionId = session.Id
                });

                session.MessagesSentByBot++;

                dbContext.Sessions.Update(session);
                await dbContext.SaveChangesAsync(stoppingToken);

                if (session.MessagesSentByBot >= session.MessageCount)
                    return;

                await Task.Delay(TimeSpan.FromSeconds(new Random().Next(2, 4)), stoppingToken);
                try
                {
                    await discordClient.SendMessageAsync(response, messageId);
                }
                catch 
                {
                    return;
                }

                session.Messages.Add(new Message
                {
                    AuthorId = session.AuthorId,
                    Content = secondPart,
                    DateTime = DateTime.UtcNow,
                    SessionId = session.Id
                });

                session.MessagesSentByBot++;

                dbContext.Sessions.Update(session);
                await dbContext.SaveChangesAsync(stoppingToken);
            }
            else
            {
                try
                {
                    await discordClient.SendMessageAsync(response, messageId);
                }
                catch
                {
                    return;
                }

                session.Messages.Add(new Message
                {
                    AuthorId = authorId,
                    Content = content,
                    DateTime = DateTime.UtcNow,
                    SessionId = session.Id
                });

                session.MessagesSentByBot++;

                dbContext.Sessions.Update(session);
                await dbContext.SaveChangesAsync(stoppingToken);
            }

            session.LastProcessedMessageId = messageId;
        }

        private async Task HandleMode3Async(Session session, IDiscordClient discordClient, AppDbContext dbContext, CancellationToken stoppingToken)
        {
            for (int i = 0; i < session.MessageCount; i++)
            {
                discordClient.SendMessageAndGetIdAsync(session.Message);

                session.MessagesSentByBot++;

                session.Messages.Add(new Message
                {
                    AuthorId = session.AuthorId,
                    Content = session.Message,
                    DateTime = DateTime.UtcNow,
                    SessionId = session.Id
                });

                dbContext.Sessions.Update(session);
                await dbContext.SaveChangesAsync(stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(session.DelayBetweenMessages), stoppingToken);
            }

            session.Status = "Completed";
            dbContext.Sessions.Update(session);
            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}
