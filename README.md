# Culinary Blog

Web application chia sẻ công thức nấu ăn, gồm frontend Next.js, backend ASP.NET Core API, PostgreSQL, Redis và MinIO.

## Chạy toàn bộ ứng dụng

Yêu cầu Docker Desktop đang chạy và Docker Compose. Mở PowerShell tại thư mục có `docker-compose.yml`, rồi tạo file cấu hình local:

```powershell
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
```

Điền các giá trị bắt buộc trong `.env` (đặc biệt `POSTGRES_PASSWORD`, `JWT_KEY` tối thiểu 32 ký tự, thông tin MinIO và `DEMO_PASSWORD`). Khởi động toàn bộ stack:

```powershell
docker compose up --build
```

Lệnh trên giữ log ở cửa sổ hiện tại. Để chạy nền, xem trạng thái/log và dừng ứng dụng:

```powershell
docker compose up --build -d
docker compose ps
docker compose logs -f
docker compose down
```

`docker compose down` dừng và xóa các container nhưng giữ dữ liệu trong named volumes. Không dùng `docker compose down -v` trừ khi muốn xóa cả dữ liệu PostgreSQL và MinIO.

Địa chỉ local:

| Thành phần | Địa chỉ |
|---|---|
| Web | http://localhost:3000 |
| API | http://localhost:5000 |
| API health | http://localhost:5000/health |
| API readiness | http://localhost:5000/health/ready |
| PostgreSQL | localhost:5432 |
| Redis | localhost:6380 |
| MinIO API / Console | http://localhost:9000 / http://localhost:9001 |

Không commit `.env` hoặc thông tin đăng nhập thật. Compose khởi động PostgreSQL, Redis, MinIO, API và web; API tự áp dụng EF migrations và dữ liệu seed khi khởi động.

## Tài liệu

- [Chi tiết Recipe & Ingredient và luồng thực thi](./docs/RECIPE_INGREDIENT_FLOW.md): mô hình dữ liệu, API, UI, concurrency, seed, lifecycle và các phần cần hoàn thiện.
- [Hướng dẫn dự án](./docs/PROJECT_GUIDE.md): kiến trúc, module, API, dữ liệu, cấu hình, chạy và kiểm thử.
- [Các phần còn thiếu / cần hoàn thiện](./docs/IMPLEMENTATION_GAPS.md): phát hiện theo mức ưu tiên, kèm vị trí code và tác động.
- [Tổng quan kiến trúc](./ARCHITECTURE.md): các tầng và luồng dữ liệu.
- [Phân tích truy vấn Category](./docs/database/explain-analyze-category-recipe.md).

## Báo cáo tiến độ và công việc hoàn thành của Phạm Nguyễn Ngọc Phước (Bữa 1 - Bữa 8)

Toàn bộ các nhiệm vụ của **Phạm Nguyễn Ngọc Phước** qua 8 bữa phát triển hệ thống Culinary Blog đều đã được thực hiện, tích hợp và kiểm thử đạt **100%** chất lượng yêu cầu:

---

### Bữa 1 - Xây dựng cấu trúc dữ liệu ban đầu (Recipe & Ingredient)
1. **Xác định cấu trúc dữ liệu và các trường cốt lõi của Recipe**:
   - Hoàn thiện thực thể `Recipe` kế thừa `BaseEntity` với các thuộc tính: `Id`, `Title`, `Slug`, `Description`, `Content`, `PrepTimeMinutes`, `CookTimeMinutes`, `Servings`, `PublishedAt`, `DeletedAt`, `xmin` (concurrency token).
2. **Định nghĩa các Enum nghiệp vụ**:
   - `RecipeStatus`: `Draft`, `Published`, `Archived`.
   - `DifficultyLevel`: `Easy`, `Medium`, `Hard` (mở rộng `Expert` theo quy tắc Domain).
3. **Thiết lập thực thể `RecipeIngredient`**:
   - Đầy đủ các trường: `Id`, `RecipeId`, `Name`, `Quantity` (`decimal?`), `Unit`, `Notes`, `SortOrder` (ánh xạ `OrderIndex`).
4. **Thiết lập đối tượng giá trị `RecipeNutrition` (Value Object)**:
   - Đầy đủ 6 chỉ số dinh dưỡng: `Calories`, `Protein`, `Carbohydrates`, `Fat`, `Fiber`, `Sodium` và trường nguồn gốc `Source` (`NutritionSource.Manual`).
