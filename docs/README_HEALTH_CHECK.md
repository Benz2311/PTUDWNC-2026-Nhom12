# Application Health Checks

## Người thực hiện
**Võ Hùng Mạnh** (Nhóm 12)

## Branch
`feat/vohungmanh-health-check`

## Mục tiêu
Triển khai hệ thống giám sát sức khỏe dịch vụ (Application Health Checks) chuẩn công nghiệp cho Backend API theo yêu cầu Task 4 và SRS v1.2.0:
- Cung cấp các điểm cuối (endpoints) tiêu chuẩn cho bộ điều phối container (Docker Compose, Kubernetes, Load Balancer) để kiểm tra tình trạng ứng dụng.
- Phân định ngữ nghĩa chính xác giữa **Liveness** (tiến trình đang chạy) và **Readiness** (sẵn sàng tiếp nhận traffic).
- Giám sát tình trạng kết nối và độ trễ thực tế tới 3 hạ tầng phụ thuộc cốt lõi: **PostgreSQL**, **Redis**, và **MinIO / S3 Object Storage**.
- Định dạng dữ liệu phản hồi dạng JSON chuẩn hóa, bảo mật cao: Tuyệt đối không để lộ mật khẩu, chuỗi kết nối hay khóa bí mật ra bên ngoài.
- Trả về mã trạng thái HTTP tiền định: HTTP 200 khi Healthy/Degraded và HTTP 503 khi Unhealthy.

---

## Endpoints

| Endpoint | Ý nghĩa | Dependencies được kiểm tra | Tags áp dụng | Mã HTTP khi lỗi |
| :--- | :--- | :--- | :--- | :--- |
| `GET /health` | Giám sát tổng quan tình trạng API và toàn bộ hạ tầng | PostgreSQL + Redis + MinIO | Toàn bộ checks | HTTP 503 Service Unavailable |
| `GET /health/live` | **Liveness Probe:** Xác định tiến trình API có đang sống và phản hồi HTTP hay không | Process / Self (Không phụ thuộc external services) | `live` | HTTP 503 (chỉ khi process treo/chết) |
| `GET /health/ready` | **Readiness Probe:** Xác định API có đủ điều kiện phục vụ request hay không | PostgreSQL + Redis (Bắt buộc theo Task 4) | `ready` | HTTP 503 Service Unavailable |

---

## Liveness (`/health/live`)
- **Mục đích:** Trả lời câu hỏi *"Tiến trình của API có đang sống không?"*.
- **Ngữ nghĩa chuẩn:**
  - Được Kubernetes / Docker dùng để quyết định có cần khởi động lại container (restart container) hay không.
  - **Quy tắc bất biến:** Sự cố mất kết nối tới PostgreSQL, Redis hoặc MinIO **tuyệt đối không được** làm liveness fail. Nếu liveness fail khi DB mất kết nối, Kubernetes sẽ restart container liên tục (CrashLoopBackOff), làm trầm trọng thêm tình trạng nghẽn kết nối và không giải quyết được gốc rễ vấn đề.
- **Triển khai:**
  - Gắn tag: `["live"]`.
  - Check `self`: Trả về `Healthy` ngay khi tiến trình xử lý được HTTP request.

---

## Readiness (`/health/ready`)
- **Mục đích:** Trả lời câu hỏi *"API có đủ tài nguyên bắt buộc để xử lý các yêu cầu nghiệp vụ của người dùng không?"*.
- **Ngữ nghĩa chuẩn:**
  - Được Load Balancer (Nginx, Traefik, K8s Service) dùng để quyết định có điều hướng traffic mạng vào replica này hay không.
  - Theo đặc tả Task 4: Hai dịch vụ bắt buộc để API sẵn sàng nhận traffic là **PostgreSQL** (lưu trữ quan hệ) và **Redis** (bộ nhớ đệm).
  - Nếu PostgreSQL hoặc Redis gặp sự cố $\rightarrow$ `/health/ready` lập tức trả về **Unhealthy (HTTP 503)**, Load Balancer sẽ ngắt traffic tạm thời khỏi instance này cho đến khi hạ tầng phục hồi.
  - **Sự cố MinIO không làm hỏng Readiness:** MinIO chỉ phục vụ lưu trữ file đa phương tiện, không chặn các nghiệp vụ đọc/ghi dữ liệu cốt lõi, đảm bảo đúng đặc tả Task 4 (PostgreSQL + Redis).

