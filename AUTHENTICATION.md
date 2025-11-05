# Authentication and User Management System

## Overview

This document describes the authentication and user management system implemented in the Ce.Gateway.Api project.

## Features

### 1. User Authentication
- JWT-based authentication
- Secure password hashing using HMACSHA512
- Token expiration: 7 days
- Login endpoint: `/api/auth/login`

### 2. User Management
- Full CRUD operations for users
- Role-based access control
- User endpoints: `/api/users`

### 3. Role-Based Authorization

The system supports three roles with different permission levels:

#### Administrator
- Full system access
- Can manage users (create, update, delete)
- Can view all reports and configurations
- Can access all endpoints

#### Management
- Can view reports and dashboards
- Can view system configurations
- Can view user list
- Cannot create, update, or delete users

#### Monitor
- Can only view dashboards and reports
- Cannot access user management
- Cannot modify system configurations

## API Endpoints

### Authentication

#### POST /api/auth/login
Login to the system and receive a JWT token.

**Request:**
```json
{
  "username": "admin",
  "password": "admin123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "admin",
  "fullName": "System Administrator",
  "role": "Administrator",
  "email": "admin@gateway.local"
}
```

#### GET /api/auth/me
Get current user information (requires authentication).

**Headers:**
```
Authorization: Bearer {token}
```

### User Management

#### GET /api/users
Get list of users with pagination (requires Administrator or Management role).

**Query Parameters:**
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 10)

**Headers:**
```
Authorization: Bearer {token}
```

#### GET /api/users/{id}
Get a specific user by ID (requires Administrator or Management role).

#### POST /api/users
Create a new user (requires Administrator role).

**Request:**
```json
{
  "username": "newuser",
  "password": "password123",
  "fullName": "New User",
  "email": "newuser@example.com",
  "role": "Monitor"
}
```

#### PUT /api/users/{id}
Update an existing user (requires Administrator role).

**Request:**
```json
{
  "fullName": "Updated Name",
  "email": "updated@example.com",
  "role": "Management",
  "isActive": true,
  "password": "newpassword123"
}
```

Note: Password field is optional. If not provided, password won't be changed.

#### DELETE /api/users/{id}
Delete a user (requires Administrator role).

#### POST /api/users/{id}/change-password
Change user password (requires Administrator role).

**Request:**
```json
{
  "newPassword": "newpassword123"
}
```

## Web Interface

### Login Page
Access: `/login.html`

A simple login interface to authenticate users. After successful login, the JWT token is stored in localStorage.

### User Management Page
Access: `/users.html`

A web interface for managing users (create, edit, delete). Only accessible to users with Administrator or Management roles.

Features:
- View all users in a table
- Create new users
- Edit existing users
- Delete users
- Visual role and status badges

## Default Credentials

The system creates a default administrator account on first run:

- **Username:** admin
- **Password:** admin123

**⚠️ Important:** Change the default password immediately after first login in production environments.

## Configuration

JWT token configuration is stored in `appsettings.json`:

```json
{
  "Tokens": {
    "Key": "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF",
    "Issuer": "http://ce.com.vn",
    "Audience": "http://ce-axe.com.vn"
  }
}
```

**⚠️ Security Note:** Change the JWT key in production to a strong, unique secret.

## Database

User data is stored in SQLite database (`gateway.db` or `gateway.development.db`).

### User Table Schema

| Column | Type | Description |
|--------|------|-------------|
| Id | INTEGER | Primary key |
| Username | TEXT | Unique username |
| PasswordHash | TEXT | Hashed password |
| FullName | TEXT | User's full name |
| Email | TEXT | User's email |
| Role | TEXT | User role (Administrator, Management, Monitor) |
| IsActive | BOOLEAN | Account status |
| CreatedAt | DATETIME | Account creation timestamp |
| LastLoginAt | DATETIME | Last login timestamp |
| UpdatedAt | DATETIME | Last update timestamp |

## Security Features

1. **Password Hashing:** Passwords are hashed using HMACSHA512 with salt
2. **JWT Tokens:** Secure token-based authentication
3. **Role-Based Access Control:** Endpoints protected by role requirements
4. **Unique Usernames:** Database constraint ensures username uniqueness
5. **Password Validation:** Minimum 6 characters required

## Testing

### Using cURL

**Login:**
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

**Get Users:**
```bash
TOKEN="your-jwt-token-here"
curl -X GET http://localhost:5000/api/users \
  -H "Authorization: Bearer $TOKEN"
```

**Create User:**
```bash
TOKEN="your-jwt-token-here"
curl -X POST http://localhost:5000/api/users \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "username":"monitor1",
    "password":"monitor123",
    "fullName":"Monitor User",
    "email":"monitor@gateway.local",
    "role":"Monitor"
  }'
```

## Migration

The user management system uses Entity Framework Core migrations. When the application starts:

1. The database is automatically migrated to the latest version
2. If no users exist, the default admin user is created

## Troubleshooting

### JWT Token Error: Key Size Too Small
If you see an error about key size being less than 256 bits, ensure the `Tokens:Key` in `appsettings.json` is at least 32 characters long.

### 403 Forbidden
This means the authenticated user doesn't have the required role to access the endpoint. Check the role requirements for each endpoint.

### 401 Unauthorized
This means:
- No token was provided
- Token is invalid or expired
- Token signature doesn't match

## Future Enhancements

Potential improvements for the authentication system:

1. Password reset functionality
2. Two-factor authentication (2FA)
3. Account lockout after failed login attempts
4. Audit logging for user actions
5. OAuth2/OpenID Connect integration
6. Refresh tokens
7. Password complexity requirements
8. Session management
