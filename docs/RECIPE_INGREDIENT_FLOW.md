# Tài liệu Recipe & Ingredient

Tài liệu này mô tả mô hình Recipe/Ingredient, luồng xử lý hiện được triển khai và các giới hạn còn lại. Nội dung được đối chiếu với source; việc build thành công không thay thế kiểm thử đầu-cuối với PostgreSQL, Redis và MinIO.

## 1. Phạm vi nghiệp vụ

Recipe là nội dung công thức do một tác giả tạo, thuộc một danh mục và có thể chứa nhiều nguyên liệu, bước chế biến, ảnh cùng thông tin dinh dưỡng. Một công thức được tạo mới ở trạng thái Draft; chỉ công thức thỏa điều kiện nghiệp vụ mới được Publish.

Vòng đời trạng thái:

```text
Draft --publish--> Published --unpublish--> Draft
  |                    |
  |                    +--archive--> Archived --unarchive--> Draft
  +--archive-----------------------------------------------> Archived

Draft / Published / Archived --delete mềm--> Thùng rác
Thùng rác --restore (Admin, trong hạn lưu)--> trạng thái trước khi xóa
Thùng rác --purge (Admin)--> xóa vật lý cùng dữ liệu con và file ảnh quản lý
```

Các trạng thái được định nghĩa trong `RecipeStatus`: `Draft`, `Published`, `Archived`. Mức độ khó hiện có `Easy`, `Medium`, `Hard`, **và `Expert`**. Vì vậy giá trị `Expert` đang được chấp nhận ở Domain/API dù yêu cầu ban đầu chỉ liệt kê ba mức đầu.

## 2. Mô hình dữ liệu

### Recipe

`Recipe` kế thừa `BaseEntity`, trong đó có `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted` và `RowVersion`. Các trường nghiệp vụ chính:

| Trường | Ý nghĩa / quy tắc |
|---|---|
| `AuthorId`, `Author` | Tác giả (`ApplicationUser`); khi tạo API lấy tác giả từ claim đăng nhập, không tin `AuthorId` client gửi lên. |
| `CategoryId`, `Category` | Danh mục công thức. Danh mục phải tồn tại và chưa bị xóa khi tạo/cập nhật. |
| `Title` | Bắt buộc, độ dài 5–200 ký tự ở validate API; DB giới hạn tối đa 200. |
| `Slug` | Đường dẫn thân thiện URL, tối đa 220 ký tự, unique index. Slug cũ được lưu trong `RecipeSlugHistory`. |
| `Description` | Mô tả ngắn; API giới hạn 2.000 ký tự, DB dùng kiểu `text`. |
| `Content` | Nội dung tổng quát, bắt buộc khi tạo/cập nhật. |
| `PrepTimeMinutes`, `CookTimeMinutes`, `Servings` | Thời gian chuẩn bị phải > 0; thời gian nấu >= 0; khẩu phần > 0. `Servings` mặc định 1 trong entity. |
| `Difficulty` | `Easy`, `Medium`, `Hard`, `Expert`. |
| `Status` | Mặc định `Draft`. |
| `PublishedAt` | Được đặt khi Publish lần đầu; code hiện giữ giá trị cũ nếu Publish lại. |
| `Ingredients`, `Steps`, `Images` | Các tập hợp con của công thức. |
| `Nutrition` | Value object/owned entity được lưu cùng hàng Recipe. |
| `DeletedAt` | Thời điểm xóa mềm, phục vụ thùng rác và retention. |
| `xmin` | PostgreSQL system column được dùng làm phiên bản ETag cho Recipe trong các endpoint hiện tại. |

`RecipeConfiguration` ánh xạ bảng `Recipes`; `Title` bắt buộc, `Slug` có unique index; có index phục vụ lọc category/status/ngày tạo và query filter `!IsDeleted`. Entity/configuration có các index tìm kiếm văn bản trên `Title` và `Description`.

### RecipeIngredient

`Recipe.Ingredients` có quan hệ 1-N với `RecipeIngredient`; xóa cứng Recipe cascade tới ingredient. Các trường hiện có:

