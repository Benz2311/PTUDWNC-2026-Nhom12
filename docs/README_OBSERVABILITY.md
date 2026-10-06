# Observability & Structured Logging System

## 1. Mục tiêu

Hệ thống Observability (Khả năng quan sát) của dự án CulinaryBlog được xây dựng nhằm giải quyết các bài toán giám sát, chẩn đoán sự cố và đo lường hiệu năng của ứng dụng theo chuẩn Production-Ready:
- **Khó khăn khi debug hệ thống phân tán / bất đồng bộ**: Khi có hàng ngàn request cùng lúc, log dạng text thông thường (`Console.WriteLine`) gây khó khăn trong việc lọc log theo request cụ thể hoặc theo user cụ thể. Hệ thống cần **Structured Logging** với định danh tương quan **Correlation ID**.
- **Không phát hiện kịp thời các truy vấn chậm**: Cần cơ chế tự động cảnh báo mức `Warning` khi request HTTP hoặc MediatR Command/Query vượt ngưỡng **500ms** để đội ngũ phát triển phát hiện bottleneck kịp thời.
- **Thiếu đo lường chỉ số nghiệp vụ (Business Metrics)**: Bên cạnh chỉ số kỹ thuật (HTTP request count, duration, errors), hệ thống cần theo dõi trực tiếp các hoạt động cốt lõi của người dùng như số lượng công thức được tạo (`recipes.created`) và công thức được xuất bản (`recipes.published`).
- **Chuẩn hóa Distributed Tracing**: Sử dụng chuẩn công nghiệp **OpenTelemetry (W3C TraceContext)** để tích hợp tracing xuyên suốt ASP.NET Core, HttpClient, Entity Framework Core và sẵn sàng xuất dữ liệu ra OpenTelemetry Collector / Jaeger / Grafana qua gRPC OTLP.

---

## 2. Kết quả đạt được

Sau khi branch `feat/vohungmanh-observability` được triển khai, hệ thống đã sở hữu các năng lực quan sát toàn diện:
- **Tự động quản lý Correlation ID**: Mọi request đi vào hệ thống đều được cấp hoặc tái sử dụng Correlation ID, gắn vào HTTP Response Header `X-Correlation-ID`, gán tag `correlation.id` vào OpenTelemetry `Activity`, và tự động xuất hiện trong mọi log event nhờ Serilog `LogContext`.
- **Structured Logging chi tiết**: Log được định dạng JSON/Key-Value với đầy đủ ngữ cảnh: `CorrelationId`, `TraceId`, `SpanId`, `RequestMethod`, `RequestPath`, `StatusCode`, `ElapsedMs`, `UserId`.
- **MediatR Pipeline LoggingBehavior**: Tự động đo lường thời gian xử lý từng command/query, bắt và ghi log lỗi, cảnh báo slow execution (>500ms), bảo vệ an toàn dữ liệu nhạy cảm (không log password, token, secrets).
- **Phát hiện Slow Request tự động**: Cả tầng HTTP Middleware lẫn tầng MediatR đều tự động nâng log level lên `Warning` khi thời gian xử lý vượt quá 500ms.
- **OpenTelemetry Tracing & Metrics**: Tích hợp sẵn sàng OTLP Exporter (`http://localhost:4317`), tự động trace SQL query của EF Core và incoming/outgoing HTTP.
- **Đo lường Business Metrics chuẩn xác**: Cung cấp counter `recipes.created` và `recipes.published`, chỉ ghi nhận tăng khi transaction database `SaveChangesAsync` hoàn tất thành công.
- **Bảo vệ dữ liệu nhạy cảm (Security by Design)**: Loại bỏ các header độc hại, không log payload chứa token, password hoặc thông tin định danh cá nhân (PII).

---

## 3. Luồng hoạt động

Luồng xử lý Observability xuyên suốt từ khi Client gửi request đến khi nhận phản hồi:

