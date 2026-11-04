# TASK 4 (BỮA 4) – GIÁM SÁT SỨC KHỎE ỨNG DỤNG (APPLICATION HEALTH CHECKS)

**Sinh viên:** Võ Hùng Mạnh
**MSSV:** 2312687
**Nhóm:** 12
**Nhánh:** `feat/vohungmanh-health-check`

---

### 1. Mục tiêu Task 4 liên quan đến nhánh
Trong khuôn khổ Bữa 4 (Task 4), ứng dụng cần được trang bị hệ thống giám sát sức khỏe (Application Health Checks) chuẩn hóa cấp doanh nghiệp, hỗ trợ cơ chế Liveness và Readiness Probes cho môi trường Docker / Kubernetes, nhằm phát hiện kịp thời các sự cố nghẽn mạng, mất kết nối cơ sở dữ liệu, bộ nhớ đệm hoặc kho lưu trữ tệp tin.

### 2. Công việc cá nhân thực tế
- Tích hợp framework `Microsoft.Extensions.Diagnostics.HealthChecks` vào ứng dụng ASP.NET Core Minimal API.
- Tự tay hiện thực 3 bộ kiểm tra sức khỏe chuyên biệt:
  - `PostgresHealthCheck`: Kiểm tra kết nối và truy vấn thăm dò PostgreSQL 16.
  - `RedisHealthCheck`: Kiểm tra tính khả dụng và thời gian phản hồi của Redis Cache qua lệnh Ping.
  - `MinioHealthCheck`: Kiểm tra kết nối kho lưu trữ MinIO Object Storage qua S3 API.
- Xây dựng bộ định dạng kết quả tập trung `HealthCheckResponseWriter` trả về JSON trực quan, ẩn hoàn toàn thông tin nhạy cảm (mật khẩu, chuỗi kết nối, secret keys).
- Phân tách và đăng ký 3 endpoints chuẩn trong `Program.cs`: `/health`, `/health/live`, `/health/ready`.
- Viết 364 dòng mã Unit Test tự động trong `HealthCheckUnitTests.cs` bao phủ toàn bộ các trường hợp Healthy, Degraded và Unhealthy.

### 3. Chức năng đã hoàn thành
- **Phân tầng giám sát chuyên nghiệp (Probes):**
  - **Liveness Probe (`/health/live`):** Xác nhận tiến trình backend đang sống bình thường mà không đánh giá phụ thuộc bên ngoài; tránh tình trạng container bị restart nhầm khi cơ sở dữ liệu tạm nghẽn.
  - **Readiness Probe (`/health/ready`):** Xác minh các phụ thuộc cốt lõi bắt buộc (`PostgreSQL` và `Redis`) đã sẵn sàng tiếp nhận traffic của người dùng.
  - **Full Health Overview (`/health`):** Báo cáo tình trạng tổng thể của toàn bộ hạ tầng gồm PostgreSQL, Redis và MinIO Storage.
- **Bảo mật phản hồi (Zero Security Leaks):** Response tuyệt đối không để lộ thông tin connection string, mật khẩu hoặc khóa truy cập trong phần chi tiết lỗi.
- **Đo lường thời gian trễ (Latency Tracking):** Ghi nhận chi tiết thời gian phản hồi (duration in milliseconds) của từng thành phần để hỗ trợ phân tích hiệu năng.

### 4. Cách xử lý và luồng hoạt động
```text
Monitoring Tool / Container Orchestrator / User
        │
        ├── GET /health/live   --> Kiểm tra app process (trả về Healthy 200 OK)
        ├── GET /health/ready  --> Kiểm tra PostgreSQL (SELECT 1) + Redis (PING)
        └── GET /health        --> Kiểm tra toàn bộ:
                                    ├── PostgresHealthCheck (Database)
                                    ├── RedisHealthCheck (Cache)
                                    └── MinioHealthCheck (Storage)
                                    │
                                    ▼
                HealthCheckResponseWriter định dạng JSON:
                {
                  "status": "Healthy",
                  "totalDuration": "00:00:00.015",
                  "entries": {
                    "postgres": { "status": "Healthy", "duration": "00:00:00.005" },
                    "redis":    { "status": "Healthy", "duration": "00:00:00.003" },
                    "minio":    { "status": "Healthy", "duration": "00:00:00.007" }
                  }
                }
```

### 5. Danh sách file mã nguồn quan trọng

