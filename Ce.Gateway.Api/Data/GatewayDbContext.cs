
using Ce.Gateway.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ce.Gateway.Api.Data
{
    public class GatewayDbContext : IdentityDbContext<ApplicationUser>
    {
        public GatewayDbContext(DbContextOptions<GatewayDbContext> options) : base(options)
        {
        }

        public DbSet<RequestLogEntry> OcrGatewayLogEntries { get; set; }
        public DbSet<RequestReportAggregate> RequestReportAggregates { get; set; }
        public DbSet<SystemUpdate> SystemUpdates { get; set; }

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

            modelBuilder.Entity<SystemUpdate>(entity =>
            {
                entity.HasIndex(e => e.Version);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.IsCurrentVersion);
            });

        }
    }
}
