# Comprehensive Testing Guide - Route Configuration Management

## Prerequisites
- Application running in Development or Production environment
- User logged in with **Administrator** role
- Browser console open (F12) for debugging if needed

---

## Test Suite Overview

### Test Categories
1. ✅ Route Viewing & Filtering
2. ✅ Node Management (Add/Edit/Delete)
3. ✅ Route Configuration Updates
4. ✅ Configuration History
5. ✅ Rollback Functionality
6. ✅ Error Handling
7. ✅ Mobile Responsiveness

---

## Detailed Test Cases

### 1. Route Viewing & Filtering

#### Test 1.1: Access Route Management Page
**Steps:**
1. Login as Administrator
2. Click "Route Configuration" in sidebar menu
3. URL should be `/routes`

**Expected Result:**
- Page loads successfully
- Loading spinner shows briefly
- List of routes displays in card format
- Each route card shows:
  - Upstream path template (e.g., `/gateway/axsdk-api/{everything}`)
  - Downstream scheme and path
  - HTTP methods (GET, POST, etc.)
  - Load balancer type (if configured)
  - QoS settings (if configured)
  - List of nodes (host:port pairs)

**Screenshot Checklist:**
- [ ] Full page view showing all routes
- [ ] Close-up of a single route card showing all details

---

#### Test 1.2: Search Filter
**Steps:**
1. In the "Search Routes" field, type: `axsdk`
2. Observe route list updates

**Expected Result:**
- Routes are filtered in real-time
- Only routes with "axsdk" in upstream path show
- Other routes are hidden

**Screenshot Checklist:**
- [ ] Search field with "axsdk" entered
- [ ] Filtered results showing only matching routes

---

#### Test 1.3: Scheme Filter
**Steps:**
1. Select "HTTP" from "Filter by Scheme" dropdown
2. Observe route list updates
3. Select "HTTPS" 
4. Select "All Schemes"

**Expected Result:**
- List filters to show only HTTP routes
- Then shows only HTTPS routes
- Then shows all routes again

**Screenshot Checklist:**
- [ ] Filter dropdown with HTTP selected
- [ ] Filtered results

---

### 2. Node Management - Add Node

#### Test 2.1: Add Node to Single Route
**Steps:**
1. Click "Add Node" button (green, top-right)
2. Modal opens titled "Add Node to Routes"
3. Check ONE route checkbox
4. Enter Host: `localhost`
5. Enter Port: `8080`
6. Click "Add Node" button

**Expected Result:**
- Success alert appears at top: "Node localhost:8080 added successfully to 1 route(s)"
- Modal closes
- Selected route card now shows the new node badge
- Node badge shows: `localhost:8080` with edit and delete icons

**Screenshot Checklist:**
- [ ] "Add Node" modal open with form filled
- [ ] Success alert message
- [ ] Route card showing new node badge

---

#### Test 2.2: Add Node to Multiple Routes
**Steps:**
1. Click "Add Node" button
2. Check MULTIPLE route checkboxes (e.g., 3 routes)
3. Enter Host: `api.example.com`
4. Enter Port: `9090`
5. Click "Add Node"

**Expected Result:**
- Success message: "Node api.example.com:9090 added successfully to 3 route(s)"
- All 3 selected routes now show the new node

**Screenshot Checklist:**
- [ ] Modal with multiple routes selected
- [ ] Multiple route cards showing the new node

---

#### Test 2.3: Add Duplicate Node (Error Case)
**Steps:**
1. Try to add a node that already exists in a route
2. Click "Add Node"

**Expected Result:**
- Error alert: "Failed to add node. Node may already exist or routes not found."
- Modal stays open
- No changes to routes

**Screenshot Checklist:**
- [ ] Error alert message
- [ ] Modal still open

---

### 3. Node Management - Edit Node

