using Microsoft.EntityFrameworkCore;

namespace YamiiDarkFantasy.Models
{
    public class AppDbContext : DbContext
    {
        public DbSet<Player> Players { get; set; }
        public DbSet<GameSession> GameSessions { get; set; }
        public DbSet<Corpse> Corpses { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
