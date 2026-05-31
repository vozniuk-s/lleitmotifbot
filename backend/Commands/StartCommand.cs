using backend.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace backend.Commands
{
    public class StartCommand : ITelegramCommand
    {
        private readonly MessageCacheService _message;
        public string Name => "/start";

        public StartCommand(MessageCacheService message)
        {
            _message = message;
        }
        public async Task ExecuteAsync(ITelegramBotClient botClient, Update update)
        {
            var chatId = update.Message.Chat.Id;

            await botClient.SendMessage(
                chatId: chatId,
                text: _message.GetMessage("StartMessage")
                );
        }
    }
}