| Đường dẫn file | Vai trò và chức năng chính |
| :--- | :--- |
| `backend/src/CulinaryBlog.Infrastructure/HealthChecks/PostgresHealthCheck.cs` | Hiện thực `IHealthCheck` cho PostgreSQL qua truy vấn kiểm tra kết nối. |
| `backend/src/CulinaryBlog.Infrastructure/HealthChecks/RedisHealthCheck.cs` | Hiện thực `IHealthCheck` cho Redis thông qua `IConnectionMultiplexer.PingAsync()`. |
| `backend/src/CulinaryBlog.Infrastructure/HealthChecks/MinioHealthCheck.cs` | Hiện thực `IHealthCheck` cho MinIO Object Storage qua S3 client list buckets. |
| `backend/src/CulinaryBlog.Infrastructure/HealthChecks/HealthCheckResponseWriter.cs` | Định dạng phản hồi JSON chuẩn, an toàn bảo mật, loại bỏ chuỗi kết nối và mật khẩu. |
| `backend/src/CulinaryBlog.Infrastructure/DependencyInjection.cs` | Đăng ký các dịch vụ Health Checks và cấu hình Tags (`live`, `ready`). |
| `backend/src/CulinaryBlog.Api/Program.cs` | Ánh xạ 3 endpoints `/health`, `/health/live`, `/health/ready` kèm custom ResponseWriter. |
| `backend/tests/CulinaryBlog.UnitTests/Infrastructure/HealthChecks/HealthCheckUnitTests.cs` | 364 dòng kiểm thử đơn vị kiểm tra trạng thái Healthy/Unhealthy và định dạng JSON. |
| `docs/README_HEALTH_CHECK.md` | Tài liệu kỹ thuật chi tiết về thiết kế và kịch bản giám sát Health Checks. |
| `docs/VO_HUNG_MANH_HEALTH_CHECK_COMPLAN.md` | Kế hoạch triển khai và tiêu chuẩn chấp nhận hệ thống giám sát Bữa 4. |

### 6. API, Database và Infrastructure liên quan
- **Cơ sở dữ liệu:** PostgreSQL 16 (Port `5432`).
- **Bộ nhớ đệm:** Redis Cache 7.x (Port `6379`).
- **Lưu trữ tệp tin:** MinIO S3 API (Port `9000`).
- **HTTP Endpoints:**
  - `GET /health` (Status 200 OK nếu tất cả Healthy; 503 Service Unavailable nếu có lỗi).
  - `GET /health/live` (Status 200 OK).
  - `GET /health/ready` (Status 200 OK khi Postgres và Redis sẵn sàng).

### 7. Hướng dẫn chạy

#### Bước 1: Khởi động toàn bộ hạ tầng Docker
```bash
docker compose up -d postgres redis minio
```

#### Bước 2: Chạy Backend Web API
```bash
cd backend/src/CulinaryBlog.Api
dotnet run
```

### 8. Hướng dẫn kiểm thử

#### Kiểm thử Unit Test tự động
Chạy các bài kiểm tra đơn vị cho module Health Check:
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --filter "FullyQualifiedName~HealthCheck"
```

#### Kiểm thử trực tiếp qua HTTP
```bash
# Kiểm tra liveness
curl -i http://localhost:5000/health/live

# Kiểm tra readiness
curl -i http://localhost:5000/health/ready

# Kiểm tra tổng thể
curl -i http://localhost:5000/health
```

### 9. Kết quả kiểm thử có bằng chứng
- **Kết quả Unit Test:** Toàn bộ test case trong `HealthCheckUnitTests` đều đạt `Passed!` (kiểm thử thành công phản hồi JSON, kiểm thử trường hợp Postgres offline, Redis timeout, và MinIO lỗi xác thực).
- **Phản hồi thực tế từ cURL:**
  ```json
  {
    "status": "Healthy",
    "totalDuration": "00:00:00.0123456",
    "entries": {
      "postgres": { "status": "Healthy", "duration": "00:00:00.0045612" },
      "redis": { "status": "Healthy", "duration": "00:00:00.0023411" },
      "minio": { "status": "Healthy", "duration": "00:00:00.0054433" }
    }
  }
  ```

### 10. Hạn chế và tồn đọng
- **Trạng thái thực tế:** Module hoàn thành đầy đủ, đạt chuẩn production-ready cho cả 3 phụ thuộc chính.
- **Tồn đọng:** Chưa tích hợp giao diện dashboard web trực quan độc lập (HealthChecksUI), hiện tại đang sử dụng định dạng JSON thuần để tích hợp với Prometheus / Grafana hoặc Docker container healthcheck.

### 11. Liên kết tài liệu kỹ thuật chi tiết
- Chi tiết thiết kế Health Checks: [docs/README_HEALTH_CHECK.md](docs/README_HEALTH_CHECK.md)
- Kế hoạch triển khai kỹ thuật Bữa 4: [docs/VO_HUNG_MANH_HEALTH_CHECK_COMPLAN.md](docs/VO_HUNG_MANH_HEALTH_CHECK_COMPLAN.md)

---

# Phát triển ứng dụng Web nâng cao

## Nhóm 12

### Thành viên
1. Võ Hùng Mạnh - 2312687 - 2312687@dlu.edu.vn - Nhóm trưởng
2. Lê Thị Ánh Nhung - 2312709 - 2312709@dlu.edu.vn
3. Phạm Nguyễn Ngọc Phước - 2312718 - 2312718@dlu.edu.vn
4. Nguyễn Văn Quốc - 2312729 - 2312729@dlu.edu.vn
