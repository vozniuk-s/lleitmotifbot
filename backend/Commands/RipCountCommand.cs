using backend.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace backend.Commands
{
    public class RipCountCommand (DickService service) : ITelegramCommand
    {
        public string Name => "/ripcount";

        public async Task ExecuteAsync(ITelegramBotClient botClient, Update update)
        {
            if (update.Message?.From == null)
                return;

            var chatId = update.Message.Chat.Id;
            var userId = update.Message.From.Id;

            string result = await service.GetRipCountAsync(userId, chatId);

            await botClient.SendMessage(
                chatId: chatId,
                text: result,
                disableNotification: true,
                parseMode: ParseMode.Html);
        }
    }
}
