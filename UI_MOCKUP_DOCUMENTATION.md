# UI Mockup Documentation - Route Configuration Management

## ⚠️ Important Note

**This document contains mockup descriptions, not actual screenshots.**

As an AI code agent, I cannot run the application or capture real screenshots. However, I can describe exactly what the UI should look like based on the code implementation.

**For actual screenshots:** A human tester must run the application and capture screens using their browser.

---

## Page 1: Route List (`/routes`)

### URL
```
http://localhost:5000/routes
```

### Layout Description

#### Header Section
```
┌─────────────────────────────────────────────────────────────────┐
│ Route Configuration Management                                  │
│                                                                  │
│ [View History Button] [Add Node Button]                         │
└─────────────────────────────────────────────────────────────────┘
```

#### Filter Section
```
┌─────────────────────────────────────────────────────────────────┐
│  Search Routes: [_________________]                             │
│                                                                  │
│  Filter by Scheme: [All Schemes ▼]                              │
└─────────────────────────────────────────────────────────────────┘
```

#### Route Cards Section

**Example Route Card 1:**
```
┌─────────────────────────────────────────────────────────────────┐
│ 🔀 /gateway/axsdk-api/{everything}          [Configure Button]  │
├─────────────────────────────────────────────────────────────────┤
│ Downstream: http:///{everything}                                │
│ HTTP Methods: POST, PUT, GET, DELETE, OPTIONS                   │
│                                                                  │
│ Load Balancer: [Least Connection]                               │
│ QoS: Timeout: 300000ms, Max Errors: 2                          │
│                                                                  │
│ Nodes (2):                                                       │
│ [localhost:10501 ✏️ ❌] [localhost:10502 ✏️ ❌]                  │
└─────────────────────────────────────────────────────────────────┘
```

**Example Route Card 2:**
```
┌─────────────────────────────────────────────────────────────────┐
│ 🔀 /gateway/auth/{everything}               [Configure Button]  │
├─────────────────────────────────────────────────────────────────┤
│ Downstream: http:///api/auth/{everything}                       │
│ HTTP Methods: POST, PUT, GET, DELETE, PATCH, OPTIONS            │
│                                                                  │
│ Load Balancer: None                                             │
│ QoS: No QoS configured                                          │
│                                                                  │
│ Nodes (1):                                                       │
│ [auth.dev.axe.vn:80 ✏️ ❌]                                       │
└─────────────────────────────────────────────────────────────────┘
```

### Expected Content Based on configuration.Development.json

The page should display routes from `configuration.Development.json` or `configuration.json` depending on environment. Each card shows:

1. **Upstream Path** (top, bold with icon)
2. **Downstream Info** (scheme + path template)
3. **HTTP Methods** (comma-separated list)
4. **Load Balancer Type** (badge)
5. **QoS Settings** (if configured)
6. **Nodes List** (badges with edit/delete icons)
7. **Configure Button** (top right)