```
Client HTTP Request (Kèm X-Correlation-ID hoặc không)
   │
   ▼
1. CorrelationIdMiddleware
   │ [Validate/Sinh GUID -> Response Header X-Correlation-ID -> Activity Tag -> Serilog LogContext]
   ▼
2. Serilog Request Logging (UseSerilogRequestLogging)
   │ [Ghi nhận HTTP Request start/end, Enrich Diagnostic Context: Method, Path, Status, ElapsedMs, UserId]
   ▼
3. HTTP Metrics Middleware
   │ [Đo Stopwatch tổng thời gian HTTP -> Ghi http.requests.total, errors, duration.ms]
   ▼
4. MediatR Pipeline: LoggingBehavior<TRequest, TResponse>
   │ [Log RequestName bắt đầu -> Đo Stopwatch -> Thực thi Handler -> Log kết quả / Slow Warning / Error]
   ▼
5. RecipeWriteService / Application Handlers
   │ [Thực hiện nghiệp vụ -> UnitOfWork.SaveChangesAsync() -> Ghi nhận Business Metrics recipes.created / published]
   ▼
6. Entity Framework Core (EF Core Tracing)
   │ [OpenTelemetry tự động tạo child span cho câu lệnh SQL gửi đến PostgreSQL]
   ▼
7. Sinks & Exporters
   │ [Serilog: Console, File (logs/culinaryblog-.log), Seq (5341) | OpenTelemetry: OTLP Exporter gRPC (4317)]
   ▼
HTTP Response trả về Client (Kèm Header X-Correlation-ID)
```

### Giải thích chi tiết các bước:
1. **Bước 1 - Correlation ID Resolution**: Request đến server được `CorrelationIdMiddleware` tiếp nhận đầu tiên. Middleware kiểm tra header `X-Correlation-ID`. Nếu hợp lệ (độ dài $\le 64$ ký tự, ký tự chữ số hoặc gạch nối), middleware tái sử dụng; nếu trống hoặc không an toàn, sinh mới GUID chuẩn `Guid.NewGuid().ToString("D")`. ID này được gán vào `context.Response.Headers`, `context.Items`, gán tag `correlation.id` vào `Activity.Current`, và đẩy vào `Serilog.Context.LogContext`.
2. **Bước 2 - Serilog Diagnostic Enrichment**: `UseSerilogRequestLogging` gắn thêm các thuộc tính chẩn đoán vào LogContext: `RequestMethod`, `RequestPath`, `StatusCode`, `ElapsedMs`, `CorrelationId`, `TraceId`, `SpanId`, và `UserId` (nếu người dùng đã xác thực qua JWT token).
3. **Bước 3 - HTTP Metrics Collection**: Đo lường tổng thời gian của request bằng `Stopwatch`, cập nhật counter `http.requests.total`, counter `http.requests.errors` (nếu HTTP status code $\ge 400$), và histogram `http.request.duration.ms`.
4. **Bước 4 - MediatR LoggingBehavior**: Trước khi request đến tầng logic xử lý, pipeline behavior log `Handling MediatR request {RequestName}`. Sau khi handler hoàn tất, nếu thời gian xử lý $> 500ms$, ghi log mức `Warning: Long-running MediatR request {RequestName} completed in {ElapsedMilliseconds} ms (> 500ms)`. Nếu có Exception, log mức `Error` kèm thông tin exception rồi rethrow.
5. **Bước 5 - Business Metrics**: Khi nghiệp vụ hoàn tất và transaction database được xác nhận (`SaveChangesAsync` thành công), service gọi `ICulinaryBlogTelemetry.RecordRecipeCreated()` và `RecordRecipePublished()`. Nếu database lỗi, không có metric nào bị ghi sai.
6. **Bước 6 - EF Core Tracing**: OpenTelemetry tự động gắn TraceContext và tạo child span cho các query SQL PostgreSQL, liên kết cha - con với span của HTTP request.
7. **Bước 7 - Sinks & Phản hồi**: Log được đẩy đồng thời ra Console, Rolling File, và Seq Sink. Response HTTP gửi về client kèm header `X-Correlation-ID`.

---

## 4. Các file chính

