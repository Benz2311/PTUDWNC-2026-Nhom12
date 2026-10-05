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

---

# Bữa 1 - Xây dựng cấu trúc dữ liệu ban đầu

**Mục tiêu:** Xác định các thành phần dữ liệu chính, thiết lập quan hệ và chuẩn bị dữ liệu nền cho hệ thống.

## Nguyễn Văn Quốc - User/Auth

- Rà soát yêu cầu về người dùng, tài khoản và quy trình xác thực theo thiết kế hệ thống.
- Hoàn thiện entity `ApplicationUser` với các thuộc tính định danh và hồ sơ: `Id`, `UserName`, `Email`, `PasswordHash`, `DisplayName`, `AvatarUrl`, `Bio`, `IsActive`, `CreatedAt`.
- Khai báo các thuộc tính phục vụ phân quyền `Roles` (mặc định vai trò `Author`) và cờ trạng thái xác thực thư điện tử `EmailConfirmed`.
- Thiết lập entity `RefreshToken` phục vụ quá trình xác thực tài khoản qua JWT và cơ chế xoay vòng token: `Id`, `UserId`, `TokenHash`, `ExpiresAt`, `RevokedAt`, `ReplacedByTokenHash`, `CreatedAt`, `CreatedByIp`.
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
- Hoàn thiện entity `Recipe` kế thừa `BaseEntity` với các thuộc tính chính: `Id`, `Title`, `Slug`, `Description`, `Content`, `PrepTimeMinutes`, `CookTimeMinutes`, `Servings`, `PublishedAt`.
- Định nghĩa các Enum thuộc phạm vi bài viết: `RecipeStatus` (Draft, Published, Archived) và `DifficultyLevel` (Easy, Medium, Hard).
- Thiết lập entity `RecipeIngredient` gồm các trường: `Id`, `RecipeId`, `Name`, `Quantity` (decimal), `Unit`, `Notes`, `OrderIndex`.
- Thiết lập đối tượng giá trị/thành phần dinh dưỡng `RecipeNutrition` gồm: `Calories`, `Protein`, `Carbohydrates`, `Fat`, `Fiber`, `Sodium`.
- Hoàn chỉnh liên kết giữa `Recipe` với tác giả `ApplicationUser` (`AuthorId`, `Author`) và danh mục `Category` (`CategoryId`, `Category`).
- Thiết lập mối quan hệ 1-N giữa `Recipe` và `RecipeIngredient` (`Recipe.Ingredients`).
- Chuẩn bị dữ liệu mẫu cho công thức, định lượng nguyên liệu và bảng giá trị dinh dưỡng.

## Võ Hùng Mạnh - Step & Image

- Xác định cấu trúc dữ liệu cho quy trình các bước chế biến và bộ sưu tập hình ảnh công thức.
- Hoàn thiện entity `RecipeStep` với các thuộc tính: `Id`, `RecipeId`, `StepNumber`, `Title`, `Description`, `TimerMinutes`, `ImageUrl`, `RowVersion`.
- Thiết lập quy tắc thứ tự các bước thực hiện tuần tự thông qua `StepNumber` bắt đầu từ 1 và thời gian hẹn giờ `TimerMinutes >= 0`.
- Hoàn thiện entity `RecipeImage` với các thuộc tính: `Id`, `RecipeId`, `OriginalUrl`, `MediumUrl`, `ThumbnailUrl`, `AltText`, `IsPrimary`, `OrderIndex`, `RowVersion`.
- Thiết lập thông tin hiển thị hình ảnh: xác định ảnh đại diện chính bằng cờ `IsPrimary` và thứ tự hiển thị bằng `OrderIndex`.
- Hoàn chỉnh liên kết 1-N giữa `Recipe` với các bước chế biến `RecipeStep` (`Recipe.Steps`) và thư viện ảnh `RecipeImage` (`Recipe.Images`).
- Chuẩn bị dữ liệu mẫu cho các bước nấu chi tiết kèm đồng hồ hẹn giờ và tập hợp hình ảnh minh họa theo thứ tự.

---

# Bữa 2 - Hoàn thiện cấu hình dữ liệu và tạo dữ liệu kiểm thử

**Mục tiêu:** Hoàn thiện Entity, Configuration và dữ liệu ngẫu nhiên cho từng chức năng; tích hợp vào DbContext và Migration để tạo cơ sở dữ liệu phục vụ kiểm thử.

