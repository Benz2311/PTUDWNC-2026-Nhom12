# VÕ HÙNG MẠNH – 2312687
## BÁO CÁO CÔNG VIỆC CÁ NHÂN – TASK 1, TASK 2, TASK 3

---

### 1. Thông tin sinh viên
- **Họ và tên:** Võ Hùng Mạnh
- **Mã số sinh viên (MSSV):** 2312687
- **Vai trò trong nhóm:** Thành viên 4 (TV4) – Backend Developer
- **Nhánh Git phụ trách:** `2312687_VoHungManh`
- **Môn học:** Phát triển ứng dụng Web nâng cao (PTUDWNC-2026-Nhom12)

---

### 2. Tổng quan các nhiệm vụ thực tế
Trên nhánh `2312687_VoHungManh`, sinh viên tập trung nghiên cứu, thiết kế kiến trúc và triển khai các chức năng cốt lõi thuộc giai đoạn nền tảng (Bữa 1 – Bữa 3):
- **Phân công trách nhiệm:** Phụ trách toàn bộ module dữ liệu và nghiệp vụ liên quan đến **Bước thực hiện công thức nấu ăn (`RecipeStep`)** và **Hình ảnh minh họa công thức (`RecipeImage`)**.
- **Mô hình kiến trúc:** Áp dụng Clean Architecture (Domain -> Application -> Infrastructure -> Api), kết hợp Repository Pattern & Unit of Work.
- **Cơ sở dữ liệu:** Thiết kế Schema PostgreSQL 16 qua Entity Framework Core Fluent API, tối ưu hóa hiệu năng truy vấn với Eager Loading, AsNoTracking, chỉ mục Unique và hệ thống tìm kiếm toàn văn Full-Text Search tiếng Việt với thuật toán mờ Fallback.
- **Độ tin cậy:** Xây dựng cơ chế bộ đệm 2 lớp (Resilient Multi-level Cache) chống nghẽn và hệ thống xử lý ngoại lệ tập trung chuẩn RFC 7807 ProblemDetails.

---

### 3. Task 1 – Công việc đã thực hiện
**Giai đoạn:** Bữa 1 (09/09/2026 – 12/09/2026) – Khởi tạo cấu trúc dự án & Phân tích nghiệp vụ
- **Thiết kế Domain Entities:**
  - `RecipeStep`: Đại diện cho một bước thực hiện trong công thức, gồm các trường dữ liệu cốt lõi: `Id` (Guid), `RecipeId` (Guid), `StepNumber` (int), `Title` (string), `Description` (string), `TimerMinutes` (int?), `ImageUrl` (string?).
  - `RecipeImage`: Đại diện cho hình ảnh đại diện hoặc ảnh minh họa đính kèm món ăn: `Id` (Guid), `RecipeId` (Guid), `ImageUrl` (string), `AltText` (string?), `OrderIndex` (int), `IsPrimary` (bool).
- **Mối quan hệ nghiệp vụ:**
  - Thiết lập mối quan hệ 1 - N giữa `Recipe` và `RecipeStep` (`Recipe.Steps`).
  - Thiết lập mối quan hệ 1 - N giữa `Recipe` và `RecipeImage` (`Recipe.Images`).
- **Thỏa thuận kiến trúc giải pháp:**
  - Thống nhất cấu trúc Clean Architecture cho ASP.NET Core Web API và phân rã các lớp: `Domain`, `Application`, `Infrastructure`, `Api`.

---

### 4. Task 2 – Công việc đã thực hiện
**Giai đoạn:** Bữa 2 (20/09/2026 – 23/09/2026) – Thiết lập Môi trường Database & Mapping EF Core
- **Docker hóa cơ sở dữ liệu:**
  - Cấu hình file `docker-compose.yml` khởi chạy dịch vụ PostgreSQL 16 với port mapping `5432:5432`, volume mount bền vững và biến môi trường bảo mật qua `.env`.
- **Cấu hình Entity Framework Core Fluent API:**
  - Tạo cấu hình `RecipeStepConfiguration`: ánh xạ vào bảng `recipe_steps`, khóa chính `Id`, khóa ngoại `RecipeId` tham chiếu đến `recipes(Id)` với hành vi xóa tầng `OnDelete(DeleteBehavior.Cascade)`.
  - Tạo cấu hình `RecipeImageConfiguration`: ánh xạ vào bảng `recipe_images`, khóa chính `Id`, khóa ngoại `RecipeId` với `DeleteBehavior.Cascade`.
  - Đăng ký thực thể và cấu hình vào `ApplicationDbContext`.
