using Ce.Gateway.Api.Data;
using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Repositories
{
    public class LogRepository : ILogRepository
    {
        private readonly IDbContextFactory<GatewayDbContext> _dbContextFactory;

        public LogRepository(IDbContextFactory<GatewayDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<PaginatedResult<RequestLogEntry>> GetLogsAsync(LogFilter filter, int page, int pageSize)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            IQueryable<RequestLogEntry> query = dbContext.OcrGatewayLogEntries.AsNoTracking();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(filter.Route))
            {
                query = query.Where(l => l.Route.Contains(filter.Route));
            }
            if (!string.IsNullOrWhiteSpace(filter.Node))
            {
                query = query.Where(l => l.DownstreamNode.Contains(filter.Node));
            }
            if (filter.StatusCode.HasValue)
            {
                query = query.Where(l => l.StatusCode == filter.StatusCode.Value);
            }
            if (filter.From.HasValue)
            {
                query = query.Where(l => l.CreatedAtUtc >= filter.From.Value.ToUniversalTime());
            }
            if (filter.To.HasValue)
            {
                query = query.Where(l => l.CreatedAtUtc <= filter.To.Value.ToUniversalTime());
            }

            // Sort
            query = query.OrderByDescending(l => l.CreatedAtUtc);

            // Pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PaginatedResult<RequestLogEntry>
            {
                TotalCount = totalCount,
                TotalPages = totalPages,
                Page = page,
                PageSize = pageSize,
                Data = data
            };
        }
    }
}
