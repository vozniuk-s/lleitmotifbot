using backend.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using backend.Services;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Options;

namespace backend.Commands
{
    public class AdminCommand : ITelegramCommand
    {
        public string Name => "/admin";
        private readonly AdminSettings _adminSettings;
        private readonly DickService _dickService;
        private readonly MessageCacheService _message;

        public AdminCommand(IOptions<AdminSettings> adminSettings, DickService dickService, MessageCacheService message)
        {
            _adminSettings = adminSettings.Value;
            _dickService = dickService;
            _message = message;
        }

        public async Task ExecuteAsync(ITelegramBotClient botClient, Update update)
        {
            var userId = update.Message.From.Id;
            long chatId = update.Message.Chat.Id;

            if (userId != _adminSettings.MasterAdminId)
                return;

            var text = await _dickService.GetAdminPanelAsync(userId, botClient);

            if (update.Message.Chat.Type != ChatType.Private)
            {
                await botClient.SendMessage(chatId, _message.GetMessage("OpenAdminPanel"));

                await botClient.SendMessage(userId, text, parseMode: ParseMode.Html);
            }
            else
                await botClient.SendMessage(chatId, text, parseMode: ParseMode.Html);
        }
    }
}
