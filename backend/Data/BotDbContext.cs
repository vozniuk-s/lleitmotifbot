using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    public class BotDbContext : DbContext
    {
        public BotDbContext(DbContextOptions<BotDbContext> options) : base(options) { }

        public DbSet<PlayerStat> PlayerStats { get; set; }
        public DbSet<BotMessage> BotMessages { get; set; }
    }
}
