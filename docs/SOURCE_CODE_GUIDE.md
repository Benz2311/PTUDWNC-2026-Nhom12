# Hướng dẫn mã nguồn toàn hệ thống Culinary Blog

Tài liệu này là bản đồ để đọc và thay đổi mã nguồn của hệ thống: từ ứng dụng web, API, nghiệp vụ, cơ sở dữ liệu tới hạ tầng, kiểm thử và cách chạy. Nội dung được đối chiếu với source/configuration hiện có; “được map” nghĩa là endpoint được gọi trong `Program.cs`, không đồng nghĩa mọi luồng đã được kiểm thử trên deployment thật.

## 1. Tổng quan hệ thống

Culinary Blog là ứng dụng chia sẻ và quản lý công thức nấu ăn. Frontend chính dùng Next.js, API dùng ASP.NET Core Minimal API, dữ liệu quan hệ lưu ở PostgreSQL, cache qua Redis, ảnh lưu ở MinIO. Backend còn tích hợp email, Hangfire và OpenTelemetry.

```text
Trình duyệt
  └─ frontend/ (Next.js App Router)
       └─ HTTP/JSON + JWT → backend/src/CulinaryBlog.Api
            ├─ Application: use case, handler, validation, contracts
            ├─ Domain: entity, enum, quy tắc miền
            └─ Infrastructure
                 ├─ PostgreSQL / EF Core
                 ├─ Redis hoặc distributed-memory cache
                 ├─ MinIO / S3-compatible storage
                 ├─ Hangfire / background work
                 └─ email, health checks, telemetry
```

Docker Compose build `frontend/` và backend API. `frontend/culinary-blog-web/` là một project riêng lồng bên trong frontend, không phải frontend mà Compose build.

## 2. Bản đồ repository

| Thư mục / file | Vai trò |
|---|---|
| `backend/CulinaryBlog.sln` | Solution .NET gồm API, Domain, Application, Infrastructure, tests và công cụ seed. |
| `backend/src/CulinaryBlog.Api/` | Điểm khởi chạy ứng dụng, HTTP endpoints, middleware, OpenAPI và hosted services. |
| `backend/src/CulinaryBlog.Application/` | Use case, MediatR handlers, DTO, validator và abstraction cho persistence/integration. |
| `backend/src/CulinaryBlog.Domain/` | Entity, enum, exception và logic nghiệp vụ cốt lõi. |
| `backend/src/CulinaryBlog.Infrastructure/` | EF Core, migrations được cấu hình cho build, repositories, auth, cache, storage, email, background jobs và telemetry. |
| `backend/tests/CulinaryBlog.UnitTests/` | Unit tests cho domain, application và một số integration service. |
| `backend/tests/CulinaryBlog.IntegrationTests/` | Test API qua `WebApplicationFactory`; cấu hình test thay PostgreSQL bằng EF InMemory. |
| `backend/tools/CulinaryBlog.DbSeeder/` | Công cụ seed database độc lập. |
| `frontend/app/` | Route và layout của frontend chính theo Next.js App Router. |
| `frontend/components/` | Component giao diện theo recipe, discovery, auth, admin, layout và UI dùng chung. |
| `frontend/lib/` | API client và các hàm truy cập endpoint. |
| `frontend/types/` | TypeScript types/schema dùng để xác nhận dữ liệu API. |
| `frontend/culinary-blog-web/` | Frontend thứ hai, có package/dependency và app độc lập. |
| `infrastructure/docker/` | Dockerfile cho API và frontend. |
| `docs/` | Hướng dẫn, đặc tả, luồng Recipe, phân tích DB và báo cáo công việc. |
| `docker-compose.yml`, `.env.example` | Khai báo stack local và tên biến cấu hình mẫu. |

`bin/`, `obj/`, `.next/`, `node_modules/` là output/dependency, không phải mã nguồn cần chỉnh sửa.

## 3. Backend

### 3.1 API và composition root

`backend/src/CulinaryBlog.Api/Program.cs` là composition root: kiểm tra JWT key, đăng ký authentication/authorization, CORS, rate limiting, Problem Details, JSON enum, OpenAPI, Application và Infrastructure; sau đó cấu hình middleware, map endpoint và khi chạy bình thường thì áp dụng EF migrations cùng dữ liệu seed.

Thứ tự xử lý request gồm exception handler, status-code pages, routing, CORS, rate limiter, authentication và authorization. API cũng đăng ký hosted service dọn Recipe hết thời hạn và sinh sitemap.

OpenAPI/Scalar được bật ở Development hoặc khi `ApiDocumentation:Enabled` được bật. Các endpoint có yêu cầu xác thực được bổ sung JWT security metadata và tài liệu có thể đánh dấu `If-Match` bắt buộc cho endpoint tương ứng.