## Nguyễn Văn Quốc - User/Auth

- Hoàn thiện entity `ApplicationUser` và `RefreshToken` đồng bộ với cấu trúc chung của hệ thống.
- Xây dựng `ApplicationUserConfiguration` ánh xạ bảng `Users`, thiết lập độ dài và ràng buộc cho `UserName` (max 100), `Email` (max 255), `PasswordHash` (max 500), `DisplayName` (max 150), `AvatarUrl` (max 1000), `Bio` (max 1000).
- Cấu hình Unique Index cho `UserName` và `Email`, Index cho `IsActive` để tối ưu tra cứu trạng thái người dùng.
- Xây dựng `RefreshTokenConfiguration` ánh xạ bảng `RefreshTokens`, cấu hình độ dài chuỗi băm `TokenHash` (max 64, Unique Index), `ReplacedByTokenHash` (max 64), `CreatedByIp` (max 45).
- Cấu hình Index cho `UserId`, `ExpiresAt`, `RevokedAt` để tăng tốc độ kiểm tra hiệu lực token.
- Cấu hình quan hệ Foreign Key giữa `Users` và `RefreshTokens` với hành vi xóa tầng `DeleteBehavior.Cascade`.
- Kiểm tra và hoàn thiện quan hệ giữa `ApplicationUser` và `Recipe` (`AuthorId`), đảm bảo tính toàn vẹn khi người dùng tạo bài viết.
- Xử lý các điểm chưa thống nhất giữa Entity và Configuration (bỏ qua các thuộc tính không ánh xạ trực tiếp hoặc cấu hình tương thích).
- Chuẩn bị dữ liệu ngẫu nhiên (Faker/Seed Data) cho danh sách người dùng với đầy đủ trạng thái (Active, Inactive, Admin, Author) và các token kiểm thử.

## Lê Thị Ánh Nhung - Category & Statistics

- Hoàn thiện entity `Category` và cấu hình `CategoryConfiguration` ánh xạ bảng `Categories`.
- Cấu hình độ dài và ràng buộc thuộc tính: `Name` (max 100, Unique Index), `Slug` (max 120, Unique Index), `Description` (text), `ImageUrl` (max 500), `OrderIndex` (Index, Default 0).
- Cấu hình `RowVersion` làm Concurrency Token và Global Query Filter hỗ trợ xóa mềm: `HasQueryFilter(c => !c.IsDeleted)`.
- Cấu hình quan hệ Foreign Key giữa `Category` và `Recipe` với hành vi `DeleteBehavior.Restrict` nhằm chặn xóa danh mục khi còn công thức liên kết.
- Cấu hình chế độ truy cập trường ẩn (`PropertyAccessMode.Field`) cho collection `Recipes` để bảo đảm tính đóng gói của Domain Model.
- Xử lý các điểm chưa thống nhất giữa Entity và Configuration thuộc phạm vi Category.
- Xây dựng dữ liệu mẫu ngẫu nhiên cho danh mục.
- Đảm bảo cơ sở dữ liệu sau khi tích hợp có ít nhất **20 Categories** hoàn chỉnh phục vụ truy vấn và thống kê.

## Phạm Nguyễn Ngọc Phước - Recipe & Ingredient

