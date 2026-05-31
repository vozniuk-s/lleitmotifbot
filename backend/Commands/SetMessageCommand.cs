using backend.Models;
using backend.Services;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace backend.Commands
{
    public class SetMessageCommand : ITelegramCommand
    {
        private readonly AdminSettings _adminSettings;
        private readonly MessageCacheService _message;
        public string Name => "/setmessage";

        public SetMessageCommand(IOptions<AdminSettings> adminSettings, MessageCacheService message)
        {
            _adminSettings = adminSettings.Value;
            _message = message;
        }
        public async Task ExecuteAsync(ITelegramBotClient botClient, Update update)
        {
            long userId = update.Message.From.Id;
            long chatId = update.Message.Chat.Id;

            if (userId != _adminSettings.MasterAdminId)
                return;

            var message = update.Message.Text;

            var parts = message.Split(' ', 3);
            if(parts.Length < 3)
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: _message.GetMessage("FormatToChangeMessage"));
                return;
            }

            string key = parts[1];
            string newValue = parts[2];

            await _message.UpdateTextAsync(key, newValue);
            await botClient.SendMessage(
                    chatId: chatId,
                    text: string.Format(_message.GetMessage("SuccessMessageChange"), key), 
                    parseMode: ParseMode.Html);
        }
    }
}