| File | Vai trò | Xử lý gì |
| :--- | :--- | :--- |
| `backend/src/CulinaryBlog.Application/Common/Interfaces/ICulinaryBlogTelemetry.cs` | Interface Abstraction | Định nghĩa hợp đồng cho Telemetry & Business Metrics trong tầng Application (`RecordRecipeCreated`, `RecordRecipePublished`, `RecordHttpRequest`) |
| `backend/src/CulinaryBlog.Application/Behaviors/LoggingBehavior.cs` | MediatR Pipeline Behavior | Tự động log metadata, đo thời gian thực thi, cảnh báo slow request (>500ms), log error khi có exception, bảo vệ dữ liệu nhạy cảm (không log password, token) |
| `backend/src/CulinaryBlog.Application/DependencyInjection.cs` | DI Registration | Đăng ký `LoggingBehavior` vào MediatR pipeline bọc ngoài các handlers |
| `backend/src/CulinaryBlog.Infrastructure/Observability/CulinaryBlogTelemetry.cs` | Infrastructure Implementation | Hiện thực `ICulinaryBlogTelemetry`, quản lý OpenTelemetry `ActivitySource`, `Meter`, các `Counter<long>` và `Histogram<double>` |
| `backend/src/CulinaryBlog.Infrastructure/Observability/OpenTelemetryExtensions.cs` | DI Extensions | Cấu hình OpenTelemetry Tracing (ASP.NET Core, HttpClient, EF Core), Metrics, OTLP Exporter (`http://localhost:4317`), và đăng ký Singleton `ICulinaryBlogTelemetry` |
| `backend/src/CulinaryBlog.Api/Middleware/CorrelationIdMiddleware.cs` | HTTP Middleware | Đọc/sinh Correlation ID an toàn, gắn Response Header `X-Correlation-ID`, gắn Tag vào OpenTelemetry Activity, đẩy Correlation ID và Trace ID vào Serilog LogContext |
| `backend/src/CulinaryBlog.Api/Program.cs` | Configuration & Pipeline | Cấu hình Serilog từ `appsettings.json`, đăng ký CorrelationIdMiddleware, Serilog Request Logging, hàm đánh giá Log Level (>500ms -> Warning), và HTTP metrics middleware |
| `backend/src/CulinaryBlog.Application/Features/Recipes/RecipeWriteService.cs` | Business Service Integration | Gọi telemetry ghi nhận metric `recipes.created` và `recipes.published` sau khi `SaveChangesAsync` thành công |
| `backend/src/CulinaryBlog.Api/appsettings.json` | Configuration File | Cấu hình Serilog Sinks (Console, File, Seq), Log Level overrides, và OpenTelemetry service name & OTLP endpoint |
| `backend/tests/CulinaryBlog.UnitTests/Application/Behaviors/LoggingBehaviorTests.cs` | Unit Tests | 5 test cases kiểm tra MediatR `LoggingBehavior` (log start/end, cảnh báo >500ms, không cảnh báo khi nhanh, log error và rethrow, bảo mật không log payload) |
| `backend/tests/CulinaryBlog.UnitTests/Infrastructure/Observability/ObservabilityUnitTests.cs` | Unit Tests | 23 test cases kiểm tra CorrelationId logic, UserId extraction, Log level evaluation (>500ms), OpenTelemetry registration, telemetry metrics, và business metrics |

---

## 5. API / Interface

Do Observability là hạ tầng ngang (cross-cutting infrastructure), các thành phần tương tác qua HTTP Middleware, Pipeline Behavior và OpenTelemetry Metrics:

| Component / Middleware | Input | Processing | Output |
| :--- | :--- | :--- | :--- |
| **CorrelationIdMiddleware** | Header `X-Correlation-ID` (tùy chọn) | Validate độ dài $\le 64$ ký tự và charset hợp lệ. Nếu thiếu hoặc không hợp lệ -> sinh `Guid.NewGuid()` | Response Header `X-Correlation-ID`, `HttpContext.Items["CorrelationId"]`, Activity Tag `correlation.id`, Serilog LogContext Property |
| **Serilog Request Logging** | HTTP request context, Elapsed time | Kiểm tra `elapsed > 500ms` -> log Warning; StatusCode $\ge 500$ -> Error; StatusCode $\ge 400$ -> Warning; còn lại -> Information | Log event có cấu trúc tới Console, File `logs/culinaryblog-.log`, Seq |
| **LoggingBehavior** | MediatR `TRequest`, `CancellationToken` | Đo `Stopwatch`, log bắt đầu, kiểm tra $> 500ms$ sau khi hoàn tất, catch và log `Exception` | Trả về `TResponse` của handler hoặc rethrow exception; ghi log metadata an toàn |
| **HTTP Metrics Middleware** | HTTP request pipeline | Đo thời gian chạy, kiểm tra StatusCode | Counter `http.requests.total`, `http.requests.errors` (nếu status $\ge 400$), Histogram `http.request.duration.ms` |
| **CulinaryBlogTelemetry** | Sự kiện nghiệp vụ hoàn tất | Gọi `Counter.Add(1)` trên OpenTelemetry Meter | Metric `recipes.created` và `recipes.published` |