### 3.2 Các endpoint đang được map

Các nhóm dưới đây được gọi trực tiếp hoặc thông qua extension từ `Program.cs`. Chi tiết request/response nên được xác nhận từ DTO và handler trong source; OpenAPI khi được bật là tài liệu tương tác cho runtime.

| Nhóm | Route | Chức năng / quyền |
|---|---|---|
| Health | `GET /health`, `/health/live`, `/health/ready` | Trạng thái ứng dụng; readiness kiểm tra DB, cache và bucket MinIO. |
| Authentication | `/api/v1/auth/*`, `/api/auth/*` | Đăng ký, đăng nhập, Google login, refresh token, logout, email confirmation, profile, đổi/quên/đặt lại mật khẩu. `/me`, logout và change-password yêu cầu JWT. |
| Categories | `/api/v1/categories/*`, `/api/categories/*` | Danh sách, thống kê, chi tiết theo slug, tạo/sửa/xóa. Các thao tác ghi yêu cầu role `Admin`. |
| Recipes | `/api/v1/recipes/*` | Danh sách, lọc theo category, tìm kiếm, chi tiết, tạo/sửa/xóa, trạng thái, ingredient, step và image. Các thao tác ghi recipe yêu cầu đăng nhập; chi tiết ghi có thể yêu cầu `If-Match`. |
| Recipe admin | `/api/v1/admin/recipes/*` | Danh sách thùng rác, restore và purge; yêu cầu role `Admin`. |
| Files | `POST /api/v1/files/images` và alias `/api/files/images` | Upload ảnh; yêu cầu JWT. |
| Sitemap | `GET /sitemap.xml` | Trả sitemap do service sinh và cache. |
| Admin users | `/api/v1/admin/users/*` | Liệt kê user và cập nhật trạng thái; yêu cầu quyền Admin theo endpoint group. |
| Root | `GET /` | Trả trạng thái dịch vụ đơn giản. |

Các route Recipe chính:

- Đọc: `GET /api/v1/recipes/`, `/category/{categorySlug}`, `/search`, `/{slug}`.
- Vòng đời: `POST /`, `PUT /{id}`, `DELETE /{id}`, cùng `POST /{id}/publish`, `/unpublish`, `/archive`, `/unarchive`.
- Nguyên liệu: `POST /{id}/ingredients`, `PUT` hoặc `DELETE /{recipeId}/ingredients/{ingredientId}`.
- Bước nấu: `POST /{id}/steps`, upload ảnh bước, `PUT /{id}/steps/{stepId}`, reorder và delete.
- Ảnh: thêm, sửa metadata, đặt primary và xóa qua `/images/...`.
- Admin: `/api/v1/admin/recipes/trash`, `/{id}/restore`, `/{id}/purge`.

Recipe dùng `xmin`/ETag và `If-Match` cho kiểm soát xung đột ở các thao tác được triển khai. Client cần giữ ETag mới nhất từ response và gửi lại khi cập nhật; xung đột cần tải lại resource trước khi thử lại.

### 3.3 Endpoint source chưa được gọi từ startup

Không nên suy ra endpoint hoạt động chỉ từ việc có file source. Trong `Program.cs` hiện tại:

- `DashboardEndpoints.MapDashboardEndpoints` chưa được gọi; dashboard backend có source nhưng route dashboard chưa được bật.
- `UserEndpoints` ở thư mục `Endpoints/Users` không được map; phần admin user đang dùng `AdminUsersEndpoints`.
- `Endpoints/CategoryEndpoints.cs` (namespace legacy `CulinaryBlog.API.Endpoints`) tồn tại song song với endpoint đang dùng trong `Endpoints/Categories/CategoryEndpoints.cs`. Startup map phiên bản trong namespace `CulinaryBlog.Api.Endpoints.Categories`.

Khi bổ sung endpoint mới, cần đăng ký extension trong `Program.cs` và cập nhật API client/test phù hợp.

### 3.4 Application và Domain

`CulinaryBlog.Application` điều phối nghiệp vụ qua MediatR. `AddApplication()` tự quét handler và validator trong assembly, đăng ký `LoggingBehavior` và `ValidationBehavior`, cùng Recipe write service. Các feature được nhóm theo Auth, Categories, Recipes và Search; command/query dùng DTO thay vì trả trực tiếp entity cho client.

`CulinaryBlog.Domain` chứa các đối tượng cốt lõi:

| Entity / nhóm | Ý nghĩa |
|---|---|
| `ApplicationUser`, `RefreshToken` | Tài khoản, role, thông tin đăng nhập và phiên refresh. |
| `Category` | Danh mục công thức, slug, mô tả, hình và thứ tự hiển thị. |
| `Recipe` | Công thức, tác giả, danh mục, trạng thái, thời gian, độ khó và các tập hợp con. |
| `RecipeIngredient`, `RecipeStep`, `RecipeImage` | Nguyên liệu, bước nấu và ảnh gắn với công thức. |
| `RecipeNutrition` | Các chỉ số dinh dưỡng của công thức. |
| `RecipeSlugHistory`, `RecipeAuditLog` | Lịch sử slug và dấu vết thao tác quản trị/vòng đời. |

