# Route Configuration Management Feature

## Overview

The Route Configuration Management feature provides a web-based interface for managing Ocelot API Gateway routes, including adding, editing, and deleting downstream nodes and modifying route parameters.

## Features

### 1. Route Viewing
- View all configured routes with their details
- Filter routes by upstream path or downstream scheme
- Display route information including:
  - Upstream and downstream paths
  - HTTP methods
  - Load balancer settings
  - QoS options
  - Downstream nodes (host:port pairs)

### 2. Node Management
- **Add Node**: Add a downstream node to one or multiple routes
- **Edit Node**: Modify an existing node's host and port
- **Delete Node**: Remove a node from a route

### 3. Route Configuration
- Modify downstream scheme (HTTP/HTTPS)
- Change downstream path template
- Configure load balancer type:
  - Least Connection
  - Round Robin
  - No Load Balancer
- Set Quality of Service (QoS) options:
  - Timeout value (milliseconds)
  - Exceptions allowed before breaking
  - Circuit break duration

### 4. Configuration History & Rollback
- View complete history of configuration changes
- Track who made each change and when
- Rollback to any previous configuration
- Automatic backup before each change

## User Interface

### Main Routes Page (`/routes`)
- **Search**: Filter routes by upstream path
- **Scheme Filter**: Filter by HTTP or HTTPS
- **Route Cards**: Display each route with its configuration
- **Add Node**: Button to add nodes to multiple routes
- **Edit/Delete**: Per-node action buttons

### History Page (`/routes/history`)
- **Timeline**: Chronological list of configuration changes
- **Details**: Timestamp, user, and description of each change
- **Rollback**: Restore any previous configuration

## API Endpoints

### Routes
- `GET /api/routes` - Get all routes
- `GET /api/routes/{routeId}` - Get specific route
- `PUT /api/routes/{routeId}` - Update route parameters

### Node Management
- `POST /api/routes/nodes` - Add node to routes
- `PUT /api/routes/nodes` - Update node
- `DELETE /api/routes/nodes` - Delete node from routes

### Configuration Management
- `GET /api/routes/history` - Get configuration history
- `POST /api/routes/rollback/{historyId}` - Rollback to previous configuration
- `POST /api/routes/reload` - Trigger configuration reload

## Security

- **Authorization**: Only users with Administrator role can access route configuration management
- **Audit Trail**: All changes are logged with user and timestamp
- **Backup**: Automatic backup created before every configuration change
- **Thread Safety**: Configuration file access is thread-safe using semaphores

## Safety Features

### 1. Automatic Backup
Every configuration change creates a timestamped backup file in `/data/config-backups/` directory.

### 2. Configuration Validation
- Node validation (host and port ranges)
- Route ID validation
- Duplicate node detection

### 3. Rollback Capability
If a configuration change causes issues:
1. Navigate to `/routes/history`
2. Find the last known good configuration
3. Click "Rollback" button
4. System automatically restores the configuration

### 4. Thread-Safe Operations
Multiple administrators can work simultaneously without corrupting the configuration file.

### 5. Hot Reload
Configuration changes are automatically detected by Ocelot without requiring application restart.

## Testing Guide

### Manual Testing

#### Test 1: View Routes
1. Login as Administrator
2. Navigate to "Route Configuration" from sidebar
3. Verify all routes are displayed
4. Test search and filter functionality

#### Test 2: Add Node
1. Click "Add Node" button
2. Select one or more routes
3. Enter host (e.g., `localhost`) and port (e.g., `8081`)
4. Click "Add Node"
5. Verify node appears in the route card
6. Check configuration file has been updated

#### Test 3: Edit Node
1. Click edit icon on a node
2. Modify host or port
3. Click "Update Node"
4. Verify changes are reflected

#### Test 4: Delete Node
1. Click delete icon on a node
2. Confirm deletion
3. Verify node is removed

#### Test 5: Update Route Configuration
1. Click "Configure" on a route card
2. Modify load balancer or QoS settings
3. Click "Update Route"
4. Verify changes are saved

#### Test 6: Configuration History
1. Navigate to "View History"
2. Verify all changes are logged
3. Check timestamp and user information

#### Test 7: Rollback Configuration
1. Make a configuration change
2. Navigate to "View History"
3. Click "Rollback" on a previous configuration
4. Confirm rollback
5. Verify configuration is restored
6. Check that a backup was created before rollback

### Integration Testing

The feature integrates with:
- **Ocelot Gateway**: Configuration file changes are automatically detected
- **Database**: Configuration history is stored in SQLite
- **File System**: Backup files are stored in `/data/config-backups/`

### Performance Considerations

- Configuration file access is optimized with semaphores
- Large route lists are handled efficiently with client-side filtering
- History queries are limited to 50 most recent entries by default

## Mobile Support

The UI is fully responsive and works on mobile devices:
- Touch-friendly buttons and controls
- Responsive layout adjusts to screen size
- Modal dialogs work on mobile browsers
- No horizontal scrolling required

## Troubleshooting

### Configuration Not Updating
1. Check file permissions on `configuration.json`
2. Verify no syntax errors in JSON
3. Check application logs for errors
4. Ensure Ocelot file watcher is enabled

### Rollback Fails
1. Verify backup file exists in `/data/config-backups/`
2. Check database for history record
3. Ensure user has write permissions
4. Check application logs

### UI Not Loading Routes
1. Check API endpoint: `GET /api/routes`
2. Verify user has Administrator role
3. Check browser console for JavaScript errors
4. Ensure configuration.json is valid JSON

## Future Enhancements

Potential improvements for future versions:
1. Route template validation
2. Bulk route operations
3. Configuration diff viewer
4. Export/import configuration
5. Route testing functionality
6. Advanced filtering options
7. Route groups/categories
8. Configuration change notifications

## Technical Architecture

### Backend
- **Service Layer**: `RouteConfigService` handles all configuration operations
- **Database**: SQLite for storing configuration history
- **File System**: JSON configuration files with automatic backups

### Frontend
- **Framework**: AdminLTE 3 with Bootstrap 4
- **JavaScript**: jQuery for AJAX and DOM manipulation
- **Styling**: Responsive CSS with mobile-first approach

### Data Flow
1. User action in UI → AJAX request to API
2. API Controller → RouteConfigService
3. Service creates backup → Modifies configuration
4. Service saves history → Returns response
5. UI updates → Shows success/error message
