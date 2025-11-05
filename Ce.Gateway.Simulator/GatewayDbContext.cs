using Microsoft.EntityFrameworkCore;

namespace Ce.Gateway.Simulator;

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
            entity.HasIndex(e => e.DownstreamHost);
        });
    }
}
