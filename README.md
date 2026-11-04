# TASK 4 (BỮA 4) – HỆ THỐNG QUAN SÁT & GHI LOG CÓ CẤU TRÚC (OBSERVABILITY & STRUCTURED LOGGING)

**Sinh viên:** Võ Hùng Mạnh
**MSSV:** 2312687
**Nhóm:** 12
**Nhánh:** `feat/vohungmanh-observability`

---

### 1. Mục tiêu Task 4 liên quan đến nhánh
Trong khuôn khổ Bữa 4 (Task 4), ứng dụng cần thiết lập nền tảng quan sát toàn diện (Observability Pillar) gồm 3 trụ cột: Ghi log có cấu trúc (Structured Logging), Theo dõi vết phân tán (Distributed Tracing) và Thu thập chỉ số hiệu năng (Custom Metrics). Hệ thống phải đảm bảo khả năng truy vết xuyên suốt mọi HTTP request giữa client và backend thông qua mã định danh tương quan `X-Correlation-ID`.

### 2. Công việc cá nhân thực tế
- Xây dựng middleware `CorrelationIdMiddleware` tiếp nhận hoặc tự động sinh mã định danh `X-Correlation-ID`, tích hợp sâu vào HttpContext, Response Headers, Serilog LogContext và OpenTelemetry Activity.
- Thiết kế interface `ICulinaryBlogTelemetry` và hiện thực `CulinaryBlogTelemetry` quản lý tập trung `ActivitySource` và `Meter` chuẩn OpenTelemetry v1.0.
- Định nghĩa các chỉ số đo lường tùy biến (Custom Metrics): đếm số lượng công thức được tạo/xuất bản, đếm tổng số HTTP requests, tỷ lệ lỗi, và biểu đồ phân phối độ trễ `http.request.duration.ms` (Histogram).
- Triển khai MediatR Pipeline Behavior `LoggingBehavior<TRequest, TResponse>` tự động đo lường thời gian xử lý và ghi log cảnh báo khi các lệnh Command/Query xử lý chậm.
- Tích hợp Serilog Structured Logging xuất log JSON có cấu trúc trong `Program.cs`.
- Viết 433 dòng mã kiểm thử trong `ObservabilityUnitTests.cs` và 182 dòng trong `LoggingBehaviorTests.cs` kiểm chứng toàn bộ logic tracing, metrics và logging behavior.

### 3. Chức năng đã hoàn thành
- **Truy vết đầu cuối với Correlation ID:**
  - Tự động nhận diện header `X-Correlation-ID` từ client (kiểm tra regex an toàn, tối đa 64 ký tự).
  - Tự động phát sinh UUID v4 nếu client không gửi header.
  - Gắn `CorrelationId` vào Serilog Diagnostic Context để mọi dòng log sinh ra trong suốt vòng đời của request đều mang cùng một mã truy vết.
  - Trả về header `X-Correlation-ID` trong response để client dễ dàng tra cứu khi báo cáo sự cố.
- **Distributed Tracing (OpenTelemetry):**
  - Khởi tạo `ActivitySource("CulinaryBlog.Api", "1.0.0")`.
  - Tự động liên kết `TraceId` và `SpanId` với `CorrelationId`.
  - Tạo các span nghiệp vụ theo dõi chi tiết trong `RecipeWriteService` và các thao tác I/O.
- **Thu thập chỉ số hiệu năng (Metrics & Instrumentation):**
  - Counter `recipes.created`: Số công thức mới được tạo thành công.
  - Counter `recipes.published`: Số công thức được chuyển sang trạng thái Published.
  - Counter `http.requests.total` & `http.requests.errors`: Đo lường lưu lượng và tỷ lệ lỗi hệ thống.
  - Histogram `http.request.duration.ms`: Phân tích phân phối thời gian phản hồi API.
- **Tự động đo thời gian MediatR (LoggingBehavior):**
  - Ghi nhận `Handling {RequestName}` khi bắt đầu và `Handled {RequestName} in {ElapsedMilliseconds} ms` khi kết thúc.
  - Cảnh báo log mức `Warning` khi thời gian xử lý vượt quá ngưỡng 500ms.

### 4. Cách xử lý và luồng hoạt động
```text
HTTP Request (Client kèm header X-Correlation-ID hoặc không)
        │
        ▼
CorrelationIdMiddleware
  ├── Validate hoặc sinh mới CorrelationId (GUID)
  ├── Đưa CorrelationId vào HttpContext.Items & Response Headers
  ├── Đẩy CorrelationId vào Serilog LogContext
  └── Khởi tạo / cập nhật OpenTelemetry Activity (TraceId / SpanId)
        │
        ▼
MediatR Pipeline (LoggingBehavior)
  ├── Bắt đầu đo Stopwatch
  ├── Ghi log Structured: Handling Command/Query
  ├── Thực thi Handler logic nghiệp vụ (Record Metrics, Activity Spans)
  ├── Dừng Stopwatch & Ghi nhận Histogram http.request.duration.ms
  └── Ghi log Handled (Warning nếu > 500ms)
        │
        ▼
HTTP Response (Kèm header X-Correlation-ID về client)
```

### 5. Danh sách file mã nguồn quan trọng

