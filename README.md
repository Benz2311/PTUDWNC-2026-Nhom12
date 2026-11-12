# Phát triển ứng dụng Web nâng cao

## Nhóm 12

### Thành viên

1. Võ Hùng Mạnh - 2312687 - 2312687@dlu.edu.vn - Nhóm trưởng
2. Lê Thị Ánh Nhung - 2312709 - 2312709@dlu.edu.vn
3. Phạm Nguyễn Ngọc Phước - 2312718 - 2312718@dlu.edu.vn
4. Nguyễn Văn Quốc - 2312729 - 2312729@dlu.edu.vn

---

## Quy tắc làm việc

1. Không push trực tiếp code chức năng lên `main`.
2. Luôn pull code mới nhất trước khi bắt đầu làm việc.
3. Mỗi task thực hiện trên branch riêng.
4. Không tự ý thay đổi cấu trúc project chung.
5. Không commit file `.env` chứa thông tin riêng của máy.
6. Commit message phải mô tả rõ thay đổi.
7. Kiểm tra code trước khi tạo Pull Request.
8. Khi push code cần tạo nhánh riêng từ nhánh cha và đặt tên theo chức năng đang thực hiện.
## Cấu trúc thư mục dự án Culinary Blog – Nhóm 12
PTUDWNC-2026-Nhom12/
│
├── backend/                              # ASP.NET Core Backend
│   ├── src/                              # Mã nguồn chính (tham khảo)
│   │   ├── CulinaryBlog.API/             # HTTP API
│   │   │   ├── Endpoints/                # Các endpoint nghiệp vụ
│   │   │   │   ├── Auth/
│   │   │   │   ├── Users/
│   │   │   │   ├── Recipes/
│   │   │   │   ├── Categories/
│   │   │   │   ├── Search/
│   │   │   │   └── Admin/
│   │   │   ├── Middleware/               # Xử lý yêu cầu, lỗi và logging
│   │   │   ├── Extensions/               # Dependency Injection và cấu hình
│   │   │   └── Program.cs                # Điểm khởi động
│   │   │
│   │   ├── CulinaryBlog.Application/     # Tầng nghiệp vụ ứng dụng
│   │   │   ├── DTOs/
│   │   │   ├── Interfaces/
│   │   │   ├── Services/
│   │   │   ├── Features/
│   │   │   └── Validators/
│   │   │
│   │   ├── CulinaryBlog.Domain/          # Domain Model
│   │   │   ├── Entities/
│   │   │   │   ├── ApplicationUser.cs
│   │   │   │   ├── Recipe.cs
│   │   │   │   ├── RecipeStep.cs
│   │   │   │   ├── RecipeImage.cs
│   │   │   │   ├── RecipeIngredient.cs
│   │   │   │   └── Category.cs
│   │   │   ├── Enums/
│   │   │   └── Common/
│   │   │
│   │   └── CulinaryBlog.Infrastructure/  # Tầng truy cập dữ liệu
│   │       ├── Persistence/
│   │       │   ├── Configurations/
│   │       │   ├── Migrations/
│   │       │   └── DbContext/
│   │       ├── Repositories/
│   │       ├── Storage/                  # Tích hợp MinIO
│   │       ├── Caching/                  # Redis
│   │       ├── BackgroundJobs/           # Hangfire
│   │       └── Observability/            # Logging, Tracing
│   │
│   ├── tests/                            # Kiểm thử Backend
│   │   ├── UnitTests/
│   │   └── IntegrationTests/
│   └── Dockerfile
│
├── frontend/
│   └── culinary-blog-web/                # Next.js + TypeScript
│       ├── app/                          # App Router
│       │   ├── page.tsx                   # Trang chủ
│       │   ├── search/                    # Tìm kiếm công thức
│       │   ├── categories/                # Danh mục
│       │   ├── recipes/                   # Công thức
│       │   ├── login/                     # Đăng nhập
│       │   ├── register/                  # Đăng ký
│       │   ├── profile/                   # Hồ sơ
│       │   └── admin/                     # Quản trị
│       │
│       ├── components/                   # Thành phần giao diện
│       │   ├── layout/                    # Header, Footer, Sidebar
│       │   ├── ui/                        # shadcn/ui
│       │   ├── recipes/                   # RecipeCard, RecipeForm
│       │   ├── search/                    # SearchBox
│       │   ├── steps/                     # StepEditor
│       │   └── images/                    # ImageManager
│       │
│       ├── hooks/                        # React Hooks
│       ├── lib/
│       │   ├── api/                       # API Client
│       │   │   ├── auth.ts
│       │   │   ├── recipes.ts
│       │   │   ├── categories.ts
│       │   │   ├── steps.ts
│       │   │   ├── images.ts
│       │   │   └── search.ts
│       │   └── utils/                     # Hàm tiện ích
│       │
│       ├── stores/                        # Quản lý trạng thái
│       ├── types/                         # TypeScript Interfaces
│       ├── public/                        # Ảnh và tài nguyên tĩnh
│       ├── e2e/                           # Playwright (tham khảo)
│       ├── package.json
│       ├── tsconfig.json
│       ├── next.config.*
│       └── Dockerfile
│
├── infrastructure/
│   ├── nginx/
│   │   └── nginx.conf                     # Reverse Proxy
│   └── postgres/
│       └── init-extensions.sql            # PostgreSQL extensions
│
├── docs/                                  # Tài liệu dự án (nếu có)
├── docker-compose.yml                     # Docker Compose
├── .env.example                           # Mẫu cấu hình
├── .gitignore
└── README.md                              # Tài liệu nhóm

---

# Bữa 1 - Xây dựng cấu trúc dữ liệu ban đầu

**Mục tiêu:** Xác định các thành phần dữ liệu chính, thiết lập quan hệ và chuẩn bị dữ liệu nền cho hệ thống.

## Nguyễn Văn Quốc - User/Auth

- Rà soát yêu cầu về người dùng, tài khoản và quy trình xác thực theo thiết kế hệ thống.
- Hoàn thiện entity `ApplicationUser` với các thuộc tính quản lý tài khoản, hồ sơ cá nhân, vai trò, trạng thái hoạt động và xác thực email.
- Khai báo các thuộc tính phục vụ phân quyền `Roles` (mặc định vai trò `Author`) và cờ trạng thái xác thực thư điện tử `EmailConfirmed`.
- Thiết lập entity `RefreshToken` để lưu mã băm, thời hạn, thời điểm thu hồi, token thay thế, `FamilyId`, lý do thu hồi và địa chỉ IP. Dùng `SessionVersion` để vô hiệu hóa phiên cũ khi đổi trạng thái tài khoản.
- Thiết lập mối quan hệ 1-N giữa `ApplicationUser` và `RefreshToken` (`ApplicationUser.RefreshTokens`).
- Hoàn chỉnh liên kết 1-N giữa người dùng và công thức (`ApplicationUser.Recipes` với `Recipe.Author`), xác định quyền tác giả bài viết.
- Chuẩn bị cấu trúc dữ liệu mẫu cho tài khoản quản trị (Admin) và tác giả (Author) phục vụ kiểm thử người dùng và tài khoản ban đầu.

## Lê Thị Ánh Nhung - Category & Statistics

- Xác định cấu trúc dữ liệu liên quan đến danh mục món ăn và nhu cầu khai thác công thức.
- Hoàn thiện entity `Category` kế thừa `BaseEntity` với các thuộc tính: `Id`, `Name`, `Slug`, `Description`, `ImageUrl`, `OrderIndex`.
- Xây dựng Factory Method `Create(...)` có kiểm tra validation tên danh mục và tự động sinh slug hợp lệ bằng `SlugHelper`.
- Thiết lập mối quan hệ 1-N giữa `Category` và `Recipe` (`Category.Recipes` và `Recipe.Category`).
- Đóng gói tập hợp danh sách công thức thuộc danh mục thông qua `IReadOnlyCollection<Recipe>` để bảo toàn tính đóng gói trong Domain.
- Chuẩn bị cấu trúc dữ liệu phục vụ các tác vụ lọc danh mục, sắp xếp, phân trang và thống kê số lượng công thức (`RecipeCount`).
- Chuẩn bị danh sách dữ liệu mẫu danh mục đa dạng (món chính, món khai vị, tráng miệng, đồ uống...) phục vụ truy vấn và thống kê.

## Phạm Nguyễn Ngọc Phước - Recipe & Ingredient

- Xác định cấu trúc dữ liệu và các trường thông tin cốt lõi của công thức nấu ăn.
- Hoàn thiện entity `Recipe` kế thừa `BaseEntity`, gồm `Id`, `Title`, `Slug`, `Description`, `Content`, `PrepTimeMinutes`, `CookTimeMinutes`, `Servings`, `Difficulty`, `Status` và `PublishedAt`. Lưu nội dung tổng quan trong `Content` và hướng dẫn chi tiết trong `RecipeStep`.
- Định nghĩa các enum thuộc phạm vi bài viết: `RecipeStatus` (Draft, Published, Archived) và `DifficultyLevel` (Easy, Medium, Hard, Expert).
- Thiết lập `RecipeIngredient` với `Id`, `RecipeId`, `Name`, `Quantity`, `Unit`, `Notes` và thứ tự hiển thị. Lưu định lượng dưới dạng số thập phân, cho phép bỏ trống và kiểm tra giá trị lớn hơn 0 khi có nhập.
- Thiết lập `RecipeNutrition` với `Calories`, `Protein`, `Carbohydrates`, `Fat`, `Fiber`, `Sodium` và `Source`. Trong MVP, dinh dưỡng do tác giả nhập và backend gán `Manual`.
- Hoàn chỉnh liên kết giữa `Recipe` với tác giả `ApplicationUser` (`AuthorId`, `Author`) và danh mục `Category` (`CategoryId`, `Category`).
- Thiết lập mối quan hệ 1-N giữa `Recipe` và `RecipeIngredient` (`Recipe.Ingredients`).
- Chuẩn bị dữ liệu mẫu cho công thức, định lượng nguyên liệu và bảng giá trị dinh dưỡng.

## Võ Hùng Mạnh - Step & Image

- Xác định cấu trúc dữ liệu cho quy trình các bước chế biến và bộ sưu tập hình ảnh công thức.
- Hoàn thiện `RecipeStep` với `Id`, `RecipeId`, `StepNumber`, `Title`, `Description`, `TimerMinutes` và `ImageUrl`. Tổ chức các bước thành danh sách một cấp.
- Thiết lập quy tắc thứ tự các bước thực hiện tuần tự thông qua `StepNumber` bắt đầu từ 1 và thời gian hẹn giờ `TimerMinutes >= 0`.
- Hoàn thiện `RecipeImage` với `Id`, `RecipeId`, `OriginalUrl`, `MediumUrl`, `ThumbnailUrl`, `AltText`, `IsPrimary` và `OrderIndex`.
- Thiết lập thông tin hiển thị hình ảnh: xác định ảnh đại diện chính bằng cờ `IsPrimary` và thứ tự hiển thị bằng `OrderIndex`.
- Hoàn chỉnh liên kết 1-N giữa `Recipe` với các bước chế biến `RecipeStep` (`Recipe.Steps`) và thư viện ảnh `RecipeImage` (`Recipe.Images`).
- Chuẩn bị dữ liệu mẫu cho các bước nấu chi tiết kèm đồng hồ hẹn giờ và tập hợp hình ảnh minh họa theo thứ tự.

---

# Bữa 2 - Hoàn thiện cấu hình dữ liệu và tạo dữ liệu kiểm thử

**Mục tiêu:** Hoàn thiện Entity, Configuration và dữ liệu ngẫu nhiên cho từng chức năng; tích hợp vào DbContext và Migration để tạo cơ sở dữ liệu phục vụ kiểm thử.

## Nguyễn Văn Quốc - User/Auth

- Hoàn thiện entity `ApplicationUser` và `RefreshToken` đồng bộ với cấu trúc chung của hệ thống.
- Xây dựng `ApplicationUserConfiguration` để ánh xạ thông tin tài khoản và hồ sơ. Thiết lập giới hạn 100 ký tự cho `DisplayName`, 500 ký tự cho `AvatarUrl` và dùng kiểu text cho `Bio`.
- Cấu hình Unique Index cho `UserName` và `Email`, Index cho `IsActive` để tối ưu tra cứu trạng thái người dùng.
- Xây dựng `RefreshTokenConfiguration` ánh xạ bảng `RefreshTokens`, cấu hình độ dài chuỗi băm `TokenHash` (max 64, Unique Index), `ReplacedByTokenHash` (max 64), `CreatedByIp` (max 45).
- Cấu hình Index cho `UserId`, `ExpiresAt`, `RevokedAt` để tăng tốc độ kiểm tra hiệu lực token.
- Cấu hình quan hệ Foreign Key giữa `Users` và `RefreshTokens` với hành vi xóa tầng `DeleteBehavior.Cascade`.
- Kiểm tra và hoàn thiện quan hệ giữa `ApplicationUser` và `Recipe` (`AuthorId`), đảm bảo tính toàn vẹn khi người dùng tạo bài viết.
- Cấu hình ánh xạ Entity và các thuộc tính chỉ dùng trong ứng dụng để EF Core lưu đúng dữ liệu tài khoản và phiên.
- Chuẩn bị dữ liệu ngẫu nhiên (Faker/Seed Data) cho danh sách người dùng với đầy đủ trạng thái (Active, Inactive, Admin, Author) và các token kiểm thử.

## Lê Thị Ánh Nhung - Category & Statistics