- Hoàn thiện `Recipe`, `RecipeIngredient`, `RecipeNutrition` và các lớp Configuration tương ứng trong EF Core.
- Xây dựng `RecipeConfiguration` ánh xạ bảng `Recipes`, cấu hình độ dài và ràng buộc cho `Title` (max 200, bắt buộc), `Slug` (max 220, Unique Index), `Description` (text), `Servings` (default 1).
- Cấu hình các Index trên `Recipes`: `CategoryId`, `Status`, `CreatedAt`, Composite Index `(IsDeleted, Status, CreatedAt)` và `(CategoryId, IsDeleted, Status, CreatedAt)`.
- Cấu hình Global Query Filter xóa mềm cho Recipe: `HasQueryFilter(r => !r.IsDeleted)`.
- Xây dựng `RecipeIngredientConfiguration` ánh xạ bảng `RecipeIngredients`, cấu hình `Name` (max 200), `Quantity` có độ chính xác `precision(10, 3)`, `Unit` (max 50), `Notes` (max 500), `OrderIndex`.
- Cấu hình Foreign Key từ `RecipeIngredients` đến `Recipes` với hành vi `DeleteBehavior.Cascade` và Index trên `RecipeId`.
- Xây dựng `RecipeNutritionConfiguration` ánh xạ `RecipeNutrition` dưới dạng Owned Entity (`OwnsOne`), ánh xạ các cột dinh dưỡng `Nutrition_Calories`, `Nutrition_Protein`, `Nutrition_Carbohydrates`, `Nutrition_Fat`, `Nutrition_Fiber`, `Nutrition_Sodium` với kiểu decimal `precision(8, 2)`.
- Cấu hình `RowVersion` cho `Recipe` và `RecipeIngredient` để kiểm soát xung đột dữ liệu đồng thời.
- Xây dựng dữ liệu ngẫu nhiên (DbSeeder/Bogus) cho công thức, nguyên liệu và giá trị dinh dưỡng.
- Đảm bảo cơ sở dữ liệu sau khi tích hợp có ít nhất **100 Recipes** và mỗi Recipe có ít nhất **10 nguyên liệu** hợp lệ.

## Võ Hùng Mạnh - Step & Image

- Hoàn thiện `RecipeStep`, `RecipeImage` và các lớp Configuration tương ứng trong EF Core.
- Xây dựng `RecipeStepConfiguration` ánh xạ bảng `RecipeSteps`, cấu hình `StepNumber` (bắt buộc), `Title` (max 200, nullable), `Description` (text/bắt buộc), `TimerMinutes`, `ImageUrl` (max 500), `RowVersion`.
- Cấu hình Foreign Key từ `RecipeSteps` đến `Recipes` với hành vi `DeleteBehavior.Cascade`.
- Cấu hình Unique Filtered Index cho `(RecipeId, StepNumber)` với điều kiện lọc `"IsDeleted" = false` nhằm chống trùng lặp thứ tự bước active.
- Cấu hình Check Constraint cho `RecipeSteps`: `CK_RecipeSteps_StepNumber` (`StepNumber >= 1`) và `CK_RecipeSteps_TimerMinutes` (`TimerMinutes IS NULL OR TimerMinutes >= 0`).
- Xây dựng `RecipeImageConfiguration` ánh xạ bảng `RecipeImages`, cấu hình `OriginalUrl` (max 2048, bắt buộc), `MediumUrl` (max 2048), `ThumbnailUrl` (max 2048), `AltText` (max 200), `OrderIndex` (bắt buộc), `IsPrimary`, `RowVersion`.
- Cấu hình Foreign Key từ `RecipeImages` đến `Recipes` với hành vi `DeleteBehavior.Cascade`.
- Cấu hình Index cho `(RecipeId, OrderIndex)` phục vụ truy vấn danh sách ảnh theo thứ tự.
- Cấu hình Unique Filtered Index cho `(RecipeId, IsPrimary)` với điều kiện lọc `"IsPrimary" = true AND "IsDeleted" = false` để đảm bảo mỗi công thức chỉ có duy nhất 1 ảnh đại diện chính còn hiệu lực.
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
- Hoàn thiện Filter (theo trạng thái, danh mục, độ khó), Sort (theo ngày tạo, ngày xuất bản) và Pagination (`Skip`/`Take`) cho danh sách Recipe.
- Sử dụng `AsNoTracking()` cho tất cả các truy vấn chỉ đọc để giảm thiểu chi phí theo dõi đối tượng của EF Core.
- Sử dụng Projection/DTO để chỉ trích xuất đúng các cột dữ liệu cần thiết từ PostgreSQL.
- Kiểm tra và xử lý triệt để N+1 Query Problem khi tải danh mục kèm số lượng công thức (`RecipeCount`) hoặc tải danh sách bài viết.
- Sử dụng `Include()` hợp lý và kết hợp `AsSplitQuery()` khi nạp nhiều quan hệ 1-N (Ingredients, Steps, Images) nhằm tránh bùng nổ dữ liệu tích Descartes (Cartesian Explosion).
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
- Kiểm tra và xử lý xung đột đồng thời thông qua Concurrency Token `RowVersion` để bảo vệ dữ liệu công thức.
- Tạo và áp dụng Migration nếu cấu trúc dữ liệu hoặc chỉ mục có sự thay đổi.
- Kiểm thử các thao tác ghi dữ liệu Recipe và Ingredient sau khi tích hợp.

