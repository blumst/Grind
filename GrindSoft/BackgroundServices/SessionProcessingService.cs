using GrindSoft.Interface;
using GrindSoft.Models;
using GrindSoft.Services;

namespace GrindSoft.BackgroundServices
{
    public class SessionProcessingService(SessionManager sessionManager, IServiceProvider serviceProvider) : BackgroundService
    {
        private readonly SessionManager _sessionManager = sessionManager;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        private const string ContinuationPrompt =
            "Imagine that you wrote this previous message, but no one replied to you. Continue the conversation in the same context, keeping the topic, language, and style the same." +
            "Make your new message a logical continuation of your previous message by adding additional details or expanding on the idea.";
        
        private string lastBotMessage = string.Empty;

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
                session.AuthorId = authorId;

                dbContext.Sessions.Add(session);
                await dbContext.SaveChangesAsync(stoppingToken);


                var initialMessages = await discordClient.GetLatestMessagesAsync();
                session.LastProcessedMessageId = initialMessages?.FirstOrDefault().MessageId ?? "0";
                session.Status = "In Progress";

                dbContext.Sessions.Update(session);
                await dbContext.SaveChangesAsync(stoppingToken);

                var lastMessageTime = DateTime.UtcNow;
                //var waitForAnswerTime = TimeSpan.FromSeconds(session.ModeType == 1 ? 30 : session.DelayBetweenMessages);
                var random = new Random();

                while (!stoppingToken.IsCancellationRequested)
                {
                    var messages = await discordClient.GetLatestMessagesAsync();

                    if (messages != null && messages.Count > 0)
                    {
                        var newMessages = messages
                            .TakeWhile(m => m.MessageId != session.LastProcessedMessageId)
                            .Reverse() 
                            .ToList();

                        foreach (var (AuthorId, Content, MessageId) in newMessages)
                        {
                            if (AuthorId != session.AuthorId)
                            {
                                session.MessagesSentByBot = 0;
                                lastMessageTime = DateTime.UtcNow;

                                if (session.ModeType == 2)
                                {
                                    bool isTargetUser = new Random().Next(0, 101) <= 90;

                                    string targetAuthorId = isTargetUser ? session.TargetUserId : messages[new Random().Next(0, messages.Count)].AuthorId;

                                    if (targetAuthorId == null || targetAuthorId != AuthorId) continue;

                                    await ProcessAndSendResponseAsync(session, discordClient, chatGPTClient, dbContext, Content, MessageId, AuthorId, stoppingToken);
                                }
                                else
                                    await ProcessAndSendResponseAsync(session, discordClient, chatGPTClient, dbContext, Content, MessageId, AuthorId, stoppingToken);
                            }
                        }
                    }

                    var timeSinceLastUserMessage = DateTime.UtcNow - lastMessageTime;

                    if (session.ModeType == 2 && timeSinceLastUserMessage.TotalSeconds >= random.Next(15, 31))
                    {
                        if (session.MessagesSentByBot < session.MessageCount)
                        {
                            string prompt = session.MessagesSentByBot == -1
                                ? session.Prompt
                                : $"Just checking in, feel free to continue the conversation!";

                            await AutoSendBotMessageAsync(session, discordClient, chatGPTClient, dbContext, prompt, stoppingToken);
                            session.MessagesSentByBot++;
                            lastMessageTime = DateTime.UtcNow;
                        }
                    }
                    else if (session.ModeType == 1 && timeSinceLastUserMessage >= TimeSpan.FromSeconds(30))
                    {
                        string prompt = session.MessagesSentByBot == -1
                            ? session.Prompt
                            : $"Your message: \"{lastBotMessage}\"\n{ContinuationPrompt}";

                        await AutoSendBotMessageAsync(session, discordClient, chatGPTClient, dbContext, prompt, stoppingToken);
                        session.MessagesSentByBot++;
                        lastMessageTime = DateTime.UtcNow;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(session.DelayBetweenMessages), stoppingToken);
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

        private static async Task AutoSendBotMessageAsync(Session session, IDiscordClient discordClient, IChatGPTClient chatGPTClient, AppDbContext dbContext, string prompt, CancellationToken stoppingToken)
        {
            Task.Run(() => discordClient.SendTypingAsync(), stoppingToken);

            var response = await chatGPTClient.SendMessageAsync(prompt);
            await discordClient.SendMessageAsync(response);

            session.Messages.Add(new Message
            {
                AuthorId = session.AuthorId,
                Content = response,
                DateTime = DateTime.UtcNow,
                SessionId = session.Id
            });

            dbContext.Sessions.Update(session);
            await dbContext.SaveChangesAsync(stoppingToken);

            await Task.Delay(TimeSpan.FromSeconds(session.DelayBetweenMessages), stoppingToken);
        }
        private async Task ProcessAndSendResponseAsync(Session session, IDiscordClient discordClient, IChatGPTClient chatGPTClient, AppDbContext dbContext, string content, string messageId, string authorId, CancellationToken stoppingToken)
        {
            await discordClient.SendTypingAsync();

            var response = await chatGPTClient.SendMessageAsync(content);
            await discordClient.SendMessageAsync(response, messageId);

            lastBotMessage = response;

            session.Messages.Add(new Message
            {
                AuthorId = authorId,
                Content = content,
                DateTime = DateTime.UtcNow,
                SessionId = session.Id
            });

            session.Messages.Add(new Message
            {
                AuthorId = session.AuthorId,
                Content = response,
                DateTime = DateTime.UtcNow,
                SessionId = session.Id
            });

            dbContext.Sessions.Update(session);
            await dbContext.SaveChangesAsync(stoppingToken);

            session.LastProcessedMessageId = messageId;
        }
    }
}
