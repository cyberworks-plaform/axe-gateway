
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RequestLogEntry>(entity =>
            {
                entity.HasIndex(e => e.CreatedAtUtc);
                entity.HasIndex(e => e.Route);
                entity.HasIndex(e => e.DownstreamNode);
            });
        }
    }
}
