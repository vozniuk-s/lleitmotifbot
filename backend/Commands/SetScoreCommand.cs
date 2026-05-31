using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using backend.Services;
using backend.Models;
using Microsoft.Extensions.Options;

namespace backend.Commands
{
    public class SetScoreCommand : ITelegramCommand
    {
        private readonly AdminSettings _adminSettings;
        private readonly DickService _dickService;
        private readonly MessageCacheService _message;
        public string Name => "/setscore";

        public SetScoreCommand(IOptions<AdminSettings> adminSettings, DickService dickService, MessageCacheService message)
        {
            _adminSettings = adminSettings.Value;
            _dickService = dickService;
            _message = message;
        }

        public async Task ExecuteAsync(ITelegramBotClient botClient, Update update)
        {
            long userId = update.Message.From.Id;
            long chatId = update.Message.Chat.Id;

            if (userId != _adminSettings.MasterAdminId)
                return;

            var parts = update.Message.Text.Split(' ');
            if (parts.Length != 3 || !int.TryParse(parts[1], out int recordId) || !int.TryParse(parts[2], out int newScore))
            {
                await botClient.SendMessage(chatId, _message.GetMessage("FormatToChangeNumber"));
                return;
            }

            var text = _dickService.ForceSetScore(userId, recordId, newScore);

            await botClient.SendMessage(chatId, text, parseMode: ParseMode.Html);
        }
    }
}
