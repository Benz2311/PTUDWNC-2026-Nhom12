# PTUDWNC-2026-Nhom12 - Culinary Blog

Culinary Blog là ứng dụng blog ẩm thực gồm frontend Next.js, backend ASP.NET Core Minimal API và PostgreSQL. Backend được tổ chức theo kiến trúc phân lớp Domain - Application - Infrastructure - API; Docker Compose cấu hình các dịch vụ phục vụ phát triển như PostgreSQL, Redis và MinIO.

Tài liệu này mô tả cấu trúc hiện tại của repository và tập trung ghi nhận phần Recipe & Ingredient đã triển khai.

## 1. Cấu trúc dự án

```text
PTUDWNC-2026-Nhom12/
├── docker-compose.yml                 # Khai báo các dịch vụ phát triển
├── .env.example                       # Mẫu biến môi trường
├── backend/
│   ├── src/
│   │   ├── CulinaryBlog.Api/          # Minimal API, endpoint, middleware, cấu hình host
│   │   ├── CulinaryBlog.Application/  # Use case, DTO và interface
│   │   ├── CulinaryBlog.Domain/       # Entity, enum và quy tắc nghiệp vụ
│   │   └── CulinaryBlog.Infrastructure/ # EF Core, PostgreSQL, repository, migration
│   ├── tests/
│   │   └── CulinaryBlog.UnitTests/    # Unit test và PostgreSQL integration test
│   └── tools/
│       └── CulinaryBlog.DbSeeder/     # Công cụ khởi tạo dữ liệu
├── frontend/
│   ├── app/                           # Route và layout của Next.js App Router
│   ├── components/                    # Component theo nhóm tính năng
│   └── lib/api.ts                      # Tiện ích gọi API
└── infrastructure/
    └── docker/                        # Dockerfile cho backend và frontend
```

### Backend

- `CulinaryBlog.Api` đăng ký dịch vụ và cung cấp endpoint cho xác thực, danh mục, công thức, dashboard, tệp và health check.
- `CulinaryBlog.Application` chứa các abstraction/use case như `IRecipeRepository`, `IUnitOfWork` và `IRecipeWriteService`.
- `CulinaryBlog.Domain` chứa các entity như `Recipe`, `RecipeIngredient`, `RecipeNutrition`, `RecipeStep`, `RecipeImage`, `Category` và `ApplicationUser`.
- `CulinaryBlog.Infrastructure` triển khai persistence bằng Entity Framework Core/Npgsql. Cấu hình model hiện tập trung trong `Persistence/ApplicationDbContext.cs` và các cấu hình entity trong `Persistence/Configurations/`; migration nằm trong `Persistence/Migrations/`.

### Frontend

Frontend sử dụng Next.js App Router và chia route thành các nhóm tính năng. Tên thư mục đặt trong ngoặc chỉ dùng để tổ chức mã nguồn, không xuất hiện trong URL.

```text
frontend/app/
├── layout.tsx
├── page.tsx
├── globals.css
├── (authentication-user-management)/ # Đăng nhập, đăng ký, hồ sơ
├── (discovery-search)/                # Khám phá và tìm kiếm
├── (recipe-management)/               # Danh sách, tạo mới, chi tiết công thức
└── (admin-system)/                    # Trang quản trị
```

Các component dùng chung theo tính năng nằm trong `frontend/components/`, gồm `admin/`, `discovery/` và `recipe-management/`.

### Công nghệ chính

- Frontend: Next.js, React, TypeScript.
- Backend: ASP.NET Core Minimal API, .NET 10.
- Persistence: Entity Framework Core 10, Npgsql, PostgreSQL 16.
- Dịch vụ phát triển: Docker Compose, Redis, MinIO.

## 2. Phần việc đã hoàn thành: Phạm Nguyễn Ngọc Phước - Recipe & Ingredient

### 2.1. Entity và quan hệ dữ liệu

- `Recipe` liên kết với `ApplicationUser` qua `AuthorId` và `Category` qua `CategoryId`. Hai quan hệ dùng `DeleteBehavior.Restrict`, tránh xóa tác giả/danh mục khi còn Recipe tham chiếu.
- `RecipeIngredient`, `RecipeStep` và `RecipeImage` là các collection của Recipe, liên kết bằng `RecipeId`. Cấu hình FK dùng cascade cho thao tác xóa vật lý; xóa Recipe từ luồng nghiệp vụ hiện tại là soft-delete.
- `RecipeIngredient` có các trường tên, số lượng, đơn vị, ghi chú và thứ tự. `Quantity` dùng precision `(10,3)`; `SortOrder` được lưu dưới tên cột `OrderIndex`.
- `RecipeNutrition` được cấu hình là owned entity qua `OwnsOne`, lưu cùng bảng `Recipes` trong các cột `Nutrition_*`, không tạo bảng riêng. Các giá trị dinh dưỡng dùng precision `(8,2)`.
- Cấu hình được lưu trong model của `ApplicationDbContext`; các file cấu hình riêng cho ingredient và nutrition cũng có trong `Persistence/Configurations/`.

