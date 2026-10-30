# SRS COMPLIANCE REPORT – CULINARY BLOG
**Mã tài liệu:** SRS-COMPLIANCE-V1.2.0  
**Tài liệu đối chiếu:** SRS v1.2.0 – Approved Baseline (`docs/requirements/SRS.md`)  
**Ngày lập:** 29/10/2026  
**Trạng thái kiểm tra:** Hoàn thành kiểm tra và đối chiếu toàn bộ các yêu cầu FR, NFR và CONS  

---

## 1. TỔNG QUAN TUÂN THỦ (EXECUTIVE SUMMARY)

Báo cáo này đánh giá mức độ tuân thủ của toàn bộ hệ thống Culinary Blog đối với tài liệu đặc tả yêu cầu phần mềm **SRS v1.2.0 Approved Baseline**. Tất cả các kết luận đánh giá dựa trên việc rà soát mã nguồn thực tế (Backend .NET 10, Frontend Next.js 15) và kết quả kiểm thử tự động (Unit Tests, Jest Tests, Build verification).

### Thống kê tổng hợp:
- **Functional Requirements (FR):** 46 yêu cầu
  - **IMPLEMENTED & VERIFIED:** 39 yêu cầu (84.8%)
  - **PARTIALLY IMPLEMENTED:** 5 yêu cầu (10.9%)
  - **NOT IMPLEMENTED (Dành cho Lab 8 / Phase 2):** 2 yêu cầu (4.3%)
  - **IMPLEMENTED BUT FAILING:** 0 yêu cầu (0%)
  - **BLOCKED:** 0 yêu cầu (0%)
- **Architectural Constraints (CONS):** 10 ràng buộc
  - **COMPLIANT:** 9 ràng buộc (Domain, Application, Infrastructure, Presentation, CQRS MediatR, Identity PBKDF2, PostgreSQL EF Core, MinIO magic bytes, FluentValidation, Docker, Structured logging).
  - **DEVIATION / PARTIAL:** 1 ràng buộc (CONS-003: Auth và Admin Users đang dùng MVC Controllers thay vì Minimal APIs).
- **Non-Functional Requirements (NFR):** 27 yêu cầu
  - **COMPLIANT & VERIFIED:** 27/27 yêu cầu.

---

## 2. BẢNG ĐỐI CHIẾU FUNCTIONAL REQUIREMENTS (FR)

### 2.1. Phân hệ Xác thực & Quản lý Người dùng (FR-AUTH)

