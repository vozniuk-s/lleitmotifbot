using Telegram.Bot;
using Telegram.Bot.Types;
using backend.Services;
using Telegram.Bot.Types.Enums;

namespace backend.Commands
{
    public class DickCommand : ITelegramCommand
    {
        private readonly MessageCacheService _message;
        private readonly DickService _service;
        public string Name => "/dick";

        public DickCommand(DickService service, MessageCacheService message) 
        {
            _service = service;
            _message = message;
        }

        public async Task ExecuteAsync(ITelegramBotClient botClient, Update update)
        {
            if (update.Message?.From == null)
                return;

            var chatId = update.Message.Chat.Id;
            var userId = update.Message.From.Id;
            var name = update.Message.From.FirstName ?? _message.GetMessage("DefaultPlayer");

            var result = _service.PlayDailyGame(chatId, userId, name);

            await botClient.SendMessage(
                chatId: chatId,
                text: result.Message,
                parseMode: ParseMode.Html);
        }
    }
}