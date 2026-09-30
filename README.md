# PTUDWNC-2026-Nhom12 - Culinary Blog

Tài liệu này ghi nhận phạm vi Recipe / Ingredient do Phạm Nguyễn Ngọc Phước phụ trách, dựa trên source hiện có. Trạng thái được chia thành **đã có**, **đã có một phần** và **chưa triển khai** để phân biệt implementation với yêu cầu còn lại.

## Công nghệ và cấu trúc liên quan

- Backend: ASP.NET Core Minimal API trên .NET 10; Entity Framework Core/Npgsql; PostgreSQL.
- Các tầng chính: `CulinaryBlog.Domain` (entity/enum), `CulinaryBlog.Application` (service/interface), `CulinaryBlog.Infrastructure` (EF Core/repository), `CulinaryBlog.Api` (HTTP endpoints).
- Frontend: Next.js App Router, React, TypeScript; dịch vụ phát triển có Redis và MinIO.
- Code Recipe: `backend/src/CulinaryBlog.Api/Endpoints/Recipes/RecipeEndpoints.cs`.
- Domain: `backend/src/CulinaryBlog.Domain/Entities/Recipe.cs`, `RecipeNutrition.cs`, `RecipeIngredient.cs`.
- Persistence: `backend/src/CulinaryBlog.Infrastructure/Persistence/ApplicationDbContext.cs`, `RecipeRepository.cs`.
- Application write flow: `backend/src/CulinaryBlog.Application/Features/Recipes/RecipeWriteService.cs`.

## Đã có

### Entity và persistence

- Có các entity `Recipe`, `RecipeNutrition`, `RecipeIngredient`; Recipe liên kết với tác giả và Category, đồng thời chứa collection ingredient, step và image.
- `RecipeNutrition` được cấu hình owned entity trong bảng Recipe; các giá trị số được lưu với precision `(8,2)`. `RecipeIngredient.Quantity` dùng precision `(10,3)`; `SortOrder` ánh xạ xuống cột `OrderIndex`.
- `ApplicationDbContext` khai báo các quan hệ, index tra cứu và concurrency token dạng `RowVersion` do ứng dụng tự gán.
- Có index unique trên slug; index cho lọc danh sách; GIN/trigram index cho title/description; index cho RecipeId của ingredient và thứ tự step/image.

### Recipe endpoints và nghiệp vụ hiện có

- `POST /api/v1/recipes`: chỉ nhận tác giả từ user đã xác thực, kiểm tra dữ liệu cơ bản/category, tạo Recipe ở trạng thái `Draft`.
- Slug được sinh từ slug hoặc title. Nếu slug đang được dùng, request bị từ chối với `409`; hiện chưa tự nối hậu tố.
- `PUT /api/v1/recipes/{id}`: kiểm tra owner/Admin, cập nhật Recipe và thay collection ingredient/step/image.
- Các endpoint publish, unpublish, archive, unarchive đã có. Publish yêu cầu ít nhất một ingredient và một step; các Recipe không ở trạng thái Published bị loại khỏi truy vấn public.
- `DELETE /api/v1/recipes/{id}` đặt `IsDeleted = true`; đây là soft delete, không xóa Recipe vật lý trong endpoint thông thường.
- `RecipeRepository` xử lý truy vấn tracked/no-tracking, slug, thay nội dung aggregate và thao tác entity con. `RecipeWriteService` lưu qua Unit of Work.

### Ingredient endpoints

- Đã có create/update/delete ingredient qua các endpoint dưới `/api/v1/recipes/{id}/ingredients`.
- Có kiểm tra quyền owner/Admin, tên bắt buộc tối đa 200 ký tự, quantity nullable nhưng nếu có phải lớn hơn 0, unit tối đa 50 ký tự và thứ tự không âm.
- Giá trị decimal được nhận dưới dạng JSON number theo kiểu .NET `decimal`.

### Nutrition source và model lifecycle mới bổ sung

