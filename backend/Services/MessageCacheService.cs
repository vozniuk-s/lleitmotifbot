using backend.Data;
using backend.Models;
using System.Collections.Concurrent;

namespace backend.Services
{
    public class MessageCacheService
    {
        private readonly ConcurrentDictionary<string, string> _messages = new();
        private readonly IServiceScopeFactory _scopeFactory;

        public MessageCacheService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public void LoadCache()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

            var allMessages = db.BotMessages.ToList();
            _messages.Clear();

            foreach (var message in allMessages)
            {
                _messages[message.Key] = message.Value;
            }
        }

        public string GetMessage(string key) 
        {
            if (_messages.TryGetValue(key, out var value))
                return value;

            return $"[Відсутній текст: {key}]";
        }

        public async Task UpdateTextAsync(string key, string newValue)
        {
            key = key.Trim();

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

            var entity = await db.BotMessages.FindAsync(key);
            if (entity != null)
                entity.Value = newValue;
            else
                db.BotMessages.Add(new BotMessage { Key = key, Value = newValue });

            await db.SaveChangesAsync();
            _messages[key] = newValue;
        }
    }
}