- Hoàn thiện entity `Category` và cấu hình `CategoryConfiguration` ánh xạ bảng `Categories`.
- Cấu hình `Name` tối đa 100 ký tự, `Slug` tối đa 120, `Description` kiểu text, `ImageUrl` tối đa 500 và `OrderIndex`. Tạo chỉ mục duy nhất cho tên chuẩn hóa và slug của các danh mục chưa xóa.
- Cấu hình `HasQueryFilter(c => !c.IsDeleted)` cho xóa mềm và dùng PostgreSQL `xmin` để kiểm soát cập nhật đồng thời.
- Cấu hình quan hệ Foreign Key giữa `Category` và `Recipe` với hành vi `DeleteBehavior.Restrict` nhằm chặn xóa danh mục khi còn công thức liên kết.
- Cấu hình chế độ truy cập trường ẩn (`PropertyAccessMode.Field`) cho collection `Recipes` để bảo đảm tính đóng gói của Domain Model.
- Xử lý các điểm chưa thống nhất giữa Entity và Configuration thuộc phạm vi Category.
- Xây dựng dữ liệu mẫu ngẫu nhiên cho danh mục.
- Đảm bảo cơ sở dữ liệu sau khi tích hợp có ít nhất **20 Categories** hoàn chỉnh phục vụ truy vấn và thống kê.

## Phạm Nguyễn Ngọc Phước - Recipe & Ingredient

- Hoàn thiện `Recipe`, `RecipeIngredient`, `RecipeNutrition` và các lớp Configuration tương ứng trong EF Core.
- Xây dựng `RecipeConfiguration` cho `Title` bắt buộc và tối đa 200 ký tự, `Slug` tối đa 220 ký tự, `Description` kiểu text và khẩu phần. Bảo đảm slug duy nhất giữa các bản ghi chưa xóa.
- Cấu hình các Index trên `Recipes`: `CategoryId`, `Status`, `CreatedAt`, Composite Index `(IsDeleted, Status, CreatedAt)` và `(CategoryId, IsDeleted, Status, CreatedAt)`.
- Cấu hình Global Query Filter xóa mềm cho Recipe: `HasQueryFilter(r => !r.IsDeleted)`.
- Ánh xạ `RecipeIngredients` với `Name` tối đa 200, `Quantity` kiểu `decimal(10,3)` nullable, `Unit` tối đa 50, `Notes` tối đa 500 và trường thứ tự. Kiểm tra ràng buộc định lượng lớn hơn 0 khi có giá trị.
- Cấu hình Foreign Key từ `RecipeIngredients` đến `Recipes` với hành vi `DeleteBehavior.Cascade` và Index trên `RecipeId`.
- Xây dựng `RecipeNutritionConfiguration` ánh xạ `RecipeNutrition` dưới dạng Owned Entity (`OwnsOne`), ánh xạ các cột dinh dưỡng `Nutrition_Calories`, `Nutrition_Protein`, `Nutrition_Carbohydrates`, `Nutrition_Fat`, `Nutrition_Fiber`, `Nutrition_Sodium` với kiểu decimal `precision(8, 2)`.
- Cấu hình PostgreSQL `xmin` làm phiên bản của Recipe và dùng phiên bản này để kiểm soát xung đột khi ghi nguyên liệu.
- Xây dựng dữ liệu ngẫu nhiên (DbSeeder/Bogus) cho công thức, nguyên liệu và giá trị dinh dưỡng.
- Đảm bảo cơ sở dữ liệu sau khi tích hợp có ít nhất **100 Recipes** và mỗi Recipe có ít nhất **10 nguyên liệu** hợp lệ.

## Võ Hùng Mạnh - Step & Image

- Hoàn thiện `RecipeStep`, `RecipeImage` và các lớp Configuration tương ứng trong EF Core.
- Ánh xạ `RecipeSteps` với `StepNumber` bắt buộc, `Title` tối đa 200 và có thể bỏ trống, `Description` bắt buộc, `TimerMinutes` và `ImageUrl` tối đa 500.
- Cấu hình Foreign Key từ `RecipeSteps` đến `Recipes` với hành vi `DeleteBehavior.Cascade`.
- Cấu hình Unique Filtered Index cho `(RecipeId, StepNumber)` với điều kiện lọc `"IsDeleted" = false` nhằm chống trùng lặp thứ tự bước active.
- Cấu hình Check Constraint cho `RecipeSteps`: `CK_RecipeSteps_StepNumber` (`StepNumber >= 1`) và `CK_RecipeSteps_TimerMinutes` (`TimerMinutes IS NULL OR TimerMinutes >= 0`).
- Ánh xạ `RecipeImages` với `OriginalUrl` bắt buộc, `MediumUrl` và `ThumbnailUrl` tùy chọn, `AltText` tối đa 200 ký tự, `OrderIndex` và `IsPrimary`. Cấu hình kiểm soát cập nhật đồng thời cho bước nấu và ảnh.
- Cấu hình Foreign Key từ `RecipeImages` đến `Recipes` với hành vi `DeleteBehavior.Cascade`.
- Cấu hình Index cho `(RecipeId, OrderIndex)` phục vụ truy vấn danh sách ảnh theo thứ tự.
- Cấu hình chỉ mục duy nhất có điều kiện trên `(RecipeId, IsPrimary)` với bộ lọc `"IsPrimary" = true AND "IsDeleted" = false`, bảo đảm tối đa một ảnh đại diện đang hoạt động cho mỗi công thức.
- Cấu hình Check Constraint `CK_RecipeImages_OrderIndex` (`OrderIndex >= 0`).
- Xây dựng dữ liệu ngẫu nhiên cho các bước chế biến và hình ảnh, đảm bảo mỗi Recipe sau khi tích hợp có ít nhất **5 bước chế biến** tuần tự và các hình ảnh kiểm thử.

---

# Bữa 3 - Thiết kế dữ liệu và tối ưu truy vấn

**Mục tiêu:** Hoàn thiện Repository, Unit of Work, tối ưu các truy vấn dữ liệu, kiểm tra Index và triển khai Full-Text Search theo nội dung Chương 3; đảm bảo từng thành viên tiếp tục xử lý đúng nhóm Entity đã được phân công.

## Nguyễn Văn Quốc - User/Auth

- Rà soát các truy vấn dữ liệu liên quan đến `ApplicationUser` và `RefreshToken`.
- Kiểm tra việc sử dụng Repository và Unit of Work cho module User/Auth, đảm bảo tính nhất quán trong các thao tác truy xuất người dùng.
- Tối ưu các truy vấn đăng nhập, đối soát tài khoản bằng `Email` hoặc `UserName` và truy vấn lấy thông tin người dùng.
- Tối ưu truy vấn kiểm tra và xác thực `RefreshToken` theo mã băm `TokenHash` và `UserId`.
- Sử dụng `AsNoTracking()` cho các truy vấn chỉ đọc như xem hồ sơ (`GetProfile`) hoặc kiểm tra tồn tại của tài khoản.
- Kiểm tra các Index phục vụ tra cứu thường xuyên như `Email`, `UserName` và `RefreshToken`, loại bỏ Sequential Scan không cần thiết.
- Kiểm tra mã SQL do EF Core sinh ra trong Console/Log và xử lý triệt để các truy vấn lấy dư thừa trường dữ liệu.
- Kiểm thử hiệu năng và độ chính xác của các truy vấn User/Auth sau khi tối ưu.

## Lê Thị Ánh Nhung - Category & Recipe Read

- Rà soát `Category` và toàn bộ các truy vấn đọc dữ liệu liên quan đến Recipe.
- Hoàn thiện `CategoryRepository` và `RecipeRepository` (phần đọc) kết hợp Unit of Work cho các nghiệp vụ truy vấn.
- Hoàn thiện các truy vấn danh sách Recipe, chi tiết Recipe theo Slug và danh sách Recipe theo Category.
- Hoàn thiện lọc danh mục, độ khó, sắp xếp và phân trang cho danh sách Recipe. Danh sách công khai chỉ trả Published, còn bộ lọc trạng thái dùng cho truy vấn cá nhân hoặc truy vấn theo tác giả của Admin.
- Sử dụng `AsNoTracking()` cho tất cả các truy vấn chỉ đọc để giảm thiểu chi phí theo dõi đối tượng của EF Core.
- Sử dụng Projection/DTO để chỉ trích xuất đúng các cột dữ liệu cần thiết từ PostgreSQL.
- Kiểm tra và xử lý triệt để N+1 Query Problem khi tải danh mục kèm số lượng công thức (`RecipeCount`) hoặc tải danh sách bài viết.
- Chọn projection và cách nạp các quan hệ Ingredients, Steps, Images phù hợp để hạn chế N+1 và dữ liệu tích Descartes. Đánh giá `AsSplitQuery()` khi phù hợp với tính nhất quán của truy vấn.
- Kiểm tra các Index phục vụ truy vấn như `CategoryId`, `Status`, `CreatedAt` và `Slug`.
- Sử dụng `EXPLAIN ANALYZE` trên PostgreSQL để đánh giá execution plan và đo đạc chi phí thực tế của các truy vấn `GetAll`, `GetById` và `GetByCategory`.
- Viết tài liệu phân tích hiệu năng truy vấn trong `docs/database/explain-analyze-category-recipe.md`.
- Kiểm thử Filter, Sort, Pagination và các truy vấn đọc sau khi tối ưu.

## Phạm Nguyễn Ngọc Phước - Recipe & Ingredient

- Rà soát cấu hình dữ liệu của `Recipe`, `RecipeIngredient` và `RecipeNutrition`.
- Kiểm tra lại quan hệ giữa Recipe với User, Category và RecipeIngredient, đảm bảo tính toàn vẹn dữ liệu khi thực hiện thao tác ghi.
- Kiểm tra cấu hình `RecipeNutrition` dưới dạng Owned Entity, đảm bảo ánh xạ đúng các trường dinh dưỡng.
- Kiểm tra các Index của Recipe như `Slug`, `AuthorId`, `CategoryId`, `Status` và `CreatedAt` để hỗ trợ quá trình kiểm tra trùng lặp và lọc bài viết.
- Hoàn thiện `RecipeRepository` (phần ghi dữ liệu) và tích hợp Unit of Work cho các thao tác tạo mới, cập nhật và xóa công thức.
- Kiểm tra việc sử dụng `SaveChangesAsync()` và quản lý Transaction trong các nghiệp vụ cập nhật dữ liệu đa thực thể.
- Tối ưu truy vấn kiểm tra duy nhất của `Slug` và truy vấn nạp Recipe phục vụ các thao tác Create/Update/Delete.
- Dùng PostgreSQL `xmin` để tạo ETag và kiểm tra xung đột khi ghi công thức.
- Tạo và áp dụng Migration nếu cấu trúc dữ liệu hoặc chỉ mục có sự thay đổi.
- Kiểm thử các thao tác ghi dữ liệu Recipe và Ingredient sau khi tích hợp.

## Võ Hùng Mạnh - Step, Image & Full-Text Search

- Rà soát `RecipeStep`, `RecipeImage` và các Configuration tương ứng trong EF Core.
- Kiểm tra và bảo đảm toàn vẹn quan hệ giữa `RecipeStep`, `RecipeImage` và `Recipe`.
- Đảm bảo dữ liệu các bước chế biến luôn được truy vấn đúng thứ tự tăng dần theo `StepNumber`.
- Đảm bảo dữ liệu hình ảnh được sắp xếp đúng theo `OrderIndex` và xác định chính xác ảnh đại diện chính `IsPrimary`.
- Tối ưu các truy vấn đọc Step và Image độc lập hoặc trong Recipe Detail bằng `AsNoTracking()` khi phù hợp.
- Kiểm tra việc nạp dữ liệu Step/Image trong truy vấn chi tiết Recipe để hạn chế N+1 Query.
- Triển khai Full-Text Search trên PostgreSQL với `SearchVector` kiểu `tsvector`, được trigger cập nhật từ tiêu đề và mô tả.
- Cấu hình chỉ mục GIN Index trên `SearchVector` phục vụ tìm kiếm toàn văn nhanh chóng.
- Tích hợp extension PostgreSQL `unaccent` và hàm `PlainToTsQuery()` với cấu hình `simple` để hỗ trợ tìm kiếm tiếng Việt không dấu.
- Xây dựng logic xếp hạng kết quả tìm kiếm theo độ liên quan (`ts_rank`) kết hợp sắp xếp phụ theo `PublishedAt DESC`.
- Kiểm thử thực tế tìm kiếm từ khóa không dấu `"pho bo"` và đảm bảo tìm thấy chính xác Recipe `"Phở bò"`.
- Tạo Migration cho PostgreSQL extension, SearchVector và GIN Index trên cơ sở dữ liệu.
- Kiểm thử lại toàn diện các truy vấn Step, Image và Full-Text Search sau khi tích hợp.

---

# Bữa 4 - Hoàn thiện API Endpoints và Giao diện

**Mục tiêu:** Hoàn thiện API và giao diện theo module đã phân công, kết nối giao diện với API và kiểm thử các luồng chính trước khi tích hợp.

---

## Nguyễn Văn Quốc - User / Authentication / Profile / Admin User Management

- Hoàn thiện các API xác thực: Đăng ký (`POST /api/v1/auth/register`), Đăng nhập (`POST /api/v1/auth/login`), Đăng nhập bằng Google (`POST /api/v1/auth/google`), Cấp lại token (`POST /api/v1/auth/refresh`) và Đăng xuất (`POST /api/v1/auth/logout`).
- Xoay vòng refresh token trong một transaction, lưu token thay thế và thu hồi các token cùng `FamilyId` khi phát hiện token đã thu hồi bị dùng lại.
- Hoàn thiện API xác nhận email bằng `userId` và `token`. Trả `202` với thông báo chung khi người dùng yêu cầu gửi lại thư và đưa Welcome Email vào tác vụ nền sau khi đăng ký.
- Hoàn thiện API lấy thông tin người dùng hiện tại (`GET /api/v1/auth/me`).
- Hoàn thiện API cập nhật hồ sơ người dùng (`PATCH /api/v1/auth/me`): cho phép cập nhật `DisplayName`, `AvatarUrl`, `Bio`; ngăn chặn tuyệt đối việc chỉnh sửa Email hoặc Username qua endpoint này.
- Hoàn thiện `PATCH /api/v1/admin/users/{id}/status`, chặn Admin tự khóa và thu hồi refresh token khi vô hiệu hóa tài khoản. Kiểm tra `SessionVersion` để chặn access token đã cấp và bảo đảm token cũ không có hiệu lực lại sau khi mở khóa.
- Xây dựng giao diện Đăng nhập (Login) và Đăng ký (Register) với form validation đầy đủ (email format, mật khẩu mạnh).
- Xây dựng nút Google Login, màn hình xác nhận email và nút gửi lại thư. Tích hợp Google Identity Services và xác minh chữ ký, issuer, audience, expiry và nonce của ID token ở backend.
- Quản lý trạng thái xác thực toàn cục (Authentication State) và thiết lập bảo vệ các tuyến đường yêu cầu đăng nhập (Protected Routes).
- Xây dựng trang Hồ sơ cá nhân (Profile Page) hiển thị thông tin người dùng và form Cập nhật hồ sơ (Update Profile) kết nối API thật.
- Xây dựng giao diện Quản lý người dùng cho Admin (Admin User Management UI) cho phép xem danh sách, lọc và thay đổi trạng thái Active/Inactive kèm hộp thoại xác nhận.
- Xử lý trạng thái giao diện: hiển thị Loading spinner, thông báo lỗi chuẩn RFC 7807, thông báo thành công và chuyển hướng trang hợp lý.
- Kết nối giao diện hoàn toàn với API Auth/User thật, không hard-code dữ liệu nghiệp vụ.
- Kiểm thử tích hợp toàn bộ luồng Register, Login, Refresh Token, Logout, Confirm Email, Profile và Admin User Status.

