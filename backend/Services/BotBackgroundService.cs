using Telegram.Bot;
using Telegram.Bot.Polling;
using backend.Commands;

namespace backend.Services
{
    public class BotBackgroundService : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BotBackgroundService> _logger;

        public BotBackgroundService(ITelegramBotClient botClient, IServiceProvider serviceProvider, ILogger<BotBackgroundService> logger)
        {
            _botClient = botClient;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _botClient.DeleteWebhook(cancellationToken: stoppingToken);
            _logger.LogInformation("Webhook deleted. Starting Long Polling...");

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }
            };

            _botClient.StartReceiving(
                updateHandler: async (botClient, update, cancellationToken) =>
                {
                    using var scope = _serviceProvider.CreateScope();
                    var executor = scope.ServiceProvider.GetRequiredService<CommandExecutor>();
                    await executor.ExecuteAsync(update);
                },
                errorHandler: (botClient, exception, errorSource, cancellationToken) =>
                {
                    _logger.LogError(exception, "Errow while getting updates from Telegram API");
                    return Task.CompletedTask;
                },
                receiverOptions: receiverOptions,
                cancellationToken: stoppingToken
            );
        }
    }
}