- **Bộ dữ liệu mẫu (Seed Data):**
  - Xây dựng dữ liệu khởi tạo mẫu đảm bảo mỗi công thức (`Recipe`) có ít nhất 5 bước thực hiện tuần tự (`StepNumber` từ 1 đến 5) kèm hình ảnh minh họa tương ứng để kiểm thử trực quan trên môi trường phát triển.

---

### 5. Task 3 – Công việc đã thực hiện
**Giai đoạn:** Bữa 3 (29/09/2026 – 30/09/2026) – Tối ưu Truy vấn, Nghiệp vụ Domain, Full-Text Search & Cache
- **Domain Entity Validation & Business Rules:**
  - Đóng gói logic nghiệp vụ trực tiếp trong Entity (`RecipeStep.cs`, `RecipeImage.cs`): kiểm tra độ dài tiêu đề (tối đa 200 ký tự), mô tả (tối đa 4000 ký tự), thời lượng hẹn giờ không âm, định dạng URL ảnh hợp lệ.
  - Quản lý trạng thái ảnh đại diện (`IsPrimary`): đảm bảo trong một công thức chỉ có duy nhất một ảnh chính; khi kích hoạt ảnh mới làm ảnh chính thì tự động hạ cờ các ảnh còn lại.
- **Tối ưu hóa Database & Chỉ mục:**
  - Thêm Unique Composite Index `(RecipeId, StepNumber)` trong bảng `recipe_steps` ngăn chặn triệt để tình trạng trùng số thứ tự bước.
  - Thêm Index `(RecipeId, OrderIndex)` trong bảng `recipe_images` hỗ trợ sắp xếp thứ tự hiển thị nhanh chóng.
- **Chống N+1 Query và Tối ưu truy vấn đọc:**
  - Cấu hình truy vấn `RecipeDetailQuery` sử dụng `.AsNoTracking()` cho các thao tác đọc dữ liệu.
  - Áp dụng Eager Loading kết hợp sắp xếp:
    ```csharp
    .Include(r => r.Steps.OrderBy(s => s.StepNumber))
    .Include(r => r.Images.OrderBy(i => i.OrderIndex))
    ```
    loại bỏ hoàn toàn vấn đề truy vấn N+1 khi tải chi tiết món ăn kèm đầy đủ các bước và hình ảnh.
- **PostgreSQL Full-Text Search (FTS) & Fuzzy Search:**
  - Tích hợp tính năng tìm kiếm tiếng Việt toàn văn trên PostgreSQL:
    - Bổ sung cấu hình từ điển `to_tsvector('vietnamese', ...)` và tạo chỉ mục GIN trên `SearchVector`.
    - Sử dụng extension `unaccent` để hỗ trợ tìm kiếm tiếng Việt không dấu (`pho bo` tìm ra `Phở bò`).
    - Xây dựng thuật toán Fallback tìm kiếm mờ (Fuzzy Search) sử dụng `pg_trgm` (`similarity()`) khi người dùng gõ sai chính tả nhẹ (ví dụ: `phoo bo`).
- **Resilient Multi-Level Caching (`ResilientCacheService`):**
  - Hiện thực bộ nhớ đệm kết hợp In-Memory Cache (L1) và Distributed Cache / Redis (L2).
  - Tích hợp cơ chế fail-safe: khi cache gặp sự cố kết nối, hệ thống ghi log cảnh báo và truy xuất thẳng xuống database mà không làm gián đoạn trải nghiệm người dùng.
- **Kiến trúc Repository & Unit of Work:**
  - Định nghĩa và hiện thực `IRecipeRepository`, `RecipeRepository`, `IUnitOfWork`, `UnitOfWork` nhằm tách biệt tầng truy cập dữ liệu và quản lý giao dịch an toàn.
- **Xử lý ngoại lệ chuẩn hóa (RFC 7807):**
  - Xây dựng các lớp Domain Exception chuyên biệt: `RecipeStepException`, `RecipeImageException`, `InvalidStepNumberException`, `PrimaryImageConflictException`.
  - Triển khai middleware `GlobalExceptionHandler` bắt mọi ngoại lệ và định dạng response theo chuẩn RFC 7807 `ProblemDetails`.
- **API Endpoints Tích hợp:**
  - Bổ sung và hoàn thiện các endpoint trong `RecipeEndpoints.cs`:
    - `GET /api/v1/recipes/{id}`: Trả về chi tiết công thức kèm các bước nấu và danh sách ảnh.
    - `GET /api/v1/recipes/search`: Endpoint tìm kiếm kết hợp Full-Text Search, Unaccent và Fuzzy matching.
    - Các API quản lý bước nấu và hình ảnh.

