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

- Rà soát yêu cầu về người dùng, tài khoản và xác thực.
- Hoàn thiện `ApplicationUser` và thông tin cơ bản của người dùng.
- Thiết lập `RefreshToken` phục vụ quá trình xác thực tài khoản.
- Hoàn chỉnh liên kết giữa người dùng và công thức.
- Chuẩn bị dữ liệu mẫu phục vụ kiểm thử người dùng và tài khoản.

## Lê Thị Ánh Nhung - Category & Statistics

- Xác định dữ liệu liên quan đến danh mục và khai thác công thức.
- Hoàn thiện `Category` và các thông tin cần thiết.
- Thiết lập liên kết giữa `Category` và `Recipe`.
- Chuẩn bị dữ liệu phục vụ lọc, sắp xếp, phân trang và thống kê.
- Chuẩn bị dữ liệu mẫu danh mục phục vụ truy vấn và thống kê.

## Phạm Nguyễn Ngọc Phước - Recipe & Ingredient

- Xác định cấu trúc và thông tin chính của công thức.
- Hoàn thiện `Recipe` và các thuộc tính chính.
- Thiết lập `RecipeIngredient` và cấu hình `RecipeNutrition`.
- Hoàn chỉnh liên kết giữa `Recipe`, User và `Category`.
- Chuẩn bị dữ liệu mẫu cho công thức, dinh dưỡng và nguyên liệu.

## Võ Hùng Mạnh - Step & Image

- Xác định dữ liệu về các bước chế biến và hình ảnh.
- Hoàn thiện `RecipeStep` và thứ tự các bước thực hiện.
- Thiết lập `RecipeImage` và thông tin hiển thị hình ảnh.
- Hoàn chỉnh liên kết `RecipeStep`, `RecipeImage` với `Recipe`.
- Chuẩn bị dữ liệu mẫu cho bước nấu và hình ảnh.

---

# Bữa 2 - Hoàn thiện cấu hình dữ liệu và tạo dữ liệu kiểm thử

**Mục tiêu:** Hoàn thiện Entity, Configuration và dữ liệu ngẫu nhiên cho từng chức năng; tích hợp vào DbContext và Migration để tạo cơ sở dữ liệu phục vụ kiểm thử.

## Nguyễn Văn Quốc - User/Auth

- Hoàn thiện `ApplicationUser` và `RefreshToken` theo cấu trúc chung của hệ thống.
- Thiết lập Configuration cho các entity thuộc User/Auth.
- Kiểm tra và hoàn thiện quan hệ giữa `ApplicationUser`, `RefreshToken` và `Recipe`.
- Xử lý các điểm chưa thống nhất giữa Entity và Configuration thuộc chức năng User/Auth.
- Chuẩn bị dữ liệu ngẫu nhiên cho người dùng và các dữ liệu liên quan phục vụ kiểm thử.

## Lê Thị Ánh Nhung - Category & Statistics

- Hoàn thiện `Category` và Configuration tương ứng.
- Kiểm tra và hoàn thiện quan hệ giữa `Category` và `Recipe`.
- Xử lý các điểm chưa thống nhất giữa Entity và Configuration thuộc chức năng Category.
- Xây dựng dữ liệu ngẫu nhiên cho danh mục.
- Đảm bảo cơ sở dữ liệu sau khi tích hợp có ít nhất **20 Categories** phục vụ truy vấn và thống kê.

## Phạm Nguyễn Ngọc Phước - Recipe & Ingredient

- Hoàn thiện `Recipe`, `RecipeIngredient`, `RecipeNutrition` và các Configuration tương ứng.
- Kiểm tra và hoàn thiện quan hệ giữa `Recipe`, `RecipeIngredient`, `RecipeNutrition`, User và Category.
- Xử lý các điểm chưa thống nhất giữa Entity và Configuration thuộc chức năng Recipe & Ingredient.
- Xây dựng dữ liệu ngẫu nhiên cho công thức, nguyên liệu và dinh dưỡng.
- Đảm bảo cơ sở dữ liệu sau khi tích hợp có ít nhất **100 Recipes** và mỗi Recipe có ít nhất **10 nguyên liệu**.

## Võ Hùng Mạnh - Step & Image

- Hoàn thiện `RecipeStep`, `RecipeImage` và các Configuration tương ứng.
- Kiểm tra và hoàn thiện quan hệ giữa `RecipeStep`, `RecipeImage` và `Recipe`.
- Xử lý các điểm chưa thống nhất giữa Entity và Configuration thuộc chức năng Step & Image.
- Xây dựng dữ liệu ngẫu nhiên cho các bước chế biến và hình ảnh.
- Đảm bảo mỗi Recipe sau khi tích hợp có ít nhất **5 bước chế biến**.

---

# Bữa 3 - Thiết kế dữ liệu và tối ưu truy vấn