---

## Lê Thị Ánh Nhung - Category / Recipe Read / Dashboard

- Hoàn thiện API danh mục: Lấy danh sách danh mục (`GET /api/v1/categories`) kèm số lượng công thức (`recipeCount`) và xem chi tiết danh mục theo slug (`GET /api/v1/categories/{slug}`).
- Hoàn thiện các API quản trị danh mục dành cho Admin: Tạo danh mục (`POST /api/v1/categories`), Cập nhật danh mục (`PUT /api/v1/categories/{id}`) và Xóa danh mục (`DELETE /api/v1/categories/{id}`).
- Xử lý ràng buộc nghiệp vụ xóa danh mục: kiểm tra các công thức trực thuộc chưa bị xóa mềm, chặn xóa và trả về mã lỗi `409 Conflict` nếu danh mục còn công thức.
- Hoàn thiện `GET /api/v1/recipes` có phân trang, lọc và sắp xếp. Mặc định chỉ trả Published. `mine=true` cần JWT và cho phép lọc trạng thái. `authorId` dành cho Admin.
- Hoàn thiện API xem chi tiết công thức (`GET /api/v1/recipes/{slug}`): nạp đầy đủ thông tin danh mục, tác giả, nguyên liệu (theo `OrderIndex`), các bước nấu (theo `StepNumber`), bộ sưu tập ảnh (theo `OrderIndex` và đánh dấu `IsPrimary`) cùng thông tin dinh dưỡng.
- Bảo đảm quy tắc bảo mật trong API xem chi tiết: công thức `Published` được xem công khai; công thức `Draft` hoặc `Archived` chỉ cho phép tác giả sở hữu hoặc Admin truy cập.
- Hoàn thiện API thùng rác công thức cho Admin (`GET /api/v1/admin/recipes/trash`) truy vấn các công thức đã bị xóa mềm (`IsDeleted = true`), hỗ trợ phân trang và sắp xếp giảm dần theo thời gian xóa.
- Xây dựng Dashboard/Thống kê của nhóm, lấy số liệu danh mục và công thức từ `GET /api/v1/categories/statistics`.
- Tối ưu truy vấn đọc, hạn chế tối đa N+1 Query và áp dụng bộ nhớ đệm phân tán Redis (TTL thích hợp cho danh mục và danh sách công thức public).
- Xây dựng giao diện Trang chủ và Khám phá công thức (Home/Recipe List UI) với các thẻ Recipe Card trực quan, thanh lọc danh mục, bộ sắp xếp và thanh phân trang.
- Xây dựng giao diện Chi tiết công thức (Recipe Detail UI) hiển thị đầy đủ thông tin bài viết, ảnh đại diện chính, thư viện ảnh phụ, danh sách nguyên liệu, các bước nấu có thời gian và bảng thông tin dinh dưỡng.
- Xây dựng giao diện Danh sách danh mục, Chi tiết danh mục và trang Quản lý danh mục dành cho Admin (Admin Category Management UI).
- Xây dựng giao diện Tổng quan Quản trị (Dashboard) với các thẻ tóm tắt số liệu (Summary Cards) và biểu đồ trực quan hóa dữ liệu thống kê từ API thật.
- Xây dựng giao diện Thùng rác công thức cho Admin (Admin Trash List UI) hiển thị danh sách bài viết đã xóa mềm (phần hành động Khôi phục/Xóa vĩnh viễn do TV3 phụ trách API).
- Phân định ranh giới rõ ràng: Giao diện `My Recipes` (danh sách công thức của tôi) sử dụng API đọc `GET /api/v1/recipes?mine=true` do Lê Thị Ánh Nhung phụ trách phần nạp dữ liệu đọc; các nút bấm thao tác tạo mới/chỉnh sửa do Phạm Nguyễn Ngọc Phước phụ trách.
- Xử lý các trạng thái giao diện: Loading skeleton, Empty state khi không có dữ liệu và hiển thị lỗi từ API trên tất cả các màn hình phụ trách.
- Kiểm thử Category CRUD, Recipe List/Detail, Filter/Sort/Pagination, Dashboard và Trash List.

---

## Phạm Nguyễn Ngọc Phước - Recipe / Ingredient

- Hoàn thiện các API vòng đời công thức: Tạo công thức ở trạng thái Draft (`POST /api/v1/recipes`) và Cập nhật công thức (`PUT /api/v1/recipes/{id}`).
- Hoàn thiện các API `PATCH /api/v1/recipes/{id}/publish`, `PATCH /api/v1/recipes/{id}/unpublish`, `PATCH /api/v1/recipes/{id}/archive` và `PATCH /api/v1/recipes/{id}/unarchive`.
- Kiểm tra quyền xuất bản của tác giả đã xác nhận email hoặc Admin. Kiểm tra công thức hợp lệ, danh mục đang hoạt động và có ít nhất một nguyên liệu cùng một bước nấu.
- Hoàn thiện xóa mềm (`DELETE /api/v1/recipes/{id}`), Admin khôi phục trong 30 ngày (`POST /api/v1/admin/recipes/{id}/restore`) và Admin xóa vĩnh viễn sau khi món đã xóa mềm (`DELETE /api/v1/admin/recipes/{id}/purge`).
- Dùng ETag từ `xmin` và `If-Match` khi cập nhật, chuyển trạng thái, xóa mềm công thức hoặc ghi nguyên liệu. Xung đột phiên bản trả `409 Conflict`.
- Duy trì lịch sử Slug cũ (`RecipeSlugHistory`) khi thay đổi tiêu đề công thức ở trạng thái Draft nhằm phục vụ định tuyến và SEO.
- Hoàn thiện các API quản lý nguyên liệu: Thêm nguyên liệu (`POST /api/v1/recipes/{id}/ingredients`), Cập nhật nguyên liệu (`PUT /api/v1/recipes/{id}/ingredients/{ingredientId}`) và Xóa nguyên liệu (`DELETE /api/v1/recipes/{id}/ingredients/{ingredientId}`).
- Kiểm tra tính hợp lệ dữ liệu nguyên liệu: `Name` (1–200 ký tự), `Quantity` (chấp nhận số thập phân decimal, từ chối chuỗi phân số `"1/2"` và trả về lỗi `422 Unprocessable Entity`), `Unit`, `Notes`, `OrderIndex`.
- Hoàn thiện xử lý thông tin dinh dưỡng trong công thức, đảm bảo trường nguồn gốc `Nutrition.Source` được quản lý đúng phía hệ thống (mặc định Manual, chặn client can thiệp trái phép).
- Lập lịch tác vụ nền tạo Sitemap cho công thức Published và thiết lập cơ chế thử lại khi tạo Sitemap thất bại.
- Dùng chung dịch vụ purge cho thao tác Admin và tác vụ dọn Recipe xóa mềm quá 30 ngày. Rà soát việc xóa bản ghi con, dọn ảnh MinIO và ghi audit log.
- Xây dựng giao diện trang Tạo công thức mới (Create Recipe) và Chỉnh sửa công thức (Edit Recipe).
- Xây dựng phần Thông tin cơ bản công thức (Recipe Basic Information Form) gồm: Tiêu đề, Mô tả tóm tắt, Chọn danh mục, Thời gian chuẩn bị, Thời gian nấu, Khẩu phần và Độ khó.
- Xây dựng thành phần chỉnh sửa dinh dưỡng (Nutrition Editor Form) cho phép nhập và cập nhật các chỉ số dinh dưỡng.
- Xây dựng thành phần Quản lý nguyên liệu (`IngredientEditor`): thêm dòng nguyên liệu, chỉnh sửa tên/định lượng/đơn vị, xóa nguyên liệu và sắp xếp thứ tự.
- Xây dựng thanh công cụ hành động trên giao diện: Lưu nháp (Save Draft), Xuất bản (Publish), Hủy xuất bản (Unpublish), Lưu trữ (Archive), Mở lưu trữ (Unarchive) và Xóa bài viết.
- Xây dựng các nút hành động Khôi phục (Restore) và Xóa vĩnh viễn (Purge) trên giao diện Thùng rác dành cho Admin.
- Tích hợp thành phần `StepEditor` và `ImageManager` của Võ Hùng Mạnh vào bố cục trang biên tập công thức (chỉ bố trí layout tích hợp, không can thiệp logic nghiệp vụ của component).
- Hiển thị hộp thoại xác nhận (Confirmation Dialog) trước các thao tác nguy hiểm (Xóa, Hủy xuất bản, Purge) và hiển thị thông báo lỗi xung đột phiên bản `409 Conflict`.
- Kiểm thử Create/Edit Recipe, Ingredient CRUD, Publish/Archive/Delete/Restore/Purge và kiểm soát xung đột đồng thời `409`.

---

## Võ Hùng Mạnh - Step / Image / Search / Infrastructure

- Hoàn thiện các API quản lý bước chế biến: Thêm bước (`POST /api/v1/recipes/{id}/steps`), Cập nhật bước (`PUT /api/v1/recipes/{id}/steps/{stepId}`), Xóa bước (`DELETE /api/v1/recipes/{id}/steps/{stepId}`) và Sắp xếp lại thứ tự bước (`PUT /api/v1/recipes/{id}/steps/reorder`).
- Đảm bảo tính liên tục của `StepNumber`: tự động cấp số thứ tự khi thêm mới, tự động đánh số lại (renumber) trong Database Transaction khi xóa một bước ở giữa; kiểm tra payload Reorder phải chứa đầy đủ toàn bộ active step của công thức.
- Kiểm tra dữ liệu bước nấu: tiêu đề tối đa 200 ký tự, mô tả bắt buộc từ 1 đến 2000 ký tự và hẹn giờ tùy chọn, không âm.
- Hoàn thiện upload ảnh công thức (`POST /api/v1/recipes/{id}/images`), cập nhật ảnh (`PATCH /api/v1/recipes/{id}/images/{imageId}`) và xóa mềm ảnh (`DELETE /api/v1/recipes/{id}/images/{imageId}`). Lên lịch dọn tệp ảnh sau thời gian giữ lại.
- Xử lý tải file qua `multipart/form-data`: giới hạn dung lượng tối đa 5 MB, hỗ trợ các định dạng JPEG, PNG, WebP, AVIF; kiểm tra MIME type, Magic Bytes và decode nội dung thực tế để chống mã độc.
- Cài đặt quy tắc nghiệp vụ ảnh đại diện chính (`IsPrimary`): ảnh tải lên đầu tiên tự động thành Primary; khi đặt ảnh mới làm Primary thì tự động hủy cờ Primary của ảnh cũ trong cùng transaction; khi xóa ảnh Primary hiện tại thì tự động thăng hạng ảnh còn lại có `OrderIndex` nhỏ nhất lên làm Primary.
- Tích hợp dịch vụ lưu trữ đối tượng MinIO (dùng AWS SDK S3): đặt tên file duy nhất (UUID), chống tấn công đường dẫn (Path Traversal), xử lý xóa an toàn (idempotent delete) và cơ chế thử lại (retry) khi gặp sự cố mạng.
- Xây dựng Hangfire Image Resize Job để tạo ảnh Medium 800×600 và Thumbnail 300×300, tải lên MinIO và cập nhật URL trong cơ sở dữ liệu.
- Hoàn thiện `GET /api/v1/recipes/search` để tìm công thức Published bằng FTS tiếng Việt không dấu và fuzzy fallback `pg_trgm`. Xếp hạng theo độ liên quan, kết hợp bộ lọc bằng AND, giới hạn các tiêu chí sắp xếp được phép, phân trang và trả `200` với danh sách rỗng khi không có kết quả. Lưu cache Redis 1 phút theo toàn bộ tham số truy vấn.
- Hoàn thiện `/health` (tổng hợp, gồm MinIO), `/health/live` (tiến trình) và `/health/ready` (các phụ thuộc gắn tag readiness, hiện gồm PostgreSQL và Redis).
- Hoàn thiện ghi log có cấu trúc (Structured Logging) bằng Serilog gắn kèm `CorrelationId`, thông tin User và cảnh báo khi thời gian xử lý yêu cầu vượt quá 500ms.
- Tích hợp giám sát vết và chỉ số (Tracing & Metrics) sử dụng OpenTelemetry cho HTTP requests và EF Core queries.
- Xây dựng thành phần Chỉnh sửa bước thực hiện (`StepEditor`): hiển thị danh sách các bước theo thứ tự `StepNumber`, cho phép thêm bước mới, chỉnh sửa tiêu đề, mô tả hướng dẫn, thời gian hẹn giờ (bấm giờ) và đổi vị trí các bước trực quan.
- Xây dựng thành phần Quản lý thư viện ảnh (`ImageManager`): hiển thị lưới ảnh (Gallery), xem trước ảnh khi chọn (Preview), nút tải ảnh lên với thanh tiến trình; cho phép cập nhật `AltText`, thay đổi thứ tự `OrderIndex`, nút bấm Đặt làm ảnh đại diện (`Set as Primary`) và nút Xóa ảnh.
- Tiền kiểm tra ảnh trên giao diện với giới hạn tối đa 5 MB và các định dạng được backend hỗ trợ. Hiển thị lỗi kết nối MinIO `503` và cập nhật huy hiệu Primary sau thao tác.
- Xây dựng giao diện Tìm kiếm công thức (Recipe Search UI): bao gồm ô nhập liệu tìm kiếm thông minh (Search Box hỗ trợ độ dài từ 2 đến 100 ký tự) và trang Kết quả tìm kiếm (Search Results Page) có bộ lọc, sắp xếp và phân trang.
- Xử lý các trạng thái giao diện tìm kiếm: Loading skeleton, Empty state sinh động khi không có công thức phù hợp; hỗ trợ kịch bản demo tìm kiếm không dấu `"pho bo"` ra kết quả `"Phở bò"` và tự động gợi ý từ khóa gần đúng.
- Các thành phần hạ tầng (Health Check, Logging, Tracing, Metrics) không tạo giao diện người dùng riêng mà được kiểm thử và trình diễn qua API, console log và công cụ đo lường hệ thống.
- Kiểm thử toàn bộ Step, Image, Storage, Thumbnail Job, Search, Health/Observability và các giao diện thuộc phạm vi phụ trách.