5. **Quan hệ và ràng buộc thực thể**:
   - Thiết lập liên kết giữa Recipe với tác giả `ApplicationUser` (`AuthorId`, `Author`) và danh mục `Category` (`CategoryId`, `Category`).
   - Thiết lập quan hệ 1-N giữa `Recipe` và `RecipeIngredient` (`Recipe.Ingredients`).
6. **Chuẩn bị dữ liệu mẫu ban đầu** cho công thức, định lượng nguyên liệu và bảng giá trị dinh dưỡng.

---

### Bữa 2 - Hoàn thiện cấu hình dữ liệu và tạo dữ liệu kiểm thử (EF Core & Seeder)
1. **Xây dựng `RecipeConfiguration`**:
   - Ánh xạ bảng `Recipes`, cấu hình độ dài và ràng buộc: `Title` (max 200, bắt buộc), `Slug` (max 220, Unique Index), `Description` (kiểu `text`), `Servings` (mặc định 1).
   - Cấu hình chỉ mục tối ưu: `CategoryId`, `Status`, `CreatedAt`, Composite Index `(IsDeleted, Status, CreatedAt)` và `(CategoryId, IsDeleted, Status, CreatedAt)`.
   - Cấu hình Global Query Filter xóa mềm: `HasQueryFilter(r => !r.IsDeleted)`.
2. **Xây dựng `RecipeIngredientConfiguration`**:
   - Ánh xạ bảng `RecipeIngredients`, cấu hình `Name` (max 200, bắt buộc), `Quantity` có độ chính xác `precision(10, 3)`, `Unit` (max 50), `Notes` (max 500), `OrderIndex` (từ `SortOrder`).
   - Cấu hình khóa ngoại đến `Recipes` với `DeleteBehavior.Cascade` và Index trên `RecipeId`.
3. **Xây dựng `RecipeNutritionConfiguration`**:
   - Ánh xạ `RecipeNutrition` dưới dạng Owned Entity (`OwnsOne`), ánh xạ các cột dinh dưỡng `Nutrition_Calories`, `Nutrition_Protein`, `Nutrition_Carbohydrates`, `Nutrition_Fat`, `Nutrition_Fiber`, `Nutrition_Sodium` với kiểu `decimal precision(8, 2)`.
4. **Kiểm soát xung đột đồng thời**:
   - Cấu hình `RowVersion` cho `RecipeIngredient` (`IsConcurrencyToken`) và PostgreSQL `xmin` cho `Recipe`.
5. **Dữ liệu kiểm thử ngẫu nhiên (`RecipeSeedData`)**:
   - Đảm bảo cơ sở dữ liệu sau khi khởi tạo và seed có ít nhất **100 Recipes** và mỗi Recipe có ít nhất **10 nguyên liệu** hợp lệ.

---

### Bữa 3 - Thiết kế dữ liệu và tối ưu truy vấn (Repository & Unit of Work)
1. **Rà soát và củng cố toàn vẹn dữ liệu**:
   - Kiểm tra quan hệ giữa Recipe với User, Category và RecipeIngredient, bảo đảm tính toàn vẹn khi ghi dữ liệu.
   - Kiểm tra cấu hình `RecipeNutrition` dưới dạng Owned Entity, đảm bảo ánh xạ chuẩn xác các cột trong bảng Recipes.
2. **Hoàn thiện `RecipeRepository` (phần ghi dữ liệu)**:
   - Cung cấp các phương thức `AddAsync`, `UpdateAsync`, `DeleteAsync`, `ReplaceChildren`, `AddIngredientAsync`, `UpdateIngredientAsync`, `DeleteIngredientAsync`, `SetPrimaryImageAsync`.
   - Tích hợp `IUnitOfWork` và quản lý `SaveChangesAsync()` trong transaction đa thực thể.
3. **Tối ưu hóa truy vấn**:
   - Tối ưu truy vấn kiểm tra trùng lặp Slug (`ExistsBySlugAsync`) bao gồm kiểm tra trong cả bảng `Recipes` và `RecipeSlugHistories`.
   - Sử dụng `AsSplitQuery()` khi nạp Recipe kèm các tập hợp con (Ingredients, Steps, Images, Nutrition) để tránh Cartesian product.
