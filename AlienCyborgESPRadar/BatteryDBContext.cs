using Microsoft.EntityFrameworkCore;

namespace AlienCyborgESPRadar
{
    public sealed class BatteryDbContext : DbContext
    {
        public BatteryDbContext(DbContextOptions<BatteryDbContext> options) : base(options)
        {
        }

        public DbSet<BatteryLog> BatteryLogs => Set<BatteryLog>();
    }
}