## Võ Hùng Mạnh - Step, Image & Full-Text Search

- Rà soát `RecipeStep`, `RecipeImage` và các Configuration tương ứng trong EF Core.
- Kiểm tra và bảo đảm toàn vẹn quan hệ giữa `RecipeStep`, `RecipeImage` và `Recipe`.
- Đảm bảo dữ liệu các bước chế biến luôn được truy vấn đúng thứ tự tăng dần theo `StepNumber`.
- Đảm bảo dữ liệu hình ảnh được sắp xếp đúng theo `OrderIndex` và xác định chính xác ảnh đại diện chính `IsPrimary`.
- Tối ưu các truy vấn đọc Step và Image độc lập hoặc trong Recipe Detail bằng `AsNoTracking()` khi phù hợp.
- Kiểm tra việc nạp dữ liệu Step/Image trong truy vấn chi tiết Recipe để hạn chế N+1 Query.
- Triển khai Full-Text Search cho Recipe trên PostgreSQL bằng `SearchVector` (`NpgsqlTsVector`).
- Cấu hình chỉ mục GIN Index trên `SearchVector` phục vụ tìm kiếm toàn văn nhanh chóng.
- Tích hợp extension PostgreSQL `unaccent` và hàm `PlainToTsQuery()` với cấu hình `simple` để hỗ trợ tìm kiếm tiếng Việt không dấu.
- Xây dựng logic xếp hạng kết quả tìm kiếm theo độ liên quan (`ts_rank`) kết hợp sắp xếp phụ theo `PublishedAt DESC`.
- Kiểm thử thực tế tìm kiếm từ khóa không dấu `"pho bo"` và đảm bảo tìm thấy chính xác Recipe `"Phở bò"`.
- Tạo Migration cho PostgreSQL extension, SearchVector và GIN Index trên cơ sở dữ liệu.
- Kiểm thử lại toàn diện các truy vấn Step, Image và Full-Text Search sau khi tích hợp.

---

# Bữa 4 - Hoàn thiện API Endpoints và Giao diện

**Mục tiêu:** Hoàn thiện các API Endpoint và giao diện tương ứng theo SRS v1.2.0; mỗi thành viên tiếp tục phụ trách đúng module/entity đã được giao, kết nối giao diện với API thật và kiểm thử các luồng chính trước khi tích hợp.

---

## Nguyễn Văn Quốc - User / Authentication / Profile / Admin User Management

