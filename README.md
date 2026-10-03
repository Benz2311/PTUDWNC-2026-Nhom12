# Phạm Nguyễn Ngọc Phước — Recipe / Ingredient

Tài liệu này mô tả phạm vi Recipe / Ingredient đã được triển khai trong dự án Culinary Blog, gồm API, quy tắc nghiệp vụ, concurrency, lifecycle, cache, MinIO, sitemap, migration và kiểm thử.

## Phạm vi phụ trách

- `Recipe`
- `RecipeNutrition` — owned entity của `Recipe`.
- `RecipeIngredient`
- Các thành phần liên quan cần cho luồng công thức: `RecipeStep`, `RecipeImage`, slug history, audit log và sitemap.

## API

API được đăng ký dưới cả prefix `/api/v1` và alias `/api`. Các endpoint chính:

| Method | Endpoint | Quyền / điều kiện chính |
|---|---|---|
| `POST` | `/api/v1/recipes` | Đăng nhập; tác giả được gán từ user hiện tại; recipe mới ở trạng thái Draft. |
| `PUT` | `/api/v1/recipes/{id}` | Owner hoặc Admin; bắt buộc `If-Match`. |
| `PATCH` | `/api/v1/recipes/{id}/publish` | Owner hoặc Admin; bắt buộc `If-Match`; phải thỏa điều kiện publish. |
| `PATCH` | `/api/v1/recipes/{id}/unpublish` | Owner hoặc Admin; bắt buộc `If-Match`; chỉ chuyển Published về Draft. |
| `PATCH` | `/api/v1/recipes/{id}/archive` | Owner hoặc Admin; bắt buộc `If-Match`; chuyển sang Archived. |
| `PATCH` | `/api/v1/recipes/{id}/unarchive` | Owner hoặc Admin; bắt buộc `If-Match`; chỉ chuyển Archived về Draft. |
| `DELETE` | `/api/v1/recipes/{id}` | Owner hoặc Admin; xóa mềm. |
| `POST` | `/api/v1/admin/recipes/{id}/restore` | Admin; chỉ restore recipe đã xóa mềm và còn trong thời hạn retention. |
| `DELETE` | `/api/v1/admin/recipes/{id}/purge` | Admin; xóa vật lý recipe hiện có. |
| `POST` | `/api/v1/recipes/{id}/ingredients` | Owner hoặc Admin; bắt buộc `If-Match` của Recipe. |
| `PUT` | `/api/v1/recipes/{id}/ingredients/{ingredientId}` | Owner hoặc Admin; bắt buộc `If-Match` của Recipe. |
| `DELETE` | `/api/v1/recipes/{id}/ingredients/{ingredientId}` | Owner hoặc Admin; bắt buộc `If-Match` của Recipe. |
| `GET` | `/sitemap.xml` | Công khai; trả sitemap XML đã tạo. |

## Quy tắc nghiệp vụ Recipe

### Tạo Recipe

- `AuthorId` do server lấy từ user đang đăng nhập; client không thể tạo recipe thay user khác.
- Title bắt buộc, dài 5–200 ký tự; Content bắt buộc.
- Description có thể để trống, tối đa 2.000 ký tự.
- `PrepTimeMinutes > 0`, `CookTimeMinutes >= 0`, `Servings > 0`; Difficulty phải thuộc enum hợp lệ.
- Category phải tồn tại và chưa bị xóa mềm.
- Recipe mới luôn ở trạng thái `Draft`.
- Slug được tạo từ slug/title; nếu slug hiện hành hoặc slug lịch sử đã tồn tại, server thêm hậu tố tăng dần như `-2`, `-3`.
- Nutrition được lưu như owned entity. Nguồn Nutrition được server đặt là `Manual`; request không nhận trường `Source` do client điều khiển.

### Cập nhật Recipe và ETag

- Chỉ Owner hoặc Admin mới được sửa. Kiểm tra quyền diễn ra trước kiểm tra phiên bản để người không có quyền nhận `403`.
- Response chi tiết trả `ETag` dạng strong tag dựa trên PostgreSQL `xmin`, ví dụ `"42"`.
- Client gửi tag hiện tại trong `If-Match` khi cập nhật Recipe, thay đổi trạng thái hoặc quản lý ingredient/step/image.
- Thiếu hoặc không khớp phiên bản dẫn tới `409 Conflict`; client cần tải lại recipe rồi thử lại.
- Khi Recipe còn Draft, cập nhật có thể tạo slug mới; slug cũ được ghi vào `RecipeSlugHistory` và được giữ lại để không cấp phát lại.
- Published hoặc Archived Recipe không được đổi slug.
- Frontend đọc ETag từ response và gửi lại qua `If-Match` cho các thao tác cập nhật/trạng thái.

