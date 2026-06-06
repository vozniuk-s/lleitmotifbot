using backend.Models;
using backend.Services;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace backend.Commands
{
    public class SetMessageCommand(IOptions<AdminSettings> adminSettings, MessageCacheService messageCache) : ITelegramCommand
    {
        private readonly AdminSettings _adminSettings = adminSettings.Value;

        public string Name => "/setmessage";

        public async Task ExecuteAsync(ITelegramBotClient botClient, Update update)
        {
            if (update.Message?.From == null)
                return;

            long userId = update.Message.From.Id;
            long chatId = update.Message.Chat.Id;

            if (userId != _adminSettings.MasterAdminId)
                return;

            if (update.Message?.From == null || string.IsNullOrWhiteSpace(update.Message.Text))
                return;

            var messageText = update.Message.Text;

            var parts = messageText.Split(' ', 3);
            if(parts.Length < 3)
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: messageCache.GetMessage("FormatToChangeMessage"));
                return;
            }

            string key = parts[1];
            string newValue = parts[2];

            await messageCache.UpdateTextAsync(key, newValue);
            await botClient.SendMessage(
                    chatId: chatId,
                    text: string.Format(messageCache.GetMessage("SuccessMessageChange"), key), 
                    parseMode: ParseMode.Html);
        }
    }
}
