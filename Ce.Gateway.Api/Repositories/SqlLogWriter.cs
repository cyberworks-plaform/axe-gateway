
using System.Threading.Tasks;
using Ce.Gateway.Api.Data;
using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Ce.Gateway.Api.Repositories
{
    public class SqlLogWriter : ILogWriter
    {
        private readonly IDbContextFactory<GatewayDbContext> _dbContextFactory;

        public SqlLogWriter(IDbContextFactory<GatewayDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task WriteLogAsync(RequestLogEntry logEntry)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            dbContext.OcrGatewayLogEntries.Add(logEntry);
            await dbContext.SaveChangesAsync();
        }
    }
}
