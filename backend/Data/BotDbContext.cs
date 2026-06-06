using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    public class BotDbContext(DbContextOptions<BotDbContext> options) : DbContext(options)
    {
        public DbSet<PlayerStat> PlayerStats { get; set; }
        public DbSet<BotMessage> BotMessages { get; set; }
    }
}