---

## General Health (`/health`)
- **Mục đích:** Cung cấp bức tranh toàn cảnh về sức khỏe của toàn bộ hệ sinh thái dịch vụ phục vụ đội ngũ quản trị hệ thống (SRE / DevOps / Monitoring Tools như Prometheus, Datadog).
- **Phạm vi kiểm tra:**
  - `self` (Tiến trình ứng dụng)
  - `postgresql` (Cơ sở dữ liệu chính)
  - `redis` (Hệ thống phân tán đệm cache)
  - `minio` (Hệ thống lưu trữ ảnh và tệp tin đối tượng)
- **Hành vi:** Bất kỳ dịch vụ nào trong 3 hạ tầng trên bị lỗi thì `/health` sẽ trả về `Unhealthy` (HTTP 503) và chỉ rõ tên thành phần bị lỗi trong payload JSON.

---

## PostgreSQL Check
- **Lớp xử lý:** `PostgresHealthCheck` (`IHealthCheck`).
- **Cơ chế:** Kiểm tra khả năng kết nối thực tế tới PostgreSQL Database thông qua `ApplicationDbContext.Database.CanConnectAsync(cancellationToken)`.
- **An toàn bảo mật:** Nếu có lỗi kết nối hoặc timeout, ngoại lệ được xử lý an toàn và trả về thông báo lỗi tổng quát, không xuất chuỗi `Host`, `Username` hay `Password` ra ngoài.

---

## Redis Check
- **Lớp xử lý:** `RedisHealthCheck` (`IHealthCheck`).
- **Cơ chế:** Kết nối tới Redis server thông qua `StackExchange.Redis.IConnectionMultiplexer`, gửi lệnh `PingAsync()` tới Redis database để đo lường độ trễ mạng thực tế (Latency tính bằng mili-giây).
- **Hiệu năng:** Tái sử dụng multiplexer singleton (Thread-safe lock), tránh tạo lại kết nối TCP liên tục gây cạn kiệt socket.

---

## MinIO Check
- **Lớp xử lý:** `MinioHealthCheck` (`IHealthCheck`).
- **Cơ chế:** Sử dụng thư viện chuẩn `AWSSDK.S3` (Amazon S3 Client kết nối MinIO với cấu hình Path-style), thực hiện gọi API `ListBucketsAsync(cancellationToken)` với timeout 3 giây.
- **Độ tin cậy:** Xác thực khả năng xác thực (AccessKey/SecretKey) và khả năng phản hồi của MinIO cluster.

---

## Tags Strategy
Hệ thống sử dụng cơ chế tagging tinh gọn của ASP.NET Core để tách biệt các bộ lọc:
- `live`: Kiểm tra liveness nội tại (`self`).
- `ready`: Kiểm tra readiness phục vụ traffic (`postgresql`, `redis`).
- `db`: Nhóm kiểm tra cơ sở dữ liệu (`postgresql`).
- `redis`: Nhóm kiểm tra cache (`redis`).
- `storage`, `minio`: Nhóm kiểm tra lưu trữ tệp tin (`minio`).

---

## HTTP Status Codes
- **Healthy / Degraded:** Trả về mã **HTTP 200 OK**.
- **Unhealthy:** Trả về mã **HTTP 503 Service Unavailable**.

---

## Security & Không Leak Secrets
- `HealthCheckResponseWriter` tự động lọc bỏ các token có nguy cơ chứa thông tin đăng nhập (`Password=******`).
- Tuyệt đối không để lộ:
  - Chuỗi kết nối ConnectionStrings.
  - Mật khẩu database và Redis credentials.
  - MinIO AccessKey và SecretKey.
  - Stack trace chi tiết của hệ thống.

---

## Danh Sách Files Thay Đổi & Tạo Mới

