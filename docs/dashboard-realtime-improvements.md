# Dashboard Real-time Improvements

## Overview

Applied the same real-time monitoring capabilities from `/requestreport` to `/dashboard` endpoint for consistent user experience across the application.

## Changes Made

### 1. Repository Layer - Enhanced Granularity Logic

**Files**: `Ce.Gateway.Api/Repositories/LogRepository.cs`

Updated `GetRequestTimelineAggregateAsync` and `GetLatencyTimelineAggregateAsync` methods:

```csharp
// OLD Logic (2 levels):
if (duration.TotalHours <= 1) → Minute granularity
else if (duration.TotalDays <= 1) → Hour granularity  
else → Day granularity

// NEW Logic (4 levels):
if (duration.TotalHours <= 1) → Minute granularity (real-time)
else if (duration.TotalDays <= 1) → Hour granularity
else if (duration.TotalDays <= 30) → Day granularity
else → Month granularity
```

**Benefits**:
- Minute-level detail for ≤1h ranges (real-time monitoring)
- Proper month-level aggregation for long-term historical data
- Consistent with `/requestreport` behavior

### 2. Service Layer - Real-time Cache Strategy

**File**: `Ce.Gateway.Api/Services/DashboardService.cs`

Updated `GetCacheDuration` method to match `/requestreport` strategy:

```csharp
// Cache TTL Strategy:
≤1h or last 10 min → 30s cache (real-time)
≤1 day → 2 min cache (recent data)
≤7 days → 10 min cache (short-term)
≤30 days → 30 min cache (medium-term)
>30 days → 6 hour cache (historical)
```

**Benefits**:
- Fresh data for real-time monitoring (30s refresh)
- Longer cache for historical data (better performance)
- Automatic detection of recent data (last 10 minutes)

### 3. Frontend - Time Filters

**Status**: Already present in `Ce.Gateway.Api/Views/Dashboard/Index.cshtml`

Dashboard already includes:
- 5m, 15m, 30m (real-time monitoring)
- 1h, 3h, 6h, 12h, 24h (intraday)
- 7d, 30d, 60d, 90d (historical)

Auto-refresh intervals:
- 10s, 30s, 1m, or manual/off

## Performance Expectations

### Real-time Queries (≤1h)
- **First Query**: 50-200ms (minute-level aggregation from DB)
- **Cached Query**: 0-10ms (30s cache TTL)
- **Expected Hit Rate**: 50-70%
- **Data Freshness**: 30 seconds

### Recent Queries (1d)
- **First Query**: 10-100ms (hour-level aggregation)
- **Cached Query**: 0-10ms (2min cache TTL)
- **Expected Hit Rate**: 70-80%
- **Data Freshness**: 2 minutes

### Historical Queries (7d-90d)
- **First Query**: 10-50ms (day/month aggregation)
- **Cached Query**: 0-10ms (10-30min cache TTL)
- **Expected Hit Rate**: 85-95%
- **Data Freshness**: 10-30 minutes

## Testing Guide

### 1. Real-time Monitoring (15m/30m/1h filters)

```javascript
// Manual test in browser console:
const startTime = new Date(Date.now() - 60*60*1000).toISOString(); // 1h ago
const endTime = new Date().toISOString();

fetch(`/api/dashboard/overview?startTime=${startTime}&endTime=${endTime}`)
  .then(r => r.json())
  .then(data => {
    console.log('Timeline points:', data.requestTimeline.length);
    console.log('First point:', data.requestTimeline[0]);
    console.log('Last point:', data.requestTimeline[data.requestTimeline.length-1]);
  });

// Expected: ~60 data points (1 per minute)
```

### 2. Cache Behavior

```javascript
// Test cache hit (repeat same query within 30s):
async function testCache() {
  const params = `startTime=${new Date(Date.now()-60*60*1000).toISOString()}&endTime=${new Date().toISOString()}`;
  
  console.time('First call');
  await fetch(`/api/dashboard/overview?${params}`).then(r => r.json());
  console.timeEnd('First call'); // Expected: 50-200ms
  
  console.time('Cached call');
  await fetch(`/api/dashboard/overview?${params}`).then(r => r.json());
  console.timeEnd('Cached call'); // Expected: <10ms
}

testCache();
```

### 3. Granularity Verification

Test different time ranges to verify correct granularity:

```
15m filter → Should see minute-level points (HH:mm format)
1d filter → Should see hour-level points (HH:00 format)
30d filter → Should see day-level points (yyyy-MM-dd format)
90d filter → Should see month-level points (yyyy-MM format)
```

## Configuration

No changes to `appsettings.json` required. Dashboard uses the same configuration as other services:

```json
{
  "Logging": {
    "LogLevel": {
      "Ce.Gateway.Api.Services.DashboardService": "Debug"
    }
  }
}
```

## Backwards Compatibility

✅ All changes are backwards compatible:
- Existing API contracts unchanged
- Frontend works with both old and new backend
- Cache keys include time range category for proper invalidation
- No database schema changes required

## Comparison with /requestreport

| Feature | /requestreport | /dashboard |
|---------|---------------|-----------|
| Minute granularity | ✅ Yes | ✅ Yes |
| Hour granularity | ✅ Yes | ✅ Yes |
| Day granularity | ✅ Yes | ✅ Yes |
| Month granularity | ✅ Yes | ✅ Yes |
| Real-time cache (30s) | ✅ Yes | ✅ Yes |
| Recent cache (2min) | ✅ Yes | ✅ Yes |
| Historical cache (30min) | ✅ Yes | ✅ Yes |
| Auto-refresh | ❌ No | ✅ Yes (10s/30s/1m) |
| sessionStorage cache | ✅ Yes | ❌ No (not needed with auto-refresh) |

## Known Limitations

1. **Minute-level for >1h**: Dashboard doesn't support minute-level detail for ranges >1h (by design, to prevent excessive data points)

2. **SQLite Date Formatting**: Uses `strftime()` which is SQLite-specific. For other databases, update the SQL format strings.

3. **Timezone**: Dashboard adds +7 hours for UTC+7 display. This is hardcoded and should be configurable for other timezones.

## Future Enhancements

### Potential Improvements:
1. Add aggregates table for dashboard (similar to requestreport)
2. Support minute-level queries for longer ranges (with sampling/downsampling)
3. Make timezone configurable
4. Add distributed cache support (Redis) for multi-instance deployments
5. Add dashboard-specific filter options (status codes, routes, nodes)

## Related Documentation

- `/docs/requestreport-realtime-improvements.md` - Original real-time implementation
- `/docs/requestreport-design.md` - Architecture and design decisions
- `/docs/requestreport-performance-report.md` - Performance analysis

## Commit Information

**Commit**: Applied dashboard real-time improvements to match /requestreport behavior
**Branch**: `feature/report-aggregates`
**PR**: Optimize request report queries with materialized aggregates and multi-layer caching