- Hoàn thiện các API xác thực: Đăng ký (`POST /api/v1/auth/register`), Đăng nhập (`POST /api/v1/auth/login`), Đăng nhập bằng Google (`POST /api/v1/auth/google`), Cấp lại token (`POST /api/v1/auth/refresh`) và Đăng xuất (`POST /api/v1/auth/logout`).
- Triển khai cơ chế bảo mật Refresh Token: xoay vòng token (Rotation), phát hiện sử dụng lại token cũ (Reuse Detection) và lập tức thu hồi toàn bộ token family khi phát hiện token bị tái sử dụng.
- Hoàn thiện API xác thực email (`POST /api/v1/auth/email/confirm`), gửi lại email xác thực (`POST /api/v1/auth/email/resend` có cơ chế chống User Enumeration) và tác vụ gửi Welcome Email khi đăng ký thành công.
- Hoàn thiện API lấy thông tin người dùng hiện tại (`GET /api/v1/auth/me`).
- Hoàn thiện API cập nhật hồ sơ người dùng (`PATCH /api/v1/auth/me`): cho phép cập nhật `DisplayName`, `AvatarUrl`, `Bio`; ngăn chặn tuyệt đối việc chỉnh sửa Email hoặc Username qua endpoint này.
- Hoàn thiện API quản trị trạng thái tài khoản (`PATCH /api/v1/admin/users/{id}/status`): kích hoạt hoặc vô hiệu hóa (Active/Inactive), tự động thu hồi toàn bộ Refresh Token của user khi bị vô hiệu hóa, và chặn không cho Admin tự khóa chính mình.
- Xây dựng giao diện Đăng nhập (Login) và Đăng ký (Register) với form validation đầy đủ (email format, mật khẩu mạnh).
- Xây dựng nút Đăng nhập bằng Google (Google Login), màn hình Xác thực email (Confirm Email) và nút Gửi lại thư xác thực (Resend Confirmation).
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
- Hoàn thiện API danh sách công thức (`GET /api/v1/recipes`) hỗ trợ phân trang (`page`, `pageSize`), bộ lọc (danh mục, độ khó, trạng thái), sắp xếp đa tiêu chí và hỗ trợ cờ `mine=true` để tác giả xem công thức của chính mình (hoặc Admin lọc theo `authorId`).
- Hoàn thiện API xem chi tiết công thức (`GET /api/v1/recipes/{slug}`): nạp đầy đủ thông tin danh mục, tác giả, nguyên liệu (theo `OrderIndex`), các bước nấu (theo `StepNumber`), bộ sưu tập ảnh (theo `OrderIndex` và đánh dấu `IsPrimary`) cùng thông tin dinh dưỡng.
- Bảo đảm quy tắc bảo mật trong API xem chi tiết: công thức `Published` được xem công khai; công thức `Draft` hoặc `Archived` chỉ cho phép tác giả sở hữu hoặc Admin truy cập.
- Hoàn thiện API thùng rác công thức cho Admin (`GET /api/v1/admin/recipes/trash`) truy vấn các công thức đã bị xóa mềm (`IsDeleted = true`), hỗ trợ phân trang và sắp xếp giảm dần theo thời gian xóa.
- Hoàn thiện API Dashboard/Thống kê (`GET /api/v1/dashboard/stats`): tổng số Category, tổng số Recipe, thống kê công thức theo trạng thái (Published/Draft/Archived), số lượng công thức theo từng danh mục, Top Category và biểu đồ công thức mới theo thời gian.
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
- Hoàn thiện các API chuyển đổi trạng thái công thức: Xuất bản (`POST /api/v1/recipes/{id}/publish`), Hủy xuất bản (`POST /api/v1/recipes/{id}/unpublish`), Lưu trữ (`POST /api/v1/recipes/{id}/archive`) và Mở lưu trữ (`POST /api/v1/recipes/{id}/unarchive`).
- Kiểm tra nghiêm ngặt điều kiện nghiệp vụ khi Publish: tài khoản tác giả đã xác thực email (`EmailConfirmed`), công thức phải có ít nhất 1 nguyên liệu và ít nhất 1 bước chế biến, danh mục đang hoạt động.
- Hoàn thiện API xóa mềm công thức (`DELETE /api/v1/recipes/{id}`), API Admin Khôi phục (`POST /api/v1/admin/recipes/{id}/restore`) và API Admin Xóa vĩnh viễn (`DELETE /api/v1/admin/recipes/{id}/purge`).
- Triển khai cơ chế kiểm soát đồng thời lạc quan (Optimistic Concurrency) sử dụng ETag và header `If-Match`, phát hiện xung đột dữ liệu và trả về mã lỗi `409 Conflict`.
- Duy trì lịch sử Slug cũ (`RecipeSlugHistory`) khi thay đổi tiêu đề công thức ở trạng thái Draft nhằm phục vụ định tuyến và SEO.
- Hoàn thiện các API quản lý nguyên liệu: Thêm nguyên liệu (`POST /api/v1/recipes/{id}/ingredients`), Cập nhật nguyên liệu (`PUT /api/v1/recipes/{id}/ingredients/{ingredientId}`) và Xóa nguyên liệu (`DELETE /api/v1/recipes/{id}/ingredients/{ingredientId}`).
- Kiểm tra tính hợp lệ dữ liệu nguyên liệu: `Name` (1–200 ký tự), `Quantity` (chấp nhận số thập phân decimal, từ chối chuỗi phân số `"1/2"` và trả về lỗi `422 Unprocessable Entity`), `Unit`, `Notes`, `OrderIndex`.
- Hoàn thiện xử lý thông tin dinh dưỡng trong công thức, đảm bảo trường nguồn gốc `Nutrition.Source` được quản lý đúng phía hệ thống (mặc định Manual, chặn client can thiệp trái phép).
- Triển khai tác vụ nền (Background Job) tự động tạo và cập nhật Sitemap cho hệ thống theo phạm vi công thức xuất bản.
- Xử lý nghiệp vụ Xóa vĩnh viễn (Purge): xóa tầng triệt để các thực thể con (nguyên liệu, bước nấu, ảnh), kích hoạt dọn dẹp file tương ứng trên MinIO và ghi nhận Audit Log.
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
- Rà soát validation dữ liệu bước: tiêu đề (tối đa 200 ký tự), nội dung hướng dẫn (bắt buộc), hẹn giờ `TimerMinutes >= 0`.
- Hoàn thiện các API quản lý hình ảnh: Tải lên hình ảnh (`POST /api/v1/recipes/{id}/images`), Cập nhật thuộc tính ảnh (`PATCH /api/v1/recipes/{id}/images/{imageId}`) và Xóa ảnh (`DELETE /api/v1/recipes/{id}/images/{imageId}`).
- Xử lý tải file qua `multipart/form-data`: giới hạn dung lượng tối đa 5 MB, hỗ trợ các định dạng JPEG, PNG, WebP, AVIF; kiểm tra MIME type, Magic Bytes và decode nội dung thực tế để chống mã độc.
- Cài đặt quy tắc nghiệp vụ ảnh đại diện chính (`IsPrimary`): ảnh tải lên đầu tiên tự động thành Primary; khi đặt ảnh mới làm Primary thì tự động hủy cờ Primary của ảnh cũ trong cùng transaction; khi xóa ảnh Primary hiện tại thì tự động thăng hạng ảnh còn lại có `OrderIndex` nhỏ nhất lên làm Primary.
- Tích hợp dịch vụ lưu trữ đối tượng MinIO (dùng AWS SDK S3): đặt tên file duy nhất (UUID), chống tấn công đường dẫn (Path Traversal), xử lý xóa an toàn (idempotent delete) và cơ chế thử lại (retry) khi gặp sự cố mạng.
- Xây dựng tác vụ nền (Hangfire Background Job) tự động tạo ảnh kích thước trung bình (Medium 800x600) và ảnh thu nhỏ (Thumbnail 300x300), tải lên MinIO và cập nhật URL vào database.
- Hoàn thiện API tìm kiếm nâng cao (`GET /api/v1/recipes/search`): kết hợp Full-Text Search qua `SearchVector` (PostgreSQL `unaccent`, từ điển cấu hình `simple`) và Fuzzy Search mờ qua `pg_trgm`, xếp hạng độ liên quan `ts_rank` kết hợp `PublishedAt DESC`, lọc theo danh mục/thời gian, phân trang và đệm Redis Cache 1 phút (khóa cache biến đổi linh hoạt theo query/filter/sort/page).
- Hoàn thiện các điểm kiểm tra sức khỏe hệ thống (Health Check): `/health` (tổng thể), `/health/live` (tiến trình), `/health/ready` (sẵn sàng kết nối PostgreSQL, Redis, MinIO).
- Hoàn thiện ghi log có cấu trúc (Structured Logging) bằng Serilog gắn kèm `CorrelationId`, thông tin User và cảnh báo khi thời gian xử lý yêu cầu vượt quá 500ms.
- Tích hợp giám sát vết và chỉ số (Tracing & Metrics) sử dụng OpenTelemetry cho HTTP requests và EF Core queries.
- Xây dựng thành phần Chỉnh sửa bước thực hiện (`StepEditor`): hiển thị danh sách các bước theo thứ tự `StepNumber`, cho phép thêm bước mới, chỉnh sửa tiêu đề, mô tả hướng dẫn, thời gian hẹn giờ (bấm giờ) và đổi vị trí các bước trực quan.
- Xây dựng thành phần Quản lý thư viện ảnh (`ImageManager`): hiển thị lưới ảnh (Gallery), xem trước ảnh khi chọn (Preview), nút tải ảnh lên với thanh tiến trình; cho phép cập nhật `AltText`, thay đổi thứ tự `OrderIndex`, nút bấm Đặt làm ảnh đại diện (`Set as Primary`) và nút Xóa ảnh.
- Xử lý tiền kiểm tra (client validation) về dung lượng tệp (< 5 MB) và đuôi file hợp lệ trên giao diện để tối ưu trải nghiệm người dùng; hiển thị lỗi kết nối MinIO `503 Service Unavailable` và tự động cập nhật lại huy hiệu ảnh Primary trên giao diện sau khi thao tác.
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

