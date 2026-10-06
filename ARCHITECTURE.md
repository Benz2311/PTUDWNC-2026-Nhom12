# Kiến trúc hệ thống Culinary Blog

## 1. Tổng quan

Dự án là một hệ thống blog ẩm thực theo mô hình web application với frontend Next.js và backend .NET Web API. Hệ thống tập trung vào quản lý tài khoản người dùng, recipe (công thức nấu ăn), danh mục món ăn, file hình ảnh, cache, upload media và sitemap dữ liệu công khai.

Khi vận hành trong môi trường local, hệ thống chạy trên Docker Compose với 5 dịch vụ chính:

- PostgreSQL: lưu trữ dữ liệu nghiệp vụ
- Redis: cache và invalidation
- MinIO: object storage cho hình ảnh/media
- Backend .NET API: xử lý nghiệp vụ và API
- Frontend Next.js: giao diện người dùng

## 2. Kiến trúc tổng thể

### 2.1 Tầng client

Frontend sử dụng Next.js 14 + React 18 + TypeScript.

Cấu trúc chính:

- `frontend/app`: routing theo feature
- `frontend/components`: UI widget theo module
- `frontend/lib`: wrapper gọi API, quản lý auth token
- `frontend/public`: tài nguyên tĩnh

Các module phía client hiện có:

- Authentication & User Management
  - login
  - register
  - profile
- Recipe Management
  - danh sách recipe
  - tạo/sửa recipe
  - xem chi tiết recipe
  - upload/chỉnh sửa bước, nguyên liệu, ảnh
- Discovery & Search
  - khám phá
  - tìm kiếm
  - danh mục
- Admin/System
  - dashboard thống kê

### 2.2 Tầng API

Backend chạy trên ASP.NET Core minimal API, theo kiểu layered architecture:

- `CulinaryBlog.Api`: presentation layer, định nghĩa endpoints và routing
- `CulinaryBlog.Application`: business logic, contracts, DTOs, use cases
- `CulinaryBlog.Domain`: entities, enums, value objects, common abstractions
- `CulinaryBlog.Infrastructure`: EF Core, repositories, auth, Redis, MinIO, seeders, jobs

Mỗi request đi theo flow:

1. Frontend gửi HTTP request tới API
2. API endpoint validate input và xác thực JWT
3. Application services xử lý nghiệp vụ
4. Infrastructure thực hiện dữ liệu và external services
5. Trả về JSON response hoặc lỗi chuẩn hóa

### 2.3 Tầng dữ liệu

Dữ liệu nghiệp vụ được lưu trong PostgreSQL. Một số bộ phận dữ liệu quan trọng:

- `Users` / `ApplicationUser`
- `Recipes`
- `Categories`
- `RecipeIngredients`
- `RecipeSteps`
- `RecipeImages`
- `RecipeNutrition`
- `RecipeSlugHistories`
- `RecipeAuditLogs`
- `RefreshTokens`

Redis được dùng cho cache và invalidation của recipe, search, category, sitemap. MinIO dùng để lưu file ảnh và media, phục vụ recipe images và file uploads.

## 3. Mô hình triển khai

### 3.1 Docker Compose

File `docker-compose.yml` định nghĩa 5 service chính:

- `postgres`: port 5432
- `redis`: port 6380
- `minio`: port 9000 (S3 API) và 9001 (console)
- `backend`: port 5000, map tới cổng 8080 của ASP.NET Core
- `frontend`: port 3000

### 3.2 Cấu hình môi trường

File `.env` chứa:

- PostgreSQL credentials
- JWT key / issuer / audience
- MinIO credentials và bucket name
- CORS origin
- demo password

Các biến này được inject vào backend và docker services. Ví dụ:

