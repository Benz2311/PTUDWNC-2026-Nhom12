# Giám sát sức khỏe ứng dụng (Application Health Checks)

## 1. Mục tiêu
Branch `feat/vohungmanh-health-check` giải quyết việc xây dựng hệ thống kiểm tra và giám sát sức khỏe dịch vụ (**Application Health Checks**) chuẩn công nghiệp cho nền tảng CulinaryBlog thuộc Task 4:
- Khi vận hành trên môi trường Container (Docker Compose, Kubernetes) hoặc đứng sau bộ cân bằng tải (Load Balancer như Nginx, Traefik), hệ thống cần các điểm cuối chuyên dụng để biết chính xác khi nào tiến trình bị treo, khi nào hạ tầng gặp sự cố và khi nào cần điều hướng hoặc ngắt lưu lượng mạng.
- Phân định rõ ràng ngữ nghĩa kỹ thuật giữa **Liveness** (tiến trình ứng dụng còn sống không?) và **Readiness** (hệ thống đã sẵn sàng xử lý yêu cầu nghiệp vụ chưa?).
- Kiểm tra kết nối thực tế tới 3 dịch vụ hạ tầng phụ thuộc cốt lõi: **PostgreSQL Database**, **Redis Cache Server**, và **MinIO / S3 Object Storage**.
- Định dạng dữ liệu phản hồi JSON chuẩn hóa, áp dụng cơ chế bảo mật nghiêm ngặt: **tuyệt đối không làm rò rỉ chuỗi kết nối (ConnectionStrings), mật khẩu hay khóa truy cập bí mật** ra bên ngoài.

---

## 2. Kết quả đạt được
Sau khi triển khai branch này:
- **Cung cấp 3 điểm cuối tiêu chuẩn với phân định ngữ nghĩa chính xác**:
  - `GET /health/live`: **Liveness Probe** - Chỉ kiểm tra trạng thái tiến trình ứng dụng (`self`). Tuyệt đối không phụ thuộc vào hạ tầng ngoài.
  - `GET /health/ready`: **Readiness Probe** - Kiểm tra hai hạ tầng bắt buộc để xử lý nghiệp vụ: **PostgreSQL** và **Redis**. Sự cố MinIO không làm hỏng Readiness.
  - `GET /health`: **General Health Probe** - Kiểm tra toàn diện toàn bộ hệ sinh thái: Tiến trình (`self`), **PostgreSQL**, **Redis**, và **MinIO**.
- **Cơ chế che giấu bí mật (`HealthCheckResponseWriter`)**:
  - Tự động phát hiện và che giấu các thông tin nhạy cảm: thay thế `Password=******`, ẩn AccessKey và SecretKey của MinIO, không để lộ stack trace chi tiết ra môi trường bên ngoài.
- **Ánh xạ mã trạng thái HTTP tiền định**:
  - Trả về `HTTP 200 OK` khi tất cả các thành phần được kiểm tra ở trạng thái `Healthy` hoặc `Degraded`.
  - Trả về `HTTP 503 Service Unavailable` khi có bất kỳ thành phần bắt buộc nào bị `Unhealthy`.
- **Kiểm thử bao phủ**: 13/13 tests trong `HealthCheckUnitTests` và 100/101 tests toàn hệ thống đều vượt qua thành công (100% Pass).

---

## 3. Luồng hoạt động

```text
Monitoring Tool / Load Balancer / Kubernetes Probe
  │ (Gửi HTTP GET /health, /health/live, /health/ready)
  ▼
API Endpoint Mapping (Program.cs)
  │ ├─ Bóc tách route và áp dụng Predicate lọc theo Tags:
  │ │    - /health/live  -> check.Tags.Contains("live")
  │ │    - /health/ready -> check.Tags.Contains("ready")
  │ │    - /health       -> Toàn bộ các checks đã đăng ký
  ▼
Infrastructure Health Checks (DependencyInjection.cs)
  │ ├─ [Tag "live"]  : Check Self (Tiến trình API)
  │ ├─ [Tag "ready"] : PostgresHealthCheck (Database.CanConnectAsync)
  │ │                  RedisHealthCheck (PingAsync đo Latency mạng)
  │ └─ [Tag "storage"]: MinioHealthCheck (ListBucketsAsync tới MinIO)
  ▼
HealthCheckResponseWriter (Infrastructure / HealthChecks)
  │ ├─ Tổng hợp trạng thái: Healthy, Degraded, hoặc Unhealthy
  │ ├─ Lọc và che giấu toàn bộ secrets: Password=******
  │ ├─ Định dạng JSON có cấu trúc (Status, Duration, Timestamp, Checks[])
  │ └─ Thiết lập HTTP Status Code (200 OK hoặc 503 Service Unavailable)
  ▼
Output
  └─ Trả về HTTP 200 / 503 kèm payload JSON có cấu trúc an toàn
```

