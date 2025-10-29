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
            
            var logs = await dbContext.OcrGatewayLogEntries
                .AsNoTracking()
                .Where(l => l.CreatedAtUtc >= from && l.CreatedAtUtc <= to)
                .Select(l => new 
                { 
                    l.CreatedAtUtc, 
                    l.DownstreamStatusCode 
                })
                .ToListAsync();

            var timeSlots = new List<TimeSlotData>();
            string timeFormat;

            if (groupBy == "hour")
            {
                timeFormat = "HH:00";
                var grouped = logs.GroupBy(l => new DateTime(l.CreatedAtUtc.Year, l.CreatedAtUtc.Month, l.CreatedAtUtc.Day, l.CreatedAtUtc.Hour, 0, 0));
                
                for (var dt = new DateTime(from.Year, from.Month, from.Day, from.Hour, 0, 0); 
                     dt <= to; 
                     dt = dt.AddHours(1))
                {
                    var group = grouped.FirstOrDefault(g => g.Key == dt);
                    if (group == null)
                    {
                        timeSlots.Add(new TimeSlotData
                        {
                            Label = dt.ToString("HH:00"),
                            SuccessCount = 0,
                            ClientErrorCount = 0,
                            ServerErrorCount = 0,
                            OtherCount = 0
                        });
                    }
                    else
                    {
                        var groupList = group.ToList();
                        timeSlots.Add(new TimeSlotData
                        {
                            Label = dt.ToString("HH:00"),
                            SuccessCount = groupList.Count(l => l.DownstreamStatusCode >= 200 && l.DownstreamStatusCode < 300),
                            ClientErrorCount = groupList.Count(l => l.DownstreamStatusCode >= 400 && l.DownstreamStatusCode < 500),
                            ServerErrorCount = groupList.Count(l => l.DownstreamStatusCode >= 500 && l.DownstreamStatusCode < 600),
                            OtherCount = groupList.Count(l => l.DownstreamStatusCode < 200 || (l.DownstreamStatusCode >= 300 && l.DownstreamStatusCode < 400) || l.DownstreamStatusCode >= 600)
                        });
                    }
                }
            }
            else if (groupBy == "month")
            {
                timeFormat = "yyyy-MM";
                var grouped = logs.GroupBy(l => new DateTime(l.CreatedAtUtc.Year, l.CreatedAtUtc.Month, 1));
                
                for (var dt = new DateTime(from.Year, from.Month, 1); 
                     dt <= new DateTime(to.Year, to.Month, 1); 
                     dt = dt.AddMonths(1))
                {
                    var group = grouped.FirstOrDefault(g => g.Key == dt);
                    if (group == null)
                    {
                        timeSlots.Add(new TimeSlotData
                        {
                            Label = dt.ToString("MMM yyyy"),
                            SuccessCount = 0,
                            ClientErrorCount = 0,
                            ServerErrorCount = 0,
                            OtherCount = 0
                        });
                    }
                    else
                    {
                        var groupList = group.ToList();
                        timeSlots.Add(new TimeSlotData
                        {
                            Label = dt.ToString("MMM yyyy"),
                            SuccessCount = groupList.Count(l => l.DownstreamStatusCode >= 200 && l.DownstreamStatusCode < 300),
                            ClientErrorCount = groupList.Count(l => l.DownstreamStatusCode >= 400 && l.DownstreamStatusCode < 500),
                            ServerErrorCount = groupList.Count(l => l.DownstreamStatusCode >= 500 && l.DownstreamStatusCode < 600),
                            OtherCount = groupList.Count(l => l.DownstreamStatusCode < 200 || (l.DownstreamStatusCode >= 300 && l.DownstreamStatusCode < 400) || l.DownstreamStatusCode >= 600)
                        });
                    }
                }
            }
            else // day
            {
                timeFormat = "yyyy-MM-dd";
                var grouped = logs.GroupBy(l => l.CreatedAtUtc.Date);
                
                for (var dt = from.Date; dt <= to.Date; dt = dt.AddDays(1))
                {
                    var group = grouped.FirstOrDefault(g => g.Key == dt);
                    if (group == null)
                    {
                        timeSlots.Add(new TimeSlotData
                        {
                            Label = dt.ToString("MM/dd"),
                            SuccessCount = 0,
                            ClientErrorCount = 0,
                            ServerErrorCount = 0,
                            OtherCount = 0
                        });
                    }
                    else
                    {
                        var groupList = group.ToList();
                        timeSlots.Add(new TimeSlotData
                        {
                            Label = dt.ToString("MM/dd"),
                            SuccessCount = groupList.Count(l => l.DownstreamStatusCode >= 200 && l.DownstreamStatusCode < 300),
                            ClientErrorCount = groupList.Count(l => l.DownstreamStatusCode >= 400 && l.DownstreamStatusCode < 500),
                            ServerErrorCount = groupList.Count(l => l.DownstreamStatusCode >= 500 && l.DownstreamStatusCode < 600),
                            OtherCount = groupList.Count(l => l.DownstreamStatusCode < 200 || (l.DownstreamStatusCode >= 300 && l.DownstreamStatusCode < 400) || l.DownstreamStatusCode >= 600)
                        });
                    }
                }
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