### 2.2. Index và migration

- Recipe có unique index trên `Slug`, cùng các index đơn trên `AuthorId`, `CategoryId`, `Status` và `CreatedAt`.
- Các index ghép `(IsDeleted, Status, CreatedAt)` và `(CategoryId, IsDeleted, Status, CreatedAt)` hỗ trợ nhóm điều kiện lọc/danh sách.
- `Title` và `Description` có GIN index với `gin_trgm_ops`, phục vụ tìm kiếm chuỗi con trên PostgreSQL; model bật extension `pg_trgm`.
- Ingredient có index theo `RecipeId`; step có unique index `(RecipeId, StepNumber)`; image có index `(RecipeId, SortOrder)`.
- Migration `20260926025747_OptimizeRecipeIndexesAndSearch` ghi nhận các thay đổi index tìm kiếm/danh sách và index thứ tự của entity con. Migration cùng snapshot model được lưu tại `backend/src/CulinaryBlog.Infrastructure/Persistence/Migrations/`.

### 2.3. Repository, Unit of Work và luồng ghi

- `RecipeRepository` triển khai `IRecipeRepository`; `ApplicationUnitOfWork` triển khai `IUnitOfWork` và chuyển tiếp lời gọi `SaveChangesAsync()` tới `ApplicationDbContext`.
- `GetForUpdateAsync` tải Recipe ở trạng thái tracked cùng ingredient, step, image và nutrition; dùng `AsSplitQuery()` để tránh nhân dòng khi tải nhiều collection.
- `ExistsBySlugAsync` dùng `AnyAsync()` và nhận `excludeId` để kiểm tra slug khi cập nhật. Unique index ở database là lớp bảo vệ cuối trước slug trùng do các request đồng thời.
- `RecipeWriteService` điều phối tạo/cập nhật/xóa mềm Recipe, thay thế nội dung và các thao tác thêm/sửa/xóa ingredient, step, image; service lưu thay đổi qua Unit of Work.
- Khi thay nội dung, repository xóa các entity con cũ khỏi tập theo dõi, gắn các entity con mới và cập nhật aggregate trước một lần `SaveChangesAsync()`.
- Mỗi thao tác ghi trong service gọi một lần `SaveChangesAsync()`. Với PostgreSQL, EF Core thực hiện các câu lệnh của một lần save trong transaction tự động. Luồng production hiện không mở explicit transaction bao quanh nhiều lần save.

### 2.4. Truy vấn phục vụ Create/Update/Delete

- Kiểm tra slug dùng truy vấn tồn tại dạng `AnyAsync()` thay vì tải cả Recipe về bộ nhớ; lúc cập nhật có thể loại trừ chính Recipe đang sửa.
- Cập nhật aggregate tải Recipe tracked cùng các collection cần thay đổi bằng `GetForUpdateAsync`; việc thay ingredient, step và image được gom vào một lần lưu.
- Xóa Recipe qua repository cập nhật cờ `IsDeleted` và `UpdatedAt`, không xóa vật lý Recipe trong luồng nghiệp vụ thông thường.
- Tải Recipe để đọc chi tiết dùng các navigation cần thiết và `AsSplitQuery()`; các truy vấn danh sách/search được triển khai tại repository/endpoint theo projection và phân trang.

### 2.5. Kiểm thử

- `RecipeWriteServiceTests` kiểm tra service gọi đúng repository và số lần lưu cho các thao tác Recipe, ingredient, step và image.
- `RecipePersistenceModelTests` kiểm tra metadata của các index tìm kiếm/danh sách và index thứ tự của entity con.
- `RecipePersistenceIntegrationTests` dùng PostgreSQL thật để kiểm tra quan hệ User/Category, owned nutrition, tạo/cập nhật/xóa ingredient, thay thế collection con và soft-delete Recipe.
- Integration test yêu cầu `CULINARYBLOG_TEST_CONNECTION` trỏ tới database riêng có tên kết thúc bằng `_test`. Test chạy migration và rollback transaction sau khi hoàn thành; không dùng database phát triển làm database test.

Chạy test project:

```powershell
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --nologo
```

Khi chưa cấu hình `CULINARYBLOG_TEST_CONNECTION`, PostgreSQL integration test được skip có chủ đích; các unit test vẫn chạy. Để chạy integration test, tạo database test riêng rồi đặt connection string vào biến môi trường trước khi chạy lệnh trên.

## 3. Trạng thái tổng quan

Backend hiện có các luồng xác thực, quản lý danh mục/công thức, upload tệp, dashboard và health check. Frontend đã có route cho xác thực, khám phá, quản lý Recipe và quản trị; một số màn hình hoặc luồng dữ liệu vẫn đang dùng mock/placeholder, nên trạng thái UI chưa đồng nghĩa mọi chức năng backend đã được nối hoàn chỉnh.
Thông tin trong tài liệu phản ánh cấu trúc mã nguồn và các kiểm thử hiện có trong repository.