#### Test 3.1: Edit Existing Node
**Steps:**
1. On a route card, find a node badge
2. Click the edit icon (pencil) on the node
3. Modal opens: "Edit Node"
4. Change Host from `localhost` to `newhost.local`
5. Change Port from `8080` to `8081`
6. Click "Update Node"

**Expected Result:**
- Success message: "Node updated successfully from localhost:8080 to newhost.local:8081"
- Modal closes
- Node badge updates to show: `newhost.local:8081`

**Screenshot Checklist:**
- [ ] Edit modal with current values
- [ ] Edit modal with new values entered
- [ ] Success message
- [ ] Updated node badge

---

### 4. Node Management - Delete Node

#### Test 4.1: Delete Node
**Steps:**
1. On a route card, find a node badge
2. Click the delete icon (X) on the node
3. Browser confirmation dialog appears: "Are you sure you want to delete node [host]:[port]?"
4. Click OK/Yes

**Expected Result:**
- Success message: "Node [host]:[port] deleted successfully from 1 route(s)"
- Node badge disappears from route card

**Screenshot Checklist:**
- [ ] Confirmation dialog
- [ ] Route card before deletion
- [ ] Route card after deletion (node removed)
- [ ] Success message

---

### 5. Route Configuration Updates

#### Test 5.1: Update Load Balancer
**Steps:**
1. Click "Configure" button on a route card
2. Modal opens: "Edit Route Configuration"
3. Change "Load Balancer Type" from "Least Connection" to "Round Robin"
4. Click "Update Route"

**Expected Result:**
- Success message: "Route updated successfully"
- Modal closes
- Route card now shows "Round Robin" badge

**Screenshot Checklist:**
- [ ] Configure modal with all fields
- [ ] Load balancer dropdown with options
- [ ] Success message
- [ ] Updated route card

---

#### Test 5.2: Update QoS Settings
**Steps:**
1. Click "Configure" on a route
2. In QoS section:
   - Set Timeout: `60000` (60 seconds)
   - Set Exceptions Before Breaking: `5`
   - Set Break Duration: `10000` (10 seconds)
3. Click "Update Route"

**Expected Result:**
- Success message appears
- Route card shows updated QoS: "Timeout: 60000ms, Max Errors: 5"

**Screenshot Checklist:**
- [ ] QoS fields filled in modal
- [ ] Updated route card showing QoS info

---

#### Test 5.3: Change Downstream Scheme
**Steps:**
1. Click "Configure" on a route
2. Change "Downstream Scheme" from "http" to "https"
3. Click "Update Route"

**Expected Result:**
- Success message
- Route card now shows: "https://[path]"

**Screenshot Checklist:**
- [ ] Scheme dropdown in modal
- [ ] Updated route card

---

### 6. Configuration History

#### Test 6.1: View History Page
**Steps:**
1. From `/routes` page, click "View History" button (blue, top-right)
2. URL changes to `/routes/history`

**Expected Result:**
- History page loads
- Table shows configuration changes with columns:
  - Status (Active/Historical badge)
  - Timestamp
  - Changed By (username)
  - Description
  - Actions (Rollback button)
- Most recent change at top
- Active configuration marked with green "Active" badge

**Screenshot Checklist:**
- [ ] Full history page
- [ ] Table with all columns visible
- [ ] Active configuration highlighted

---

#### Test 6.2: History Entries After Changes
**Steps:**
1. Go back to `/routes`
2. Make a change (add a node)
3. Return to `/routes/history`

**Expected Result:**
- New entry at top of history table
- Description matches the change made
- Your username in "Changed By" column
- Previous active config now marked as "Historical"
- New config marked as "Active"

**Screenshot Checklist:**
- [ ] New history entry
- [ ] Correct description and user

---

### 7. Rollback Functionality

#### Test 7.1: Rollback Configuration
**Steps:**
1. On history page, find a historical configuration (not active)
2. Click "Rollback" button (yellow/warning color)
3. Confirmation modal appears
4. Read the warning message
5. Click "Rollback" button in modal

