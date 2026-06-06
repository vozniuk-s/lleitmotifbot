using backend.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using backend.Services;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Options;

namespace backend.Commands
{
    public class AdminCommand(IOptions<AdminSettings> adminSettings, DickService dickService, MessageCacheService messageCache) : ITelegramCommand
    {
        private readonly AdminSettings _adminSettings = adminSettings.Value;

        public string Name => "/admin";

        public async Task ExecuteAsync(ITelegramBotClient botClient, Update update)
        {
            if (update.Message?.From == null)
                return;

            var userId = update.Message.From.Id;
            long chatId = update.Message.Chat.Id;
            
            if (userId != _adminSettings.MasterAdminId)
                return;

            var text = await dickService.GetAdminPanelAsync(userId, botClient);

            if (update.Message.Chat.Type != ChatType.Private)
            {
                await botClient.SendMessage(chatId, messageCache.GetMessage("OpenAdminPanel"));

                await botClient.SendMessage(userId, text, parseMode: ParseMode.Html);
            }
            else
                await botClient.SendMessage(chatId, text, parseMode: ParseMode.Html);
        }
    }
}
