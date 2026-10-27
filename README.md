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

---

---

# Bữa 5 - Hoàn thiện bảo mật, tài liệu API và nền tảng Front-end

**Mục tiêu:** Hoàn thành các cấu hình bảo mật gồm CORS, Rate Limiting và Authentication/Authorization; hoàn thiện tài liệu đặc tả API và API Versioning; đồng thời xây dựng môi trường, cấu trúc dự án Front-end, cài đặt các thư viện cần thiết và các kiểu dữ liệu TypeScript phục vụ ứng dụng.

---

## Nguyễn Văn Quốc - Authentication / CORS / User Types

- Cấu hình và hoàn thiện JWT Authentication cho Backend.
- Thiết lập Issuer, Audience, Signing Key và thời hạn Access Token.
- Hoàn thiện cơ chế Authorization theo các vai trò `User`, `Author` và `Admin`.
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
- Đối chiếu các Types với DTO/Response thật của Backend.
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

- Hoàn thiện chức năng Đăng ký tài khoản với form Register gồm Username, Email, Password, Confirm Password và các trường hồ sơ ban đầu nếu Backend hỗ trợ.
- Thực hiện validation phía Client theo đúng yêu cầu Backend, bao gồm định dạng Email, độ mạnh Password và xác nhận Password.
- Hoàn thiện chức năng Đăng nhập bằng Email/Username và Password, tích hợp với JWT Authentication và `auth-store.ts`.
- Xử lý đầy đủ các trạng thái đăng nhập thành công, sai thông tin đăng nhập, tài khoản bị vô hiệu hóa, email chưa xác thực và lỗi Backend.
- Hoàn thiện chức năng Đăng xuất, gọi API Logout, xóa trạng thái Authentication và điều hướng người dùng về trang phù hợp.
- Hoàn thiện chức năng Đổi mật khẩu với các trường mật khẩu hiện tại, mật khẩu mới và xác nhận mật khẩu mới.
- Hoàn thiện chức năng Quên mật khẩu, xây dựng form nhập Email để yêu cầu Reset Password và xử lý response theo đúng cơ chế chống User Enumeration của Backend.
- Xây dựng màn hình Reset Password nếu Backend sử dụng Reset Token và hoàn thiện luồng thay đổi mật khẩu thông qua token.
- Hoàn thiện trang Profile hiển thị Avatar, Username, Email, DisplayName, Bio, Roles và trạng thái tài khoản.
- Xây dựng form Update Profile cho phép cập nhật DisplayName, AvatarUrl và Bio theo đúng API; không cho phép chỉnh Email hoặc Username nếu SRS không cho phép.
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
- Hiển thị đầy đủ các thông tin cơ bản gồm Title, Description, Content, Author, Category, Difficulty, PrepTimeMinutes, CookTimeMinutes, Servings và PublishedAt.
- Hiển thị ảnh Primary của Recipe ở vị trí nổi bật và hiển thị bộ sưu tập ảnh phụ theo thứ tự nếu Recipe có nhiều hình ảnh.
- Hiển thị danh sách Ingredient đúng theo `OrderIndex`, bao gồm Name, Quantity, Unit và Notes.
- Hiển thị thông tin Nutrition gồm Calories, Protein, Carbohydrates, Fat, Fiber và Sodium nếu Recipe có dữ liệu dinh dưỡng.
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
- Đồng bộ Search State với URL để người dùng reload trang vẫn giữ lại từ khóa và filter nếu kiến trúc hiện tại hỗ trợ.
- Không gửi request khi từ khóa không hợp lệ hoặc chưa đạt độ dài tối thiểu.
- Kiểm thử tìm kiếm tiếng Việt không dấu với trường hợp `"pho bo"` và xác nhận có thể tìm thấy Recipe `"Phở bò"` khi Backend và dữ liệu hiện tại hỗ trợ.
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
- Các thao tác Filter, Sort và Pagination phải giữ đúng query parameter để người dùng có thể reload hoặc quay lại trang mà không làm mất trạng thái khi kiến trúc hiện tại hỗ trợ.
- Không sửa hoặc ghi đè phần nghiệp vụ của thành viên khác khi chỉ cần tích hợp UI.
- Trước khi Merge phải kiểm tra Register, Login, Logout, Profile, Home, Category, Recipe Detail và Search hoạt động trên cùng một Front-end mà không xung đột route hoặc state.
- Phải chạy TypeScript compile, lint, frontend build và kiểm tra các API chính trước khi tạo Pull Request.

---

# Bữa 8 - Hoàn thiện quản lý Recipe, Upload Image, Admin và Logging

**Mục tiêu:** Hoàn thiện các chức năng nâng cao gồm upload ảnh, tạo/cập nhật và quản lý vòng đời Recipe, quản trị người dùng, Category, Logging và các chức năng còn thiếu theo SRS; tích hợp đầy đủ các module trước khi thực hiện tổng kiểm thử toàn hệ thống.

---

