# Observability

## Người thực hiện
Võ Hùng Mạnh (Nhóm 12)

## Branch
`feat/vohungmanh-observability`

## Mục tiêu
Thiết lập hệ thống Giám sát & Đo lường toàn diện (Observability) cho dự án CulinaryBlog theo chuẩn Production-Ready:
1. **Structured Logging (Serilog)**: Ghi log có cấu trúc đầy đủ các trường ngữ cảnh (`CorrelationId`, `RequestPath`, `RequestMethod`, `StatusCode`, `ElapsedMs`, `UserId`, `TraceId`, `SpanId`).
2. **Correlation ID**: Quản lý định danh tương quan request từ client đến server, xuyên suốt async flow và phản hồi lại qua header `X-Correlation-ID`.
3. **Slow Request Warning**: Tự động phát hiện và ghi log cảnh báo mức `Warning` khi request HTTP hoặc MediatR command/query vượt ngưỡng 500ms.
4. **MediatR LoggingBehavior**: Pipeline behavior ghi log metadata, đo lường thời gian thực thi, bắt exception và bảo vệ an toàn dữ liệu nhạy cảm (không log password, token, PII).
5. **OpenTelemetry Tracing**: Tích hợp ASP.NET Core instrumentation, HttpClient instrumentation, EF Core database instrumentation và gRPC OTLP exporter (`http://localhost:4317`).
6. **OpenTelemetry Metrics & Business Metrics**: Theo dõi lưu lượng HTTP (`http.requests.total`, `http.requests.errors`, `http.request.duration.ms`) và đo lường nghiệp vụ chuyên biệt (`recipes.created`, `recipes.published`).

---

## Architecture

```
Client HTTP Request
  │ (Header: X-Correlation-ID [tùy chọn])
  ▼
┌─────────────────────────────────────────────────────────────┐
│ 1. CorrelationId Middleware                                │
│    - Validate / Sinh GUID an toàn                          │
│    - Gắn context.Items["CorrelationId"]                     │
│    - Gắn header response X-Correlation-ID                  │
│    - Gắn tag vào Activity.Current ("correlation.id")        │
│    - Push CorrelationId, TraceId, SpanId vào LogContext     │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. Serilog Request Logging                                 │
│    - Template: HTTP {RequestMethod} {RequestPath} responded │
│    - EnrichDiagnosticContext: RequestPath, Method, Status,  │
│      ElapsedMs, UserId, CorrelationId, TraceId, SpanId      │
│    - Phân cấp Log: >500ms -> Warning (Slow Request),       │
│      >=400 -> Warning, >=500 -> Error, còn lại -> Info      │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. HTTP Metrics Middleware                                  │
│    - Đo stopwatch tổng thời gian xử lý request             │
│    - Ghi nhận http.requests.total, errors, duration.ms     │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. MediatR Pipeline: LoggingBehavior                        │
│    - Log RequestName bắt đầu                                │
│    - Đo stopwatch thực thi handler                          │
│    - Cảnh báo >500ms (Slow MediatR Request)                │
│    - Log Error và rethrow nếu có Exception                  │
│    - Tuyệt đối không log body chứa password, token, PII    │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. Application Core / Services / Handlers                   │
│    - RecipeWriteService (Create / Publish)                  │
│    - Ghi Business Metrics sau khi SaveChangesAsync success: │
│      * recipes.created                                      │
│      * recipes.published                                    │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│ 6. Observability Sinks & Exporters                          │
│    - Serilog Sinks: Console, Rolling File, Seq (5341)       │
│    - OpenTelemetry OTLP Exporter (gRPC 4317)                │
│    - Traces: ASP.NET Core, HttpClient, EF Core, Custom      │
│    - Metrics: HTTP Counters/Histogram, Business Counters    │
└─────────────────────────────────────────────────────────────┘
```

---