| Trường | Ý nghĩa / quy tắc |
|---|---|
| `Id`, `RecipeId`, `Recipe` | Định danh và khóa ngoại đến Recipe. |
| `Name` | Bắt buộc, tối đa 200 ký tự. |
| `Quantity` | `decimal?` (cho phép bỏ trống), lưu precision `(10,3)`; nếu có phải > 0. |
| `Unit` | Tùy chọn, tối đa 50 ký tự. |
| `Notes` | Tùy chọn, tối đa 500 ký tự. |
| `SortOrder` | Trường Domain tương ứng `OrderIndex` trong database/API; không được âm. |
| `CreatedAt`, `UpdatedAt`, `IsDeleted`, `RowVersion` | Metadata; `RowVersion` là concurrency token do ứng dụng quản lý. |

Quantity trong request được đọc từ JSON number hoặc `null`; chuỗi như `"1/2"` bị từ chối. Client nên gửi số thập phân JSON, ví dụ `0.5`.

### RecipeNutrition

`RecipeNutrition` được cấu hình bằng EF Core `OwnsOne`, các giá trị lưu thành các cột trên bảng Recipe:

| Thuộc tính | Cột |
|---|---|
| `Calories` | `Nutrition_Calories` |
| `Protein` | `Nutrition_Protein` |
| `Carbohydrates` | `Nutrition_Carbohydrates` |
| `Fat` | `Nutrition_Fat` |
| `Fiber` | `Nutrition_Fiber` |
| `Sodium` | `Nutrition_Sodium` |

Các cột cấu hình precision `(8,2)`. `Source` mặc định là `Manual`; API không lấy Source từ client và đặt lại thành Manual khi map request.

**Chênh lệch hợp đồng hiện tại:** `RecipeNutrition` và DB có `Sodium`, nhưng `NutritionRequest`, `NutritionResponse` và form frontend hiện không có Sodium. `Source` cũng không xuất hiện trong response DTO hiện tại. Cần thống nhất hợp đồng nếu muốn cho phép đọc/ghi các trường đó từ UI.

### Thực thể liên quan

- `RecipeStep`: bước có số thứ tự, tiêu đề, mô tả, thời gian hẹn giờ và ảnh tùy chọn; có unique index theo `(RecipeId, StepNumber)` cho bước chưa xóa.
- `RecipeImage`: ảnh gắn với Recipe, có URL, mô tả thay thế và thứ tự/primary image.
- `RecipeSlugHistory`: lưu slug cũ để URL cũ có thể được phân giải.
- `RecipeAuditLog`: ghi hành động vòng đời, restore và purge.
- `Category` và `ApplicationUser`: quan hệ nhiều Recipe về một Category và một Author.

## 3. Cấu hình lưu trữ và concurrency

EF Core cấu hình các entity qua `RecipeConfiguration`, `RecipeIngredientConfiguration`, `RecipeNutritionConfiguration`, `RecipeStepConfiguration` và configuration liên quan. Recipe soft-delete được lọc mặc định qua global query filter; các luồng Admin dùng truy vấn phù hợp để đọc bản ghi đã xóa.

Optimistic concurrency của Recipe được biểu diễn ra HTTP bằng ETag:

1. API trả `ETag: "<xmin>"` khi đọc chi tiết hoặc sau khi cập nhật.
2. Client cần gửi lại `If-Match: "<xmin>"` khi thay đổi/xóa/chuyển trạng thái hoặc CRUD ingredient/step/image.
3. Nếu thiếu hoặc không khớp phiên bản hiện tại, endpoint trả `409 Conflict`.

`RecipeIngredient.RowVersion` được đánh dấu concurrency token trong EF và được `ApplicationDbContext` cập nhật khi entity thay đổi. Tuy nhiên API ingredient kiểm tra ETag trên Recipe cha trước khi ghi; client cần đồng bộ một cơ chế phiên bản chung cho toàn bộ nội dung công thức. Recipe cũng có `RowVersion` trong `BaseEntity`, nhưng ETag HTTP của Recipe hiện được tạo từ `xmin`.

Các thao tác ghi qua `RecipeWriteService` gọi repository rồi `IUnitOfWork.SaveChangesAsync()`. Khi thay toàn bộ nội dung Recipe, service phối hợp Recipe với ingredient/step/image rồi lưu. Một số endpoint cũng ghi audit log hoặc cập nhật ETag sau thao tác.

## 4. Luồng chạy ứng dụng và seed

Khi API khởi động, `Program.cs`:

1. Đọc và xác thực cấu hình `Jwt:Key` (tối thiểu 32 ký tự).
2. Đăng ký authentication/authorization và infrastructure.
3. Map Health, Auth, Category và Recipe endpoints.
4. Chạy `Database.MigrateAsync()`.
5. Gọi `RecipeSeedData.InitializeAsync()`.

Seed dữ liệu hiện không dùng Bogus. `RecipeSeedData` dùng `Random` với seed cố định để tạo dữ liệu có thể lặp lại:

1. Tạo hoặc đảm bảo demo user và các category mẫu.
2. Đếm Recipe hiện có, tạo thêm cho tới khi tổng số đạt ít nhất 100.
3. Tạo dữ liệu Recipe/Nutrition và danh sách ingredient.
4. Quét toàn bộ Recipe, bổ sung ingredient để mỗi Recipe đạt ít nhất 10.
5. Đảm bảo có ảnh mẫu cho Recipe.

Seed yêu cầu `Seed:DemoPassword` được cấu hình và có ít nhất 12 ký tự. Tài khoản demo được đánh dấu `EmailConfirmed = true`; đây là dữ liệu demo, không phải luồng xác minh email dành cho đăng ký người dùng thật.

## 5. Luồng tạo và chỉnh sửa trên giao diện

Frontend chính nằm trong thư mục `frontend/` (đây cũng là build context được Compose dùng). Luồng form đang có:

1. Màn hình tạo/chỉnh sửa dùng `RecipeForm`; màn hình chỉnh sửa tải chi tiết qua `RecipeEditor`.
2. Form tải danh mục từ `GET /api/v1/categories`.
3. Người dùng nhập thông tin cơ bản, ingredient, step, nutrition và chọn ảnh.
4. Khi có ảnh mới, form gửi `POST /api/v1/files/images` dưới dạng `multipart/form-data`, rồi đưa URL ảnh vào request Recipe.
5. Form gửi toàn bộ nội dung trong một request:
   - Tạo mới: `POST /api/v1/recipes`.
   - Chỉnh sửa: `PUT /api/v1/recipes/{id}`.
6. Khi thành công, chuyển hướng tới `/recipes/{slug}`. Thanh hành động hỗ trợ lưu nháp, publish, unpublish, archive, unarchive và xóa; thao tác nguy hiểm yêu cầu xác nhận.

Frontend gửi ingredient có `name`, `quantity`, `unit`, `notes` và thứ tự; API chấp nhận `sortOrder`/`orderIndex` theo DTO hiện hành.

### Luồng xử lý POST tạo Recipe

1. Yêu cầu phải đăng nhập; backend lấy `AuthorId` từ JWT claim.
2. Validate tiêu đề, nội dung, category, thời gian, khẩu phần, difficulty, nutrition và từng ingredient.
3. Kiểm tra tác giả/category có tồn tại; tạo slug duy nhất, bao gồm slug của Recipe đã soft-delete và slug đã được lưu trong lịch sử.
4. Tạo Recipe ở trạng thái Draft; map nutrition, images, steps và ingredients.
5. `RecipeWriteService.CreateAsync` gọi repository và Unit of Work để lưu.
6. Trả `201 Created`, `Id` và `Slug`, đồng thời thiết lập ETag.

### Luồng xử lý PUT cập nhật Recipe

1. Tải Recipe và nội dung con phục vụ cập nhật; trả 404 nếu không tồn tại.
2. Xác minh người dùng là tác giả hoặc Admin.
3. Đọc và kiểm tra `If-Match`; thiếu/sai phiên bản trả 409.
4. Validate request; slug không đổi được với Recipe đã Published/Archived.
5. Nếu slug Draft đổi, lưu slug cũ vào `RecipeSlugHistory` và tạo slug duy nhất mới.
6. Cập nhật các trường gốc và thay toàn bộ tập hợp ingredient/step/image theo payload.
7. Lưu qua `RecipeWriteService`, sau đó trả `Id`, `Slug` và ETag mới.

`RecipeEditor` lưu ETag từ response header; `RecipeForm` gửi `If-Match` khi cập nhật và cập nhật lại ETag sau thao tác. Nếu nhận 409 do phiên bản cũ, giao diện báo người dùng tải lại dữ liệu trước khi thử tiếp. Khi slug của Draft thay đổi, slug cũ được lưu trong `RecipeSlugHistory` và GET bằng URL cũ phân giải về Recipe hiện tại.

