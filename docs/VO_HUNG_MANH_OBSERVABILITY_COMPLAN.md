# BÁO CÁO TOÀN DIỆN VÀ TÀI LIỆU BẢO VỆ CHUYÊN ĐỀ OBSERVABILITY

**Sinh viên thực hiện**: Võ Hùng Mạnh (Nhóm 12)  
**Phần hành đảm nhiệm**: Feature cuối Task 4 — Observability (Structured Logging, CorrelationId, MediatR LoggingBehavior, Slow Request Warning, OpenTelemetry Tracing & Metrics, Business Metrics)  
**Branch Git**: `feat/vohungmanh-observability`  
**Base Commit**: `4592f1d98d280f16e7e771a8d06ca651661cea67` (nhánh `origin/main` mới nhất)

---

## MỤC LỤC
1. [Khái niệm Observability](#1-observability-là-gì)
2. [Phân biệt Monitoring và Observability](#2-monitoring-khác-observability-như-thế-nào)
3. [Ba trụ cột cốt lõi: Logs, Metrics, Traces](#3-ba-trụ-cột-logs-metrics-traces)
4. [Structured Logging](#4-structured-logging)
5. [Serilog Engine trong .NET](#5-serilog)
6. [Khái niệm Sink](#6-sink)
7. [Seq Server](#7-seq)
8. [CorrelationId](#8-correlationid)
9. [TraceId](#9-traceid)
10. [SpanId](#10-spanid)
11. [So sánh CorrelationId vs TraceId](#11-correlationid-vs-traceid)
12. [Middleware Architecture trong ASP.NET Core](#12-middleware)
13. [RequestPath](#13-requestpath)
14. [HTTP Method](#14-http-method)
15. [HTTP Status Code](#15-status-code)
16. [Elapsed Time](#16-elapsed-time)
17. [UserId và Claim Extraction](#17-userid)
18. [Cơ chế cảnh báo Slow Request > 500ms](#18-slow-request-500ms)
19. [MediatR LoggingBehavior](#19-mediatr-loggingbehavior)
20. [Pipeline Behavior Pattern](#20-pipeline-behavior)
21. [Chuẩn mở OpenTelemetry](#21-opentelemetry)
22. [Activity trong .NET](#22-activity)
23. [ActivitySource](#23-activitysource)
24. [Distributed Tracing](#24-distributed-tracing)
25. [HTTP Instrumentation](#25-http-instrumentation)
26. [EF Core Instrumentation](#26-ef-core-instrumentation)
27. [Meter trong .NET Diagnostics](#27-meter)
28. [Counter Instrument](#28-counter)
29. [Histogram Instrument](#29-histogram)
30. [Request Count Metric](#30-request-count)
31. [Duration Histogram Metric](#31-duration-histogram)
32. [Error Rate Mechanism](#32-error-rate)
33. [Business Metrics](#33-business-metric)
34. [Metric Recipe Created](#34-recipe-created)
35. [Metric Recipe Published](#35-recipe-published)
36. [OTLP Protocol](#36-otlp)
37. [OpenTelemetry Collector](#37-collector)
38. [Bảo mật thông tin trong Logging](#38-security--sensitive-logs)
39. [Code Walkthrough chi tiết từng File](#39-code-walkthrough-từng-file)
40. [Request Flow chi tiết](#40-request-flow)
41. [Trace Flow chi tiết](#41-trace-flow)
42. [Metrics Flow chi tiết](#42-metrics-flow)
43. [Test Matrix toàn diện](#43-test-matrix)
44. [Kịch bản Demo thực tế](#44-demo)
45. [Giới hạn hệ thống (Limitations)](#45-limitations)
46. [25 câu hỏi phản biện của Giảng viên & Đáp án chi tiết](#46-25-câu-hỏi-phản-biện-giảng-viên--đáp-án)
47. [Kịch bản bảo vệ thuyết trình 3 phút](#47-kịch-bản-bảo-vệ-3-phút)

---

## 1. Observability là gì?
Observability (Khả năng quan sát) là thước đo mức độ chúng ta có thể hiểu được trạng thái bên trong của một hệ thống phần mềm phức tạp chỉ bằng cách quan sát các đầu ra dữ liệu đo lường (telemetry outputs) bên ngoài của nó. Trong hệ thống backend hiện đại theo kiến trúc phân tán (Microservices hoặc Modular Monolith), việc chỉ biết hệ thống "sống hay chết" là không đủ; chúng ta phải giải thích được **tại sao** một request bị chậm, **điều gì** xảy ra trong cơ sở dữ liệu, và **nguyên nhân gốc rễ** của lỗi là gì mà không cần can thiệp trực tiếp vào mã nguồn đang chạy.

## 2. Monitoring khác Observability như thế nào?
- **Monitoring (Giám sát)**: Tập trung vào "Known Unknowns" (những điều đã biết trước là có thể xảy ra). Monitoring thu thập các chỉ số định sẵn (CPU, RAM, số lượng lỗi 500) và phát cảnh báo (Alert) khi vượt ngưỡng quy định. Monitoring cho ta biết: *"Hệ thống đang bị lỗi!"*.
- **Observability (Quan sát)**: Tập trung vào "Unknown Unknowns" (những tình huống bất thường chưa từng dự đoán trước). Observability cho phép ta bóc tách sâu dữ liệu telemetry theo đa chiều (multidimensional context: CorrelationId, UserId, SQL Query, Trace timeline). Observability trả lời câu hỏi: *"Tại sao request của User X vào lúc 14h05 lại mất 520ms tại câu lệnh truy vấn bảng Recipes?"*.

## 3. Ba trụ cột: Logs, Metrics, Traces
Hệ thống quan sát hoàn chỉnh được cấu thành từ 3 trụ cột dữ liệu:
1. **Logs**: Bản ghi rời rạc, có dấu thời gian (timestamped events) mô tả cụ thể điều gì đã xảy ra tại một thời điểm trong code.
2. **Metrics**: Dữ liệu số học được tổng hợp theo thời gian (aggregated numerical data), có chi phí lưu trữ thấp, tối ưu cho việc vẽ đồ thị dashboard và phát hiện xu hướng (trend/spike).
3. **Traces (Distributed Tracing)**: Chuỗi hành trình liên kết xuyên suốt một request từ khi client gửi tới server, qua các tầng middleware, service, repository, và câu truy vấn database.

## 4. Structured Logging
Ghi log truyền thống thường là chuỗi văn bản thuần (Unstructured Text), ví dụ: `Log.Info("User " + userId + " created recipe " + id)`. Kiểu log này khiến việc tìm kiếm, phân tích và lọc dữ liệu trên quy mô lớn trở thành cơn ác mộng vì phải dùng regular expressions chậm chạp.  
**Structured Logging** ghi lại log dưới dạng một tập hợp các cặp Key-Value có kiểu dữ liệu rõ ràng (thường xuất ra dạng JSON). Ví dụ:
```json
{
  "@t": "2026-10-05T15:30:12.1234567Z",
  "@mt": "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms",
  "RequestMethod": "GET",
  "RequestPath": "/api/v1/recipes",
  "StatusCode": 200,
  "Elapsed": 12.3456,
  "CorrelationId": "c6f39d2e-4b1a-4d7a-8f9e-1a2b3c4d5e6f",
  "UserId": "user-guid-001",
  "TraceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```
Nhờ vậy, ta có thể viết truy vấn chính xác: `StatusCode >= 500 AND Elapsed > 500`.

## 5. Serilog
Serilog là thư viện logging thế hệ mới phổ biến nhất trong hệ sinh thái .NET. Serilog được thiết kế từ gốc với tư duy coi message template là cú pháp cấu trúc (Message Template DSL).  
Trong dự án CulinaryBlog:
- Serilog được cấu hình thông qua `LoggerConfiguration` đọc từ `appsettings.json`.
- Sử dụng `builder.Host.UseSerilog()` để thay thế hoàn toàn logger mặc định của Microsoft.
- Sử dụng `app.UseSerilogRequestLogging()` để gom nhật ký truy cập HTTP về một log event duy nhất, loại bỏ sự rác rưởi (chatter) của logging mặc định.

## 6. Sink
Trong kiến trúc của Serilog, **Sink** là thành phần đích nhận dữ liệu log (Log Consumer). Một sự kiện log có thể được gửi đồng thời tới nhiều sink khác nhau:
- `Console Sink`: In ra terminal khi phát triển.
- `File Sink`: Ghi log cuốn chiếu (Rolling File) theo ngày vào thư mục `logs/culinaryblog-.log` kèm giới hạn lưu trữ `retainedFileCountLimit: 30`.
- `Seq Sink`: Gửi trực tiếp dữ liệu log có cấu trúc qua mạng tới Seq server bằng giao thức HTTP POST JSON.

## 7. Seq
Seq là một log aggregation server chuyên biệt dành riêng cho Structured Logging. Seq cho phép lập trình viên và DevOps:
- Tìm kiếm theo cú pháp tự nhiên: `RequestPath = '/api/v1/recipes' and Elapsed > 500`.
- Tự động nhận diện và hiển thị tất cả các thuộc tính dynamic trong log context.
- Tạo biểu đồ, alerts và theo dõi sức khỏe ứng dụng theo thời gian thực.
- Cấu hình trong `appsettings.json`: `"serverUrl": "http://localhost:5341"`.

## 8. CorrelationId
Correlation ID là một chuỗi định danh duy nhất (UUID/GUID) đại diện cho một luồng nghiệp vụ hoặc một chuỗi các tác vụ liên quan chặt chẽ tới nhau.  
Khi một người dùng kích hoạt một hành động từ trình duyệt/mobile app, hành động đó có thể kích hoạt nhiều lời gọi API, background jobs và database operations. Nhờ gắn chung một CorrelationId vào tất cả các log events liên quan, người vận hành có thể lọc toàn bộ hành trình của request đó mà không bị lẫn với hàng triệu request của người khác.

## 9. TraceId
TraceId là định danh 128-bit (thường biểu diễn dưới dạng 32 ký tự hex) đại diện cho một Distributed Trace theo chuẩn quốc tế **W3C Trace Context**.  
TraceId được tự động sinh ra hoặc lan truyền qua HTTP header chuẩn `traceparent`. TraceId kết nối các node/dịch vụ khác nhau trong hệ sinh thái microservices.

## 10. SpanId
SpanId là định danh 64-bit (16 ký tự hex) đại diện cho một phân đoạn thời gian (Span) thực hiện một công việc cụ thể trong trace (ví dụ: thời gian xử lý một HTTP request, thời gian thực thi một truy vấn SQL, thời gian đọc Redis). Mỗi Span chứa SpanId riêng, TraceId chung của toàn trace, và ParentSpanId trỏ về span cha.

## 11. CorrelationId vs TraceId
| Tiêu chí | CorrelationId | TraceId |
| :--- | :--- | :--- |
| **Nguồn gốc** | Do ứng dụng tự định nghĩa (Application Level) | Do chuẩn W3C TraceContext quy định (Infrastructure Level) |
| **Mục đích chính** | Gom nhóm log events để dễ tìm kiếm trên Seq/ELK | Đo lường thời gian, quan sát độ trễ (waterfall timeline) |
| **Định dạng** | Tự do (thường là GUID chuẩn hoặc client string) | Cố định 32 ký tự hex (128-bit) |
| **Header giao tiếp** | `X-Correlation-ID` | `traceparent` |
| **Trong dự án** | Do `CorrelationIdMiddleware` sinh hoặc nhận | Do OpenTelemetry / .NET `Activity` quản lý |

## 12. Middleware
Middleware trong ASP.NET Core là các thành phần phần mềm được ghép nối thành một đường ống xử lý (Pipeline) để đón nhận HTTP request và sinh ra HTTP response.  
Trong dự án, `CorrelationIdMiddleware` được đặt ở vị trí **đầu tiên** của pipeline để đảm bảo:
1. Mọi thành phần phía sau (Authentication, Authorization, Serilog, Endpoints) đều đã có sẵn `CorrelationId` trong `HttpContext.Items`.
2. Header phản hồi `X-Correlation-ID` luôn được gửi lại cho client.
3. LogContext của Serilog được bao bọc (scope) bởi `CorrelationId`.

## 13. RequestPath
Đường dẫn tài nguyên HTTP đang được yêu cầu, trích xuất từ `httpContext.Request.Path.Value`. Ví dụ: `/api/v1/recipes`, `/api/v1/auth/login`. Đây là thuộc tính quan trọng giúp phân nhóm hiệu năng theo từng API endpoint.

## 14. HTTP Method
Động từ HTTP chỉ thị hành động mong muốn: `GET`, `POST`, `PUT`, `PATCH`, `DELETE`. Giúp phân biệt các thao tác đọc dữ liệu và ghi dữ liệu trên cùng một đường dẫn tài nguyên.

## 15. Status Code
Mã trạng thái HTTP đại diện cho kết quả xử lý của server (200 OK, 201 Created, 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found, 500 Internal Server Error). Căn cứ vào status code, hệ thống phân loại log level: 4xx được đưa về Warning, 5xx được đưa về Error.

## 16. Elapsed Time
Tổng thời gian (tính bằng mili-giây) từ khi server nhận được byte đầu tiên của request cho đến khi hoàn tất việc gửi response. Serilog đo đạc thông qua `ValueStopwatch` nội bộ với độ chính xác cao (sub-millisecond precision).

## 17. UserId
Mã định danh của người dùng thực hiện request. Trong dự án CulinaryBlog, hệ thống xác thực dựa trên JWT Bearer Token. Middleware phân tích token và nạp danh sách Claims vào `ClaimsPrincipal`. `Program.cs` trích xuất `UserId` bằng cách đọc `User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")`. Nếu user chưa đăng nhập (Anonymous), giá trị trả về `null`, hệ thống không đưa `UserId` vào log và hoạt động an toàn không gây lỗi.

## 18. Slow Request > 500ms
Quy định nghiệm thu bắt buộc của Task 4: Bất kỳ request nào có thời gian xử lý vượt quá **500ms** đều phải bị coi là Slow Request và ghi nhận ở mức `Warning`.  
Cơ chế thực hiện:
- **HTTP Request Level**: Tại delegate `options.GetLevel` của `app.UseSerilogRequestLogging`, nếu `elapsed > 500` thì trả về `LogEventLevel.Warning`.
- **MediatR Level**: Tại `LoggingBehavior<TRequest, TResponse>`, sử dụng `Stopwatch` đo thời gian gọi `await next(cancellationToken)`. Nếu `stopwatch.ElapsedMilliseconds > 500`, ghi log Warning cảnh báo: `"Long-running MediatR request {RequestName} completed in {ElapsedMilliseconds} ms (> 500ms)"`.

## 19. MediatR LoggingBehavior
MediatR là thư viện hiện thực Mediator Pattern trong .NET. Để quan sát toàn diện tầng ứng dụng (Application Layer), ta tạo lớp `LoggingBehavior<TRequest, TResponse>` kế thừa `IPipelineBehavior<TRequest, TResponse>`. Behavior này hoạt động như một middleware cho MediatR, tự động bắt mọi Command và Query chạy qua hệ thống.

## 20. Pipeline Behavior
Pipeline Behavior là biến thể của mẫu thiết kế Decorator / Chain of Responsibility. Thay vì viết code đo thời gian và try-catch trong từng handler, Pipeline Behavior cho phép chèn các Cross-Cutting Concerns (Logging, Validation, Caching) chạy xung quanh handler một cách hoàn toàn tự động. Trong dự án:
```
Request -> LoggingBehavior -> ValidationBehavior -> CommandHandler
```

## 21. OpenTelemetry
OpenTelemetry (viết tắt là OTel) là chuẩn mở quốc tế (thuộc quỹ CNCF - Cloud Native Computing Foundation) cung cấp bộ API, SDK và công cụ tiêu chuẩn để tạo, thu thập và xuất dữ liệu telemetry (Metrics, Traces, Logs) độc lập với nhà cung cấp (vendor-neutral).

## 22. Activity
Trong .NET runtime (.NET 5 trở lên), class `System.Diagnostics.Activity` là hiện thực cốt lõi của khái niệm **Span** trong OpenTelemetry. Khi một request được tiếp nhận, ASP.NET Core tạo một `Activity` đại diện cho công việc đó. Thông qua thuộc tính `Activity.Current`, bất kỳ đoạn code nào trong luồng thực thi cũng có thể truy cập `TraceId`, `SpanId`, hoặc gắn thêm `Tag`.

## 23. ActivitySource
`System.Diagnostics.ActivitySource` là factory dùng để tạo và quản lý các `Activity`. Trong dự án, lớp `CulinaryBlogTelemetry` tạo một `ActivitySource` tập trung mang tên `"CulinaryBlog.Api"`. Khi đăng ký OpenTelemetry:
```csharp
tracing.AddSource(CulinaryBlogTelemetry.ActivitySourceName);
```
OTel SDK sẽ tự động lắng nghe và xuất các Activity được tạo từ nguồn này.

## 24. Distributed Tracing
Kỹ thuật theo dõi và trực quan hóa toàn bộ dòng chảy của một request khi nó di chuyển qua các ranh giới mạng, tiến trình và máy chủ. Giúp phát hiện tức thì điểm nghẽn cổ chai (bottleneck) trong chuỗi thực thi.

## 25. HTTP Instrumentation
Gói `OpenTelemetry.Instrumentation.AspNetCore` tự động lắng nghe các sự kiện của ASP.NET Core framework, tự động tạo Span cho mỗi HTTP incoming request, gán status code, route template và đo thời gian xử lý. Gói `OpenTelemetry.Instrumentation.Http` làm điều tương tự cho các outgoing request qua `HttpClient`.

## 26. EF Core Instrumentation
Gói `OpenTelemetry.Instrumentation.EntityFrameworkCore` can thiệp vào `DiagnosticSource` của Entity Framework Core, tự động tạo child span cho mỗi câu truy vấn SQL gửi đến PostgreSQL, bao gồm câu lệnh `SELECT`, `INSERT`, `UPDATE`, `DELETE`, và tham số truy vấn (đã được làm sạch).

## 27. Meter
Trong .NET (`System.Diagnostics.Metrics`), `Meter` là thành phần chịu trách nhiệm tạo và phát các công cụ đo lường số liệu (Instruments: Counter, Histogram, Gauge). Dự án định nghĩa Meter tập trung `"CulinaryBlog.Api"` và đăng ký với OTel qua `metrics.AddMeter(CulinaryBlogTelemetry.MeterName)`.

## 28. Counter
Counter là một instrument chỉ tăng giá trị đơn điệu theo thời gian (Monotonically Increasing Counter). Dùng để đếm tổng số lần một sự kiện xảy ra, ví dụ: tổng số request, tổng số công thức được tạo.

## 29. Histogram
Histogram là một instrument thống kê phân phối của các giá trị đo lường được (Statistical Distribution). Thay vì chỉ tính giá trị trung bình (vốn dễ bị sai lệch bởi các giá trị ngoại lai - outliers), Histogram cho phép các công cụ giám sát tính toán các phân vị thời gian phản hồi: p50, p90, p95, p99.

## 30. Request Count
Metric đo tổng số request HTTP được tiếp nhận bởi server. Trong dự án, counter `http.requests.total` tăng 1 đơn vị sau mỗi request, gắn kèm nhãn (dimensions): `http.method`, `http.route`, `http.status_code`.

## 31. Duration Histogram
Metric đo lường thời gian thực thi của HTTP request: `http.request.duration.ms` (đơn vị: ms). Cung cấp cái nhìn toàn diện về phân bố độ trễ của API.

## 32. Error Rate
Tỷ lệ lỗi (Error Rate) là chỉ số vàng trong Site Reliability Engineering (SRE). Trong dự án, hệ thống cung cấp counter `http.requests.errors` (tăng khi status code $\ge 400$). Error Rate được tính toán downstream trên công cụ phân tích (Prometheus/Grafana) theo công thức:
$$\text{Error Rate} = \frac{\Delta(http\_requests\_errors)}{\Delta(http\_requests\_total)} \times 100\%$$
Dự án không hard-code một giá trị tỷ lệ giả tạo mà cung cấp đúng các metric nguyên thủy chuẩn xác.

## 33. Business Metric
Business Metric (Chỉ số nghiệp vụ) là các số liệu đo lường gắn liền với hoạt động kinh doanh của sản phẩm thay vì chỉ là chỉ số kỹ thuật phần cứng. Business metrics cho người quản lý sản phẩm biết: người dùng đang tạo bao nhiêu công thức mỗi giờ, danh mục nào đang được xuất bản nhiều nhất.

## 34. Recipe Created
Metric nghiệp vụ `recipes.created`:
- Kiểu: `Counter<long>`
- Đơn vị: `{recipes}`
- Vị trí: `RecipeWriteService.CreateAsync`
- Thời điểm tăng: **CHỈ** sau khi `await _unitOfWork.SaveChangesAsync(cancellationToken)` hoàn tất thành công. Nếu database bị lỗi, throw exception hoặc rollback transaction thì metric tuyệt đối **KHÔNG** tăng.

## 35. Recipe Published
Metric nghiệp vụ `recipes.published`:
- Kiểu: `Counter<long>`
- Đơn vị: `{recipes}`
- Vị trí: `RecipeWriteService.PublishAsync` (hoặc khi `CreateAsync` với status là `Published`)
- Thời điểm tăng: **CHỈ** sau khi `SaveChangesAsync` hoàn tất thành công, đánh dấu công thức đã chính thức ra mắt cộng đồng.

## 36. OTLP
OpenTelemetry Protocol (OTLP) là giao thức truyền thông chuẩn hóa được xây dựng trên nền tảng Protocol Buffers (Protobuf) qua gRPC (cổng mặc định 4317) hoặc HTTP/JSON (cổng mặc định 4318). OTLP giúp vận chuyển Traces, Metrics và Logs một cách cực kỳ tối ưu và hiệu quả cao về mặt băng thông mạng.

## 37. Collector
OpenTelemetry Collector là một tiến trình proxy/agent độc lập đứng trung gian giữa các ứng dụng và các backend giám sát (Prometheus, Jaeger, Grafana, Datadog). Collector nhận dữ liệu qua OTLP, xử lý (batch, filter, sample, enrich metadata) rồi xuất (export) đến các hệ thống đích.

## 38. Security / Sensitive Logs
Bảo mật là ưu tiên hàng đầu trong Observability. Nguyên tắc bất di bất dịch: **KHÔNG BAO GIỜ ĐƯỢC PHÉP ĐỂ LỘ DỮ LIỆU NHẠY CẢM VÀO LOG**.
1. **Mật khẩu & Token**: Tuyệt đối không log các trường `Password`, `ConfirmPassword`, `RefreshToken`, `Authorization: Bearer <JWT>`, chuỗi kết nối database có mật khẩu, secret key của MinIO.
2. **CorrelationId Sanitization**: Middleware lọc sạch header `X-Correlation-ID`, từ chối các chuỗi chứa khoảng trắng, dấu chấm, ký tự script hoặc token JWT dài ngoằng nhằm ngăn ngừa tấn công Log Injection / Header Injection.
3. **MediatR Logging**: Chỉ log tên command (`RequestName`) và thời gian thực thi (`ElapsedMilliseconds`). Không bao giờ dùng `JsonSerializer.Serialize(request)` bừa bãi vì đối tượng request có thể chứa dữ liệu tài khoản người dùng hoặc file stream nhị phân.

---

## 39. Code Walkthrough chi tiết từng File

### 1. `ICulinaryBlogTelemetry.cs`
- Nằm tại: `CulinaryBlog.Application/Common/Interfaces/ICulinaryBlogTelemetry.cs`
- Mục đích: Đóng vai trò là Interface Abstraction trong tầng Core Application, tuân thủ nguyên lý Dependency Inversion Principle (DIP) của Clean Architecture.
- Cung cấp 4 phương thức:
  - `RecordRecipeCreated(string? category = null)`
  - `RecordRecipePublished(string? category = null)`
  - `RecordHttpRequest(string method, string path, int statusCode, double durationMs)`
  - `RecordError(string errorType, string? endpoint = null)`

### 2. `CulinaryBlogTelemetry.cs`
- Nằm tại: `CulinaryBlog.Infrastructure/Observability/CulinaryBlogTelemetry.cs`
- Triển khai interface `ICulinaryBlogTelemetry`.
- Khởi tạo các singleton tĩnh:
  - `public static readonly ActivitySource ActivitySource = new("CulinaryBlog.Api", "1.0.0");`
  - `public static readonly Meter Meter = new("CulinaryBlog.Api", "1.0.0");`
- Đăng ký các Counters và Histogram thông qua `Meter`:
  - `_recipesCreatedCounter`: Counter `recipes.created`
  - `_recipesPublishedCounter`: Counter `recipes.published`
  - `_httpRequestsCounter`: Counter `http.requests.total`
  - `_httpErrorsCounter`: Counter `http.requests.errors`
  - `_httpRequestDurationHistogram`: Histogram `http.request.duration.ms`

### 3. `OpenTelemetryExtensions.cs`
- Nằm tại: `CulinaryBlog.Infrastructure/Observability/OpenTelemetryExtensions.cs`
- Phương thức mở rộng `IServiceCollection.AddObservability(...)`:
  - Đăng ký `ICulinaryBlogTelemetry` vào DI Container dưới dạng Singleton.
  - Cấu hình `AddOpenTelemetry().ConfigureResource(...)`.
  - Cấu hình Tracing: `AddSource("CulinaryBlog.Api")`, `AddAspNetCoreInstrumentation()`, `AddHttpClientInstrumentation()`, `AddEntityFrameworkCoreInstrumentation()`, `AddOtlpExporter()`.
  - Cấu hình Metrics: `AddMeter("CulinaryBlog.Api")`, `AddAspNetCoreInstrumentation()`, `AddHttpClientInstrumentation()`, `AddOtlpExporter()`.
  - Kiểm tra `Uri.TryCreate` an toàn: nếu endpoint không hợp lệ hoặc collector vắng mặt, không ném exception gây crash ứng dụng.

### 4. `CorrelationIdMiddleware.cs`
- Nằm tại: `CulinaryBlog.Api/Middleware/CorrelationIdMiddleware.cs`
- Kiểm tra header `X-Correlation-ID`:
  - Dùng Regex an toàn `^[a-zA-Z0-9_\-]+$` và giới hạn độ dài $\le 64$ ký tự.
  - Tái sử dụng nếu hợp lệ; tự động sinh `Guid.NewGuid().ToString("D")` nếu thiếu hoặc không hợp lệ.
- Gắn CorrelationId vào:
  - `context.Items["CorrelationId"]`
  - Header phản hồi HTTP qua `context.Response.OnStarting(...)`
  - Tag của `Activity.Current` (`SetTag("correlation.id", ...)`)
  - Serilog `LogContext.PushProperty("CorrelationId", ...)`
  - Nếu `Activity.Current` tồn tại: push tiếp `TraceId` và `SpanId` vào `LogContext`.

### 5. `LoggingBehavior.cs`
- Nằm tại: `CulinaryBlog.Application/Behaviors/LoggingBehavior.cs`
- Pipeline behavior generic: `LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>`.
- Ghi log bắt đầu xử lý với metadata `RequestName`.
- Đo thời gian xử lý chính xác bằng `Stopwatch`.
- Khi kết thúc: nếu `elapsedMs > 500`, ghi log cảnh báo mức `Warning`; ngược lại ghi `Information`.
- Bắt lỗi: nếu handler ném `Exception`, ghi log mức `Error` kèm stack trace và thời gian đã trôi qua, sau đó `throw;` để không làm mất luồng xử lý lỗi của hệ thống.

### 6. `RecipeWriteService.cs`
- Nằm tại: `CulinaryBlog.Application/Features/Recipes/RecipeWriteService.cs`
- Inject `ICulinaryBlogTelemetry? telemetry = null` qua constructor (optional parameter để không phá vỡ unit test hiện có).
- Trong `CreateAsync`:
  ```csharp
  await _recipeRepository.AddAsync(recipe, cancellationToken);
  await _unitOfWork.SaveChangesAsync(cancellationToken);
  _telemetry?.RecordRecipeCreated(recipe.CategoryId.ToString());
  if (recipe.Status == RecipeStatus.Published)
  {
      _telemetry?.RecordRecipePublished(recipe.CategoryId.ToString());
  }
  ```
- Thêm phương thức `PublishAsync`:
  ```csharp
  recipe.Status = RecipeStatus.Published;
  recipe.PublishedAt ??= DateTime.UtcNow;
  await _recipeRepository.UpdateAsync(recipe, cancellationToken);
  await _unitOfWork.SaveChangesAsync(cancellationToken);
  _telemetry?.RecordRecipePublished(recipe.CategoryId.ToString());
  ```

### 7. `Program.cs`
- Nằm tại: `CulinaryBlog.Api/Program.cs`
- Khởi tạo Serilog trước khi dựng Host, đọc cấu hình từ `appsettings.json`.
- Sử dụng `app.UseMiddleware<CorrelationIdMiddleware>()` ở đầu pipeline.
- Cấu hình `app.UseSerilogRequestLogging` với:
  - Custom `MessageTemplate`: `"HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms"`
  - `options.GetLevel`: Tự động nâng mức Warning khi `elapsed > 500ms` hoặc status code $\ge 400$; mức Error khi có Exception hoặc status code $\ge 500$.
  - `options.EnrichDiagnosticContext`: Enrich đầy đủ `RequestPath`, `RequestMethod`, `StatusCode`, `CorrelationId`, `UserId` (từ `ClaimsPrincipal`), `TraceId` và `SpanId`.
- Middleware ghi nhận HTTP Metrics: Đo thời gian thực tế và gọi `ICulinaryBlogTelemetry.RecordHttpRequest`.

---

## 40. Request Flow chi tiết
1. **Bước 1**: Request từ Client gửi đến ASP.NET Core Kestrel Server.
2. **Bước 2**: Request đi vào `CorrelationIdMiddleware`:
   - Kiểm tra `X-Correlation-ID` header.
   - Xác thực/sinh mới CorrelationId an toàn.
   - Lưu vào `context.Items`, gán hook vào response headers.
   - Đẩy CorrelationId, TraceId, SpanId vào Serilog `LogContext`.
3. **Bước 3**: Request đi qua Authentication & Authorization middleware:
   - Nếu có JWT Bearer Token hợp lệ, `ClaimsPrincipal` được khởi tạo chứa các claim (trong đó có `NameIdentifier` / `sub`).
4. **Bước 4**: Request đi vào `SerilogRequestLoggingMiddleware`:
   - Bắt đầu tính thời gian và thu thập thông tin chẩn đoán (DiagnosticContext).
5. **Bước 5**: Request đi vào `HTTP Metrics Middleware`:
   - Bắt đầu bấm giờ `Stopwatch`.
6. **Bước 6**: Endpoint nhận request và gọi MediatR `ISender.Send(command)`:
   - `LoggingBehavior` kích hoạt: ghi log Information `"Handling MediatR request ..."`.
   - `ValidationBehavior` kích hoạt: kiểm tra FluentValidation rules.
   - Handler thực thi: gọi repository, truy vấn EF Core (được EF Core tracing tự động ghi nhận child span).
   - Nếu thực thi command tạo/xuất bản recipe: `RecipeWriteService` lưu DB qua UnitOfWork. Sau khi thành công, gọi `ICulinaryBlogTelemetry` để increment business counters.
   - `LoggingBehavior` kết thúc: nếu thời gian xử lý $> 500ms$, ghi log Warning; ngược lại ghi Information.
7. **Bước 7**: Request quay ngược trở ra qua `HTTP Metrics Middleware`:
   - Ghi nhận `RecordHttpRequest` với thời gian thực tế và status code.
8. **Bước 8**: Request quay ra `SerilogRequestLoggingMiddleware`:
   - Trích xuất `UserId` từ `httpContext.User`.
   - Kiểm tra điều kiện `elapsed > 500ms`: nếu chậm ghi log Warning, nếu lỗi ghi log Error.
   - Xuất log event ra Console, File và Seq.
9. **Bước 9**: Response được trả về cho Client kèm header `X-Correlation-ID`.

---

## 41. Trace Flow chi tiết
```
[HTTP GET /api/v1/recipes/{slug}]  <── Root Span (TraceId: a1b2c3d4...)
│
├── [MediatR GetRecipeBySlugQuery]  <── Internal Execution
│   │
│   ├── [PostgreSQL SELECT recipes] <── Child Span (EF Core Instrumentation)
│   │   Duration: 4.2ms
│   │
│   └── [Redis GET recipe_cache]    <── Child Span (Caching Instrumentation)
│       Duration: 0.8ms
│
└── HTTP 200 OK (Total: 18.5ms)
```

---

## 42. Metrics Flow chi tiết
```
Http Request ──> HTTP Metrics Middleware ──> ICulinaryBlogTelemetry.RecordHttpRequest(...)
                                                  │
                  ┌───────────────────────────────┴───────────────────────────────┐
                  ▼                                                               ▼
        Counter: http.requests.total                               Histogram: http.request.duration.ms
        Tags: method, route, status_code                           Tags: method, route, status_code
                  │
                  ▼ (nếu status_code >= 400)
        Counter: http.requests.errors
```

---

## 43. Test Matrix toàn diện

Hệ thống kiểm thử bao gồm **115 tests passed** (100% pass, 0 failed, 1 skipped theo thiết kế):

| STT | Tên Test Case | Mục đích kiểm tra | Kết quả |
| :---: | :--- | :--- | :---: |
| 1 | `ResolveCorrelationId_GeneratesNewGuid_WhenHeaderNotProvided` | Tự sinh GUID mới khi không có header | **PASS** |
| 2 | `ResolveCorrelationId_ReusesClientHeader_WhenValid` | Tái sử dụng header hợp lệ từ client | **PASS** |
| 3 | `ResolveCorrelationId_RegeneratesSafeGuid_WhenHeaderInvalidOrTooLong` | Chống Log Injection, loại bỏ header độc hại | **PASS** |
| 4 | `CorrelationIdMiddleware_SetsItem_AndResponseHeader_AndInvokesNext` | Gắn CorrelationId vào Items và Response Header | **PASS** |
| 5 | `CorrelationIdMiddleware_TagsActivity_WhenActivityCurrentExists` | Liên kết CorrelationId vào Activity Tag | **PASS** |
| 6 | `UserIdExtraction_FindsNameIdentifierOrSub_WhenAuthenticated` | Đọc UserId từ NameIdentifier theo convention | **PASS** |
| 7 | `UserIdExtraction_FindsSub_WhenNameIdentifierMissing` | Fallback đọc UserId từ claim `sub` | **PASS** |
| 8 | `UserIdExtraction_ReturnsNull_ForAnonymousUser_WithoutCrashing` | Anonymous user không gây crash hay exception | **PASS** |
| 9 | `LogLevelEvaluation_ReturnsWarning_WhenElapsedExceeds500ms` | Cảnh báo Warning khi request > 500ms | **PASS** |
| 10 | `LogLevelEvaluation_ReturnsInformation_WhenElapsedIsUnder500ms` | Mức Information khi request <= 500ms | **PASS** |
| 11 | `LogLevelEvaluation_ReturnsError_WhenStatusCodeIs500OrExceptionThrown` | Mức Error khi lỗi 500 hoặc có Exception | **PASS** |
| 12 | `LogLevelEvaluation_ReturnsWarning_WhenStatusCodeIs404` | Mức Warning khi mã lỗi 4xx (404) | **PASS** |
| 13 | `AddObservability_RegistersTelemetryService_InServiceCollection` | Kiểm tra đăng ký DI cho Telemetry Service | **PASS** |
| 14 | `CulinaryBlogTelemetry_RecordHttpRequest_DoesNotThrow` | Ghi nhận HTTP metrics và counters an toàn | **PASS** |
| 15 | `RecipeWriteService_CreateAsync_CallsRecordRecipeCreated_OnSuccess` | Tăng counter `recipes.created` khi tạo thành công | **PASS** |
| 16 | `RecipeWriteService_CreateAsync_CallsBothCreatedAndPublished_WhenInitiallyPublished` | Tăng cả 2 counter khi tạo công thức Published | **PASS** |
| 17 | `RecipeWriteService_CreateAsync_DoesNotRecordMetrics_WhenUnitOfWorkFails` | Không tăng metric nếu SaveChanges thất bại | **PASS** |
| 18 | `RecipeWriteService_PublishAsync_UpdatesStatus_AndRecordsMetric_OnSuccess` | Chuyển trạng thái và tăng counter khi xuất bản | **PASS** |
| 19 | `RecipeWriteService_PublishAsync_DoesNotRecordMetric_WhenUnitOfWorkFails` | Không tăng metric khi Publish bị lỗi DB | **PASS** |
| 20 | `CorrelationId_Sanitization_RejectsTokensAndPasswordsInHeader` | Từ chối token/password trong header CorrelationId | **PASS** |
| 21 | `LoggingBehavior_LogsRequestNameAndDuration_OnSuccess` | MediatR log tên request và thời gian thực thi | **PASS** |
| 22 | `LoggingBehavior_LogsWarning_WhenExecutionExceeds500ms` | MediatR log Warning khi xử lý > 500ms | **PASS** |
| 23 | `LoggingBehavior_DoesNotLogWarning_WhenExecutionUnder500ms` | MediatR không log Warning khi xử lý nhanh | **PASS** |
| 24 | `LoggingBehavior_LogsErrorAndRethrows_WhenHandlerThrowsException` | MediatR log Error và rethrow khi handler lỗi | **PASS** |
| 25 | `LoggingBehavior_DoesNotLogSensitiveData_SuchAsPasswordsOrTokens` | Không bao giờ log dữ liệu mật khẩu, token | **PASS** |

---

## 44. Demo

### Demo 1: Kiểm chứng Structured Log với CorrelationId
1. Khởi chạy ứng dụng: `dotnet run --project backend/src/CulinaryBlog.Api/CulinaryBlog.API.csproj`.
2. Gửi request:
   ```bash
   curl -i -H "X-Correlation-ID: demo-manh-2026" http://localhost:5000/api/v1/recipes
   ```
3. Quan sát:
   - Header trả về: `X-Correlation-ID: demo-manh-2026`.
   - Log trong terminal / file `logs/culinaryblog-.log` thể hiện:
     - `CorrelationId`: `demo-manh-2026`
     - `RequestPath`: `/api/v1/recipes`
     - `RequestMethod`: `GET`
     - `StatusCode`: `200`
     - `Elapsed`: thời gian mili-giây.

### Demo 2: Kiểm chứng cảnh báo Slow Request (> 500ms)
- Chạy unit test `LogLevelEvaluation_ReturnsWarning_WhenElapsedExceeds500ms` và `LoggingBehavior_LogsWarning_WhenExecutionExceeds500ms`:
  Khi thời gian xử lý đạt $550ms$, log level tự động chuyển sang `Warning` kèm câu thông báo rõ ràng: `"Long-running MediatR request ... completed in 550 ms (> 500ms)"`.

### Demo 3: Kiểm chứng Business Metrics
- Chạy test case `RecipeWriteService_CreateAsync_CallsRecordRecipeCreated_OnSuccess` và `RecipeWriteService_PublishAsync_UpdatesStatus_AndRecordsMetric_OnSuccess`:
  Chứng minh rõ ràng `recipes.created` và `recipes.published` chỉ được kích hoạt sau khi `SaveChangesAsync` thành công, và hoàn toàn không bị gọi nếu ném `InvalidOperationException`.

---

## 45. Limitations
1. **Môi trường cục bộ không chạy sẵn OpenTelemetry Collector & Seq**: Tệp `docker-compose.yml` gốc của nhóm chỉ định nghĩa container PostgreSQL. Do đó, OTLP gRPC Exporter và Seq Sink được thiết kế có cơ chế chịu lỗi (Fault-tolerant): nếu collector/Seq không phản hồi, ứng dụng vẫn hoạt động bình thường và xuất log an toàn ra Console/File.
2. **Dashboard UI**: Trực quan hóa Prometheus/Grafana phụ thuộc vào việc cấu hình hạ tầng Kubernetes/Docker Compose mở rộng ở môi trường production.

---

## 46. 25 câu hỏi phản biện Giảng viên & Đáp án

### Câu 1: Tại sao em lại cần CorrelationId khi OpenTelemetry đã có TraceId?
**Đáp án**: TraceId là định danh cấp hạ tầng tuân theo chuẩn W3C, chủ yếu dùng cho tracing độ trễ qua các dịch vụ. CorrelationId là định danh cấp ứng dụng/nghiệp vụ, có thể được cấp phát từ phía client (frontend web/mobile) ngay khi người dùng bấm nút, giúp hỗ trợ khách hàng (Customer Support) tra cứu sự cố chỉ bằng một mã tra cứu duy nhất mà không phụ thuộc vào hạ tầng trace. Kết hợp cả hai giúp ta vừa tra cứu được log theo nghiệp vụ, vừa mở được trace waterfall kỹ thuật.

### Câu 2: Khi client gửi lên một header X-Correlation-ID chứa mã độc hoặc chuỗi quá dài, em xử lý thế nào?
**Đáp án**: Em đã hiện thực phương thức `CorrelationIdMiddleware.ResolveCorrelationId` sử dụng Regex `^[a-zA-Z0-9_\-]+$` và giới hạn độ dài tối đa 64 ký tự. Nếu client gửi chuỗi rác, chứa ký tự đặc biệt, xuống dòng (CRLF injection) hoặc token JWT, hệ thống lập tức loại bỏ và tự sinh một GUID chuẩn mới. Điều này đã được chứng minh qua unit test `CorrelationId_Sanitization_RejectsTokensAndPasswordsInHeader`.

### Câu 3: Làm thế nào để CorrelationId truyền xuyên suốt các tác vụ bất đồng bộ (async/await)?
**Đáp án**: Em sử dụng Serilog `LogContext.PushProperty("CorrelationId", correlationId)`. Serilog xây dựng `LogContext` trên nền tảng `AsyncLocal<T>` của .NET runtime, đảm bảo giá trị ngữ cảnh tự động lan truyền (flow) xuyên suốt toàn bộ cây thực thi bất đồng bộ mà không bị mất hoặc xung đột giữa các luồng.

### Câu 4: Làm sao em phân biệt được request của người dùng đã đăng nhập và người dùng ẩn danh trong log?
**Đáp án**: Tại `Program.cs`, trong `EnrichDiagnosticContext`, em kiểm tra `httpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? httpContext.User?.FindFirstValue("sub")`. Nếu user đã đăng nhập, thuộc tính `UserId` được nạp vào log context; nếu là anonymous, giá trị là null và không đưa thuộc tính này vào, đảm bảo không bao giờ bị lỗi NullReferenceException.

### Câu 5: Tại sao em lại chọn ngưỡng 500ms cho Slow Request Warning?
**Đáp án**: Ngưỡng 500ms là tiêu chuẩn vàng trong trải nghiệm người dùng web (theo nghiên cứu Nielsen Norman Group, phản hồi dưới 100ms cho cảm giác tức thì, dưới 1000ms giữ được luồng suy nghĩ của người dùng, và 500ms là ngưỡng cảnh báo suy giảm hiệu năng API cần can thiệp tối ưu). Đồng thời đây là yêu cầu kỹ thuật bắt buộc của đề tài Task 4.

### Câu 6: Làm thế nào em đảm bảo không log mật khẩu và dữ liệu nhạy cảm?
**Đáp án**: Em tuân thủ 3 nguyên tắc: (1) Không bao giờ dùng `JsonSerializer.Serialize` trên toàn bộ đối tượng request; (2) Trong `LoggingBehavior`, em chỉ ghi nhận metadata gồm `RequestName` và thời gian thực thi; (3) Sử dụng cấu hình Serilog tách biệt và kiểm soát chặt các trường thông tin trong DiagnosticContext.

### Câu 7: MediatR LoggingBehavior của em đặt ở vị trí nào trong pipeline?
**Đáp án**: Trong `DependencyInjection.cs`, em đăng ký `LoggingBehavior` trước `ValidationBehavior`. Do đó `LoggingBehavior` bọc ở lớp ngoài cùng, giúp đo lường được toàn bộ thời gian kể cả thời gian chạy validation, và ghi nhận được exception nếu validation thất bại hoặc handler ném lỗi.

### Câu 8: Tại sao không log Exception rồi nuốt luôn (swallow) mà lại phải `throw;`?
**Đáp án**: Trong `LoggingBehavior`, sau khi ghi log lỗi `_logger.LogError(ex, ...)`, bắt buộc phải `throw;` để ngoại lệ tiếp tục lan truyền lên Exception Handling Middleware của API nhằm chuyển đổi thành HTTP status code chuẩn (như 400 Bad Request hoặc 500 Internal Server Error) trả về cho client, đồng thời kích hoạt rollback transaction của cơ sở dữ liệu nếu có.

### Câu 9: Tracing trong EF Core hoạt động như thế nào?
**Đáp án**: Gói `OpenTelemetry.Instrumentation.EntityFrameworkCore` đăng ký lắng nghe `DiagnosticListener` nội bộ của EF Core (`Microsoft.EntityFrameworkCore`). Mỗi khi EF Core mở kết nối, biên dịch câu lệnh SQL, hoặc thực thi lệnh qua ADO.NET Npgsql, thư viện tự động tạo một Span con (child span) với tên lệnh, text SQL và thời gian thực thi.

### Câu 10: Tại sao Business Metrics lại phải đặt ở tầng Service/Application mà không đặt ở Controller?
**Đáp án**: Vì Business Metric đo lường hành vi nghiệp vụ cốt lõi (tạo/xuất bản công thức thành công). Nếu đặt ở Controller, metric có thể bị tính sai nếu request đi qua controller thành công nhưng tầng database bên dưới bị lỗi hoặc rollback. Đặt tại `RecipeWriteService` ngay sau `_unitOfWork.SaveChangesAsync` đảm bảo 100% metric phản ánh sự thật đã được lưu vào cơ sở dữ liệu.

### Câu 11: Nếu database lưu thất bại (throw Exception), metric của em có tăng không?
**Đáp án**: Tuyệt đối không. Em đã viết unit test `RecipeWriteService_CreateAsync_DoesNotRecordMetrics_WhenUnitOfWorkFails` và `RecipeWriteService_PublishAsync_DoesNotRecordMetric_WhenUnitOfWorkFails`. Vì dòng gọi metric nằm sau `await _unitOfWork.SaveChangesAsync(cancellationToken)`, khi SaveChanges ném ngoại lệ thì dòng lệnh ghi metric không bao giờ được chạm tới.

### Câu 12: Hệ thống của em tính Error Rate như thế nào?
**Đáp án**: Em cung cấp counter `http.requests.total` và counter `http.requests.errors`. Error Rate là chỉ số phái sinh (derived metric) được tính toán trên dashboard Prometheus/Grafana qua công thức $\frac{\text{errors}}{\text{total}}$. Đây là chuẩn mực thiết kế Observability, không nên tự sinh ra một metric rate cố định trong code vì rate phụ thuộc vào cửa sổ thời gian (1 phút, 5 phút, 1 giờ).

### Câu 13: OTLP Exporter kết nối qua giao thức gì và cổng nào?
**Đáp án**: Mặc định OTLP Exporter trong dự án sử dụng giao thức gRPC trên cổng 4317 (`http://localhost:4317`) với định dạng nhị phân Protocol Buffers, giúp tối ưu hiệu năng và giảm tải tài nguyên CPU/mạng.

### Câu 14: Nếu hạ tầng chưa dựng OpenTelemetry Collector thì API của em có bị crash không?
**Đáp án**: Không. Trong `OpenTelemetryExtensions.cs`, em bọc endpoint bằng `Uri.TryCreate` và OTel SDK có cơ chế retry/backoff chạy background không chặn luồng HTTP chính. Nếu Collector không chạy, OTel SDK âm thầm hủy gói tin mà không gây ảnh hưởng đến trải nghiệm của người dùng.

### Câu 15: Phân biệt sự khác nhau giữa Log Level Warning và Error trong thiết kế của em?
**Đáp án**: 
- `Information`: Các request bình thường, mã 2xx/3xx và thời gian xử lý $\le 500ms$.
- `Warning`: Các tình huống bất thường nhưng hệ thống vẫn tự xử lý được: client request lỗi (4xx), request chạy chậm ($> 500ms$).
- `Error`: Các lỗi nghiêm trọng: server exception chưa bắt được, lỗi cơ sở dữ liệu, hoặc HTTP status code $\ge 500$.

### Câu 16: Tại sao em lại dùng `UseSerilogRequestLogging` thay vì middleware tự viết từ đầu?
**Đáp án**: `UseSerilogRequestLogging` của package `Serilog.AspNetCore` đã được tối ưu hóa cực kỳ sâu bởi đội ngũ Serilog (sử dụng struct-based timing, giảm thiểu cấp phát bộ nhớ heap allocation, tích hợp sâu vào DiagnosticSource của ASP.NET Core). Em tận dụng engine tối ưu này và mở rộng bằng cách enrich các thuộc tính chuyên biệt của đồ án qua `EnrichDiagnosticContext`.

### Câu 17: Biến `Activity.Current` có thể bị `null` không? Em xử lý trường hợp đó thế nào?
**Đáp án**: Hoàn toàn có thể bị `null` nếu request không được lấy mẫu (sampled) hoặc tính năng tracing bị vô hiệu hóa. Do đó, trong code của `CorrelationIdMiddleware` và `Program.cs`, em luôn dùng toán tử kiểm tra an toàn: `var currentActivity = Activity.Current; if (currentActivity != null) ...` để đảm bảo không bao giờ bị lỗi NullReferenceException.

### Câu 18: Lớp `CulinaryBlogTelemetry` được đăng ký trong DI với vòng đời (Lifetime) nào? Tại sao?
**Đáp án**: Được đăng ký dưới dạng `Singleton` (`services.AddSingleton<ICulinaryBlogTelemetry, CulinaryBlogTelemetry>()`). Lý do là vì `Meter`, `ActivitySource`, các `Counter` và `Histogram` cần được giữ sống trong suốt vòng đời của ứng dụng để tích lũy số liệu liên tục. Tạo mới (Transient/Scoped) sẽ làm mất số liệu và gây lãng phí bộ nhớ.

### Câu 19: Trong unit test, làm sao em giả lập được một slow request mà không dùng `Thread.Sleep`?
**Đáp án**: Tuyệt đối không dùng `Thread.Sleep` vì nó chặn luồng của hệ điều hành. Trong unit test `LoggingBehaviorTests`, em dùng `await Task.Delay(550, ct)` trong mock delegate của `RequestHandlerDelegate`, vừa mô phỏng chính xác sự chậm trễ bất đồng bộ vừa an toàn cho tài nguyên máy tính.

### Câu 20: Các tags gắn vào metric `http.requests.total` gồm những gì?
**Đáp án**: Gồm 3 tags: `http.method` (GET/POST/...), `http.route` (đường dẫn tài nguyên), và `http.status_code` (200, 404, 500). Việc gắn tags này cho phép vẽ biểu đồ phân loại lưu lượng theo method hoặc theo mã phản hồi trên Grafana.

### Câu 21: Tại sao em không dùng `Stopwatch.ElapsedMilliseconds` mà lại dùng `stopwatch.Elapsed.TotalMilliseconds` trong HTTP metrics?
**Đáp án**: `ElapsedMilliseconds` là số nguyên (long), làm tròn mất phần thập phân. `TotalMilliseconds` là số thực (double), cung cấp độ chính xác cao đến mức micro-giây, rất hữu ích cho các API cực nhanh cần đo phân vị độ trễ p99.

### Câu 22: Tại sao trong MediatR `LoggingBehavior` lại không log toàn bộ các thuộc tính của Command?
**Đáp án**: Vì trong Command có thể chứa: (1) Mật khẩu người dùng (ví dụ: `RegisterCommand`, `ChangePasswordCommand`), (2) Mã thẻ ngân hàng / thông tin cá nhân PII, (3) Mảng byte hoặc Stream của file upload (khiến dung lượng log phình to hàng trăm megabytes). Do đó chỉ log `RequestName` là phương án chuẩn mực và an toàn nhất.

### Câu 23: Log cuốn chiếu (Rolling File) trong cấu hình của em hoạt động thế nào?
**Đáp án**: Trong `appsettings.json`, File Sink được cấu hình: `"path": "logs/culinaryblog-.log"`, `"rollingInterval": "Day"`, `"retainedFileCountLimit": 30`. Mỗi ngày Serilog tự động tạo một file log mới với đuôi ngày tháng (ví dụ: `culinaryblog-20261005.log`), và tự động xóa các file log cũ hơn 30 ngày để chống tràn ổ đĩa.

### Câu 24: Làm sao liên kết được 1 dòng log trong Seq với 1 trace trong Jaeger/Tempo?
**Đáp án**: Nhờ việc đẩy `TraceId` vào Serilog `LogContext`, mỗi log event trên Seq đều có trường `TraceId`. Ta có thể cấu hình Seq tạo một link chuyển hướng (Trace Link) trực tiếp sang giao diện Jaeger với URL: `http://localhost:16686/trace/{TraceId}`.

### Câu 25: Thành quả lớn nhất của em trong phần Observability này là gì?
**Đáp án**: Em đã xây dựng thành công một hệ thống Observability chuẩn chỉnh cấp Production, kết hợp nhịp nhàng giữa Structured Logging (Serilog), Correlation Context, MediatR Pipeline, và OpenTelemetry Tracing/Metrics. Toàn bộ mã nguồn tuân thủ Clean Architecture, đạt 115/115 tests passed, bảo vệ an toàn dữ liệu nhạy cảm và sẵn sàng đáp ứng yêu cầu vận hành thực tế.

---

## 47. Kịch bản bảo vệ thuyết trình 3 phút

> **Kính thưa Thầy/Cô và Hội đồng chấm đồ án,**  
> Em tên là **Võ Hùng Mạnh**, đại diện Nhóm 12 phụ trách hoàn thiện tính năng cuối cùng của Task 4: **Hệ thống Quan sát và Giám sát toàn diện — Observability**.
> 
> Trong các hệ thống phần mềm quy mô lớn, việc chỉ có monitoring thông thường là không đủ. Khi phát sinh lỗi hoặc chậm trễ, chúng ta cần một cơ chế Observability đa chiều để trả lời được câu hỏi **nguyên nhân gốc rễ là gì**. Em đã xây dựng giải pháp dựa trên 3 trụ cột cốt lõi:
> 
> **Thứ nhất: Structured Logging và Correlation ID**  
> Em tích hợp thư viện Serilog, chuyển toàn bộ log sang dạng có cấu trúc Key-Value lưu trữ ra Console, Rolling File và Seq. Đặc biệt, em thiết kế `CorrelationIdMiddleware` đặt ở đầu HTTP pipeline. Mỗi request đều được định danh bằng một `CorrelationId` an toàn. ID này được gắn vào header phản hồi, đưa vào `HttpContext`, và đẩy vào Serilog `LogContext` qua `AsyncLocal`. Nhờ đó, dù request đi qua bao nhiêu tác vụ bất đồng bộ, lập trình viên vẫn lọc ra toàn bộ hành trình chỉ bằng một truy vấn đơn giản.
> 
> **Thứ hai: Cảnh báo Slow Request > 500ms & MediatR LoggingBehavior**  
> Tuân thủ chặt chẽ tiêu chuẩn nghiệm thu, em cài đặt cơ chế phát hiện Slow Request ở cả hai tầng: tầng HTTP và tầng ứng dụng MediatR. Nếu một request mất trên 500ms, hệ thống lập tức nâng log level lên `Warning`. Lớp generic `LoggingBehavior` bọc ngoài MediatR pipeline giúp đo đạc chính xác thời gian thực thi, bắt lỗi exception, và tuân thủ nghiêm ngặt nguyên tắc bảo mật: tuyệt đối không log mật khẩu, token hay PII của người dùng.
> 
> **Thứ ba: Distributed Tracing và Business Metrics theo chuẩn OpenTelemetry**  
> Em tích hợp OpenTelemetry Tracing thu thập tự động từ ASP.NET Core, HttpClient, và Entity Framework Core Database, xuất qua giao thức chuẩn OTLP gRPC. Quan trọng hơn, em xây dựng lớp tập trung `CulinaryBlogTelemetry` để đo lường các **Chỉ số nghiệp vụ (Business Metrics)**: bao gồm bộ đếm `recipes.created` và `recipes.published`. Các bộ đếm này chỉ tăng khi thao tác cơ sở dữ liệu đã hoàn tất thành công, hoàn toàn không bị đếm sai khi transaction bị rollback.
> 
> Toàn bộ tính năng đã được kiểm thử nghiêm ngặt với bộ test gồm **115 test cases passed 100%**, mã nguồn biên dịch đạt **0 lỗi**, và tuân thủ tuyệt đối cấu trúc Clean Architecture.
> 
> Em xin chân thành cảm ơn Thầy/Cô đã lắng nghe và sẵn sàng trả lời các câu hỏi phản biện!