---

### 6. Các file mã nguồn và vai trò

| Đường dẫn file | Vai trò và chức năng chính |
| :--- | :--- |
| `backend/src/CulinaryBlog.Domain/Entities/RecipeStep.cs` | Domain Entity đại diện cho bước thực hiện, chứa validation logic và factory methods. |
| `backend/src/CulinaryBlog.Domain/Entities/RecipeImage.cs` | Domain Entity quản lý hình ảnh công thức và nghiệp vụ duy nhất của ảnh chính (`IsPrimary`). |
| `backend/src/CulinaryBlog.Domain/Exceptions/` | Định nghĩa hệ thống domain exceptions (`RecipeStepException`, `PrimaryImageConflictException`, v.v.). |
| `backend/src/CulinaryBlog.Infrastructure/Data/Configurations/RecipeStepConfiguration.cs` | EF Core Fluent API cho `RecipeStep`, cấu hình bảng `recipe_steps`, Unique Index `(RecipeId, StepNumber)`. |
| `backend/src/CulinaryBlog.Infrastructure/Data/Configurations/RecipeImageConfiguration.cs` | EF Core Fluent API cho `RecipeImage`, cấu hình bảng `recipe_images`, Index `(RecipeId, OrderIndex)`. |
| `backend/src/CulinaryBlog.Infrastructure/Data/ApplicationDbContext.cs` | Đăng ký `DbSet<RecipeStep>`, `DbSet<RecipeImage>`, khởi tạo extensions PostgreSQL (`unaccent`, `pg_trgm`). |
| `backend/src/CulinaryBlog.Infrastructure/Repositories/RecipeRepository.cs` | Hiện thực repository truy vấn công thức, eager loading Steps & Images, thực hiện Full-Text Search. |
| `backend/src/CulinaryBlog.Infrastructure/Repositories/UnitOfWork.cs` | Quản lý transaction commit tập trung qua giao diện `IUnitOfWork`. |
| `backend/src/CulinaryBlog.Infrastructure/Services/ResilientCacheService.cs` | Dịch vụ bộ đệm 2 lớp chịu lỗi (L1 In-Memory / L2 Distributed) bảo vệ DB trước tải đọc lớn. |
| `backend/src/CulinaryBlog.Api/Endpoints/Recipes/RecipeEndpoints.cs` | Định nghĩa các Minimal API endpoints lấy chi tiết công thức, tìm kiếm FTS và quản lý Step/Image. |
| `backend/src/CulinaryBlog.Api/Middleware/GlobalExceptionHandler.cs` | Middleware xử lý ngoại lệ toàn cục chuẩn hóa theo RFC 7807 `ProblemDetails`. |
| `backend/src/CulinaryBlog.Infrastructure/Data/Migrations/20260928080623_AddStepImageAndFullTextSearch.cs` | EF Core Migration tạo bảng `recipe_steps`, `recipe_images` và chỉ mục tìm kiếm GIN. |
| `backend/tests/CulinaryBlog.IntegrationTests/Program.cs` | Bộ kiểm thử tích hợp tự động cho Full-Text Search, Unaccent, Fuzzy Search, Caching và Problem Details. |

---

### 7. Hướng dẫn chạy chương trình

#### Yêu cầu môi trường
- .NET SDK 10.0
- Docker Desktop (cho PostgreSQL 16)
- Node.js 20+ (nếu chạy Frontend)

#### Bước 1: Khởi động Cơ sở dữ liệu PostgreSQL
Tại thư mục gốc dự án:
```bash
docker compose up -d postgres
```

#### Bước 2: Áp dụng Migration cơ sở dữ liệu
```bash
cd backend/src/CulinaryBlog.Api
dotnet ef database update
```

#### Bước 3: Khởi chạy Backend Web API
```bash
cd backend/src/CulinaryBlog.Api
dotnet run
```
API lắng nghe tại `http://localhost:5000` (Swagger UI: `http://localhost:5000/swagger` hoặc OpenAPI definition).

---

### 8. Hướng dẫn kiểm thử

#### 8.1. Chạy bộ Integration Tests tự động
Dự án có sẵn bộ kiểm thử tích hợp tự động kiểm tra toàn bộ tính năng Task 3:
```bash
cd backend/tests/CulinaryBlog.IntegrationTests
dotnet run
```
Hoặc dùng lệnh test chuẩn:
```bash
dotnet test backend/tests/CulinaryBlog.IntegrationTests/CulinaryBlog.IntegrationTests.csproj
```
Kết quả mong đợi: Toàn bộ kịch bản kiểm thử (FTS, Unaccent, Fuzzy, Cache, RFC 7807) đều trả về `Passed!`.

