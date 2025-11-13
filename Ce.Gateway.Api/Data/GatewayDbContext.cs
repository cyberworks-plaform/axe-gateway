
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


        }
    }
}
