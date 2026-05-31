using Telegram.Bot;
using Telegram.Bot.Types;

namespace backend.Commands
{
    public class CommandExecutor
    {
        private readonly ITelegramBotClient _botClient;

        public CommandExecutor(ITelegramBotClient botClient) 
        {
            _botClient = botClient;
        }

        public async Task ExecuteAsync(Update update)
        {
            if(update.Message?.Text == null)
                return;

            var message = update.Message.Text;
            long chatId = update.Message.Chat.Id;

            if (message == "/start")
            {
                 await _botClient.SendMessage(
                    chatId: chatId,
                    text: "Hello");
            }
        }
    }
}