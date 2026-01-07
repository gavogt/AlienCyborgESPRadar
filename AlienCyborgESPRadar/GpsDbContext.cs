using Microsoft.EntityFrameworkCore;

namespace AlienCyborgESPRadar
{
    public sealed class GpsDbContext: DbContext
    { 
        public GpsDbContext(DbContextOptions<GpsDbContext> options) : base(options) { }
        public DbSet<GpsLogs> GpsLogs => Set<GpsLogs>();
    }
}