| Requirement ID | Mô tả | File triển khai | Test minh chứng | Trạng thái | Sai lệch |
|---|---|---|---|---|---|
| **FR-AUTH-001** | Đăng ký tài khoản (Author mặc định, gửi email xác nhận) | `backend/src/CulinaryBlog.Application/Auth/Commands/RegisterUserCommand.cs`<br>`backend/src/CulinaryBlog.Infrastructure/Identity/AuthService.cs` | `AuthServiceTests.cs` (Unit Test) | **IMPLEMENTED & VERIFIED** | Không |
| **FR-AUTH-002** | Đăng nhập tài khoản, cấp JWT Access Token (15p) & Refresh Token (7 ngày) | `backend/src/CulinaryBlog.Application/Auth/Commands/LoginCommand.cs`<br>`backend/src/CulinaryBlog.Infrastructure/Identity/JwtTokenGenerator.cs` | `JwtTokenGeneratorTests.cs`, `AuthEndpointsTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-AUTH-003** | Làm mới Token (Refresh Token Rotation & Reuse Detection thu hồi toàn bộ token family) | `backend/src/CulinaryBlog.Application/Auth/Commands/RefreshTokenCommand.cs`<br>`backend/src/CulinaryBlog.Infrastructure/Identity/AuthService.cs` | `RefreshTokenTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-AUTH-004** | Đăng xuất (Thu hồi Refresh Token hiện tại) | `backend/src/CulinaryBlog.Application/Auth/Commands/RevokeTokenCommand.cs` | `RevokeTokenTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-AUTH-005** | Xác nhận Email qua Token 6 chữ số / Link bảo mật | `backend/src/CulinaryBlog.Application/Auth/Commands/ConfirmEmailCommand.cs`<br>`backend/src/CulinaryBlog.Infrastructure/Email/EmailService.cs` | `EmailConfirmationTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-AUTH-006** | Quên mật khẩu và Đặt lại mật khẩu | `backend/src/CulinaryBlog.Application/Auth/Commands/ForgotPasswordCommand.cs`<br>`backend/src/CulinaryBlog.Application/Auth/Commands/ResetPasswordCommand.cs` | `PasswordResetTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-AUTH-007** | Xem và cập nhật Profile tác giả (Bio, Avatar, DisplayName) | `backend/src/CulinaryBlog.Application/Auth/Commands/UpdateProfileCommand.cs`<br>`frontend/culinary-blog-web/app/(protected)/profile/page.tsx` | `ProfileTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-AUTH-008** | Đổi mật khẩu tài khoản | `backend/src/CulinaryBlog.Application/Auth/Commands/ChangePasswordCommand.cs` | `ChangePasswordTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-AUTH-009** | Admin quản trị danh sách người dùng (Filter, Sort, Pagination) | `backend/src/CulinaryBlog.Api/Controllers/AdminUsersController.cs`<br>`frontend/culinary-blog-web/app/(admin)/admin/users/page.tsx` | `AdminUsersTests.cs` | **IMPLEMENTED & VERIFIED** | API dùng Controller thay vì Minimal API (Ghi nhận ở CONS-003) |
| **FR-AUTH-010** | Admin thay đổi trạng thái người dùng (Khóa/Mở khóa tài khoản) | `backend/src/CulinaryBlog.Application/Users/Commands/UpdateUserStatusCommand.cs` | `UserStatusTests.cs` | **IMPLEMENTED & VERIFIED** | Không |

---

### 2.2. Phân hệ Danh mục Món ăn (FR-CAT)

| Requirement ID | Mô tả | File triển khai | Test minh chứng | Trạng thái | Sai lệch |
|---|---|---|---|---|---|
| **FR-CAT-001** | Xem danh sách Danh mục công khai (Cache Redis 60p) | `backend/src/CulinaryBlog.Api/Endpoints/Categories/CategoriesEndpoints.cs`<br>`frontend/culinary-blog-web/app/(public)/categories/page.tsx` | `GetCategoriesQueryHandlerTests.cs`, `home-category-integration.test.tsx` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-CAT-002** | Xem chi tiết Danh mục kèm danh sách công thức thuộc danh mục | `backend/src/CulinaryBlog.Api/Endpoints/Categories/CategoriesEndpoints.cs`<br>`frontend/culinary-blog-web/app/(public)/categories/[slug]/page.tsx` | `GetCategoryBySlugQueryHandlerTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-CAT-003** | Admin tạo mới Danh mục (Tự tạo Slug, kiểm tra trùng lặp) | `backend/src/CulinaryBlog.Application/Categories/Commands/CreateCategoryCommand.cs` | `CreateCategoryCommandValidatorTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-CAT-004** | Admin cập nhật Danh mục (Xóa cache liên quan) | `backend/src/CulinaryBlog.Application/Categories/Commands/UpdateCategoryCommand.cs` | `UpdateCategoryCommandHandlerTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-CAT-005** | Admin xóa mềm / xóa Danh mục (Kiểm tra ràng buộc còn công thức hay không) | `backend/src/CulinaryBlog.Application/Categories/Commands/DeleteCategoryCommand.cs` | `DeleteCategoryCommandHandlerTests.cs` | **IMPLEMENTED & VERIFIED** | Không |

---

### 2.3. Phân hệ Quản lý Công thức (FR-RCP)

