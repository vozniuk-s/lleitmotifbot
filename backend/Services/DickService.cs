using backend.Data;
using backend.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using Telegram.Bot;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class DickService(BotDbContext db, MessageCacheService messageCache, IOptions<AdminSettings> adminSettings)
    {
        private readonly AdminSettings _adminSettings = adminSettings.Value;

        private readonly Random _random = new();
        private static readonly ConcurrentDictionary<long, (int LastRoll, int Streak)> _chatStreaks = new();
        private const int MaximumRepeatNumber = 2;

        private static readonly int[] _negativeValues = [-1, -2, -3, -4, -5, -6, -7, -8, -9, -10];
        private static readonly int[] _negativeWeights = [4, 6, 10, 14, 16, 16, 12, 10, 7, 5];

        private static readonly int[] _positiveValues = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
        private static readonly int[] _positiveWeights = [3, 4, 12, 15, 15, 14, 12, 10, 7, 4, 2, 2];

        private static TimeZoneInfo GetKyivTimeZone()
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

        public async Task<(bool IsSuccess, string Message)> PlayDailyGameAsync(long chatId, long userId, string name)
        {
            var user = db.PlayerStats.FirstOrDefault(u => u.TelegramId == userId && u.ChatId == chatId);

            if (user == null)
            {
                user = new PlayerStat
                {
                    TelegramId = userId,
                    ChatId = chatId,
                    Name = string.IsNullOrEmpty(name) ? messageCache.GetMessage("DefaultPlayer") : name,
                    Score = 0,
                    LastPlayedUtc = DateTime.MinValue
                };
                db.PlayerStats.Add(user);
            }
            else
                user.Name = string.IsNullOrEmpty(name) ? messageCache.GetMessage("DefaultPlayer") : name;

            var kyivZone = GetKyivTimeZone();

            DateTime utcNow = DateTime.UtcNow;
            DateTime kyivNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, kyivZone);
            DateTime kyivToday = kyivNow.Date;
            DateTime lastPlayedKyiv = TimeZoneInfo.ConvertTimeFromUtc(user.LastPlayedUtc, kyivZone);

            if (lastPlayedKyiv.Date == kyivToday)
            {
                DateTime nextMidnightKyiv = kyivToday.AddDays(1);
                TimeSpan timeUntilNextDay = nextMidnightKyiv - kyivNow;
                return (false, messageCache.GetMessage("AlreadyPlayed"));
            }

            // Ігровий алгоритм
            bool isNegative = _random.Next(1, 101) <= 23;

            int change;

            if (isNegative && user.Score > 0)
                change = WeightedRandomAsync(_negativeValues, _negativeWeights);
            else
                change = WeightedRandomAsync(_positiveValues, _positiveWeights);

            var currentStreak = _chatStreaks.GetValueOrDefault(chatId, (LastRoll: 0, Streak: 0));

            if (change == currentStreak.LastRoll)
                currentStreak.Streak++;
            else
            {
                currentStreak.LastRoll = change;
                currentStreak.Streak = 1;
            }

            // Якщо гравцю тричі випадає одне й те саме число, то воно трохи змінюється.
            // 1 та -1 перетворються у 2 та -2 відповідно, щоб не було 0
            if (currentStreak.Streak > MaximumRepeatNumber)
            {
                if (change == 1) change = 2;
                else if (change == -1) change = -2;
                else --change;

                currentStreak.LastRoll = change;
                currentStreak.Streak = 1;
            }

            _chatStreaks[chatId] = currentStreak;

            int oldScore = user.Score;
            user.Score += change;

            if (user.Score < 0)
                user.Score = 0;

            user.LastPlayedUtc = utcNow;

            bool showRankMessage = false;
            string rankMessage = string.Empty;

            if(user.Score > user.HighScore)
            {
                string oldRank = RankProvider.GetRank(user.HighScore);
                string newRank = RankProvider.GetRank(user.Score);

                if(oldRank != newRank)
                {
                    showRankMessage = true;
                    rankMessage = RankProvider.GetRankMessage(user.Score);
                }

                user.HighScore = user.Score;
            }

            await db.SaveChangesAsync();

            int actualChange = user.Score - oldScore;
            string parseName = WebUtility.HtmlEncode(user.Name);
            string resultMessage = actualChange > 0
                ? string.Format(messageCache.GetMessage("PlusToScore").Replace("\\n", "\n"), actualChange, user.Score, user.TelegramId, parseName)
                : string.Format(messageCache.GetMessage("MinusToScore").Replace("\\n", "\n"), (-1) * actualChange, user.Score, user.TelegramId, parseName);

            if (showRankMessage)
                resultMessage += $"\n{rankMessage}";

            return (true, resultMessage);
        }

        public async Task<string> GetTopTenPlayersAsync(long chatId)
        {
            var topUsers = await db.PlayerStats
                .Where(u => u.ChatId == chatId)
                .OrderByDescending(u => u.Score)
                .Take(10)
                .ToListAsync();

            if (topUsers.Count == 0)
                return messageCache.GetMessage("EmptyDatabase");

            var sb = new StringBuilder($"<b>{messageCache.GetMessage("TopTenPlayers")}</b>\n\n");

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
                return messageCache.GetMessage("NoAccess");

            var users = await db.PlayerStats.ToListAsync();
            var sb = new StringBuilder($"<b>{messageCache.GetMessage("AdminPanel")}</b>\n\n");

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

            sb.AppendLine(messageCache.GetMessage("AdminPanelChangeInstructions"));
            return sb.ToString();
        }
        public async Task<string> ForceSetScoreAsync(long requestUserId, int recordId, int newScore)
        {
            if (requestUserId != _adminSettings.MasterAdminId)
                return messageCache.GetMessage("NoAccess");

            var user = await db.PlayerStats.FirstOrDefaultAsync(u => u.Id == recordId);
            if (user == null)
                return messageCache.GetMessage("DidntFind");

            user.Score = newScore;
            await db.SaveChangesAsync();

            return string.Format(messageCache.GetMessage("SuccessNumberChange"), user.Name, newScore);
        }
        public async Task<string> DeleteRecordAsync(long requestUserId, int recordId)
        {
            if (requestUserId != _adminSettings.MasterAdminId)
                return messageCache.GetMessage("NoAccess");

            var user = await db.PlayerStats.FirstOrDefaultAsync(u => u.Id == recordId);
            if (user == null)
                return messageCache.GetMessage("DidntFind");

            string playerName = user.Name;
            db.PlayerStats.Remove(user);
            await db.SaveChangesAsync();

            return string.Format(messageCache.GetMessage("DeleteSuccess"), playerName);
        }
        public async Task<string> ResetPlayerTimeAsync(long requestUserId, int recordId)
        {
            if (requestUserId != _adminSettings.MasterAdminId)
                return messageCache.GetMessage("NoAccess");

            var user = await db.PlayerStats.FirstOrDefaultAsync(u => u.Id == recordId);
            if (user == null)
                return messageCache.GetMessage("DidntFind");

            user.LastPlayedUtc = DateTime.MinValue;
            await db.SaveChangesAsync();

            return string.Format(messageCache.GetMessage("ResetSuccess"), user.Name);
        }
        public async Task<string> GetRipCountAsync(long userId, long chatId)
        {
            var user = await db.PlayerStats.
                FirstOrDefaultAsync(u => u.TelegramId == userId && u.ChatId == chatId);
            if (user == null)
                return messageCache.GetMessage("DidntFind");

            string playerName = WebUtility.HtmlEncode(user.Name);
            int count = user.RipCount;

            return string.Format(messageCache.GetMessage("RipCount"), userId, playerName, count, GetWordForm(count));
        }

        private int WeightedRandomAsync(int[] values, int[] weights)
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
        private string GetWordForm(int count)
        {
            int n = Math.Abs(count) % 100;
            int n1 = n % 10;

            if (n > 10 && n < 20) return "разів";
            if (n1 > 1 && n1 < 5) return "рази";
            if (n1 == 1) return "раз";
            return "разів";
        }
    }
}