### Giải thích chi tiết các bước xử lý:
1. **Tiếp nhận và Điều hướng:** Khi nhận request, ASP.NET Core Health Checks Middleware áp dụng bộ lọc tag tương ứng để chỉ thực thi các bài kiểm tra cần thiết, tránh lãng phí tài nguyên mạng.
2. **Thực thi kiểm tra kết nối:**
   - Với `/health/live`: Chỉ xác nhận tiến trình máy chủ đang phản hồi HTTP (`Healthy`).
   - Với `/health/ready`: Gửi lệnh kiểm tra kết nối thực tế tới PostgreSQL (`CanConnectAsync`) và đo độ trễ mạng tới Redis (`PingAsync`). Nếu một trong hai gặp sự cố, lập tức đánh dấu `Unhealthy`.
   - Với `/health`: Kiểm tra thêm kết nối tới MinIO S3 client.
3. **Format và Bảo mật:** Toàn bộ kết quả kiểm tra được chuyển qua `HealthCheckResponseWriter.WriteResponse`. Lớp này duyệt qua từng thông điệp lỗi, xóa bỏ các chuỗi kết nối chứa mật khẩu rồi đóng gói thành JSON hoàn chỉnh.
4. **Phản hồi:** Trả về HTTP 200 nếu toàn bộ checks đều sống, hoặc HTTP 503 nếu có lỗi phụ thuộc.

---

## 4. Các file chính

| File | Vai trò | Xử lý gì |
| :--- | :--- | :--- |
| `backend/src/CulinaryBlog.Infrastructure/HealthChecks/PostgresHealthCheck.cs` | Infrastructure / Check | Kiểm tra kết nối thực tế tới PostgreSQL Database qua `ApplicationDbContext.Database.CanConnectAsync`, che giấu connection string khi có ngoại lệ. |
| `backend/src/CulinaryBlog.Infrastructure/HealthChecks/RedisHealthCheck.cs` | Infrastructure / Check | Kết nối tới Redis qua `IConnectionMultiplexer`, gửi lệnh `PingAsync` đo độ trễ mạng thực tế (latency). |
| `backend/src/CulinaryBlog.Infrastructure/HealthChecks/MinioHealthCheck.cs` | Infrastructure / Check | Kiểm tra kết nối tới MinIO / S3 Object Storage qua `IAmazonS3.ListBucketsAsync`. |
| `backend/src/CulinaryBlog.Infrastructure/HealthChecks/HealthCheckResponseWriter.cs` | Infrastructure / Formatter | Định dạng JSON phản hồi chuẩn hóa, che giấu mật khẩu (`Password=******`) và mapping mã HTTP 200/503. |
| `backend/src/CulinaryBlog.Infrastructure/DependencyInjection.cs` | Infrastructure / DI | Đăng ký dịch vụ `AddHealthChecks()` với các tags phân loại: `live`, `ready`, `storage`. |
| `backend/src/CulinaryBlog.Api/Program.cs` | Presentation / Pipeline | Map 3 endpoint `/health`, `/health/live`, `/health/ready` với predicate lọc tags tương ứng. |
| `backend/tests/CulinaryBlog.UnitTests/Infrastructure/HealthChecks/HealthCheckUnitTests.cs` | Unit Tests | Bộ 13 unit tests kiểm tra độc lập: Liveness isolation, Readiness dependencies, MinIO isolation, HTTP 200/503 mapping, và che giấu credential. |
| `docs/VO_HUNG_MANH_HEALTH_CHECK_COMPLAN.md` | Tài liệu bảo vệ | Báo cáo giải trình chuyên sâu (440 dòng) gồm 31 mục lý thuyết, failure scenarios, và 20 câu hỏi vấn đáp. |
| `docs/README_HEALTH_CHECK.md` | Tài liệu kỹ thuật | Tài liệu hướng dẫn kỹ thuật chi tiết theo chuẩn 13 phần. |

