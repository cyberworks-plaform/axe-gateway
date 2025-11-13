# Kế hoạch Hoàn thiện Tính năng Đăng nhập và Quản lý User

## Ngày lập: 2025-11-13
## Version: 1.0

---

## 1. PHÂN TÍCH HIỆN TRẠNG

### 1.1. Đã có (Existing Features)
✅ **Database & Entity Framework**
- GatewayDbContext kế thừa IdentityDbContext<ApplicationUser>
- ApplicationUser entity với các trường: FullName, IsActive, CreatedAt, LastLoginAt, UpdatedAt
- Entity Framework Migrations đã được cấu hình
- SQLite database (gateway.db/gateway.development.db)
- Database seeding với admin user mặc định

✅ **Authentication & Authorization**
- ASP.NET Core Identity đã được cấu hình
- JWT Bearer authentication đã được thiết lập
- Cookie-based authentication cho MVC pages
- 3 roles: Administrator, Management, Monitor
- Role-based authorization attributes

✅ **MVC Controllers & Views**
- AccountController: Login, Logout, AccessDenied
- UserController: Index, Create, Edit, Delete (MVC pages)
- Views đã có: Login, User Index, Create, Edit
- Sử dụng AdminLTE template

✅ **Models & DTOs**
- LoginRequest, CreateUserRequest, UpdateUserRequest
- UserDto, Roles constants
- Basic validation attributes

### 1.2. Còn thiếu (Missing Features)

❌ **API Controllers**
- Chưa có AuthController (API endpoint cho login/logout)
- Chưa có UsersApiController (RESTful API cho CRUD operations)

❌ **Services Layer**
- Chưa có IAuthService/AuthService
- Chưa có IUserService/UserService
- Business logic đang nằm trực tiếp trong Controllers

❌ **Advanced Security Features**
- Chưa có validation logic nghiệp vụ (không xóa admin gốc, không xóa hết user)
- Chưa có kiểm tra quyền chi tiết (user không thể tự xóa chính mình)
- Chưa có audit log cho các hành động user

❌ **Unit Tests**
- Chưa có tests cho Authentication
- Chưa có tests cho User Management
- Chưa có tests cho Authorization rules

❌ **Documentation**
- Chưa có API documentation đầy đủ trong code
- Swagger/OpenAPI chưa được cấu hình

---

## 2. KẾ HOẠCH THỰC HIỆN

### Phase 1: Tái cấu trúc & Service Layer (2-3 giờ)

#### 2.1. Tạo Service Interfaces và Implementations
**Files cần tạo:**
1. `Services/Interface/IAuthService.cs`
   - Task<LoginResponse> LoginAsync(LoginRequest request)
   - Task<bool> ValidateTokenAsync(string token)
   - Task<ApplicationUser> GetCurrentUserAsync()

2. `Services/Interface/IUserService.cs`
   - Task<PaginatedResult<UserDto>> GetUsersAsync(int page, int pageSize)
   - Task<UserDto> GetUserByIdAsync(string id)
   - Task<UserDto> CreateUserAsync(CreateUserRequest request, string createdBy)
   - Task<UserDto> UpdateUserAsync(string id, UpdateUserRequest request, string updatedBy)
   - Task<bool> DeleteUserAsync(string id, string deletedBy)
   - Task<bool> CanDeleteUserAsync(string id, string currentUserId)
   - Task<int> GetActiveUserCountAsync()
   - Task<bool> IsRootAdminAsync(string userId)

3. `Services/AuthService.cs`
4. `Services/UserService.cs`

**Business Rules trong UserService:**
- Không cho phép xóa root admin (username = "admin")
- Không cho phép xóa nếu chỉ còn 1 active admin
- Không cho phép user tự xóa chính mình
- Validate role phải hợp lệ
- Username phải unique
- Email validation
- Password complexity check