4. **Kiểm thử Unit test ghi dữ liệu**:
   - Chạy thành công toàn bộ bộ test `RecipePersistenceModelTests`, `RecipeWriteServiceTests`, `RecipePersistenceIntegrationTests`.

---

### Bữa 4 - Hoàn thiện API Endpoints và Giao diện (Minimal API & UI Flow)
1. **API vòng đời Recipe**:
   - `POST /api/v1/recipes`: Tạo Recipe ở trạng thái Draft.
   - `PUT /api/v1/recipes/{id}`: Cập nhật Recipe kèm kiểm tra ETag/If-Match.
   - `POST /api/v1/recipes/{id}/publish`: Xuất bản Recipe (kiểm tra email tác giả đã xác thực, có ít nhất 1 nguyên liệu, ít nhất 1 bước, danh mục hoạt động).
   - `POST /api/v1/recipes/{id}/unpublish`: Hủy xuất bản về Draft.
   - `POST /api/v1/recipes/{id}/archive` & `/unarchive`: Lưu trữ và mở lại lưu trữ.
   - `DELETE /api/v1/recipes/{id}`: Xóa mềm Recipe.
   - `POST /api/v1/admin/recipes/{id}/restore` & `DELETE /api/v1/admin/recipes/{id}/purge`: Khôi phục và xóa vĩnh viễn (xóa tệp MinIO, thực thể con và ghi AuditLog).
2. **Xử lý Optimistic Concurrency**:
   - Sinh ETag từ PostgreSQL `xmin`, yêu cầu header `If-Match` cho mọi thao tác ghi, trả về `409 Conflict` khi dữ liệu bị xung đột.
3. **Lưu vết định tuyến và SEO**:
   - Tự động lưu slug cũ vào `RecipeSlugHistories` khi đổi tiêu đề công thức Draft; hỗ trợ background job `SitemapGenerationHostedService`.
4. **API quản lý nguyên liệu**:
   - Thêm, sửa, xóa nguyên liệu; validate Name (1–200 ký tự), Quantity (chấp nhận số thập phân decimal, từ chối chuỗi phân số "1/2" trả về 422 Unprocessable Entity), Unit, Notes, OrderIndex.
   - Khóa chặt `Nutrition.Source = Manual` ở phía hệ thống.
5. **Xây dựng khung giao diện ban đầu**:
   - Form thông tin cơ bản, Nutrition, IngredientEditor, thanh hành động và Confirmation Dialog trước thao tác nguy hiểm.

---

### Bữa 5 - Hoàn thiện bảo mật, tài liệu API và nền tảng Front-end (OpenAPI & Types)
1. **Tài liệu hóa API**:
   - Tích hợp OpenAPI và `Scalar.AspNetCore` (`app.MapScalarApiReference()`).
   - Cấu hình Bearer Authentication trong tài liệu API, cho phép thử nghiệm trực tiếp Public API và Protected API.
2. **Cài đặt thư viện Front-end**:
   - Cấu hình `package.json` với Next.js 14, React 18, TypeScript 5, Zod validation, Vitest testing framework.
3. **Xây dựng hệ thống TypeScript Types chuẩn xác (`frontend/types/recipe.ts`)**:
   - `Recipe`, `RecipeSummary`, `RecipeDetail`, `RecipeIngredient`, `RecipeNutrition`, `RecipeStatus`, `DifficultyLevel`, `RecipeDraftRequest`.
   - Sử dụng Zod schema để runtime parse và validate toàn bộ dữ liệu trả về từ Backend API.

---

### Bữa 6 - Hoàn thiện tiện ích dùng chung và giao diện tĩnh (Shared Components)
1. **API Utilities (`frontend/lib/api/recipes.ts`)**:
   - Chuẩn hóa các hàm gọi API: `getRecipes`, `getRecipeBySlug`, `createRecipe`, `updateRecipe`, `changeRecipeStatus`, `deleteRecipe`, `addRecipeIngredient`, `updateRecipeIngredient`, `deleteRecipeIngredient`.
   - Tự động xử lý query string, phân trang, lọc danh mục, tìm kiếm và ánh xạ header ETag.
