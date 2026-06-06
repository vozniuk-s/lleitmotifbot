using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using backend.Services;
using backend.Models;
using Microsoft.Extensions.Options;

namespace backend.Commands
{
    public class ResetAttemptCommand(IOptions<AdminSettings> adminSettings, DickService dickService, MessageCacheService messageCache) : ITelegramCommand
    {
        private readonly AdminSettings _adminSettings = adminSettings.Value;

        public string Name => "/reset";
        
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

            var parts = update.Message.Text.Split(' ');
            if (parts.Length != 2 || !int.TryParse(parts[1], out int recordId))
            {
                await botClient.SendMessage(chatId, messageCache.GetMessage("ResetFormat"));
                return;
            }

            var text = await dickService.ResetPlayerTimeAsync(userId, recordId);

            await botClient.SendMessage(chatId, text, parseMode: ParseMode.Html);
        }
    }
}