## Structured Logging
Hệ thống sử dụng **Serilog** làm logging engine chủ đạo:
- Cấu hình qua `appsettings.json` và code khởi tạo trong `Program.cs`.
- Mọi HTTP request được ghi dưới dạng key-value structured properties, cho phép truy vấn trực tiếp trên Seq hoặc ELK/OpenSearch mà không cần parse regex chuỗi text.
- Các thuộc tính structured cốt lõi:
  - `CorrelationId`: Định danh luồng xử lý tương quan.
  - `RequestPath`: Đường dẫn endpoint (vd: `/api/v1/recipes`).
  - `RequestMethod`: Phương thức HTTP (vd: `GET`, `POST`, `PATCH`).
  - `StatusCode`: Mã trạng thái HTTP phản hồi (vd: `200`, `201`, `400`, `500`).
  - `Elapsed`: Thời gian xử lý tính bằng mili-giây.
  - `UserId`: Mã định danh người dùng nếu đã đăng nhập.
  - `TraceId` & `SpanId`: Liên kết trực tiếp giữa log event và OpenTelemetry trace span.

---

## CorrelationId
- **Header**: `X-Correlation-ID`.
- **Cơ chế hoạt động**:
  1. Client gửi request kèm header `X-Correlation-ID` hợp lệ (độ dài $\le 64$ ký tự, chỉ gồm ký tự `[a-zA-Z0-9_\-]`): Tái sử dụng nguyên vẹn.
  2. Client không gửi header hoặc gửi chuỗi rác/chứa ký tự nhạy cảm/vượt độ dài: Middleware tự sinh một `Guid.NewGuid().ToString("D")`.
  3. Giá trị CorrelationId được đưa vào `HttpContext.Response.Headers["X-Correlation-ID"]`.
  4. Đưa vào `HttpContext.Items["CorrelationId"]`.
  5. Đẩy vào Serilog `LogContext.PushProperty("CorrelationId", correlationId)`, giúp mọi log phát sinh trong suốt async execution context đều tự động mang `CorrelationId`.
  6. Gắn tag `correlation.id` vào `Activity.Current` của OpenTelemetry.

---

## UserId
- Tuân thủ quy ước xác thực hiện tại của hệ thống: Đọc từ `ClaimsPrincipal` qua `ClaimTypes.NameIdentifier` hoặc fallback về claim `sub`.
- Khi người dùng đã đăng nhập (JWT Bearer Token hợp lệ): `UserId` được enrich tự động vào Serilog diagnostic context.
- Khi người dùng ẩn danh (Anonymous): `UserId` không được đưa vào context, hệ thống hoạt động an toàn và không gây crash hay NullReferenceException.

---

## Slow Request >500ms
- Ngưỡng thời gian xử lý chuẩn cho Slow Request là **500ms**.
- **Tại HTTP Request Logging**:
  Hàm delegate `options.GetLevel` kiểm tra thời gian thực thi `elapsed`:
  - Nếu `elapsed > 500ms`: Tự động nâng log level lên `LogEventLevel.Warning` kèm thông báo chi tiết thời gian xử lý.
  - Nếu $\le 500ms$: Giữ mức `LogEventLevel.Information`.
- **Tại MediatR LoggingBehavior**:
  Stopwatch đo thời gian xử lý của handler. Nếu `stopwatch.ElapsedMilliseconds > 500ms`, log cảnh báo `Warning: "Long-running MediatR request {RequestName} completed in {ElapsedMilliseconds} ms (> 500ms)"`.
- Tuyệt đối không sử dụng `Thread.Sleep` trong production code.

---

## MediatR LoggingBehavior
Lớp generic `LoggingBehavior<TRequest, TResponse>` kế thừa `IPipelineBehavior<TRequest, TResponse>`:
- Đặt tại: `CulinaryBlog.Application/Behaviors/LoggingBehavior.cs`.
- Đăng ký trong `CulinaryBlog.Application/DependencyInjection.cs` bọc ngoài các behavior khác.
- Chức năng:
  - Ghi log Information khi bắt đầu: `"Handling MediatR request {RequestName}"`.
  - Đo thời gian xử lý chính xác bằng `Stopwatch`.
  - Khi hoàn tất thành công: ghi log Information với `ElapsedMilliseconds` (hoặc Warning nếu $> 500ms$).
  - Khi gặp lỗi: bắt exception, ghi log Error kèm stack trace và `ElapsedMilliseconds`, sau đó rethrow để tầng trên xử lý.
  - **Bảo mật tuyệt đối**: Chỉ log metadata (`RequestName`, `ElapsedMilliseconds`), không serialize hay in body của `request` để tránh lộ mật khẩu, bearer token, hoặc PII.