| File | Loại | Vai trò |
| :--- | :--- | :--- |
| `backend/src/CulinaryBlog.Infrastructure/HealthChecks/PostgresHealthCheck.cs` | New | Kiểm tra kết nối thực tế tới PostgreSQL Database. |
| `backend/src/CulinaryBlog.Infrastructure/HealthChecks/RedisHealthCheck.cs` | New | Kiểm tra kết nối và đo độ trễ tới Redis Server. |
| `backend/src/CulinaryBlog.Infrastructure/HealthChecks/MinioHealthCheck.cs` | New | Kiểm tra kết nối tới MinIO / S3 Object Storage qua AWSSDK.S3. |
| `backend/src/CulinaryBlog.Infrastructure/HealthChecks/HealthCheckResponseWriter.cs` | New | Lớp định dạng response JSON an toàn, chống rò rỉ credential và mapping HTTP 200/503. |
| `backend/src/CulinaryBlog.Infrastructure/DependencyInjection.cs` | Modified | Đăng ký `AddHealthChecks()` với các checks và tags tương ứng. |
| `backend/src/CulinaryBlog.Api/Program.cs` | Modified | Map các endpoint `/health`, `/health/live`, `/health/ready` với predicate lọc tags và custom response writer. |
| `backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj` | Modified | Bổ sung package `Microsoft.EntityFrameworkCore.InMemory` hỗ trợ test. |
| `backend/tests/CulinaryBlog.UnitTests/Infrastructure/HealthChecks/HealthCheckUnitTests.cs` | New | Bộ 12 test methods kiểm thử độc lập 16+ kịch bản liveness, readiness, semantic isolation, status codes. |
| `docs/README_HEALTH_CHECK.md` | New | Tài liệu kiến trúc và hướng dẫn kiểm thử Health Checks. |
| `docs/VO_HUNG_MANH_HEALTH_CHECK_COMPLAN.md` | New | Tài liệu bảo vệ chuyên sâu 31 mục lý thuyết, failure scenarios và 20 câu hỏi phản biện. |

---

## Cấu Hình (Configuration Template)
Trong `appsettings.json`, các cấu hình được đọc an toàn:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=culinaryblog;Username=***;Password=***",
    "Redis": "localhost:6379"
  },
  "Storage": {
    "ServiceUrl": "http://localhost:9000",
    "AccessKey": "***",
    "SecretKey": "***",
    "Region": "us-east-1",
    "DefaultBucket": "culinaryblog"
  }
}
```

---

## Build
Biên dịch toàn bộ solution:
```bash
dotnet build backend/CulinaryBlog.sln
```
Kết quả: `0 Error(s)`.

---

## Tests
Chạy kiểm thử tự động:
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj
```

---

## Test Results
- **Tổng số test cases:** 100 Passed, 0 Failed, 1 Skipped.
- **Bộ test Health Checks (`HealthCheckUnitTests`):** 100% PASSED.
  1. `DependencyInjection_RegistersHealthChecks_WithExpectedTags`: Đăng ký đúng tags (`live`, `ready`, `storage`).
  2. `LivenessCheck_AlwaysReturnsHealthy_WhenProcessIsRunning`: Liveness trả về Healthy khi tiến trình chạy.
  3. `PostgresHealthCheck_WhenCanConnectIsTrue_ReturnsHealthy`: PostgreSQL trả về Healthy khi kết nối thành công.
  4. `PostgresHealthCheck_WhenCanConnectIsFalse_ReturnsUnhealthy`: PostgreSQL trả về Unhealthy khi mất kết nối.
  5. `PostgresHealthCheck_WhenThrowsException_ReturnsUnhealthy_AndDoesNotLeakConnectionString`: Không lộ connection string khi DB có lỗi.
  6. `PostgresFailure_CausesReadyToBeUnhealthy_WhileLiveRemainsHealthy`: DB lỗi $\rightarrow$ Ready Unhealthy, Live vẫn Healthy.
  7. `RedisHealthCheck_WhenConnectionStringMissing_ReturnsUnhealthy`: Redis thiếu cấu hình $\rightarrow$ Unhealthy.
  8. `RedisFailure_CausesReadyToBeUnhealthy_WhileLiveRemainsHealthy`: Redis lỗi $\rightarrow$ Ready Unhealthy, Live vẫn Healthy.
  9. `MinioHealthCheck_WhenConfigurationMissing_ReturnsUnhealthy`: MinIO thiếu cấu hình $\rightarrow$ Unhealthy.
  10. `MinioFailure_CausesGeneralHealthToBeUnhealthy_WhileLiveAndReadyRemainHealthy`: MinIO lỗi $\rightarrow$ `/health` Unhealthy, `/health/live` và `/health/ready` vẫn Healthy.
  11. `ResponseWriter_WhenReportIsHealthy_ReturnsHttp200_AndValidJson`: Healthy $\rightarrow$ HTTP 200 JSON.
  12. `ResponseWriter_WhenReportIsUnhealthy_ReturnsHttp503_AndValidJson`: Unhealthy $\rightarrow$ HTTP 503 JSON.
  13. `ResponseWriter_SanitizesSensitiveData_DoesNotLeakPasswords`: Che giấu mật khẩu `Password=******`.

