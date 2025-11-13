# Request Report Real-time Improvements

## Overview
This document describes the improvements made to the request report system to support real-time data visualization and proper granularity selection based on time ranges.

## Issues Fixed

### 1. Missing Minute-Level Granularity
**Problem**: Users filtering data for ≤1 hour couldn't see minute-level details for real-time monitoring.

**Solution**: Added `Granularity.Minute` enum value and implemented full support in:
- Repository: SQL formatting, period calculation, label formatting
- Controller: Automatic granularity selection
- Service: Cache and aggregation logic

### 2. Incorrect Granularity Mapping
**Problem**: Fixed period filters didn't follow the required logic:
- ≤1h → should show minutes (was showing hours)
- ≤1 day → should show hours (was correct)
- ≤30 days → should show days (was correct)
- >30 days → should show months (was correct)

**Solution**: Implemented dynamic granularity selection in controller based on time range duration:
```csharp
var duration = to - from;
if (duration.TotalHours <= 1)
    granularity = Granularity.Minute;  // Real-time, minute-level
else if (duration.TotalDays <= 1)
    granularity = Granularity.Hour;     // Hourly for 1 day
else if (duration.TotalDays <= 30)
    granularity = Granularity.Day;      // Daily for 1 month
else
    granularity = Granularity.Month;    // Monthly for longer
```

### 3. Stale Aggregates for Recent Data
**Problem**: Background worker aggregates data every 5 minutes, but real-time dashboard showed outdated data for the last 10 minutes.

**Solution**: 
- Service now detects "recent data" (last 10 minutes) and bypasses aggregates
- Uses raw query for real-time accuracy
- Separate cache TTL for real-time data: 30 seconds (vs 2-30 minutes for historical)

### 4. Missing Hour Aggregation
**Problem**: Worker only aggregated Day and Month granularities, missing Hour which is needed for 1-day and 6-hour filters.

**Solution**: Added hourly aggregation to worker:
```csharp
// Aggregate by hour (last 7 days for recent data)
await AggregateByGranularityAsync(hourLookbackDate, Granularity.Hour, ...);

// Aggregate by day (last 30 days)
await AggregateByGranularityAsync(dayLookbackDate, Granularity.Day, ...);

// Aggregate by month (last 12 months)
await AggregateByGranularityAsync(monthLookbackDate, Granularity.Month, ...);
```

### 5. Excessive Lookback Period
**Problem**: 90-day lookback for all granularities caused slow aggregation.

**Solution**: Optimized lookback periods per granularity:
- **Hour**: 7 days (recent, frequently updated)
- **Day**: 30 days (configurable, default reduced from 90)
- **Month**: 12 months (long-term trends)

## New Features

### New Period Filters
Added support for real-time period filters:
- `15m` - Last 15 minutes (minute granularity)
- `30m` - Last 30 minutes (minute granularity)
- `1h` - Last 1 hour (minute granularity)
- `6h` - Last 6 hours (hour granularity)
- `12h` - Last 12 hours (hour granularity)

Existing filters maintained:
- `1d`, `7d`, `1m`, `3m`, `9m`, `12m`

### Smart Cache Strategy

#### Real-time Data (≤1 hour or last 10 minutes)
- **Cache TTL**: 30 seconds
- **Data Source**: Raw query (bypasses aggregates)
- **Purpose**: Ensure latest data visible

#### Short-term Data (1 hour - 1 day)
- **Cache TTL**: 2 minutes (configurable)
- **Data Source**: Aggregates if available, raw query with filters
- **Purpose**: Balance freshness and performance

#### Long-term Data (>1 day)
- **Cache TTL**: 30 minutes (configurable)
- **Data Source**: Aggregates (highly optimized)
- **Purpose**: Maximum performance for historical queries

### Aggregation Strategy

| Granularity | Lookback Period | Update Frequency | Use Case |
|-------------|-----------------|------------------|----------|
| Hour | 7 days | Every 5 min | Recent trends, 1-day views |
| Day | 30 days | Every 5 min | Weekly/monthly analysis |
| Month | 12 months | Every 5 min | Long-term trends |

**Note**: Minute-level data is NEVER aggregated - always uses raw queries for real-time accuracy.

## Performance Impact

### Before
- All periods used same cache (2-30 min)
- No minute-level support
- Recent data showed stale aggregates
- 90-day aggregation took longer

### After
- Real-time data: 30s cache, raw query → **Fresh data**
- Minute-level support → **Real-time monitoring**
- Recent data bypasses stale aggregates → **Accurate**
- Optimized lookback periods → **Faster aggregation** (7/30/365 days)
- Hour aggregation added → **Better 1-day performance**

### Expected Performance