#### 2.2. Tạo Additional Models
**Files cần tạo:**
1. `Models/Auth/LoginResponse.cs` (nếu chưa có)
2. `Models/Auth/ChangePasswordRequest.cs` (đã có)
3. `Models/Common/ApiResponse.cs` - Generic API response wrapper
4. `Models/Common/ErrorResponse.cs` - Standardized error response

### Phase 2: API Controllers (1-2 giờ)

#### 2.1. Tạo Auth API Controller
**File:** `Controllers/Api/AuthController.cs`

**Endpoints:**
- POST /api/auth/login - Login and get JWT token
- POST /api/auth/refresh - Refresh token (future)
- GET /api/auth/me - Get current user info
- POST /api/auth/logout - Logout (invalidate token)

#### 2.2. Tạo Users API Controller
**File:** `Controllers/Api/UsersController.cs`

**Endpoints:**
- GET /api/users - Get all users (paginated)
- GET /api/users/{id} - Get user by ID
- POST /api/users - Create new user [Administrator only]
- PUT /api/users/{id} - Update user [Administrator only]
- DELETE /api/users/{id} - Delete user [Administrator only]
- POST /api/users/{id}/change-password - Change password [Administrator only]
- GET /api/users/{id}/can-delete - Check if user can be deleted

**Authorization:**
- GET endpoints: Administrator, Management
- POST/PUT/DELETE: Administrator only

### Phase 3: Enhanced Validation & Business Logic (1 giờ)

#### 3.1. Custom Validation Attributes
**Files cần tạo:**
1. `Validation/ValidRoleAttribute.cs`
2. `Validation/UniqueUsernameAttribute.cs` (async validation)

#### 3.2. Extension Methods
**File:** `Extensions/UserManagerExtensions.cs`
- IsRootAdmin(ApplicationUser user)
- GetActiveAdminCountAsync()

### Phase 4: Unit Tests (3-4 giờ)

#### 4.1. Test Setup
**File:** `Ce.Gateway.Api.Tests/TestHelpers/IdentityTestHelper.cs`
- Mock UserManager, RoleManager, SignInManager
- In-memory database setup
- Test data seeding

#### 4.2. Auth Service Tests
**File:** `Ce.Gateway.Api.Tests/Services/AuthServiceTests.cs`

**Test cases:**
- LoginAsync_WithValidCredentials_ReturnsToken
- LoginAsync_WithInvalidCredentials_ThrowsException
- LoginAsync_WithInactiveUser_ThrowsException
- LoginAsync_UpdatesLastLoginTime
- GetCurrentUserAsync_WithValidToken_ReturnsUser
- GetCurrentUserAsync_WithInvalidToken_ReturnsNull

#### 4.3. User Service Tests
**File:** `Ce.Gateway.Api.Tests/Services/UserServiceTests.cs`

**Test cases:**
- CreateUserAsync_WithValidData_CreatesUser
- CreateUserAsync_WithDuplicateUsername_ThrowsException
- CreateUserAsync_WithInvalidRole_ThrowsException
- UpdateUserAsync_WithValidData_UpdatesUser
- UpdateUserAsync_WithInvalidId_ThrowsException
- DeleteUserAsync_WithRootAdmin_ThrowsException
- DeleteUserAsync_WithLastAdmin_ThrowsException
- DeleteUserAsync_WhenUserDeletingSelf_ThrowsException
- DeleteUserAsync_WithValidUser_DeletesUser
- CanDeleteUserAsync_WithRootAdmin_ReturnsFalse
- CanDeleteUserAsync_WithLastAdmin_ReturnsFalse
- CanDeleteUserAsync_WithSelf_ReturnsFalse
- CanDeleteUserAsync_WithValidUser_ReturnsTrue
- GetUsersAsync_ReturnsPaginatedResults
- GetActiveUserCountAsync_ReturnsCorrectCount

#### 4.4. API Controller Tests
**File:** `Ce.Gateway.Api.Tests/Controllers/AuthControllerTests.cs`
**File:** `Ce.Gateway.Api.Tests/Controllers/UsersControllerTests.cs`

