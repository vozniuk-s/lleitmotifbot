using backend.Services;
using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace backend.Commands
{
    public class CommandExecutor
    {
        private readonly IEnumerable<ITelegramCommand> _commands;
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger _logger;
        private readonly MessageCacheService _message;

        public CommandExecutor(IEnumerable<ITelegramCommand> commands, ITelegramBotClient botClient, ILogger<CommandExecutor> logger, MessageCacheService message)
        {   
            _commands = commands;
            _botClient = botClient;
            _logger = logger;
            _message = message;
        }

        public async Task ExecuteAsync(Update update)
        {
            if (update.Type != UpdateType.Message || update.Message?.Text == null)
                return;

            var messageText = update.Message.Text;
            var chatId = update.Message.Chat.Id;

            if (!messageText.StartsWith("/"))
                return;

            _logger.LogInformation("Chat: {ChatId} | User: {UserId} ({Username}) | Message: {MessageText}",
               chatId,
               update.Message.From?.Id,
               update.Message.From?.Username ?? "NoUsername",
               messageText);

            var commandString = messageText.Split(' ')[0].Split('@')[0];
            var command = _commands.FirstOrDefault(c =>
                string.Equals(c.Name, commandString, StringComparison.OrdinalIgnoreCase));

            if (command != null)
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    await command.ExecuteAsync(_botClient, update);
                    stopwatch.Stop();
                    _logger.LogInformation("Success command: {CommandName} | Time: {ElapsedMs} ms", command.Name, stopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.LogError(ex, "Error executing command {Command} in chat {ChatId} after {ElapsedMs} ms", messageText, chatId, stopwatch.ElapsedMilliseconds);
                }
            }
            else
            {
                _logger.LogInformation("Command not recognized: {MessageText} in chat {ChatId}", messageText, chatId);
                await _botClient.SendMessage(chatId, _message.GetMessage("UnknownCommand"));
            }
        }
    }
}