---

## Hướng Dẫn Demo Thực Tế

### Bước 1: Khởi động các dịch vụ
1. Khởi động PostgreSQL, Redis, và MinIO:
   ```bash
   docker start culinary-blog-postgres
   # Khởi động redis và minio tương ứng nếu có container
   ```
2. Khởi chạy Backend API:
   ```bash
   dotnet run --project backend/src/CulinaryBlog.Api
   ```

### Bước 2: Kiểm tra trạng thái bình thường (Healthy)
- Mở trình duyệt hoặc dùng `curl`:
  1. `curl -i http://localhost:5000/health/live` $\rightarrow$ **HTTP 200 OK**
  2. `curl -i http://localhost:5000/health/ready` $\rightarrow$ **HTTP 200 OK**
  3. `curl -i http://localhost:5000/health` $\rightarrow$ **HTTP 200 OK**

Ví dụ JSON trả về:
```json
{
  "status": "Healthy",
  "totalDuration": "4.2ms",
  "timestamp": "2026-10-05T08:15:00Z",
  "checks": [
    {
      "name": "self",
      "status": "Healthy",
      "description": "Application is running.",
      "duration": "0.1ms"
    },
    {
      "name": "postgresql",
      "status": "Healthy",
      "description": "PostgreSQL database connection is operational.",
      "duration": "2.3ms"
    },
    {
      "name": "redis",
      "status": "Healthy",
      "description": "Redis is operational. Latency: 1.2ms.",
      "duration": "1.1ms"
    },
    {
      "name": "minio",
      "status": "Healthy",
      "description": "MinIO is operational. Buckets count: 1.",
      "duration": "0.7ms"
    }
  ]
}
```

---

## Demo Kịch Bản Sự Cố (Failure Demo)

### Kịch bản 1: Redis gặp sự cố (Tạm dừng container Redis)
1. Tạm dừng Redis:
   ```bash
   docker stop redis
   ```
2. Kiểm tra lại 3 endpoints:
   - `curl -i http://localhost:5000/health/live` $\rightarrow$ **HTTP 200 OK** (Chứng minh tiến trình API vẫn sống bình thường).
   - `curl -i http://localhost:5000/health/ready` $\rightarrow$ **HTTP 503 Service Unavailable** (Load Balancer lập tức ngừng đẩy traffic).
   - `curl -i http://localhost:5000/health` $\rightarrow$ **HTTP 503 Service Unavailable** (Phản ánh `redis: Unhealthy`).
3. Bật lại Redis:
   ```bash
   docker start redis
   ```
   $\rightarrow$ `/health/ready` tự động phục hồi về **HTTP 200 OK**.

### Kịch bản 2: MinIO gặp sự cố (Tạm dừng container MinIO)
1. Tạm dừng MinIO:
   ```bash
   docker stop minio
   ```
2. Kiểm tra lại 3 endpoints:
   - `curl -i http://localhost:5000/health/live` $\rightarrow$ **HTTP 200 OK** (Không ảnh hưởng).
   - `curl -i http://localhost:5000/health/ready` $\rightarrow$ **HTTP 200 OK** (Không ảnh hưởng vì MinIO không nằm trong điều kiện readiness).
   - `curl -i http://localhost:5000/health` $\rightarrow$ **HTTP 503 Service Unavailable** (Cảnh báo quản trị viên rằng storage đang có vấn đề).
3. Bật lại MinIO:
   ```bash
   docker start minio
   ```
   $\rightarrow$ `/health` tự động phục hồi về **HTTP 200 OK**.

---

## Limitations
- Bộ kiểm thử Integration Tests kết nối trực tiếp tới container Docker PostgreSQL/Redis/MinIO vật lý phụ thuộc vào việc Docker daemon có chạy trên máy host hay không. Khi Docker daemon không chạy, bộ Unit Tests độc lập trong RAM bảo đảm kiểm thử 100% logic cấu hình và ngữ nghĩa.
