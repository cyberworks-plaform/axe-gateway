# Bug Fix: Route List Not Displaying

## Issue
When accessing the `/routes` page, the route list was not displaying. The page would load but show no routes.

## Root Cause
The `JsonSerializerOptions` in `RouteConfigService.cs` was configured with:
```csharp
PropertyNamingPolicy = JsonNamingPolicy.CamelCase
```

This caused the JSON deserializer to expect properties in camelCase format (e.g., `routes`, `downstreamPathTemplate`), but Ocelot configuration files use PascalCase (e.g., `Routes`, `DownstreamPathTemplate`).

### Example of the mismatch:

**Configuration File (PascalCase):**
```json
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [...]
    }
  ]
}
```

**Deserializer Expected (camelCase):**
```json
{
  "routes": [
    {
      "downstreamPathTemplate": "/{everything}",
      "downstreamScheme": "http",
      "downstreamHostAndPorts": [...]
    }
  ]
}
```

## Solution

### 1. Fixed JSON Serializer Options
Changed in `RouteConfigService.cs`:
```csharp
private static readonly JsonSerializerOptions _jsonOptions = new()
{
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = null, // Use PascalCase to match Ocelot configuration format
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    PropertyNameCaseInsensitive = true // Allow case-insensitive property matching
};
```

### 2. Improved Error Handling
- Replaced `toastr` notifications (which wasn't included) with Bootstrap alerts
- Added comprehensive console logging for debugging
- Added detailed error messages in AJAX error callbacks

### 3. Added Loading Indicator
- Added a spinner that shows while routes are being loaded
- Provides better UX feedback to users

## Files Changed
1. `Ce.Gateway.Api/Services/RouteConfigService.cs` - Fixed JSON serializer options
2. `Ce.Gateway.Api/wwwroot/js/routeconfig.js` - Improved error handling and added loading indicator
3. `Ce.Gateway.Api/wwwroot/js/routeconfig-history.js` - Improved error handling
4. `Ce.Gateway.Api/Views/RouteConfig/Index.cshtml` - Added loading indicator

## Testing
After these changes, the route list should display correctly when:
1. Accessing `/routes` page as an Administrator
2. Using any environment (Development, Production, etc.)
3. The configuration file exists and is valid JSON

## Expected Behavior
- Page loads with a spinner
- Routes are fetched from `/api/routes`
- Each route is displayed in a card showing:
  - Upstream path template
  - Downstream scheme and path
  - List of nodes (host:port)
  - Load balancer type
  - QoS settings
- Filter and search functionality works correctly

## Debugging
If routes still don't load:
1. Open browser console (F12)
2. Check for error messages
3. Look for the "Loading routes from /api/routes" log message
4. Check the API response structure
5. Verify the configuration file exists and has the correct format
