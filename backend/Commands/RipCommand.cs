using backend.Services;
using System.Net;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Caching.Memory;

namespace backend.Commands
{
    public class RipCommand(MessageCacheService messageCache, IMemoryCache cache) : ITelegramCommand
    {
        public string Name => "/rip";

        public async Task ExecuteAsync(ITelegramBotClient botClient, Update update)
        {
            if (update.Message?.From == null)
                return;

            var chatId = update.Message.Chat.Id;
            var userId = update.Message.From.Id;
            string cacheKey = $"rip_cooldown_{userId}";

            if (cache.TryGetValue(cacheKey, out _))
            {
                await botClient.DeleteMessage(chatId, update.Message.Id);
                return;
            }

            cache.Set(cacheKey, true, TimeSpan.FromHours(1));

            var name = update.Message.From.FirstName ?? messageCache.GetMessage("DefaultPlayer");
            string parseName = WebUtility.HtmlEncode(name);
            int randomIndex = Random.Shared.Next(1, 4);
            string template = messageCache.GetMessage($"RIPBot{randomIndex}");

            if (string.IsNullOrEmpty(template))
            {
                template = messageCache.GetMessage("RIPBot1");
            }

            await botClient.SendMessage(
                chatId: chatId,
                text: string.Format(template, userId, parseName),
                disableNotification: true,
                parseMode: ParseMode.Html);
        }
    }
}
