using Ce.Gateway.Api.Data;
using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Repositories
{
    public class RequestReportRepository : IRequestReportRepository
    {
        private readonly IDbContextFactory<GatewayDbContext> _dbContextFactory;
        private readonly ILogger<RequestReportRepository> _logger;

        public RequestReportRepository(
            IDbContextFactory<GatewayDbContext> dbContextFactory,
            ILogger<RequestReportRepository> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<RequestReportDto> GetAggregatedCountsAsync(
            DateTime from,
            DateTime to,
            Granularity granularity,
            ReportFilter filter)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var granularityStr = granularity.ToString().ToLower();
            
            // Query the aggregates table
            var aggregates = await dbContext.RequestReportAggregates
                .AsNoTracking()
                .Where(a => a.PeriodStart >= from.Date 
                    && a.PeriodStart <= to.Date 
                    && a.Granularity == granularityStr)
                .ToListAsync();

            // Group by period and build time slots
            var groupedData = aggregates
                .GroupBy(a => a.PeriodStart)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    PeriodStart = g.Key,
                    SuccessCount = (int)g.Where(a => a.StatusCategory == 2).Sum(a => a.Count),
                    ClientErrorCount = (int)g.Where(a => a.StatusCategory == 4).Sum(a => a.Count),
                    ServerErrorCount = (int)g.Where(a => a.StatusCategory == 5).Sum(a => a.Count),
                    OtherCount = (int)g.Where(a => a.StatusCategory == 0).Sum(a => a.Count)
                })
                .ToList();

            // Fill in missing time slots
            var timeSlots = new List<TimeSlotData>();
            var current = GetPeriodStart(from, granularity);
            var end = GetPeriodStart(to, granularity);

            while (current <= end)
            {
                var data = groupedData.FirstOrDefault(d => d.PeriodStart == current);
                
                timeSlots.Add(new TimeSlotData
                {
                    TimeValue = current,
                    Label = FormatLabel(current, granularity),
                    SuccessCount = data?.SuccessCount ?? 0,
                    ClientErrorCount = data?.ClientErrorCount ?? 0,
                    ServerErrorCount = data?.ServerErrorCount ?? 0,
                    OtherCount = data?.OtherCount ?? 0
                });

                current = IncrementPeriod(current, granularity);
            }

            var totalSuccess = timeSlots.Sum(t => t.SuccessCount);
            var totalClientError = timeSlots.Sum(t => t.ClientErrorCount);
            var totalServerError = timeSlots.Sum(t => t.ServerErrorCount);
            var totalOther = timeSlots.Sum(t => t.OtherCount);

            return new RequestReportDto
            {
                TimeSlots = timeSlots,
                TimeFormat = GetTimeFormat(granularity),
                TotalRequests = totalSuccess + totalClientError + totalServerError + totalOther,
                SuccessRequests = totalSuccess,
                ClientErrorRequests = totalClientError,
                ServerErrorRequests = totalServerError,
                OtherRequests = totalOther
            };
        }

        public async Task<RequestReportDto> GetRawCountsAsync(
            DateTime from,
            DateTime to,
            Granularity granularity,
            ReportFilter filter)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            
            string sqlFormat = GetSqlFormat(granularity);

            // Use StringBuilder for better SQL construction
            var sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine($@"
                SELECT
                    strftime('{sqlFormat}', CreatedAtUtc) as TimeValue,
                    '' as Label,
                    SUM(CASE WHEN DownstreamStatusCode >= 200 AND DownstreamStatusCode < 300 THEN 1 ELSE 0 END) as SuccessCount,
                    SUM(CASE WHEN DownstreamStatusCode >= 400 AND DownstreamStatusCode < 500 THEN 1 ELSE 0 END) as ClientErrorCount,
                    SUM(CASE WHEN DownstreamStatusCode >= 500 AND DownstreamStatusCode < 600 THEN 1 ELSE 0 END) as ServerErrorCount,
                    SUM(CASE WHEN DownstreamStatusCode < 200 OR (DownstreamStatusCode >= 300 AND DownstreamStatusCode < 400) OR DownstreamStatusCode >= 600 THEN 1 ELSE 0 END) as OtherCount
                FROM OcrGatewayLogEntries
                WHERE CreatedAtUtc >= @p0 AND CreatedAtUtc <= @p1");

            // Add filter conditions if needed
            var paramIndex = 2;
            var parameters = new List<object> { from, to };

            if (!string.IsNullOrWhiteSpace(filter?.UpstreamPath))
            {
                sqlBuilder.AppendLine($" AND UpstreamPath LIKE @p{paramIndex}");
                parameters.Add($"%{filter.UpstreamPath}%");
                paramIndex++;
            }

            if (!string.IsNullOrWhiteSpace(filter?.DownstreamHost))
            {
                sqlBuilder.AppendLine($" AND DownstreamHost LIKE @p{paramIndex}");
                parameters.Add($"%{filter.DownstreamHost}%");
                paramIndex++;
            }

            if (!string.IsNullOrWhiteSpace(filter?.UpstreamClientIp))
            {
                sqlBuilder.AppendLine($" AND UpstreamClientIp LIKE @p{paramIndex}");
                parameters.Add($"%{filter.UpstreamClientIp}%");
                paramIndex++;
            }

            sqlBuilder.AppendLine(" GROUP BY TimeValue ORDER BY TimeValue");
            
            var sql = sqlBuilder.ToString();

            var dbResults = await dbContext.Database
                .SqlQueryRaw<TimeSlotData>(sql, parameters.ToArray())
                .ToListAsync();

            // Fill in missing time slots
            var timeSlots = new List<TimeSlotData>();
            var dbResultsDict = dbResults.ToDictionary(r => r.TimeValue);
            
            var current = GetPeriodStart(from, granularity);
            var end = GetPeriodStart(to, granularity);

            while (current <= end)
            {
                if (dbResultsDict.TryGetValue(current, out var value))
                {
                    value.Label = FormatLabel(current, granularity);
                    timeSlots.Add(value);
                }
                else
                {
                    timeSlots.Add(new TimeSlotData
                    {
                        TimeValue = current,
                        Label = FormatLabel(current, granularity),
                        SuccessCount = 0,
                        ClientErrorCount = 0,
                        ServerErrorCount = 0,
                        OtherCount = 0
                    });
                }

                current = IncrementPeriod(current, granularity);
            }

            var totalSuccess = timeSlots.Sum(t => t.SuccessCount);
            var totalClientError = timeSlots.Sum(t => t.ClientErrorCount);
            var totalServerError = timeSlots.Sum(t => t.ServerErrorCount);
            var totalOther = timeSlots.Sum(t => t.OtherCount);

            return new RequestReportDto
            {
                TimeSlots = timeSlots,
                TimeFormat = GetTimeFormat(granularity),
                TotalRequests = totalSuccess + totalClientError + totalServerError + totalOther,
                SuccessRequests = totalSuccess,
                ClientErrorRequests = totalClientError,
                ServerErrorRequests = totalServerError,
                OtherRequests = totalOther
            };
        }

        public async Task UpsertAggregatesAsync(
            DateTime periodStart,
            Granularity granularity,
            Dictionary<int, long> statusCounts)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var granularityStr = granularity.ToString().ToLower();
            var now = DateTime.UtcNow;

            foreach (var kvp in statusCounts)
            {
                var statusCategory = kvp.Key;
                var count = kvp.Value;

                var existing = await dbContext.RequestReportAggregates
                    .FirstOrDefaultAsync(a => 
                        a.PeriodStart == periodStart 
                        && a.Granularity == granularityStr 
                        && a.StatusCategory == statusCategory);

                if (existing != null)
                {
                    existing.Count = count;
                    existing.LastUpdatedAt = now;
                }
                else
                {
                    dbContext.RequestReportAggregates.Add(new RequestReportAggregate
                    {
                        PeriodStart = periodStart,
                        Granularity = granularityStr,
                        StatusCategory = statusCategory,
                        Count = count,
                        LastUpdatedAt = now
                    });
                }
            }

            await dbContext.SaveChangesAsync();
        }

        public async Task<DateTime?> GetAggregatesLastUpdatedAsync(
            DateTime from,
            DateTime to,
            Granularity granularity)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var granularityStr = granularity.ToString().ToLower();

            return await dbContext.RequestReportAggregates
                .AsNoTracking()
                .Where(a => a.PeriodStart >= from.Date 
                    && a.PeriodStart <= to.Date 
                    && a.Granularity == granularityStr)
                .MaxAsync(a => (DateTime?)a.LastUpdatedAt);
        }

        // Helper methods
        private DateTime GetPeriodStart(DateTime date, Granularity granularity)
        {
            return granularity switch
            {
                Granularity.Hour => new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0),
                Granularity.Day => date.Date,
                Granularity.Month => new DateTime(date.Year, date.Month, 1),
                _ => date.Date
            };
        }

        private DateTime IncrementPeriod(DateTime date, Granularity granularity)
        {
            return granularity switch
            {
                Granularity.Hour => date.AddHours(1),
                Granularity.Day => date.AddDays(1),
                Granularity.Month => date.AddMonths(1),
                _ => date.AddDays(1)
            };
        }

        private string FormatLabel(DateTime date, Granularity granularity)
        {
            return granularity switch
            {
                Granularity.Hour => date.ToString("HH:00"),
                Granularity.Day => date.ToString("MM/dd"),
                Granularity.Month => date.ToString("MMM yyyy"),
                _ => date.ToString("MM/dd")
            };
        }

        private string GetTimeFormat(Granularity granularity)
        {
            return granularity switch
            {
                Granularity.Hour => "HH:00",
                Granularity.Day => "yyyy-MM-dd",
                Granularity.Month => "yyyy-MM",
                _ => "yyyy-MM-dd"
            };
        }

        private string GetSqlFormat(Granularity granularity)
        {
            return granularity switch
            {
                Granularity.Hour => "%Y-%m-%d %H:00:00",
                Granularity.Day => "%Y-%m-%d 00:00:00",
                Granularity.Month => "%Y-%m-01 00:00:00",
                _ => "%Y-%m-%d 00:00:00"
            };
        }
    }
}
