using Microsoft.EntityFrameworkCore;

namespace AlienCyborgESPRadar
{
    public sealed class BatteryDBContext : DbContext
    {
        public BatteryDBContext(DbContextOptions<BatteryDBContext> options) : base(options)
        {
        }

        public DbSet<BatteryLog> BatteryLogs { get; set; } = null!;
    }
}
