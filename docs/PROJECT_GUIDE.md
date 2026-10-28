# Hướng dẫn dự án Culinary Blog

Tài liệu này mô tả cấu trúc code và hành vi có thể xác nhận trong repository. Các endpoint chỉ được xem là hoạt động khi được gọi từ `Program.cs`; có source endpoint riêng nhưng chưa đăng ký sẽ được ghi chú là chưa khả dụng.

## 1. Mục tiêu và công nghệ

Culinary Blog là ứng dụng web chia sẻ và quản lý công thức nấu ăn.

| Khu vực | Công nghệ |
|---|---|
| Web frontend đang được Compose dùng | Next.js 14, React 18, TypeScript |
| API | ASP.NET Core Minimal API, .NET 10 |
| Business/application | C#; có MediatR và FluentValidation |
| Persistence | EF Core 10, PostgreSQL |
| Cache | Redis |
| Object storage | MinIO |
| Background jobs | Hangfire |
| Local orchestration | Docker Compose |

Lưu ý có project frontend thứ hai tại `frontend/culinary-blog-web/` dùng Next.js 15/React 19. Compose trỏ tới `frontend/`, không phải project lồng đó.

## 2. Cấu trúc repository

```text
.
|-- backend/
|   |-- src/
|   |   |-- CulinaryBlog.Api/             HTTP endpoints, Program.cs
|   |   |-- CulinaryBlog.Application/     use cases, handlers, interfaces
|   |   |-- CulinaryBlog.Domain/          entities, enums, domain rules
|   |   `-- CulinaryBlog.Infrastructure/  EF Core, integrations, jobs
|   |-- tests/
|   |   |-- CulinaryBlog.UnitTests/
|   |   `-- CulinaryBlog.IntegrationTests/
|   `-- tools/CulinaryBlog.DbSeeder/
|-- frontend/
|   |-- app/                              Next.js App Router đang dùng
|   |-- components/                       UI theo module
|   |-- lib/                              API client, auth, helpers
|   |-- e2e/
|   `-- culinary-blog-web/                frontend project riêng/lồng
|-- infrastructure/docker/                Dockerfiles
|-- docs/
|-- docker-compose.yml
|-- .env.example
|-- ARCHITECTURE.md
`-- README.md
```

Các file build được tạo (`bin`, `obj`, `.next`, `node_modules`) không phải source.

## 3. Backend

### 3.1 Trách nhiệm các project

- **Domain**: `ApplicationUser`, `RefreshToken`, `Category`, `Recipe`, `RecipeNutrition`, `RecipeIngredient`, `RecipeStep`, `RecipeImage`, `RecipeSlugHistory`, `RecipeAuditLog`; enum gồm trạng thái Recipe, độ khó, vai trò và nguồn Nutrition.
- **Application**: các handler truy vấn Category/Recipe, service interface, validation behaviors, repository/service abstractions.
- **Infrastructure**: `ApplicationDbContext`, EF configurations/migrations, PostgreSQL repositories, dịch vụ auth, Redis/cache invalidation, MinIO, seeder và job nền.
- **API**: auth, category, recipe, dashboard, file, sitemap và health endpoint source. Danh sách đăng ký thực tế nằm trong `Program.cs`.

### 3.2 Endpoint được đăng ký trong `Program.cs`

| Nhóm | Đường dẫn hiện có | Ghi chú |
|---|---|---|
| Health | `/health`, `/health/live`, `/health/ready` | Ready kiểm tra PostgreSQL, Redis và bucket MinIO. |
| Authentication | `POST /api[/v1]/auth/register`, `/login`, `/refresh`; `POST /logout`; `GET /me`; `PATCH /me` | Register/login/refresh công khai; logout/profile cần JWT. |
| Category | `GET /api[/v1]/categories`, `GET /categories/{slug}`, `POST /categories`, `PUT /categories/{id}`, `DELETE /categories/{id}` | GET công khai; ghi yêu cầu role Admin. Chưa có `GET /categories/statistics` trong implementation đang map. |
| Recipe | Prefix `/api/recipes` và `/api/v1/recipes` | `GET /` list, `GET /search`, `GET /{slug}`, `POST /`, `PUT /{id}`, `DELETE /{id}`; quản lý ingredient/step/image; publish/unpublish/archive/unarchive. |
| Admin Recipe | Prefix `/api/admin/recipes` và `/api/v1/admin/recipes` | `GET /trash`, `POST /{id}/restore`, `DELETE /{id}/purge`; yêu cầu Admin. |
| Root | `/` | Trả trạng thái service đơn giản. |

Recipe update/state/child write dùng ETag/`If-Match` để phát hiện version cũ. Recipe mới là Draft; publish có các điều kiện nghiệp vụ và tác giả cần xác nhận email. Recipe deletion hỗ trợ soft delete, restore/purge có giới hạn retention.

Hiện đăng ký người dùng đặt `EmailConfirmed = true` ngay trong backend; chưa có route xác nhận email. Vì vậy điều kiện xác nhận email khi publish hiện chưa đại diện cho quy trình xác minh email thực tế.

### 3.3 Endpoint source chưa đăng ký

Các extension `MapDashboardEndpoints`, `MapFileEndpoints`, `MapSitemapEndpoints` tồn tại trong source nhưng không được gọi từ `Program.cs`:

- Dashboard API chưa thể truy cập dù có frontend dashboard và API client gọi thống kê.
- Upload hình ảnh chưa được phục vụ qua route API.
- `/sitemap.xml` chưa được phục vụ qua route API.

Chi tiết tác động và đề xuất được ghi trong [IMPLEMENTATION_GAPS.md](./IMPLEMENTATION_GAPS.md).

### 3.4 Persistence

EF migrations nằm trong `backend/src/CulinaryBlog.Infrastructure/Persistence/Migrations/`. API gọi `Database.MigrateAsync()` và khởi tạo seed trong lúc startup; do đó PostgreSQL phải sẵn sàng trước khi API chạy. Test PostgreSQL yêu cầu database chuyên biệt.

## 4. Frontend đang được triển khai

Source Compose dùng là `frontend/`. Các route chính quan sát được:

| Nhóm | Route |
|---|---|
| Public | `/`, `/about`, `/categories`, `/categories/[slug]` |
| Auth/profile | `/login`, `/register`, `/profile` |
| Recipe | `/recipes`, `/recipes/new`, `/recipes/[slug]`, `/recipes/[slug]/edit`, `/recipes/[slug]/cook` |
| Discovery | `/explore`, `/search`, `/categories/[slug]` |
| Admin | `/admin`, `/admin/recipes`, `/admin/recipes/trash`, `/admin/reports`, `/admin/reviews`, `/admin/settings`, `/admin/users` |
| Dashboard | `/dashboard/categories` |

API client `frontend/lib/api.ts` gửi bearer token, refresh token khi nhận 401 và giữ response headers cho các thao tác cần ETag. Một số trang admin Reviews/Users render `AdminDataPlaceholder`, không đọc dữ liệu backend.

## 5. Local setup và chạy

### 5.1 Chạy cả stack (đề xuất)

Yêu cầu Docker Desktop/Compose. Từ root `PTUDWNC-2026-Nhom12`:

```powershell
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
```

Đặt giá trị riêng, không dùng placeholder cho các biến bắt buộc:

- `POSTGRES_PASSWORD`
- `JWT_KEY` (ít nhất 32 ký tự)
- `MINIO_ROOT_USER`, `MINIO_ROOT_PASSWORD`
- `MINIO_ACCESS_KEY`, `MINIO_SECRET_KEY`
- `DEMO_PASSWORD`

Sau đó:

```powershell
docker compose up --build
docker compose ps
```

Compose mở web `localhost:3000`, API `localhost:5000`, PostgreSQL `localhost:5432`, Redis `localhost:6380`, MinIO API `localhost:9000` và console `localhost:9001`. Không commit `.env`.

### 5.2 Chạy frontend riêng

Frontend root dùng `frontend/`:

```powershell
Set-Location frontend
npm install
npm run dev
```

Để lấy dữ liệu thật, backend và dependencies (PostgreSQL/Redis/MinIO) cũng phải chạy. Mặc định API URL là `http://localhost:5000`; có thể cấu hình `NEXT_PUBLIC_API_URL`.