---

## OpenTelemetry
Được cấu hình thông qua phương thức mở rộng `AddObservability(this IServiceCollection services, IConfiguration configuration)` trong `CulinaryBlog.Infrastructure/Observability/OpenTelemetryExtensions.cs`:
- Khởi tạo `ResourceBuilder` với `ServiceName = "CulinaryBlog.Api"`.
- Đăng ký gRPC OTLP Exporter trỏ đến endpoint cấu hình (`http://localhost:4317`).
- Hỗ trợ graceful fallback: Nếu OTLP Collector không hoạt động, ứng dụng vẫn vận hành trơn tru và ghi log bình thường ra Console/File/Seq mà không bị crash.

---

## HTTP Tracing
- Sử dụng package `OpenTelemetry.Instrumentation.AspNetCore` và `OpenTelemetry.Instrumentation.Http`.
- Tự động bắt mọi HTTP incoming request, gán Trace ID, Span ID và đo lường latency của từng route.
- Tự động trace các outgoing HTTP request phát sinh bởi `HttpClient`.

---

## EF Core Tracing
- Sử dụng package `OpenTelemetry.Instrumentation.EntityFrameworkCore`.
- Tự động tạo child span cho mọi câu truy vấn SQL gửi đến cơ sở dữ liệu PostgreSQL.
- Thể hiện chi tiết command text, thời gian thực thi query, và thông tin connection trong trace waterfall.

---

## TraceId
- Khi request đi qua ASP.NET Core pipeline, OpenTelemetry khởi tạo một `Activity.Current`.
- `CorrelationIdMiddleware` đọc `Activity.Current.TraceId` và `Activity.Current.SpanId`, sau đó đẩy vào Serilog `LogContext`.
- Nhờ đó, mỗi dòng log sinh ra trong phạm vi một request đều mang cùng một `TraceId`, cho phép lập trình viên copy `TraceId` từ log file hoặc Seq để tìm kiếm trực tiếp trace tương ứng trên Jaeger / Grafana Tempo.
- **Phân biệt**:
  - `CorrelationId`: Chuỗi định danh logic cấp ứng dụng/nghiệp vụ (có thể do client truyền vào để liên kết luồng).
  - `TraceId`: Chuỗi hex 128-bit do W3C TraceContext quy định, định danh một distributed trace xuyên suốt các microservices.
  - `SpanId`: Chuỗi hex 64-bit định danh một bước/hoạt động đơn lẻ trong trace.

---

## Metrics

### Request Count
- Được đo lường qua OpenTelemetry built-in metric và custom counter `http.requests.total`.
- Tăng 1 đơn vị mỗi khi một HTTP request được xử lý, gắn kèm các tags: `http.method`, `http.route`, `http.status_code`.

### Request Duration
- Được đo lường qua custom histogram `http.request.duration.ms` (đơn vị: ms) và ASP.NET Core request duration histogram.
- Phục vụ việc tính toán các phân vị thời gian phản hồi: p50, p90, p95, p99.

### Errors
- Được đo lường qua counter `http.requests.errors`.
- Tự động tăng khi mã trạng thái HTTP $\ge 400$ (bao gồm cả client errors 4xx và server errors 5xx).
- **Error Rate**: Được tính toán tại hệ thống giám sát downstream (Prometheus / Grafana) bằng công thức chuẩn:
  $$\text{Error Rate} = \frac{\text{rate}(http\_requests\_errors[5m])}{\text{rate}(http\_requests\_total[5m])} \times 100\%$$

---

## Business Metrics
Lớp tập trung `CulinaryBlogTelemetry` cung cấp các business metrics theo chuẩn OpenTelemetry:

### Recipe Created
- Metric name: `recipes.created`
- Kiểu: `Counter<long>`
- Đơn vị: `{recipes}`
- Vị trí kích hoạt: Tầng `RecipeWriteService.CreateAsync`, **CHỈ** được gọi sau khi `_unitOfWork.SaveChangesAsync(cancellationToken)` hoàn tất thành công.
- Không tăng nếu transaction database thất bại.

