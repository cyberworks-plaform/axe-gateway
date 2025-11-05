
using Ce.Gateway.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ce.Gateway.Api.Data
{
    public class GatewayDbContext : DbContext
    {
        public GatewayDbContext(DbContextOptions<GatewayDbContext> options) : base(options)
        {
        }

        public DbSet<RequestLogEntry> OcrGatewayLogEntries { get; set; }
        public DbSet<RequestReportAggregate> RequestReportAggregates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RequestLogEntry>(entity =>
            {
                entity.HasIndex(e => e.CreatedAtUtc);
                entity.HasIndex(e => e.DownstreamHost);
                entity.HasIndex(e => e.UpstreamPath);
                entity.HasIndex(e => e.UpstreamHost);
                entity.HasIndex(e => new { e.CreatedAtUtc, e.IsError });
                entity.HasIndex(e => new { e.CreatedAtUtc, e.DownstreamStatusCode });
            });

            modelBuilder.Entity<RequestReportAggregate>(entity =>
            {
                entity.HasKey(e => new { e.PeriodStart, e.Granularity, e.StatusCategory });
                entity.HasIndex(e => e.PeriodStart);
                entity.HasIndex(e => new { e.PeriodStart, e.Granularity });
                entity.Property(e => e.Granularity).HasMaxLength(16);
            });
        }
    }
}