### 5.3 Chạy backend riêng

Yêu cầu .NET 10 SDK và PostgreSQL/Redis/MinIO đã chạy. Cấu hình `ConnectionStrings:DefaultConnection`, `Jwt:Key` (tối thiểu 32 ký tự), Redis và MinIO bằng User Secrets hoặc biến môi trường, rồi chạy:

```powershell
dotnet run --project backend/src/CulinaryBlog.Api/CulinaryBlog.API.csproj
```

`appsettings.Development.json` để connection string rỗng; thiếu connection string/JWT key sẽ làm API dừng khi startup. Không dùng credential mặc định cho môi trường chia sẻ hoặc production.

### 5.4 Địa chỉ kiểm tra

- Web: `http://localhost:3000`
- API root: `http://localhost:5000/`
- Health: `http://localhost:5000/health`
- Readiness: `http://localhost:5000/health/ready`

Readiness `503` nghĩa là PostgreSQL, Redis hoặc MinIO chưa sẵn sàng/cấu hình chưa đúng. OpenAPI service được đăng ký, nhưng hiện chưa có `MapOpenApi()`/UI mapping trong startup.

## 6. Cấu hình

`.env.example` liệt kê biến cho Compose. Nhóm cấu hình chính:

| Nhóm | Biến/cấu hình |
|---|---|
| PostgreSQL | `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_PORT` |
| JWT | `JWT_KEY`, `JWT_ISSUER`, `JWT_AUDIENCE` |
| MinIO | `MINIO_ROOT_USER`, `MINIO_ROOT_PASSWORD`, `MINIO_ACCESS_KEY`, `MINIO_SECRET_KEY`, `MINIO_BUCKET_NAME` |
| Web / seed | `FRONTEND_ORIGIN`, `DEMO_PASSWORD` |
| Frontend API | `NEXT_PUBLIC_API_URL` |

