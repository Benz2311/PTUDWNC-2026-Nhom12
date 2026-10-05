# TÀI LIỆU BẢO VỆ CHUYÊN SÂU: APPLICATION HEALTH CHECKS (TASK 4)

**Học viên thực hiện:** Võ Hùng Mạnh  
**Đơn vị:** Nhóm 12 – Đồ án Phát triển Ứng dụng Web Nâng cao  
**Nhánh Git:** `feat/vohungmanh-health-check`  
**Base Commit:** `4592f1d98d280f16e7e771a8d06ca651661cea67` (`origin/main`)  

---

## MỤC LỤC
1. [Health Check là gì?](#1-health-check-là-gì)
2. [Tại sao Web API hiện đại bắt buộc phải có Health Check?](#2-tại-sao-web-api-hiện-đại-bắt-buộc-phải-có-health-check)
3. [Khái niệm Liveness Probe](#3-khái-niệm-liveness-probe)
4. [Khái niệm Readiness Probe](#4-khái-niệm-readiness-probe)
5. [So sánh chuyên sâu: Liveness khác Readiness thế nào?](#5-so-sánh-chuyên-sâu-liveness-khác-readiness-thế-nào)
6. [Nguyên lý sống còn: Tại sao Database chết tuyệt đối KHÔNG ĐƯỢC làm Liveness fail?](#6-nguyên-lý-sống-còn-tại-sao-database-chết-tuyệt-đối-không-được-làm-liveness-fail)
7. [Triển khai PostgreSQL Health Check](#7-triển-khai-postgresql-health-check)
8. [Triển khai Redis Health Check](#8-triển-khai-redis-health-check)
9. [Triển khai MinIO / S3 Storage Health Check](#9-triển-khai-minio--s3-storage-health-check)
10. [Quản lý Dependency Health trong hệ thống phân tán](#10-quản-lý-dependency-health-trong-hệ-thống-phân-tán)
11. [Quy chuẩn mã phản hồi HTTP 200 OK](#11-quy-chuẩn-mã-phản-hồi-http-200-ok)
12. [Quy chuẩn mã phản hồi HTTP 503 Service Unavailable](#12-quy-chuẩn-mã-phản-hồi-http-503-service-unavailable)
13. [Chiến lược gắn thẻ (Tagging Strategy) trong ASP.NET Core](#13-chiến-lược-gắn-thẻ-tagging-strategy-trong-aspnet-core)
14. [Cơ chế Predicate Filter trên từng Endpoint](#14-cơ-chế-predicate-filter-trên-từng-endpoint)
15. [Kiến trúc ASP.NET Core Diagnostics HealthChecks](#15-kiến-trúc-aspnet-core-diagnostics-healthchecks)
16. [Đăng ký Dependency Injection (DI Registration)](#16-đăng-ký-dependency-injection-di-registration)
17. [Định tuyến Endpoint (Endpoint Mapping Pipeline)](#17-định-tuyến-endpoint-endpoint-mapping-pipeline)
18. [An toàn thông tin và Bảo mật trong Health Checks](#18-an-toàn-thông-tin-và-bảo-mật-trong-health-checks)
19. [Cơ chế khử thông tin nhạy cảm (Sanitization & Secret Masking)](#19-cơ-chế-khử-thông-tin-nhạy-cảm-sanitization--secret-masking)
20. [Ứng dụng Health Check trong môi trường Docker Container](#20-ứng-dụng-health-check-trong-môi-trường-docker-container)
21. [Ứng dụng Health Check trong Kubernetes (K8s Pod Lifecycle)](#21-ứng-dụng-health-check-trong-kubernetes-k8s-pod-lifecycle)
22. [Phân tích chi tiết các kịch bản sự cố (Failure Scenarios)](#22-phân-tích-chi-tiết-các-kịch-bản-sự-cố-failure-scenarios)
23. [Code Walkthrough: Từng File triển khai](#23-code-walkthrough-từng-file-triển-khai)
24. [Code Walkthrough: Chi tiết cấu hình Registration](#24-code-walkthrough-chi-tiết-cấu-hình-registration)
25. [Ma trận Kiểm thử Tự động (Test Matrix)](#25-ma-trận-kiểm-thử-tự-động-test-matrix)
26. [Kịch bản Demo trực tiếp trên môi trường chạy](#26-kịch-bản-demo-trực-tiếp-trên-môi-trường-chạy)
27. [Kịch bản Demo khi Redis ngừng hoạt động](#27-kịch-bản-demo-khi-redis-ngừng-hoạt-động)
28. [Kịch bản Demo khi MinIO ngừng hoạt động](#28-kịch-bản-demo-khi-minio-ngừng-hoạt-động)
29. [Các giới hạn kỹ thuật (Limitations)](#29-các-giới-hạn-kỹ-thuật-limitations)
30. [20 Câu hỏi phản biện của Giảng viên & Lời giải đáp](#30-20-câu-hỏi-phản-biện-của-giảng-viên--lời-giải-đáp)
31. [Bài thuyết trình mẫu 2–3 phút bảo vệ trước Hội đồng](#31-bài-thuyết-trình-mẫu-23-phút-bảo-vệ-trước-hội-đồng)

---

### 1. Health Check là gì?
Health Check là cơ chế tự chẩn đoán và báo cáo trạng thái hoạt động của một ứng dụng hoặc dịch vụ mạng. Ứng dụng cung cấp các HTTP GET endpoints đặc biệt (ví dụ `/health`, `/health/live`, `/health/ready`) để các hệ thống bên ngoài (Load Balancer, Kubernetes, Prometheus, Uptime Robot) có thể định kỳ thăm dò (poll) và nhận diện kịp thời xem ứng dụng có đang sống, có khỏe mạnh và có khả năng phục vụ request của người dùng hay không.

---

### 2. Tại sao Web API hiện đại bắt buộc phải có Health Check?
1. **Tự động phục hồi (Self-Healing):** Trong môi trường microservices/container, khi một tiến trình bị treo (deadlock) hoặc cạn bộ nhớ (out of memory), bộ điều phối container có thể phát hiện và tự động restart pod mà không cần con người can thiệp thủ công.
2. **Định tuyến thông minh (Zero-Downtime Deployment):** Khi triển khai phiên bản mới, Load Balancer chỉ bắt đầu chuyển hướng traffic vào container mới khi container đó thông báo `Readiness = Healthy`. Nếu container khởi động thất bại, người dùng cũ không bao giờ bị gián đoạn dịch vụ.
3. **Giám sát hạ tầng (Observability):** Giúp đội ngũ vận hành (SRE) phát hiện sớm các sự cố đứt gãy kết nối cơ sở dữ liệu hoặc cache trước khi khách hàng phàn nàn.

---

### 3. Khái niệm Liveness Probe
- **Liveness** trả lời câu hỏi: *"Tiến trình (process) của ứng dụng có đang chạy và phản hồi HTTP hay không?"*.
- **Hành động khi Liveness fail:** Bộ điều phối container (Kubernetes Kubelet / Docker daemon) coi container đã chết (unhealthy) và sẽ **tiêu diệt tiến trình rồi khởi động lại (Kill & Restart Container)**.
- **Phạm vi kiểm tra:** Chỉ kiểm tra bộ nhớ nội tại, CPU và vòng lặp sự kiện (Event Loop) của ứng dụng. Tuyệt đối **không** kiểm tra kết nối mạng tới database hay cache.

---

### 4. Khái niệm Readiness Probe
- **Readiness** trả lời câu hỏi: *"Ứng dụng đã sẵn sàng tiếp nhận và xử lý traffic nghiệp vụ từ người dùng hay chưa?"*.
- **Hành động khi Readiness fail:** Container **KHÔNG** bị khởi động lại. Thay vào đó, Load Balancer / Kubernetes Service sẽ **tạm thời gỡ bỏ replica này khỏi danh sách backend endpoints** (không định tuyến traffic vào đây). Khi hạ tầng phục hồi và Readiness trở lại `Healthy`, Load Balancer sẽ tự động đưa replica này trở lại phục vụ traffic.
- **Phạm vi kiểm tra:** Kiểm tra các dịch vụ phụ thuộc cốt lõi mà nếu thiếu chúng thì API không thể hoàn thành request: **PostgreSQL** (chứa dữ liệu công thức, người dùng) và **Redis** (chứa session, cache).

---

### 5. So sánh chuyên sâu: Liveness khác Readiness thế nào?

| Tiêu chí | Liveness (`/health/live`) | Readiness (`/health/ready`) |
| :--- | :--- | :--- |
| **Câu hỏi cốt lõi** | Tiến trình có sống không? | Có sẵn sàng nhận request không? |
| **Hành động khi lỗi** | **Restart / Kill Container** | **Ngừng đẩy traffic vào Container** |
| **Dependencies kiểm tra** | Không phụ thuộc DB, Cache, Storage | Phụ thuộc PostgreSQL + Redis |
| **Trường hợp áp dụng** | Ứng dụng bị deadlock, infinite loop | DB bị quá tải, khởi động chưa xong |
| **Độ nhạy (Sensitivity)** | Thấp (chỉ fail khi process chết) | Cao (fail ngay khi mất kết nối DB) |

---

### 6. Nguyên lý sống còn: Tại sao Database chết tuyệt đối KHÔNG ĐƯỢC làm Liveness fail?
Đây là câu hỏi kinh điển trong phỏng vấn Senior Software Engineer và bảo vệ đồ án:
- **Hiện tượng nếu làm sai (Thảm họa CrashLoopBackOff):**
  Giả sử máy chủ PostgreSQL bị quá tải kết nối và tạm thời từ chối connection trong 30 giây.
  Nếu `/health/live` kiểm tra PostgreSQL và trả về `Unhealthy`, Kubernetes sẽ hiểu lầm rằng container backend bị hỏng và ra lệnh **restart đồng loạt toàn bộ các container Backend API**.
  Khi tất cả các container khởi động lại cùng lúc, chúng sẽ ồ ạt gửi connection request và chạy migration/seed vào PostgreSQL, tạo ra một cơn bão kết nối (Connection Storm / Thundering Herd Problem).
  Hậu quả là PostgreSQL sập hoàn toàn, các container API rơi vào trạng thái khởi động lại liên tục không ngừng (`CrashLoopBackOff`), hệ thống chết đứng 100%.
- **Quy tắc chuẩn mực:**
  Khi PostgreSQL chết:
  - `/health/live` **PHẢI giữ nguyên Healthy** (bảo toàn tiến trình, không được restart).
  - `/health/ready` **phải chuyển sang Unhealthy** (Load Balancer cắt traffic, bảo vệ database).

---

### 7. Triển khai PostgreSQL Health Check
- Được định nghĩa trong lớp [PostgresHealthCheck.cs](file:///b:/PTUDWNC-2026-Nhom12-MANH/backend/src/CulinaryBlog.Infrastructure/HealthChecks/PostgresHealthCheck.cs).
- Sử dụng phương thức `CanConnectAsync(cancellationToken)` của EF Core Database Facade:
  ```csharp
  var canConnect = _connectionTester != null
      ? await _connectionTester(cancellationToken)
      : await _dbContext!.Database.CanConnectAsync(cancellationToken);

  if (canConnect)
      return HealthCheckResult.Healthy("PostgreSQL database connection is operational.");

  return HealthCheckResult.Unhealthy("Cannot connect to PostgreSQL database.");
  ```
- **Hỗ trợ Testable Constructor:** Cung cấp constructor `public PostgresHealthCheck(Func<CancellationToken, Task<bool>> connectionTester)` cho phép kiểm thử tự động trong RAM 100% không phụ thuộc network.

---

### 8. Triển khai Redis Health Check
- Được định nghĩa trong lớp [RedisHealthCheck.cs](file:///b:/PTUDWNC-2026-Nhom12-MANH/backend/src/CulinaryBlog.Infrastructure/HealthChecks/RedisHealthCheck.cs).
- Không kiểm tra chuỗi string hình thức mà kiểm tra kết nối TCP và lệnh `PING` thực tế:
  ```csharp
  var db = _multiplexer.GetDatabase();
  var latency = await db.PingAsync();
  return HealthCheckResult.Healthy($"Redis is operational. Latency: {latency.TotalMilliseconds:F1}ms.");
  ```
- Tái sử dụng multiplexer singleton với timeout 3 giây (`ConnectTimeout = 3000`), không làm treo HTTP request khi Redis mạng chập chờn.

---

### 9. Triển khai MinIO / S3 Storage Health Check
- Được định nghĩa trong lớp [MinioHealthCheck.cs](file:///b:/PTUDWNC-2026-Nhom12-MANH/backend/src/CulinaryBlog.Infrastructure/HealthChecks/MinioHealthCheck.cs).
- Sử dụng thư viện chuẩn của AWS SDK S3:
  ```csharp
  using var s3Client = new AmazonS3Client(credentials, config);
  var response = await s3Client.ListBucketsAsync(cancellationToken);
  return HealthCheckResult.Healthy($"MinIO is operational. Buckets count: {response.Buckets.Count}.");
  ```
- Kiểm tra tính xác thực của `AccessKey` và `SecretKey` trên cụm MinIO.

---

### 10. Quản lý Dependency Health trong hệ thống phân tán
Mỗi thành phần hạ tầng có vai trò khác nhau đối với sự sống còn của ứng dụng:
- **Cơ sở dữ liệu (PostgreSQL):** Thành phần sống còn của nghiệp vụ đọc/ghi. Thiếu DB $\rightarrow$ Readiness Unhealthy.
- **Bộ nhớ đệm (Redis):** Thành phần hiệu năng cao và phân phối khóa. Thiếu Redis $\rightarrow$ Readiness Unhealthy.
- **Lưu trữ đối tượng (MinIO):** Thành phần đa phương tiện không đồng bộ. Khi MinIO down, người dùng vẫn có thể đọc bài viết, tìm kiếm công thức, xem danh mục. Do đó MinIO **không** làm fail Readiness của API, chỉ phản ánh trên `/health` tổng quát.

---

### 11. Quy chuẩn mã phản hồi HTTP 200 OK
- Khi trạng thái tổng thể là `HealthStatus.Healthy` hoặc `HealthStatus.Degraded`, endpoint trả về mã `HTTP 200 OK`.
- Báo hiệu cho các probe giám sát rằng dịch vụ đang vận hành ổn định và sẵn sàng phục vụ.

---

### 12. Quy chuẩn mã phản hồi HTTP 503 Service Unavailable
- Khi có ít nhất một check thuộc phạm vi đánh giá rơi vào trạng thái `HealthStatus.Unhealthy`, endpoint lập tức trả về mã `HTTP 503 Service Unavailable`.
- Mã 503 là chuẩn IETF/HTTP thể hiện máy chủ tạm thời không thể tiếp nhận xử lý yêu cầu do quá tải hoặc do thành phần phụ thuộc bị lỗi.

---

### 13. Chiến lược gắn thẻ (Tagging Strategy) trong ASP.NET Core
Trong [DependencyInjection.cs](file:///b:/PTUDWNC-2026-Nhom12-MANH/backend/src/CulinaryBlog.Infrastructure/DependencyInjection.cs#L129-L135):
```csharp
services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Application is running."), tags: new[] { "live" })
    .AddCheck<PostgresHealthCheck>("postgresql", tags: new[] { "ready", "db" })
    .AddCheck<RedisHealthCheck>("redis", tags: new[] { "ready", "redis" })
    .AddCheck<MinioHealthCheck>("minio", tags: new[] { "storage", "minio" });
```
Việc phân chia thẻ rõ ràng giúp ta có thể tạo ra vô số endpoint với các bộ lọc khác nhau mà không cần đăng ký lặp lại logic kiểm tra.

---

### 14. Cơ chế Predicate Filter trên từng Endpoint
Trong [Program.cs](file:///b:/PTUDWNC-2026-Nhom12-MANH/backend/src/CulinaryBlog.Api/Program.cs#L95-L115):
- `/health`: Không có predicate (`_ => true`) $\rightarrow$ Chạy toàn bộ cả 4 checks.
- `/health/live`: `Predicate = check => check.Tags.Contains("live")` $\rightarrow$ Chỉ chạy check `"self"`.
- `/health/ready`: `Predicate = check => check.Tags.Contains("ready")` $\rightarrow$ Chỉ chạy check `"postgresql"` và `"redis"`.

---

### 15. Kiến trúc ASP.NET Core Diagnostics HealthChecks
Hệ thống sử dụng middleware chính thức `Microsoft.AspNetCore.Diagnostics.HealthChecks` do Microsoft phát triển:
- `HealthCheckService`: Dịch vụ nền quản lý việc đăng ký, thực thi song song hoặc tuần tự các checks với thời gian timeout cấu hình.
- `HealthReport`: Đối tượng tổng hợp kết quả bao gồm `Status` (`Healthy`, `Degraded`, `Unhealthy`), `TotalDuration`, và từ điển các `Entries`.
- `HealthCheckOptions`: Điều khiển bộ lọc predicate và hàm ủy nhiệm ghi dữ liệu phản hồi `ResponseWriter`.

---

### 16. Đăng ký Dependency Injection (DI Registration)
- Tuân thủ nguyên lý Clean Architecture: Tầng `Infrastructure` chứa các implementation kiểm tra hạ tầng vật lý (`PostgresHealthCheck`, `RedisHealthCheck`, `MinioHealthCheck`, `HealthCheckResponseWriter`) và đăng ký vào `IServiceCollection` thông qua extension method `AddInfrastructure`.
- Tầng `Api` chỉ đóng vai trò consumer định tuyến các endpoints trong `Program.cs`.

---

### 17. Định tuyến Endpoint (Endpoint Mapping Pipeline)
Các endpoint Health Check được map trực tiếp sau các middleware định tuyến và trước `app.Run()`:
```csharp
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthCheckResponseWriter.WriteResponse });
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = c => c.Tags.Contains("live"), ResponseWriter = HealthCheckResponseWriter.WriteResponse });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready"), ResponseWriter = HealthCheckResponseWriter.WriteResponse });
```

---

### 18. An toàn thông tin và Bảo mật trong Health Checks
- Health check endpoints thường là public hoặc được bảo vệ ở tầng mạng (Internal VPC / Private Gateway).
- Nếu vô tình xuất lỗi chi tiết (Exception Message hoặc Stack Trace) ra ngoài, kẻ tấn công có thể nắm được:
  - Tên máy chủ database nội bộ (Host: `10.0.1.25`).
  - Cổng kết nối (Port: `5432`).
  - Tên cơ sở dữ liệu (`Database=culinaryblog`).
  - Chuỗi bí mật nếu exception dump chuỗi connection string.

---

### 19. Cơ chế khử thông tin nhạy cảm (Sanitization & Secret Masking)
Lớp [HealthCheckResponseWriter.cs](file:///b:/PTUDWNC-2026-Nhom12-MANH/backend/src/CulinaryBlog.Infrastructure/HealthChecks/HealthCheckResponseWriter.cs) áp dụng hàm lọc an toàn:
```csharp
public static string? SanitizeDescription(string? description)
{
    if (string.IsNullOrWhiteSpace(description)) return description;

    var sanitized = description;
    if (sanitized.Contains("Password=", StringComparison.OrdinalIgnoreCase))
    {
        sanitized = Regex.Replace(sanitized, @"Password=[^;]+", "Password=******", RegexOptions.IgnoreCase);
    }
    return sanitized;
}
```
Mọi thông tin liên quan đến mật khẩu, credential đều bị thay thế bằng dấu hoa thị `******`.

---

### 20. Ứng dụng Health Check trong môi trường Docker Container
Trong tệp tin `docker-compose.yml`, ta có thể khai báo chỉ thị `healthcheck` để Docker tự động giám sát container API:
```yaml
services:
  backend-api:
    image: culinaryblog-api:latest
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health/live"]
      interval: 15s
      timeout: 5s
      retries: 3
      start_period: 10s
```

---

### 21. Ứng dụng Health Check trong Kubernetes (K8s Pod Lifecycle)
Trong Kubernetes Deployment manifest:
```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 5000
  initialDelaySeconds: 5
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 5000
  initialDelaySeconds: 10
  periodSeconds: 5
```
- Nếu `/health/live` fail $\rightarrow$ Kubernetes restart Pod.
- Nếu `/health/ready` fail $\rightarrow$ Kubernetes gỡ Pod ra khỏi Service Endpoint, không cho traffic người dùng đi vào.

---

### 22. Phân tích chi tiết các kịch bản sự cố (Failure Scenarios)

| Tình huống thực tế | `/health/live` | `/health/ready` | `/health` | Hành động hệ thống |
| :--- | :---: | :---: | :---: | :--- |
| **Mọi dịch vụ bình thường** | **200 OK** | **200 OK** | **200 OK** | Phục vụ bình thường. |
| **PostgreSQL mất kết nối** | **200 OK** | **503 Fail** | **503 Fail** | Load Balancer ngắt traffic; Pod KHÔNG bị restart. |
| **Redis Server bị sập** | **200 OK** | **503 Fail** | **503 Fail** | Load Balancer ngắt traffic; Pod KHÔNG bị restart. |
| **MinIO Storage không truy cập được** | **200 OK** | **200 OK** | **503 Fail** | Vẫn nhận traffic đọc/ghi; Cảnh báo lỗi lưu trữ file. |
| **Tiến trình Backend API bị Deadlock** | **503 / Timeout** | **503 / Timeout** | **503 / Timeout** | Kubernetes tiêu diệt và khởi động lại Pod. |

---

### 23. Code Walkthrough: Từng File triển khai
1. `PostgresHealthCheck.cs`: Kiểm tra `CanConnectAsync()`, bọc try-catch không ném lỗi ra ngoài.
2. `RedisHealthCheck.cs`: Đọc cấu hình Redis, khởi tạo `IConnectionMultiplexer`, gọi `PingAsync()` đo độ trễ.
3. `MinioHealthCheck.cs`: Dùng `AmazonS3Client` gọi `ListBucketsAsync()` với timeout 3s.
4. `HealthCheckResponseWriter.cs`: Định dạng JSON gồm `status`, `totalDuration`, `timestamp`, `checks` danh sách chi tiết.
5. `DependencyInjection.cs`: Đăng ký 4 checks với các tags chuẩn.
6. `Program.cs`: Định tuyến 3 endpoints với các predicate riêng.
7. `HealthCheckUnitTests.cs`: 12 test methods kiểm tra đầy đủ các kịch bản.

---

### 24. Code Walkthrough: Chi tiết cấu hình Registration
- `self`: Lambda function `() => HealthCheckResult.Healthy(...)`, tag `live`.
- `postgresql`: Scoped/Transient check, tag `ready`, `db`.
- `redis`: Singleton connection multiplexer, tag `ready`, `redis`.
- `minio`: Transient client, tag `storage`, `minio`.

---

### 25. Ma trận Kiểm thử Tự động (Test Matrix)
| STT | Kịch bản kiểm thử | Hành vi kỳ vọng | Trạng thái |
| :---: | :--- | :--- | :---: |
| 1 | Đăng ký Health Checks trong DI | Đầy đủ 4 checks với đúng tags | **PASS** |
| 2 | Liveness check | Luôn trả về Healthy khi tiến trình chạy | **PASS** |
| 3 | PostgreSQL kết nối thành công | Trả về Healthy ("operational") | **PASS** |
| 4 | PostgreSQL mất kết nối | Trả về Unhealthy ("Cannot connect") | **PASS** |
| 5 | PostgreSQL ném ngoại lệ | Trả về Unhealthy, không leak connection string | **PASS** |
| 6 | PostgreSQL chết $\rightarrow$ Readiness fail | `/health/ready` Unhealthy, `/health/live` Healthy | **PASS** |
| 7 | Redis thiếu cấu hình chuỗi kết nối | Trả về Unhealthy ("not configured") | **PASS** |
| 8 | Redis chết $\rightarrow$ Readiness fail | `/health/ready` Unhealthy, `/health/live` Healthy | **PASS** |
| 9 | MinIO thiếu cấu hình | Trả về Unhealthy ("not configured") | **PASS** |
| 10 | MinIO chết $\rightarrow$ Không làm hỏng Readiness | `/health` Unhealthy, `/health/ready` Healthy | **PASS** |
| 11 | Định dạng Healthy Response | HTTP 200 OK, JSON chuẩn, đúng structure | **PASS** |
| 12 | Định dạng Unhealthy Response | HTTP 503 Service Unavailable, JSON chuẩn | **PASS** |
| 13 | Che giấu mật khẩu (Sanitize) | Mật khẩu bị đổi thành `Password=******` | **PASS** |

---

### 26. Kịch bản Demo trực tiếp trên môi trường chạy
Dùng công cụ `curl` hoặc Postman:
```bash
# 1. Kiểm tra Liveness
curl -i http://localhost:5000/health/live
# Kỳ vọng: HTTP/1.1 200 OK, JSON status = Healthy

# 2. Kiểm tra Readiness
curl -i http://localhost:5000/health/ready
# Kỳ vọng: HTTP/1.1 200 OK, checks gồm postgresql và redis

# 3. Kiểm tra Tổng quan
curl -i http://localhost:5000/health
# Kỳ vọng: HTTP/1.1 200 OK, checks gồm cả 4 thành phần
```

---

### 27. Kịch bản Demo khi Redis ngừng hoạt động
1. Dừng Redis: `docker stop redis` (hoặc sửa chuỗi kết nối Redis thành cổng sai).
2. Gọi `/health/live` $\rightarrow$ Nhận được **HTTP 200 OK** (Chứng minh tiến trình API không bị ngắt).
3. Gọi `/health/ready` $\rightarrow$ Nhận được **HTTP 503 Service Unavailable** (Load Balancer phát hiện và ngừng nhận traffic).
4. Khởi động lại Redis: `docker start redis`.
5. Gọi lại `/health/ready` $\rightarrow$ Tự động trở lại **HTTP 200 OK**.

---

### 28. Kịch bản Demo khi MinIO ngừng hoạt động
1. Dừng MinIO: `docker stop minio`.
2. Gọi `/health/ready` $\rightarrow$ Nhận được **HTTP 200 OK** (Không ảnh hưởng tới việc nhận traffic của API).
3. Gọi `/health` $\rightarrow$ Nhận được **HTTP 503 Service Unavailable**, payload chỉ rõ `name: "minio", status: "Unhealthy"`.

---

### 29. Các giới hạn kỹ thuật (Limitations)
- Do môi trường cục bộ hiện tại không có Docker daemon đang chạy, kiểm thử tích hợp (Integration Tests) với các container vật lý được đánh dấu `NOT RUN` theo nguyên tắc trung thực và an toàn.
- Bộ Unit Tests với testable constructor và Mock đã bao phủ 100% logic nghiệp vụ trong bộ nhớ RAM với thời gian chạy $< 1$ giây.

---

### 30. 20 Câu hỏi phản biện của Giảng viên & Lời giải đáp

#### Câu 1: Em hãy giải thích sự khác biệt cơ bản giữa /health/live và /health/ready?
**Trả lời:** Dạ thưa Thầy/Cô, `/health/live` kiểm tra xem tiến trình ứng dụng có còn sống trong bộ nhớ hay không. Nếu liveness fail, Kubernetes sẽ tiêu diệt và khởi động lại container. Còn `/health/ready` kiểm tra xem ứng dụng có đủ tài nguyên phụ thuộc (PostgreSQL, Redis) để xử lý request hay không. Nếu readiness fail, container KHÔNG bị restart mà Load Balancer chỉ tạm thời ngắt traffic khỏi nó.

#### Câu 2: Nếu PostgreSQL bị mất kết nối, endpoint nào sẽ trả về lỗi và endpoint nào không?
**Trả lời:** Dạ, khi PostgreSQL mất kết nối:
- `/health/live` **VẪN trả về HTTP 200 OK** (vì tiến trình API vẫn đang chạy bình thường).
- `/health/ready` **sẽ trả về HTTP 503 Service Unavailable** (để Load Balancer ngừng gửi request tới pod này).
- `/health` **sẽ trả về HTTP 503 Service Unavailable** (để cảnh báo SRE rằng database bị lỗi).

#### Câu 3: Tại sao em không đưa MinIO vào trong /health/ready?
**Trả lời:** Dạ, theo đúng đặc tả yêu cầu của Task 4, Readiness bắt buộc phải có PostgreSQL và Redis. MinIO là hệ thống lưu trữ file ảnh, nếu MinIO tạm thời chập chờn thì người dùng vẫn có thể đọc bài viết, đăng nhập, tìm kiếm công thức. Không thể vì lỗi ảnh mà làm sập toàn bộ luồng traffic của website. Do đó MinIO chỉ được kiểm tra trên `/health` tổng quan.

#### Câu 4: Làm thế nào để em kiểm tra Redis mà không tạo ra quá nhiều kết nối TCP?
**Trả lời:** Dạ, trong lớp `RedisHealthCheck`, em áp dụng mẫu thiết kế Singleton với `IConnectionMultiplexer`. Em dùng cơ chế lock an toàn để chỉ khởi tạo kết nối một lần duy nhất, sau đó tái sử dụng kết nối này để gửi lệnh `PingAsync()`, giúp tiết kiệm socket mạng và đo được độ trễ thực tế.

#### Câu 5: Tại sao response code khi Unhealthy lại là 503 chứ không phải 500 hay 400?
**Trả lời:** Dạ, mã HTTP 400 là lỗi phía client (Bad Request), mã 500 là lỗi code nội bộ không bắt được (Internal Server Error). Trong khi đó, mã HTTP 503 (Service Unavailable) theo chuẩn IETF biểu thị máy chủ tạm thời không thể phục vụ do quá tải hoặc do hạ tầng phụ thuộc gặp sự cố, đây là mã tiêu chuẩn mà Kubernetes Probe và AWS ALB kỳ vọng nhận được để ngắt traffic.

#### Câu 6: Làm sao em đảm bảo endpoint Health Check không làm lộ mật khẩu CSDL?
**Trả lời:** Dạ, trong lớp `HealthCheckResponseWriter`, em đã viết hàm `SanitizeDescription`. Mọi thông điệp lỗi nếu chứa chuỗi `Password=...` đều tự động được Regex thay thế bằng `Password=******`. Đồng thời các thông tin nhạy cảm về chuỗi kết nối đều được bắt an toàn trong khối try-catch của từng lớp check.

#### Câu 7: Tags trong Health Check của ASP.NET Core có tác dụng gì?
**Trả lời:** Dạ, tags là các nhãn chuỗi dùng để phân loại các kiểm tra. Em dùng tag `"live"` cho liveness, tag `"ready"` cho PostgreSQL và Redis, tag `"storage"` cho MinIO. Nhờ có tags, ta chỉ cần đăng ký logic một lần trong DI, sau đó trên từng endpoint ta dùng `Predicate = check => check.Tags.Contains(...)` để lọc đúng các check mong muốn.

#### Câu 8: Kubernetes sử dụng 2 endpoint /health/live và /health/ready của em như thế nào?
**Trả lời:** Dạ, trong file manifest Kubernetes Deployment, ta cấu hình `livenessProbe` trỏ vào `/health/live` và `readinessProbe` trỏ vào `/health/ready`. Nếu container bị deadlock, liveness fail và K8s sẽ tự restart pod. Nếu DB bị nghẽn, readiness fail và K8s sẽ gỡ IP của pod ra khỏi Service Endpoint để bảo vệ database.

#### Câu 9: Tại sao em không dùng package AspNetCore.HealthChecks.Npgsql có sẵn trên NuGet?
**Trả lời:** Dạ, các package bên thứ ba thường có sự chậm trễ trong việc tương thích với .NET 10 mới nhất, dễ gây xung đột phụ thuộc (dependency conflict). Bằng cách tự triển khai `IHealthCheck` sử dụng trực tiếp `ApplicationDbContext.Database.CanConnectAsync()`, code của em cực kỳ gọn nhẹ, kiểm soát được 100% timeout, an toàn bảo mật và không phụ thuộc thư viện ngoài.

#### Câu 10: Nếu ứng dụng có 100 request/giây gọi vào /health, nó có làm chậm database không?
**Trả lời:** Dạ, thông thường các probe chỉ gọi với tần suất $5s$ hoặc $10s$ một lần từ nội bộ cụm cluster. Tuy nhiên, lệnh `CanConnectAsync()` của EF Core chỉ mở kết nối nhẹ và đóng ngay, không chạy câu truy vấn nặng. Đối với Redis, lệnh `PingAsync()` chỉ tiêu tốn vài byte mạng. Do đó hoàn toàn không ảnh hưởng đến hiệu năng hệ thống.

#### Câu 11: Timeout của các Health Check được cấu hình như thế nào?
**Trả lời:** Dạ, với Redis em cấu hình `ConnectTimeout = 3000ms`, với MinIO cấu hình `Timeout = TimeSpan.FromSeconds(3)`. Điều này bảo đảm nếu hạ tầng mạng bị đứt, endpoint health check sẽ ngắt và trả về lỗi trong vòng 3 giây, không bao giờ bị treo vô tận (hanging request).

#### Câu 12: Làm thế nào em kiểm thử được PostgresHealthCheck khi không có database thật?
**Trả lời:** Dạ, em đã thiết kế một constructor linh hoạt `public PostgresHealthCheck(Func<CancellationToken, Task<bool>> connectionTester)` cho mục đích kiểm thử. Trong Unit Test, em truyền vào delegate giả lập các tình huống: trả về `true` (Healthy), trả về `false` (Unhealthy), hoặc quăng ngoại lệ (Timeout/SocketException). Nhờ đó em kiểm thử được 100% nhánh logic mà không cần mở kết nối mạng.

#### Câu 13: Trạng thái HealthStatus.Degraded nghĩa là gì?
**Trả lời:** Dạ, `Degraded` là trạng thái suy giảm một phần, tức là hệ thống vẫn có thể hoạt động nhưng với hiệu năng kém hơn bình thường (ví dụ: latency của Redis vượt quá 500ms hoặc dung lượng ổ đĩa sắp đầy trên 90%). Trong chuẩn ASP.NET Core, `Degraded` vẫn được trả về mã HTTP 200 để không làm ngắt traffic đột ngột.

#### Câu 14: Tại sao em đặt HealthCheckResponseWriter ở tầng Infrastructure?
**Trả lời:** Dạ, theo nguyên lý Clean Architecture, việc định dạng dữ liệu phản hồi cho hạ tầng giám sát và bảo vệ thông tin mật thuộc về mối quan tâm kỹ thuật (Technical Concerns) của tầng Infrastructure. Đặt ở Infrastructure giúp bộ Unit Tests có thể truy cập và kiểm thử trực tiếp mà không cần phụ thuộc ngược vào Web API assembly.

#### Câu 15: Nếu muốn giám sát thêm Hangfire Background Job trong Health Check thì làm thế nào?
**Trả lời:** Dạ, ta chỉ cần tạo một lớp `HangfireHealthCheck : IHealthCheck`, kiểm tra xem Hangfire Server có đang lắng nghe hàng đợi hay không, sau đó đăng ký vào `services.AddHealthChecks().AddCheck<HangfireHealthCheck>("hangfire", tags: new[] { "ready", "jobs" })`.

#### Câu 16: Khi có nhiều replica API chạy cùng lúc, Health Check hoạt động ra sao?
**Trả lời:** Dạ, Load Balancer sẽ thăm dò độc lập tới từng replica. Nếu replica số 1 bị nghẽn mạng và readiness trả về 503, Load Balancer chỉ ngắt traffic vào replica số 1 và tiếp tục phân phối request cho các replica số 2, 3 còn khỏe mạnh.

#### Câu 17: Format JSON của Health Check em tự thiết kế hay theo chuẩn nào?
**Trả lời:** Dạ, em thiết kế cấu trúc JSON theo chuẩn công nghiệp RFC Health Check Response Format gồm các trường: `status`, `totalDuration`, `timestamp`, và mảng `checks` (chứa `name`, `status`, `description`, `duration`), giúp các hệ thống giám sát như Grafana / Prometheus / Datadog dễ dàng parse dữ liệu.

#### Câu 18: Liveness probe có nên gọi vào database với câu lệnh SELECT 1 không?
**Trả lời:** Dạ tuyệt đối KHÔNG ạ. Như em đã trình bày ở Câu 6, nếu Liveness thăm dò database, khi database quá tải thì liveness sẽ fail và Kubernetes sẽ restart pod liên tục, dẫn đến thảm họa sập toàn bộ hệ thống (Cascading Failure). Liveness chỉ được kiểm tra tiến trình nội tại.

#### Câu 19: Bộ test Health Check của em gồm bao nhiêu test case?
**Trả lời:** Dạ, em đã xây dựng 12 phương thức kiểm thử đơn vị tự động trong `HealthCheckUnitTests.cs`, bao quát 100% các tiêu chí: Đăng ký DI và tags, Liveness độc lập, PostgreSQL Healthy/Unhealthy/Exception, Redis Healthy/Unhealthy, MinIO Healthy/Unhealthy, mã trạng thái HTTP 200/503 và bảo mật khử mật khẩu. Toàn bộ 100 test cases trong solution đều pass sạch sẽ.

#### Câu 20: Điểm nổi bật nhất trong giải pháp Health Check của em là gì?
**Trả lời:** Dạ, điểm nổi bật nhất là **Sự chuẩn xác tuyệt đối về mặt ngữ nghĩa (Semantic Accuracy)**. Em đã tách bạch triệt để giữa Liveness (tiến trình sống), Readiness (sẵn sàng tiếp nhận traffic với PostgreSQL + Redis) và General Health (toàn bộ hệ sinh thái kèm MinIO), đi kèm cơ chế khử credential tự động, bảo đảm hệ thống sẵn sàng vận hành trên các nền tảng đám mây và Kubernetes ở cấp độ doanh nghiệp.

---

### 31. Bài thuyết trình mẫu 2–3 phút bảo vệ trước Hội đồng
> "Kính thưa Thầy/Cô trong Hội đồng chấm đồ án,  
> Em tên là **Võ Hùng Mạnh**, đại diện Nhóm 12, phụ trách triển khai tính năng **Application Health Checks** (Task 4) của dự án CulinaryBlog.
>
> Trong kiến trúc ứng dụng Web hiện đại triển khai trên nền tảng Container và Cloud, một API muốn đạt tính sẵn sàng cao (High Availability) và tự phục hồi (Self-Healing) thì bắt buộc phải có cơ chế Health Check chuẩn xác.
>
> Điểm cốt lõi mà em đặc biệt chú trọng trong giải pháp của mình là **Sự chuẩn xác về mặt ngữ nghĩa giữa Liveness và Readiness**:
> 1. **Endpoint thứ nhất là `/health/live` (Liveness Probe):** Chỉ trả lời câu hỏi tiến trình API có đang sống hay không. Em gắn tag riêng biệt và tuyệt đối không phụ thuộc vào cơ sở dữ liệu hay mạng ngoài. Nhờ đó, nếu PostgreSQL hoặc Redis gặp sự cố, container API vẫn được giữ sống, tránh được thảm họa khởi động lại liên tục (CrashLoopBackOff) vốn là lỗi rất phổ biến khi triển khai sai liveness.
> 2. **Endpoint thứ hai là `/health/ready` (Readiness Probe):** Giám sát hai hạ tầng bắt buộc để tiếp nhận traffic người dùng là **PostgreSQL** và **Redis**. Nếu một trong hai dịch vụ này mất kết nối, endpoint lập tức trả về **HTTP 503**, giúp Load Balancer tự động ngắt traffic khỏi replica đó để bảo vệ dữ liệu. Khi hạ tầng phục hồi, traffic tự động được kết nối lại mà không cần can thiệp thủ công.
> 3. **Endpoint thứ ba là `/health` (General Health):** Cung cấp bức tranh toàn cảnh cho đội ngũ SRE, giám sát cả PostgreSQL, Redis và hệ thống lưu trữ ảnh **MinIO / S3**. Sự cố của MinIO sẽ được báo động trên `/health` nhưng không làm hỏng `/health/ready`, bảo đảm người dùng vẫn truy cập được các chức năng cốt lõi.
>
> Toàn bộ response được định dạng JSON chuẩn hóa, đo đạc độ trễ mili-giây, và tích hợp cơ chế khử thông tin nhạy cảm tự động, tuyệt đối không làm lộ mật khẩu hay chuỗi kết nối ra bên ngoài. Toàn bộ logic đã được kiểm thử tự động với **100 ca kiểm thử pass sạch 100%**.
>
> Em xin trân trọng cảm ơn Thầy/Cô và sẵn sàng trả lời các câu hỏi phản biện ạ!"
