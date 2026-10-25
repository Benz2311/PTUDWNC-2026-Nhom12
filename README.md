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

## Phạm Nguyễn Ngọc Phước - Chi tiết công thức theo Slug (Bữa 7)

Trong Bữa 7, Phạm Nguyễn Ngọc Phước phụ trách hoàn thiện trang xem chi tiết công thức nấu ăn theo Slug và tích hợp dữ liệu với Backend:

- Hoàn thiện trang xem chi tiết công thức theo Slug bằng API `GET /api/v1/recipes/{slug}`, truyền xác thực `withAuth = true` để hỗ trợ phân quyền người dùng.
- Hiển thị đầy đủ các thông tin cơ bản: Title, Description, Content, Author, Category, Difficulty (`DifficultyBadge`), PrepTimeMinutes, CookTimeMinutes, Servings (`RecipeTimeInfo`), trạng thái (`RecipeStatusBadge`) và ngày xuất bản PublishedAt (định dạng ngày chuẩn tiếng Việt).
- Tích hợp hệ thống Breadcrumbs điều hướng: *Trang chủ* > *Công thức* > *Danh mục* > *Tên công thức*.
- Hiển thị ảnh Primary của Recipe ở vị trí nổi bật có huy hiệu *Ảnh chính* và caption mô tả; hiển thị bộ sưu tập ảnh phụ theo thứ tự `sortOrder` với thumbnail có thể tương tác; có giao diện fallback trang nhã khi công thức chưa có ảnh.
- Sắp xếp danh sách nguyên liệu (Ingredients) đúng theo `OrderIndex` (`orderIndex ?? sortOrder`), hiển thị chi tiết Name, Quantity, Unit và Notes; hỗ trợ Empty State khi chưa có nguyên liệu.
- Hiển thị bảng thông tin dinh dưỡng (Nutrition) gồm đầy đủ 6 chỉ số: Calories (kcal), Protein (g), Carbohydrates (g), Chất béo (g), Chất xơ (g) và Natri (mg) khi công thức có dữ liệu; hiển thị thông báo phù hợp khi chưa có dinh dưỡng.
- Hiển thị các bước thực hiện đúng theo `StepNumber`, kèm Title, Description, thời gian hẹn giờ `TimerMinutes` (⏱ X phút) và hình ảnh bước chế biến (`imageUrl`) nếu có.
- Tái sử dụng component `RecipeSteps` thuộc Võ Hùng Mạnh ở chế độ hiển thị phù hợp mà không sửa logic nội bộ của component; liên kết trực tiếp sang chế độ nấu tương tác `CookingGuide` (`/recipes/[slug]/cook`).
- Hiển thị thông tin tác giả, Category với liên kết điều hướng lọc danh mục và liên kết quay lại danh sách công thức.
- Xử lý trường hợp Recipe không tồn tại bằng giao diện 404 Not Found thích hợp kèm các nút điều hướng khám phá món ăn khác hoặc về trang chủ.
- Xử lý quyền truy cập đối với công thức `Draft` hoặc `Archived` đúng theo Backend: chỉ cho phép Author sở hữu hoặc Admin xem khi có token xác thực; hiển thị màn hình 403 Forbidden giải thích rõ lý do kèm nút đăng nhập tài khoản khi người dùng không đủ quyền truy cập.
- Xây dựng component `RecipeDetailSkeleton` với hiệu ứng shimmer mô phỏng chính xác bố cục trang chi tiết; hoàn thiện giao diện Error State kèm nút "Thử lại" và các Empty State dùng chung.
- Hoàn thiện Responsive layout cho Recipe Detail: bố cục 2 cột trên Desktop (cột nội dung chính và bảng dinh dưỡng sticky cố định), tự động co giãn và chuyển sang 1 cột tối ưu trên Tablet và Mobile.
- Không triển khai chức năng Create/Edit Recipe trong Bữa 7.
- Kiểm thử tự động Recipe Detail tại `frontend/lib/api/recipe-detail.test.ts` bao phủ các trường hợp: Recipe Published đầy đủ, Recipe có/không có ảnh, Recipe có nhiều Step/Ingredient, Recipe không tồn tại (404) và Recipe không đủ quyền xem (403).

Kiểm tra Bữa 7: `npm run build`, `npm run lint` và `npm test` trong thư mục `frontend`; build API bằng `dotnet build backend/src/CulinaryBlog.Api/CulinaryBlog.API.csproj --no-restore`. Frontend build thành công 21 routes, lint 0 lỗi/cảnh báo, toàn bộ 11 tests đều đạt; backend build thành công 0 lỗi và 80 unit tests đều PASS.

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