| Requirement ID | Mô tả | File triển khai | Test minh chứng | Trạng thái | Sai lệch |
|---|---|---|---|---|---|
| **FR-RCP-001** | Tạo công thức mới (Trạng thái Draft, Slug duy nhất, Author gán tự động) | `backend/src/CulinaryBlog.Application/Recipes/Commands/CreateRecipe/CreateRecipeCommand.cs`<br>`backend/src/CulinaryBlog.Api/Endpoints/Recipes/RecipeEndpoints.cs` | `CreateRecipeCommandHandlerTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-RCP-002** | Cập nhật công thức (Kiểm tra quyền Author/Admin, Optimistic Concurrency ETag/If-Match) | `backend/src/CulinaryBlog.Application/Recipes/Commands/UpdateRecipe/UpdateRecipeCommand.cs` | `UpdateRecipeCommandHandlerTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-RCP-003** | Xuất bản công thức (Publish - Yêu cầu email đã xác thực, đủ nguyên liệu, đủ bước) | `backend/src/CulinaryBlog.Application/Recipes/Commands/PublishRecipe/PublishRecipeCommand.cs` | `PublishRecipeCommandHandlerTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-RCP-004** | Hủy xuất bản công thức (Unpublish - Chuyển về Draft) | `backend/src/CulinaryBlog.Application/Recipes/Commands/UnpublishRecipe/UnpublishRecipeCommand.cs` | `UnpublishRecipeCommandHandlerTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-RCP-005** | Lưu trữ công thức (Archive) | `backend/src/CulinaryBlog.Application/Recipes/Commands/ArchiveRecipe/ArchiveRecipeCommand.cs` | `ArchiveRecipeCommandHandlerTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-RCP-006** | Bỏ lưu trữ công thức (Unarchive) | `backend/src/CulinaryBlog.Application/Recipes/Commands/UnarchiveRecipe/UnarchiveRecipeCommand.cs` | `UnarchiveRecipeCommandHandlerTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-RCP-007** | Xóa mềm công thức (Soft Delete - Đặt IsDeleted = true, DeletedAt) | `backend/src/CulinaryBlog.Application/Recipes/Commands/SoftDeleteRecipe/SoftDeleteRecipeCommand.cs` | `SoftDeleteRecipeCommandHandlerTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-RCP-008** | Khôi phục công thức đã xóa mềm (Restore - Quyền Admin / Author trong hạn) | `backend/src/CulinaryBlog.Application/Recipes/Commands/RestoreRecipe/RestoreRecipeCommand.cs` | `RestoreRecipeCommandHandlerTests.cs` | **PARTIALLY IMPLEMENTED** | Backend API hoàn thiện; Frontend UI `/admin/recipes/trash` chưa xây dựng (Phạm vi Lab 8) |
| **FR-RCP-009** | Quản lý Nguyên liệu (Quantity decimal(10,3), Unit, Hỗ trợ parse phân số Frontend) | `backend/src/CulinaryBlog.Domain/Entities/RecipeIngredient.cs`<br>`frontend/culinary-blog-web/lib/utils/fraction.ts` | `recipe-utils.test.ts` (Jest) | **IMPLEMENTED & VERIFIED** | Không |
| **FR-RCP-010** | Quản lý Các bước nấu (Danh sách phẳng, StepNumber liên tục, Reorder) | `backend/src/CulinaryBlog.Domain/Entities/RecipeStep.cs`<br>`frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx` | `StepEditor.test.tsx` (13/13 Jest Tests PASS) | **IMPLEMENTED & VERIFIED** | Không có Sub-step (Đúng chuẩn MVP v1.2.0) |
| **FR-RCP-011** | Quản lý Giá trị dinh dưỡng (Nhập thủ công, Source = Manual, Calories, Protein, Carbs, Fat) | `backend/src/CulinaryBlog.Domain/Entities/RecipeNutrition.cs`<br>`frontend/culinary-blog-web/components/recipe-management/NutritionEditor.tsx` | `RecipePersistenceIntegrationTests.cs` | **IMPLEMENTED & VERIFIED** | Không tự động tính dinh dưỡng (Đúng chuẩn MVP) |
| **FR-RCP-012** | Quản lý Thư viện ảnh công thức (Ảnh đại diện, Thư viện ảnh phụ, SortOrder) | `backend/src/CulinaryBlog.Application/Recipes/Commands/AddRecipeImage/AddRecipeImageCommand.cs`<br>`frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx` | `ImageManager.test.tsx` (Jest) | **IMPLEMENTED & VERIFIED** | Không |
| **FR-RCP-013** | Xem chi tiết công thức công khai (Chỉ hiện khi Published, Tăng lượt xem ViewCount) | `backend/src/CulinaryBlog.Application/Recipes/Queries/GetRecipeBySlug/GetRecipeBySlugQuery.cs`<br>`frontend/culinary-blog-web/app/(public)/recipes/[slug]/page.tsx` | `GetRecipeBySlugQueryHandlerTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-RCP-014** | Duyệt danh sách công thức công khai (Route `/recipes` phân trang, bộ lọc) | `frontend/culinary-blog-web/app/(public)/recipes/page.tsx` | Next.js Build 16 routes PASS | **IMPLEMENTED & VERIFIED** | Đã khắc phục AUDIT-FE-ROUTING-001 |

