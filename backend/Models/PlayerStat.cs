namespace backend.Models
{
    public class PlayerStat
    {
        public int Id { get; set; }
        public long TelegramId { get; set; }
        public long ChatId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; } = 0;
        public DateTime LastPlayedUtc { get; set; } = DateTime.MinValue;
    }
}