| Time Range | Granularity | Data Source | Cache TTL | First Query | Cached |
|------------|-------------|-------------|-----------|-------------|--------|
| 15m, 30m, 1h | Minute | Raw | 30s | 50-200ms | 0ms |
| 6h, 12h, 1d | Hour | Aggregates* | 2min | 10-100ms | 0ms |
| 7d, 1m | Day | Aggregates | 30min | 10-50ms | 0ms |
| 3m, 9m, 12m | Month | Aggregates | 30min | 10-30ms | 0ms |

*Recent data (last 10 min) uses raw query

## Configuration

### appsettings.json
```json
{
  "RequestReport": {
    "AggregationIntervalMinutes": 5,        // How often to run aggregation
    "AggregationLookbackDays": 30,          // Lookback for day aggregation
    "CacheDefaultTtlMinutes": 30,           // Cache TTL for historical data
    "CacheShortTtlMinutes": 2               // Cache TTL for recent data
  }
}
```

### Real-time Cache TTL
Hardcoded to 30 seconds for data where:
- Time range ≤ 1 hour, OR
- End time is within last 10 minutes

This ensures real-time dashboard updates without excessive database load.

## Testing Recommendations

### Manual Testing
1. **Real-time Test**:
   - Run K6 load generator
   - Open dashboard with `period=15m` or `period=1h`
   - Verify data updates every 30 seconds
   - Check for minute-level granularity

2. **Granularity Test**:
   - Test each period: 15m, 1h, 6h, 1d, 7d, 1m, 3m, 12m
   - Verify granularity matches: minute, minute, hour, hour, day, day, month, month

3. **Aggregation Test**:
   - Wait for worker to run (every 5 minutes)
   - Check logs for hourly/daily/monthly aggregation
   - Verify performance improvement for 1d, 7d, 1m periods

### Performance Testing
```bash
# Test real-time performance
k6 run --duration 5m load-test.js

# Monitor dashboard at /requestreport?period=1h
# Expect: 30-second refresh, minute-level data

# Test aggregate performance
# Monitor dashboard at /requestreport?period=7d
# Expect: Fast response (<100ms), day-level data
```

## Migration Notes

### Database
No schema changes required. Existing `RequestReportAggregates` table supports all granularities via string field.

### Breaking Changes
None. All existing API endpoints remain compatible. New period filters are additive.

### Backward Compatibility
✅ Existing period filters work as before
✅ Existing aggregates remain valid
✅ Cache keys include granularity, so old/new data don't conflict

## Monitoring

### Key Metrics to Watch

1. **Aggregation Duration**:
   - Target: < 10 seconds for hourly (7 days = 168 periods)
   - Target: < 5 seconds for daily (30 days = 30 periods)
   - Target: < 2 seconds for monthly (12 months = 12 periods)

2. **Cache Hit Rate**:
   - Real-time (15m-1h): 50-70% (30s TTL)
   - Recent (1d): 70-80% (2min TTL)
   - Historical (7d+): 90-95% (30min TTL)

3. **Query Performance**:
   - Raw query (minute, <1K records): < 100ms
   - Raw query (hour, <10K records): < 200ms
   - Aggregate query (any): < 50ms

### Log Messages
```
# Normal operation
[INFO] Starting aggregation process (attempt 1)
[INFO] Aggregating 168 periods for granularity Hour
[INFO] Aggregating 30 periods for granularity Day
[INFO] Aggregating 12 periods for granularity Month
[INFO] Aggregation process completed in 8523ms

# Real-time query
[DEBUG] Using raw query (granularity: Minute, hasFilters: False, isRecent: True)
[INFO] Cache miss for key: reqreport:2025-11-13 13:30_2025-11-13 13:45:Minute:

# Performance
[DEBUG] Cache hit for key: reqreport:2025-11-06_2025-11-13:Day:
```

## Future Enhancements

1. **Minute Aggregation** (Optional):
   - If very high traffic, consider minute-level aggregation for last 1 hour
   - Trade-off: More aggregation load vs better query performance

2. **Separate Tables** (If Needed):
   - Could split into `RequestReportAggregates_Hour`, `_Day`, `_Month`
   - Benefit: Better indexing, faster queries
   - Current: Single table works well with composite PK

3. **Intelligent Invalidation**:
   - Invalidate only affected hours when new data arrives
   - Currently: Invalidates entire range

4. **Redis Caching**:
   - For multi-instance deployment
   - Configure `UseDistributedCache: true`

## Summary

These improvements ensure the dashboard provides:
- ✅ **Real-time updates** for short periods (≤1 hour)
- ✅ **Proper granularity** matching user expectations
- ✅ **Fast performance** via optimized aggregation
- ✅ **Fresh data** for recent time ranges
- ✅ **Scalable** aggregation strategy

The system now supports real-time monitoring while maintaining excellent performance for historical analysis.