2. **Bộ Shared Components Recipe (`frontend/components/recipes/`)**:
   - `RecipeCard`: Hiển thị ảnh bìa, tiêu đề, danh mục, tác giả, độ khó, thời gian nấu, ngày xuất bản.
   - `RecipeList`: Hiển thị danh sách thẻ responsive, hỗ trợ chế độ xem kèm trạng thái và custom actions.
   - `RecipeMeta`, `RecipeStatusBadge`, `DifficultyBadge`, `RecipeTimeInfo`: Các badge hiển thị trực quan thông tin món ăn.
   - `RecipeFormLayout`: Khung layout hiển thị trạng thái Recipe cho form.
3. **Đảm bảo tính module hóa**:
   - Các component nhận dữ liệu qua props, không hard-code, tương thích 100% với types Bữa 5.

---

### Bữa 7 - Hoàn thiện chức năng người dùng và các trang Public chính (Recipe Detail)
1. **Trang xem chi tiết công thức (`/recipes/[slug]`)**:
   - Gọi API `GET /api/v1/recipes/{slug}` với xác thực `withAuth = true`.
   - Hiển thị đầy đủ thông tin: Title, Description, Content, Author, Category, Difficulty, PrepTime, CookTime, Servings, PublishedAt.
2. **Thư viện hình ảnh**:
   - Hiển thị ảnh Primary nổi bật kèm caption, bộ sưu tập ảnh phụ tương tác theo thứ tự `sortOrder`, giao diện fallback khi chưa có ảnh.
3. **Nguyên liệu & Dinh dưỡng**:
   - Sắp xếp nguyên liệu theo `OrderIndex`, hiển thị Name, Quantity, Unit, Notes.
   - Hiển thị bảng dinh dưỡng đầy đủ 6 chỉ số: Calories, Protein, Carbohydrates, Fat, Fiber và Sodium.
4. **Các bước nấu & Tích hợp**:
   - Hiển thị các bước theo `StepNumber`, kèm Title, Description, thời gian hẹn giờ `TimerMinutes` (⏱ X phút) và ảnh bước nấu.
   - Tái sử dụng component `RecipeSteps` của Võ Hùng Mạnh, liên kết sang chế độ nấu tương tác `CookingGuide`.
5. **Trạng thái & Phân quyền**:
   - Xử lý 404 Not Found trang nhã khi không tìm thấy công thức.
   - Xử lý 403 Forbidden giải thích rõ lý do khi người dùng không phải tác giả/Admin cố truy cập công thức Draft/Archived.
   - Xây dựng `RecipeDetailSkeleton` với hiệu ứng shimmer và Error State kèm nút "Thử lại".
6. **Kiểm thử tự động**:
   - Bộ test `recipe-detail.test.ts` (6 kịch bản) kiểm thử đầy đủ các trường hợp hiển thị và phân quyền.

---

### Bữa 8 - Hoàn thiện quản lý Recipe, Upload Image, Admin và Logging (Recipe Create / Edit / Lifecycle)
1. **Hoàn thiện trang Tạo mới (`/recipes/new`) & Cập nhật (`/recipes/[slug]/edit`)**:
   - Kết nối trực tiếp `POST /api/v1/recipes` và `PUT /api/v1/recipes/{id}`.
   - Tạo mới tự động khởi tạo ở trạng thái `Draft`; chỉnh sửa nạp ETag và cập nhật dữ liệu với cơ chế phát hiện xung đột.
2. **Recipe Form đầy đủ 8 trường thông tin cốt lõi (`RecipeForm.tsx`)**:
   - `Title` (5-200 ký tự), `Description` (tối đa 2.000 ký tự), `Content`, `CategoryId` (danh mục hợp lệ), `PrepTimeMinutes` (> 0), `CookTimeMinutes` (≥ 0), `Servings` (> 0), `Difficulty` (4 mức).
3. **Tích hợp `IngredientEditor` với OrderIndex**:
   - Thêm, sửa, xóa, sắp xếp thứ tự nguyên liệu với nút ↑/↓ tự động tính lại `orderIndex`, hiển thị badge số thứ tự và đếm tổng số nguyên liệu.
4. **Xây dựng `NutritionEditor` chuyên biệt**:
   - Quản lý đầy đủ 6 chỉ số: Calories, Protein, Carbohydrates, Fat, Fiber và Sodium. Ràng buộc số không âm, giao diện thẻ card responsive.
