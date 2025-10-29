# Ce.Gateway Request Simulator

Bộ giả lập incoming request để phục vụ việc review và kiểm thử các tính năng dashboard của Gateway.

## Chức năng

- Tạo dữ liệu giả `RequestLogEntry` với thông tin realistic
- Gửi liên tục vào database của Gateway
- Hỗ trợ tùy chỉnh số lượng request và tần suất tạo

## Dữ liệu được giả lập

Simulator sử dụng thư viện **Bogus** để tạo dữ liệu fake bao gồm:

- **HTTP Methods**: GET, POST, PUT, DELETE, PATCH
- **Upstream paths**: /api/ocr/process, /api/ocr/batch, /api/documents/upload, v.v.
- **Downstream hosts**: localhost, ocr-node-1, ocr-node-2, ocr-node-3
- **Status codes**: 200, 201, 400, 404, 500, 503
- **Client IPs**: Các IP ngẫu nhiên từ pool định sẵn
- **Latency**: 10ms - 5000ms
- **Request/Response sizes**: Ngẫu nhiên realistic

## Cách sử dụng

### 1. Chạy Gateway trước

```powershell
.\run_gateway_manual.ps1
```

Gateway cần được chạy ít nhất 1 lần để tạo database `Ce.Gateway.Api\data\gateway.db`.

### 2. Chạy Simulator

```powershell
.\run_simulator.ps1
```

### 3. Cấu hình tùy chỉnh

Khi chạy simulator, bạn có thể:

- **ENTER**: Chạy với cấu hình mặc định (1 request mỗi 2 giây)
- **Nhập custom**: `<số_request> <interval_ms>`
  - Ví dụ: `5 1000` = 5 requests mỗi 1 giây
  - Ví dụ: `10 500` = 10 requests mỗi 0.5 giây
  - Ví dụ: `1 5000` = 1 request mỗi 5 giây

### 4. Dừng Simulator

Nhấn `Ctrl+C` để dừng.

## Ví dụ output

```
=== Ce.Gateway Request Simulator ===

📦 Database: D:\project\cyberworks-github\axe-gateway\Ce.Gateway.Api\data\gateway.db

⚙️  Configuration:
   - Press ENTER for default (1 request every 2 seconds)
   - Or enter custom: <count> <interval_ms>
   - Example: 5 1000 (5 requests every 1 second)

👉 Your choice: 5 1000

✅ Starting simulator:
   - 5 request(s) per batch
   - Every 1000ms
   - Press Ctrl+C to stop

✨ Generated: 150 | Rate: 4.98 req/s | Elapsed: 00:00:30
```

## Kiến trúc

```
Ce.Gateway.Simulator/
├── RequestLogEntry.cs        # Entity model (copy từ Gateway.Api)
├── GatewayDbContext.cs        # EF Core DbContext
├── RequestLogGenerator.cs     # Logic tạo fake data (sử dụng Bogus)
└── Program.cs                 # Console app entry point
```

## Dependencies

- **.NET 9.0**
- **Microsoft.EntityFrameworkCore.Sqlite** - Kết nối SQLite database
- **Bogus 35.6.1** - Thư viện tạo fake data

## Lưu ý

- Simulator **không ảnh hưởng** đến code base của Gateway
- Dữ liệu được ghi trực tiếp vào database, không qua HTTP API
- Có thể chạy đồng thời với Gateway để xem real-time data trên dashboard
- Dữ liệu fake có thể xóa bằng cách xóa file database và chạy lại Gateway