**Expected Result:**
- Success message: "Configuration rolled back successfully"
- Modal closes
- History table updates
- The rolled-back configuration now marked "Active"
- A new entry appears: "Pre-rollback backup..."
- Page auto-refreshes after 1 second

**Screenshot Checklist:**
- [ ] Rollback confirmation modal
- [ ] Warning message details
- [ ] Success alert
- [ ] Updated history table with new active config

---

#### Test 7.2: Verify Rollback Effect
**Steps:**
1. After rollback, click "Back to Routes"
2. Check route configurations

**Expected Result:**
- Routes match the configuration from the rollback timestamp
- Any changes made after that timestamp are reverted

**Screenshot Checklist:**
- [ ] Routes page after rollback
- [ ] Comparison showing reverted changes

---

### 8. Error Handling

#### Test 8.1: Invalid Port Number
**Steps:**
1. Click "Add Node"
2. Enter Host: `localhost`
3. Enter Port: `99999` (invalid, > 65535)
4. Click "Add Node"

**Expected Result:**
- Browser validation error
- Red border on port field
- Error message: "Value must be between 1 and 65535"
- Cannot submit

**Screenshot Checklist:**
- [ ] Validation error on port field

---

#### Test 8.2: Empty Host Field
**Steps:**
1. Click "Add Node"
2. Leave Host field empty
3. Enter Port: `8080`
4. Try to submit

**Expected Result:**
- Browser validation error
- Red border on host field
- Error message: "This field is required"
- Cannot submit

**Screenshot Checklist:**
- [ ] Validation error on host field

---

#### Test 8.3: Invalid Host Characters
**Steps:**
1. Click "Add Node"
2. Enter Host: `host@#$%invalid`
3. Enter Port: `8080`
4. Click "Add Node"

**Expected Result:**
- Server validation error
- Error alert: "Host must contain only alphanumeric characters, dots, and hyphens"

**Screenshot Checklist:**
- [ ] Error alert message

---

### 9. Mobile Responsiveness

#### Test 9.1: Mobile View - Routes List
**Steps:**
1. Resize browser to mobile width (375px)
2. Or use browser DevTools device emulation (iPhone, Android)
3. Navigate to `/routes`

**Expected Result:**
- Layout adjusts to mobile
- Search and filter fields stack vertically
- Route cards fill width
- "Add Node" and "View History" buttons stack or wrap
- All content readable without horizontal scroll
- Buttons remain touchable (not too small)

**Screenshot Checklist:**
- [ ] Mobile view of routes page
- [ ] Mobile view of route card
- [ ] Mobile view of filter section

---

#### Test 9.2: Mobile View - Modals
**Steps:**
1. On mobile view, click "Add Node"
2. Check modal display
3. Try "Edit Node" modal
4. Try "Configure" modal

**Expected Result:**
- Modals fit mobile screen
- Form fields are accessible
- Buttons are touchable
- Scrolling works if content is tall

**Screenshot Checklist:**
- [ ] Add Node modal on mobile
- [ ] Edit modal on mobile
- [ ] Configure modal on mobile

---

### 10. Browser Console Verification

#### Test 10.1: Check Console Logs
**Steps:**
1. Open browser DevTools (F12)
2. Go to Console tab
3. Navigate to `/routes`
4. Perform various operations

**Expected Result:**
- Console shows logs:
  - "Loading routes from /api/routes"
  - "Routes API response: ..." with data
  - Success/error messages
- No JavaScript errors (red text)
- No 404 errors for resources

**Screenshot Checklist:**
- [ ] Console showing successful logs
- [ ] No errors present

---

#### Test 10.2: Network Tab Check
**Steps:**
1. Open DevTools Network tab
2. Reload `/routes` page
3. Check API calls

**Expected Result:**
- GET `/api/routes` returns 200 status
- Response contains route data in JSON
- POST/PUT/DELETE operations return appropriate status codes
- No failed requests (except expected validation errors)

