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

## Phạm Nguyễn Ngọc Phước - API Utilities / Recipe Shared Components (Bữa 6)

Trong Bữa 6, Phạm Nguyễn Ngọc Phước phụ trách hoàn thiện tiện ích gọi API và xây dựng các thành phần giao diện Recipe có thể tái sử dụng:

- Tiếp tục dùng API Client đã có từ Bữa 5; tập trung các hàm truy vấn, tìm kiếm và thao tác Recipe trong `frontend/lib/api/recipes.ts`, không tạo API Client mới.
- Hỗ trợ tạo query string với phân trang, tìm kiếm, danh mục, trạng thái và sắp xếp; xử lý response theo các Recipe Type/schema dùng chung.
- Chuẩn hóa các thao tác Recipe như tạo, cập nhật, đổi trạng thái, xóa và quản lý nguyên liệu/bước/ảnh; các thao tác có kiểm soát phiên bản sử dụng ETag và `If-Match`.
- Xây dựng các component dùng chung `RecipeCard`, `RecipeList`, `RecipeMeta`, `RecipeStatusBadge`, `DifficultyBadge` và `RecipeTimeInfo`. Dữ liệu được truyền qua props, các component sử dụng Recipe Types hiện có thay vì khai báo type nghiệp vụ trùng lặp.
- Tích hợp Recipe Shared Components vào trang chủ, trang khám phá/tìm kiếm, trang danh mục và khu vực quản lý công thức. Recipe Card hiển thị ảnh, tên món, danh mục, tác giả, độ khó, thời gian chuẩn bị/nấu và ngày xuất bản khi API có dữ liệu.
- Hoàn thiện giao diện Recipe Detail với thông tin công thức, ảnh và thư viện ảnh, nguyên liệu, dinh dưỡng và các bước chế biến.
- Sắp xếp Recipe Form thành các khu vực thông tin cơ bản, nguyên liệu, các bước, dinh dưỡng, ảnh và trạng thái công thức. `StepEditor` và `ImageManager` được tích hợp vào bố cục; không thay đổi logic bên trong hai component này.
- Bổ sung ngày xuất bản vào response DTO và Recipe Type để giao diện hiển thị đúng ngày xuất bản, không dùng ngày tạo thay thế.
- Bổ sung kiểm thử serialize query và mở rộng cấu hình Vitest để nhận diện kiểm thử trong `lib/`.

Kiểm tra Bữa 6: `npm run build`, `npm run lint` và `npm test` trong thư mục `frontend`; build API bằng `dotnet build backend/src/CulinaryBlog.Api/CulinaryBlog.API.csproj --no-restore`. Frontend build, lint và 5 kiểm thử đều đạt; backend build thành công, còn các cảnh báo analyzer hiện có.

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