| Đường dẫn file | Vai trò và chức năng chính |
| :--- | :--- |
| `backend/src/CulinaryBlog.Api/Middleware/CorrelationIdMiddleware.cs` | Middleware quản lý Correlation ID, validation chuỗi an toàn, gắn vào LogContext và Response. |
| `backend/src/CulinaryBlog.Application/Common/Interfaces/ICulinaryBlogTelemetry.cs` | Interface quản lý các đồng hồ đo Counter và Histogram OpenTelemetry. |
| `backend/src/CulinaryBlog.Infrastructure/Observability/CulinaryBlogTelemetry.cs` | Khởi tạo `ActivitySource` và `Meter`, hiện thực bộ đếm và phân phối thời gian phản hồi. |
| `backend/src/CulinaryBlog.Infrastructure/Observability/OpenTelemetryExtensions.cs` | Cấu hình dịch vụ OpenTelemetry trong DI container. |
| `backend/src/CulinaryBlog.Application/Behaviors/LoggingBehavior.cs` | MediatR pipeline behavior tự động log và đo lường thời gian thực thi của requests. |
| `backend/src/CulinaryBlog.Api/Program.cs` | Cấu hình Serilog logger, enricher và đăng ký middleware quan sát. |
| `backend/tests/CulinaryBlog.UnitTests/Infrastructure/Observability/ObservabilityUnitTests.cs` | 433 dòng kiểm thử đơn vị kiểm tra CorrelationId, OpenTelemetry Spans và Metrics Counters. |
| `backend/tests/CulinaryBlog.UnitTests/Application/Behaviors/LoggingBehaviorTests.cs` | 182 dòng kiểm thử pipeline logging và ngưỡng cảnh báo thời gian xử lý. |
| `docs/README_OBSERVABILITY.md` | Tài liệu kỹ thuật chi tiết về kiến trúc quan sát, tracing và logging tiêu chuẩn. |
| `docs/VO_HUNG_MANH_OBSERVABILITY_COMPLAN.md` | Kế hoạch triển khai kỹ thuật và danh mục chỉ số đo lường Bữa 4. |

### 6. API, Database và Infrastructure liên quan
- **Framework Tracing:** `OpenTelemetry.Api` và `System.Diagnostics.ActivitySource`.
- **Framework Metrics:** `System.Diagnostics.Metrics.Meter`.
- **Thư viện Logging:** `Serilog.AspNetCore`, `Serilog.Enrichers.Environment`, `Serilog.Enrichers.Thread`, `Serilog.Formatting.Compact`.
- **HTTP Header:** `X-Correlation-ID`.

### 7. Hướng dẫn chạy

#### Khởi chạy Backend Web API
```bash
cd backend/src/CulinaryBlog.Api
dotnet run
```
Mọi request gửi tới API sẽ được Serilog ghi nhận trên Console dưới dạng log JSON có cấu trúc kèm trường `CorrelationId`.

### 8. Hướng dẫn kiểm thử

#### Kiểm thử Unit Test tự động
Chạy toàn bộ bài kiểm tra đơn vị về Observability:
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --filter "FullyQualifiedName~Observability|FullyQualifiedName~LoggingBehavior"
```

#### Kiểm thử thực tế bằng cURL
Gửi request kèm custom Correlation ID:
```bash
curl -i -H "X-Correlation-ID: test-trace-uuid-12345" http://localhost:5000/api/v1/recipes
```
Kiểm tra response header nhận về:
```http
X-Correlation-ID: test-trace-uuid-12345
```
Đồng thời kiểm tra log console backend, xác nhận log xuất hiện thuộc tính `"CorrelationId": "test-trace-uuid-12345"`.

### 9. Kết quả kiểm thử có bằng chứng
- **Kết quả Unit Test:** Toàn bộ test case trong `ObservabilityUnitTests` và `LoggingBehaviorTests` đều đạt `Passed!` (xác nhận CorrelationId tái sử dụng khi hợp lệ, tự sinh mới khi thiếu, khử ký tự nguy hiểm, và đo thời gian chính xác).
- **Log thực tế dạng cấu trúc:**
  ```json
  {"@t":"2026-10-05T08:15:20.123Z","@l":"Information","CorrelationId":"test-trace-uuid-12345","SourceContext":"CulinaryBlog.Application.Behaviors.LoggingBehavior","MessageTemplate":"Handled GetRecipeByIdQuery in 12 ms"}
  ```

### 10. Hạn chế và tồn đọng
- **Trạng thái thực tế:** Module hoàn thành production-ready cho tầng tracing, metrics in-process và structured logging.
- **Tồn đọng:** Chưa dựng cụm Jaeger / OTLP Collector chuyên dụng trên Docker Compose ở giai đoạn này; các telemetry spans và metrics hiện đang export ra Console và chuẩn bị sẵn exporter để kết nối OTLP khi triển khai production.

### 11. Liên kết tài liệu kỹ thuật chi tiết
- Kiến trúc hệ thống quan sát chi tiết: [docs/README_OBSERVABILITY.md](docs/README_OBSERVABILITY.md)
- Kế hoạch triển khai và thông số kỹ thuật: [docs/VO_HUNG_MANH_OBSERVABILITY_COMPLAN.md](docs/VO_HUNG_MANH_OBSERVABILITY_COMPLAN.md)

---

# Phát triển ứng dụng Web nâng cao

## Nhóm 12

### Thành viên
1. Võ Hùng Mạnh - 2312687 - 2312687@dlu.edu.vn - Nhóm trưởng
2. Lê Thị Ánh Nhung - 2312709 - 2312709@dlu.edu.vn
3. Phạm Nguyễn Ngọc Phước - 2312718 - 2312718@dlu.edu.vn
4. Nguyễn Văn Quốc - 2312729 - 2312729@dlu.edu.vn