---

## Quy ước tích hợp Bữa 4

- Mỗi thành viên thực hiện đúng module Backend và component Frontend đã được phân công.
- Không viết đè hoặc tái triển khai logic nghiệp vụ thuộc sở hữu của thành viên khác.
- Phân định rõ ràng trách nhiệm bố cục và nghiệp vụ: khi một trang tích hợp component của thành viên khác, người sở hữu trang chịu trách nhiệm Layout bố trí, người sở hữu component chịu trách nhiệm State và Logic bên trong (Ví dụ: `StepEditor` và `ImageManager` thuộc Võ Hùng Mạnh, Phạm Nguyễn Ngọc Phước chỉ nhúng vào trang biên tập công thức).
- Ranh giới giữa TV2 và TV3 tại màn hình `My Recipes`: Lê Thị Ánh Nhung phụ trách API đọc danh sách công thức của tác giả (`mine=true`), Phạm Nguyễn Ngọc Phước phụ trách các nút hành động chỉnh sửa/tạo mới công thức.
- Shared Component và API Client tầng giao tiếp dùng chung chỉ được điều chỉnh khi có sự thống nhất của cả nhóm.
- Tuyệt đối không hard-code dữ liệu nghiệp vụ hoặc backend URL rải rác trong các component.
- Các thao tác Create/Update/Delete phải cập nhật State hoặc kích hoạt làm mới (Refresh) để giao diện phản ánh dữ liệu mới nhất từ API.
- Trước khi tạo Pull Request và Merge vào nhánh chính, phải đảm bảo Build Backend, Build Frontend và toàn bộ kiểm thử hiện có của dự án đều vượt qua thành công.
---

---

---

# Bữa 5 - Hoàn thiện bảo mật, tài liệu API và nền tảng Front-end

**Mục tiêu:** Hoàn thành các cấu hình bảo mật gồm CORS, Rate Limiting và Authentication/Authorization; hoàn thiện tài liệu đặc tả API và API Versioning; đồng thời xây dựng môi trường, cấu trúc dự án Front-end, cài đặt các thư viện cần thiết và các kiểu dữ liệu TypeScript phục vụ ứng dụng.

---

## Nguyễn Văn Quốc - Authentication / CORS / User Types

- Cấu hình và hoàn thiện JWT Authentication cho Backend.
- Thiết lập Issuer, Audience, Signing Key và thời hạn Access Token.
- Thiết lập phân quyền Author và Admin, giới hạn Guest chưa đăng nhập vào dữ liệu công khai.
- Kiểm tra các trường hợp `401 Unauthorized` và `403 Forbidden`.
- Cấu hình CORS cho các API thuộc phạm vi Authentication/User.
- Kiểm tra Front-end có thể gọi các API đăng ký, đăng nhập và lấy thông tin người dùng.
- Xây dựng các TypeScript Types phục vụ module người dùng và xác thực gồm:
  - `User`
  - `AuthUser`
  - `LoginRequest`
  - `RegisterRequest`
  - `AuthResponse`
  - `RefreshTokenRequest`
- Bảo đảm các Types phía Front-end khớp với DTO/Response của Backend.
- Kiểm thử toàn bộ phần Authentication/Authorization và kết nối Front-end với Auth API.

---

## Lê Thị Ánh Nhung - Rate Limiting / Front-end Environment / Category Types

- Cấu hình Rate Limiting bằng cơ chế Rate Limiter của ASP.NET Core.
- Thiết lập policy giới hạn request cho các API thông thường.
- Kiểm tra trường hợp vượt giới hạn request trả về `429 Too Many Requests`.
- Khởi tạo và cấu hình môi trường phát triển Front-end bằng Next.js và TypeScript.
- Thiết lập các file cấu hình:
  - `package.json`
  - `tsconfig.json`
  - `next.config.*`
  - ESLint
  - `.env`
  - `.env.example`
- Thiết lập biến `NEXT_PUBLIC_API_URL` để Front-end kết nối Backend.
- Xây dựng các TypeScript Types thuộc phạm vi Category gồm:
  - `Category`
  - `CategoryDetail`
  - `CategoryListItem`
  - `CategoryRequest`
- Cài đặt các thư viện cơ bản cần thiết để chạy dự án Front-end.
- Kiểm tra Front-end khởi động thành công và có thể gọi Backend API.

---

## Phạm Nguyễn Ngọc Phước - API Documentation / Recipe Types / Front-end Libraries

- Cấu hình OpenAPI để sinh tài liệu đặc tả API.
- Cài đặt và cấu hình Scalar hoặc công cụ API Documentation được sử dụng trong dự án.
- Cấu hình Bearer Authentication trong API Documentation.
- Kiểm tra tài liệu API hiển thị đầy đủ HTTP Method, Route, Parameter, Request và Response.
- Thử nghiệm Public API và Protected API trực tiếp từ giao diện API Documentation.
- Cài đặt các thư viện Front-end cần thiết cho:
  - gọi API;
  - validation;
  - quản lý form;
  - giao diện;
  - kiểm thử.
- Phân loại Dependency và Dev Dependency trong `package.json`.
- Xây dựng các TypeScript Types thuộc phạm vi Recipe gồm:
  - `Recipe`
  - `RecipeSummary`
  - `RecipeDetail`
  - `RecipeIngredient`
  - `RecipeNutrition`
  - `RecipeStatus`
  - `DifficultyLevel`
- Bảo đảm các Types Recipe khớp với dữ liệu Backend trả về.
- Kiểm tra quá trình cài Dependency và build Front-end.

---

## Võ Hùng Mạnh - API Versioning / Front-end Structure / Step-Image-Search Types

- Thiết lập và hoàn thiện API Versioning cho hệ thống.
- Chuẩn hóa các Business API theo định dạng `/api/v1/...`.
- Kiểm tra các endpoint sử dụng version thống nhất.
- Xây dựng cấu trúc dự án Front-end gồm các thư mục:
  - `app/`
  - `components/`
  - `hooks/`
  - `lib/`
  - `types/`
  - `public/`
- Xây dựng khu vực API Client và các thành phần dùng chung phía Front-end.
- Xây dựng các TypeScript Types thuộc phạm vi Step, Image và Search gồm:
  - `RecipeStep`
  - `RecipeImage`
  - `SearchRequest`
  - `SearchResult`
  - `Pagination`
  - `ApiResponse`
  - `ApiError`
- Định nghĩa kiểu dữ liệu TypeScript phù hợp với DTO và phản hồi của API Step, Image và Search.
- Kiểm tra việc import và tái sử dụng các Types trong Front-end.
- Kiểm tra TypeScript compile không phát sinh lỗi.
- Chạy `npm install`, `npm run lint` và `npm run build` để xác nhận nền tảng Front-end hoạt động.

# Bữa 6 - Hoàn thiện nền tảng Front-end, tiện ích dùng chung và giao diện tĩnh

**Mục tiêu:** Hoàn thiện các lớp tiện ích phục vụ xử lý lỗi và gọi API; cài đặt thư viện hỗ trợ thiết kế giao diện; xây dựng store quản lý trạng thái; hoàn thiện giao diện tĩnh, các component dùng chung và bố cục tổng thể cho Blog App nhằm chuẩn bị nền tảng cho các chức năng nghiệp vụ ở Bữa 7 và Bữa 8.

---

## Nguyễn Văn Quốc - Auth Utilities / Auth Store / Error Handling