### Recipe Published
- Metric name: `recipes.published`
- Kiểu: `Counter<long>`
- Đơn vị: `{recipes}`
- Vị trí kích hoạt: Tầng `RecipeWriteService.PublishAsync` (hoặc khi `CreateAsync` với trạng thái `RecipeStatus.Published`), **CHỈ** được gọi sau khi `_unitOfWork.SaveChangesAsync(cancellationToken)` hoàn tất thành công.
- Không tăng nếu transaction database thất bại.

---

## Serilog
- Cấu hình qua `appsettings.json` trong `backend/src/CulinaryBlog.Api/appsettings.json`:
  - `WriteTo`: Console, Rolling File (`logs/culinaryblog-.log`), Seq (`http://localhost:5341`).
  - `Enrich`: `FromLogContext`, `WithMachineName`, `WithThreadId`.
- Tách biệt rõ log level theo namespace:
  - Mặc định: `Information`
  - `Microsoft`: `Warning`
  - `Microsoft.EntityFrameworkCore`: `Warning`

---

## Seq
- Tích hợp sink `Serilog.Sinks.Seq` gửi log trực tiếp qua HTTP endpoint `http://localhost:5341`.
- Hỗ trợ xem log có cấu trúc trực quan, lọc theo `@Properties['CorrelationId']`, `RequestPath`, `StatusCode`, `Elapsed > 500`.

---

## OTLP
- Giao thức chuẩn OpenTelemetry Protocol (gRPC) trên cổng `4317`.
- Cấu hình trong `appsettings.json`:
  ```json
  "OpenTelemetry": {
    "ServiceName": "CulinaryBlog.Api",
    "Endpoint": "http://localhost:4317"
  }
  ```
- Nếu Collector không bật: Ứng dụng ghi nhận warning/lỗi nội bộ exporter mà không làm gián đoạn API của người dùng.

---

## Security
- **Nguyên tắc bảo vệ dữ liệu nhạy cảm**:
  - Tuyệt đối không log mật khẩu (`Password`), token xác thực (`Authorization`, `Bearer`, `RefreshToken`), cookie phiên làm việc, chuỗi kết nối cơ sở dữ liệu (`ConnectionString`), hoặc access key MinIO/AWS S3.
  - `CorrelationIdMiddleware` kiểm tra và loại bỏ bất kỳ header nào chứa chuỗi có nguy cơ rò rỉ token hoặc chèn ký tự injection, thay thế bằng GUID chuẩn an toàn.
  - `LoggingBehavior` chỉ log `RequestName` và thời gian thực thi, không bao giờ serialize đối tượng request chứa dữ liệu người dùng.

---

## Files

| File | Vai trò |
| :--- | :--- |
| `backend/src/CulinaryBlog.Application/Common/Interfaces/ICulinaryBlogTelemetry.cs` | Interface định nghĩa abstraction cho Telemetry & Business Metrics trong tầng Application |
| `backend/src/CulinaryBlog.Application/Behaviors/LoggingBehavior.cs` | MediatR pipeline behavior đo thời gian thực thi, cảnh báo slow request (>500ms), log lỗi và bảo vệ dữ liệu nhạy cảm |
| `backend/src/CulinaryBlog.Application/DependencyInjection.cs` | Đăng ký `LoggingBehavior` vào MediatR pipeline trước `ValidationBehavior` |
| `backend/src/CulinaryBlog.Application/Interfaces/IRecipeWriteService.cs` | Bổ sung phương thức `PublishAsync` |
| `backend/src/CulinaryBlog.Application/Features/Recipes/RecipeWriteService.cs` | Tích hợp gọi Business Metrics (`recipes.created`, `recipes.published`) khi tạo và xuất bản công thức thành công |
| `backend/src/CulinaryBlog.Infrastructure/Observability/CulinaryBlogTelemetry.cs` | Lớp tập trung quản lý `ActivitySource`, `Meter`, `Counters` và `Histograms` chuẩn OpenTelemetry |
| `backend/src/CulinaryBlog.Infrastructure/Observability/OpenTelemetryExtensions.cs` | Đăng ký OpenTelemetry Tracing, Metrics, OTLP Exporter, và `ICulinaryBlogTelemetry` singleton |
| `backend/src/CulinaryBlog.Api/Middleware/CorrelationIdMiddleware.cs` | Middleware xử lý đọc/sinh Correlation ID, gắn Response Header, gán Tag Activity, và đẩy vào Serilog LogContext |
| `backend/src/CulinaryBlog.Api/Program.cs` | Thiết lập pipeline CorrelationId, Serilog structured request logging, cảnh báo slow request (>500ms), và HTTP metrics middleware |
| `backend/tests/CulinaryBlog.UnitTests/Application/Behaviors/LoggingBehaviorTests.cs` | Unit tests kiểm tra `LoggingBehavior` (log request name, cảnh báo >500ms, không cảnh báo khi nhanh, log lỗi, không rò rỉ secret) |
| `backend/tests/CulinaryBlog.UnitTests/Infrastructure/Observability/ObservabilityUnitTests.cs` | 21+ unit tests kiểm tra toàn diện CorrelationId, UserId extraction, slow request evaluation, OpenTelemetry registration, telemetry metrics, và business metrics |
| `docs/README_OBSERVABILITY.md` | Tài liệu kiến trúc và hướng dẫn vận hành Observability |
| `docs/VO_HUNG_MANH_OBSERVABILITY_COMPLAN.md` | Tài liệu báo cáo, giải thích lý thuyết, code walkthrough, demo và phản biện bảo vệ đồ án |