Trạng thái recipe gồm Draft, Published và Archived; độ khó có Easy, Medium, Hard và Expert. Recipe/category dùng soft-delete theo logic entity/persistence. Domain exceptions biểu đạt các lỗi như not found, conflict, forbidden và sai quy tắc nghiệp vụ; lớp API/middleware chuyển lỗi thành phản hồi HTTP phù hợp.

### 3.5 Infrastructure và persistence

`AddInfrastructure()` đăng ký:

- EF Core/Npgsql và `ApplicationDbContext`.
- Repositories cho recipe/category/user/refresh token và Unit of Work.
- Authentication/JWT services và password hasher.
- Cache distributed: Redis nếu có cấu hình; nếu không đăng ký distributed-memory cache. `ResilientCacheService` cung cấp abstraction cache ứng dụng.
- MinIO client và S3-compatible storage service.
- Hangfire với PostgreSQL ngoài môi trường Testing; HTTP client, image resize job và email service.
- Health checks cho PostgreSQL, Redis, MinIO; OpenTelemetry/logging được đăng ký qua extension riêng.

`ApplicationDbContext` khai báo DbSet cho các entity, tải EF configurations từ assembly và cập nhật concurrency tokens khi lưu. Search recipe hỗ trợ PostgreSQL full-text/trigram qua extension và index cấu hình ở model/migrations.

Migrations được lưu tại cả `Infrastructure/Migrations/` và `Infrastructure/Persistence/Migrations/`. `CulinaryBlog.Infrastructure.csproj` loại migrations khỏi compile tổng quát rồi khai báo cụ thể một tập migration/snapshot; hãy kiểm tra cấu hình project và model snapshot trước khi thêm hoặc tạo migration mới để tránh dùng nhầm thư mục. API chạy `Database.MigrateAsync()` lúc startup, ngoại trừ DesignTime.

Seeder nằm trong `Persistence/Seed/`: khởi tạo category, recipe mẫu, step và image. Không nên giả định dữ liệu seed đại diện cho dữ liệu production.

### 3.6 Các luồng xử lý chính

**Đọc công thức:** route nhận query/slug → endpoint gọi handler MediatR hoặc truy vấn service/repository → Application tạo DTO → Infrastructure đọc PostgreSQL và có thể dùng cache → API trả JSON, chi tiết Recipe có ETag.

**Ghi công thức:** API xác thực JWT và quyền sở hữu/role → kiểm tra request và version `If-Match` → write service/repository cập nhật recipe và entity con → Unit of Work/EF lưu → cache liên quan được invalidated → trả response và version mới.

**Upload ảnh:** API nhận multipart/form data → validator kiểm tra dữ liệu ảnh → storage service ghi object vào MinIO → metadata/URL được dùng bởi recipe hoặc bước nấu. API file upload riêng yêu cầu đăng nhập.

**Xác thực:** Auth endpoint gọi `IAuthService`; Infrastructure kiểm tra credential/Google token, tạo access/refresh token và lưu refresh-token state. Email confirmation và password-reset có DTO/route tương ứng; cấu hình môi trường ảnh hưởng việc xác nhận email khi đăng nhập.

## 4. Frontend chính (`frontend/`)

Frontend chính chạy Next.js 14, React 18 và TypeScript. `app/layout.tsx` thiết lập layout HTML/metadata cấp gốc; route nằm trong App Router và route groups dùng để sắp xếp trang mà không thêm segment vào URL.

| Khu vực | URL |
|---|---|
| Trang chung | `/`, `/about`, `/categories` |
| Auth/profile | `/login`, `/register`, `/confirm-email`, `/profile` |
| Công thức | `/recipes`, `/recipes/new`, `/recipes/[slug]`, `/recipes/[slug]/edit`, `/recipes/[slug]/cook` |
| Discovery | `/explore`, `/search`, `/categories/[slug]` |
| Admin | `/admin`, `/admin/recipes`, `/admin/recipes/trash`, `/admin/reports`, `/admin/reviews`, `/admin/settings`, `/admin/users` |
| Dashboard | `/dashboard/categories` |

Component được chia theo feature: `recipe-management/` cho editor, ingredient, nutrition, steps, ảnh, lifecycle và cooking guide; `discovery/` cho danh mục/khám phá; `admin/` cho admin/trash; `auth/`, `layout/`, `common/` và `ui/` cung cấp thành phần dùng chung.