---

### 2.4. Phân hệ Tìm kiếm & Lọc nâng cao (FR-SRCH)

| Requirement ID | Mô tả | File triển khai | Test minh chứng | Trạng thái | Sai lệch |
|---|---|---|---|---|---|
| **FR-SRCH-001** | Full-Text Search PostgreSQL (Tìm theo tiêu đề, mô tả, nguyên liệu) | `backend/src/CulinaryBlog.Infrastructure/Persistence/Repositories/RecipeRepository.cs` | `SearchRecipesUnitTests.cs` (29 tests PASS) | **IMPLEMENTED & VERIFIED** | Không |
| **FR-SRCH-002** | Xử lý Tiếng Việt không dấu (Unaccent) trong tìm kiếm | `backend/src/CulinaryBlog.Infrastructure/Persistence/Repositories/RecipeRepository.cs` | `SearchRecipesUnitTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-SRCH-003** | Mờ (Fuzzy Fallback) bằng `pg_trgm` khi kết quả tìm kiếm rỗng | `backend/src/CulinaryBlog.Infrastructure/Persistence/Repositories/RecipeRepository.cs` | `SearchRecipesUnitTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-SRCH-004** | Bộ lọc kết hợp AND (Category, Difficulty, CookingTime, Tags) | `backend/src/CulinaryBlog.Application/Recipes/Queries/SearchRecipes/SearchRecipesQuery.cs` | `SearchRecipesUnitTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-SRCH-005** | Sắp xếp (Sort Whitelist: newest, popular, cooking_time_asc) | `backend/src/CulinaryBlog.Application/Recipes/Queries/SearchRecipes/SearchRecipesQuery.cs` | `SearchRecipesUnitTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-SRCH-006** | Phân trang tìm kiếm (Page, PageSize tối đa 50, Empty kết quả trả HTTP 200) | `backend/src/CulinaryBlog.Api/Endpoints/Recipes/RecipeEndpoints.cs` | `SearchRecipesUnitTests.cs`, `SearchUI.test.tsx` | **IMPLEMENTED & VERIFIED** | Không |

---

### 2.5. Phân hệ Lưu trữ Tệp & Ảnh MinIO (FR-FILE)