Compose tạo connection string cho container backend từ biến PostgreSQL. Chỉ dùng giá trị bí mật cục bộ; `.env.example` là mẫu cấu trúc, không phải bộ credential cho production.

## 7. Kiểm thử

### Backend unit tests

```powershell
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --nologo
```

Trong lượt xác minh gần nhất: 80 passed, 1 skipped. Test PostgreSQL dùng `CULINARYBLOG_TEST_CONNECTION`, bắt buộc database thử nghiệm có tên kết thúc `_test`.

### Backend integration tests

```powershell
dotnet test backend/tests/CulinaryBlog.IntegrationTests/CulinaryBlog.IntegrationTests.csproj --nologo
```

Integration project hiện có smoke test Category/API host; cần cấu hình môi trường database phù hợp.

### Frontend

Tại `frontend/`, có thể chạy `npm run lint` và `npm run build`. Package root hiện không khai báo script test. Một project frontend lồng riêng có Jest/Playwright scripts và test utility riêng; không được nhầm nó với frontend mà Compose build. File `critical-flows.spec.ts` hiện nằm trong project frontend lồng.

## 8. Các tài liệu liên quan

- [Implementation gaps và việc cần ưu tiên](./IMPLEMENTATION_GAPS.md)
- [Kiến trúc hệ thống](../ARCHITECTURE.md)
- [Phân tích EXPLAIN ANALYZE](./database/explain-analyze-category-recipe.md)
