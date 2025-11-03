using Ce.Gateway.Api.Data;
using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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

            if (!string.IsNullOrWhiteSpace(filter.DownstreamHost))
            {
                query = query.Where(l => l.DownstreamHost.Contains(filter.DownstreamHost));
            }
            if (!string.IsNullOrWhiteSpace(filter.UpstreamClientIp))
            {
                query = query.Where(l => l.UpstreamClientIp.Contains(filter.UpstreamClientIp));
            }
            if (filter.DownstreamStatusCode.HasValue)
            {
                query = query.Where(l => l.DownstreamStatusCode == filter.DownstreamStatusCode.Value);
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

        public async Task<RequestReportDto> GetRequestReportAsync(DateTime from, DateTime to, string groupBy)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            string timeFormat;
            string sqlFormat;

            switch (groupBy)
            {
                case "hour":
                    timeFormat = "HH:00";
                    sqlFormat = "%Y-%m-%d %H:00:00";
                    break;
                case "month":
                    timeFormat = "yyyy-MM";
                    sqlFormat = "%Y-%m-01 00:00:00";
                    break;
                default: // day
                    timeFormat = "yyyy-MM-dd";
                    sqlFormat = "%Y-%m-%d 00:00:00";
                    break;
            }

            var sql = string.Format("""
                SELECT
                    strftime('{0}', CreatedAtUtc) as TimeValue,
                    '' as Label,
                    SUM(CASE WHEN DownstreamStatusCode >= 200 AND DownstreamStatusCode < 300 THEN 1 ELSE 0 END) as SuccessCount,
                    SUM(CASE WHEN DownstreamStatusCode >= 400 AND DownstreamStatusCode < 500 THEN 1 ELSE 0 END) as ClientErrorCount,
                    SUM(CASE WHEN DownstreamStatusCode >= 500 AND DownstreamStatusCode < 600 THEN 1 ELSE 0 END) as ServerErrorCount,
                    SUM(CASE WHEN DownstreamStatusCode < 200 OR (DownstreamStatusCode >= 300 AND DownstreamStatusCode < 400) OR DownstreamStatusCode >= 600 THEN 1 ELSE 0 END) as OtherCount
                FROM OcrGatewayLogEntries
                WHERE CreatedAtUtc >= @p0 AND CreatedAtUtc <= @p1
                GROUP BY TimeValue
                ORDER BY TimeValue
            """, sqlFormat);

            var dbResults = await dbContext.Database.SqlQueryRaw<TimeSlotData>(sql, from, to).ToListAsync();

            var timeSlots = new List<TimeSlotData>();
            var dbResultsDict = dbResults.ToDictionary(r => r.TimeValue);

            var loopTo = to;
            var loopFrom = from;

            if (groupBy == "hour")
            {
                loopFrom = new DateTime(from.Year, from.Month, from.Day, from.Hour, 0, 0);
            }
            else if (groupBy == "day")
            {
                loopFrom = from.Date;
                loopTo = to.Date.AddDays(1).AddTicks(-1);
            }
            else if (groupBy == "month")
            {
                loopFrom = new DateTime(from.Year, from.Month, 1);
            }

            for (var dt = loopFrom; dt <= loopTo; )
            {
                DateTime key;
                string label;
                if (groupBy == "hour")
                {
                    key = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0);
                    label = key.ToString("HH:00");
                }
                else if (groupBy == "day")
                {
                    key = dt.Date;
                    label = key.ToString("MM/dd");
                }
                else // month
                {
                    key = new DateTime(dt.Year, dt.Month, 1);
                    label = key.ToString("MMM yyyy");
                }

                if (dbResultsDict.TryGetValue(key, out var value))
                {
                    value.Label = label;
                    timeSlots.Add(value);
                }
                else
                {
                    timeSlots.Add(new TimeSlotData
                    {
                        TimeValue = key,
                        Label = label,
                        SuccessCount = 0,
                        ClientErrorCount = 0,
                        ServerErrorCount = 0,
                        OtherCount = 0
                    });
                }

                if (groupBy == "hour") dt = dt.AddHours(1);
                else if (groupBy == "month") dt = dt.AddMonths(1);
                else dt = dt.AddDays(1);
            }

            var totalSuccess = timeSlots.Sum(t => t.SuccessCount);
            var totalClientError = timeSlots.Sum(t => t.ClientErrorCount);
            var totalServerError = timeSlots.Sum(t => t.ServerErrorCount);
            var totalOther = timeSlots.Sum(t => t.OtherCount);

            return new RequestReportDto
            {
                TimeSlots = timeSlots,
                TimeFormat = timeFormat,
                TotalRequests = totalSuccess + totalClientError + totalServerError + totalOther,
                SuccessRequests = totalSuccess,
                ClientErrorRequests = totalClientError,
                ServerErrorRequests = totalServerError,
                OtherRequests = totalOther
            };
        }
    }
}
