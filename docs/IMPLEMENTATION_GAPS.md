# Phần còn thiếu / cần hoàn thiện

Đây là rà soát tĩnh source/config/test, không phải xác nhận mọi luồng trên một deployment đang chạy. “Chưa wire” nghĩa là có source nhưng không được đăng ký từ `Program.cs`; “mismatch” là route/DTO frontend và backend không tương thích theo code hiện tại.

## Ưu tiên cao

| Ưu tiên | Phát hiện | Bằng chứng trong code | Tác động |
|---|---|---|---|
| P1 | Dashboard, upload file và sitemap endpoint chưa được map khi khởi động API. | `Program.cs` chỉ gọi health/auth/category/recipe; `DashboardEndpoints`, `FileEndpoints`, `SitemapEndpoints` có extension map riêng nhưng không có call-site. | Route dashboard, upload và `/sitemap.xml` không được phục vụ dù source/consumer đã tồn tại. |
| P1 | Frontend Category dashboard chưa thể lấy thống kê. | `frontend/lib/api/categories.ts` gọi `/api/v1/categories/statistics`; active Category endpoint không có route này, còn Dashboard endpoint chưa được map. Dashboard backend (nếu map) chỉ cho Admin, nhưng frontend server-side client không gửi bearer token; DTO `DashboardResponse` cũng khác shape `CategoryStatistics` của UI. | Cần thống nhất route, authorization/token forwarding và DTO mapping trước khi dashboard hoạt động. |
| P1 | Luồng xác minh email chưa được triển khai dù publish yêu cầu email đã xác nhận. | `AuthEndpoints.Register` tạo user với `EmailConfirmed = true`; auth route hiện không có endpoint xác nhận email. | Bất kỳ người đăng ký nào cũng được xem là đã xác nhận; ràng buộc publish không chứng minh chủ sở hữu email. |

## Ưu tiên tiếp theo

| Ưu tiên | Phát hiện | Bằng chứng trong code | Hướng xử lý |
|---|---|---|---|
| P2 | Có hai frontend project với dependencies, routes và UI khác nhau. | `frontend/package.json` là Next 14/React 18 và được Compose build; `frontend/culinary-blog-web/package.json` là Next 15/React 19, phần category detail giả định response `{ category, recipes }` trong khi API trả Category DTO phẳng. | Chọn một app chuẩn, chuyển source cần giữ, rồi loại bỏ hoặc ghi rõ trạng thái app còn lại; contract mismatch hiện thuộc project lồng chưa được Compose dùng. |
| P2 | API rate limiting được khai báo trên routes nhưng không thấy policy/middleware đăng ký trong startup. | Recipe/Auth gọi `RequireRateLimiting`; không tìm thấy `AddRateLimiter`/`UseRateLimiter` trong backend C#. | Đăng ký named policies (`api`, `auth`) và middleware, hoặc gỡ metadata nếu không dùng. Xác minh bằng test startup và HTTP. |
| P2 | OpenAPI được thêm service nhưng không có endpoint/document UI mapping. | `Program.cs` gọi `AddOpenApi()`; không có `MapOpenApi()` hoặc Swagger UI mapping trong source startup. | Map OpenAPI theo environment và thêm hướng dẫn truy cập docs API. |
| P2 | Cấu hình `Cors:Origins` có trong cấu hình/Compose nhưng chưa thấy `AddCors`/`UseCors`. | Không tìm thấy đăng ký và middleware CORS trong backend C#. | Nếu browser gọi trực tiếp API, cấu hình policy có allowlist và test preflight; nếu chỉ dùng server-side proxy, mô tả rõ kiến trúc đó. |
| P2 | Reviews và quản lý Users mới là placeholder; chưa thấy API reset password/xác nhận email. | `AdminDataPlaceholder` được dùng ở `/admin/reviews` và `/admin/users`; Auth endpoints chỉ có register/login/refresh/logout/me; dashboard/reports/settings cần rà lại kết nối API. | Ưu tiên endpoint, authorization/policy, DTO, UI và test cho từng module hoặc đánh dấu rõ ngoài scope. |
| P2 | Category helper trong project frontend lồng nuốt lỗi và trả dữ liệu giả dạng rỗng/zero. | `frontend/culinary-blog-web/lib/api/categories.ts` catch lỗi rồi trả `[]`, `null` hoặc thống kê 0; frontend đang được Compose dùng không áp dụng fallback này. | Nếu project lồng được giữ lại, phân biệt empty success với backend/network/auth error và hiển thị retry/error state. |
| P2 | Test coverage chưa chứng minh các luồng HTTP chính. | Unit tests là phần lớn test backend; integration API hiện có Categories smoke test; một số PostgreSQL integration test skip nếu không cấu hình DB test. | Bổ sung API integration tests cho auth, Recipe CRUD/ETag/status, upload, dashboard và sitemap; thêm test frontend cho lỗi/loading/auth và contract Category. |
| P3 | Compose chưa khai báo healthcheck/dependency readiness. | `docker-compose.yml` dùng `depends_on` đơn giản cho backend dependencies. | Thêm healthchecks và điều kiện healthy hoặc retry/startup handling; hiện API tự migrate/seed trong startup. |
| P3 | MinIO image dùng tag `latest`. | `docker-compose.yml`. | Pin image version để local/CI builds có thể tái lập. |
| P3 | Category endpoint implementation trùng/legacy. | Có `Endpoints/CategoryEndpoints.cs` trong namespace `CulinaryBlog.API.Endpoints` và implementation khác trong `Endpoints/Categories/CategoryEndpoints.cs`; startup import/mapping dùng namespace `CulinaryBlog.Api.Endpoints.Categories`. | Xác định một implementation chuẩn, gỡ code cũ hoặc đánh dấu legacy; tránh thay đổi nhầm implementation không chạy. |

## Tài liệu và trạng thái hiện tại

- README cũ chứa các marker merge `<<<<<<<`, `=======`, `>>>>>>>` và nội dung trộn từ nhiều phiên bản; đã thay bằng hướng dẫn vào dự án và liên kết sang tài liệu chi tiết.
- `ARCHITECTURE.md` cũ mô tả một số page/feature và phiên bản frontend không phản ánh chính xác tree mà Compose build; đã cập nhật sơ đồ và chỉ rõ hai app frontend.
- Lượt xác minh gần nhất trước khi viết tài liệu: API build thành công; backend unit tests 80 passed, 1 skipped. Không có kết luận rằng toàn bộ Compose stack đã chạy thành công hoặc tất cả route đã được kiểm thử end-to-end.

## Lộ trình đề xuất

1. Chốt frontend root duy nhất; kiểm tra home page, route đích và trạng thái loading/error trên frontend Compose dùng.
2. Chốt API contract Category, map dashboard/file/sitemap thật sự cần dùng, rồi kiểm thử HTTP.
3. Đăng ký rate limit, CORS (nếu cần), OpenAPI và xác minh startup pipeline.
4. Bổ sung tests cho authentication/authorization, Recipe state/concurrency và integrations.
5. Thêm healthchecks cho Compose và cố định phiên bản image.