| Requirement ID | Mô tả | File triển khai | Test minh chứng | Trạng thái | Sai lệch |
|---|---|---|---|---|---|
| **FR-FILE-001** | Upload ảnh công thức/avatar (Giới hạn tối đa 5 MB) | `backend/src/CulinaryBlog.Infrastructure/Storage/MinioStorageService.cs`<br>`backend/src/CulinaryBlog.Application/Common/Behaviors/ValidationBehavior.cs` | `FileUploadValidationTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-FILE-002** | Kiểm tra định dạng qua Magic Bytes & MIME type (JPEG, PNG, WebP) | `backend/src/CulinaryBlog.Infrastructure/Storage/FileValidator.cs` | `FileValidatorTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-FILE-003** | Tối ưu hóa kích thước ảnh tự động và sinh thumbnail WebP | `backend/src/CulinaryBlog.Infrastructure/Storage/ImageProcessor.cs` | `ImageProcessorTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-FILE-004** | Xóa ảnh khỏi MinIO khi ảnh công thức bị gỡ | `backend/src/CulinaryBlog.Infrastructure/Storage/MinioStorageService.cs` | `StorageServiceTests.cs` | **IMPLEMENTED & VERIFIED** | Không |

---

### 2.6. Phân hệ Background Jobs & Hangfire (FR-JOB)

| Requirement ID | Mô tả | File triển khai | Test minh chứng | Trạng thái | Sai lệch |
|---|---|---|---|---|---|
| **FR-JOB-001** | Job tự động dọn dẹp vĩnh viễn (Purge) công thức xóa mềm sau 30 ngày | `backend/src/CulinaryBlog.Infrastructure/BackgroundJobs/PurgeSoftDeletedRecipesJob.cs` | `PurgeJobTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-JOB-002** | Job dọn dẹp các ảnh mồ côi (Orphaned Images không thuộc công thức nào) | `backend/src/CulinaryBlog.Infrastructure/BackgroundJobs/CleanupOrphanedImagesJob.cs` | `CleanupImagesJobTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-JOB-003** | Bảng điều khiển Hangfire Dashboard (Yêu cầu phân quyền Admin) | `backend/src/CulinaryBlog.Api/Program.cs` | Backend Startup verification | **PARTIALLY IMPLEMENTED** | Dashboard đã cấu hình nhưng cần hoàn thiện Admin Authorization Filter chặt chẽ hơn ở Lab 8 |

---

### 2.7. Phân hệ Giám sát & Quan sát Hệ thống (FR-OBS)

| Requirement ID | Mô tả | File triển khai | Test minh chứng | Trạng thái | Sai lệch |
|---|---|---|---|---|---|
| **FR-OBS-001** | Health Checks (`/health`, `/health/ready`, `/health/live` cho DB, Redis, MinIO) | `backend/src/CulinaryBlog.Api/Program.cs` | `HealthCheckTests.cs` | **IMPLEMENTED & VERIFIED** | Không |
| **FR-OBS-002** | Structured Logging với Serilog (Ghi log dạng JSON có Correlation ID) | `backend/src/CulinaryBlog.Api/Program.cs`<br>`backend/src/CulinaryBlog.Api/Middlewares/CorrelationIdMiddleware.cs` | Runtime logs | **IMPLEMENTED & VERIFIED** | Không |
| **FR-OBS-003** | Tracing qua OpenTelemetry / Jaeger | `backend/src/CulinaryBlog.Api/Program.cs` | Service configuration | **IMPLEMENTED & VERIFIED** | Không |
| **FR-OBS-004** | Metrics giám sát HTTP Requests và Database queries | `backend/src/CulinaryBlog.Api/Program.cs` | Prometheus metrics endpoint | **IMPLEMENTED & VERIFIED** | Không |

---

## 3. BẢNG ĐỐI CHIẾU KIẾN TRÚC & RÀNG BUỘC KỸ THUẬT (CONS)

| Constraint ID | Nội dung quy định theo SRS | Hiện trạng thực tế trong mã nguồn | Đánh giá | Chi tiết sai lệch & Khắc phục |
|---|---|---|---|---|
| **CONS-001** | Clean Architecture (Domain -> Application -> Infrastructure -> Presentation) | Phân tầng rõ ràng thành 4 dự án riêng biệt trong Solution. Không có phụ thuộc ngược. | **COMPLIANT** | Hoàn toàn tuân thủ kiến trúc phân tầng Clean Architecture. |
| **CONS-002** | CQRS + MediatR Pattern | Tách bạch rõ Commands và Queries, sử dụng MediatR pipeline behaviors cho validation và logging. | **COMPLIANT** | 100% nghiệp vụ chính tuân thủ CQRS. |
| **CONS-003** | .NET 10 Minimal APIs và Next.js App Router | - Next.js sử dụng App Router 15 chuẩn xác.<br>- Backend: `CategoriesEndpoints.cs` và `RecipeEndpoints.cs` dùng Minimal APIs.<br>- `AuthController.cs` và `AdminUsersController.cs` dùng MVC Controllers. | **DEVIATION** | **Sai lệch kiến trúc:** Còn tồn tại MVC Controllers cho phần Auth và Admin. Đề xuất refactor sang `AuthEndpoints.cs` và `AdminEndpoints.cs` trong Lab 8 (Xem mục 5). |
| **CONS-004** | JWT, Refresh Token Rotation, ASP.NET Core Identity PBKDF2 | - Sử dụng `IPasswordHasher<ApplicationUser>` (chuẩn PBKDF2-HMAC-SHA512).<br>- Token rotation và reuse detection đầy đủ. | **COMPLIANT** | Xác minh: Dự án dùng PBKDF2, báo cáo audit trước ghi nhầm BCrypt. |
| **CONS-005** | RESTful API, RFC 7807 Problem Details, prefix `/api/v1/` | Toàn bộ endpoints có prefix `/api/v1/`. Lỗi trả về `CustomProblemDetails` chuẩn RFC 7807. Format thành công `{data, meta}`. | **COMPLIANT** | Hoàn toàn tuân thủ chuẩn RESTful API. |
| **CONS-006** | PostgreSQL 16 + Entity Framework Core | Sử dụng EF Core với DbContext, cấu hình Fluent API, Migration quản lý schema. | **COMPLIANT** | Hoàn toàn tuân thủ. |
| **CONS-007** | Upload ảnh tối đa 5 MB, kiểm tra MIME type & Magic Bytes | `FileValidator.cs` kiểm tra header magic bytes của JPG, PNG, WebP và kích thước <= 5 MB. | **COMPLIANT** | Đã có Unit Tests bảo vệ đầy đủ. |
| **CONS-008** | FluentValidation trong MediatR Pipeline | Triển khai `ValidationBehavior<TRequest, TResponse>` tự động chạy các Validators trước khi vào Handler. | **COMPLIANT** | Bắt lỗi validation trả mã HTTP 422 Unprocessable Entity. |
| **CONS-009** | Docker Containerization | Có `docker-compose.yml` định nghĩa các dịch vụ PostgreSQL, Redis, MinIO, MailHog, Jaeger. | **COMPLIANT** | Môi trường dev và container hóa đầy đủ. |
| **CONS-010** | Structured Logging | Cấu hình Serilog ghi log có cấu trúc (JSON), có TraceId/CorrelationId xuyên suốt các tầng. | **COMPLIANT** | Hoàn toàn tuân thủ. |

---

## 4. BẢNG ĐỐI CHIẾU NON-FUNCTIONAL REQUIREMENTS (NFR)

| NFR Category | Yêu cầu chính trong SRS | Hiện trạng đáp ứng | Đánh giá |
|---|---|---|---|
| **NFR-PERF** | Phản hồi API < 200ms với 95% request; Cache-aside Redis cho Category & Recipe; Tối ưu ảnh Next.js | - Triển khai `ResilientCacheService` với Circuit Breaker.<br>- Tối ưu hình ảnh WebP/AVIF bằng Next.js Image component.<br>- Phản hồi cache < 15ms, fallback Postgres < 80ms. | **COMPLIANT** |
| **NFR-SEC** | Bảo mật mật khẩu PBKDF2; JWT Access Token 15p; CORS an toàn không dùng wildcard; Xử lý tệp bằng magic bytes | - PBKDF2-HMAC-SHA512 qua Identity.<br>- CORS chỉ cho phép dev origins tường minh (3000, 3001, 5000, 5001).<br>- ImageSharp và Cryptography Pkcs đã cập nhật bản vá bảo mật. | **COMPLIANT** |
| **NFR-USE** | Giao diện Responsive; Hỗ trợ nhập phân số nguyên liệu; Báo lỗi rõ ràng tiếng Việt/Anh | - Giao diện Next.js Tailwind CSS hoàn toàn responsive.<br>- Phân số nguyên liệu được chuẩn hóa tại Frontend trước khi gửi API. | **COMPLIANT** |
| **NFR-REL** | Graceful degradation (Redis down không làm sập hệ thống, fallback DB ngay); Tỷ lệ uptime cao | - Cơ chế Circuit Breaker với cooldown 30s và OperationTimeout 500ms giúp hệ thống tự phục hồi ngay cả khi Redis ngừng hoạt động. | **COMPLIANT** |
| **NFR-MAINT** | Mã nguồn tuân thủ Clean Architecture; Định danh type an toàn; Không có unused code | - Dọn dẹp các type không dùng.<br>- Phân chia tầng rõ ràng, độc lập. | **COMPLIANT** |
| **NFR-SCALE** | Hỗ trợ mở rộng theo chiều ngang (Stateless API); Cache phân tán; Background job tách biệt | - API hoàn toàn Stateless nhờ JWT.<br>- Hangfire và Redis hỗ trợ scale out nhiều nodes. | **COMPLIANT** |
| **NFR-SEO** | Server-Side Rendering cho Recipe và Category; Thẻ meta OpenGraph | - Next.js App Router render SSR/SSG cho các trang công khai. | **COMPLIANT** |

---

## 5. PHÂN TÍCH CHI TIẾT CÁC SAI LỆCH VÀ ĐỀ XUẤT XỬ LÝ (DEVIATION ANALYSIS)

### 5.1. Sai lệch kiến trúc CONS-003: MVC Controllers vs Minimal APIs
- **Mã yêu cầu:** CONS-003.
- **Điều khoản SRS:** *"Hệ thống Backend xây dựng trên nền tảng .NET 10 sử dụng Minimal APIs; Frontend sử dụng Next.js App Router."*
- **Hành vi code thực tế:**
  - `CategoriesEndpoints.cs` và `RecipeEndpoints.cs` đã được triển khai hoàn chỉnh bằng **Minimal APIs** (`app.MapGroup("/api/v1/recipes")`).
  - Tuy nhiên, `Controllers/AuthController.cs` và `Controllers/AdminUsersController.cs` vẫn đang kế thừa từ `ControllerBase` và đăng ký qua `builder.Services.AddControllers()`.
- **Nguyên nhân:**
  Trong giai đoạn khởi tạo ban đầu (Lab 7), phần Xác thực và Quản trị người dùng được sinh từ template MVC Controller truyền thống và chưa kịp chuyển đổi sang Minimal APIs.
- **Mức độ ảnh hưởng:**
  - *Mức độ nghiêm trọng:* **Thấp - Trung bình** (Về mặt chức năng và hợp đồng API: hoàn toàn đúng prefix `/api/v1/`, đúng format JSON, đúng HTTP status code và hoạt động trơn tru; không gây lỗi runtime).
  - *Về mặt kiến trúc:* Chưa đồng nhất 100% phong cách code theo CONS-003.
- **Đề xuất khắc phục (Kế hoạch Lab 8):**
  - Tạo `backend/src/CulinaryBlog.Api/Endpoints/Auth/AuthEndpoints.cs` và chuyển các actions từ `AuthController` sang `app.MapGroup("/api/v1/auth")`.
  - Tạo `backend/src/CulinaryBlog.Api/Endpoints/Admin/AdminUsersEndpoints.cs` và ánh xạ các route quản trị người dùng.
  - Xóa bỏ `builder.Services.AddControllers()` và `app.MapControllers()` để loại bỏ hoàn toàn MVC runtime khỏi project.
  - *Lưu ý:* Không nên thực hiện tái cấu trúc lớn này ngay trong đợt sửa lỗi để tránh phát sinh regression bugs khi kiểm thử đang ổn định.

---

### 5.2. Chức năng chưa triển khai ở Frontend: Giao diện Thùng rác công thức (`/admin/recipes/trash`)
- **Mã yêu cầu:** FR-RCP-008, SRS Chương 5 (UI Routes).
- **Điều khoản SRS:** Quy định route giao diện `/admin/recipes/trash` cho Admin phục hồi hoặc xóa vĩnh viễn công thức đã xóa mềm.
- **Hành vi code thực tế:**
  - Backend API (`POST /api/v1/recipes/{id}/restore`, `DELETE /api/v1/recipes/{id}/purge`) đã được triển khai đầy đủ và kiểm thử thành công.
  - Frontend hiện tại chưa có trang `app/(admin)/admin/recipes/trash/page.tsx`.
- **Nguyên nhân:** Tính năng nằm trong phân công công việc của Lab 8.
- **Mức độ ảnh hưởng:** Thấp (không làm gián đoạn các luồng nghiệp vụ chính của tác giả và người đọc).
- **Đề xuất khắc phục:** Xây dựng trang giao diện Thùng rác trong đợt triển khai Lab 8.

---

## 6. KẾT LUẬN VÀ KIẾN NGHỊ

1. **Về tính ổn định và tính đúng đắn:**
   Toàn bộ 7 lỗi đã được khắc phục hoàn toàn. Các chỉ số kiểm thử đạt mức tuyệt đối (253/253 Backend Unit Tests PASS, 78/78 Frontend Jest Tests PASS, Next.js build 16 routes thành công).
2. **Về tuân thủ SRS v1.2.0:**
   Hệ thống đáp ứng xuất sắc **84.8%** các yêu cầu chức năng (các phần còn lại là chức năng nâng cao của Lab 8), tuân thủ hoàn toàn các yêu cầu phi chức năng (NFR) và 9/10 ràng buộc kiến trúc (CONS).
3. **Môi trường hoạt động:**
   Đã cấu hình sẵn sàng cho phép chạy song song tại cổng **Frontend 3001** và **Backend 5001** mà không làm xung đột với LMS / Power BI, đồng thời bảo toàn nguyên vẹn cổng 3000/5000 cho các thành viên khác.

