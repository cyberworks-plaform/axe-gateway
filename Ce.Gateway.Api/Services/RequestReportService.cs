using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Repositories.Interface;
using Ce.Gateway.Api.Services.Interface;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Services
{
    public class RequestReportService : IRequestReportService
    {
        private readonly IRequestReportRepository _repository;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<RequestReportService> _logger;
        private readonly IConfiguration _configuration;

        // Per-key locks to prevent multiple simultaneous queries for the same data
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        
        // Track cache keys by date range for efficient invalidation
        private readonly ConcurrentDictionary<string, (DateTime from, DateTime to)> _cacheKeyRanges = new();

        // Configuration values
        private readonly int _defaultCacheTtlMinutes;
        private readonly int _shortCacheTtlMinutes;

        public RequestReportService(
            IRequestReportRepository repository,
            IMemoryCache memoryCache,
            ILogger<RequestReportService> logger,
            IConfiguration configuration)
        {
            _repository = repository;
            _memoryCache = memoryCache;
            _logger = logger;
            _configuration = configuration;

            // Load cache TTL configuration
            _defaultCacheTtlMinutes = _configuration.GetValue("RequestReport:CacheDefaultTtlMinutes", 30);
            _shortCacheTtlMinutes = _configuration.GetValue("RequestReport:CacheShortTtlMinutes", 2);
        }

        public async Task<RequestReportDto> GetReportAsync(
            DateTime from,
            DateTime to,
            Granularity granularity,
            ReportFilter filter = null)
        {
            filter ??= new ReportFilter();

            // Build cache key
            var cacheKey = BuildCacheKey(from, to, granularity, filter);

            // Try to get from cache
            if (_memoryCache.TryGetValue(cacheKey, out RequestReportDto cachedResult))
            {
                _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
                return cachedResult;
            }

            // Get or create a lock for this cache key
            var keyLock = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

            await keyLock.WaitAsync();
            try
            {
                // Double-check cache after acquiring lock
                if (_memoryCache.TryGetValue(cacheKey, out cachedResult))
                {
                    _logger.LogDebug("Cache hit after lock for key: {CacheKey}", cacheKey);
                    return cachedResult;
                }

                _logger.LogInformation("Cache miss for key: {CacheKey}, fetching from database", cacheKey);

                RequestReportDto result;

                // Decide whether to use aggregates or raw query
                // Use aggregates only if:
                // 1. Granularity is Day or Month (Hour aggregates can be used, but not Minute)
                // 2. No additional filters are applied (filter is empty)
                // 3. Data is not too recent (to avoid stale aggregates for real-time data)
                
                var isRecentData = (DateTime.UtcNow - to).TotalMinutes < 10; // Last 10 minutes is "recent"
                bool useAggregates = (granularity == Granularity.Day || granularity == Granularity.Month || granularity == Granularity.Hour) 
                    && filter.IsEmpty()
                    && !isRecentData; // Don't use aggregates for very recent data

                if (useAggregates)
                {
                    _logger.LogDebug("Using aggregates for query");
                    result = await _repository.GetAggregatedCountsAsync(from, to, granularity, filter);
                }
                else
                {
                    _logger.LogDebug("Using raw query (granularity: {Granularity}, hasFilters: {HasFilters}, isRecent: {IsRecent})", 
                        granularity, !filter.IsEmpty(), isRecentData);
                    result = await _repository.GetRawCountsAsync(from, to, granularity, filter);
                }

                // Determine cache TTL based on date range
                var cacheTtl = CalculateCacheTtl(from, to);

                // Store in cache
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    SlidingExpiration = cacheTtl
                };
                
                // Register callback for when cache entry is evicted
                cacheOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
                {
                    // Clean up tracking when cache entry is evicted
                    var keyStr = key.ToString();
                    _cacheKeyRanges.TryRemove(keyStr, out _);
                    
                    // Cleanup and dispose SemaphoreSlim to prevent memory leak
                    if (_locks.TryRemove(keyStr, out var semaphore))
                    {
                        semaphore?.Dispose();
                    }
                    
                    _logger.LogDebug("Cache entry evicted: {Key}, reason: {Reason}", keyStr, reason);
                });

                _memoryCache.Set(cacheKey, result, cacheOptions);
                
                // Track this cache key's range for invalidation
                _cacheKeyRanges[cacheKey] = (from, to);

                return result;
            }
            finally
            {
                keyLock.Release();
            }
        }

        public void InvalidateCache(DateTime from, DateTime to)
        {
            _logger.LogInformation("Invalidating cache for range: {From} to {To}", from, to);

            // Find all cache keys that overlap with the given range
            var keysToRemove = _cacheKeyRanges
                .Where(kvp => RangesOverlap(kvp.Value.from, kvp.Value.to, from, to))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                _cacheKeyRanges.TryRemove(key, out _);
                _locks.TryRemove(key, out var lockObj);
                lockObj?.Dispose();
                
                _logger.LogDebug("Removed cache entry: {Key}", key);
            }

            _logger.LogInformation("Invalidated {Count} cache entries", keysToRemove.Count);
        }

        // Helper methods
        private string BuildCacheKey(DateTime from, DateTime to, Granularity granularity, ReportFilter filter)
        {
            var filterKey = filter?.GetNormalizedKey() ?? "";
            return $"reqreport:{from:yyyy-MM-dd}_{to:yyyy-MM-dd}:{granularity}:{filterKey}";
        }

        private TimeSpan CalculateCacheTtl(DateTime from, DateTime to)
        {
            var duration = to - from;
            var timeSinceEnd = DateTime.UtcNow - to;
            
            // For very recent data (real-time, last 10 minutes), use very short TTL
            if (timeSinceEnd.TotalMinutes < 10 || duration.TotalHours <= 1)
            {
                return TimeSpan.FromSeconds(30); // 30 seconds for real-time data
            }
            
            // For short time ranges (< 1 day), use shorter TTL
            if (duration.TotalDays < 1)
            {
                return TimeSpan.FromMinutes(_shortCacheTtlMinutes);
            }
            
            // For longer ranges, use default TTL
            return TimeSpan.FromMinutes(_defaultCacheTtlMinutes);
        }

        private bool RangesOverlap(DateTime range1From, DateTime range1To, DateTime range2From, DateTime range2To)
        {
            // Two ranges overlap if one starts before the other ends
            // Using < instead of <= for stricter boundary handling to avoid false positives
            return range1From < range2To && range2From < range1To;
        }
    }
}