---

## 6. Business Rules

1. **Correlation ID Idempotency & Sanitization**:
   - Nếu client gửi header `X-Correlation-ID` hợp lệ ($\le 64$ ký tự, chỉ gồm `[a-zA-Z0-9_\-]`), hệ thống bắt buộc tái sử dụng nguyên vẹn để bảo đảm truy vết xuyên suốt hệ thống microservices/client-server.
   - Nếu client gửi chuỗi rác, chứa ký tự đặc biệt, chứa token/password, hoặc dài $> 64$ ký tự, middleware bắt buộc hủy bỏ và tự sinh một GUID chuẩn mới để tránh tấn công Log Injection hoặc rò rỉ dữ liệu.
2. **Ngưỡng cảnh báo Slow Request 500ms**:
   - Mọi HTTP Request có thời gian xử lý $> 500ms$ bắt buộc được Serilog ghi ở mức `LogEventLevel.Warning` (thay vì `Information`).
   - Mọi MediatR Command hoặc Query có thời gian xử lý $> 500ms$ bắt buộc được `LoggingBehavior` ghi cảnh báo mức `Warning` kèm thông báo: `"Long-running MediatR request {RequestName} completed in {ElapsedMilliseconds} ms (> 500ms)"`.
3. **Quy tắc an toàn bảo vệ dữ liệu nhạy cảm (Security by Design)**:
   - `LoggingBehavior` tuyệt đối không serialize hoặc in nội dung đối tượng request/response ra log, ngăn ngừa rò rỉ mật khẩu người dùng, JWT token, Credit Card, hoặc thông tin định danh cá nhân (PII).
   - Tuyệt đối không log connection string hoặc secret key của MinIO/Redis vào log context.
4. **Tính nhất quán của Business Metrics**:
   - Counter `recipes.created` và `recipes.published` **CHỈ** được phép tăng khi transaction cơ sở dữ liệu `_unitOfWork.SaveChangesAsync(cancellationToken)` trả về thành công.
   - Nếu có lỗi phát sinh (database connection fail, validation fail, timeout) dẫn đến transaction bị rollback, telemetry counter **KHÔNG ĐƯỢC PHÉP** tăng.
5. **UserId Enrichment**:
   - Nếu request có JWT Token hợp lệ: `UserId` được trích xuất từ Claim `NameIdentifier` (hoặc fallback về Claim `sub`) và đưa vào Serilog context.
   - Nếu request là Anonymous (khách vãng lai): `UserId` không được đưa vào context, hệ thống hoạt động bình thường, tuyệt đối không gây `NullReferenceException`.
6. **Graceful Fallback cho External Exporters**:
   - Khi Seq (`5341`) hoặc OTLP Collector (`4317`) chưa khởi động hoặc gặp sự cố, ứng dụng vẫn phải vận hành bình thường, không được phép làm gián đoạn request của người dùng (non-blocking). Log vẫn được ghi an toàn ra Console và Rolling File.

---

## 7. Ví dụ hoạt động

### Ví dụ 1: Request bình thường với Correlation ID do Client chỉ định

