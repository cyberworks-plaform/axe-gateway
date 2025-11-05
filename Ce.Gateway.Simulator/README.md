# Ce.Gateway Request Simulator

Bộ giả lập incoming request để phục vụ việc review và kiểm thử các tính năng dashboard của Gateway.

## Chức năng

- **Real-time mode**: Tạo dữ liệu liên tục (continuous generation)
- **Historical mode**: Tạo dữ liệu lịch sử cho các khung thời gian khác nhau
- Hỗ trợ tùy chỉnh số lượng request và phân bố thời gian

## Dữ liệu được giả lập

Simulator sử dụng thư viện **Bogus** để tạo dữ liệu fake bao gồm:

- **HTTP Methods**: GET, POST, PUT, DELETE, PATCH
- **Upstream paths**: /api/ocr/process, /api/ocr/batch, /api/documents/upload, v.v.
- **Downstream hosts**: localhost, ocr-node-1, ocr-node-2, ocr-node-3
- **Status codes**: 
  - 70% Success (200, 201, 204)
  - 15% Client Error (400, 401, 403, 404)
  - 10% Server Error (500, 502, 503)
  - 5% Other
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

### 3. Chọn Mode

#### Mode 1: Real-time (Continuous Generation)

Tạo dữ liệu liên tục theo thời gian thực.

- **ENTER**: Chạy với cấu hình mặc định (1 request mỗi 2 giây)
- **Custom**: `<số_request> <interval_ms>`
  - Ví dụ: `5 1000` = 5 requests mỗi 1 giây
  - Ví dụ: `10 500` = 10 requests mỗi 0.5 giây

**Use case**: Testing real-time monitoring, dashboard auto-refresh

#### Mode 2: Historical Data Generation

Tạo dữ liệu lịch sử cho các khung thời gian:

**Các tùy chọn:**

1. **Last 24 hours** (Phân bố theo giờ)
   - Default: 50 requests/giờ
   - Use case: Test biểu đồ theo giờ trong ngày

2. **Last 7 days** (Phân bố theo ngày)
   - Default: 200 requests/ngày
   - Use case: Test biểu đồ theo ngày trong tuần

3. **Last 30 days** (Phân bố theo ngày)
   - Default: 150 requests/ngày
   - Use case: Test biểu đồ theo ngày trong tháng

4. **Last 90 days** (Phân bố theo ngày)
   - Default: 100 requests/ngày
   - Use case: Test biểu đồ theo ngày trong quý

5. **Last 12 months** (Phân bố theo tháng)
   - Default: 500 requests/tháng
   - Use case: Test biểu đồ theo tháng trong năm

**Variance**: Số lượng request có độ biến động ±30% để tạo dữ liệu realistic hơn.

## Ví dụ sử dụng

### Real-time Mode

```
=== Ce.Gateway Request Simulator ===

[OK] Database: D:\...\gateway.db

Select Mode:
  1 - Real-time mode (continuous generation)
  2 - Historical data mode (generate past data)

Your choice [1/2]: 1

Configuration:
   - Press ENTER for default (1 request every 2 seconds)
   - Or enter custom: <count> <interval_ms>
   - Example: 5 1000 (5 requests every 1 second)

Your choice: 5 1000

[START] Real-time mode:
   - 5 request(s) per batch
   - Every 1000ms
   - Press Ctrl+C to stop

Generated: 150 | Rate: 4.98 req/s | Elapsed: 00:00:30
```

### Historical Mode

```
=== Ce.Gateway Request Simulator ===

[OK] Database: D:\...\gateway.db

Select Mode:
  1 - Real-time mode (continuous generation)
  2 - Historical data mode (generate past data)

Your choice [1/2]: 2

Historical Data Generator
-------------------------
Select time range:
  1 - Last 24 hours (hourly distribution)
  2 - Last 7 days (daily distribution)
  3 - Last 30 days (daily distribution)
  4 - Last 90 days (daily distribution)
  5 - Last 12 months (monthly distribution)

Your choice [1-5]: 5

Requests per month (default: 500): 1000

[INFO] Generating historical data...
   - From: 2024-10-29 12:30
   - To: 2025-10-29 12:30
   - Distribution: 1000 requests per month

[INFO] Total time slots: 12

Progress: 100% | Generated: 12543 requests

[DONE] Historical data generated successfully!
   - Total requests: 12543
   - Time range: 2024-10-29 12:30 to 2025-10-29 12:30
```

## Kiến trúc

```
Ce.Gateway.Simulator/
├── RequestLogEntry.cs        # Entity model (copy từ Gateway.Api)
├── GatewayDbContext.cs        # EF Core DbContext
├── RequestLogGenerator.cs     # Logic tạo fake data (sử dụng Bogus)
└── Program.cs                 # Console app với 2 modes
```

## Dependencies

- **.NET 9.0**
- **Microsoft.EntityFrameworkCore.Sqlite** - Kết nối SQLite database
- **Bogus 35.6.1** - Thư viện tạo fake data

## Tips

### Tạo dữ liệu cho demo

1. **Xóa dữ liệu cũ**: Xóa file `Ce.Gateway.Api\data\gateway.db`
2. **Chạy Gateway**: `.\run_gateway_manual.ps1` (tạo database mới)
3. **Tạo dữ liệu 12 tháng**: Chọn mode 2 → option 5 → nhập số lượng mong muốn
4. **Xem kết quả**: Truy cập `/requestreport` và chọn khung thời gian

### Best Practices

- **Test hourly chart**: Dùng mode 2 → option 1 (24 hours)
- **Test daily chart**: Dùng mode 2 → option 2 hoặc 3 (7 hoặc 30 days)
- **Test monthly chart**: Dùng mode 2 → option 5 (12 months)
- **Monitor real-time**: Dùng mode 1 với interval ngắn (100-500ms)

## Lưu ý

- Simulator **không ảnh hưởng** đến code base của Gateway
- Dữ liệu được ghi trực tiếp vào database, không qua HTTP API
- Historical mode tạo dữ liệu rất nhanh (có thể tạo hàng nghìn records trong vài giây)
- Variance ±30% tạo biểu đồ realistic hơn (không phẳng)
- Dữ liệu fake có thể xóa bằng cách xóa file database và chạy lại Gateway