**Test cases cho AuthController:**
- Login_WithValidCredentials_Returns200AndToken
- Login_WithInvalidCredentials_Returns401
- GetMe_WithValidToken_Returns200AndUserInfo
- GetMe_WithoutToken_Returns401

**Test cases cho UsersController:**
- GetUsers_AsAdmin_Returns200WithUsers
- GetUsers_AsMonitor_Returns403
- CreateUser_AsAdmin_WithValidData_Returns201
- CreateUser_AsManagement_Returns403
- DeleteUser_RootAdmin_Returns400
- DeleteUser_LastAdmin_Returns400
- DeleteUser_Self_Returns400
- DeleteUser_ValidUser_Returns204

#### 4.5. Authorization Tests
**File:** `Ce.Gateway.Api.Tests/Authorization/RoleAuthorizationTests.cs`

**Test cases:**
- AdminCanAccessAllEndpoints
- ManagementCanViewButNotModify
- MonitorCanOnlyViewDashboard

### Phase 5: Integration & Refinement (1-2 giờ)

#### 5.1. Update Startup.cs
- Register IAuthService, AuthService
- Register IUserService, UserService
- Configure Swagger/OpenAPI (optional)

#### 5.2. Update Existing Controllers
- Refactor UserController để sử dụng IUserService
- Refactor AccountController để sử dụng IAuthService
- Add better error handling
- Add logging

#### 5.3. Enhanced Security
**File:** `Middleware/JwtValidationMiddleware.cs`
- Validate JWT token on each request
- Check token expiration
- Check user still active

#### 5.4. Audit Logging
**File:** `Services/Interface/IAuditLogService.cs`
**File:** `Services/AuditLogService.cs`
- Log user creation, update, deletion
- Log login/logout
- Log failed login attempts

### Phase 6: Testing & Documentation (1 giờ)

#### 6.1. Run All Tests
```bash
dotnet test --logger "console;verbosity=detailed"
dotnet-coverage collect -f cobertura -o coverage.cobertura.xml dotnet test
```

#### 6.2. Build & Run
```bash
dotnet build
dotnet run --project Ce.Gateway.Api
```

#### 6.3. Manual Testing
- Test login flow (MVC)
- Test API endpoints với Postman/curl
- Test authorization rules
- Test validation rules

#### 6.4. Update Documentation
- Update AUTHENTICATION.md
- Add API documentation comments
- Create TESTING.md with test coverage report

---

## 3. BUSINESS RULES CHECKLIST

### 3.1. User Creation
- ✅ Username must be unique
- ✅ Username: alphanumeric, 3-50 characters
- ✅ Password: minimum 6 characters
- ✅ Email: valid format (optional)
- ✅ Role: must be one of [Administrator, Management, Monitor]
- ✅ CreatedAt: auto-set to UTC now
- ✅ IsActive: default to true