### Publish / Unpublish

- Publish yêu cầu email tác giả đã xác nhận; Admin có thể publish thay.
- Recipe cần có Category chưa xóa, field cơ bản hợp lệ, ít nhất một Ingredient và ít nhất một Step.
- Khi publish, `Status` thành `Published`; `PublishedAt` chỉ được gán lần đầu và được giữ lại khi unpublish.
- Gọi lại action khi Recipe đã ở trạng thái đích trả kết quả hiện tại (idempotent).
- Unpublish chỉ cho phép `Published → Draft`; unarchive chỉ cho phép `Archived → Draft`. Transition sai trạng thái trả `409`.

### Archive

- Archive đặt trạng thái `Archived`; Recipe Archived không được đưa vào tập nội dung công khai vì public query chỉ lấy Published Recipe.
- Unarchive đưa Archived Recipe về Draft, không tự publish lại.
- Các cập nhật trạng thái đi qua write flow và làm mất hiệu lực cache liên quan.

## Ingredient

- Hỗ trợ tạo, cập nhật và xóa ingredient theo Recipe.
- Từng thao tác yêu cầu Owner/Admin và `If-Match` của Recipe; ETag Recipe được cập nhật sau mutation.
- `Name` bắt buộc, tối đa 200 ký tự.
- `Quantity` nhận JSON number decimal hoặc `null`; nếu có thì phải lớn hơn 0.
- `Unit` có thể null, tối đa 50 ký tự; `Notes` có thể null, tối đa 500 ký tự.
- `SortOrder` không được âm.
- Giá trị phân số dạng chuỗi, ví dụ `"1/2"`, không phải JSON number và bị từ chối với `422 Unprocessable Entity`.

## Xóa mềm, Restore và Purge

### Xóa mềm

- Delete Recipe đặt `IsDeleted = true` và `DeletedAt = thời điểm UTC`.
- Không xóa Ingredient, Step, Image hay file MinIO ngay trong thao tác này.
- Cache Recipe, Search và Category được invalidation thông qua generation keys.

### Restore

- Chỉ Admin.
- Recipe phải đang xóa mềm; thời hạn retention là 30 ngày.
- Restore đặt `IsDeleted = false`, `DeletedAt = null`, ghi audit log và invalidation cache.
- Recipe đã quá retention không thể restore.

### Purge

- Chỉ Admin qua API; thao tác xóa vật lý do `IRecipePurgeService` thực hiện.
- Xóa các Ingredient, Step, Image và slug history của recipe; xóa file MinIO được quản lý bởi ứng dụng; tạo audit entry `Purge` và invalidation cache.
- Audit log được giữ lại sau khi Recipe bị xóa vật lý để phục vụ truy vết.
- Purge Job gọi cùng `IRecipePurgeService` với API purge.
- Purge tự động chỉ xử lý recipe đã xóa mềm và hết retention. Job chạy mỗi 24 giờ và mặc định **đang tắt** (`Recipes:PurgeJobEnabled = false`). Chỉ bật sau khi xác nhận retention và cấu hình môi trường triển khai.

## Cache và sitemap

### Cache invalidation

- Dùng generation keys cho detail/search thay vì quét Redis theo wildcard.
- Các write flow ảnh hưởng nội dung công khai invalidation generation của Recipe, Search, Category và sitemap.
- Sitemap XML được cache tối đa 6 giờ.

### Sitemap Generation (`JOB-003`)

- Hosted service tạo sitemap khi khởi chạy và làm mới định kỳ mỗi 6 giờ.
- URL Recipe chỉ gồm recipe `Published` và chưa xóa mềm.
- URL Category chỉ gồm category chưa xóa mềm, theo route `/categories/{slug}`.
- Sitemap không chứa Draft, Archived hoặc Deleted Recipe.
- Base URL lấy từ `Site:PublicBaseUrl` (mặc định `http://localhost:3000`).
- Endpoint công khai: `GET /sitemap.xml`.