# Cài đặt PostgreSQL bằng Docker

Project sử dụng **PostgreSQL 16** chạy bằng Docker.

Cả nhóm sử dụng chung file `docker-compose.yml`. Mỗi thành viên có file `.env` riêng trên máy để thiết lập mật khẩu và port PostgreSQL.

---

## 1. Yêu cầu

Trước khi chạy project cần cài:

- Docker Desktop
- Git
- pgAdmin 4 *(nếu muốn quản lý database bằng giao diện)*

Mở Docker Desktop và chờ Docker Engine chạy.

Có thể kiểm tra bằng:

```powershell
docker info
```

Nếu Docker hoạt động bình thường, lệnh sẽ hiển thị thông tin của cả **Client** và **Server**.

---

## 2. Lấy code mới nhất

### Trường hợp chưa có project

Clone repository:

```powershell
git clone <URL_REPOSITORY>
cd PTUDWNC-2026-Nhom12
```

### Trường hợp đã có project

Lấy code mới nhất:

```powershell
git pull
```

---

## 3. Tạo file `.env`

Project có sẵn file:

```text
.env.example
```

File này chỉ chứa cấu hình mẫu và được đưa lên Git.

Mỗi thành viên cần tạo một file `.env` riêng trên máy.

Trên PowerShell:

```powershell
Copy-Item .env.example .env
```

Sau đó mở file `.env` và cấu hình.

Ví dụ:

```env
POSTGRES_DB=culinary_blog
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_password
POSTGRES_PORT=5432
```

### Ý nghĩa các biến

| Biến | Ý nghĩa |
|---|---|
| `POSTGRES_DB` | Tên database của project |
| `POSTGRES_USER` | Tài khoản PostgreSQL |
| `POSTGRES_PASSWORD` | Mật khẩu PostgreSQL do mỗi thành viên tự đặt |
| `POSTGRES_PORT` | Port PostgreSQL được mở trên máy |

Ví dụ cấu hình thực tế:

```env
POSTGRES_DB=culinary_blog
POSTGRES_USER=postgres
POSTGRES_PASSWORD=abc123
POSTGRES_PORT=5432
```

> **Lưu ý:** Không commit file `.env` lên Git.

File `.env` đã được thêm vào `.gitignore`.

---

## 4. Trường hợp port `5432` đã được sử dụng

Một số máy đã cài PostgreSQL trực tiếp nên port `5432` có thể đang được sử dụng.

Khi đó chỉ cần đổi trong `.env`:

```env
POSTGRES_PORT=5433
```

Docker sẽ kết nối theo dạng:

```text
localhost:5433 -> PostgreSQL Docker:5432
```

Không cần sửa `docker-compose.yml`.

Ví dụ `.env`:

```env
POSTGRES_DB=culinary_blog
POSTGRES_USER=postgres
POSTGRES_PASSWORD=abc123
POSTGRES_PORT=5433
```

Mỗi thành viên có thể sử dụng port khác nhau tùy cấu hình máy.

---

## 5. Khởi động PostgreSQL

Tại thư mục gốc của project chạy:

```powershell
docker compose up -d
```

Kiểm tra:

```powershell
docker compose ps
```

Nếu sử dụng port `5432`, kết quả sẽ có dạng:

```text
0.0.0.0:5432->5432/tcp
```

Nếu sử dụng port `5433`:

```text
0.0.0.0:5433->5432/tcp
```

Container PostgreSQL của project có tên:

```text
culinary-blog-postgres
```

---

## 6. Kiểm tra database

Kiểm tra danh sách database bên trong container:

```powershell
docker exec -it culinary-blog-postgres psql -U postgres -l
```

Nếu khởi tạo thành công sẽ có database:

```text
culinary_blog
```

Có thể truy cập trực tiếp database bằng:

```powershell
docker exec -it culinary-blog-postgres psql -U postgres -d culinary_blog
```

Khi thấy:

```text
culinary_blog=#
```

nghĩa là đã truy cập được PostgreSQL.

Để thoát:

```text
\q
```

Nếu màn hình đang hiển thị danh sách và phía dưới có:

```text
(END)
```

nhấn phím:

```text
q
```

để quay lại terminal.

---

# Kết nối PostgreSQL Docker bằng pgAdmin 4

## 1. Tạo Server

Trong pgAdmin chọn:

```text
Servers
→ Register
→ Server...
```

### Tab General

Nhập:

```text
Name: Culinary Blog Docker
```

### Tab Connection

Nhập:

```text
Host name/address: localhost
Port: POSTGRES_PORT trong file .env
Maintenance database: culinary_blog
Username: postgres
Password: POSTGRES_PASSWORD trong file .env
```

Có thể bật:

```text
Save password
```

Sau đó chọn **Save**.

---

## 2. Ví dụ kết nối

Nếu `.env`:

```env
POSTGRES_DB=culinary_blog
POSTGRES_USER=postgres
POSTGRES_PASSWORD=abc123
POSTGRES_PORT=5433
```

thì pgAdmin sử dụng:

```text
Host: localhost
Port: 5433
Maintenance database: culinary_blog
Username: postgres
Password: abc123
```

Sau khi kết nối thành công sẽ thấy:

```text
Culinary Blog Docker
└── Databases
    ├── culinary_blog
    └── postgres
```

---

# Quản lý Docker

## Kiểm tra container

```powershell
docker compose ps
```

## Dừng container nhưng giữ database

```powershell
docker compose down
```

Dữ liệu PostgreSQL vẫn được giữ trong Docker volume.

## Khởi động lại

```powershell
docker compose up -d
```

## Xem log PostgreSQL

```powershell
docker compose logs postgres
```

Theo dõi log liên tục:

```powershell
docker compose logs -f postgres
```

---

# Reset database

Chỉ sử dụng khi thật sự muốn xóa database local và tạo lại từ đầu:

```powershell
docker compose down -v
docker compose up -d
```

> **Lưu ý:** `docker compose down -v` sẽ xóa Docker volume và toàn bộ dữ liệu PostgreSQL local.

Không sử dụng lệnh này nếu database đang có dữ liệu cần giữ.

---

# Cấu hình Docker Compose

File `docker-compose.yml` của project sử dụng các biến trong `.env`:

```yaml
services:
  postgres:
    image: postgres:16
    container_name: culinary-blog-postgres
    restart: unless-stopped

    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}

    ports:
      - "${POSTGRES_PORT}:5432"

    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

> Không ghi trực tiếp mật khẩu cá nhân vào `docker-compose.yml`.

---

# Cấu hình `.env.example`

File `.env.example` được commit lên Git để các thành viên sử dụng làm mẫu:

```env
POSTGRES_DB=culinary_blog
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_password
POSTGRES_PORT=5432
```

Mỗi thành viên copy file này thành `.env` rồi thay đổi thông tin phù hợp với máy của mình.

---

# Một số lỗi thường gặp

## 1. Docker Engine chưa chạy

Nếu gặp lỗi dạng:

```text
failed to connect to the docker API
dockerDesktopLinuxEngine
```

Mở Docker Desktop và chờ Docker Engine chạy.

Sau đó kiểm tra:

```powershell
docker info
```

---

## 2. Port `5432` bị trùng

Nếu máy đang có PostgreSQL khác chạy trên port `5432`, đổi trong `.env`:

```env
POSTGRES_PORT=5433
```

Sau đó chạy lại:

```powershell
docker compose down
docker compose up -d
```

Kiểm tra:

```powershell
docker compose ps
```

---

## 3. Thay đổi password nhưng password mới không hoạt động

Các biến:

```text
POSTGRES_DB
POSTGRES_USER
POSTGRES_PASSWORD
```

được PostgreSQL sử dụng khi database được khởi tạo lần đầu.

Nếu Docker volume cũ đã tồn tại, chỉ sửa `.env` sẽ không tự thay đổi User/Password bên trong database cũ.

Trong trường hợp database local chưa có dữ liệu cần giữ và muốn khởi tạo lại hoàn toàn:

```powershell
docker compose down -v
docker compose up -d
```

> **Cảnh báo:** Lệnh `docker compose down -v` sẽ xóa database local hiện tại.

---

# Lưu ý khi làm việc nhóm

- Cả nhóm sử dụng chung `docker-compose.yml`.
- Mỗi thành viên sử dụng `.env` riêng.
- Không push `.env` lên Git.
- Không đưa password cá nhân vào `docker-compose.yml`.
- `.env.example` được phép push lên Git vì chỉ chứa cấu hình mẫu.
- Tên database của project thống nhất là `culinary_blog`.
- Port có thể khác nhau trên từng máy.
- Trước khi làm task mới cần pull code mới nhất.
- Mỗi chức năng thực hiện trên branch riêng.
- Không sử dụng `docker compose down -v` nếu đang có dữ liệu cần giữ.