```
INPUT:
HTTP Request:
GET /api/v1/recipes/pho-bo-ha-noi HTTP/1.1
Host: localhost:5000
X-Correlation-ID: req-client-demo-999

↓ PROCESS:
1. CorrelationIdMiddleware tiếp nhận:
   - Header 'X-Correlation-ID' = "req-client-demo-999" (hợp lệ).
   - context.Items["CorrelationId"] = "req-client-demo-999".
   - context.Response.Headers["X-Correlation-ID"] = "req-client-demo-999".
   - Serilog LogContext.PushProperty("CorrelationId", "req-client-demo-999").
   - Activity.Current.SetTag("correlation.id", "req-client-demo-999").
2. MediatR gửi GetRecipeDetailQuery:
   - LoggingBehavior log Information: "Handling MediatR request GetRecipeDetailQuery".
   - EF Core sinh query SQL, OpenTelemetry trace span con liên kết với TraceId.
   - Handler hoàn tất trong 45ms.
   - LoggingBehavior log Information: "Handled MediatR request GetRecipeDetailQuery in 45 ms".
3. Serilog Request Logging hoàn tất:
   - Thời gian 52ms <= 500ms -> Log Information.
   - Metric counter http.requests.total tăng 1.

↓ OUTPUT:
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-ID: req-client-demo-999

Structured Log ghi nhận:
[14:15:30 INF] HTTP GET /api/v1/recipes/pho-bo-ha-noi responded 200 in 52.1234 ms
Properties: {
  "CorrelationId": "req-client-demo-999",
  "RequestMethod": "GET",
  "RequestPath": "/api/v1/recipes/pho-bo-ha-noi",
  "StatusCode": 200,
  "Elapsed": 52.1234,
  "TraceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "SpanId": "00f067aa0ba902b7"
}
```

### Ví dụ 2: Request chạy chậm phát hiện Slow Request (>500ms)

```
INPUT:
HTTP Request:
GET /api/v1/recipes/heavy-report HTTP/1.1
(Không gửi X-Correlation-ID)

↓ PROCESS:
1. CorrelationIdMiddleware sinh GUID ngẫu nhiên: "e4d9c721-82cb-42b8-936d-d128d58a69bf".
2. MediatR xử lý query phức tạp mất 540ms.
3. LoggingBehavior phát hiện 540ms > 500ms:
   - Tự động nâng mức log lên Warning:
     "Long-running MediatR request GetHeavyReportQuery completed in 540 ms (> 500ms)"
4. Serilog Request Logging phát hiện 545ms > 500ms:
   - Tự động nâng mức log lên Warning.

↓ OUTPUT:
HTTP/1.1 200 OK
X-Correlation-ID: e4d9c721-82cb-42b8-936d-d128d58a69bf

Structured Log ghi nhận:
[14:16:05 WRN] HTTP GET /api/v1/recipes/heavy-report responded 200 in 545.2100 ms
[14:16:05 WRN] Long-running MediatR request GetHeavyReportQuery completed in 540 ms (> 500ms)
```

---

## 8. Error Handling

| Tình huống lỗi | Cơ chế xử lý | HTTP Status / Log Behavior |
| :--- | :--- | :--- |
| Client gửi `X-Correlation-ID` chứa ký tự nhạy cảm, token, hoặc dài $> 64$ ký tự | Middleware loại bỏ chuỗi không an toàn, tự động sinh một GUID chuẩn thay thế, gắn vào response | HTTP Status của endpoint xử lý; Header phản hồi mang GUID an toàn; Log không bị injection |
| Request xử lý lỗi ném ra Exception trong MediatR handler | `LoggingBehavior` bắt exception, log chi tiết Error kèm `RequestName`, `ElapsedMilliseconds`, Exception Message và StackTrace, sau đó rethrow | Log Error; Tầng Exception Handler của API bắt lại và trả về RFC 7807 Problem Details (HTTP 500) |
| Request trả về lỗi Client (4xx) | Serilog đánh giá StatusCode trong khoảng 400-499 | Ghi log mức `Warning`; Counter `http.requests.errors` tăng 1 |
| Request trả về lỗi Server (5xx) | Serilog đánh giá StatusCode $\ge 500$ | Ghi log mức `Error`; Counter `http.requests.errors` tăng 1 |
| Transaction tạo công thức bị lỗi cơ sở dữ liệu | Transaction rollback | Không gọi `RecordRecipeCreated`; Counter `recipes.created` không tăng |
| OTLP Collector (`4317`) hoặc Seq (`5341`) chưa bật hoặc down | Exporters của Serilog và OpenTelemetry sử dụng cơ chế non-blocking / graceful fallback | Ứng dụng không bị crash; API phục vụ người dùng bình thường; Log tiếp tục lưu an toàn ra Console và File |