### 3.2. User Update
- ✅ Cannot change username (it's the identity)
- ✅ Can update: FullName, Email, Role, IsActive, Password
- ✅ UpdatedAt: auto-set to UTC now
- ✅ Password is optional (only if changing)
- ✅ Cannot deactivate root admin
- ✅ Cannot deactivate last active admin

### 3.3. User Deletion
- ✅ Cannot delete root admin (username = "admin")
- ✅ Cannot delete if it's the last active administrator
- ✅ Cannot delete self
- ✅ Soft delete option (set IsActive = false) vs Hard delete
- ✅ Log deletion activity

### 3.4. Authentication
- ✅ Lock account after 5 failed attempts (already configured)
- ✅ Lockout duration: 5 minutes
- ✅ Cannot login if IsActive = false
- ✅ Update LastLoginAt on successful login
- ✅ JWT token expiration: 7 days (configurable)

### 3.5. Authorization Matrix

| Endpoint | Administrator | Management | Monitor |
|----------|--------------|------------|---------|
| GET /api/users | ✅ | ✅ | ❌ |
| GET /api/users/{id} | ✅ | ✅ | ❌ |
| POST /api/users | ✅ | ❌ | ❌ |
| PUT /api/users/{id} | ✅ | ❌ | ❌ |
| DELETE /api/users/{id} | ✅ | ❌ | ❌ |
| GET /dashboard | ✅ | ✅ | ✅ |
| GET /api/logs | ✅ | ✅ | ❌ |
| POST /api/config | ✅ | ❌ | ❌ |

---

## 4. FILE STRUCTURE

```
Ce.Gateway.Api/
├── Controllers/
│   ├── Api/
│   │   ├── AuthController.cs          [NEW]
│   │   ├── UsersController.cs         [NEW]
│   │   ├── DashboardController.cs     [EXISTS]
│   │   └── ...
│   └── Pages/
│       ├── AccountController.cs       [EXISTS - REFACTOR]
│       ├── UserController.cs          [EXISTS - REFACTOR]
│       └── ...
├── Services/
│   ├── Interface/
│   │   ├── IAuthService.cs            [NEW]
│   │   ├── IUserService.cs            [NEW]
│   │   └── IAuditLogService.cs        [NEW]
│   ├── AuthService.cs                 [NEW]
│   ├── UserService.cs                 [NEW]
│   └── AuditLogService.cs             [NEW]
├── Models/
│   ├── Auth/
│   │   ├── LoginRequest.cs            [EXISTS]
│   │   ├── LoginResponse.cs           [NEW]
│   │   ├── CreateUserRequest.cs       [EXISTS]
│   │   ├── UpdateUserRequest.cs       [EXISTS]
│   │   ├── ChangePasswordRequest.cs   [EXISTS]
│   │   ├── UserDto.cs                 [EXISTS]
│   │   └── Roles.cs                   [EXISTS]
│   └── Common/
│       ├── ApiResponse.cs             [NEW]
│       └── ErrorResponse.cs           [NEW]
├── Validation/
│   ├── ValidRoleAttribute.cs          [NEW]
│   └── UniqueUsernameAttribute.cs     [NEW]
├── Extensions/
│   └── UserManagerExtensions.cs       [NEW]
└── Middleware/
    └── JwtValidationMiddleware.cs     [NEW]

Ce.Gateway.Api.Tests/
├── Services/
│   ├── AuthServiceTests.cs            [NEW]
│   └── UserServiceTests.cs            [NEW]
├── Controllers/
│   ├── AuthControllerTests.cs         [NEW]
│   └── UsersControllerTests.cs        [NEW]
├── Authorization/
│   └── RoleAuthorizationTests.cs      [NEW]
└── TestHelpers/
    └── IdentityTestHelper.cs          [NEW]
```

---

## 5. TECHNOLOGY STACK

- **Framework**: ASP.NET Core 9.0
- **ORM**: Entity Framework Core 9.0
- **Database**: SQLite
- **Authentication**: ASP.NET Core Identity + JWT Bearer
- **Testing**: xUnit, Microsoft.EntityFrameworkCore.InMemory
- **UI**: Razor Pages + AdminLTE

---

## 6. TIMELINE ESTIMATE

| Phase | Estimated Time | Priority |
|-------|---------------|----------|
| Phase 1: Service Layer | 2-3h | High |
| Phase 2: API Controllers | 1-2h | High |
| Phase 3: Validation | 1h | Medium |
| Phase 4: Unit Tests | 3-4h | High |
| Phase 5: Integration | 1-2h | High |
| Phase 6: Testing & Docs | 1h | Medium |
| **TOTAL** | **9-13h** | - |

---

## 7. SUCCESS CRITERIA

✅ **Functional Requirements**
1. Users can login via MVC and API
2. Administrators can CRUD users
3. Management can view users but not modify
4. Monitors cannot access user management
5. Business rules are enforced (no delete root admin, etc.)
6. All API endpoints return proper status codes
7. Proper error messages in English

✅ **Technical Requirements**
1. Code coverage > 80%
2. All unit tests pass
3. Solution builds without errors
4. No breaking changes to existing features
5. Follows .NET best practices
6. Proper logging
7. Proper exception handling

✅ **Security Requirements**
1. JWT tokens are validated
2. Passwords are hashed
3. Role-based authorization works
4. No sensitive data in logs
5. CORS configured properly
6. SQL injection prevention (EF Core handles this)

---

## 8. RISKS & MITIGATION

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Breaking existing features | High | Medium | Comprehensive testing, backwards compatibility |
| Identity/JWT misconfiguration | High | Low | Use existing patterns, test thoroughly |
| Test coverage insufficient | Medium | Medium | Write tests incrementally, aim for 80%+ |
| Performance degradation | Medium | Low | Use async/await, proper indexing |
| Database migration issues | High | Low | Test migrations in dev environment first |

---

## 9. POST-IMPLEMENTATION

### 9.1. Future Enhancements
- [ ] Password reset via email
- [ ] Two-factor authentication (2FA)
- [ ] OAuth2/OpenID Connect integration
- [ ] Refresh tokens
- [ ] User profile page
- [ ] Activity audit log viewer
- [ ] User search and filtering
- [ ] Bulk user operations
- [ ] Export users to CSV
- [ ] Role management UI

### 9.2. Performance Optimization
- [ ] Add Redis caching for user data
- [ ] Implement repository pattern with caching
- [ ] Database query optimization
- [ ] Add database indexes

### 9.3. Monitoring
- [ ] Application Insights integration
- [ ] Failed login monitoring
- [ ] User activity dashboard
- [ ] Performance metrics

---

## 10. REFERENCES

- [ASP.NET Core Identity Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [JWT Bearer Authentication](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/jwt)
- [xUnit Testing](https://xunit.net/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)

---

## IMPLEMENTATION STATUS UPDATE

### Completed (✅):
1. **Service Layer** - DONE
   - ✅ IAuthService and AuthService created
   - ✅ IUserService and UserService created
   - ✅ All business rules implemented
   - ✅ UserManagerExtensions created
   - ✅ Proper error handling and validation

2. **API Controllers** - DONE
   - ✅ AuthController created with login and me endpoints
   - ✅ UsersController created with full CRUD + additional endpoints
   - ✅ Proper authorization attributes applied
   - ✅ ApiResponse wrapper for consistent responses

3. **Models** - DONE
   - ✅ LoginResponse, ApiResponse, ErrorResponse created
   - ✅ All DTOs already existed

4. **Dependency Injection** - DONE
   - ✅ Services registered in Startup.cs

5. **Unit Tests** - PARTIAL
   - ✅ Test infrastructure created (IdentityTestHelper, TestDbContextFactory)
   - ✅ AuthServiceTests created with 8 test cases
   - ✅ UserServiceTests created with 22 test cases
   - ⚠️ Some tests failing due to EF Core async query mocking limitations
   - ⚠️ 3 existing LogRepository tests were already failing (not related to our changes)

### Build Status:
- ✅ Solution builds successfully
- ⚠️ Test run: 67 total, 58 passed, 9 failed (6 from our new tests, 3 pre-existing failures)

### Test Failures to Fix:
1. Auth/User service tests: 6 failures due to Moq not supporting EF Core async queries properly
   - Need to refactor tests to use InMemory database instead of mocks
   - Or use more sophisticated mocking library like MockQueryable

2. LogRepository tests: 3 failures (pre-existing, not related to our implementation)
   - These can be fixed separately

### Next Steps:
1. Refactor failing auth/user tests to use InMemory database
2. Create API controller tests
3. Manual integration testing
4. Update documentation

---

**Prepared by**: C# Expert Agent  
**Date**: 2025-11-13  
**Status**: IMPLEMENTATION ~85% COMPLETE - Tests need refinement  