`frontend/lib/api.ts` là lớp gọi HTTP dùng `fetch`: gắn access token, làm mới token sau 401, chuẩn hóa envelope/error và có phiên bản trả lại response headers để giữ ETag. `frontend/lib/api/recipes.ts` và `categories.ts` cung cấp hàm theo feature. `frontend/types/recipe.ts` xác thực response/request bằng Zod; client nên parse dữ liệu từ API tại biên thay vì giả định response luôn hợp lệ.

Trang `/dashboard/categories` gọi `GET /api/v1/categories/statistics`, endpoint hiện được map trong nhóm Categories. Trang quản trị Users và Reviews hiện hiển thị `AdminDataPlaceholder`; việc có route/page không có nghĩa giao diện đó đã kết nối đầy đủ với API quản trị tương ứng.

## 5. Frontend phụ (`frontend/culinary-blog-web/`)

Đây là project Next.js độc lập với `package.json`, dependency và source riêng; đang dùng Next.js 15/React 19, khác frontend chính. Docker Compose không build project này. Không trộn package, lệnh chạy hoặc route từ app này vào mô tả frontend chính nếu chưa chuyển đổi/chốt lại project được triển khai.

## 6. Dữ liệu, cấu hình và chạy local

`docker-compose.yml` khai báo PostgreSQL, Redis, MinIO, backend và frontend. Các cổng local:

| Dịch vụ | URL/port |
|---|---|
| Web | `http://localhost:3000` |
| API | `http://localhost:5000` |
| Health / readiness | `/health`, `/health/ready` trên API |
| PostgreSQL | `localhost:5432` |
| Redis | `localhost:6380` (container 6379) |
| MinIO API / Console | `localhost:9000` / `localhost:9001` |

Tạo `.env` local từ `.env.example`, rồi đặt các biến riêng cho PostgreSQL, JWT, MinIO, mật khẩu demo và tùy chọn bật API docs. `.env` chứa thông tin cấu hình nhạy cảm: không đọc để đưa vào tài liệu, không commit, không chia sẻ; chỉ ghi tên biến và mục đích trong hướng dẫn. `docker compose down` giữ named volumes; không dùng `down -v` nếu không chủ ý xóa dữ liệu.

Chạy stack từ repository root:

```powershell
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
docker compose up --build
```

Frontend riêng:

```powershell
Set-Location frontend
npm install
npm run dev
```

Backend riêng cần .NET 10 SDK cùng PostgreSQL, Redis và MinIO; cung cấp connection string, JWT key và thông tin storage qua User Secrets hoặc environment variables, sau đó chạy:

```powershell
dotnet run --project backend/src/CulinaryBlog.Api/CulinaryBlog.API.csproj
```

## 7. Kiểm thử và xác minh

- Backend unit tests: `backend/tests/CulinaryBlog.UnitTests/`.
- Backend API/integration tests: `backend/tests/CulinaryBlog.IntegrationTests/`; factory dùng environment Testing và EF InMemory cho database.
- Frontend tests: Vitest theo `frontend/package.json`; một số test nằm cạnh API client và component.
- Build backend: `dotnet build backend/CulinaryBlog.sln --nologo`.
- Test backend: `dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --nologo`.
- Test frontend: `Set-Location frontend; npm test`.
- Build frontend: `Set-Location frontend; npm run build`.

Test dùng InMemory không thay thế kiểm thử PostgreSQL, MinIO hoặc toàn bộ Docker stack. Với thay đổi endpoint, cần xác minh route được map, response contract, authorization và luồng thực tế của dependency liên quan.

## 8. Tài liệu và điểm cần kiểm tra khi phát triển

- [PROJECT_GUIDE.md](./PROJECT_GUIDE.md): cách chạy và mô tả module.
- [API_DOCUMENTATION.md](./API_DOCUMENTATION.md): OpenAPI, contract Recipe và client.
- [RECIPE_INGREDIENT_FLOW.md](./RECIPE_INGREDIENT_FLOW.md): vòng đời Recipe/Ingredient.
- [IMPLEMENTATION_GAPS.md](./IMPLEMENTATION_GAPS.md): các khoảng trống đã được ghi nhận ở thời điểm tài liệu đó cập nhật; đối chiếu lại source vì trạng thái có thể thay đổi.
- [requirements/SRS.md](./requirements/SRS.md): đặc tả yêu cầu.
- [database/explain-analyze-category-recipe.md](./database/explain-analyze-category-recipe.md): truy vấn và phân tích hiệu năng DB.

Trước khi kết luận một chức năng hoạt động, kiểm tra cả ba nơi: route được gọi trong `Program.cs`, client/page thực sự gọi đúng contract, và test bao phủ hành vi đó. Các file có tên giống nhau hoặc endpoint extension chưa được đăng ký không tự động trở thành chức năng chạy được.
