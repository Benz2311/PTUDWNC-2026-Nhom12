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

## Bữa 8 - Phạm Nguyễn Ngọc Phước: Recipe Create / Edit / Lifecycle, Ingredient & Nutrition

Trong Bữa 8 (**Hoàn thiện quản lý Recipe, Upload Image, Admin và Logging**), Phạm Nguyễn Ngọc Phước chịu trách nhiệm Recipe Create/Edit, Ingredient, Nutrition và toàn bộ vòng đời Recipe. Tất cả các mục được hoàn thành 100%:

1. **Trang Tạo mới (`/recipes/new`) & Cập nhật Recipe (`/recipes/[slug]/edit`)**:
   - Kết nối trực tiếp với các API Backend: `POST /api/v1/recipes`, `PUT /api/v1/recipes/{id}`, các endpoint chuyển trạng thái và `GET /api/v1/categories`.
   - Tạo mới tự động khởi tạo công thức ở trạng thái `Draft`; chỉnh sửa cho phép cập nhật dữ liệu và duy trì hoặc cập nhật slug lịch sử (`RecipeSlugHistory`).

2. **Recipe Form hoàn chỉnh với đầy đủ 8 trường thông tin cốt lõi**:
   - `Title`: Bắt buộc, kiểm tra độ dài 5 - 200 ký tự.
   - `Description`: Mô tả ngắn gọn giới thiệu món ăn, tối đa 2.000 ký tự.
   - `Content`: Bắt buộc, nội dung tổng quát quy trình nấu nướng.
   - `CategoryId`: Chọn danh mục từ danh sách API Backend, kiểm tra hợp lệ.
   - `PrepTimeMinutes`: Thời gian chuẩn bị > 0 phút.
   - `CookTimeMinutes`: Thời gian nấu >= 0 phút.
   - `Servings`: Khẩu phần ăn > 0 người.
   - `Difficulty`: 4 mức độ theo enum Domain (`Easy`, `Medium`, `Hard`, `Expert`).

3. **Tích hợp `IngredientEditor`**:
   - Thêm, sửa, xóa và sắp xếp nguyên liệu linh hoạt bằng các nút di chuyển (↑, ↓) tự động cập nhật lại `orderIndex`.
   - Hỗ trợ đầy đủ các trường: `name` (1 - 200 ký tự), `quantity` (số thập phân dương hoặc để trống), `unit` (tối đa 50 ký tự), `notes` (tối đa 500 ký tự) và `orderIndex`.
   - Hiển thị badge số thứ tự (#1, #2...) và đếm tổng số nguyên liệu hợp lệ.

4. **Xây dựng và tích hợp `NutritionEditor`**:
   - Thành phần chuyên biệt quản lý đầy đủ 6 chỉ số dinh dưỡng: `Calories` (kcal), `Protein` (g), `Carbohydrates` (g), `Fat` (g), `Fiber` (g) và `Sodium` (mg).
   - Ngăn chặn nhập số âm, bố cục lưới thẻ responsive trực quan và có chú thích cách tính theo khẩu phần.

5. **Tích hợp module của Võ Hùng Mạnh**:
   - Tích hợp `StepEditor` vào Recipe Form mà không thay đổi bất kỳ logic nội bộ nào của component.
   - Tích hợp `ImageManager` vào Recipe Form tuân thủ nguyên tắc: Phước chịu trách nhiệm bố trí layout, Mạnh chịu trách nhiệm logic Step/Image.

6. **Vòng đời trạng thái (Recipe Lifecycle)**:
   - Hỗ trợ đầy đủ các bước chuyển trạng thái: `Draft → Published`, `Published → Unpublished` (về Draft), `Published → Archived`, `Archived → Unarchive` (về Draft), và `Recipe → Deleted` (xóa mềm vào Thùng rác).
   - Kiểm tra điều kiện tiên quyết trước khi Publish: Category hợp lệ, ít nhất 1 Ingredient, ít nhất 1 Step, thông tin cơ bản hợp lệ và Author đã xác thực Email (`EmailConfirmed == true`).
   - Cập nhật tức thời trạng thái trên giao diện (`Status Badge`) và cập nhật ETag từ HTTP response header.

7. **Xử lý Optimistic Concurrency (ETag / If-Match & 409 Conflict)**:
   - Gửi tiêu đề `If-Match: "<xmin>"` trong mọi thao tác PUT, đổi trạng thái và xóa công thức.
   - Phát hiện lỗi 409 Conflict khi xảy ra cập nhật đồng thời, hiển thị banner cảnh báo trực quan kèm nút *"🔄 Tải lại dữ liệu mới nhất"* giúp đồng bộ dữ liệu mà không gây mất tính nhất quán.

8. **Confirmation Dialog chuyên nghiệp**:
   - Xây dựng component `ConfirmationDialog` dạng modal accessible (hỗ trợ phím ESC, focus trap, backdrop blur) thay thế hoàn toàn `window.confirm`.
   - Áp dụng đồng bộ cho các thao tác: Xuất bản, Hủy xuất bản, Lưu trữ, Mở lưu trữ, Xóa công thức, Khôi phục (Restore) và Xóa vĩnh viễn (Purge).

9. **Hoàn thiện các action tại My Recipes (`/recipes`)**:
   - Mỗi thẻ công thức cung cấp đầy đủ các action tùy theo trạng thái: *Xem chi tiết*, *Chỉnh sửa*, *Xuất bản*, *Hủy xuất bản*, *Lưu trữ*, *Mở lưu trữ* và *Xóa*.
   - Thực hiện qua ETag và cập nhật trực tiếp danh sách tại client không cần reload toàn trang.

10. **Rà soát Thùng rác Quản trị viên (`/admin/recipes/trash`)**:
    - Tích hợp `ConfirmationDialog` cho các thao tác *Khôi phục (Restore)* và *Xóa vĩnh viễn (Purge)* cùng thông báo kết quả chi tiết.

11. **Kiểm thử tự động toàn diện**:
    - Bộ test tự động `frontend/lib/api/recipe-lifecycle.test.ts` (14 kịch bản) kiểm thử toàn bộ vòng đời Create, Save Draft, Edit, Publish, Unpublish, Archive, Unarchive, Delete, ETag Concurrency và Ingredient OrderIndex.
    - Tất cả 25 unit tests Vitest và toàn bộ 80 backend unit tests đều PASS 100%. Next.js build hoàn thành 21/21 routes không lỗi.

Kiểm tra Bữa 8:
- Chạy frontend tests: `npm test` trong thư mục `frontend` -> **4 test files passed, 25 tests passed (100%)**.
- Chạy frontend lint: `npm run lint` trong thư mục `frontend` -> **No ESLint warnings or errors**.
- Build production frontend: `npm run build` trong thư mục `frontend` -> **21/21 routes static/dynamic compiled successfully**.
- Chạy backend unit tests: `dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --nologo` -> **80 tests passed, 0 failed**.

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