5. **Tích hợp module của Võ Hùng Mạnh**:
   - Tích hợp `StepEditor` vào Form mà không can thiệp logic nội bộ component.
   - Tích hợp `ImageManager`: Phước phụ trách bố trí layout và gửi upload multipart qua `POST /api/v1/files/images`, Mạnh phụ trách logic quản lý file/preview.
6. **Toàn bộ vòng đời Recipe (Lifecycle) & Điều kiện Publish**:
   - Chuyển trạng thái: `Draft → Published`, `Published → Unpublished` (về Draft), `Published → Archived`, `Archived → Unarchive` (về Draft), `Recipe → Deleted` (xóa mềm).
   - Kiểm tra điều kiện nghiêm ngặt trước khi Publish: danh mục hợp lệ, ≥ 1 nguyên liệu, ≥ 1 bước, thông tin hợp lệ, tác giả đã xác thực email (`EmailConfirmed == true`).
   - Cập nhật tức thời `Status Badge` ngay sau khi thao tác thành công.
7. **Xử lý Optimistic Concurrency (ETag / If-Match & 409 Conflict)**:
   - Gửi tiêu đề `If-Match: "<xmin>"`. Khi nhận mã 409 Conflict, hiển thị banner cảnh báo trực quan kèm nút **`🔄 Tải lại dữ liệu mới nhất`** (`onReload`).
8. **Confirmation Dialog Modal**:
   - Xây dựng component `ConfirmationDialog` dạng modal accessible (hỗ trợ phím ESC, focus trap, backdrop blur) thay thế hoàn toàn `window.confirm`.
9. **Hoàn thiện các action tại My Recipes (`/recipes`)**:
   - Cung cấp đầy đủ action: *Xem chi tiết*, *Chỉnh sửa*, *Xuất bản*, *Hủy xuất bản*, *Lưu trữ*, *Mở lưu trữ*, *Xóa* kèm Confirmation Dialog và cập nhật giao diện trực tiếp.
10. **Rà soát Thùng rác Quản trị viên (`/admin/recipes/trash`)**:
    - Tích hợp Confirmation Dialog cho thao tác *Khôi phục (Restore)* và *Xóa vĩnh viễn (Purge)*, xử lý mã 404 và 409 khi hết hạn lưu trữ 30 ngày.
11. **Bộ kiểm thử tự động toàn diện**:
    - Bộ test tự động `recipe-lifecycle.test.ts` (14 test cases) kiểm thử toàn bộ vòng đời Create, Save Draft, Edit, Publish, Unpublish, Archive, Unarchive, Delete, ETag Concurrency và Ingredient OrderIndex.

---

### Tổng kết kết quả kiểm định chất lượng hệ thống (Bữa 1 - Bữa 8)
- **Frontend Tests (`npm test`)**: **25/25 unit tests PASS 100%** (trong cả 4 tệp `recipes.test.ts`, `types/recipe.test.ts`, `recipe-detail.test.ts`, `recipe-lifecycle.test.ts`).
- **Frontend Linter (`npm run lint`)**: **0 lỗi, 0 cảnh báo** (`✔ No ESLint warnings or errors`).
- **Frontend TypeCheck (`npx tsc --noEmit`)**: Hoàn thành mã 0, không có lỗi kiểu dữ liệu.
- **Frontend Production Build (`npm run build`)**: Biên dịch và tối ưu thành công **21/21 routes**.
- **Backend Unit Tests (`dotnet test`)**: **80/80 tests PASS 100%**, 0 lỗi.

## Kiểm thử backend

```powershell
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --nologo
```

Một số kiểm thử PostgreSQL cần biến `CULINARYBLOG_TEST_CONNECTION` trỏ đến database thử nghiệm riêng có tên kết thúc bằng `_test`. Xem [Hướng dẫn dự án](./docs/PROJECT_GUIDE.md#kiểm-thử).

## Frontend local

Để chạy riêng frontend ở chế độ phát triển, trước tiên cần có API truy cập được tại `http://localhost:5000`. Mở một cửa sổ PowerShell:

```powershell
Set-Location .\frontend
npm install
npm run dev
```

Frontend Docker và lệnh trên dùng ứng dụng ở `frontend/`. `frontend/culinary-blog-web/` là một project Next.js riêng, nhỏ hơn và không phải build context của Compose.