**Mục tiêu:** Hoàn thiện Repository, Unit of Work, tối ưu các truy vấn dữ liệu, kiểm tra Index và triển khai Full-Text Search theo nội dung Chương 3; đảm bảo từng thành viên tiếp tục xử lý đúng nhóm Entity đã được phân công.

## Nguyễn Văn Quốc - User/Auth

- Rà soát các truy vấn liên quan đến `ApplicationUser` và `RefreshToken`.
- Kiểm tra việc sử dụng Repository và Unit of Work cho chức năng User/Auth.
- Tối ưu các truy vấn đăng nhập, lấy thông tin người dùng và Refresh Token.
- Sử dụng `AsNoTracking()` cho các truy vấn chỉ đọc khi phù hợp.
- Kiểm tra các Index phục vụ tra cứu thường xuyên như `Email`, `UserName` và `RefreshToken`.
- Kiểm tra SQL được EF Core sinh ra và xử lý các truy vấn lấy dư dữ liệu nếu có.
- Kiểm thử các truy vấn User/Auth sau khi tối ưu.

## Lê Thị Ánh Nhung - Category & Recipe Read

- Rà soát `Category` và các truy vấn đọc dữ liệu liên quan đến Recipe.
- Hoàn thiện các truy vấn danh sách Recipe, chi tiết Recipe và Recipe theo Category.
- Hoàn thiện Filter, Sort và Pagination cho danh sách Recipe.
- Sử dụng `AsNoTracking()` cho các truy vấn chỉ đọc.
- Sử dụng Projection/DTO để chỉ lấy các trường dữ liệu cần thiết.
- Kiểm tra và xử lý N+1 Query Problem trong các truy vấn Category và Recipe.
- Sử dụng `Include()` và `AsSplitQuery()` khi cần lấy nhiều dữ liệu liên quan.
- Kiểm tra các Index phục vụ truy vấn như `CategoryId`, `Status`, `CreatedAt` và `Slug`.
- Sử dụng `EXPLAIN ANALYZE` để kiểm tra các truy vấn `GetAll`, `GetById` và `GetByCategory`.
- Kiểm thử Filter, Sort, Pagination và các truy vấn đọc sau khi tối ưu.

## Phạm Nguyễn Ngọc Phước - Recipe & Ingredient

- Rà soát cấu hình dữ liệu của `Recipe`, `RecipeIngredient` và `RecipeNutrition`.
- Kiểm tra lại quan hệ giữa Recipe, User, Category và RecipeIngredient.
- Kiểm tra cấu hình `RecipeNutrition` dưới dạng Owned Entity.
- Kiểm tra các Index của Recipe như `Slug`, `AuthorId`, `CategoryId`, `Status` và `CreatedAt`.
- Hoàn thiện Repository và Unit of Work cho các thao tác ghi dữ liệu Recipe.
- Kiểm tra việc sử dụng `SaveChangesAsync()` và Transaction trong các nghiệp vụ cập nhật dữ liệu.
- Tối ưu các truy vấn kiểm tra Slug và truy vấn lấy Recipe phục vụ Create/Update/Delete.
- Tạo và Apply Migration nếu cấu trúc dữ liệu có thay đổi.
- Kiểm thử các thao tác ghi dữ liệu Recipe và Ingredient sau khi tích hợp.

## Võ Hùng Mạnh - Step, Image & Full-Text Search

- Rà soát `RecipeStep`, `RecipeImage` và các Configuration tương ứng.
- Kiểm tra quan hệ giữa `RecipeStep`, `RecipeImage` và `Recipe`.
- Kiểm tra thứ tự các bước chế biến bằng `StepNumber`.
- Kiểm tra thứ tự hiển thị hình ảnh bằng `SortOrder` và ảnh chính của Recipe.
- Tối ưu các truy vấn đọc Step và Image bằng `AsNoTracking()` khi phù hợp.
- Kiểm tra việc load Step/Image trong Recipe Detail để hạn chế N+1 Query.
- Triển khai Full-Text Search cho Recipe bằng PostgreSQL.
- Cấu hình `SearchVector` và GIN Index phục vụ tìm kiếm.
- Sử dụng `PlainToTsQuery()` và `unaccent` để hỗ trợ tìm kiếm tiếng Việt không dấu.
- Kiểm thử tìm kiếm từ khóa `"pho bo"` và đảm bảo có thể tìm được Recipe `"Phở bò"`.
- Tạo Migration cho SearchVector/GIN Index nếu cần.
- Kiểm thử lại Step, Image và Full-Text Search sau khi tích hợp.

---

# Bữa 4 - Hoàn thiện API Endpoints

## Mục tiêu

Hoàn thành việc cài đặt tất cả API Endpoints.

Việc triển khai Backend tuân theo tài liệu SRS v1.2.0 của dự án.

### Nguyên tắc phân công