---

## Configuration

Trong `backend/src/CulinaryBlog.Api/appsettings.json`:
```json
{
  "OpenTelemetry": {
    "ServiceName": "CulinaryBlog.Api",
    "Endpoint": "http://localhost:4317"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/culinaryblog-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://localhost:5341"
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ]
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

Chạy toàn bộ bộ kiểm thử đơn vị:
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj
```

---

## Test Results

- **Unit Tests**: `Passed: 115, Failed: 0, Skipped: 1, Total: 116`. (Thời gian: ~1 giây).
- **Observability Unit Tests Coverage**:
  1. `ResolveCorrelationId_GeneratesNewGuid_WhenHeaderNotProvided`: PASS.
  2. `ResolveCorrelationId_ReusesClientHeader_WhenValid`: PASS.
  3. `ResolveCorrelationId_RegeneratesSafeGuid_WhenHeaderInvalidOrTooLong`: PASS.
  4. `CorrelationIdMiddleware_SetsItem_AndResponseHeader_AndInvokesNext`: PASS.
  5. `CorrelationIdMiddleware_TagsActivity_WhenActivityCurrentExists`: PASS.
  6. `UserIdExtraction_FindsNameIdentifierOrSub_WhenAuthenticated`: PASS.
  7. `UserIdExtraction_FindsSub_WhenNameIdentifierMissing`: PASS.
  8. `UserIdExtraction_ReturnsNull_ForAnonymousUser_WithoutCrashing`: PASS.
  9. `LogLevelEvaluation_ReturnsWarning_WhenElapsedExceeds500ms`: PASS.
  10. `LogLevelEvaluation_ReturnsInformation_WhenElapsedIsUnder500ms`: PASS.
  11. `LogLevelEvaluation_ReturnsError_WhenStatusCodeIs500OrExceptionThrown`: PASS.
  12. `LogLevelEvaluation_ReturnsWarning_WhenStatusCodeIs404`: PASS.
  13. `AddObservability_RegistersTelemetryService_InServiceCollection`: PASS.
  14. `CulinaryBlogTelemetry_RecordHttpRequest_DoesNotThrow`: PASS.
  15. `RecipeWriteService_CreateAsync_CallsRecordRecipeCreated_OnSuccess`: PASS.
  16. `RecipeWriteService_CreateAsync_CallsBothCreatedAndPublished_WhenInitiallyPublished`: PASS.
  17. `RecipeWriteService_CreateAsync_DoesNotRecordMetrics_WhenUnitOfWorkFails`: PASS.
  18. `RecipeWriteService_PublishAsync_UpdatesStatus_AndRecordsMetric_OnSuccess`: PASS.
  19. `RecipeWriteService_PublishAsync_DoesNotRecordMetric_WhenUnitOfWorkFails`: PASS.
  20. `CorrelationId_Sanitization_RejectsTokensAndPasswordsInHeader`: PASS.
  21. `LoggingBehavior_LogsRequestNameAndDuration_OnSuccess`: PASS.
  22. `LoggingBehavior_LogsWarning_WhenExecutionExceeds500ms`: PASS.
  23. `LoggingBehavior_DoesNotLogWarning_WhenExecutionUnder500ms`: PASS.
  24. `LoggingBehavior_LogsErrorAndRethrows_WhenHandlerThrowsException`: PASS.
  25. `LoggingBehavior_DoesNotLogSensitiveData_SuchAsPasswordsOrTokens`: PASS.