## 6. Luồng API Recipe và trạng thái

Các route Recipe được map ở cả prefix `/api/recipes` và `/api/v1/recipes`; nhóm Admin tương ứng là `/api/admin/recipes` và `/api/v1/admin/recipes`. Frontend hiện sử dụng prefix `/api/v1`.

| Thao tác | Endpoint v1 | Quyền / kết quả |
|---|---|---|
| Danh sách, lọc, phân trang | `GET /api/v1/recipes` | Public chỉ thấy Published; tác giả thấy thêm Recipe của mình; Admin có thể lọc status. |
| Tìm kiếm | `GET /api/v1/recipes/search` | Trả công thức Published, có thể lọc category và phân trang. |
| Chi tiết | `GET /api/v1/recipes/{slug}` | Published public; Draft/Archived chỉ tác giả hoặc Admin. Trả ETag. |
| Tạo | `POST /api/v1/recipes` | Đăng nhập; luôn tạo Draft. |
| Cập nhật | `PUT /api/v1/recipes/{id}` | Tác giả/Admin, bắt buộc `If-Match`. |
| Publish / unpublish | `POST /api/v1/recipes/{id}/publish` và `/unpublish` | Tác giả/Admin; yêu cầu `If-Match`. |
| Archive / unarchive | `POST /api/v1/recipes/{id}/archive` và `/unarchive` | Tác giả/Admin; yêu cầu `If-Match`. |
| Xóa mềm | `DELETE /api/v1/recipes/{id}` | Tác giả/Admin; yêu cầu `If-Match`. |
| Thêm ingredient | `POST /api/v1/recipes/{id}/ingredients` | Tác giả/Admin; validate và kiểm tra ETag. |
| Sửa/xóa ingredient | `PUT` hoặc `DELETE /api/v1/recipes/{recipeId}/ingredients/{ingredientId}` | Tác giả/Admin; kiểm tra ETag của Recipe cha. |
| Thêm/sửa/xóa step | `/api/v1/recipes/{id}/steps` và `/steps/{stepId}` | Tác giả/Admin; kiểm tra ETag; step có mô tả bắt buộc. |
| Chọn ảnh chính/xóa ảnh | `PATCH .../images/{imageId}/primary`, `DELETE .../images/{imageId}` | Tác giả/Admin; kiểm tra ETag. |
| Danh sách thùng rác | `GET /api/v1/admin/recipes/trash` | Chỉ Admin. |
| Khôi phục | `POST /api/v1/admin/recipes/{id}/restore` | Chỉ Admin, trong retention 30 ngày; ghi audit log. |
| Xóa vĩnh viễn | `DELETE /api/v1/admin/recipes/{id}/purge` | Chỉ Admin; chỉ nhận Recipe đã soft-delete; xóa data con và object ảnh thuộc MinIO quản lý. |

Publish kiểm tra:

- Tác giả tồn tại và `EmailConfirmed == true`.
- Các trường Recipe hợp lệ và category còn hoạt động.
- Có tối thiểu một ingredient và một step.
- Giá trị dinh dưỡng không âm; ingredient có tên, quantity nếu có phải dương, Unit/Notes đúng giới hạn.

Thay đổi trạng thái và audit log được lưu trong cùng transaction. Các thay đổi nội dung/lifecycle invalidate cache sau khi ghi thành công.

## 7. Xóa mềm, restore, purge và sitemap

### Xóa mềm và restore

Xóa mềm đánh dấu `IsDeleted` và thời gian xóa, không loại bỏ nội dung khỏi database. Query filter loại Recipe đó khỏi các truy vấn thông thường. Admin có thể xem thùng rác, restore Recipe trong vòng 30 ngày; restore xóa cờ soft-delete, ghi audit log và invalidate cache.

### Purge

Purge tải Recipe cùng Images/Ingredients/Steps, xóa object ảnh nếu URL thuộc MinIO endpoint và bucket đã cấu hình, xóa các thực thể con và slug history, ghi audit log, rồi xóa Recipe. Audit log được chủ ý giữ lại sau khi Recipe bị xóa vật lý.

`RecipePurgeHostedService` được đăng ký để dọn Recipe soft-delete hết retention. Purge thủ công qua Admin API chỉ chấp nhận Recipe đã soft-delete.

