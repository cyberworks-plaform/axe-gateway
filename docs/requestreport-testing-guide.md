# Request Report Testing Guide

## Overview

This document provides comprehensive testing procedures for the request report performance optimization feature, covering both backend and frontend functionality.

## Backend Testing

### Automated Unit Tests

Run all new tests:
```bash
cd /home/runner/work/axe-gateway/axe-gateway

# Run all request report tests
dotnet test Ce.Gateway.Api.Tests/Ce.Gateway.Api.Tests.csproj \
  --filter "FullyQualifiedName~RequestReport"

# Run specific test classes
dotnet test Ce.Gateway.Api.Tests/Ce.Gateway.Api.Tests.csproj \
  --filter "FullyQualifiedName~RequestReportServiceTests"

dotnet test Ce.Gateway.Api.Tests/Ce.Gateway.Api.Tests.csproj \
  --filter "FullyQualifiedName~RequestReportAggregationWorkerTests"

dotnet test Ce.Gateway.Api.Tests/Ce.Gateway.Api.Tests.csproj \
  --filter "FullyQualifiedName~RequestReportControllerTests"

dotnet test Ce.Gateway.Api.Tests/Ce.Gateway.Api.Tests.csproj \
  --filter "FullyQualifiedName~RequestReportIntegrationTests"
```

### Test Coverage

**RequestReportServiceTests (5 tests)**
- ✅ Cache hit/miss behavior
- ✅ Cache invalidation
- ✅ Data source selection (aggregates vs raw)
- ✅ Filter handling
- ✅ Hourly granularity

**RequestReportAggregationWorkerTests (3 tests)**
- ✅ Aggregate upsert operations
- ✅ Aggregate retrieval
- ✅ Last updated timestamp tracking

**RequestReportControllerTests (5 tests)**
- ✅ Period to granularity mapping
- ✅ Date range calculation
- ✅ Default period handling
- ✅ Invalid period handling
- ✅ Service integration

**RequestReportIntegrationTests (6 tests)**
- ✅ End-to-end with raw data
- ✅ End-to-end with aggregates
- ✅ Cache invalidation flow
- ✅ Filtered queries
- ✅ Performance comparison

**Total: 19 automated tests**

## Frontend Testing (Manual)

### Prerequisites

1. Application running with sample data
2. Browser with DevTools open (F12)
3. Clear browser cache before starting tests

### Test 1: Basic Functionality

**Objective**: Verify the page loads and displays data correctly

**Steps**:
1. Navigate to `/requestreport`
2. Observe the page loads without errors
3. Check that summary cards show numbers (not "...")
4. Verify chart displays with data

**Expected Results**:
- Page loads in < 5 seconds
- Summary cards show: Total Requests, Success (2xx), Client Error (4xx), Server Error (5xx)
- Chart displays stacked bar graph with 4 data series
- Footer shows "Report at [timestamp] (Generated in X ms)"

**Pass/Fail**: ___________

---

### Test 2: Cache Behavior - First Load

**Objective**: Verify first load fetches from server

**Steps**:
1. Open DevTools → Console tab
2. Navigate to `/requestreport`
3. Watch console for cache messages

**Expected Results**:
- Console shows: No "Using cached data" message
- Footer shows generation time (e.g., "Generated in 500 ms")
- No "[cached]" indicator in footer

**Pass/Fail**: ___________

---

### Test 3: Cache Behavior - Second Load

**Objective**: Verify cache works on page refresh

**Steps**:
1. With page already loaded from Test 2
2. Refresh the page (F5)
3. Watch console for cache messages

**Expected Results**:
- Console shows: "Using cached data (age: X s)"
- Footer shows "[cached]" indicator
- Page loads faster than first time (< 100ms)

**Pass/Fail**: ___________

---

### Test 4: Period Filter - All Options

**Objective**: Verify all period filters work correctly

**Steps**:
1. Navigate to `/requestreport`
2. Open DevTools → Network tab
3. Test each period option:
   - Last 1 Day
   - Last 7 Days
   - Last 1 Month
   - Last 3 Months
   - Last 9 Months
   - Last 12 Months
4. For each selection, verify:
   - Data loads
   - Chart updates
   - Summary cards update