## Nguyễn Văn Quốc - Admin User Management / Auth Completion

- Hoàn thiện trang danh sách người dùng dành cho Admin, kết nối trực tiếp với API quản trị User.
- Hiển thị các thông tin người dùng gồm Id, UserName, Email, DisplayName, Role, IsActive và CreatedAt.
- Hỗ trợ phân biệt các vai trò Admin, Author và User theo dữ liệu Backend.
- Hoàn thiện Search, Filter và Pagination cho danh sách người dùng nếu API hỗ trợ.
- Xây dựng giao diện xem thông tin chi tiết người dùng hoặc Dialog hiển thị các thông tin cần thiết.
- Hoàn thiện thao tác kích hoạt và vô hiệu hóa tài khoản, hiển thị Confirmation Dialog trước các thao tác có ảnh hưởng đến người dùng.
- Bảo đảm Admin không thể tự vô hiệu hóa chính mình theo đúng quy tắc Backend.
- Xử lý và hiển thị chính xác các lỗi `403 Forbidden`, `404 Not Found`, `409 Conflict` từ API.
- Kiểm tra việc thu hồi token hoặc buộc đăng xuất đối với tài khoản bị vô hiệu hóa nếu Backend đã triển khai.
- Rà soát toàn bộ các chức năng Authentication đã hoàn thiện ở Bữa 7 gồm Register, Login, Logout, Refresh Token, Change Password, Forgot Password và Profile.
- Hoàn thiện các chức năng Auth/User còn thiếu theo SRS thuộc phạm vi Nguyễn Văn Quốc.
- Kiểm thử Role-based Authorization giữa Admin, Author và User.
- Bảo đảm người không có quyền Admin không thể truy cập trang quản lý người dùng.

---

## Lê Thị Ánh Nhung - Category Completion / Home & Layout Finalization

- Hoàn thiện các chức năng Category còn thiếu theo SRS, bao gồm giao diện quản lý Category dành cho Admin.
- Xây dựng và hoàn thiện chức năng tạo Category với các trường Name, Description, ImageUrl, OrderIndex và các trường Backend thực tế yêu cầu.
- Xây dựng chức năng cập nhật Category và đồng bộ dữ liệu mới trên UI sau khi thao tác thành công.
- Xây dựng chức năng xóa Category, hiển thị Confirmation Dialog và xử lý lỗi `409 Conflict` khi Category vẫn còn Recipe liên kết.
- Rà soát Category List và Category Detail sau khi Recipe Create/Edit được tích hợp, bảo đảm `recipeCount` được cập nhật đúng.
- Rà soát Trang chủ sau khi các chức năng Recipe hoàn thiện, kiểm tra danh sách Recipe mới, Sidebar Category, Pagination, Header và Footer.
- Hoàn thiện giao diện tổng thể Blog App về responsive, spacing, typography, card layout và mobile navigation.
- Rà soát các Shared Components gồm Header, Footer, Sidebar, Pagination, Loading, Error và Empty State để bảo đảm cách hiển thị thống nhất giữa các page.
- Hoàn thiện các chức năng Category, Recipe Read hoặc Dashboard còn thiếu theo SRS thuộc phạm vi Lê Thị Ánh Nhung.
- Không can thiệp business logic Recipe Create/Edit thuộc Phạm Nguyễn Ngọc Phước.
- Kiểm thử Category CRUD, Home, Layout và responsive sau khi tích hợp toàn hệ thống.

---

## Phạm Nguyễn Ngọc Phước - Recipe Create / Update / Lifecycle

- Hoàn thiện trang tạo mới Recipe và trang cập nhật Recipe, kết nối trực tiếp với các API ghi dữ liệu của Backend.
- Xây dựng Recipe Form đầy đủ với các trường Title, Description, Content, Category, PrepTimeMinutes, CookTimeMinutes, Servings và Difficulty.
- Tích hợp `IngredientEditor` cho phép thêm, sửa, xóa và sắp xếp nguyên liệu, hỗ trợ Name, Quantity, Unit, Notes và OrderIndex.
- Tích hợp `NutritionEditor` cho phép nhập và cập nhật Calories, Protein, Carbohydrates, Fat, Fiber và Sodium.
- Thực hiện luồng tạo Recipe ở trạng thái Draft và cập nhật Recipe Draft.
- Hoàn thiện các thao tác chuyển trạng thái Recipe gồm Draft → Published, Published → Unpublished nếu Backend hỗ trợ, Published → Archived, Archived → Unarchive và Recipe → Deleted.
- Hiển thị trạng thái hiện tại của Recipe bằng Status Badge và cập nhật giao diện ngay sau khi thao tác trạng thái thành công.
- Kiểm tra đầy đủ điều kiện Publish như Category hợp lệ, có ít nhất một Ingredient, có ít nhất một Step và Author đã xác thực Email nếu SRS yêu cầu.
- Tích hợp `StepEditor` của Võ Hùng Mạnh vào Recipe Form mà không thay đổi logic nội bộ của component.
- Tích hợp `ImageManager` của Võ Hùng Mạnh vào Recipe Form theo nguyên tắc Phước chịu trách nhiệm bố trí layout, Mạnh chịu trách nhiệm logic Step/Image.
- Hoàn thiện Confirmation Dialog cho các thao tác Delete, Publish, Archive, Purge hoặc các thao tác nguy hiểm khác.
- Hoàn thiện xử lý Optimistic Concurrency bằng ETag/`If-Match` nếu Backend đang sử dụng, hiển thị lỗi `409 Conflict` khi dữ liệu bị cập nhật đồng thời.
- Hoàn thiện các action tại My Recipes gồm Edit, Publish, Archive và Delete.
- Rà soát các chức năng Restore/Purge dành cho Admin nếu thuộc phạm vi SRS và API đã triển khai.
- Hoàn thiện các chức năng Recipe, Ingredient và Nutrition còn thiếu theo SRS.
- Kiểm thử toàn bộ vòng đời Recipe gồm Create, Save Draft, Edit, Publish, Archive, Delete và Restore/Purge nếu có.

