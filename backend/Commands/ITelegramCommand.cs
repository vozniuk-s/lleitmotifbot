using Telegram.Bot;
using Telegram.Bot.Types;

namespace backend.Commands
{
    public interface ITelegramCommand
    {
        public string Name { get; }

        Task ExecuteAsync(ITelegramBotClient botClient, Update update);
    }
}