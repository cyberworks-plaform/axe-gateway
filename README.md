![Build Status](https://github.com/cyberworks-plaform/axe-gateway/actions/workflows/build.yml/badge.svg)

[Check and download package](https://github.com/cyberworks-plaform/axe-gateway/actions/workflows/build.yml)
# axe-gateway

API Gateway với tính năng quản lý cập nhật tự động

## Tính năng chính

- ✅ API Gateway sử dụng Ocelot
- ✅ Quản lý người dùng và xác thực JWT
- ✅ Dashboard giám sát và báo cáo
- ✅ Health checks cho downstream services
- ✅ Request logging và analytics
- ✅ **System Update Management** - Quản lý cập nhật hệ thống
  - Kiểm tra phiên bản mới từ GitHub Releases
  - Tải lên gói cập nhật thủ công (cho môi trường offline)
  - Backup tự động trước khi cập nhật
  - Rollback khi cần thiết
  - Lịch sử cập nhật và audit trail

## Cập nhật hệ thống

Xem hướng dẫn chi tiết: [docs/UPDATE_FEATURE.md](docs/UPDATE_FEATURE.md)

### Quy trình cập nhật nhanh

**Online (có internet):**
1. Đăng nhập với quyền Admin
2. Vào menu "System Update"
3. Click "Check for Updates"
4. Nếu có bản mới, click "Apply"
5. Hệ thống tự động backup và cập nhật

**Offline (không có internet):**
1. Build gói cập nhật: `cd Ce.Gateway.Api && .\build.ps1`
2. Đăng nhập vào hệ thống với quyền Admin
3. Vào menu "System Update"
4. Upload file ZIP trong thư mục `publish/`
5. Click "Apply" để cài đặt

### Cấu hình

Thêm vào `appsettings.json`:

```json
{
  "Update": {
    "GitHubOwner": "cyberworks-plaform",
    "GitHubRepo": "axe-gateway",
    "UpdatesDirectory": "updates",
    "BackupsDirectory": "backups",
    "MaxBackupsToKeep": 5
  }
}
```

## Build và Deploy

### Build thủ công
```powershell
cd Ce.Gateway.Api
.\build.ps1
```

Gói cập nhật sẽ được tạo trong thư mục `publish/` với tên:
```
Ce.Gateway.Api-v{version}-{git-hash}-update-{timestamp}.zip
```

### Deploy truyền thống
1. Copy file ZIP lên server
2. Stop IIS app pool
3. Backup thư mục hiện tại
4. Giải nén vào thư mục ứng dụng
5. Start app pool

### Deploy với tính năng Update
1. Upload file ZIP qua web interface (`/update`)
2. Click "Apply Update"
3. Hệ thống tự động xử lý backup và restart