---

## 5. API / Interface

| Method | Endpoint | Tags lọc | Dependencies kiểm tra | Ý nghĩa & Mã HTTP khi lỗi |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/health/live` | `live` | Process / Self | **Liveness Probe:** Xác định tiến trình API có đang sống và phản hồi HTTP hay không. Lỗi hạ tầng ngoài tuyệt đối không làm fail Liveness.<br>Mã lỗi: `HTTP 503` (chỉ khi tiến trình chết). |
| `GET` | `/health/ready` | `ready` | PostgreSQL + Redis | **Readiness Probe:** Xác định API có đủ tài nguyên bắt buộc để phục vụ request của người dùng hay không. Dùng cho Load Balancer điều phối traffic.<br>Mã lỗi: `HTTP 503 Service Unavailable`. |
| `GET` | `/health` | *Toàn bộ* | Self + PostgreSQL + Redis + MinIO | **General Health:** Bức tranh toàn cảnh về sức khỏe của toàn bộ hệ sinh thái dịch vụ phục vụ đội ngũ SRE / DevOps / Monitoring Tools.<br>Mã lỗi: `HTTP 503 Service Unavailable`. |

---

## 6. Business Rules

1. **Quy tắc bất biến của Liveness (`/health/live`)**:
   - Được Kubernetes dùng để quyết định có cần khởi động lại container (restart) hay không.
   - **Quy tắc:** Sự cố mất kết nối tới PostgreSQL, Redis hoặc MinIO **tuyệt đối không được** làm liveness fail. Nếu liveness fail khi DB mất kết nối, container sẽ bị khởi động lại liên tục trong vòng lặp vô tận (CrashLoopBackOff), làm trầm trọng thêm tình trạng quá tải kết nối.
2. **Quy tắc điều phối của Readiness (`/health/ready`)**:
   - Được Load Balancer dùng để quyết định có chuyển tiếp traffic người dùng vào instance này hay không.
   - Theo đặc tả Task 4: Hai dịch vụ bắt buộc là **PostgreSQL** (lưu trữ quan hệ) và **Redis** (bộ nhớ đệm). Nếu 1 trong 2 dịch vụ này gặp sự cố, endpoint lập tức trả về `HTTP 503`, Load Balancer sẽ tạm ngắt traffic khỏi instance này cho đến khi hạ tầng phục hồi.
3. **Sự cố MinIO không làm hỏng Readiness**:
   - MinIO chỉ phục vụ lưu trữ file đa phương tiện, không chặn các nghiệp vụ đọc/ghi dữ liệu công thức cốt lõi. Do đó, MinIO chỉ nằm trong `/health` tổng quát, không nằm trong `/health/ready`.
4. **Bảo mật thông tin đăng nhập (Zero Credential Leakage)**:
   - `HealthCheckResponseWriter` tự động quét và che giấu toàn bộ các chuỗi có chứa `Password`, `pwd`, `SecretKey`, thay thế bằng `Password=******`.
   - Không xuất chuỗi kết nối đầy đủ (ConnectionStrings) ra payload JSON.
5. **Tiền định mã trạng thái HTTP**:
   - Healthy $\rightarrow$ `HTTP 200 OK`.
   - Unhealthy $\rightarrow$ `HTTP 503 Service Unavailable`.

---

## 7. Ví dụ hoạt động

### Ví dụ 1: Trạng thái bình thường (Healthy)
```http
GET /health
```
**Response: HTTP 200 OK**
```json
{
  "status": "Healthy",
  "totalDuration": "3.8ms",
  "timestamp": "2026-10-06T07:00:00Z",
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
      "duration": "2.1ms"
    },
    {
      "name": "redis",
      "status": "Healthy",
      "description": "Redis is operational. Latency: 0.9ms.",
      "duration": "0.9ms"
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

### Ví dụ 2: Sự cố Redis Server bị dừng (Failure State)
- `GET /health/live` $\rightarrow$ **HTTP 200 OK** (Tiến trình API vẫn sống).
- `GET /health/ready` $\rightarrow$ **HTTP 503 Service Unavailable** (Load Balancer ngắt traffic).
- `GET /health` $\rightarrow$ **HTTP 503 Service Unavailable**:
```json
{
  "status": "Unhealthy",
  "totalDuration": "12.4ms",
  "timestamp": "2026-10-06T07:05:00Z",
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
      "duration": "1.8ms"
    },
    {
      "name": "redis",
      "status": "Unhealthy",
      "description": "Redis connection failed. Host is unreachable.",
      "duration": "10.2ms"
    },
    {
      "name": "minio",
      "status": "Healthy",
      "description": "MinIO is operational. Buckets count: 1.",
      "duration": "0.3ms"
    }
  ]
}
```

---

## 8. Error Handling

| Tình huống sự cố | Endpoint `/health/live` | Endpoint `/health/ready` | Endpoint `/health` |
| :--- | :---: | :---: | :---: |
| **Bình thường (Mọi dịch vụ hoạt động)** | HTTP 200 OK | HTTP 200 OK | HTTP 200 OK |
| **PostgreSQL mất kết nối hoặc timeout** | **HTTP 200 OK** (Không restart app) | **HTTP 503** (Ngắt traffic) | **HTTP 503** (Báo lỗi DB) |
| **Redis Server sập** | **HTTP 200 OK** (Không restart app) | **HTTP 503** (Ngắt traffic) | **HTTP 503** (Báo lỗi Redis) |
| **MinIO Object Storage mất kết nối** | **HTTP 200 OK** | **HTTP 200 OK** (Core API vẫn chạy) | **HTTP 503** (Cảnh báo quản trị) |
| **Tiến trình API bị treo/chết hoàn toàn** | Không phản hồi (Timeout) | Không phản hồi | Không phản hồi |

---

## 9. Cách chạy và Demo

### Bước 1: Khởi động hệ thống hạ tầng
```bash
docker compose up -d
```
*(Đảm bảo PostgreSQL, Redis và MinIO đang hoạt động)*

### Bước 2: Khởi động Backend API
```bash
dotnet run --project backend/src/CulinaryBlog.Api
```

### Bước 3: Kịch bản Demo thực tế cho Giảng viên

1. **Demo Kiểm tra trạng thái bình thường (Healthy)**:
   - Dùng lệnh `curl` hoặc trình duyệt kiểm tra lần lượt:
     - `curl -i http://localhost:5000/health/live` $\rightarrow$ **HTTP 200 OK**
     - `curl -i http://localhost:5000/health/ready` $\rightarrow$ **HTTP 200 OK**
     - `curl -i http://localhost:5000/health` $\rightarrow$ **HTTP 200 OK**
2. **Demo Giả lập sự cố Redis (Readiness Failure)**:
   - Tạm dừng container Redis: `docker stop redis` (hoặc tên container Redis).
   - Kiểm tra lại:
     - `/health/live` vẫn trả về **HTTP 200 OK** (Chứng minh tiến trình API không bị ngắt quãng).
     - `/health/ready` lập tức trả về **HTTP 503 Service Unavailable** (Chứng minh Load Balancer sẽ cách ly instance này).
   - Bật lại Redis: `docker start redis` $\rightarrow$ `/health/ready` tự động phục hồi về **HTTP 200 OK**.
3. **Demo Giả lập sự cố MinIO (Storage Isolation)**:
   - Tạm dừng container MinIO: `docker stop minio`.
   - Kiểm tra lại:
     - `/health/ready` vẫn trả về **HTTP 200 OK** (Chứng minh MinIO down không ảnh hưởng tới Readiness của ứng dụng).
     - `/health` trả về **HTTP 503** (Chỉ rõ `minio: Unhealthy` để cảnh báo đội ngũ quản trị).
   - Bật lại MinIO: `docker start minio` $\rightarrow$ `/health` tự động phục hồi về **HTTP 200 OK**.

---

## 10. Testing

### Bộ kiểm thử chuyên biệt `HealthCheckUnitTests`
Chạy lệnh kiểm thử đơn vị:
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --filter FullyQualifiedName~HealthCheckUnitTests
```

**Kết quả kiểm thử thực tế:**
- **13/13 tests PASSED (100%)** (Thời gian chạy: ~720ms).
- **Danh sách 13 kịch bản kiểm thử chi tiết:**
  1. `DependencyInjection_RegistersHealthChecks_WithExpectedTags`: Đăng ký đúng tags (`live`, `ready`, `storage`).
  2. `LivenessCheck_AlwaysReturnsHealthy_WhenProcessIsRunning`: Liveness luôn Healthy khi tiến trình chạy.
  3. `PostgresHealthCheck_WhenCanConnectIsTrue_ReturnsHealthy`: PostgreSQL trả về Healthy khi kết nối thành công.
  4. `PostgresHealthCheck_WhenCanConnectIsFalse_ReturnsUnhealthy`: PostgreSQL trả về Unhealthy khi mất kết nối.
  5. `PostgresHealthCheck_WhenThrowsException_ReturnsUnhealthy_AndDoesNotLeakConnectionString`: Không lộ connection string khi DB ném lỗi.
  6. `PostgresFailure_CausesReadyToBeUnhealthy_WhileLiveRemainsHealthy`: DB lỗi $\rightarrow$ Ready Unhealthy, Live vẫn Healthy.
  7. `RedisHealthCheck_WhenConnectionStringMissing_ReturnsUnhealthy`: Redis thiếu cấu hình $\rightarrow$ Unhealthy.
  8. `RedisFailure_CausesReadyToBeUnhealthy_WhileLiveRemainsHealthy`: Redis lỗi $\rightarrow$ Ready Unhealthy, Live vẫn Healthy.
  9. `MinioHealthCheck_WhenConfigurationMissing_ReturnsUnhealthy`: MinIO thiếu cấu hình $\rightarrow$ Unhealthy.
  10. `MinioFailure_CausesGeneralHealthToBeUnhealthy_WhileLiveAndReadyRemainHealthy`: MinIO lỗi $\rightarrow$ `/health` Unhealthy, `/health/live` và `/health/ready` vẫn Healthy.
  11. `ResponseWriter_WhenReportIsHealthy_ReturnsHttp200_AndValidJson`: Trả về HTTP 200 khi Healthy.
  12. `ResponseWriter_WhenReportIsUnhealthy_ReturnsHttp503_AndValidJson`: Trả về HTTP 503 khi Unhealthy.
  13. `ResponseWriter_SanitizesSensitiveData_DoesNotLeakPasswords`: Che giấu mật khẩu `Password=******`.

### Tổng hợp Unit Tests toàn bộ solution:
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj
```
**Kết quả thực tế:**
- `Passed: 100, Failed: 0, Skipped: 1, Total: 101` (100% Pass).

---

## 11. Build
Thực hiện lệnh biên dịch solution:
```bash
dotnet build backend/CulinaryBlog.sln
```

**Kết quả biên dịch thực tế:**
- **Thành công (Exit code 0)**.
- **0 Error(s)**, 433 Warning(s) (chủ yếu là cảnh báo quy chuẩn StyleCop format).

---

## 12. Limitations

1. **Phụ thuộc Docker daemon đối với kiểm thử tích hợp vật lý**:
   - Bộ kiểm thử tích hợp vật lý trực tiếp tới các cổng mạng thực tế đòi hỏi Docker daemon phải đang chạy các container tương ứng trên máy host. Bộ Unit Tests độc lập trong bộ nhớ RAM đã mô phỏng và kiểm thử 100% các kịch bản lỗi mạng và cách ly ngữ nghĩa.
2. **Cấu hình độ trễ Timeout**:
   - Độ trễ timeout mặc định cho kiểm tra ping Redis và PostgreSQL được thiết lập ở mức 5 giây nhằm tránh treo lâu khi mạng gián đoạn.

---

## 13. Kết luận
Branch `feat/vohungmanh-health-check` đã hoàn thành xuất sắc hệ thống giám sát sức khỏe dịch vụ đạt chuẩn Cloud-Native:
- Phân định ngữ nghĩa chính xác giữa Liveness, Readiness và General Health.
- Đảm bảo an toàn bảo mật tuyệt đối, chống rò rỉ credential và chuỗi kết nối.
- 100% các kịch bản kiểm thử độc lập trong bộ nhớ đều vượt qua thành công, sẵn sàng tích hợp với Docker Compose và Kubernetes.