**Expected Results**:
- Each period selection triggers API call to `/api/requestreport/data?period=XX`
- Chart time labels match selected period:
  - 1 Day: Hourly labels (00:00, 01:00, etc.)
  - 7 Days: Daily labels (MM/dd)
  - 1+ Month: Monthly labels (MMM yyyy)
- Summary totals make sense for the period

**Pass/Fail for each period**:
- 1d: ___________
- 7d: ___________
- 1m: ___________
- 3m: ___________
- 9m: ___________
- 12m: ___________

---

### Test 5: Cache Expiration (5 minutes)

**Objective**: Verify cache expires after 5 minutes

**Steps**:
1. Navigate to `/requestreport`
2. Note the current time
3. Wait 6 minutes (or modify CACHE_TTL_MS in code to 10 seconds for faster testing)
4. Refresh the page
5. Check console and footer

**Expected Results**:
- After expiration, console shows no "Using cached data" message
- Footer shows fresh generation time without "[cached]"
- New API call made to server

**Pass/Fail**: ___________

---

### Test 6: Multiple Period Switches

**Objective**: Verify cache works independently for different periods

**Steps**:
1. Navigate to `/requestreport`
2. Select "Last 7 Days" → Wait for load → Note "[cached]" status
3. Select "Last 1 Month" → Wait for load → Should NOT show "[cached]"
4. Select "Last 7 Days" again → Should show "[cached]"
5. Select "Last 1 Month" again → Should show "[cached]"

**Expected Results**:
- Each period has its own cache entry
- Switching to previously loaded period shows "[cached]"
- First load of a new period does NOT show "[cached]"

**Pass/Fail**: ___________

---

### Test 7: SessionStorage Inspection

**Objective**: Verify cache is stored in sessionStorage correctly

**Steps**:
1. Navigate to `/requestreport`
2. Load "Last 7 Days"
3. Open DevTools → Application tab → Storage → Session Storage
4. Find entries starting with "reqreport_"
5. Inspect the data structure

**Expected Results**:
- Session storage contains key "reqreport_7d"
- Value is JSON with structure:
  ```json
  {
    "data": { ... report data ... },
    "timestamp": 1234567890
  }
  ```
- timestamp is within last few seconds

**Pass/Fail**: ___________

---

### Test 8: Error Handling - Network Failure

**Objective**: Verify graceful error handling

**Steps**:
1. Open DevTools → Network tab
2. Enable "Offline" mode (or throttle to Offline)
3. Navigate to `/requestreport`
4. Observe behavior

**Expected Results**:
- Alert shows: "Failed to load report data. Please try again."
- Console shows error message
- Page doesn't crash
- Summary cards show "..." or previous values

**Pass/Fail**: ___________

---

### Test 9: Browser Compatibility

**Objective**: Verify works across browsers

**Browsers to test**:
- [ ] Chrome/Edge
- [ ] Firefox
- [ ] Safari

**Steps** (for each browser):
1. Navigate to `/requestreport`
2. Verify page loads and displays correctly
3. Test period filter
4. Verify cache works (refresh page, check for "[cached]")

**Expected Results**:
- Identical behavior across all browsers
- No console errors
- Cache works in all browsers

**Pass/Fail**:
- Chrome/Edge: ___________
- Firefox: ___________
- Safari: ___________

---

### Test 10: Performance Measurement

**Objective**: Measure actual performance improvements

**Steps**:
1. Clear cache completely (DevTools → Application → Clear storage)
2. Navigate to `/requestreport` with DevTools Network tab open
3. Select "Last 7 Days"
4. Record generation time from footer: __________ ms
5. Refresh the page
6. Record cached generation time: __________ ms
7. Calculate improvement: ((first - cached) / first * 100) = __________ %

**Expected Results**:
- First load: 200-2000ms (depending on data volume)
- Cached load: < 50ms
- Improvement: > 90%

**Pass/Fail**: ___________

---

## Backend Integration Testing (Manual)

### Test 11: Background Worker Verification

**Objective**: Verify worker creates aggregates

**Steps**:
1. Start application
2. Wait 35 seconds (30s startup delay + 5s interval)
3. Check logs for:
   ```
   [Information] RequestReportAggregationWorker started
   [Information] Starting aggregation process
   [Information] Aggregation process completed in XXXms
   ```