## Migration và cấu hình cần lưu ý

Migration lifecycle:

```text
backend/src/CulinaryBlog.Infrastructure/Persistence/Migrations/20261003090000_AddRecipeLifecycleSupport.cs
```

Migration bổ sung `Recipes.DeletedAt`, bảng `RecipeSlugHistories` và `RecipeAuditLogs`; snapshot EF Core cũng được cập nhật. Cần áp dụng migration vào database mục tiêu trước khi dùng các endpoint lifecycle.

Các cấu hình liên quan trong `backend/src/CulinaryBlog.Api/appsettings.json`:

- `Recipes:PurgeJobEnabled`: mặc định `false`.
- `Site:PublicBaseUrl`: URL gốc dùng để tạo sitemap.
- `Minio:Endpoint`, `Minio:PublicEndpoint`, `Minio:BucketName`: dùng khi purge file do ứng dụng quản lý.
- `Redis:ConnectionString`: Redis dùng cho cache và generation keys.

Không bật purge job trên môi trường thật trước khi xác nhận chính sách retention, quyền MinIO và cấu hình database.

## Frontend liên quan

- `frontend/lib/api.ts` cung cấp fetch giữ lại response headers để client đọc ETag.
- `RecipeForm` gửi `If-Match` khi chỉnh sửa Recipe.
- `RecipeActions` gửi ETag khi publish/unpublish và cập nhật ETag từ response mới.
- Category page dùng route `/categories/[slug]`, là route được sitemap tham chiếu.

## Kiểm thử

Chạy backend tests từ thư mục gốc repository:

```powershell
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --nologo
```

Kiểm thử hiện có bao gồm:

- Recipe write service gọi repository và lưu thay đổi; ingredient/step/image operations được kiểm tra delegation và số lần save.
- EF model/indexes, `DeletedAt`, slug history, audit log và khả năng discover migration.
- ETag được phát theo `xmin`; stale ETag bị guard từ chối.
- Publish guard: tác giả chưa xác nhận bị từ chối, recipe hợp lệ cho phép tác giả đã xác nhận hoặc Admin.
- Ingredient quantity: JSON decimal và null được chấp nhận; phân số dạng string bị từ chối.
- PostgreSQL integration test cho quan hệ Recipe/Ingredient, owned Nutrition, replace children và soft delete.

PostgreSQL integration test yêu cầu `CULINARYBLOG_TEST_CONNECTION` trỏ tới database test riêng có tên kết thúc bằng `_test`. Nếu chưa cấu hình, test đó được skip. Test suite hiện **chưa phải** bộ kiểm thử HTTP end-to-end cho toàn bộ endpoint trong checklist; các nhánh quyền, state transition, restore/purge và sitemap còn cần thêm test tích hợp chuyên biệt.

Frontend production build:

```powershell
cd frontend
npm run build
```

### Kết quả xác minh gần nhất

- Backend: 21 tests, 20 passed, 1 skipped, 0 failed; test bị skip do chưa cấu hình PostgreSQL test database.
- Frontend: `npm run build` thành công.
- `git diff --check`: không phát hiện lỗi whitespace trong lần xác minh gần nhất.

## File triển khai chính

- API và validation: `backend/src/CulinaryBlog.Api/Endpoints/Recipes/RecipeEndpoints.cs`
- Recipe write service: `backend/src/CulinaryBlog.Application/Features/Recipes/RecipeWriteService.cs`
- Cache invalidation: `backend/src/CulinaryBlog.Infrastructure/Persistence/RecipeCacheInvalidator.cs`
- Purge service: `backend/src/CulinaryBlog.Infrastructure/Persistence/RecipePurgeService.cs`
- Sitemap generator: `backend/src/CulinaryBlog.Infrastructure/Persistence/SitemapGenerator.cs`
- Lifecycle migration: `backend/src/CulinaryBlog.Infrastructure/Persistence/Migrations/20261003090000_AddRecipeLifecycleSupport.cs`
- Tests: `backend/tests/CulinaryBlog.UnitTests/RecipePersistenceModelTests.cs`, `RecipeWriteServiceTests.cs`, `RecipePersistenceIntegrationTests.cs`