- Mỗi thành viên tiếp tục phụ trách đúng module/entity đã được giao.
- Hạn chế chỉnh sửa trực tiếp phần code của thành viên khác.
- Tất cả API phải tuân theo Clean Architecture + CQRS + MediatR.
- Validation thực hiện bằng FluentValidation.
- API lỗi trả theo RFC 7807.
- Mỗi endpoint phải có ít nhất:
  - 1 Happy Path.
  - 1 Error Case.
- Kiểm thử API bằng Scalar.
- Kiểm tra dữ liệu thực tế trên PostgreSQL sau các thao tác ghi.

---

# Nguyễn Văn Quốc - User / Authentication

## Entity phụ trách

- `ApplicationUser`
- `RefreshToken`

## API Endpoints

- [ ] `POST /api/v1/auth/register`
- [ ] `POST /api/v1/auth/login`
- [ ] `POST /api/v1/auth/google`
- [ ] `POST /api/v1/auth/refresh`
- [ ] `POST /api/v1/auth/logout`
- [ ] `POST /api/v1/auth/email/confirm`
- [ ] `POST /api/v1/auth/email/resend`
- [ ] `GET /api/v1/auth/me`
- [ ] `PATCH /api/v1/auth/me`
- [ ] `PATCH /api/v1/admin/users/{id}/status`

## Công việc chi tiết

### Register

- [ ] Tạo Request/Response DTO.
- [ ] Tạo `RegisterCommand`.
- [ ] Tạo Validator.
- [ ] Kiểm tra Email duy nhất, không phân biệt hoa thường.
- [ ] Validate Password.
- [ ] Tạo `ApplicationUser`.
- [ ] Gán role `Author`.
- [ ] Sinh Access Token.
- [ ] Sinh Refresh Token.
- [ ] Hash Refresh Token bằng SHA-256 trước khi lưu.
- [ ] Trả HTTP `201 Created`.

### Login

- [ ] Kiểm tra Email/Password.
- [ ] Kiểm tra trạng thái tài khoản.
- [ ] Xử lý AccessFailedCount.
- [ ] Xử lý Lockout.
- [ ] Sinh Access Token mới.
- [ ] Sinh Refresh Token mới.
- [ ] Lưu Refresh Token vào PostgreSQL.

### Google Login

- [ ] Nhận Google ID Token.
- [ ] Verify Signature.
- [ ] Verify Issuer.
- [ ] Verify Audience.
- [ ] Verify Expiration.
- [ ] Nếu Email đã tồn tại thì liên kết Google Login.
- [ ] Nếu chưa tồn tại thì tạo User mới.
- [ ] Gán role `Author`.
- [ ] Sinh Access Token + Refresh Token.

### Refresh Token

- [ ] Hash token client gửi lên.
- [ ] Kiểm tra token tồn tại.
- [ ] Kiểm tra Expired.
- [ ] Kiểm tra Revoked.
- [ ] Token Rotation.
- [ ] Quản lý `FamilyId`.
- [ ] Lưu `ReplacedByTokenHash`.
- [ ] Detect Refresh Token Reuse.
- [ ] Nếu reuse → revoke toàn bộ token family.

### Logout

- [ ] Không yêu cầu Access Token.
- [ ] Revoke Refresh Token.
- [ ] Logout phải idempotent.
- [ ] Luôn trả `204 No Content`.

### Email

- [ ] Confirm Email.
- [ ] Resend Email Confirmation.
- [ ] Chống User Enumeration.
- [ ] Resend luôn trả `202 Accepted`.
- [ ] Hoàn thiện `JOB-001 Welcome Email`.

### Profile

- [ ] GET Profile.
- [ ] Update `DisplayName`.
- [ ] Update `AvatarUrl`.
- [ ] Update `Bio`.
- [ ] Không cho update Email/Username qua API Profile.

### Admin User

- [ ] Admin Active/Inactive User.
- [ ] Khi Inactive → revoke toàn bộ Refresh Token của User.
- [ ] User không được tự disable chính mình.

## Kiểm thử bắt buộc

- [ ] Register thành công.
- [ ] Email trùng → `409`.
- [ ] Password không hợp lệ → `422`.
- [ ] Login thành công.
- [ ] Sai Email/Password → `401`.
- [ ] Lockout → `423`.
- [ ] Google Token sai → `401`.
- [ ] Refresh Token hết hạn.
- [ ] Refresh Token bị revoke.
- [ ] Refresh Token reuse.
- [ ] Logout nhiều lần.
- [ ] Confirm Email.
- [ ] Resend Email.
- [ ] GET/UPDATE Profile.
- [ ] Admin Disable User.

---

# Lê Thị Ánh Nhung - Category / Recipe Read / Dashboard

## Entity phụ trách

- `Category`
- Được phép đọc `Recipe` phục vụ:
  - List.
  - Detail.
  - Filter.
  - Sort.
  - Pagination.
  - Statistics.