- **Integration Tests (với External Collector / Seq)**: `NOT RUN / NOT VERIFIED` do môi trường hiện tại chưa khởi chạy container Docker OpenTelemetry Collector và Seq.

---

## Demo

### Demo A - Structured Log
1. Khởi động API:
   ```bash
   dotnet run --project backend/src/CulinaryBlog.Api/CulinaryBlog.API.csproj
   ```
2. Gửi request:
   ```bash
   curl -i http://localhost:5000/api/v1/recipes
   ```
3. Quan sát log Console/File:
   ```
   [15:30:12 INF] HTTP GET /api/v1/recipes responded 200 in 12.3456 ms
   ```
   Log có chứa đầy đủ structured properties:
   - `CorrelationId`: `c6f3...`
   - `RequestPath`: `/api/v1/recipes`
   - `RequestMethod`: `GET`
   - `StatusCode`: `200`
   - `Elapsed`: `12.3456`

### Demo B - Correlation ID
1. Gửi request kèm header tùy biến:
   ```bash
   curl -i -H "X-Correlation-ID: demo-manh-001" http://localhost:5000/api/v1/recipes
   ```
2. Phản hồi HTTP Header:
   ```http
   HTTP/1.1 200 OK
   X-Correlation-ID: demo-manh-001
   ```
3. Trong log của Serilog, thuộc tính `CorrelationId` mang đúng giá trị `demo-manh-001`.

### Demo C - Slow Request Warning (>500ms)
1. Trong unit test hoặc endpoint chạy nặng $> 500ms$:
2. Hệ thống tự động ghi nhận mức `Warning`:
   ```
   [15:30:15 WRN] HTTP GET /api/v1/recipes/heavy-query responded 200 in 521.4320 ms
   ```
   và tại MediatR pipeline:
   ```
   [15:30:15 WRN] Long-running MediatR request GetRecipeDetailQuery completed in 521 ms (> 500ms)
   ```

### Demo D - Distributed Tracing & TraceId
1. Gửi request tới API.
2. `CorrelationIdMiddleware` gắn `TraceId` và `SpanId` từ `Activity.Current` vào log context.
3. Khi kiểm tra log event, trường `TraceId` khớp chính xác với trace span được xuất ra OTLP exporter.

### Demo E - HTTP Metrics
1. Gửi một số request thành công (200) và thất bại (404/500).
2. Counter `http.requests.total` tăng theo từng request.
3. Counter `http.requests.errors` chỉ tăng khi response trả về lỗi $\ge 400$.
4. Histogram `http.request.duration.ms` ghi nhận thời gian thực thi.

### Demo F - Business Metrics (Recipe Created & Published)
1. Khi tạo công thức thành công: Counter `recipes.created` tăng 1.
2. Khi xuất bản công thức thành công: Counter `recipes.published` tăng 1.
3. Nếu cơ sở dữ liệu gặp lỗi và rollback: Cả hai counter đều không tăng.

---

## Limitations
1. **External Collectors**: Docker Compose của dự án hiện tại chỉ cấu hình PostgreSQL. OpenTelemetry Collector (OTLP `4317`) và Seq (`5341`) chưa được cấu hình làm container mặc định trong `docker-compose.yml`. Khi chạy ở môi trường không có Collector/Seq, Serilog vẫn lưu log ra Console/File bình thường và OpenTelemetry exporter bỏ qua các gói tin gRPC không gửi được.
2. **Prometheus / Grafana Dashboards**: Việc dựng dashboard hiển thị đồ thị Prometheus/Grafana phụ thuộc vào hạ tầng triển khai Kubernetes/Docker bên ngoài phạm vi mã nguồn ứng dụng backend.