### Color Scheme (AdminLTE)
- Primary: Blue (#007bff)
- Success: Green (#28a745)
- Warning: Yellow (#ffc107)
- Danger: Red (#dc3545)
- Info: Cyan (#17a2b8)

---

## Modal 1: Add Node Modal

### Trigger
Click the "Add Node" button (green, top-right of page)

### Modal Appearance
```
┌─────────────────────────────────────────────────────────────────┐
│ Add Node to Routes                                         [✕]  │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ Select Routes:                                                   │
│ ┌────────────────────────────────────────────────────────────┐  │
│ │ ☐ /gateway/axsdk-api/{everything}                         │  │
│ │ ☐ /gateway/auth/{everything}                              │  │
│ │ ☐ /gateway/file/{everything}                              │  │
│ │ ... (scrollable list)                                      │  │
│ └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│ Host: [___________________]                                      │
│       (e.g., localhost or api.example.com)                       │
│                                                                  │
│ Port: [_____]                                                    │
│       (e.g., 8080)                                               │
│                                                                  │
│ [Cancel]                            [Add Node]                   │
└─────────────────────────────────────────────────────────────────┘
```

### Form Fields
1. **Route Selection**: Checkboxes for each route
2. **Host**: Text input (required, regex validated)
3. **Port**: Number input (required, range 1-65535)

### Success Result
After clicking "Add Node":
- Green success alert appears: "Node localhost:8080 added successfully to 2 route(s)"
- Modal closes
- Route cards update to show new node badges

---

## Modal 2: Edit Node Modal

### Trigger
Click the pencil icon (✏️) on any node badge

### Modal Appearance
```
┌─────────────────────────────────────────────────────────────────┐
│ Edit Node                                                  [✕]  │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ Host: [localhost___________]                                     │
│                                                                  │
│ Port: [8080_]                                                    │
│                                                                  │
│ [Cancel]                            [Update Node]                │
└─────────────────────────────────────────────────────────────────┘
```

### Pre-filled Data
- Current host value
- Current port value

### Success Result
- Green alert: "Node updated successfully from localhost:8080 to newhost:8081"
- Modal closes
- Node badge updates in route card

---

## Modal 3: Edit Route Configuration Modal

### Trigger
Click the "Configure" button on any route card

### Modal Appearance
```
┌─────────────────────────────────────────────────────────────────┐
│ Edit Route Configuration                                   [✕]  │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ Downstream Scheme: [http ▼]    Downstream Path: [/api/...____] │
│                                                                  │
│ Load Balancer Type: [Least Connection ▼]                        │
│                                                                  │
│ QoS Settings:                                                    │
│ Timeout (ms):     [30000___]                                     │
│ Max Errors:       [3_____]                                       │
│ Break Duration:   [5000____]                                     │
│                                                                  │
│ ☐ Accept Any Server Certificate (Development only)              │
│                                                                  │
│ [Cancel]                            [Update Route]               │
└─────────────────────────────────────────────────────────────────┘
```

### Form Fields
1. **Downstream Scheme**: Dropdown (http/https)
2. **Downstream Path Template**: Text input
3. **Load Balancer Type**: Dropdown (None, Least Connection, Round Robin)
4. **Timeout**: Number input (milliseconds)
5. **Max Errors**: Number input
6. **Break Duration**: Number input (milliseconds)
7. **Certificate Checkbox**: Boolean

### Success Result
- Green alert: "Route updated successfully"
- Modal closes
- Route card updates to show new settings

---

## Page 2: Configuration History (`/routes/history`)

### URL
```
http://localhost:5000/routes/history
```

### Layout Description

#### Header
```
┌─────────────────────────────────────────────────────────────────┐
│ Configuration History                    [← Back to Routes]     │
└─────────────────────────────────────────────────────────────────┘
```

#### History Table
```
┌─────────────────────────────────────────────────────────────────┐
│ Status   │ Timestamp           │ Changed By │ Description       │
├──────────┼────────────────────┼────────────┼───────────────────┤
│ [Active] │ 2025-11-15 10:30   │ admin      │ Added node...    │
│ [------] │ 2025-11-15 10:25   │ admin      │ Deleted node...  │
│ [------] │ 2025-11-15 10:20   │ admin      │ Updated route... │
│          │                     │            │ [Rollback] button │
└─────────────────────────────────────────────────────────────────┘
```

### Table Columns
1. **Status**: Badge (green "Active" or gray "Historical")
2. **Timestamp**: Date and time
3. **Changed By**: Username
4. **Description**: Change description
5. **Actions**: Rollback button (yellow) for non-active entries

### Active Configuration
- Only ONE row has green "Active" badge
- This is the current configuration
- No rollback button on active row

---

## Modal 4: Rollback Confirmation

### Trigger
Click "Rollback" button on any historical configuration

### Modal Appearance
```
┌─────────────────────────────────────────────────────────────────┐
│ ⚠️ Confirm Rollback                                        [✕]  │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ Are you sure you want to rollback to this configuration?        │
│                                                                  │
│ Note: This will:                                                 │
│ • Create a backup of the current configuration                  │
│ • Restore the selected configuration                            │
│ • Trigger an automatic reload of the gateway                    │
│                                                                  │
│ [Cancel]                            [⟲ Rollback]                │
└─────────────────────────────────────────────────────────────────┘
```

### Warning Style
- Yellow/orange background on header
- Warning icon (⚠️)
- Informative bullet points

### Success Result
- Green alert: "Configuration rolled back successfully"
- Page refreshes after 1 second
- History table updates with new active configuration

---

## Alert Messages

### Success Alert (Green)
```
┌─────────────────────────────────────────────────────────────────┐
│ ✓ Node localhost:8080 added successfully to 2 route(s)     [✕] │
└─────────────────────────────────────────────────────────────────┘
```

### Error Alert (Red)
```
┌─────────────────────────────────────────────────────────────────┐
│ ✕ Failed to add node: Node may already exist               [✕] │
└─────────────────────────────────────────────────────────────────┘
```

### Alert Behavior
- Appears at top of content area
- Auto-dismisses after 5 seconds (success only)
- Can be manually closed with [✕] button
- Uses Font Awesome icons (✓ or ✕)

---

## Loading State

### When Page First Loads
```
┌─────────────────────────────────────────────────────────────────┐
│                                                                  │
│                     ⟳ (spinning icon)                            │
│                   Loading routes...                              │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Spinner Details
- Font Awesome spinner icon (`fa-spinner fa-spin`)
- Blue color (#007bff)
- Size: 3x (large)
- Centered on page

---

## Mobile Responsive View (< 768px)

### Layout Changes
1. **Buttons stack vertically**
   ```
   [View History Button - Full Width]
   [Add Node Button - Full Width]
   ```

2. **Search/Filter fields stack**
   ```
   Search: [___________]
   Filter: [___________]
   ```

3. **Route cards full width**
   - Single column layout
   - All information still visible
   - Buttons remain touchable (44px minimum)

4. **Modals adapt**
   - Full width on mobile
   - Scrollable if content is tall
   - Touch-friendly controls

---

## Browser Console Output

### Successful Load
```javascript
Loading routes from /api/routes
Routes API response: {success: true, data: Array(15)}
```

### On Add Node
```javascript
Success: Node localhost:8080 added successfully to 2 route(s)
```

### On Error
```javascript
Error: Failed to add node: Node may already exist
Error loading routes: 500 Internal Server Error
```

---

## Network Tab Expected Calls

### GET /api/routes
```
Status: 200 OK
Content-Type: application/json

{
  "success": true,
  "data": [
    {
      "routeId": "L2dhdGV3YXkvYXhzZGstaXBpL3tldmVyeXRoaW5nfQ==",
      "upstreamPathTemplate": "/gateway/axsdk-api/{everything}",
      "downstreamScheme": "http",
      "downstreamPathTemplate": "/{everything}",
      "downstreamHostAndPorts": [
        {"host": "localhost", "port": 10501},
        {"host": "localhost", "port": 10502}
      ],
      "upstreamHttpMethod": ["POST", "PUT", "GET", "DELETE", "OPTIONS"],
      "loadBalancerOptions": {"type": "LeastConnection"},
      "qoSOptions": {
        "timeoutValue": 300000,
        "exceptionsAllowedBeforeBreaking": 2,
        "durationOfBreak": 5000
      }
    }
    // ... more routes
  ]
}
```

### POST /api/routes/nodes
```
Request Body:
{
  "routeIds": ["L2dhdGV3YXkvYXhzZGstaXBpL3tldmVyeXRoaW5nfQ=="],
  "host": "localhost",
  "port": 8080
}

Response: 200 OK
{
  "success": true,
  "message": "Node localhost:8080 added successfully to 1 route(s)"
}
```

### GET /api/routes/history
```
Status: 200 OK
{
  "success": true,
  "data": [
    {
      "id": "abc123",
      "timestamp": "2025-11-15T10:30:00Z",
      "changedBy": "admin",
      "description": "Added node localhost:8080 to 1 route(s)",
      "backupFilePath": "/path/to/backup.json",
      "isActive": true
    }
    // ... more history entries
  ]
}
```

---

## Verification Checklist

To verify the UI is working correctly, check:

### ✅ Route List Page
- [ ] All routes from config file are displayed
- [ ] Each route shows complete information
- [ ] Search filter works in real-time
- [ ] Scheme filter shows correct routes
- [ ] Configure buttons visible
- [ ] Node badges show with edit/delete icons

### ✅ Add Node Functionality
- [ ] Modal opens when clicking "Add Node"
- [ ] Route checkboxes populate correctly
- [ ] Can select multiple routes
- [ ] Host/port validation works
- [ ] Success message appears
- [ ] New nodes appear in route cards

### ✅ Edit Node Functionality
- [ ] Modal opens with current values
- [ ] Can modify host and port
- [ ] Success message appears
- [ ] Node badge updates

### ✅ Delete Node Functionality
- [ ] Confirmation dialog appears
- [ ] Node is removed after confirmation
- [ ] Success message appears

### ✅ Route Configuration
- [ ] Modal opens with current settings
- [ ] All fields are editable
- [ ] Dropdowns work correctly
- [ ] Success message appears
- [ ] Route card updates

### ✅ Configuration History
- [ ] History page loads
- [ ] All changes are logged
- [ ] Active configuration is marked
- [ ] Rollback buttons work

### ✅ Rollback
- [ ] Confirmation modal appears
- [ ] Rollback executes successfully
- [ ] Routes revert to previous state
- [ ] New history entry created

---

## File Locations for Reference

### Views
- `/Ce.Gateway.Api/Views/RouteConfig/Index.cshtml` - Main page
- `/Ce.Gateway.Api/Views/RouteConfig/History.cshtml` - History page

### JavaScript
- `/Ce.Gateway.Api/wwwroot/js/routeconfig.js` - Main functionality
- `/Ce.Gateway.Api/wwwroot/js/routeconfig-history.js` - History functionality

### API Endpoints
- `/Ce.Gateway.Api/Controllers/Api/RouteConfigController.cs` - REST API

### Service
- `/Ce.Gateway.Api/Services/RouteConfigService.cs` - Business logic

---

## To Get Actual Screenshots

**Steps for human tester:**

1. **Start the application**
   ```bash
   cd /home/runner/work/axe-gateway/axe-gateway/Ce.Gateway.Api
   dotnet run
   ```

2. **Open browser**
   - Navigate to: `http://localhost:5000/routes`
   - Login as Administrator

3. **Capture screenshots**
   - Use Print Screen (Windows)
   - Use Command+Shift+4 (Mac)
   - Use browser DevTools screenshot feature (F12 → three dots → Capture screenshot)

4. **Follow testing guide**
   - Open `TESTING_GUIDE_DETAILED.md`
   - Complete each test case
   - Capture screenshot for each step

5. **Share screenshots**
   - Save to a folder
   - Upload to issue/PR
   - Or share via team communication tool

---

**Document Type**: UI Mockup/Description (Not Actual Screenshots)  
**Created By**: Copilot (AI Code Agent)  
**Date**: 2025-11-15  
**Purpose**: Show expected UI appearance based on implemented code