4. Query database:
   ```sql
   SELECT COUNT(*) FROM RequestReportAggregates;
   ```

**Expected Results**:
- Worker starts and runs successfully
- Aggregates table populated with data
- No errors in logs

**Pass/Fail**: ___________

---

### Test 12: Aggregate vs Raw Query Performance

**Objective**: Measure performance difference

**Steps**:
1. Ensure aggregates table has data (run worker)
2. Make API call: `GET /api/requestreport/data?period=7d`
3. Record response time (should use aggregates)
4. Make API call: `GET /api/requestreport/data?period=1d`
5. Record response time (should use raw query)

**Expected Results**:
- 7d query (aggregates): < 100ms
- 1d query (raw): 100-500ms (depending on data)
- Aggregates query faster or similar

**7d time**: __________ ms
**1d time**: __________ ms

**Pass/Fail**: ___________

---

### Test 13: Cache Invalidation on Update

**Objective**: Verify cache is invalidated when aggregates update

**Steps**:
1. Load page with "Last 7 Days" → Record total requests
2. Wait for worker to run (5 minutes)
3. Add new log entries to database
4. Wait for next worker run
5. Refresh page
6. Verify data updated

**Expected Results**:
- After worker runs, cache is invalidated
- Page shows updated data on next load

**Pass/Fail**: ___________

---

## Test Summary Template

**Date**: _______________
**Tester**: _______________
**Environment**: _______________

| Test # | Test Name | Status | Notes |
|--------|-----------|--------|-------|
| 1 | Basic Functionality | ⬜ | |
| 2 | Cache - First Load | ⬜ | |
| 3 | Cache - Second Load | ⬜ | |
| 4 | Period Filters | ⬜ | |
| 5 | Cache Expiration | ⬜ | |
| 6 | Multiple Periods | ⬜ | |
| 7 | SessionStorage | ⬜ | |
| 8 | Error Handling | ⬜ | |
| 9 | Browser Compat | ⬜ | |
| 10 | Performance | ⬜ | |
| 11 | Worker Verification | ⬜ | |
| 12 | Query Performance | ⬜ | |
| 13 | Cache Invalidation | ⬜ | |

**Overall Result**: ⬜ Pass / ⬜ Fail

**Issues Found**:
1. _______________________________________________
2. _______________________________________________
3. _______________________________________________

**Recommendations**:
1. _______________________________________________
2. _______________________________________________
3. _______________________________________________

---

## Troubleshooting Common Issues

### Issue: "[cached]" never appears

**Possible Causes**:
- Cache TTL too short
- SessionStorage disabled in browser
- JavaScript error preventing cache read

**Solution**:
1. Check browser console for errors
2. Verify sessionStorage is enabled
3. Check `CACHE_TTL_MS` value in code

### Issue: Data not updating after worker runs

**Possible Causes**:
- Worker not running
- Cache not being invalidated
- Browser still showing cached data

**Solution**:
1. Check worker logs
2. Clear browser cache
3. Verify `InvalidateCache` is called in worker

### Issue: Tests timing out

**Possible Causes**:
- Database slow
- Too much test data
- Network issues

**Solution**:
1. Reduce test data volume
2. Increase test timeout values
3. Check database performance

---

## JavaScript Console Tests

You can also run these commands in browser console for quick verification:

```javascript
// Test 1: Check cache exists
console.log(sessionStorage.getItem('reqreport_7d'));

// Test 2: Clear cache
sessionStorage.clear();

// Test 3: Check cache age
const cached = JSON.parse(sessionStorage.getItem('reqreport_7d'));
if (cached) {
    const ageSeconds = (Date.now() - cached.timestamp) / 1000;
    console.log(`Cache age: ${ageSeconds} seconds`);
}

// Test 4: Force reload without cache
sessionStorage.clear();
location.reload();

// Test 5: Verify CACHE_TTL_MS setting
console.log(`Cache TTL: ${CACHE_TTL_MS / 1000} seconds`);
```

---

## Appendix: Expected Test Data Volumes

For optimal testing, ensure database has:
- Minimum 1000 log entries
- Data spanning at least 30 days
- Mix of status codes (2xx, 4xx, 5xx)
- At least 10 entries per day

Use the simulator to generate test data:
```bash
./run_simulator.ps1
```
