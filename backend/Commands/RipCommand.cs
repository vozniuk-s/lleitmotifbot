using backend.Services;
using System.Net;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Caching.Memory;

namespace backend.Commands
{
    public class RipCommand : ITelegramCommand
    {
        private readonly MessageCacheService _message;
        private readonly IMemoryCache _cache;
        public string Name => "/rip";

        public RipCommand(MessageCacheService message, IMemoryCache cache)
        {
            _message = message;
            _cache = cache;
        }

        public async Task ExecuteAsync(ITelegramBotClient botClient, Update update)
        {
            if (update.Message?.From == null)
                return;

            var chatId = update.Message.Chat.Id;
            var userId = update.Message.From.Id;
            string cacheKey = $"rip_cooldown_{userId}";

            if (_cache.TryGetValue(cacheKey, out _))
            {
                await botClient.DeleteMessage(chatId, update.Message.Id);
                return;
            }

            _cache.Set(cacheKey, true, TimeSpan.FromHours(1));

            var name = update.Message.From.FirstName ?? _message.GetMessage("DefaultPlayer");
            string parseName = WebUtility.HtmlEncode(name);
            int randomIndex = Random.Shared.Next(1, 4);
            string template = _message.GetMessage($"RIPBot{randomIndex}");

            if (string.IsNullOrEmpty(template))
            {
                template = _message.GetMessage("RIPBot1");
            }

            await botClient.SendMessage(
                chatId: chatId,
                text: string.Format(template, userId, parseName),
                disableNotification: true,
                parseMode: ParseMode.Html);
        }
    }
}