> Thành viên 2 không phụ trách nghiệp vụ tạo/cập nhật trạng thái Recipe.

## API Endpoints

- [ ] `GET /api/v1/categories`
- [ ] `GET /api/v1/categories/{slug}`
- [ ] `POST /api/v1/categories`
- [ ] `PUT /api/v1/categories/{id}`
- [ ] `DELETE /api/v1/categories/{id}`
- [ ] `GET /api/v1/recipes`
- [ ] `GET /api/v1/recipes/{slug}`
- [ ] `GET /api/v1/admin/recipes/trash`
- [ ] API Dashboard/Statistics của nhóm.

## Công việc chi tiết

### Category List

- [ ] Query tất cả Category chưa bị xóa.
- [ ] `AsNoTracking()`.
- [ ] Sort theo Name.
- [ ] Tính `recipeCount` chỉ với Recipe Published.
- [ ] Redis Cache.
- [ ] Cache key `categories:all`.
- [ ] TTL 30 phút.

### Category Detail

- [ ] Tìm Category theo Slug.
- [ ] Không tồn tại → `404`.
- [ ] Trả Recipe thuộc Category.
- [ ] Pagination.
- [ ] Guest chỉ thấy Published.
- [ ] Author có thể thấy thêm Recipe Draft của chính mình.

### Create Category

- [ ] Chỉ Admin.
- [ ] Validate Name.
- [ ] Generate Slug.
- [ ] Nếu Slug trùng → tự thêm suffix.
- [ ] Kiểm tra Name duplicate.
- [ ] Save Database.
- [ ] Invalidate Redis Cache.
- [ ] Trả `201 Created`.

### Update Category

- [ ] Chỉ Admin.
- [ ] Update Name.
- [ ] Update Description.
- [ ] Không thay đổi Slug khi đổi Name.
- [ ] Invalidate Cache.

### Delete Category

- [ ] Chỉ Admin.
- [ ] Kiểm tra Recipe chưa Soft Delete.
- [ ] Nếu còn Recipe → `409`.
- [ ] Nếu không còn → Soft Delete Category.

### Recipe List

- [ ] Public chỉ trả Recipe `Published`.
- [ ] Hỗ trợ Pagination.
- [ ] Hỗ trợ Category Filter.
- [ ] Hỗ trợ Difficulty Filter.
- [ ] Hỗ trợ các Filter theo SRS.
- [ ] Hỗ trợ Sort.
- [ ] Hỗ trợ `mine=true`.
- [ ] Khi `mine=true`, Author xem Recipe của chính mình.
- [ ] Hỗ trợ Status Filter khi xem Recipe của mình.
- [ ] Admin được Filter bằng `authorId`.
- [ ] Author thường không được dùng `authorId`.
- [ ] Public List Redis Cache 1 phút.
- [ ] Private List không dùng Shared Cache.

### Recipe Detail

- [ ] Load Recipe.
- [ ] Load Category.
- [ ] Load Author.
- [ ] Load Ingredients.
- [ ] Load Steps.
- [ ] Load Images.
- [ ] Load Nutrition.
- [ ] Steps sort theo `StepNumber`.
- [ ] Ingredients sort theo `OrderIndex`.
- [ ] Images sort theo `OrderIndex`.
- [ ] `AsNoTracking()`.
- [ ] Sử dụng Split Query khi phù hợp.
- [ ] Kiểm tra không xảy ra N+1 Query.
- [ ] Published → Public.
- [ ] Draft/Archived → chỉ Owner/Admin.
- [ ] Public Recipe Detail Cache 5 phút.

### Admin Trash List

- [ ] Chỉ Admin.
- [ ] Query Recipe có `IsDeleted = true`.
- [ ] Ignore Global Query Filter có kiểm soát.
- [ ] Sort `DeletedAt DESC`.
- [ ] Pagination.
- [ ] Không Restore/Purge ở phần TV2.

### Dashboard / Statistics

- [ ] Tổng số Category.
- [ ] Tổng số Recipe.
- [ ] Tổng số Published Recipe.
- [ ] Tổng số Draft Recipe.
- [ ] Tổng số Archived Recipe.
- [ ] Số Recipe theo Category.
- [ ] Top Category có nhiều Recipe.
- [ ] Tỷ lệ Recipe theo Category.
- [ ] Recipe mới theo thời gian.
- [ ] Projection DTO.
- [ ] `AsNoTracking()`.
- [ ] Không N+1 Query.

## Kiểm thử bắt buộc