Purge xóa object ảnh thuộc MinIO trước khi lưu thay đổi PostgreSQL; hai hệ thống không nằm trong cùng transaction phân tán, do đó lỗi một phía có thể yêu cầu phục hồi thủ công.

### Sitemap

Có `SitemapGenerator`, endpoint `GET /sitemap.xml` và `SitemapGenerationHostedService` (chu kỳ 6 giờ); endpoint và hosted service được map/đăng ký trong startup. `FileEndpoints` cũng được map để form upload ảnh. Cần xác minh khả năng kết nối các dịch vụ phụ thuộc ở môi trường triển khai thực tế.

## 8. Trạng thái triển khai theo yêu cầu

| Hạng mục | Hiện trạng đối chiếu source |
|---|---|
| Entity Recipe / Ingredient / Nutrition và quan hệ | Đã có; Ingredient quantity nullable, cột DB `OrderIndex` ánh xạ từ `SortOrder`. |
| Enum trạng thái / độ khó | Đã có; Difficulty còn thêm `Expert`. |
| Cấu hình EF, owned nutrition, index, soft delete | Đã có phần lớn; xem các configuration tương ứng. |
| RowVersion / concurrency | Ingredient có `RowVersion` EF token; Recipe API concurrency dựa trên PostgreSQL `xmin` + ETag/If-Match. |
| Dữ liệu seed 100 Recipe, 10 ingredient mỗi Recipe | Được bảo đảm bởi seed khi API khởi động và chạy migrations; dữ liệu tạo bằng `Random`, không Bogus. |
| API create/update, lifecycle, ingredient/step/image CRUD | Có trong Recipe endpoints; yêu cầu auth/ownership, validation, concurrency tùy route. |
| UI create/edit cơ bản | Có form gộp thông tin, ingredient, steps, nutrition, ảnh. |
| UI trạng thái Publish/Archive/Unpublish/Unarchive | Đã có thanh hành động trong form; thao tác nguy hiểm yêu cầu xác nhận. |
| UI trash Admin | Có xem, restore và purge; purge có hộp xác nhận. |
| Upload ảnh từ form | File endpoint đã map; cần MinIO cấu hình đúng để kiểm chứng end-to-end. |
| ETag round-trip khi edit | Frontend lấy ETag, gửi `If-Match` và báo lỗi 409. |
| Nutrition Sodium | Có trong entity, mapping, DTO và form. |
| Tác vụ nền purge hết hạn | Hosted service đã được đăng ký. |
| Sitemap nền | Endpoint/hosted service đã được map/đăng ký. |
| Email confirmation thật | Chưa có; đăng ký hiện đánh dấu email đã xác nhận ngay. Điều kiện Publish chưa được bảo vệ bởi quy trình xác minh thật. |

## 9. Kiểm thử và lệnh chạy

Các test hiện có gồm unit test cho entity/write service, query handler, persistence model và integration persistence. Integration persistence (bao gồm kiểm tra slug không bị tái sử dụng sau soft-delete) chỉ chạy khi `CULINARYBLOG_TEST_CONNECTION` trỏ đến database thử nghiệm chuyên dụng có tên kết thúc bằng `_test`. Chưa có xác minh end-to-end toàn bộ đường đi HTTP/UI với các dịch vụ ngoại vi; cần bổ sung kiểm thử create/edit, validation 422, lifecycle, If-Match 409, restore/purge, ảnh và sitemap.

Chạy unit tests từ root repository:

```powershell
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --nologo
```

Chạy toàn bộ ứng dụng bằng Docker Compose:

```powershell
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
# Cập nhật các secret và cấu hình bắt buộc trong .env trước khi chạy.
docker compose up --build
```

Chi tiết setup chung ở [README](../README.md); tài liệu kiến trúc hiện trạng ở [PROJECT_GUIDE](./PROJECT_GUIDE.md).

## 10. Thứ tự hoàn thiện khuyến nghị

1. Triển khai xác minh email thật trước khi cho phép Publish, thay vì tự đánh dấu đã xác nhận khi đăng ký.
2. Bổ sung test API/frontend cho các kịch bản đường đi đầy đủ và lỗi 409/422/403/404.
3. Xác minh upload/purge ảnh với MinIO và xử lý lỗi một phần giữa MinIO và PostgreSQL.
4. Xác minh startup, cache invalidation, migration và hosted services trong môi trường chạy thực tế.
