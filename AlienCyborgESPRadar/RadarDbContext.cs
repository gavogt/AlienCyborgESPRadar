using Microsoft.EntityFrameworkCore;

namespace AlienCyborgESPRadar
{
    public sealed class RadarDbContext : DbContext
    {
        public RadarDbContext(DbContextOptions<RadarDbContext> options) : base(options) { }

        public DbSet<RadarLog> RadarLogs => Set<RadarLog>();
    }
}