**Screenshot Checklist:**
- [ ] Network tab showing successful API calls
- [ ] Response payload with route data

---

## Summary Checklist

After completing all tests, verify:

### Functional Requirements
- [ ] All routes display correctly
- [ ] Search and filter work properly
- [ ] Nodes can be added to single/multiple routes
- [ ] Nodes can be edited
- [ ] Nodes can be deleted
- [ ] Route parameters can be updated
- [ ] Configuration history is tracked
- [ ] Rollback functionality works
- [ ] All error cases handled gracefully

### Non-Functional Requirements
- [ ] UI is responsive on desktop
- [ ] UI works on mobile devices
- [ ] Loading indicators show appropriately
- [ ] Success/error messages are clear
- [ ] No JavaScript console errors
- [ ] All API calls succeed
- [ ] Performance is acceptable

### Security
- [ ] Only Administrators can access pages
- [ ] Authorization checks in place
- [ ] Input validation works
- [ ] No sensitive data in console logs

---

## Bug Reporting Template

If you find issues during testing, report them with:

```
**Bug Title**: [Short description]

**Steps to Reproduce**:
1. Step 1
2. Step 2
3. Step 3

**Expected Result**: 
[What should happen]

**Actual Result**: 
[What actually happened]

**Environment**:
- Browser: [Chrome/Firefox/Edge/Safari]
- Version: [e.g., Chrome 120]
- Environment: [Development/Production]
- User Role: Administrator

**Screenshots**: 
[Attach screenshots]

**Console Errors**: 
[Copy any error messages from browser console]

**Severity**: [Critical/High/Medium/Low]
```

---

## Test Execution Log

Use this table to track testing progress:

| Test ID | Test Case | Status | Tester | Date | Notes |
|---------|-----------|--------|--------|------|-------|
| 1.1 | Access Route Page | ⬜ | | | |
| 1.2 | Search Filter | ⬜ | | | |
| 1.3 | Scheme Filter | ⬜ | | | |
| 2.1 | Add Node (Single) | ⬜ | | | |
| 2.2 | Add Node (Multiple) | ⬜ | | | |
| 2.3 | Add Duplicate Node | ⬜ | | | |
| 3.1 | Edit Node | ⬜ | | | |
| 4.1 | Delete Node | ⬜ | | | |
| 5.1 | Update Load Balancer | ⬜ | | | |
| 5.2 | Update QoS | ⬜ | | | |
| 5.3 | Change Scheme | ⬜ | | | |
| 6.1 | View History | ⬜ | | | |
| 6.2 | History After Changes | ⬜ | | | |
| 7.1 | Rollback Config | ⬜ | | | |
| 7.2 | Verify Rollback | ⬜ | | | |
| 8.1 | Invalid Port | ⬜ | | | |
| 8.2 | Empty Host | ⬜ | | | |
| 8.3 | Invalid Host Chars | ⬜ | | | |
| 9.1 | Mobile Routes | ⬜ | | | |
| 9.2 | Mobile Modals | ⬜ | | | |
| 10.1 | Console Logs | ⬜ | | | |
| 10.2 | Network Tab | ⬜ | | | |

Legend: ⬜ Not Started | 🟡 In Progress | ✅ Pass | ❌ Fail

---

## Environment-Specific Testing Notes

### Development Environment
- Configuration file: `configuration.Development.json`
- Expected routes: 2-3 test routes
- Node addresses: localhost with various ports

### Production Environment
- Configuration file: `configuration.json` or `configuration.Prod.json`
- Expected routes: All production routes
- Node addresses: Production hostnames

### Testing Both Environments
1. Test in Development first
2. If all tests pass, test in Production
3. Verify configuration file paths are correct
4. Check that environment-specific settings work

---

**Document Version**: 1.0
**Last Updated**: 2025-11-15
**Created By**: Copilot (AI Assistant)