- [ ] Category List.
- [ ] Category Detail.
- [ ] Create Category.
- [ ] Update Category.
- [ ] Delete Category.
- [ ] Delete Category còn Recipe → `409`.
- [ ] Redis Cache Hit/Miss.
- [ ] Cache Invalidation.
- [ ] Recipe List.
- [ ] Pagination.
- [ ] Filter.
- [ ] Sort.
- [ ] `mine=true`.
- [ ] Admin `authorId`.
- [ ] Recipe Detail Published.
- [ ] Recipe Detail Draft.
- [ ] Recipe Detail Archived.
- [ ] Admin Trash.
- [ ] Dashboard Statistics.
- [ ] Kiểm tra SQL Log / N+1.

---

# Phạm Nguyễn Ngọc Phước - Recipe / Ingredient

## Entity phụ trách

- `Recipe`
- `RecipeNutrition`
- `RecipeIngredient`

## API Endpoints

- [ ] `POST /api/v1/recipes`
- [ ] `PUT /api/v1/recipes/{id}`
- [ ] `PATCH /api/v1/recipes/{id}/publish`
- [ ] `PATCH /api/v1/recipes/{id}/unpublish`
- [ ] `PATCH /api/v1/recipes/{id}/archive`
- [ ] `PATCH /api/v1/recipes/{id}/unarchive`
- [ ] `DELETE /api/v1/recipes/{id}`
- [ ] `POST /api/v1/admin/recipes/{id}/restore`
- [ ] `DELETE /api/v1/admin/recipes/{id}/purge`
- [ ] `POST /api/v1/recipes/{id}/ingredients`
- [ ] `PUT /api/v1/recipes/{id}/ingredients/{ingredientId}`
- [ ] `DELETE /api/v1/recipes/{id}/ingredients/{ingredientId}`

## Công việc chi tiết

### Create Recipe

- [ ] Owner phải là User hiện tại.
- [ ] Validate Title.
- [ ] Validate Description.
- [ ] Validate Prep Time.
- [ ] Validate Cook Time.
- [ ] Validate Servings.
- [ ] Validate Difficulty.
- [ ] Kiểm tra Category tồn tại và Active.
- [ ] Tạo Recipe ở trạng thái `Draft`.
- [ ] Generate Slug unique.
- [ ] Nếu Slug trùng → thêm suffix.
- [ ] Nutrition là Owned Entity.
- [ ] `Nutrition.Source = Manual`.
- [ ] Client không được tự thay đổi Source.

### Update Recipe

- [ ] Owner/Admin.
- [ ] Yêu cầu `If-Match`.
- [ ] Sử dụng ETag dựa trên PostgreSQL `xmin`.
- [ ] Detect Optimistic Concurrency.
- [ ] Conflict → `409`.
- [ ] Draft đổi Title → có thể đổi Slug.
- [ ] Lưu Slug cũ vào `RecipeSlugHistory`.
- [ ] Published/Archived → không đổi Slug.

### Publish

- [ ] Owner/Admin.
- [ ] Yêu cầu `If-Match`.
- [ ] Email Author đã Confirm hoặc Admin.
- [ ] Recipe có ít nhất 1 Ingredient.
- [ ] Recipe có ít nhất 1 Step.
- [ ] Category đang Active.
- [ ] Các field cơ bản hợp lệ.
- [ ] Set `Status = Published`.
- [ ] Set `PublishedAt` lần đầu.
- [ ] Idempotent.

### Unpublish

- [ ] `Published → Draft`.
- [ ] Giữ `PublishedAt`.
- [ ] Idempotent.

### Archive / Unarchive

- [ ] Archive Recipe.
- [ ] Recipe Archived không xuất hiện Public.
- [ ] Unarchive đúng state.
- [ ] Invalidate Cache.

### Delete Recipe

- [ ] Soft Delete.
- [ ] `IsDeleted = true`.
- [ ] `DeletedAt = now`.
- [ ] Không xóa Child ngay.
- [ ] Không xóa MinIO File ngay.
- [ ] Invalidate Recipe Cache.
- [ ] Invalidate Search Cache.
- [ ] Invalidate Category Cache.

### Restore

- [ ] Chỉ Admin.
- [ ] Recipe phải đang Soft Delete.
- [ ] Restore trong thời gian Retention.
- [ ] `IsDeleted = false`.
- [ ] `DeletedAt = null`.
- [ ] Invalidate Cache.

### Purge

- [ ] Chỉ Admin.
- [ ] Physical Delete Recipe.
- [ ] Delete Child Entities.
- [ ] Delete Files MinIO.
- [ ] Audit Log.
- [ ] Dùng cùng Application Service với Purge Job.

### Ingredient

- [ ] Create Ingredient.
- [ ] Update Ingredient.
- [ ] Delete Ingredient.
- [ ] Owner/Admin.
- [ ] Yêu cầu `If-Match` Recipe.
- [ ] Name 1–200.
- [ ] Quantity nullable.
- [ ] Quantity > 0 nếu có.
- [ ] Unit nullable.
- [ ] Notes max 500.
- [ ] OrderIndex.
- [ ] JSON Number decimal.
- [ ] `"1/2"` dạng string → `422`.