#### 8.2. Kiểm thử thủ công qua API
1. **Lấy chi tiết công thức (bao gồm các bước và danh sách ảnh):**
   ```bash
   curl -X GET "http://localhost:5000/api/v1/recipes/{recipe-id}" -H "Accept: application/json"
   ```
2. **Tìm kiếm Full-Text Search có dấu:**
   ```bash
   curl -X GET "http://localhost:5000/api/v1/recipes/search?q=Ph%E1%BB%9F%20b%C3%B2"
   ```
3. **Tìm kiếm không dấu (unaccent):**
   ```bash
   curl -X GET "http://localhost:5000/api/v1/recipes/search?q=pho%20bo"
   ```
4. **Tìm kiếm mờ khi sai lỗi chính tả nhẹ (Fuzzy Similarity):**
   ```bash
   curl -X GET "http://localhost:5000/api/v1/recipes/search?q=phoo%20bo"
   ```

---

### 9. Kết quả thực tế và bằng chứng

- **Bằng chứng Git Commit:**
  - `41867b9`, `0fd8014`, `2e95b4e`: Thiết kế thực thể `RecipeStep` và `RecipeImage` (Task 1).
  - `05b7f6f`, `a985c3d`, `8ae00d7`, `be592f9`: Docker Postgres, cấu hình EF Core Fluent API, Seed Data mẫu (Task 2).
  - `76f766a`, `ede382e`, `5ff6188`, `fd1524b`, `10a90d1`, `bd9b82b`: Validation, Eager loading chống N+1, PostgreSQL FTS, Resilient Cache, API Endpoints, ProblemDetails (Task 3).
- **Bằng chứng thực thi kiểm thử:**
  - Lệnh `dotnet test backend/tests/CulinaryBlog.IntegrationTests/CulinaryBlog.IntegrationTests.csproj` biên dịch và thực thi kiểm chứng thành công:
    ```text
    Test run for CulinaryBlog.IntegrationTests.dll (.NETCoreApp,Version=v10.0)
    Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 5 s
    ```

---

### 10. Hạn chế và tồn đọng

- **Trạng thái tại mốc Bữa 3:** Nhánh `2312687_VoHungManh` phản ánh đúng khối lượng công việc kết thúc tại ngày 30/09/2026 (Task 1, 2 và 3).
- **Giao diện người dùng:** Phần UI hiển thị động các bước nấu dạng carousel và upload trực quan ảnh trên Next.js được tách sang phát triển chuyên biệt ở các giai đoạn Task 6, Task 7 và Task 8.
- **Tách nhánh tính năng:** Từ Task 4 đến Task 8, sinh viên chuyển sang làm việc trên các nhánh tính năng độc lập theo chuẩn Git Feature Branching (`feat/vohungmanh-*`) để phục vụ review và merge độc lập.

---

### 11. Liên kết đến các nhánh Task tiếp theo

Sau khi hoàn thành nền tảng Task 1–3 trên nhánh này, các công việc phát triển chuyên sâu tiếp theo được thực hiện độc lập trên các nhánh sau:

- **Task 6 (Quản lý trạng thái và UI Frontend):**
  - [`feat/vohungmanh-task6-step-management`](https://github.com/vohungmanh/PTUDWNC-2026-Nhom12/tree/feat/vohungmanh-task6-step-management): Quản lý bước nấu ăn.
  - [`feat/vohungmanh-task6-image-management`](https://github.com/vohungmanh/PTUDWNC-2026-Nhom12/tree/feat/vohungmanh-task6-image-management): Quản lý upload và thư viện ảnh.
- **Task 7 (Tìm kiếm nâng cao & Tối ưu hóa UI/UX):**
  - [`feat/vohungmanh-task7-search`](https://github.com/vohungmanh/PTUDWNC-2026-Nhom12/tree/feat/vohungmanh-task7-search): Bộ lọc nâng cao và giao diện tìm kiếm tức thì.
- **Task 8 (Tối ưu hóa, Kiểm thử toàn diện & Hoàn thiện):**
  - [`feat/vohungmanh-task8-step`](https://github.com/vohungmanh/PTUDWNC-2026-Nhom12/tree/feat/vohungmanh-task8-step): Hoàn thiện giao diện step-by-step cooking mode, timer và test coverage.
  - [`feat/vohungmanh-task8-image`](https://github.com/vohungmanh/PTUDWNC-2026-Nhom12/tree/feat/vohungmanh-task8-image): Tối ưu nén ảnh, lazy loading và Responsive Image Gallery.