- `RecipeNutrition.Source` hiện mặc định `Manual`; request DTO không nhận trường Source nên client chưa thể truyền giá trị này qua API.
- Đã thêm nền tảng source cho `DeletedAt`, PostgreSQL `xmin`, `RecipeSlugHistory` và `RecipeAuditLog` trong domain/EF model.
- Các mục trên **chưa được hoàn tất thành tính năng chạy được**: chưa có migration mới, chưa có API/service sử dụng slug history/audit, và chưa dùng ETag/If-Match với xmin.

### Kiểm thử hiện có

- `RecipeWriteServiceTests`: kiểm tra delegation và số lần gọi Unit of Work.
- `RecipePersistenceModelTests`: kiểm tra một số metadata/index của EF model.
- `RecipePersistenceIntegrationTests`: PostgreSQL integration test cho quan hệ, nutrition, ingredient, thay collection con và soft delete.
- `SlugTests`: kiểm tra chuyển tiếng Việt có dấu thành slug ASCII.
- Integration test cần `CULINARYBLOG_TEST_CONNECTION` trỏ tới database riêng có hậu tố `_test`; khi không cấu hình, test được skip.

Chạy test project:

```powershell
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --nologo
```

## Đã có một phần, cần hoàn thiện

- **Create Recipe:** các trường title, description, prep time, cook time, servings, difficulty và category đã được kiểm tra. Slug chưa tự thêm suffix khi trùng. Nutrition đang là owned entity và mặc định Manual ở object, nhưng cột `Nutrition_Source` cần migration để lưu được vào database.
- **Update Recipe:** quyền owner/Admin và cập nhật nội dung đã có. Chưa bắt buộc `If-Match`; chưa trả/kiểm tra ETag từ PostgreSQL `xmin`; chưa lưu slug cũ; chưa giới hạn đổi slug theo trạng thái Draft/Published/Archived.
- **Publish:** quyền, trạng thái và điều kiện có ingredient/step đã có. Chưa kiểm tra email tác giả xác nhận, category active khi publish, `If-Match` hoặc idempotency đầy đủ; `PublishedAt` hiện được gán lại mỗi lần publish.
- **Unpublish:** chuyển về Draft và giữ `PublishedAt`; chưa có If-Match và thao tác vẫn cập nhật thời điểm sửa/concurrency token khi gọi lặp.
- **Archive/Unarchive:** trạng thái được cập nhật và public query chỉ lấy Published. Unarchive hiện chuyển về Draft; chưa có cache invalidation.
- **Soft delete:** hiện đặt `IsDeleted` và `UpdatedAt`; chưa gán `DeletedAt`, chưa áp dụng retention và chưa invalidation recipe/search/category cache. Child entity và MinIO file không bị xóa bởi soft-delete hiện tại.
- **Ingredient:** API chưa nhận `Notes`, dù entity/schema có trường này. Chưa yêu cầu If-Match của Recipe. Chuỗi như `"1/2"` không phải JSON number hợp lệ cho decimal và hiện chưa có xử lý riêng để bảo đảm phản hồi `422`.

## Chưa triển khai

- `POST /api/v1/admin/recipes/{id}/restore` và quy tắc restore trong thời hạn retention.
- `DELETE /api/v1/admin/recipes/{id}/purge`: physical delete recipe/children, xóa MinIO files và ghi audit log.
- Application service dùng chung cho purge endpoint và purge background job.
- Invalidate cache Recipe, Search và Category khi status/delete/restore thay đổi.
- `JOB-003 Sitemap Generation`: sitemap Recipe chỉ gồm Published, kèm category pages; loại Draft, Archived và Deleted.
- Bộ test endpoint/service cho owner khác `403`, stale ETag `409`, email chưa xác nhận, thiếu ingredient/step, restore/purge, decimal invalid và sitemap job.

## Phạm vi hiện tại

Các endpoint được yêu cầu ở nhóm Recipe/Ingredient phần lớn đã có implementation cơ bản; những endpoint restore/purge và sitemap job chưa có. Các tiêu chí concurrency, retention, cache invalidation, audit và lưu lịch sử slug vẫn là phần công việc tiếp theo, không được xem là hoàn thành chỉ vì đã có model nền tảng.