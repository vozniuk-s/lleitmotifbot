using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using backend.Services;

namespace backend.Commands
{
    public class TopCommand(DickService dickService) : ITelegramCommand
    {
        public string Name => "/top";

        public async Task ExecuteAsync(ITelegramBotClient botClient, Update update)
        {
            if (update.Message?.Chat == null)
                return;

            var chatId = update.Message.Chat.Id;

            var topMessage = await dickService.GetTopTenPlayersAsync(chatId);

            await botClient.SendMessage(
                chatId: chatId,
                text: topMessage,
                parseMode: ParseMode.Html);
        }
    }
}