- Rà soát và hoàn thiện các lớp tiện ích phía Front-end phục vụ Authentication, quản lý phiên đăng nhập và xử lý lỗi trả về từ Backend.
- Xây dựng cơ chế chuẩn hóa lỗi API theo định dạng RFC 7807 `ProblemDetails`, hỗ trợ xử lý thống nhất các trường hợp `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `409 Conflict`, `422 Unprocessable Entity` và lỗi kết nối mạng.
- Hoàn thiện các hàm tiện ích dùng cho luồng đăng nhập, đăng ký, đăng xuất, refresh token và lấy thông tin người dùng hiện tại, bảo đảm các page và component không tự xử lý request theo nhiều cách khác nhau.
- Xây dựng `auth-store.ts` để quản lý trạng thái xác thực toàn cục, bao gồm thông tin người dùng hiện tại, trạng thái đăng nhập, vai trò, trạng thái loading và các thao tác cập nhật hoặc xóa thông tin người dùng khi đăng nhập, đăng xuất hoặc cập nhật hồ sơ.
- Tích hợp `auth-store.ts` với API Client hiện có, không hard-code token hoặc trạng thái Authentication trong từng component.
- Chuẩn bị cơ chế Protected Route hoặc kiểm tra quyền truy cập cho các page yêu cầu đăng nhập và các page dành riêng cho Author/Admin.
- Xây dựng và tái sử dụng các trạng thái Loading, Error Alert và Success Message cho các form Authentication.
- Kiểm tra các TypeScript Types thuộc Auth đã xây dựng ở Bữa 5 được import và sử dụng thống nhất, không tạo thêm các interface trùng lặp.
- Kiểm thử các tình huống đăng nhập thành công, sai thông tin đăng nhập, token hết hạn, request không đủ quyền và lỗi Backend nhằm bảo đảm utility và store hoạt động ổn định.
- Chạy TypeScript compile, lint và production build để xác nhận phần Auth Utilities và Auth Store không phát sinh lỗi.

---

## Lê Thị Ánh Nhung - UI Library / Shared Components / Global Layout

- Cài đặt và cấu hình thư viện hỗ trợ thiết kế giao diện cho Front-end, ưu tiên `shadcn/ui` hoặc thư viện UI được nhóm thống nhất sử dụng trong toàn bộ Blog App.
- Thiết lập các component giao diện cơ bản như Button, Input, Textarea, Select, Dialog, Alert, Badge, Card, Skeleton và Dropdown Menu phục vụ các màn hình nghiệp vụ ở những bữa tiếp theo.
- Xây dựng các component dùng chung gồm `Header`, `Footer`, `Sidebar`, `Pagination`, `LoadingSpinner`, `LoadingSkeleton`, `ErrorState`, `EmptyState` và `ConfirmDialog`.
- Thiết kế `Header` hiển thị Logo/Tên Blog, liên kết Trang chủ, danh mục, khu vực tìm kiếm, các nút đăng nhập/đăng ký hoặc menu người dùng tùy theo trạng thái Authentication.
- Thiết kế `Footer` hiển thị thông tin dự án, liên kết điều hướng chính, thông tin nhóm và nội dung bản quyền cần thiết.
- Xây dựng `Sidebar` dùng chung cho các trang Public, chuẩn bị khu vực hiển thị danh sách Category và các liên kết điều hướng liên quan.
- Hoàn thiện component `Pagination` có khả năng nhận trang hiện tại, tổng số trang và callback hoặc URL chuyển trang để tái sử dụng cho Recipe List, Category, Search và Admin List.
- Hoàn thiện bố cục tổng thể của Blog App bằng Next.js Layout với Header phía trên, vùng nội dung chính, Sidebar ở các trang cần thiết và Footer phía dưới.
- Thiết kế giao diện tĩnh responsive cho Desktop, Tablet và Mobile; chuẩn hóa spacing, typography, container, card và grid để các thành viên khác sử dụng thống nhất.
- Chuẩn bị giao diện tĩnh cho Trang chủ, Category và các màn hình Public mà chưa gắn business logic hoặc dữ liệu thật.
- Không hard-code dữ liệu Recipe/Category trực tiếp trong Shared Components và không can thiệp logic nghiệp vụ của module thành viên khác.
- Kiểm tra khả năng tái sử dụng Shared Components và chạy TypeScript compile, lint, production build sau khi hoàn thiện UI Foundation.

---

## Phạm Nguyễn Ngọc Phước - API Utilities / Recipe Shared Components

- Rà soát và hoàn thiện các lớp tiện ích dùng chung để gọi API phía Front-end, tiếp tục sử dụng API Client đã được xây dựng ở Bữa 5 thay vì tạo một client mới.
- Chuẩn hóa các hàm gọi API cho các phương thức `GET`, `POST`, `PUT`, `PATCH`, `DELETE`, đồng thời hỗ trợ xử lý query string, pagination parameter, filter parameter, request body và response mapping.
- Hoàn thiện khu vực `lib/api/recipes.ts` để các page và component Recipe có thể sử dụng chung các hàm truy vấn và thao tác dữ liệu.
- Xây dựng các component Recipe dùng chung gồm `RecipeCard`, `RecipeList`, `RecipeMeta`, `RecipeStatusBadge`, `DifficultyBadge` và `RecipeTimeInfo`.
- Thiết kế `RecipeCard` với các khu vực hiển thị ảnh đại diện, tên công thức, danh mục, tác giả, độ khó, thời gian nấu và ngày xuất bản để tái sử dụng tại Trang chủ, Category và Search.
- Xây dựng giao diện tĩnh cho Recipe Detail với các khu vực tiêu đề, ảnh đại diện, thông tin cơ bản, nguyên liệu, dinh dưỡng, các bước thực hiện và thư viện ảnh.
- Xây dựng giao diện tĩnh cho Recipe Form phục vụ Bữa 8 gồm Basic Information, Ingredient, Nutrition, khu vực trạng thái Recipe, khu vực tích hợp `StepEditor` và `ImageManager`.
- Không triển khai business logic Create/Edit Recipe đầy đủ ở Bữa 6 và không chỉnh sửa logic bên trong `StepEditor` hoặc `ImageManager` thuộc Võ Hùng Mạnh.
- Bảo đảm các Recipe Shared Components nhận dữ liệu thông qua props hoặc state thích hợp, không hard-code dữ liệu nghiệp vụ.
- Kiểm tra các component sử dụng đúng Recipe Types đã xây dựng ở Bữa 5 và không phát sinh type trùng lặp.
- Kiểm tra khả năng import và tái sử dụng các component Recipe trong nhiều page khác nhau.
- Chạy TypeScript compile, lint và production build sau khi hoàn thiện API Utilities và Recipe Shared Components.

---

## Võ Hùng Mạnh - Step / Image / Search Utilities, State & Shared Components

- Rà soát và hoàn thiện các utility và API Client thuộc phạm vi Step, Image và Search trong `lib/api/steps.ts`, `lib/api/images.ts` và `lib/api/search.ts`, bảo đảm sử dụng chung API Client và biến môi trường `NEXT_PUBLIC_API_URL`.
- Hoàn thiện xử lý `multipart/form-data` cho chức năng upload ảnh, bảo đảm Browser/Axios tự sinh multipart boundary và không hard-code `Content-Type` sai cách.
- Xây dựng state/store cần thiết cho Search, quản lý từ khóa, filter, sort, page, trạng thái loading và trạng thái lỗi.
- Chuẩn bị state quản lý Step trong quá trình chỉnh sửa Recipe, bao gồm danh sách Step, thêm mới, cập nhật, xóa và thay đổi thứ tự.
- Chuẩn bị state quản lý Image bao gồm danh sách ảnh, preview, upload progress, ảnh Primary và trạng thái upload/delete.
- Xây dựng các component dùng chung thuộc phạm vi phụ trách gồm `SearchBox`, `SearchLoadingState`, `SearchEmptyState`, `SearchErrorState`, `StepItem`, `ImagePreview` và `ImageUploadButton`.
- Thiết kế `SearchBox` hỗ trợ nhập từ khóa, submit bằng Enter hoặc nút Search, kiểm tra độ dài từ khóa và hiển thị validation error.
- Xây dựng giao diện tĩnh cơ bản cho Step Editor và Image Manager nhằm chuẩn bị cho việc tích hợp đầy đủ tại Bữa 8.
- Bảo đảm các component Step/Image/Search có thể nhận dữ liệu và handler thông qua props hoặc state, không phụ thuộc trực tiếp vào RecipeForm.
- Kiểm tra việc tái sử dụng các TypeScript Types gồm `RecipeStep`, `RecipeImage`, `SearchRequest`, `SearchResult` và `Pagination`.
- Không can thiệp business logic Recipe Basic Information, Ingredient hoặc Nutrition thuộc Phạm Nguyễn Ngọc Phước.
- Chạy TypeScript compile, lint và production build để xác nhận Step/Image/Search Utilities, State và Shared Components hoạt động đúng.

---

## Quy ước tích hợp Bữa 6

- Mỗi thành viên chỉ thực hiện đúng phần nền tảng Front-end và Shared Component đã được phân công, không tự ý viết lại hoặc thay thế phần của thành viên khác.
- Nguyễn Văn Quốc chịu trách nhiệm `auth-store.ts`, các tiện ích Authentication, xử lý lỗi API và trạng thái xác thực toàn cục.
- Lê Thị Ánh Nhung chịu trách nhiệm thư viện UI dùng chung, `Header`, `Footer`, `Sidebar`, `Pagination`, `Loading`, `Error`, `Empty State` và bố cục Layout tổng thể.
- Phạm Nguyễn Ngọc Phước chịu trách nhiệm API Utilities liên quan Recipe và các Recipe Shared Components như `RecipeCard`, `RecipeList`, `RecipeMeta`, `RecipeStatusBadge`, `DifficultyBadge`.
- Võ Hùng Mạnh chịu trách nhiệm utility/state/component thuộc Step, Image và Search, bao gồm `SearchBox`, Step state, Image state và các component hỗ trợ upload/preview.
- Chỉ sử dụng một API Client chung cho toàn bộ Front-end; không tạo thêm API Client riêng trong từng module nếu chức năng đã được hỗ trợ.
- Chỉ sử dụng một thư viện UI chính được nhóm thống nhất; không tự ý cài nhiều UI framework gây xung đột style hoặc dependency.
- Shared Component phải được thiết kế để tái sử dụng, ưu tiên nhận dữ liệu qua props hoặc state thay vì hard-code dữ liệu nghiệp vụ.
- Không hard-code Backend URL trong component; toàn bộ API phải sử dụng biến môi trường và API Client chung đã được thiết lập từ Bữa 5.
- Không viết business logic Recipe vào Shared Layout, không viết Auth logic vào Category component và không đưa logic Step/Image vào Recipe Basic Information.
- Khi cần sử dụng component của thành viên khác, chỉ import và truyền props/callback; không chỉnh sửa logic bên trong component nếu chưa thống nhất.
- Các TypeScript Types đã xây dựng ở Bữa 5 phải được tái sử dụng, không tạo interface trùng lặp với cùng ý nghĩa.
- Không commit các file `.env`, file log runtime, `node_modules`, `.next`, file build/cache hoặc dữ liệu cục bộ của máy cá nhân.
- Trước khi tạo Pull Request hoặc Merge phải chạy `npm install`, TypeScript compile, lint và production build, đồng thời kiểm tra không phát sinh lỗi mới do Shared Component hoặc Store gây ra.

---

# Bữa 7 - Hoàn thiện chức năng người dùng và các trang Public chính

**Mục tiêu:** Hoàn thiện các chức năng Authentication và Profile; xây dựng các trang Public chính của Blog App gồm Trang chủ, Danh mục, Chi tiết công thức và Tìm kiếm; kết nối giao diện với API thật và hoàn thiện đầy đủ các trạng thái Loading, Error, Empty, Filter và Pagination cần thiết.

---

## Nguyễn Văn Quốc - Authentication / Password / Profile

- Xây dựng form đăng ký với email, mật khẩu, xác nhận mật khẩu và tên hiển thị. Dùng ô xác nhận mật khẩu để kiểm tra trên giao diện và xử lý username nội bộ từ email ở backend.
- Thực hiện validation phía Client theo đúng yêu cầu Backend, bao gồm định dạng Email, độ mạnh Password và xác nhận Password.
- Xây dựng luồng đăng nhập bằng email và mật khẩu, tích hợp JWT và `auth-store.ts`.
- Xử lý đầy đủ các trạng thái đăng nhập thành công, sai thông tin đăng nhập, tài khoản bị vô hiệu hóa, email chưa xác thực và lỗi Backend.
- Hoàn thiện chức năng Đăng xuất, gọi API Logout, xóa trạng thái Authentication và điều hướng người dùng về trang phù hợp.
- Xây dựng chức năng đổi mật khẩu với các trường mật khẩu hiện tại, mật khẩu mới và xác nhận mật khẩu mới.
- Xây dựng chức năng quên mật khẩu và dùng phản hồi chung khi người dùng yêu cầu đặt lại mật khẩu qua email.
- Xây dựng màn hình đặt lại mật khẩu và kết nối với API xác thực mã đặt lại mật khẩu.
- Hoàn thiện trang Profile hiển thị Avatar, Username, Email, DisplayName, Bio, Roles và trạng thái tài khoản.
- Xây dựng form cập nhật `DisplayName`, `AvatarUrl` và `Bio`. Đặt email và username ở chế độ chỉ đọc.
- Đồng bộ thông tin Profile mới vào `auth-store.ts` ngay sau khi cập nhật thành công.
- Hoàn thiện Protected Route cho Profile và các màn hình yêu cầu Authentication hoặc Role.
- Xử lý Loading, Error, Success Message và Redirect thống nhất trên toàn bộ các màn hình Auth.
- Kiểm thử toàn bộ các luồng Register, Login, Logout, Change Password, Forgot Password, Reset Password, Get Profile và Update Profile.

---

## Lê Thị Ánh Nhung - Home / Category / Public Layout Integration

- Hoàn thiện Trang chủ của Blog App bằng cách tích hợp Header, Footer, Sidebar và Main Content từ các Shared Components đã xây dựng ở Bữa 6.
- Sidebar của Trang chủ hiển thị danh sách Category lấy từ API thật, không sử dụng dữ liệu hard-code.
- Main Content hiển thị danh sách Recipe Published mới nhất bằng `RecipeCard`, hỗ trợ page, pageSize và sắp xếp phù hợp theo API.
- Hoàn thiện các section Category nổi bật hoặc Recipe mới nếu API hiện tại cung cấp dữ liệu tương ứng.
- Xây dựng trang xem danh sách tất cả Category, hiển thị tên danh mục, hình ảnh, mô tả và `recipeCount`.
- Hoàn thiện trang Category Detail theo slug, hiển thị thông tin Category và danh sách Recipe thuộc Category.
- Tích hợp Pagination cho danh sách Recipe trên Trang chủ và Category Detail.
- Hoàn thiện liên kết điều hướng từ Category Sidebar/Category Card đến trang Category Detail và từ Recipe Card đến Recipe Detail.
- Tái sử dụng các Shared Components Loading Skeleton, Error State, Empty State và Pagination thay vì viết riêng cho từng màn hình.
- Bảo đảm Trang chủ và Category hoạt động responsive trên Desktop, Tablet và Mobile.
- Kiểm tra người dùng chưa đăng nhập vẫn có thể truy cập Trang chủ, danh sách Category và các Recipe Published.
- Không hard-code dữ liệu Category hoặc Recipe trong UI.
- Kiểm thử việc tải Trang chủ, danh sách Category, Category Detail, Pagination, Empty State và API Error State.

---

## Phạm Nguyễn Ngọc Phước - Recipe Detail Page

- Hoàn thiện trang xem chi tiết công thức theo Slug bằng API `GET /api/v1/recipes/{slug}`.
- Hiển thị tiêu đề, mô tả, tác giả, danh mục, độ khó, thời gian, khẩu phần và ngày xuất bản theo dữ liệu API. `Content` dùng cho phần tổng quan riêng của dự án, còn hướng dẫn nấu hiển thị từ RecipeStep.
- Hiển thị ảnh Primary của Recipe ở vị trí nổi bật và hiển thị bộ sưu tập ảnh phụ theo thứ tự nếu Recipe có nhiều hình ảnh.
- Hiển thị danh sách Ingredient đúng theo `OrderIndex`, bao gồm Name, Quantity, Unit và Notes.
- Hiển thị Calories, Protein, Carbohydrates, Fat, Fiber và Sodium khi có dữ liệu. Thêm lưu ý rằng thông tin dinh dưỡng do tác giả cung cấp và chỉ mang tính tham khảo.
- Hiển thị các bước thực hiện đúng theo `StepNumber`, kèm Title, Description, TimerMinutes và Image nếu có.
- Tái sử dụng các component Step/Image thuộc Võ Hùng Mạnh ở chế độ hiển thị phù hợp mà không sửa logic nội bộ của các component.
- Hiển thị thông tin tác giả, Category và các liên kết điều hướng liên quan.
- Xử lý trường hợp Recipe không tồn tại bằng giao diện `404 Not Found` thích hợp.
- Xử lý quyền truy cập đối với Draft hoặc Archived đúng theo Backend, chỉ cho Author sở hữu hoặc Admin xem khi hệ thống yêu cầu.
- Tái sử dụng Loading Skeleton, Error State và Empty State dùng chung.
- Hoàn thiện responsive layout cho Recipe Detail.
- Không triển khai chức năng Create/Edit Recipe trong Bữa 7.
- Kiểm thử Recipe Detail với Recipe Published, Recipe có/không có ảnh, Recipe có nhiều Step/Ingredient, Recipe không tồn tại và Recipe không đủ quyền xem.

---

## Võ Hùng Mạnh - Recipe Search

- Hoàn thiện chức năng tìm kiếm công thức và route `/search`, kết nối trực tiếp với API `GET /api/v1/recipes/search`.
- Hoàn thiện `SearchBox` cho phép nhập từ khóa, submit bằng Enter hoặc nút Search, validation độ dài từ khóa và đồng bộ từ khóa với URL query parameter nếu cấu trúc ứng dụng sử dụng.
- Hoàn thiện các bộ lọc Search theo đúng Backend hỗ trợ, bao gồm Category, Difficulty, Max Cook Time và các filter thực tế khác nếu có.
- Hoàn thiện cơ chế sắp xếp kết quả theo độ liên quan, thời gian xuất bản mới nhất, thời gian nấu hoặc các tiêu chí Backend hỗ trợ.
- Tích hợp Pagination cho kết quả tìm kiếm và giữ đúng query/filter/sort/page khi người dùng chuyển trang.
- Hiển thị kết quả tìm kiếm bằng Search Result Card hoặc `RecipeCard` dùng chung.
- Xử lý đầy đủ các trạng thái Loading, Empty, Error và Search Result.
- Đồng bộ từ khóa, bộ lọc, kiểu sắp xếp và số trang với URL tìm kiếm. Kiểm thử việc giữ trạng thái sau khi tải lại, đồng thời kiểm tra phân trang trên Trang chủ và trang chi tiết danh mục.
- Không gửi request khi từ khóa không hợp lệ hoặc chưa đạt độ dài tối thiểu.
- Kiểm thử tìm kiếm tiếng Việt không dấu với từ khóa `pho bo` và dữ liệu mẫu có món Phở bò.
- Kiểm thử Search với nhiều trang kết quả, nhiều Category, filter và sort khác nhau.
- Hoàn thiện responsive cho Search UI và bảo đảm Recipe Published có thể được tìm kiếm mà không cần đăng nhập.

---

## Quy ước tích hợp Bữa 7

- Nguyễn Văn Quốc chịu trách nhiệm toàn bộ Authentication, Password, Profile và Protected Route.
- Lê Thị Ánh Nhung chịu trách nhiệm Trang chủ, danh sách Category, Category Detail và việc tích hợp Public Layout trên các page thuộc phạm vi của mình.
- Phạm Nguyễn Ngọc Phước chịu trách nhiệm Recipe Detail Page và cách trình bày thông tin Recipe, Ingredient và Nutrition.
- Võ Hùng Mạnh chịu trách nhiệm toàn bộ chức năng Search, Search State, Search Result, Filter, Sort và Pagination của trang tìm kiếm.
- Các page phải kết nối trực tiếp với API thật; không sử dụng dữ liệu mock hoặc hard-code để thay thế dữ liệu nghiệp vụ trong bản hoàn thiện.
- Trang chủ chỉ hiển thị Recipe Published và Category public theo dữ liệu API; không tự truy cập hoặc hiển thị Draft/Archived trái quyền.
- Trang Recipe Detail có thể sử dụng Step/Image component của Võ Hùng Mạnh nhưng Phạm Nguyễn Ngọc Phước không được thay đổi logic nội bộ của các component đó.
- `SearchBox` có thể xuất hiện trong `Header` do Lê Thị Ánh Nhung phụ trách Layout, nhưng logic Search, validation từ khóa và điều hướng kết quả thuộc Võ Hùng Mạnh.
- Các component `Header`, `Footer`, `Sidebar`, `Pagination`, `Loading`, `Error`, `Empty State` từ Bữa 6 phải được tái sử dụng, không tạo phiên bản riêng cho từng page nếu không thực sự cần thiết.
- Auth State phải lấy từ `auth-store.ts`; không tự tạo state đăng nhập riêng tại từng page.
- Tất cả route yêu cầu đăng nhập hoặc Role phải kiểm tra quyền truy cập thống nhất bằng cơ chế Protected Route hoặc guard chung.
- Các lỗi từ API phải hiển thị theo chuẩn xử lý lỗi chung đã được xây dựng ở Bữa 6, không tự hiển thị thông báo theo nhiều định dạng khác nhau.
- Đồng bộ tham số URL cho các màn hình có bộ lọc, sắp xếp và phân trang qua URL để giữ trạng thái khi tải lại hoặc quay về trang.
- Không sửa hoặc ghi đè phần nghiệp vụ của thành viên khác khi chỉ cần tích hợp UI.
- Trước khi Merge phải kiểm tra Register, Login, Logout, Profile, Home, Category, Recipe Detail và Search hoạt động trên cùng một Front-end mà không xung đột route hoặc state.
- Phải chạy TypeScript compile, lint, frontend build và kiểm tra các API chính trước khi tạo Pull Request.

---

# Bữa 8 - Hoàn thiện quản lý Recipe, Upload Image, Admin và Logging

**Mục tiêu:** Hoàn thiện upload ảnh, tạo và cập nhật công thức, quản lý vòng đời công thức, quản trị người dùng, danh mục và ghi log. Tích hợp các module trước khi kiểm thử toàn hệ thống.

---

## Nguyễn Văn Quốc - Admin User Management / Auth Completion

- Hoàn thiện trang danh sách người dùng dành cho Admin, kết nối trực tiếp với API quản trị User.
- Hiển thị các thông tin người dùng gồm Id, UserName, Email, DisplayName, Role, IsActive và CreatedAt.
- Hiển thị vai trò Author và Admin đúng theo quyền tài khoản. Phân biệt Guest chưa đăng nhập với tài khoản đăng nhập thiếu quyền.
- Hoàn thiện Search, Filter và Pagination cho danh sách người dùng nếu API hỗ trợ.
- Xây dựng giao diện xem thông tin chi tiết người dùng hoặc Dialog hiển thị các thông tin cần thiết.
- Hoàn thiện thao tác kích hoạt và vô hiệu hóa tài khoản, hiển thị Confirmation Dialog trước các thao tác có ảnh hưởng đến người dùng.
- Bảo đảm Admin không thể tự vô hiệu hóa chính mình theo đúng quy tắc Backend.
- Xử lý và hiển thị chính xác các lỗi `403 Forbidden`, `404 Not Found`, `409 Conflict` từ API.
- Kiểm thử việc chặn đăng nhập, làm mới token và gọi API được bảo vệ khi tài khoản bị khóa. Sau khi mở khóa, kiểm tra token của phiên cũ vẫn bị từ chối.
- Kiểm thử tích hợp các luồng đăng ký, đăng nhập, đăng xuất, làm mới token, đổi mật khẩu, quên mật khẩu và cập nhật hồ sơ sau khi tích hợp quản trị người dùng.
- Hoàn thiện các luồng Auth/User còn lại trong phạm vi đăng nhập, quản lý phiên, xác thực email và quản trị tài khoản.
- Kiểm thử phân quyền giữa Admin, Author, Guest và tài khoản đã đăng nhập nhưng không có quyền cần thiết.
- Bảo đảm người không có quyền Admin không thể truy cập trang quản lý người dùng.

---

## Lê Thị Ánh Nhung - Category Completion / Home & Layout Finalization

- Hoàn thiện các thao tác quản lý danh mục và giao diện dành cho Admin.
- Xây dựng và hoàn thiện chức năng tạo Category với các trường Name, Description, ImageUrl, OrderIndex và các trường Backend thực tế yêu cầu.
- Xây dựng chức năng cập nhật Category và đồng bộ dữ liệu mới trên UI sau khi thao tác thành công.
- Xây dựng chức năng xóa Category, hiển thị Confirmation Dialog và xử lý lỗi `409 Conflict` khi Category vẫn còn Recipe liên kết.
- Rà soát Category List và Category Detail sau khi Recipe Create/Edit được tích hợp, bảo đảm `recipeCount` được cập nhật đúng.
- Rà soát Trang chủ sau khi các chức năng Recipe hoàn thiện, kiểm tra danh sách Recipe mới, Sidebar Category, Pagination, Header và Footer.
- Hoàn thiện giao diện tổng thể Blog App về responsive, spacing, typography, card layout và mobile navigation.
- Rà soát các Shared Components gồm Header, Footer, Sidebar, Pagination, Loading, Error và Empty State để bảo đảm cách hiển thị thống nhất giữa các page.
- Tích hợp dữ liệu danh mục, danh sách công thức và số liệu Dashboard vào các trang đọc và thống kê.
- Không can thiệp business logic Recipe Create/Edit thuộc Phạm Nguyễn Ngọc Phước.
- Kiểm thử Category CRUD, Home, Layout và responsive sau khi tích hợp toàn hệ thống.

---

## Phạm Nguyễn Ngọc Phước - Recipe Create / Update / Lifecycle

- Hoàn thiện trang tạo mới Recipe và trang cập nhật Recipe, kết nối trực tiếp với các API ghi dữ liệu của Backend.
- Xây dựng Recipe Form với Title, Description, Category, thời gian chuẩn bị/nấu, Servings và Difficulty. Trường Content là phần tổng quan bổ sung trong code, còn chỉ dẫn chi tiết dùng StepEditor.
- Tích hợp `IngredientEditor` để thêm, sửa, xóa và đổi thứ tự nguyên liệu. Cho phép nhập phân số trên giao diện và chuyển sang số thập phân trước khi gửi API.
- Tích hợp `NutritionEditor` cho các giá trị dinh dưỡng nhập tay, gồm Sodium. Gửi đúng các trường dinh dưỡng theo DTO và để backend gán `Source`.
- Thực hiện luồng tạo Recipe ở trạng thái Draft và cập nhật Recipe Draft.
- Hoàn thiện vòng đời: Publish đưa món sang Published, Unpublish đưa về Draft, Archive đưa sang Archived và Unarchive đưa về Draft. Delete đánh dấu xóa mềm độc lập với trạng thái.
- Hiển thị trạng thái hiện tại của Recipe bằng Status Badge và cập nhật giao diện ngay sau khi thao tác trạng thái thành công.
- Kiểm tra thông tin món, danh mục hoạt động, ít nhất một nguyên liệu, ít nhất một bước nấu và quyền xuất bản của tác giả đã xác nhận email hoặc Admin.
- Tích hợp `StepEditor` của Võ Hùng Mạnh vào Recipe Form mà không thay đổi logic nội bộ của component.
- Tích hợp `ImageManager` của Võ Hùng Mạnh vào Recipe Form theo nguyên tắc Phước chịu trách nhiệm bố trí layout, Mạnh chịu trách nhiệm logic Step/Image.
- Hoàn thiện Confirmation Dialog cho các thao tác Delete, Publish, Archive, Purge hoặc các thao tác nguy hiểm khác.
- Xử lý ETag và `If-Match` cho các thao tác ghi cần kiểm soát phiên bản. Hiển thị lỗi `409 Conflict` và cho tải lại phiên bản mới nhất.
- Hoàn thiện các action tại My Recipes gồm Edit, Publish, Archive và Delete.
- Triển khai Restore và Purge dành riêng cho Admin. Chỉ cho thao tác với món đã xóa mềm và giới hạn khôi phục trong 30 ngày.
- Hoàn thiện kiểm tra hợp lệ và lưu dữ liệu Recipe, Ingredient và Nutrition khi tạo hoặc chỉnh sửa công thức.
- Kiểm thử vòng đời Recipe gồm Create, Save Draft, Edit, Publish, Unpublish, Archive, Unarchive, xóa mềm và Restore/Purge theo quyền Admin.

---

## Võ Hùng Mạnh - Image Upload / Step Integration / Logging / Observability

- Hoàn thiện tính năng Upload Image cho Recipe, kết nối ImageManager với API upload ảnh thật và xử lý `multipart/form-data` đúng chuẩn.
- Hỗ trợ chọn file bằng File Picker, Drag & Drop nếu component có, hiển thị Preview trước khi upload và Upload Progress nếu API Client hỗ trợ.
- Thực hiện Client Validation trước khi upload dựa trên giới hạn dung lượng tối đa 5 MB và các định dạng file thực tế Backend đang hỗ trợ.
- Hoàn thiện ImageManager cho phép upload ảnh, cập nhật AltText, thay đổi OrderIndex, đặt ảnh làm Primary và xóa ảnh.
- Tự động refresh danh sách ảnh sau các mutation và cập nhật trạng thái Primary trên UI ngay sau thao tác Set Primary.
- Xử lý lỗi Object Storage/MinIO, bao gồm `503 Service Unavailable`, upload thất bại, file không hợp lệ và các lỗi kết nối.
- Tải ảnh bước nấu từ thiết bị qua `POST /api/v1/recipes/{id}/steps/images` và lưu `imageUrl` cho bước tương ứng.
- Hoàn thiện `StepEditor` khi tích hợp thật với API, hỗ trợ Add Step, Update Step, Delete Step, Reorder Step, TimerMinutes và Step Image.
- Kiểm tra `StepNumber` được cập nhật chính xác sau khi xóa hoặc thay đổi thứ tự Step.
- Bảo đảm `StepEditor` và `ImageManager` hoạt động ổn định khi được nhúng vào Recipe Form của Phạm Nguyễn Ngọc Phước.
- Không thay đổi business logic Recipe Basic Information, Ingredient hoặc Nutrition.
- Hoàn thiện Structured Logging bằng Serilog và ghi log cho các sự kiện quan trọng như lỗi request, lỗi upload ảnh, lỗi MinIO, lỗi Search, lỗi Background Job và exception hệ thống.
- Thêm `CorrelationId` vào log để truy vết yêu cầu và ghi định danh người dùng khi yêu cầu đã được xác thực.
- Kiểm tra cơ chế Slow Request Logging và cảnh báo khi thời gian xử lý vượt ngưỡng cấu hình.
- Rà soát và hoàn thiện Health Check tại `/health`, `/health/live` và `/health/ready` theo cấu hình thực tế của hệ thống.
- Rà soát OpenTelemetry Tracing & Metrics cho HTTP Request, EF Core và các dependency phù hợp.
- Kiểm thử Image Resize Job sau upload, gồm cập nhật URL Medium/Thumbnail, hiển thị ảnh gốc khi job lỗi và thử lại khi xử lý thất bại.
- Tích hợp Step, Image, Search, File Storage, Logging và Observability rồi kiểm tra cách các module phối hợp trên luồng công thức.
- Kiểm thử tích hợp toàn bộ Upload Image, Set Primary, Delete Image, Step CRUD, Reorder, Search, Logging, Health Check và Background Job.

---

## Quy ước tích hợp Bữa 8

- Nguyễn Văn Quốc tiếp tục chịu trách nhiệm Auth/User/Admin User Management và toàn bộ các kiểm tra phân quyền liên quan người dùng.
- Lê Thị Ánh Nhung tiếp tục chịu trách nhiệm Category, Home, Shared Layout và hoàn thiện giao diện tổng thể của Blog App.
- Phạm Nguyễn Ngọc Phước chịu trách nhiệm Recipe Create/Edit, Ingredient, Nutrition và toàn bộ vòng đời Recipe.
- Võ Hùng Mạnh chịu trách nhiệm Step, Image, Upload, Search, File Storage, Logging và Observability.
- `RecipeForm` chính thuộc Phạm Nguyễn Ngọc Phước; `StepEditor` và `ImageManager` thuộc Võ Hùng Mạnh.
- Khi tích hợp `StepEditor` và `ImageManager` vào Recipe Form, Phạm Nguyễn Ngọc Phước chỉ chịu trách nhiệm bố trí Layout, truyền Recipe ID, dữ liệu và callback cần thiết; không chỉnh sửa logic Step/Image bên trong.
- Võ Hùng Mạnh không chỉnh sửa Recipe Basic Information, Ingredient hoặc Nutrition trừ khi cần thay đổi interface tích hợp đã được cả hai thống nhất.
- Mọi thao tác Create, Update, Delete, Publish, Archive, Restore, Purge, Upload hoặc Set Primary phải cập nhật State hoặc kích hoạt refresh/query invalidation để UI hiển thị dữ liệu mới nhất.
- Chỉ sử dụng đúng API endpoint và DTO đã được Backend công bố; không tự tạo payload khác với hợp đồng API.
- Upload Image phải sử dụng API Client chung và `multipart/form-data` đúng chuẩn; không hard-code Content-Type multipart boundary.
- Validation phía Client chỉ nhằm cải thiện trải nghiệm người dùng; Backend vẫn là nơi quyết định cuối cùng đối với dung lượng file, định dạng file, quyền truy cập và business rule.
- Kiểm tra Logging, Health Check và Observability qua API, nhật ký và công cụ giám sát.
- Không commit file `.env`, secret, log runtime, `bin/`, `obj/`, `.next/`, `node_modules` hoặc file build/cache.
- Không đưa UI hoặc source code cũ ngoài phạm vi hiện tại vào branch tích hợp.
- Không tự ý thay đổi vai trò ownership của module khi chưa có thống nhất của nhóm.
- Trước khi Merge phải kiểm thử tối thiểu các luồng Admin User Management, Category CRUD, Create/Edit Recipe, Draft/Publish/Archive/Delete, Upload Image, Set Primary, Step CRUD/Reorder, Search, Logging và Health Check.
- Trước khi tạo Pull Request hoặc Merge vào `main` phải bảo đảm Backend build, Backend tests, `npm install`, TypeScript compile, lint và Frontend production build đều thành công.

# Chức năng mới - Phát triển chức năng mở rộng và nâng cao trải nghiệm người dùng

**Mục tiêu:** Bổ sung các chức năng tương tác cộng đồng, cá nhân hóa công thức, thống kê quản trị, quản lý nội dung nâng cao và hỗ trợ quá trình nấu ăn; tận dụng hệ thống API, PostgreSQL, Redis và các thành phần Front-end đã xây dựng từ Bữa 1 đến Bữa 8.
## Nguyễn Văn Quốc - Đánh giá/Bình luận/Báo cáo bình luận
Xây dựng chức năng đánh giá công thức theo thang điểm từ 1 đến 5 sao cho người dùng đã đăng nhập.
Thiết lập liên kết giá trị dữ liệu với ApplicationUservà Recipeđảm bảo mỗi người dùng chỉ có một giá trị đánh giá cho mỗi công thức.
Xây dựng API bổ sung, cập nhật và xóa giá đánh giá; kiểm tra JWT và quyền sở hữu trước khi thực hiện thao tác.
Tính toán và hiển thị điểm đánh giá trung bình tổng số lần đánh giá trên trang công thức chi tiết.
Xây dựng chức năng bình luận dưới công thức được xuất bản, hiển thị nội dung, tác giả và thời gian tạo bình luận.
Hoàn thiện API bổ sung, sửa, xóa bình luận và cho phép trả lời bình luận theo cấu trúc một cấp.
Xây dựng giao diện danh sách bình luận có phân trang, Đang tải, Trạng thái trống và thông báo lỗi phù hợp.
Xây dựng chức năng báo cáo bình luận vi phạm, cho phép người dùng lựa chọn lý do báo cáo và gửi yêu cầu đến hệ thống.
Thiết lập cơ chế hạn chế lặp lại báo cáo và lưu trạng thái xử lý của từng báo cáo trong PostgreSQL.
Xây dựng giao diện Quản trị viên quản lý báo cáo bình luận, cho phép xem lý do, giữ lại hoặc ẩn bình luận vi phạm.
Bảo đảm quản trị viên có quyền xử lý báo cáo và người dùng thông thường không thể chỉnh sửa bình luận của các tài khoản khác.
Kiểm tra Xếp hạng, CRUD nhận xét, Trả lời, Báo cáo nhận xét, phân quyền và dữ liệu hợp lệ các trường không hợp lệ.
## Lê Thị Ánh Nhung - Personalized Recommendations / Collections / Trending Recipes / Admin Activity Analytics / Category Performance Analytics

- Rà soát các chức năng Trang chủ, Category, Recipe Read, Dashboard và Statistics đã triển khai từ Bữa 1 đến Bữa 8 để xác định những thành phần có thể tái sử dụng, không xây dựng lại chức năng thống kê tổng quan đã có.
- Xây dựng chức năng gợi ý công thức cá nhân hóa (Personalized Recommendations) dựa trên danh mục người dùng quan tâm, lịch sử xem và những tương tác hợp lệ đã được hệ thống ghi nhận.
- Thiết kế thuật toán xếp hạng công thức theo mức độ phù hợp, ưu tiên những danh mục người dùng thường xuyên tương tác, không yêu cầu sử dụng AI hoặc Machine Learning.
- Hoàn thiện API lấy danh sách công thức được đề xuất, chỉ trả về Recipe Published và chưa bị xóa mềm, có giới hạn số lượng kết quả và cơ chế gợi ý công thức phổ biến khi chưa có lịch sử tương tác.
- Xây dựng khu vực “Có thể bạn sẽ thích” trên Trang chủ hoặc trang Khám phá, tái sử dụng `RecipeCard`, Loading Skeleton, Empty State và Error State hiện có.
- Xây dựng chức năng tạo, chỉnh sửa và xóa bộ sưu tập công thức cá nhân (Recipe Collections) cho người dùng đã đăng nhập.
- Thiết lập dữ liệu Collection liên kết với `ApplicationUser` và Recipe; cho phép một Recipe thuộc nhiều Collection nhưng không được trùng lặp trong cùng một Collection.
- Hoàn thiện API thêm và xóa công thức khỏi bộ sưu tập, hỗ trợ trạng thái công khai hoặc riêng tư và kiểm tra quyền sở hữu tại Backend.
- Xây dựng trang quản lý bộ sưu tập cá nhân, hỗ trợ xem danh sách, đổi tên, thêm hoặc loại bỏ công thức và điều hướng đến Recipe Detail.
- Xây dựng chức năng khám phá công thức thịnh hành (Trending Recipes) dựa trên lượt xem hợp lệ trong 7 ngày và 30 ngày.
- Thiết lập cơ chế ghi nhận lượt xem có giới hạn theo người dùng hoặc phiên truy cập trong khoảng thời gian phù hợp để hạn chế tăng lượt xem do tải lại trang liên tục.
- Hoàn thiện API truy vấn và xếp hạng Trending Recipes theo lượt xem, kết hợp lọc Category, sắp xếp và Redis caching khi cần thiết.
- Xây dựng giao diện Khám phá công thức thịnh hành, hiển thị thứ hạng, ảnh đại diện, tên công thức, danh mục và liên kết đến Recipe Detail.
- Xây dựng chức năng **Admin Activity Analytics** để phân tích hoạt động đăng tải công thức theo thời gian, tập trung vào các chỉ số và biểu đồ nâng cao chưa có trong Dashboard tổng quan ở Bữa 4.
- Thống kê số lượng Recipe được tạo mới và xuất bản theo từng ngày trong 7 ngày hoặc 30 ngày, hỗ trợ so sánh số liệu giữa hai khoảng thời gian tương ứng.
- Xây dựng bảng thống kê tác giả hoạt động tích cực dựa trên số lượng công thức được tạo hoặc xuất bản trong khoảng thời gian được chọn.
- Hoàn thiện API thống kê hoạt động bằng PostgreSQL Aggregate Query, `GROUP BY` và các bộ lọc thời gian, bảo đảm dữ liệu được lấy từ cơ sở dữ liệu thực tế.
- Xây dựng biểu đồ đường hoặc biểu đồ cột thể hiện xu hướng đăng công thức theo ngày, có bộ lọc 7 ngày và 30 ngày.
- Xây dựng chức năng **Admin Category Performance Analytics** để phân tích hiệu quả hoạt động của từng danh mục, không lặp lại chức năng đếm Recipe hoặc thống kê Category cơ bản đã triển khai ở Bữa 4.
- Thống kê số công thức mới được tạo và số công thức được xuất bản theo danh mục trong 7 ngày hoặc 30 ngày.
- Tính toán tỷ lệ tăng trưởng số công thức Published theo danh mục giữa khoảng thời gian hiện tại và khoảng thời gian trước đó có cùng độ dài.
- Xây dựng bảng xếp hạng danh mục tăng trưởng nhanh và danh mục ít phát sinh nội dung mới, có thể lọc, sắp xếp và điều hướng đến trang quản lý Category.
- Hoàn thiện biểu đồ so sánh hoạt động giữa các Category, tái sử dụng thư viện biểu đồ và Shared Components hiện có.
- Bảo đảm các API và trang Admin Activity Analytics, Admin Category Performance Analytics chỉ cho phép tài khoản có vai trò Admin truy cập; kiểm tra quyền tại Backend, không chỉ ẩn giao diện Frontend.
- Tận dụng `CategoryRepository`, `RecipeRepository`, API Client, Redis và hệ thống Authentication/Authorization đã có; không tạo Repository hoặc cơ chế xác thực trùng lặp.
- Bảo đảm các phép thống kê theo ngày sử dụng quy ước múi giờ nhất quán, loại trừ dữ liệu bị xóa mềm theo quy tắc nghiệp vụ và xử lý đúng trường hợp không có dữ liệu.
- Hoàn thiện Loading, Empty State, Error State và responsive trên Desktop, Tablet và Mobile cho các màn hình mở rộng.
- Kiểm thử Personalized Recommendations, Collections CRUD, Trending Recipes, Activity Analytics, Category Performance Analytics, bộ lọc thời gian, phân quyền Admin, tính đúng đắn của dữ liệu thống kê và khả năng hiển thị responsive.
- Chạy kiểm thử hồi quy Home, Category, Dashboard, Category CRUD, Filter, Sort và Pagination sau khi tích hợp, bảo đảm các chức năng đã hoàn thành ở Bữa 1 đến Bữa 8 không bị ảnh hưởng.

## Phạm Nguyễn Ngọc Phước - Duplicate Recipe / Personal Notes / Recipe Version History

- Xây dựng chức năng sao chép công thức (Duplicate Recipe) cho phép tác giả tạo một bản sao từ công thức thuộc quyền sở hữu của mình.
- Sao chép các thông tin cần thiết của Recipe gồm tiêu đề, mô tả, danh mục, thời gian chuẩn bị, thời gian nấu, khẩu phần, độ khó, nguyên liệu và các bước thực hiện.
- Bảo đảm bản sao được tạo với `Id` và `Slug` mới, trạng thái mặc định `Draft` và không thay đổi dữ liệu công thức gốc.
- Không sao chép các thông tin tương tác như lượt đánh giá, bình luận, lượt xem hoặc dữ liệu quản trị không thuộc nội dung công thức.
- Xây dựng API Duplicate Recipe, kiểm tra JWT, quyền sở hữu, dữ liệu nguồn và xử lý trường hợp công thức không tồn tại.
- Bổ sung nút “Sao chép công thức” tại khu vực quản lý công thức của tác giả và điều hướng đến bản Draft mới sau khi sao chép thành công.
- Xây dựng chức năng ghi chú cá nhân (Personal Recipe Notes), cho phép người dùng đã đăng nhập tạo ghi chú riêng khi xem một công thức.
- Thiết lập dữ liệu ghi chú liên kết với `ApplicationUser` và `Recipe`, lưu nội dung, thời gian tạo và thời gian chỉnh sửa trong PostgreSQL.
- Hoàn thiện API thêm, xem, cập nhật và xóa ghi chú; chỉ chủ sở hữu ghi chú có quyền xem hoặc thay đổi nội dung.
- Xây dựng giao diện ghi chú cá nhân trên trang Recipe Detail, hỗ trợ chỉnh sửa nội dung, lưu và xóa ghi chú.
- Xây dựng chức năng Recipe Version History để lưu lại các phiên bản nội dung khi tác giả chỉnh sửa công thức.
- Lưu thông tin phiên bản gồm số phiên bản, thời gian thay đổi, người thực hiện và dữ liệu nội dung cần theo dõi.
- Hoàn thiện API xem danh sách phiên bản và nội dung phiên bản cũ theo quyền của tác giả hoặc Admin.
- Xây dựng giao diện lịch sử chỉnh sửa và hỗ trợ so sánh những thay đổi cơ bản về tiêu đề, mô tả và nguyên liệu giữa các phiên bản.
- Chỉ triển khai chức năng xem và so sánh phiên bản; chưa yêu cầu tự động khôi phục phiên bản cũ.
- Bảo đảm các chức năng mới không làm thay đổi quy trình Draft, Publish, Archive, Soft Delete, Restore và Purge hiện có.
- Kiểm thử Duplicate Recipe, Personal Notes CRUD, Version History, quyền sở hữu, dữ liệu sao chép và tính toàn vẹn của công thức gốc.

## Võ Hùng Mạnh - Cooking Mode / Smart Timer / Search Autocomplete

- Xây dựng chức năng Cooking Mode cho phép người dùng thực hiện công thức theo từng bước chế biến trên giao diện riêng.
- Tải danh sách RecipeStep từ API công thức hiện có và hiển thị đúng thứ tự tăng dần theo `StepNumber`.
- Thiết kế giao diện hiển thị một bước nấu tại một thời điểm, bao gồm tiêu đề, mô tả, hình ảnh minh họa và thời lượng nếu có.
- Hoàn thiện nút “Bước trước”, “Bước tiếp theo” và thanh tiến độ, xử lý giới hạn bước đầu tiên và bước cuối cùng.
- Hỗ trợ hiển thị toàn màn hình khi trình duyệt cho phép và bố cục phù hợp trên Desktop, Tablet và Mobile.
- Xử lý trường hợp công thức không có bước nấu, không có hình ảnh hoặc dữ liệu bước nấu chưa tải thành công.
- Xây dựng Smart Cooking Timer sử dụng thời lượng `TimerMinutes` của từng RecipeStep làm giá trị hẹn giờ mặc định.
- Hoàn thiện các thao tác Bắt đầu, Tạm dừng, Tiếp tục và Đặt lại bộ đếm, hiển thị thời gian còn lại theo phút và giây.
- Sử dụng mốc thời gian thực để tính thời gian còn lại, hạn chế sai lệch khi người dùng chuyển tab hoặc trình duyệt giảm tần suất cập nhật giao diện.
- Hiển thị thông báo khi hết giờ và cho phép người dùng chủ động dừng hoặc khởi động lại bộ hẹn giờ.
- Bảo đảm chuyển giữa các bước nấu không làm mất hoặc tạo trùng trạng thái Timer ngoài ý muốn.
- Xây dựng chức năng Search Autocomplete trên SearchBox hiện có, hiển thị danh sách công thức gợi ý ngay khi người dùng nhập từ khóa.
- Chỉ gửi yêu cầu tìm kiếm khi từ khóa đáp ứng độ dài hợp lệ theo Backend, kết hợp Debounce để hạn chế số lượng request.
- Sử dụng `AbortController` hoặc cơ chế kiểm tra request hiện hành để tránh kết quả tìm kiếm cũ ghi đè kết quả của từ khóa mới.
- Tái sử dụng PostgreSQL Full-Text Search, `unaccent` và `pg_trgm` hiện có để tìm kiếm tên công thức tiếng Việt có dấu và không dấu.
- Bảo đảm kết quả gợi ý chỉ bao gồm Recipe Published, chưa bị xóa mềm và tuân thủ các giới hạn truy vấn của hệ thống.
- Hiển thị tên công thức, ảnh đại diện phù hợp và liên kết đến trang Recipe Detail khi người dùng chọn một gợi ý.
- Hỗ trợ thao tác bằng chuột và bàn phím, bao gồm phím mũi tên, Enter và Escape trong danh sách gợi ý.
- Hoàn thiện các trạng thái Loading, Empty State và Error State cho Search Autocomplete.
- Bảo đảm Cooking Mode, Smart Timer và Search Autocomplete không làm thay đổi dữ liệu RecipeStep, RecipeImage và Search hiện có nếu người dùng chỉ xem hoặc thao tác trên giao diện.
- Kiểm thử Cooking Mode, chuyển bước, Timer Start/Pause/Resume/Reset, trạng thái tab nền, Search Autocomplete, Debounce, kết quả không dấu, phân quyền và responsive.

## Quy ước tích hợp

- Mỗi thành viên thực hiện đúng nhóm chức năng mở rộng được phân công và không tự ý thay thế business logic thuộc sở hữu của thành viên khác.
- Trước khi bắt đầu triển khai, phải rà soát mã nguồn hiện tại để xác nhận chức năng mới chưa tồn tại hoặc chưa được thực hiện đầy đủ, tránh phát triển trùng với Bữa 1 đến Bữa 8.
- Nguyễn Văn Quốc chịu trách nhiệm Rating, Comment, Reply, Comment Report và phần kiểm duyệt bình luận.
- Lê Thị Ánh Nhung chịu trách nhiệm Personalized Recommendations, Recipe Collections, Trending Recipes, Admin Activity Analytics và Admin Category Performance Analytics; không triển khai lại Dashboard tổng quan và thống kê Category cơ bản đã có từ các bữa trước.
- Phạm Nguyễn Ngọc Phước chịu trách nhiệm Duplicate Recipe, Personal Recipe Notes và Recipe Version History.
- Võ Hùng Mạnh chịu trách nhiệm Cooking Mode, Smart Cooking Timer và Search Autocomplete.
- Các thành viên tái sử dụng API Client, Authentication State, Shared Components và cấu hình môi trường chung; không tạo các hệ thống trùng lặp nếu dự án đã có thành phần tương đương.
- Khi một màn hình sử dụng component thuộc thành viên khác, phải giữ đúng ranh giới giữa Layout và Business Logic đã thống nhất ở các bữa trước.
- Mọi API tạo mới, cập nhật hoặc xóa dữ liệu phải kiểm tra Authentication, Authorization, Validation và quyền sở hữu tại Backend.
- Các chức năng công khai chỉ được hiển thị dữ liệu Recipe Published, không làm lộ Draft, Archived hoặc bản ghi đã xóa mềm.
- Nếu bổ sung Entity hoặc thay đổi cấu trúc PostgreSQL, phải tạo Migration hợp lệ và kiểm thử trên cơ sở dữ liệu thử nghiệm trước khi tích hợp.
- Không sử dụng dữ liệu mock hoặc hard-code để thay thế dữ liệu nghiệp vụ trong phiên bản hoàn thiện.
- Mỗi chức năng phải có giao diện hoạt động thực tế, kết nối API phù hợp và bộ kiểm thử cho các trường hợp thành công, thất bại, dữ liệu không hợp lệ và thiếu quyền.
- Không được làm hỏng những chức năng đã hoàn thành theo SRS v1.2.0; phải chạy lại kiểm thử hồi quy sau khi tích hợp.
- Mỗi thành viên làm việc trên nhánh riêng, commit message bằng tiếng Việt và tạo Pull Request theo quy tắc làm việc của nhóm.
- Không push trực tiếp code chức năng lên `main`, không force push hoặc ghi đè lịch sử Git.
- Không commit `.env`, secret, file backup, dữ liệu PostgreSQL/MinIO hoặc file build/cache.
- Trước khi Merge vào `main`, phải bảo đảm Backend build, Backend Unit/Integration Tests, TypeScript compile, ESLint, Frontend production build, Jest và Playwright đều vượt qua các tiêu chí nghiệm thu.
- Chỉ tích hợp chính thức sau khi kiểm thử các luồng nghiệp vụ mới trên bản code nhóm đã tổng hợp và xác nhận không phát sinh lỗi hồi quy.

# Hướng dẫn cài đặt, chạy Docker và sử dụng website

## Giới thiệu Culinary Blog

Culinary Blog là website của Nhóm 12 để tìm, chia sẻ và quản lý công thức nấu ăn. Dự án dùng Next.js cho giao diện, ASP.NET Core cho API, PostgreSQL cho dữ liệu, Redis cho bộ nhớ đệm, MinIO cho ảnh và Nginx để truy cập web qua một địa chỉ. Docker Compose chạy các dịch vụ này cùng MailHog để xem thư thử và Seq để xem nhật ký.

## Yêu cầu cài đặt

- Git để tải mã nguồn.
- Docker Desktop có Docker Compose. Trên Windows, bật WSL 2 và dùng chế độ Linux containers.
- Mở Docker Desktop, rồi kiểm tra trong PowerShell bằng `docker version` và `docker compose version`. Lệnh `docker version` cần hiển thị cả phần Server.

## Tải mã nguồn

```powershell
git clone https://github.com/Benz2311/PTUDWNC-2026-Nhom12.git
Set-Location PTUDWNC-2026-Nhom12
```

Dùng nhánh được nhóm bàn giao. Trước khi chạy, kiểm tra thư mục có `docker-compose.yml`, `.env.example`, `backend/Dockerfile`, `frontend/culinary-blog-web/Dockerfile`, `infrastructure/nginx/nginx.conf` và `infrastructure/postgres/init-extensions.sql`. Nếu bản clone thiếu các tệp này, lấy đúng bản nguồn được nhóm bàn giao trước khi build.

## Cấu hình môi trường

Tại thư mục gốc, tạo file riêng nếu chưa có.

```powershell
if (-not (Test-Path .env.docker-test)) {
    Copy-Item .env.example .env.docker-test
}
```

Mở `.env.docker-test`. Điền `POSTGRES_PASSWORD` cho PostgreSQL, `MINIO_ROOT_USER` và `MINIO_ROOT_PASSWORD` cho MinIO, cùng `JWT_SECRET_KEY` cho API. Tạo giá trị riêng cho môi trường của bạn. Giữ cấu hình hiện có nếu đang dùng một bộ dữ liệu đã thiết lập.

Với cấu hình mới, các giá trị dưới đây đồng bộ với địa chỉ ví dụ `cb-dev` trong hướng dẫn. Các biến còn lại có thể giữ theo mẫu.

```dotenv
CONTAINER_PREFIX=cb-dev
VOLUME_PREFIX=cb_dev
NETWORK_NAME=cb_dev_net
NGINX_PORT=8088
API_PORT=5008
FRONTEND_PORT=3008
POSTGRES_PORT=55432
REDIS_PORT=56379
MINIO_PORT=9008
MINIO_CONSOLE_PORT=9009
MAILHOG_UI_PORT=8028
MAILHOG_SMTP_PORT=1028
SEQ_PORT=5348
NEXT_PUBLIC_API_URL=http://localhost:8088/api/v1
PUBLIC_ORIGIN=http://localhost:8088
DATABASE_AUTO_MIGRATE=false
```

- **Cài mới với volume trống.** Xác nhận project và tên volume dành riêng cho lần cài này. Đặt tạm `DATABASE_AUTO_MIGRATE=true` trước lần khởi chạy đầu để API tạo bảng và nạp dữ liệu mẫu. Sau khi API khởi động và tải được danh sách món, đổi lại `false` rồi áp dụng bằng `docker compose --env-file .env.docker-test -p cb-dev up -d --no-deps api`.
- **Dùng lại volume có dữ liệu.** Giữ `DATABASE_AUTO_MIGRATE=false` và giữ nguyên tên volume. Nếu bản mã mới cần cập nhật bảng, xác nhận đúng cơ sở dữ liệu Docker, sao lưu và kiểm tra migration trước khi áp dụng.

## Khởi chạy bằng Docker Compose

Dùng lệnh sau khi chạy lần đầu hoặc sau khi cập nhật mã nguồn.

```powershell
docker compose --env-file .env.docker-test -p cb-dev up -d --build
docker compose --env-file .env.docker-test -p cb-dev ps
curl.exe -f http://localhost:8088/health/ready
```

Compose phát triển có 8 dịch vụ. Chờ container chạy ổn định và kiểm tra `/health/ready` trả HTTP `200`. Sau đó mở website và thử tải danh sách công thức để xác nhận dữ liệu đã sẵn sàng.

## Truy cập hệ thống

| Dịch vụ | Địa chỉ ví dụ `cb-dev` |
| --- | --- |
| Website qua Nginx | [localhost:8088](http://localhost:8088) |
| API qua Nginx | [localhost:8088/api/v1/recipes](http://localhost:8088/api/v1/recipes) |
| API trực tiếp | [localhost:5008/health](http://localhost:5008/health) |
| Frontend trực tiếp | [localhost:3008](http://localhost:3008) |
| MailHog | [localhost:8028](http://localhost:8028) |
| MinIO Console | [localhost:9009](http://localhost:9009) |
| Seq | [localhost:5348](http://localhost:5348) |
| PostgreSQL Docker | `localhost:55432` |
| Redis Docker | `localhost:56379` |

Nếu máy bạn dùng cổng khác, truy cập theo file môi trường của máy. Khi đổi cổng web, đổi đồng thời `NGINX_PORT`, `NEXT_PUBLIC_API_URL` và `PUBLIC_ORIGIN` rồi build lại frontend.

## Hướng dẫn sử dụng cơ bản

1. Chọn **Đăng ký** hoặc **Đăng nhập**. Nếu cần xác nhận email khi chạy local, mở MailHog và bấm liên kết trong thư. Có thể dùng Google nếu dự án đã được cấu hình nút đăng nhập Google.
2. Dùng ô tìm kiếm hoặc **Danh mục món ăn**. Bấm vào một món để xem ảnh, nguyên liệu và cách nấu.
3. Trong menu ảnh đại diện, chọn **Tạo công thức mới**. Điền thông tin món, thêm nguyên liệu, bước nấu và ảnh minh họa, rồi chọn **Lưu bản nháp**.
4. Mở **Hồ sơ cá nhân → Khu vực tác giả** để vào **Quản lý công thức**. Bấm **Chỉnh sửa** cạnh món hoặc **Chỉnh sửa công thức** trên trang chi tiết của mình.
5. Khi món có ít nhất một nguyên liệu, một bước nấu và tài khoản tác giả đã xác nhận email, bấm **Xuất bản → Xuất bản ngay** để đăng công khai.
6. Tài khoản **Admin** có thể mở [Quản lý danh mục](http://localhost:8088/admin/categories) và [Quản lý người dùng](http://localhost:8088/admin/users).

## Các lệnh Docker thông dụng

Chạy các lệnh trong thư mục gốc. Khi thay đổi mã nguồn, dùng lệnh khởi chạy có `--build` ở trên.

```powershell
docker compose --env-file .env.docker-test -p cb-dev up -d
docker compose --env-file .env.docker-test -p cb-dev ps
docker compose --env-file .env.docker-test -p cb-dev logs --tail 100 api postgres frontend nginx
docker compose --env-file .env.docker-test -p cb-dev restart
docker compose --env-file .env.docker-test -p cb-dev down
```

Các lệnh lần lượt khởi động, xem trạng thái, xem log, khởi động lại và dừng hệ thống. Lệnh `down` ở trên giữ các volume có tên.

## Xử lý lỗi thường gặp

| Vấn đề | Cách kiểm tra |
| --- | --- |
| Docker Desktop chưa chạy | Mở Docker Desktop, đợi Docker Engine khởi động rồi chạy `docker version`. |
| Trùng cổng | Đổi biến cổng host tương ứng trong `.env.docker-test`. Nếu đổi cổng web, cập nhật URL theo mục truy cập. |
| Thiếu biến môi trường | Kiểm tra file `.env.docker-test` và chạy `docker compose --env-file .env.docker-test -p cb-dev config --quiet`. |
| API hoặc database chưa sẵn sàng | Xem `ps` và log của `api`, `postgres`, `redis`. Với volume trống, kiểm tra bước khởi tạo. Với volume có dữ liệu, kiểm tra đúng đích kết nối và schema trước khi xử lý. |

## Lưu ý quan trọng

- Giữ các file `.env` chứa thông tin xác thực ngoài Git.
- Dừng bằng `docker compose down` với cấu hình đúng của dự án. Tránh `down -v` và giữ các volume PostgreSQL, MinIO.
- Dùng cổng PostgreSQL Docker theo cấu hình, ví dụ `55432`. Kiểm tra đích kết nối để tránh thao tác vào PostgreSQL Windows ở cổng `5432`.
- Kiểm tra và sao lưu trước khi chạy seeder hoặc migration trên dữ liệu đã có.
