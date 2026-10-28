# Kiến trúc Culinary Blog

## Sơ đồ thành phần

```text
Browser
  |
  v
Next.js frontend (frontend/)
  |
  | HTTP / JSON, JWT
  v
ASP.NET Core Minimal API (backend/src/CulinaryBlog.Api)
  |                         |
  v                         v
Application layer        Infrastructure layer
  |                         |-- EF Core / PostgreSQL
  |                         |-- Redis
  |                         |-- MinIO
  v                         |-- Hangfire / background jobs
Domain layer <--------------+
```

## Backend layers

- `CulinaryBlog.Domain`: entities, enums và quy tắc miền như Recipe, Category, Ingredient, Step, Image, Nutrition, User và RefreshToken.
- `CulinaryBlog.Application`: use cases, MediatR handlers, validation, interfaces và DTO.
- `CulinaryBlog.Infrastructure`: EF Core/Npgsql, migrations, repositories, authentication, cache, object storage, seeding và background services.
- `CulinaryBlog.Api`: ASP.NET Core Minimal API endpoints, HTTP validation, authorization và composition root trong `Program.cs`.

Các thao tác Recipe có repository/write-service; các truy vấn Category hiện có cả code use-case/MediatR và một nhóm endpoint API trực tiếp dùng DbContext. Chi tiết phần nào đang hoạt động và phần nào chưa được wire được ghi tại [implementation gaps](./docs/IMPLEMENTATION_GAPS.md).

## Frontend

Ứng dụng frontend mà Compose sử dụng nằm tại `frontend/` (Next.js 14, React 18, TypeScript). App Router được chia theo nhóm:

- `(public)`: danh mục công khai.
- `(authentication-user-management)`: đăng nhập, đăng ký, hồ sơ.
- `(recipe-management)`: danh sách, chi tiết, tạo/sửa và cooking guide.
- `(discovery-search)`: khám phá và tìm kiếm.
- `(admin-system)`: dashboard/quản trị; một số trang là placeholder.

`frontend/culinary-blog-web/` là project Next.js lồng riêng, dùng phiên bản Next/React khác và hiện chỉ có homepage mẫu cùng vài trang Category. Đây không phải thư mục frontend mà Docker Compose build.

## Dữ liệu và hạ tầng

- PostgreSQL lưu người dùng, refresh token, category, recipe và các bảng con; schema được quản lý bằng EF Core migrations.
- Redis được dùng cho cache/invalidation.
- MinIO lưu media/object.
- Docker Compose định nghĩa các dịch vụ PostgreSQL, Redis, MinIO, API và frontend.
- Khi API khởi động, `Program.cs` chạy migrations và seed dữ liệu.

## Luồng request

1. Frontend gửi HTTP request đến API; các endpoint cần đăng nhập dùng JWT.
2. Endpoint gọi use case/service hoặc truy vấn qua Infrastructure.
3. Infrastructure tương tác PostgreSQL, Redis hoặc MinIO.
4. API trả JSON/HTTP status và frontend hiển thị trạng thái.

Recipe có optimistic concurrency qua PostgreSQL `xmin`/ETag trên các thao tác được hỗ trợ. Cache Recipe có cơ chế invalidation; sitemap và file endpoints đã có source nhưng hiện chưa được gọi từ composition root.
