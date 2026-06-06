using Telegram.Bot;
using Telegram.Bot.Polling;
using backend.Commands;

namespace backend.Services
{
    public class BotBackgroundService(ITelegramBotClient botClient, IServiceProvider serviceProvider, ILogger<BotBackgroundService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await botClient.DeleteWebhook(cancellationToken: stoppingToken);
            logger.LogInformation("Webhook deleted. Starting Long Polling...");

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }
            };

            botClient.StartReceiving(
                updateHandler: async (botClient, update, cancellationToken) =>
                {
                    using var scope = serviceProvider.CreateScope();
                    var executor = scope.ServiceProvider.GetRequiredService<CommandExecutor>();
                    await executor.ExecuteAsync(update);
                },
                errorHandler: (botClient, exception, errorSource, cancellationToken) =>
                {
                    logger.LogError(exception, "Errow while getting updates from Telegram API");
                    return Task.CompletedTask;
                },
                receiverOptions: receiverOptions,
                cancellationToken: stoppingToken
            );
        }
    }
}