- `POSTGRES_DB`
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`
- `JWT_KEY`
- `JWT_ISSUER`
- `JWT_AUDIENCE`
- `MINIO_ROOT_USER`
- `MINIO_ROOT_PASSWORD`
- `MINIO_ACCESS_KEY`
- `MINIO_SECRET_KEY`
- `FRONTEND_ORIGIN`
- `DEMO_PASSWORD`

## 4. Các thành phần kỹ thuật

### 4.1 Frontend Next.js

Cấu trúc trang kiểu route-based:

- `app/page.tsx`: home page
- `app/(authentication-user-management)`: login/register/profile
- `app/(discovery-search)`: categories, search, explore
- `app/(recipe-management)`: recipe list và recipe detail

Các component quan trọng:

- `AuthNav`: điều hướng auth
- `FeaturedRecipes`: hiển thị recipe nổi bật
- `RecipeEditor`, `RecipeForm`, `RecipeList`, `RecipeDetail`, `RecipeActions`: thao tác recipe
- `DiscoveryBrowser`: khám phá và tìm kiếm

### 4.2 Backend API

API bảo vệ bằng JWT và rate limiting.

Mỗi endpoint nhóm theo module:

- `AuthEndpoints`
- `CategoryEndpoints`
- `RecipeEndpoints`
- `FileEndpoints`
- `DashboardEndpoints`
- `SitemapEndpoints`
- `HealthEndpoints`

Cấu hình API trong `Program.cs` bao gồm:

- JWT authentication
- CORS
- Exception handler
- Rate limiter
- OpenAPI
- UseAuthentication / UseAuthorization
- Auto migration của database khi chạy
- Seed dữ liệu ban đầu

### 4.3 DI và Infrastructure

`DependencyInjection.cs` đăng ký các service đã được triển khai trong infrastructure:

- `ApplicationDbContext` -> PostgreSQL
- Redis cache
- MinIO client
- `IAuthenticationService`
- `IRecipeRepository`
- `IRecipeWriteService`
- `IUnitOfWork`
- `IRecipeCacheInvalidator`
- `IRecipePurgeService`
- `ISitemapGenerator`

Điều này cho phép backend tách rõ:

- API không phụ thuộc trực tiếp vào EF Core
- Infrastructure chịu trách nhiệm cho database, cache, storage và công việc nền

## 5. Module nghiệp vụ chính

### 5.1 Quản lý xác thực và người dùng

Hệ thống hỗ trợ:

- đăng ký
- đăng nhập
- refresh token
- logout
- lấy thông tin profile
- cập nhật profile

Cách hoạt động:

- Access token có thời hạn ngắn (15 phút)
- Refresh token có thời hạn 7 ngày
- Hệ thống lưu refresh token hash trong database
- Refresh flow revoke token cũ và phát token mới
- API kiểm tra `IsActive`, `EmailConfirmed`, `PasswordHash`, `Roles`

### 5.2 Quản lý recipe

Recipe là core domain của project. Các entity chính:

- `Recipe`
- `RecipeIngredient`
- `RecipeStep`
- `RecipeImage`
- `RecipeNutrition`
- `Category`

Recipe có lifecycle status:

- Draft
- Published
- Archived

Ngoài ra còn có soft delete và retention logic. Một số tính năng quan trọng:

- `ETag` / `If-Match` để chống race condition
- `xmin` PostgreSQL để kiểm tra concurrency
- kiểm tra quyền owner/admin trước khi cập nhật
- slug duy nhất và history slug
- publish/unpublish/archive/unarchive
- purge job cho recipe đã soft-delete qua retention

Các endpoint chính đã được kế thừa từ README project và map API như sau:

- `POST /api/v1/recipes`
- `PUT /api/v1/recipes/{id}`
- `PATCH /api/v1/recipes/{id}/publish`
- `PATCH /api/v1/recipes/{id}/unpublish`
- `PATCH /api/v1/recipes/{id}/archive`
- `PATCH /api/v1/recipes/{id}/unarchive`
- `DELETE /api/v1/recipes/{id}`
- `POST /api/v1/recipes/{id}/ingredients`
- `PUT /api/v1/recipes/{id}/ingredients/{ingredientId}`
- `DELETE /api/v1/recipes/{id}/ingredients/{ingredientId}`

### 5.3 Danh mục và khám phá

Category được lưu trong `Category` entity, có:

- `Name`
- `Slug`
- `Description`
- `ImageUrl`
- `OrderIndex`
- `IsDeleted`

Danh mục dùng cho:

- giao diện home page
- lọc recipe
- category page
- sitemap URL category

### 5.4 Files và MinIO

Cấp lưu trữ hình ảnh và media thông qua MinIO.

Cơ chế:

- backend tạo bucket nếu cần
- frontend upload ảnh qua API
- MinIO lưu object binary
- URL public hoặc internal được trả về cho client
- API thực hiện kiểm tra Media/Recipe ownership trước khi xoá

### 5.5 Sitemap và background jobs

Hệ thống có hosted services background chạy khi backend khởi động:

- `SitemapGenerationHostedService`
- `MinioBucketPolicyHostedService`
- `RecipePurgeHostedService` (chỉ kích hoạt khi `Recipes:PurgeJobEnabled = true`)

Chức năng sitemap:

- tạo sitemap XML cho recipe published
- chỉ chứa category active và recipe published
- sử dụng `Site:PublicBaseUrl`
- endpoint công khai: `/sitemap.xml`

### 5.6 Dashboard / admin

Dashboard endpoints tập trung vào thống kê tổng quan như:

- số recipe theo danh mục
- số lượng bài viết / trạng thái
- thống kê hệ thống và tần suất sử dụng

## 6. Luồng dữ liệu quan trọng

### 6.1 Luồng đăng ký / đăng nhập

1. Client gửi email/password qua frontend
2. API kiểm tra user tồn tại và mật khẩu
3. Backend tạo access token + refresh token
4. Frontend lưu token trong localStorage
5. Mỗi request đến API đều đính kèm Authorization header
6. Nếu access token hết hạn, frontend gọi refresh endpoint

### 6.2 Luồng tạo recipe

1. Người dùng đăng nhập
2. Frontend gửi request tạo recipe
3. API lấy `AuthorId` từ JWT
4. Server validate title, description, ingredients, steps, category
5. Recipe mới được lưu với status `Draft`
6. Hệ thống tạo slug và lưu slug history nếu cần
7. Response trả về recipe object + `ETag`

### 6.3 Luồng publish recipe

1. Client gửi patch publish
2. Server kiểm tra quyền owner/admin
3. Kiểm tra điều kiện publish: category hợp lệ, field đủ, có ingredient, bước, email xác nhận
4. Recipe chuyển từ Draft → Published
5. Cache liên quan bị invalidation
6. Response trả lại trạng thái mới và ETag

### 6.4 Luồng cập nhật recipe đồng thời

1. Client đọc ETag từ response
2. Client gửi `If-Match` trong request update
3. Server kiểm tra `xmin` hoặc version
4. Nếu version không khớp -> `409 Conflict`
5. Client reload lại dữ liệu và retry

## 7. Bảo mật và chất lượng hệ thống

Các yếu tố bảo mật hiện có:

- JWT ký bằng secret key
- CORS được giới hạn theo nguồn frontend
- Rate limiting cho API và auth endpoint
- PasswordHash sử dụng ASP.NET Identity PasswordHasher
- Refresh token được hash trước khi lưu
- Quản lý quyền owner/admin
- Soft delete để giảm rủi ro mất dữ liệu

Các yếu tố kỹ thuật quan trọng:

- `ProblemDetails` để chuẩn hóa lỗi
- `DbUpdateConcurrencyException` treated as 409
- Kubernetes-like best practice nhưng trong project hiện at local Docker Compose

## 8. Vấn đề kiến trúc đáng chú ý

1. Frontend hiện vẫn có nhiều phần demo/placeholder nhưng đã có cấu trúc module rõ ràng và có thể mở rộng.
2. Backend đã triển khai hướng kiến trúc clean, nhưng phần application layer có thể tiếp tục được tinh gọn nếu tách các use cases thành feature-specific handler rõ hơn.
3. Cache và invalidation được thiết kế tương đối tốt nhờ generation key thay vì wildcard scan Redis.
4. MinIO và Redis là các dependency cần được đảm bảo an toàn và có backup khi chuyển production.

## 9. Kết luận

Cấu trúc hệ thống của dự án Culinary Blog tập trung vào mô hình 3 tầng rõ ràng:

- Frontend Next.js phục vụ người dùng
- Backend .NET API xử lý nghiệp vụ
- Infrastructure layer kết nối PostgreSQL, Redis, MinIO và background jobs

Project đã có sự chuẩn bị tốt cho một hệ thống blog ẩm thực quản lý recipe, nội dung, file media, auth, cache và lifecycle công thức, đồng thời đã thể hiện quan tâm tới concurrency, audit, soft delete và background processing.
