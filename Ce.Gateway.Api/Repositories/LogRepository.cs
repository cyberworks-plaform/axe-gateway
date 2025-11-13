using Ce.Gateway.Api.Data;
using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Repositories
{
    public class LogRepository : ILogRepository
    {
        private readonly IDbContextFactory<GatewayDbContext> _dbContextFactory;
        private readonly IMemoryCache _memoryCache;

        public LogRepository(IDbContextFactory<GatewayDbContext> dbContextFactory, IMemoryCache memoryCache)
        {
            _dbContextFactory = dbContextFactory;
            _memoryCache = memoryCache;
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
            // Create cache key based on parameters
            var cacheKey = $"request-report-{from:yyyyMMddHHmmss}-{to:yyyyMMddHHmmss}-{groupBy}";
            
            // Try to get from cache
            if (_memoryCache.TryGetValue(cacheKey, out RequestReportDto cachedResult))
            {
                return cachedResult;
            }

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

            var result = new RequestReportDto
            {
                TimeSlots = timeSlots,
                TimeFormat = timeFormat,
                TotalRequests = totalSuccess + totalClientError + totalServerError + totalOther,
                SuccessRequests = totalSuccess,
                ClientErrorRequests = totalClientError,
                ServerErrorRequests = totalServerError,
                OtherRequests = totalOther
            };

            // Calculate cache expiration based on date range
            var duration = to - from;
            TimeSpan cacheExpiration;
            
            if (duration.TotalDays <= 1)
            {
                // For 1 day or less: cache for 15 minutes
                cacheExpiration = TimeSpan.FromMinutes(15);
            }
            else
            {
                // For more than 1 day: cache for 1 day
                cacheExpiration = TimeSpan.FromDays(1);
            }

            // Store in cache with sliding expiration
            _memoryCache.Set(cacheKey, result, new MemoryCacheEntryOptions
            {
                SlidingExpiration = cacheExpiration
            });

            return result;
        }

        public async Task<Models.Dashboard.DashboardOverviewAggregateDto> GetDashboardOverviewAggregateAsync(DateTime startTime, DateTime endTime)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            
            var sql = @"
                SELECT 
                    COALESCE(COUNT(*), 0) as TotalRequests,
                    COALESCE(SUM(CASE WHEN IsError = 1 THEN 1 ELSE 0 END), 0) as ErrorRequests,
                    COALESCE(SUM(GatewayLatencyMs), 0) as TotalLatencyMs
                FROM OcrGatewayLogEntries
                WHERE CreatedAtUtc >= @p0 AND CreatedAtUtc <= @p1";

            var result = await dbContext.Database
                .SqlQueryRaw<Models.Dashboard.DashboardOverviewAggregateDto>(sql, startTime, endTime)
                .FirstOrDefaultAsync();

            return result ?? new Models.Dashboard.DashboardOverviewAggregateDto();
        }

        public async Task<List<Models.Dashboard.RouteSummaryDto>> GetRouteSummaryAggregateAsync(DateTime startTime, DateTime endTime)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            
            var sql = @"
                SELECT 
                    UpstreamPath as Route,
                    COALESCE(MIN(GatewayLatencyMs), 0) as MinLatencyMs,
                    COALESCE(MAX(GatewayLatencyMs), 0) as MaxLatencyMs,
                    COALESCE(CAST(AVG(GatewayLatencyMs) AS INTEGER), 0) as AvgLatencyMs,
                    COUNT(*) as TotalRequests
                FROM OcrGatewayLogEntries
                WHERE CreatedAtUtc >= @p0 AND CreatedAtUtc <= @p1
                GROUP BY UpstreamPath
                ORDER BY TotalRequests DESC";

            return await dbContext.Database
                .SqlQueryRaw<Models.Dashboard.RouteSummaryDto>(sql, startTime, endTime)
                .ToListAsync();
        }

        public async Task<List<Models.Dashboard.NodeSummaryDto>> GetNodeSummaryAggregateAsync(DateTime startTime, DateTime endTime)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            
            var sql = @"
                SELECT 
                    (DownstreamHost || ':' || COALESCE(DownstreamPort, 0)) as Node,
                    COALESCE(MIN(GatewayLatencyMs), 0) as MinLatencyMs,
                    COALESCE(MAX(GatewayLatencyMs), 0) as MaxLatencyMs,
                    COALESCE(CAST(AVG(GatewayLatencyMs) AS INTEGER), 0) as AvgLatencyMs,
                    COUNT(*) as TotalRequests
                FROM OcrGatewayLogEntries
                WHERE CreatedAtUtc >= @p0 AND CreatedAtUtc <= @p1
                    AND DownstreamHost IS NOT NULL
                GROUP BY DownstreamHost, DownstreamPort
                ORDER BY TotalRequests DESC";

            return await dbContext.Database
                .SqlQueryRaw<Models.Dashboard.NodeSummaryDto>(sql, startTime, endTime)
                .ToListAsync();
        }

        public async Task<List<Models.Dashboard.TimelineChartDataDto>> GetRequestTimelineAggregateAsync(DateTime startTime, DateTime endTime)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            
            TimeSpan duration = endTime - startTime;
            string sqlFormat;
            string displayFormat;
            int maxDataPoints = 100;

            // Improved granularity logic matching /requestreport
            if (duration.TotalHours <= 1)
            {
                // Minute granularity for ≤1h (real-time monitoring)
                sqlFormat = "%Y-%m-%d %H:%M:00";
                displayFormat = "HH:mm";
                maxDataPoints = 60;
            }
            else if (duration.TotalDays <= 1)
            {
                // Hour granularity for ≤1 day
                sqlFormat = "%Y-%m-%d %H:00:00";
                displayFormat = "HH:00";
                maxDataPoints = 24;
            }
            else if (duration.TotalDays <= 30)
            {
                // Day granularity for ≤30 days
                sqlFormat = "%Y-%m-%d 00:00:00";
                displayFormat = "yyyy-MM-dd";
                maxDataPoints = Math.Min((int)duration.TotalDays + 1, 100);
            }
            else
            {
                // Month granularity for >30 days
                sqlFormat = "%Y-%m-01 00:00:00";
                displayFormat = "yyyy-MM";
                maxDataPoints = Math.Min((int)(duration.TotalDays / 30) + 1, 100);
            }

            var sql = $@"
                SELECT 
                    strftime('{sqlFormat}', CreatedAtUtc) as TimeKey,
                    COUNT(*) as RequestCount
                FROM OcrGatewayLogEntries
                WHERE CreatedAtUtc >= @p0 AND CreatedAtUtc <= @p1
                GROUP BY TimeKey
                ORDER BY TimeKey
                LIMIT {maxDataPoints}";

            var results = await dbContext.Database
                .SqlQueryRaw<TimelineAggregateResult>(sql, startTime, endTime)
                .ToListAsync();

            return results.Select(r => 
            {
                DateTime parsedTime;
                if (DateTime.TryParse(r.TimeKey, out parsedTime))
                {
                    return new Models.Dashboard.TimelineChartDataDto
                    {
                        Timestamp = parsedTime.AddHours(7).ToString(displayFormat),
                        RequestCount = r.RequestCount
                    };
                }
                else
                {
                    return new Models.Dashboard.TimelineChartDataDto
                    {
                        Timestamp = r.TimeKey,
                        RequestCount = r.RequestCount
                    };
                }
            }).ToList();
        }

        public async Task<List<Models.Dashboard.TimelineChartDataDto>> GetLatencyTimelineAggregateAsync(DateTime startTime, DateTime endTime)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            
            TimeSpan duration = endTime - startTime;
            string sqlFormat;
            string displayFormat;
            int maxDataPoints = 100;

            // Improved granularity logic matching /requestreport
            if (duration.TotalHours <= 1)
            {
                // Minute granularity for ≤1h (real-time monitoring)
                sqlFormat = "%Y-%m-%d %H:%M:00";
                displayFormat = "HH:mm";
                maxDataPoints = 60;
            }
            else if (duration.TotalDays <= 1)
            {
                // Hour granularity for ≤1 day
                sqlFormat = "%Y-%m-%d %H:00:00";
                displayFormat = "HH:00";
                maxDataPoints = 24;
            }
            else if (duration.TotalDays <= 30)
            {
                // Day granularity for ≤30 days
                sqlFormat = "%Y-%m-%d 00:00:00";
                displayFormat = "yyyy-MM-dd";
                maxDataPoints = Math.Min((int)duration.TotalDays + 1, 100);
            }
            else
            {
                // Month granularity for >30 days
                sqlFormat = "%Y-%m-01 00:00:00";
                displayFormat = "yyyy-MM";
                maxDataPoints = Math.Min((int)(duration.TotalDays / 30) + 1, 100);
            }

            var sql = $@"
                SELECT 
                    strftime('{sqlFormat}', CreatedAtUtc) as TimeKey,
                    COALESCE(CAST(AVG(GatewayLatencyMs) AS INTEGER), 0) as RequestCount
                FROM OcrGatewayLogEntries
                WHERE CreatedAtUtc >= @p0 AND CreatedAtUtc <= @p1
                GROUP BY TimeKey
                ORDER BY TimeKey
                LIMIT {maxDataPoints}";

            var results = await dbContext.Database
                .SqlQueryRaw<TimelineAggregateResult>(sql, startTime, endTime)
                .ToListAsync();

            return results.Select(r => 
            {
                DateTime parsedTime;
                if (DateTime.TryParse(r.TimeKey, out parsedTime))
                {
                    return new Models.Dashboard.TimelineChartDataDto
                    {
                        Timestamp = parsedTime.AddHours(7).ToString(displayFormat),
                        RequestCount = r.RequestCount
                    };
                }
                else
                {
                    return new Models.Dashboard.TimelineChartDataDto
                    {
                        Timestamp = r.TimeKey,
                        RequestCount = r.RequestCount
                    };
                }
            }).ToList();
        }

        public async Task<List<Models.Dashboard.DonutChartDataDto>> GetHttpStatusDistributionAsync(DateTime startTime, DateTime endTime)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            
            var sql = @"
                SELECT 
                    (DownstreamStatusCode / 100) || 'xx' as Label,
                    COUNT(*) as Count
                FROM OcrGatewayLogEntries
                WHERE CreatedAtUtc >= @p0 AND CreatedAtUtc <= @p1 
                    AND DownstreamStatusCode IS NOT NULL
                GROUP BY DownstreamStatusCode / 100
                ORDER BY Label";

            return await dbContext.Database
                .SqlQueryRaw<Models.Dashboard.DonutChartDataDto>(sql, startTime, endTime)
                .ToListAsync();
        }
    }

    public class TimelineAggregateResult
    {
        public string TimeKey { get; set; }
        public long RequestCount { get; set; }
    }
}
