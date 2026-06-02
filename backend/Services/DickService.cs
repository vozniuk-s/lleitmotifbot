using backend.Data;
using backend.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using Telegram.Bot;

namespace backend.Services
{
    public class DickService
    {
        private readonly AdminSettings _adminSettings;
        private readonly MessageCacheService _message;
        private readonly BotDbContext _db;
        private readonly Random _random = new Random();
        public DickService(BotDbContext db, MessageCacheService message, IOptions<AdminSettings> adminSettings) 
        { 
            _db = db;
            _message = message;
            _adminSettings = adminSettings.Value;
        }
        private TimeZoneInfo GetKyivTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Kyiv"); // linux 
            }
            catch
            {
                return TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time"); // windows
            }
        }

        public (bool IsSuccess, string Message) PlayDailyGame(long chatId, long userId, string name)
        {
            var user = _db.PlayerStats.FirstOrDefault(u => u.TelegramId == userId && u.ChatId == chatId);

            if (user == null)
            {
                user = new PlayerStat
                {
                    TelegramId = userId,
                    ChatId = chatId,
                    Name = string.IsNullOrEmpty(name) ? _message.GetMessage("DefaultPlayer") : name,
                    Score = 0,
                    LastPlayedUtc = DateTime.MinValue
                };
                _db.PlayerStats.Add(user);
            }
            else
                user.Name = string.IsNullOrEmpty(name) ? _message.GetMessage("DefaultPlayer") : name;

            var kyivZone = GetKyivTimeZone();

            // Check Time
            DateTime utcNow = DateTime.UtcNow;
            DateTime kyivNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, kyivZone);
            DateTime kyivToday = kyivNow.Date;
            DateTime lastPlayedKyiv = TimeZoneInfo.ConvertTimeFromUtc(user.LastPlayedUtc, kyivZone);

            if (lastPlayedKyiv.Date == kyivToday)
            {
                DateTime nextMidnightKyiv = kyivToday.AddDays(1);
                TimeSpan timeUntilNextDay = nextMidnightKyiv - kyivNow;
                return (false, _message.GetMessage("AlreadyPlayed"));
            }

            // Game logic
            // 23% - minus, 85% - plus
            bool isNegative = _random.Next(1, 101) <= 23;

            int change;

            if (isNegative && user.Score > 0)
            {
                change = WeightedRandom(
                    new[] { -1, -2, -3, -4, -5, -6, -7, -8 },
                    new[] { 15, 25, 25, 15, 10, 6, 3, 1 });
            }
            else
            {
                change = WeightedRandom(
                    new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                    new[] { 2, 5, 15, 25, 25, 15, 8, 3, 1, 1 });
            }

            int oldScore = user.Score;
            user.Score += change;

            if (user.Score < 0)
                user.Score = 0;

            // Result
            user.LastPlayedUtc = utcNow;

            _db.SaveChanges();

            int actualChange = user.Score - oldScore;
            string parseName = WebUtility.HtmlEncode(user.Name);
            string resultMessage = actualChange > 0
                ? string.Format(_message.GetMessage("PlusToScore").Replace("\\n", "\n"), actualChange, user.Score, user.TelegramId, parseName)
                : string.Format(_message.GetMessage("MinusToScore").Replace("\\n", "\n"), (-1) * actualChange, user.Score, user.TelegramId, parseName);

            return (true, resultMessage);
        }

        private int WeightedRandom(int[] values, int[] weights)
        {
            int totalWeight = weights.Sum();
            int roll = _random.Next(totalWeight);

            int cumulative = 0;

            for (int i = 0; i < values.Length; i++)
            {
                cumulative += weights[i];

                if (roll < cumulative)
                    return values[i];
            }

            return values[^1];
        }
        public string GetTopTenPlayers(long chatId)
        {
            var topUsers = _db.PlayerStats
                .Where(u => u.ChatId == chatId)
                .OrderByDescending(u => u.Score)
                .Take(10)
                .ToList();

            if (!topUsers.Any())
                return _message.GetMessage("EmptyDatabase");

            var sb = new StringBuilder($"<b>{_message.GetMessage("TopTenPlayers")}</b>\n\n");

            for (int i = 0; i < topUsers.Count; i++)
            {
                if (i == 3)
                    sb.AppendLine();
                
                string medal = i switch
                {
                    0 => "🥇",
                    1 => "🥈",
                    2 => "🥉",
                    _ => "🏅"
                };

                string parseName = WebUtility.HtmlEncode(topUsers[i].Name);
                sb.AppendLine($"{medal} <b>{parseName}</b> — {topUsers[i].Score} см");
            }

            return sb.ToString();
        }

        public async Task<string> GetAdminPanelAsync(long requestUserId, ITelegramBotClient botClient)
        {
            if (requestUserId != _adminSettings.MasterAdminId)
                return _message.GetMessage("NoAccess");

            var users = _db.PlayerStats.ToList();
            var sb = new StringBuilder($"<b>{_message.GetMessage("AdminPanel")}</b>\n\n");

            var groupedUsers = users.GroupBy(u => u.ChatId);

            foreach (var group in groupedUsers)
            {
                string chatName = group.Key.ToString();

                if (group.Key < 0)
                {
                    try
                    {
                        var chatInfo = await botClient.GetChat(group.Key);
                        chatName = chatInfo.Title ?? chatInfo.FirstName ?? chatName;

                        await Task.Delay(150);
                    }
                    catch { }
                }

                sb.AppendLine($"📂 <b>Чат:</b> {chatName} (<code>{group.Key}</code>)");

                foreach (var u in group)
                {
                    sb.AppendLine($"   № <code>{u.Id}</code> | TG ID: <code>{u.TelegramId}</code> | <b>{u.Name}</b> | Число: {u.Score}");
                }
                sb.AppendLine();
            }

            sb.AppendLine(_message.GetMessage("AdminPanelChangeInstructions"));
            return sb.ToString();
        }

        public string ForceSetScore(long requestUserId, int recordId, int newScore)
        {
            if (requestUserId != _adminSettings.MasterAdminId)
                return _message.GetMessage("NoAccess");

            var user = _db.PlayerStats.FirstOrDefault(u => u.Id == recordId);
            if (user == null)
                return _message.GetMessage("DidntFind");

            user.Score = newScore;
            _db.SaveChanges();

            return string.Format(_message.GetMessage("SuccessNumberChange"), user.Name, newScore);
        }

        public string DeleteRecord(long requestUserId, int recordId)
        {
            if (requestUserId != _adminSettings.MasterAdminId)
                return _message.GetMessage("NoAccess");

            var user = _db.PlayerStats.FirstOrDefault(u => u.Id == recordId);
            if (user == null)
                return _message.GetMessage("DidntFind");

            string playerName = user.Name;
            _db.PlayerStats.Remove(user);
            _db.SaveChanges();

            return string.Format(_message.GetMessage("DeleteSuccess"), playerName);
        }

        public string ResetPlayerTime(long requestUserId, int recordId)
        {
            if (requestUserId != _adminSettings.MasterAdminId)
                return _message.GetMessage("NoAccess");

            var user = _db.PlayerStats.FirstOrDefault(u => u.Id == recordId);
            if (user == null)
                return _message.GetMessage("DidntFind");

            user.LastPlayedUtc = DateTime.MinValue;
            _db.SaveChanges();

            return string.Format(_message.GetMessage("ResetSuccess"), user.Name);
        }
    }
}