---

## Võ Hùng Mạnh - Image Upload / Step Integration / Logging / Observability

- Hoàn thiện tính năng Upload Image cho Recipe, kết nối ImageManager với API upload ảnh thật và xử lý `multipart/form-data` đúng chuẩn.
- Hỗ trợ chọn file bằng File Picker, Drag & Drop nếu component có, hiển thị Preview trước khi upload và Upload Progress nếu API Client hỗ trợ.
- Thực hiện Client Validation trước khi upload dựa trên giới hạn dung lượng tối đa 5 MB và các định dạng file thực tế Backend đang hỗ trợ.
- Hoàn thiện ImageManager cho phép upload ảnh, cập nhật AltText, thay đổi OrderIndex, đặt ảnh làm Primary và xóa ảnh.
- Tự động refresh danh sách ảnh sau các mutation và cập nhật trạng thái Primary trên UI ngay sau thao tác Set Primary.
- Xử lý lỗi Object Storage/MinIO, bao gồm `503 Service Unavailable`, upload thất bại, file không hợp lệ và các lỗi kết nối.
- Hoàn thiện chức năng upload ảnh cho Recipe Step nếu Backend hỗ trợ endpoint tương ứng.
- Hoàn thiện `StepEditor` khi tích hợp thật với API, hỗ trợ Add Step, Update Step, Delete Step, Reorder Step, TimerMinutes và Step Image.
- Kiểm tra `StepNumber` được cập nhật chính xác sau khi xóa hoặc thay đổi thứ tự Step.
- Bảo đảm `StepEditor` và `ImageManager` hoạt động ổn định khi được nhúng vào Recipe Form của Phạm Nguyễn Ngọc Phước.
- Không thay đổi business logic Recipe Basic Information, Ingredient hoặc Nutrition.
- Hoàn thiện Structured Logging bằng Serilog và ghi log cho các sự kiện quan trọng như lỗi request, lỗi upload ảnh, lỗi MinIO, lỗi Search, lỗi Background Job và exception hệ thống.
- Bổ sung `CorrelationId` vào log để hỗ trợ truy vết request và ghi thông tin UserId/UserName khi request đã được xác thực nếu kiến trúc hiện tại hỗ trợ.
- Kiểm tra cơ chế Slow Request Logging và cảnh báo khi thời gian xử lý vượt ngưỡng cấu hình.
- Rà soát và hoàn thiện Health Check tại `/health`, `/health/live` và `/health/ready` theo cấu hình thực tế của hệ thống.
- Rà soát OpenTelemetry Tracing & Metrics cho HTTP Request, EF Core và các dependency phù hợp.
- Kiểm tra Thumbnail Background Job sau khi upload ảnh và bảo đảm các URL Medium/Thumbnail được cập nhật đúng.
- Rà soát và hoàn thiện các chức năng còn thiếu theo SRS trong phạm vi Step, Image, Search, File Storage, Logging và Observability.
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
- Logging, Health Check và Observability là thành phần hạ tầng; không xây dựng UI riêng nếu SRS không yêu cầu.
- Không commit file `.env`, secret, log runtime, `bin/`, `obj/`, `.next/`, `node_modules` hoặc file build/cache.
- Không đưa UI hoặc source code cũ ngoài phạm vi hiện tại vào branch tích hợp.
- Không tự ý thay đổi vai trò ownership của module khi chưa có thống nhất của nhóm.
- Trước khi Merge phải kiểm thử tối thiểu các luồng Admin User Management, Category CRUD, Create/Edit Recipe, Draft/Publish/Archive/Delete, Upload Image, Set Primary, Step CRUD/Reorder, Search, Logging và Health Check.
- Trước khi tạo Pull Request hoặc Merge vào `main` phải bảo đảm Backend build, Backend tests, `npm install`, TypeScript compile, lint và Frontend production build đều thành công.

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
