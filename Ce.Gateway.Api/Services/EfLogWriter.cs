
using System.Threading.Tasks;
using Ce.Gateway.Api.Data;
using Ce.Gateway.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ce.Gateway.Api.Services
{
    public class EfLogWriter : ILogWriter
    {
        private readonly IDbContextFactory<GatewayDbContext> _dbContextFactory;

        public EfLogWriter(IDbContextFactory<GatewayDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task WriteLogAsync(OcrGatewayLogEntry logEntry)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            dbContext.OcrGatewayLogEntries.Add(logEntry);
            await dbContext.SaveChangesAsync();
        }
    }
}
