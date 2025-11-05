# Quick Start Guide - Request Report Testing

## Mục tiêu
Hướng dẫn nhanh để test tính năng Request Report với dữ liệu lịch sử.

## Bước 1: Chuẩn bị môi trường

### Xóa dữ liệu cũ (optional)
```powershell
Remove-Item Ce.Gateway.Api\data\gateway.db -ErrorAction SilentlyContinue
```

### Khởi động Gateway
```powershell
.\run_gateway_manual.ps1
```

Chờ Gateway khởi động xong, sau đó có thể dừng lại (Ctrl+C).

## Bước 2: Tạo dữ liệu test

### Test biểu đồ theo GIỜ (Last 1 Day)

```powershell
.\run_simulator.ps1
```

Chọn:
- Mode: `2` (Historical)
- Time range: `1` (Last 24 hours)
- Requests/hour: ENTER (default 50) hoặc nhập số tùy chỉnh

**Kết quả**: ~1,200 requests phân bố đều 24 giờ

### Test biểu đồ theo NGÀY (Last 7 Days)

```powershell
.\run_simulator.ps1
```

Chọn:
- Mode: `2`
- Time range: `2` (Last 7 days)
- Requests/day: ENTER (default 200)

**Kết quả**: ~1,400 requests phân bố 7 ngày

### Test biểu đồ theo NGÀY (Last 30 Days)

```powershell
.\run_simulator.ps1
```

Chọn:
- Mode: `2`
- Time range: `3` (Last 30 days)
- Requests/day: ENTER (default 150)

**Kết quả**: ~4,500 requests phân bố 30 ngày

### Test biểu đồ theo THÁNG (Last 12 Months)

```powershell
.\run_simulator.ps1
```

Chọn:
- Mode: `2`
- Time range: `5` (Last 12 months)
- Requests/month: `1000` (hoặc số tùy chọn)

**Kết quả**: ~12,000 requests phân bố 12 tháng

## Bước 3: Xem kết quả

### Khởi động Gateway
```powershell
.\run_gateway_manual.ps1
```

### Truy cập Request Report
Mở browser: `http://localhost:5000/requestreport`

### Test các khung thời gian

Chọn từ dropdown và quan sát biểu đồ:
- **Last 1 Day**: Biểu đồ 24 cột theo giờ (00:00 - 23:00)
- **Last 7 Days**: Biểu đồ 7 cột theo ngày
- **Last 1 Month**: Biểu đồ 30-31 cột theo ngày
- **Last 3 Months**: Biểu đồ 90 cột theo ngày
- **Last 12 Months**: Biểu đồ 365 cột theo ngày (có thể scroll)

## Bước 4: Tạo dữ liệu đầy đủ cho demo

Để có dữ liệu cho TẤT CẢ các khung thời gian, chạy simulator nhiều lần:

```powershell
# 1. Tạo dữ liệu 12 tháng
.\run_simulator.ps1
# Chọn: Mode 2 -> Option 5 -> 1000

# 2. Tạo thêm dữ liệu 24 giờ gần đây
.\run_simulator.ps1
# Chọn: Mode 2 -> Option 1 -> 100
```

**Kết quả**: Có đủ dữ liệu để test tất cả các khung thời gian từ giờ → tháng.

## Tips

### Tạo dữ liệu nhanh
- Số lượng mặc định đã tối ưu cho test
- Để demo ấn tượng, tăng số lượng: 500-1000 requests/slot

### Xóa và tạo lại
```powershell
# Xóa database
Remove-Item Ce.Gateway.Api\data\gateway.db

# Chạy Gateway để tạo database mới
.\run_gateway_manual.ps1
# (Ctrl+C sau khi khởi động xong)

# Tạo dữ liệu mới
.\run_simulator.ps1
```

### Kiểm tra số lượng records
```powershell
# Sử dụng SQLite viewer hoặc
dotnet run --project Ce.Gateway.Api
# Truy cập /requestlog để xem tổng số records
```

## Troubleshooting

### Lỗi "Database not found"
→ Chạy Gateway ít nhất 1 lần để tạo database

### Biểu đồ trống
→ Đảm bảo đã tạo dữ liệu cho đúng khung thời gian

### Biểu đồ không cân đối
→ Tạo lại dữ liệu với variance để có phân bố realistic

### Quá nhiều data
→ Xóa database và tạo lại với số lượng nhỏ hơn

## Demo Workflow

```powershell
# 1. Clean start
Remove-Item Ce.Gateway.Api\data\gateway.db -ErrorAction SilentlyContinue

# 2. Init database
.\run_gateway_manual.ps1
# (Ctrl+C sau khi start)

# 3. Generate 12 months data
.\run_simulator.ps1
# Mode 2 -> 5 -> 800

# 4. Generate recent hourly data
.\run_simulator.ps1
# Mode 2 -> 1 -> 80

# 5. Start Gateway
.\run_gateway_manual.ps1

# 6. View results
# Browser: http://localhost:5000/requestreport
```

## Expected Results

### Last 1 Day
```
4 summary cards showing totals
Bar chart: 24 columns (hourly)
Each column: Stacked (2xx green, 4xx yellow, 5xx red)
```

### Last 7 Days
```
Bar chart: 7 columns (daily)
Variance visible across days
```

### Last 12 Months
```
Bar chart: 12 columns (monthly) 
Clear trend over year
```

## Performance

- Historical generation: ~5,000 records/second
- 24 hours data: < 1 second
- 12 months data: < 3 seconds
- Real-time mode: Depends on interval setting
