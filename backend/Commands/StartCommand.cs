using backend.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace backend.Commands
{
    public class StartCommand(MessageCacheService messageCache) : ITelegramCommand
    {
        public string Name => "/start";

        public async Task ExecuteAsync(ITelegramBotClient botClient, Update update)
        {
            if (update.Message?.Chat == null)
                return;

            var chatId = update.Message.Chat.Id;

            await botClient.SendMessage(
                chatId: chatId,
                text: messageCache.GetMessage("StartMessage"));
        }
    }
}