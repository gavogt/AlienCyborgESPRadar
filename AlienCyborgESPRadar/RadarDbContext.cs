using Microsoft.EntityFrameworkCore;

namespace AlienCyborgESPRadar
{
    public sealed class RadarDbContext : DbContext
    {
        public RadarDbContext(DbContextOptions<RadarDbContext> options) : base(options) { }

        public DbSet<RadarLog> RadarLogs => Set<RadarLog>();
        public DbSet<GpsLogs> GpsLogs => Set<GpsLogs>();
        public DbSet<BatteryLog> BatteryLogs => Set<BatteryLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // RadarLog -> GpsLogs (shared primary key one-to-one)
            modelBuilder.Entity<RadarLog>()
                .HasOne(r => r.GpsLog)
                .WithOne(g => g.RadarLog)
                .HasForeignKey<GpsLogs>(g => g.RadarLogId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // RadarLog -> BatteryLog (shared primary key one-to-one)
            modelBuilder.Entity<RadarLog>()
                .HasOne(r => r.BatteryLog)
                .WithOne(b => b.RadarLog)
                .HasForeignKey<BatteryLog>(b => b.RadarLogId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