### Background Job

- [ ] Hoàn thiện `JOB-003 Sitemap Generation`.
- [ ] Sitemap chỉ chứa Published Recipe.
- [ ] Category pages.
- [ ] Không chứa Draft/Archived/Deleted.

## Kiểm thử bắt buộc

- [ ] Create Recipe.
- [ ] Category không tồn tại.
- [ ] Update Recipe.
- [ ] User khác update → `403`.
- [ ] Stale ETag → `409`.
- [ ] Publish thiếu Confirm Email.
- [ ] Publish thiếu Ingredient.
- [ ] Publish thiếu Step.
- [ ] Publish thành công.
- [ ] Unpublish.
- [ ] Archive.
- [ ] Unarchive.
- [ ] Soft Delete.
- [ ] Restore.
- [ ] Purge.
- [ ] Ingredient Create.
- [ ] Ingredient Update.
- [ ] Ingredient Delete.
- [ ] Quantity null.
- [ ] Quantity decimal.
- [ ] Quantity `"1/2"` → `422`.
- [ ] Sitemap Job.

---

# Võ Hùng Mạnh - Step / Image / Search / MinIO / Observability

## Entity phụ trách

- `RecipeStep`
- `RecipeImage`

## API Endpoints

### Step

- [ ] `POST /api/v1/recipes/{id}/steps`
- [ ] `PUT /api/v1/recipes/{id}/steps/{stepId}`
- [ ] `PUT /api/v1/recipes/{id}/steps/reorder`
- [ ] `DELETE /api/v1/recipes/{id}/steps/{stepId}`

### Image

- [ ] `POST /api/v1/recipes/{id}/images`
- [ ] `PATCH /api/v1/recipes/{id}/images/{imageId}`
- [ ] `DELETE /api/v1/recipes/{id}/images/{imageId}`

### Search

- [ ] `GET /api/v1/recipes/search`

### Health

- [ ] `GET /health`
- [ ] `GET /health/live`
- [ ] `GET /health/ready`

## Công việc chi tiết

### RecipeStep

- [ ] Create Step.
- [ ] Update Step.
- [ ] Delete Step.
- [ ] Reorder Step.
- [ ] Validate `title`.
- [ ] Validate `description`.
- [ ] Validate `timerMinutes >= 0`.
- [ ] Hỗ trợ `imageUrl?`.
- [ ] Server tự cấp `StepNumber`.
- [ ] StepNumber liên tục.
- [ ] Khi Delete → Renumber trong Transaction.
- [ ] Reorder phải chứa đúng toàn bộ Active Step.
- [ ] Không có `ParentStepId`.
- [ ] Không có Sub-Step trong v1.2.

### RecipeImage

- [ ] Multipart Upload.
- [ ] Nhận `file`.
- [ ] Nhận `altText?`.
- [ ] Nhận `isPrimary?`.
- [ ] Max File Size = 5 MB.
- [ ] JPEG.
- [ ] PNG.
- [ ] WebP.
- [ ] AVIF.
- [ ] Kiểm tra MIME.
- [ ] Kiểm tra Magic Bytes.
- [ ] Decode thực tế.
- [ ] Upload MinIO.
- [ ] Ảnh đầu tiên tự Primary.
- [ ] Khi set Primary mới → unset Primary cũ trong cùng Transaction.
- [ ] Update AltText.
- [ ] Update OrderIndex.
- [ ] Delete Image metadata theo SRS.
- [ ] Schedule Delete File.
- [ ] Nếu Delete Primary → ảnh OrderIndex nhỏ nhất trở thành Primary.
- [ ] MinIO unavailable → `503`.

### File Storage

- [ ] `IFileStorageService`.
- [ ] FILE-001 Upload File.
- [ ] FILE-002 Delete File.
- [ ] Unique File Name.
- [ ] Chống Path Traversal.
- [ ] Delete idempotent.
- [ ] Retry khi MinIO lỗi.

### Thumbnail Job

- [ ] Hoàn thiện `JOB-002 Image Resize / Thumbnail`.
- [ ] Original Image.
- [ ] Medium Image.
- [ ] Thumbnail Image.
- [ ] Upload các phiên bản lên MinIO.
- [ ] Update URL trong Database.
- [ ] Retry 3 lần.

### Full-Text Search

- [ ] `SearchVector`.
- [ ] PostgreSQL Trigger.
- [ ] Search trên Title.
- [ ] Search trên Description.
- [ ] PostgreSQL `unaccent`.
- [ ] Text Search Config `simple`.
- [ ] GIN Index.
- [ ] `pg_trgm`.
- [ ] Fuzzy fallback.
- [ ] Ranking theo Relevance.
- [ ] Sau đó `PublishedAt DESC`.
- [ ] Chỉ tìm Published Recipe.
- [ ] Query 2–100 ký tự.
- [ ] Pagination.
- [ ] Filter.
- [ ] Sort.
- [ ] Redis Cache 1 phút.
- [ ] Cache key vary theo query/filter/page/sort.