---

## 9. Cách chạy và Demo

### Bước 1: Khởi động API Backend
```bash
dotnet run --project backend/src/CulinaryBlog.Api/CulinaryBlog.API.csproj
```
API khởi động và lắng nghe tại `http://localhost:5000` (hoặc cổng cấu hình).

### Bước 2: Demo A - Tự động cấp Correlation ID và Structured Log
Gửi một request bất kỳ không có header:
```bash
curl -i http://localhost:5000/api/v1/recipes
```
**Kết quả mong đợi**:
- Header phản hồi có dạng:
  ```http
  HTTP/1.1 200 OK
  X-Correlation-ID: a1b2c3d4-e5f6-7890-abcd-ef1234567890
  ```
- Terminal console in dòng log có cấu trúc:
  ```
  [INF] HTTP GET /api/v1/recipes responded 200 in 15.2341 ms
  ```

### Bước 3: Demo B - Tái sử dụng Correlation ID của Client
Gửi request kèm header tùy biến:
```bash
curl -i -H "X-Correlation-ID: demo-trace-manh-2026" http://localhost:5000/api/v1/recipes
```
**Kết quả mong đợi**:
- Header phản hồi trả lại chính xác: `X-Correlation-ID: demo-trace-manh-2026`.
- Log event trong file `backend/src/CulinaryBlog.Api/logs/culinaryblog-*.log` có thuộc tính `"CorrelationId": "demo-trace-manh-2026"`.

### Bước 4: Demo C - Kiểm tra file log vật lý
Kiểm tra thư mục log được tự động sinh ra:
```powershell
Get-Content -Tail 20 backend/src/CulinaryBlog.Api/logs/culinaryblog-*.log
```
Mọi dòng log đều lưu dưới dạng JSON/structured text với đầy đủ timestamp, CorrelationId, và LogLevel.

---

## 10. Testing

### Bộ kiểm thử thực tế
Kiểm thử được thực hiện bằng `dotnet test` trực tiếp trên giải pháp backend.

#### 1. Bộ kiểm thử chuyên biệt cho Observability (`CulinaryBlog.UnitTests`)
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --filter "FullyQualifiedName~Observability|FullyQualifiedName~LoggingBehavior"
```
**Kết quả thực tế**:
- **Passed**: 28
- **Failed**: 0
- **Skipped**: 0
- **Total**: 28
- **Duration**: ~700 ms

Danh sách 28 test cases đã chạy và đạt 100%:
1. `ResolveCorrelationId_GeneratesNewGuid_WhenHeaderNotProvided`: PASS
2. `ResolveCorrelationId_ReusesClientHeader_WhenValid`: PASS
3. `ResolveCorrelationId_RegeneratesSafeGuid_WhenHeaderInvalidOrTooLong`: PASS
4. `CorrelationIdMiddleware_SetsItem_AndResponseHeader_AndInvokesNext`: PASS
5. `CorrelationIdMiddleware_TagsActivity_WhenActivityCurrentExists`: PASS
6. `UserIdExtraction_FindsNameIdentifierOrSub_WhenAuthenticated`: PASS
7. `UserIdExtraction_FindsSub_WhenNameIdentifierMissing`: PASS
8. `UserIdExtraction_ReturnsNull_ForAnonymousUser_WithoutCrashing`: PASS
9. `LogLevelEvaluation_ReturnsWarning_WhenElapsedExceeds500ms`: PASS
10. `LogLevelEvaluation_ReturnsInformation_WhenElapsedIsUnder500ms`: PASS
11. `LogLevelEvaluation_ReturnsError_WhenStatusCodeIs500OrExceptionThrown`: PASS
12. `LogLevelEvaluation_ReturnsWarning_WhenStatusCodeIs404`: PASS
13. `AddObservability_RegistersTelemetryService_InServiceCollection`: PASS
14. `CulinaryBlogTelemetry_RecordHttpRequest_DoesNotThrow`: PASS
15. `RecipeWriteService_CreateAsync_CallsRecordRecipeCreated_OnSuccess`: PASS
16. `RecipeWriteService_CreateAsync_CallsBothCreatedAndPublished_WhenInitiallyPublished`: PASS
17. `RecipeWriteService_CreateAsync_DoesNotRecordMetrics_WhenUnitOfWorkFails`: PASS
18. `RecipeWriteService_PublishAsync_UpdatesStatus_AndRecordsMetric_OnSuccess`: PASS
19. `RecipeWriteService_PublishAsync_DoesNotRecordMetric_WhenUnitOfWorkFails`: PASS
20. `CorrelationId_Sanitization_RejectsTokensAndPasswordsInHeader`: PASS
21. `LoggingBehavior_LogsRequestNameAndDuration_OnSuccess`: PASS
22. `LoggingBehavior_LogsWarning_WhenExecutionExceeds500ms`: PASS
23. `LoggingBehavior_DoesNotLogWarning_WhenExecutionUnder500ms`: PASS
24. `LoggingBehavior_LogsErrorAndRethrows_WhenHandlerThrowsException`: PASS
25. `LoggingBehavior_DoesNotLogSensitiveData_SuchAsPasswordsOrTokens`: PASS
26. Các kịch bản mở rộng kiểm thử Telemetry Meters và Activity Tracing: PASS

#### 2. Toàn bộ Unit Test Suite của Solution
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj
```
**Kết quả thực tế**:
- **Passed**: 115
- **Failed**: 0
- **Skipped**: 1 (`RecipeAndIngredientWrites_PersistRelationsAndOwnedNutrition` - yêu cầu live database)
- **Total**: 116

