using Telegram.Bot;
using Telegram.Bot.Types;
using backend.Services;
using Telegram.Bot.Types.Enums;

namespace backend.Commands
{
    public class DickCommand(DickService service, MessageCacheService messageCache) : ITelegramCommand
    {
        public string Name => "/dick";

        public async Task ExecuteAsync(ITelegramBotClient botClient, Update update)
        {
            if (update.Message?.From == null)
                return;

            var chatId = update.Message.Chat.Id;
            var userId = update.Message.From.Id;
            var name = update.Message.From.FirstName ?? messageCache.GetMessage("DefaultPlayer");

            var result = await service.PlayDailyGameAsync(chatId, userId, name);

            await botClient.SendMessage(
                chatId: chatId,
                text: result.Message,
                disableNotification: true,
                parseMode: ParseMode.Html);
        }
    }
}