### Health Check

- [ ] `GET /health`.
- [ ] Check PostgreSQL.
- [ ] Check Redis.
- [ ] Check MinIO.
- [ ] `GET /health/live`.
- [ ] Liveness chỉ kiểm tra process.
- [ ] `GET /health/ready`.
- [ ] Readiness kiểm tra PostgreSQL + Redis.

### Structured Logging

- [ ] Serilog.
- [ ] CorrelationId.
- [ ] RequestPath.
- [ ] HTTP Method.
- [ ] Status Code.
- [ ] Elapsed Time.
- [ ] UserId nếu Login.
- [ ] LoggingBehavior cho MediatR.
- [ ] Warning khi request > 500ms.

### Tracing / Metrics

- [ ] OpenTelemetry.
- [ ] HTTP traces.
- [ ] EF Core traces.
- [ ] Activity TraceId.
- [ ] Request Count.
- [ ] Duration Histogram.
- [ ] Error Rate.
- [ ] Business Metrics Recipe Created/Published.

## Kiểm thử bắt buộc

- [ ] Create Step.
- [ ] Update Step.
- [ ] Delete Step.
- [ ] Delete → Renumber đúng.
- [ ] Reorder Step đúng.
- [ ] Reorder thiếu Step → lỗi.
- [ ] Upload JPEG.
- [ ] Upload PNG.
- [ ] Upload WebP.
- [ ] Upload AVIF.
- [ ] File >5MB.
- [ ] MIME giả.
- [ ] Set Primary.
- [ ] Delete Primary.
- [ ] MinIO unavailable → `503`.
- [ ] Thumbnail Job.
- [ ] Search `"pho bo"` tìm được `"Phở bò"`.
- [ ] Fuzzy Search.
- [ ] Query Search quá ngắn.
- [ ] Search Pagination.
- [ ] `/health`.
- [ ] `/health/live`.
- [ ] `/health/ready`.
- [ ] Logging có CorrelationId.
- [ ] Logging có UserId.
- [ ] Trace xuất hiện.
- [ ] Metrics hoạt động.

---

# Cài đặt PostgreSQL bằng Docker

Project sử dụng **PostgreSQL 16** chạy bằng Docker.

Cả nhóm sử dụng chung file `docker-compose.yml`. Mỗi thành viên có file `.env` riêng trên máy để thiết lập mật khẩu và port PostgreSQL.

## 1. Yêu cầu

Trước khi chạy project cần cài:

- Docker Desktop
- Git
- pgAdmin 4 (nếu muốn quản lý database bằng giao diện)

Mở Docker Desktop và chờ Docker Engine chạy.

Có thể kiểm tra bằng:

```powershell
docker info
Nếu Docker hoạt động bình thường, lệnh sẽ hiển thị thông tin của cả Client và Server.
2. Lấy code mới nhất
Clone repository nếu chưa có project:
git clone <URL_REPOSITORY>
cd PTUDWNC-2026-Nhom12

Nếu đã có project:
git pull

3. Tạo file .env
Project có sẵn file:
.env.example

File này chỉ chứa cấu hình mẫu và được đưa lên Git.
Mỗi thành viên cần tạo một file .env riêng.
Trên PowerShell:
Copy-Item .env.example .env

Sau đó mở .env.
Ví dụ:
POSTGRES_DB=culinary_blog
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_password
POSTGRES_PORT=5432

Trong đó:
- POSTGRES_DB: tên database của project.
- POSTGRES_USER: tài khoản PostgreSQL.
- POSTGRES_PASSWORD: mật khẩu PostgreSQL do mỗi thành viên tự đặt.
- POSTGRES_PORT: port PostgreSQL được mở trên máy.
Ví dụ:
POSTGRES_DB=culinary_blog
POSTGRES_USER=postgres
POSTGRES_PASSWORD=abc123
POSTGRES_PORT=5432

Không commit file .env lên Git.

File .env đã được thêm vào .gitignore.
4. Trường hợp port 5432 đã được sử dụng
Một số máy đã cài PostgreSQL trực tiếp nên port 5432 có thể đang được sử dụng.
Khi đó chỉ cần đổi trong .env:
POSTGRES_PORT=5433

Docker sẽ kết nối theo dạng:
localhost:5433 -> PostgreSQL Docker:5432

Không cần sửa docker-compose.yml.
Ví dụ .env:
POSTGRES_DB=culinary_blog
POSTGRES_USER=postgres
POSTGRES_PASSWORD=abc123
POSTGRES_PORT=5433

Mỗi thành viên có thể sử dụng port khác nhau tùy cấu hình máy.
5. Khởi động PostgreSQL
Tại thư mục gốc của project chạy:
docker compose up -d

Kiểm tra:
docker compose ps

Nếu sử dụng port 5432, kết quả sẽ có dạng:
0.0.0.0:5432->5432/tcp

Nếu sử dụng port 5433:
0.0.0.0:5433->5432/tcp

Container PostgreSQL của project có tên:
culinary-blog-postgres

6. Kiểm tra database
Kiểm tra danh sách database bên trong container:
docker exec -it culinary-blog-postgres psql -U postgres -l

Nếu khởi tạo thành công sẽ có database:
culinary_blog

Có thể truy cập trực tiếp database bằng:
docker exec -it culinary-blog-postgres psql -U postgres -d culinary_blog

Khi thấy:
culinary_blog=#

nghĩa là đã truy cập được PostgreSQL.
Để thoát:
\q

Nếu màn hình đang hiển thị danh sách và phía dưới có:
(END)

nhấn phím:
q

để quay lại terminal.
Kết nối PostgreSQL Docker bằng pgAdmin 4
1. Tạo Server
Trong pgAdmin chọn:
Servers
→ Register
→ Server...

Tab General
Nhập:
Name: Culinary Blog Docker

Tab Connection
Nhập:
Host name/address: localhost
Port: POSTGRES_PORT trong file .env
Maintenance database: culinary_blog
Username: postgres
Password: POSTGRES_PASSWORD trong file .env

Có thể bật:
Save password

sau đó chọn Save.
2. Ví dụ kết nối
Nếu .env:
POSTGRES_DB=culinary_blog
POSTGRES_USER=postgres
POSTGRES_PASSWORD=abc123
POSTGRES_PORT=5433

thì pgAdmin sử dụng:
Host: localhost
Port: 5433
Maintenance database: culinary_blog
Username: postgres
Password: abc123

Sau khi kết nối thành công sẽ thấy:
Culinary Blog Docker
└── Databases
    ├── culinary_blog
    └── postgres

Quản lý Docker
Kiểm tra container
docker compose ps

Dừng container nhưng giữ database
docker compose down

Dữ liệu PostgreSQL vẫn được giữ trong Docker volume.
Khởi động lại:
docker compose up -d

Xem log PostgreSQL
docker compose logs postgres

Theo dõi log liên tục:
docker compose logs -f postgres

Reset database
Chỉ sử dụng khi thật sự muốn xóa database local và tạo lại từ đầu:
docker compose down -v
docker compose up -d

Lưu ý: docker compose down -v sẽ xóa Docker volume và toàn bộ dữ liệu PostgreSQL local.
Không sử dụng lệnh này nếu database đang có dữ liệu cần giữ.
Cấu hình Docker Compose
File docker-compose.yml của project sử dụng các biến trong .env:
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

Không ghi trực tiếp mật khẩu cá nhân vào docker-compose.yml.
Cấu hình .env.example
File .env.example được commit lên Git để các thành viên sử dụng làm mẫu:
POSTGRES_DB=culinary_blog
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_password
POSTGRES_PORT=5432

Mỗi thành viên copy file này thành .env rồi thay đổi thông tin phù hợp với máy của mình.
Một số lỗi thường gặp
Docker Engine chưa chạy
Nếu gặp lỗi dạng:
failed to connect to the docker API
dockerDesktopLinuxEngine

mở Docker Desktop và chờ Docker Engine chạy.
Sau đó kiểm tra:
docker info

Port 5432 bị trùng
Nếu máy đang có PostgreSQL khác chạy trên port 5432, đổi trong .env:
POSTGRES_PORT=5433

Sau đó chạy lại:
docker compose down
docker compose up -d

Kiểm tra:
docker compose ps

Thay đổi password nhưng password mới không hoạt động
Các biến:
POSTGRES_DB
POSTGRES_USER
POSTGRES_PASSWORD

được PostgreSQL sử dụng khi database được khởi tạo lần đầu.
Nếu Docker volume cũ đã tồn tại, chỉ sửa .env sẽ không tự thay đổi user/password bên trong database cũ.
Trong trường hợp database local chưa có dữ liệu cần giữ và muốn khởi tạo lại hoàn toàn:
docker compose down -v
docker compose up -d

Lưu ý khi làm việc nhóm
- Cả nhóm sử dụng chung docker-compose.yml.
- Mỗi thành viên sử dụng .env riêng.
- Không push .env lên Git.
- Không đưa password cá nhân vào docker-compose.yml.
- .env.example được phép push lên Git vì chỉ chứa cấu hình mẫu.
- Tên database của project thống nhất là culinary_blog.
- Port có thể khác nhau trên từng máy.
- Trước khi làm task mới cần pull code mới nhất.
- Mỗi chức năng thực hiện trên branch riêng.
- Không sử dụng docker compose down -v nếu đang có dữ liệu cần giữ.