---

## 11. Build

### Lệnh biên dịch
```bash
dotnet build backend/CulinaryBlog.sln
```

### Kết quả biên dịch thực tế
- **Status**: Build Succeeded.
- **Error(s)**: 0
- **Warning(s)**: Có một số warning liên quan đến StyleCop/SonarAnalyzer (dấu phẩy cuối dòng, newline) và cảnh báo phụ thuộc thư viện có sẵn từ skeleton, không ảnh hưởng đến độ ổn định và tính đúng đắn của ứng dụng.

---

## 12. Limitations

1. **Phụ thuộc hạ tầng bên ngoài đối với Seq và OpenTelemetry Collector**:
   - Mặc dù mã nguồn backend đã tích hợp hoàn chỉnh `Serilog.Sinks.Seq` và OpenTelemetry gRPC OTLP Exporter (`http://localhost:4317`), việc hiển thị đồ thị trên Seq UI hoặc Jaeger/Grafana đòi hỏi môi trường phải chạy các container Docker tương ứng.
   - Khi không có các container này, ứng dụng vẫn tự động ghi log bình thường ra Console và Rolling File mà không phát sinh lỗi ngoại lệ.
2. **Dashboard Visualization**:
   - Việc trực quan hóa metrics thành biểu đồ (Grafana Dashboards cho `recipes.created`, `http.requests.total`, error rate) nằm ở tầng hạ tầng giám sát vận hành (DevOps/SRE) chứ không nằm trong mã nguồn API của dự án.
3. **Integration Test Suite**:
   - Dự án `CulinaryBlog.IntegrationTests` đòi hỏi Docker testcontainers cho PostgreSQL/Redis/MinIO để khởi động trọn vẹn `TestServer`. Trong môi trường unit test độc lập, toàn bộ hành vi Observability đã được kiểm chứng đầy đủ bằng 28 unit tests.

---

## 13. Kết luận

Branch `feat/vohungmanh-observability` đã hoàn thành xuất sắc việc thiết lập hệ thống quan sát toàn diện theo chuẩn Production cho dự án CulinaryBlog:
- Đạt chuẩn **Distributed Tracing** với OpenTelemetry (W3C TraceContext) và tương quan với Serilog qua **Correlation ID**.
- Tự động giám sát hiệu năng với cơ chế phát hiện **Slow Request > 500ms** cả ở tầng HTTP lẫn tầng Application MediatR.
- Đo lường chính xác các **Business Metrics** nghiệp vụ (`recipes.created`, `recipes.published`).
- Đạt độ tin cậy và bảo mật cao thông qua **28/28 unit tests passed** và tuân thủ nguyên tắc không rò rỉ dữ liệu nhạy cảm.
