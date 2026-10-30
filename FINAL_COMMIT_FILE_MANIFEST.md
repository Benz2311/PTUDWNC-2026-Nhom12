# BẢNG PHÂN BỔ TỆP TIN VÀ KẾ HOẠCH COMMIT CHÍNH THỨC TRƯỚC KHI PUSH MAIN
## (FINAL COMMIT FILE MANIFEST & ATOMIC STAGING PLAN)

> **Dự án:** Culinary Blog – Phát triển Web Nâng Cao – Nhóm 12  
> **Thư mục làm việc:** `B:\bài web nhóm mới\main-merge`  
> **Nhánh mục tiêu:** `main` (đồng bộ tuyệt đối với `origin/main` tại commit `6d46d1e`)  
> **Tài liệu chuẩn:** `docs/requirements/SRS.md` – Phiên bản **1.2.0 Approved Baseline**  
> **Thời điểm lập:** 30/10/2026 12:58:00 (UTC+7)  
> **Nguyên tắc thực thi:** **TUYỆT ĐỐI CHƯA COMMIT, CHƯA PUSH, CHƯA STAGE (READ-ONLY PLANNING)**  

---

## I. TỔNG QUAN HIỆN TRẠNG MÃ NGUỒN (CODEBASE AUDIT METRICS)

- **Commit HEAD / origin/main:** `6d46d1e` (*merge: integrate task 6 frontend search, image management, step editor, shared components and layout*)
- **Tình trạng nhánh:** `0 ahead, 0 behind` (Đồng bộ tuyệt đối, 0% nguy cơ diverge).
- **Tổng số tệp Tracked Modified:** **72 tệp** (+4.719 dòng thêm, -1.843 dòng xóa).
- **Tổng số tệp Untracked:** **104 tệp** (Gồm mã nguồn Minimal APIs, DTOs, Tests, SVG icons, Báo cáo và Screenshots Task 7).
- **Tệp bị xóa (Deleted):** **0 tệp**.
- **Kết quả `git diff --check`:** **0 lỗi khoảng trắng** (Exit code 0).

---

## II. MA TRẬN PHÂN LOẠI NGUỒN GỐC THAY ĐỔI (ATTRIBUTION MATRIX)

| Nhóm thay đổi | Mục đích & Nghiệp vụ liên quan | Thành viên phụ trách chính | Quy chuẩn SRS |
| :--- | :--- | :--- | :--- |
| **Sửa lỗi Auth & Minimal APIs** | Chuyển đổi MVC Controllers sang Minimal APIs, chuẩn hóa luồng đổi/quên/reset mật khẩu, phân quyền Admin. | Nguyễn Văn Quốc | CONS-003, SEC-01..09 |
| **Sửa Recipe Lifecycle & Concurrency** | Sửa xử lý xóa mềm khi Purge/Restore, đồng bộ `RowVersion` làm Concurrency Token trên PostgreSQL. | Phạm Nguyễn Ngọc Phước | REQ-REC-01..07, CONC-01 |
| **Sửa Image Security & Redis Failover** | Kiểm tra quyền upload ảnh (401/422), Circuit Breaker cho Redis cache khi mất kết nối. | Võ Hùng Mạnh | SEC-02, NFR-REL-002, PERF-01 |
| **Frontend Runtime, UI & Navigation** | Loại bỏ 401 khi Guest mở web, Controlled Mode cho `/components-preview`, Next.js Image unoptimized. | Lê Thị Ánh Nhung / Toàn nhóm | UI-01..08, NFR-USAB-01 |
| **Minh chứng nghiệm thu Task 6 & Task 7** | Tài liệu đặc tả 13 phần cho Search FTS, Step Editor, Image Manager và 40 ảnh chụp Playwright. | Võ Hùng Mạnh | LAB-6, LAB-7, SRS v1.2.0 |

---

## III. ĐÁNH GIÁ TÍNH CẦN THIẾT CỦA FILE `SRS.md`

- **Đường dẫn:** `docs/requirements/SRS.md` (Hiện đang ở trạng thái **Untracked**).
- **Phân tích:** 
  - File `SRS.md` chứa toàn bộ 25 trang đặc tả yêu cầu phần mềm phiên bản **1.2.0 Approved Baseline** (phiên bản chuẩn duy nhất được phê duyệt để nghiệm thu).
  - Tất cả các báo cáo nghiệm thu, tài liệu Task 6, Task 7 và mã nguồn hiện tại đều trích dẫn các mã yêu cầu từ file này (`REQ-REC`, `NFR-REL`, `CONS-003`, v.v.).
  - Nếu **KHÔNG COMMIT** `SRS.md`, bất kỳ ai clone repository từ GitHub sẽ thiếu tài liệu đặc tả chuẩn, làm mất tính toàn vẹn của đồ án.
- **Quy tắc bất di bất dịch:**
  - **BẮT BUỘC COMMIT** `docs/requirements/SRS.md` vào Git.
  - **TUYỆT ĐỐI GIỮ NGUYÊN TRẠNG 100%, KHÔNG CHỈNH SỬA** bất kỳ ký tự nào trong file này vì đây là Approved Baseline đã đóng băng.

---

## IV. ĐÁNH GIÁ RỦI RO BẢO MẬT TRƯỚC KHI PUSH LÊN MAIN

| Thành phần cấu hình | Giá trị hiện tại trong Git | Đánh giá rủi ro | Biện pháp bảo vệ khi triển khai thật |
| :--- | :--- | :---: | :--- |
| **PostgreSQL Dev Connection** | `Host=localhost;Port=5432;Database=culinaryblog;Username=culinaryadmin;Password=123456` | **AN TOÀN CHO ĐỒ ÁN** | Đây là thông tin kết nối container Docker local dành riêng cho môi trường dev của nhóm, không thể truy cập từ bên ngoài internet. Khi deploy Production, bắt buộc ghi đè bằng biến môi trường `ConnectionStrings__DefaultConnection`. |
| **Redis Connection** | `localhost:6379` | **AN TOÀN CHO ĐỒ ÁN** | Kết nối Redis container local, không có mật khẩu public. Production sẽ ghi đè qua `ConnectionStrings__Redis`. |
| **MinIO S3 Credentials** | `minioadmin / minioadmin` | **AN TOÀN CHO ĐỒ ÁN** | Tài khoản mặc định của container MinIO local. Production sẽ dùng S3 IAM Role hoặc MinIO credentials thật cấu hình qua environment variables. |
| **JWT Dev Secret Key** | Chuỗi bí mật thử nghiệm trong `appsettings.json` | **AN TOÀN CHO ĐỒ ÁN** | Độ dài > 32 ký tự, đủ điều kiện HMAC-SHA256. Production sẽ nạp key thật từ Secret Manager hoặc Docker secrets. |
| **File `.env.local`** | Nằm trong `frontend/culinary-blog-web/` | **ĐÃ ĐƯỢC BẢO VỆ** | File này đã được `.gitignore` loại trừ hoàn toàn, không bị commit vào Git. |
| **Cổng 3000 và 5000** | Cấu hình mặc định của nhóm | **ĐƯỢC BẢO VỆ NGUYÊN VẸN** | Hệ thống giữ nguyên cấu hình cổng 3000/5000 cho dự án LMS; đã thêm cổng 3001/5001 song song, không gây xung đột cổng. |

---

## V. PHÂN LOẠI VÀ ĐỀ XUẤT XỬ LÝ 12 BÁO CÁO MARKDOWN TẠI THƯ MỤC GỐC

Hiện tại thư mục gốc có 12 báo cáo Markdown. Để người chấm điểm dễ theo dõi mà không làm rác thư mục gốc, đề xuất phân loại như sau (chờ người dùng phê duyệt trước khi di chuyển):

| STT | Tên báo cáo Markdown | Nội dung chính | Đề xuất phân loại |
| :---: | :--- | :--- | :--- |
| 1 | `FINAL_ZERO_KNOWN_BUGS_REPORT.md` | Báo cáo nghiệm thu chính thức: 0 lỗi, 310/310 Backend tests, 78/78 Jest, 5/5 Playwright, 12/12 Security | **GIỮ TẠI ROOT** (Báo cáo tổng kết chính cho giảng viên/nhóm trưởng) |
| 2 | `PRE_COMMIT_CLEANUP_REPORT.md` | Báo cáo hoàn tất dọn dẹp tiền commit (0 lỗi whitespace, ignore sạch sẽ) | **GIỮ TẠI ROOT** (Minh chứng chất lượng Git hygiene) |
| 3 | `FINAL_COMMIT_FILE_MANIFEST.md` | Bảng phân bổ tệp tin và kế hoạch commit chính thức này | **GIỮ TẠI ROOT** (Kế hoạch hành động chính) |
| 4 | `SRS_COMPLIANCE_REPORT.md` | Bảng đối chiếu chi tiết từng điều khoản SRS v1.2.0 | **Đề xuất chuyển vào `docs/requirements/`** |
| 5 | `BAO_CAO_HOP_NHAT_LAB_VA_DOCKER_NHOM12.md` | Báo cáo hợp nhất môi trường Docker và cấu hình nhóm | **Đề xuất chuyển vào `docs/audit_reports/`** |
| 6 | `FINAL_SRS_ACCEPTANCE_REPORT.md` | Báo cáo nghiệm thu giai đoạn trước | **Đề xuất chuyển vào `docs/audit_reports/`** |
| 7 | `FINAL_BUG_FIX_REPORT.md` | Báo cáo sửa lỗi giai đoạn trước | **Đề xuất chuyển vào `docs/audit_reports/`** |
| 8 | `FULL_PROJECT_AUDIT_REPORT.md` | Báo cáo kiểm toán tổng thể đợt 1 | **Đề xuất chuyển vào `docs/audit_reports/`** |
| 9 | `FULL_PROJECT_FIX_REPORT.md` | Báo cáo khắc phục lỗi đợt 1 | **Đề xuất chuyển vào `docs/audit_reports/`** |
| 10 | `FULL_RUNTIME_DEBUG_REPORT.md` | Báo cáo chẩn đoán lỗi runtime sâu | **Đề xuất chuyển vào `docs/audit_reports/`** |
| 11 | `PRE_MAIN_COMMIT_AUDIT_REPORT.md` | Báo cáo kiểm toán read-only trước tiền commit | **Đề xuất chuyển vào `docs/audit_reports/`** |
| 12 | `REMAINING_FUNCTIONAL_TEST_REPORT.md` | Báo cáo kiểm thử vòng đời công thức | **Đề xuất chuyển vào `docs/audit_reports/`** |

> [!IMPORTANT]
> **Cam kết:** Toàn bộ 12 file báo cáo trên hiện được giữ nguyên trạng tại thư mục gốc, **chưa di chuyển hay xóa bỏ bất kỳ file nào**.

---

## VI. BẢNG PHÂN BỔ ĐẦY ĐỦ TỪNG TẬP TIN CHO 6 COMMITS

### COMMIT 1: CHUẨN HÓA MINIMAL APIS CHO AUTH VÀ ADMIN (CONS-003)
- **Mục tiêu:** Đưa toàn bộ các endpoint Auth và Admin Users sang Minimal APIs theo đúng kiến trúc của dự án, hỗ trợ đầy đủ luồng mật khẩu và phân quyền quản trị.
- **Khả năng biên dịch (Buildability):** Tự chứa độc lập (Self-contained). Đầy đủ Endpoint, Service, Repository, DTO và Unit test tương ứng.
- **Thông điệp Commit (Tiếng Việt):**
  `fix(backend): chuẩn hóa kiến trúc Minimal APIs cho xác thực và quản trị người dùng theo SRS v1.2.0`

| STT | Loại tệp | Trạng thái Git | Đường dẫn đầy đủ | Nội dung / Hunk thay đổi |
| :---: | :---: | :---: | :--- | :--- |
| 1 | Endpoint | **Untracked** | `backend/src/CulinaryBlog.Api/Endpoints/Auth/AuthEndpoints.cs` | Khai báo `MapAuthEndpoints`: `/register`, `/login`, `/google-login`, `/refresh`, `/revoke`, `/logout`, `/me`, `/profile`, `/change-password`, `/forgot-password`, `/reset-password`. |
| 2 | Endpoint | **Untracked** | `backend/src/CulinaryBlog.Api/Endpoints/Admin/AdminUsersEndpoints.cs` | Khai báo `MapAdminUsersEndpoints`: `GET /` (phân trang, tìm kiếm), `PATCH /{id}/status` (khóa/mở khóa tài khoản). |
| 3 | Controller | Modified | `backend/src/CulinaryBlog.Api/Controllers/AuthController.cs` | Bổ sung các endpoint chuyển tiếp `change-password`, `forgot-password`, `reset-password` để tương thích ngược. |
| 4 | Controller | Modified | `backend/src/CulinaryBlog.Api/Controllers/AdminUsersController.cs` | Bổ sung `GetUsers` có phân trang, lọc từ khóa cho Controller cũ. |
| 5 | DTO | Modified | `backend/src/CulinaryBlog.Application/DTOs/Auth/AuthResponse.cs` | Bổ sung `EmailConfirmed`, `Roles`, `IsActive`, `CreatedAt`. |
| 6 | DTO | **Untracked** | `backend/src/CulinaryBlog.Application/DTOs/Auth/ChangePasswordRequest.cs` | Khai báo record DTO: `CurrentPassword`, `NewPassword`, `ConfirmPassword`. |
| 7 | DTO | **Untracked** | `backend/src/CulinaryBlog.Application/DTOs/Auth/ForgotPasswordRequest.cs` | Khai báo record DTO: `Email`. |
| 8 | DTO | **Untracked** | `backend/src/CulinaryBlog.Application/DTOs/Auth/ResetPasswordRequest.cs` | Khai báo record DTO: `Token`, `NewPassword`, `ConfirmPassword`. |
| 9 | Interface | Modified | `backend/src/CulinaryBlog.Application/Interfaces/IJwtTokenGenerator.cs` | Bổ sung method `GeneratePasswordResetToken` và `ValidatePasswordResetToken`. |
| 10 | Repository | Modified | `backend/src/CulinaryBlog.Application/Repositories/IUserRepository.cs` | Bổ sung method `GetUsersAsync` phân trang. |
| 11 | Service | Modified | `backend/src/CulinaryBlog.Application/Services/IAuthService.cs` | Bổ sung `ChangePasswordAsync`, `ForgotPasswordAsync`, `ResetPasswordAsync`. |
| 12 | Security | Modified | `backend/src/CulinaryBlog.Infrastructure/Authentication/JwtTokenGenerator.cs` | Triển khai sinh JWT token reset mật khẩu với claim `purpose: password_reset` (thời hạn 1 giờ). |
| 13 | Repository | Modified | `backend/src/CulinaryBlog.Infrastructure/Repositories/UserRepository.cs` | Triển khai query `GetUsersAsync` tìm kiếm trên PostgreSQL. |
| 14 | Repository | Modified | `backend/src/CulinaryBlog.Infrastructure/Repositories/RefreshTokenRepository.cs` | Thay `ExecuteUpdateAsync` bằng EF tracking tiêu chuẩn để tương thích cả InMemory test provider. |
| 15 | Service | Modified | `backend/src/CulinaryBlog.Infrastructure/Services/AuthService.cs` | Bổ sung hàm kiểm tra mật khẩu mạnh `ValidatePassword`, xử lý đổi/quên mật khẩu và gán đủ Roles. |
| 16 | UnitTest | Modified | `backend/tests/CulinaryBlog.UnitTests/Application/AuthServiceTests.cs` | Cập nhật mật khẩu mẫu thỏa mãn luật mật khẩu mạnh (`Password123!`), bổ sung profile fields. |

---

### COMMIT 2: SỬA VÒNG ĐỜI CÔNG THỨC, XÓA MỀM VÀ KHÓA LẠC QUAN
- **Mục tiêu:** Đảm bảo toàn bộ nghiệp vụ xóa mềm (Soft-delete), dọn sạch vĩnh viễn (Purge), cập nhật slug và khóa lạc quan (Optimistic Concurrency) hoạt động chính xác trên cả PostgreSQL và InMemory tests.
- **Khả năng biên dịch (Buildability):** Tự chứa độc lập (Self-contained). Đầy đủ Persistence, Configuration, Query và UnitTest tương ứng.
- **Thông điệp Commit (Tiếng Việt):**
  `fix(backend): sửa xử lý xóa mềm khi dọn sạch công thức và đồng bộ khóa lạc quan RowVersion`

| STT | Loại tệp | Trạng thái Git | Đường dẫn đầy đủ | Nội dung / Hunk thay đổi |
| :---: | :---: | :---: | :--- | :--- |
| 1 | Query | Modified | `backend/src/CulinaryBlog.Application/Features/Recipes/Queries/GetRecipeBySlug/GetRecipeBySlugQuery.cs` | Chuyển ánh xạ Concurrency Token từ `xmin` (PostgreSQL specific) sang `RowVersion` chuẩn. |
| 2 | Persistence | Modified | `backend/src/CulinaryBlog.Infrastructure/Persistence/ApplicationDbContext.cs` | Bọc điều kiện `!isInMemory` khi đăng ký extension `unaccent`, `pg_trgm`, `SearchVectorFts` để InMemory không crash. |
| 3 | Config | Modified | `backend/src/CulinaryBlog.Infrastructure/Persistence/Configurations/RecipeConfiguration.cs` | Cấu hình `RowVersion` là Concurrency Token với `.ValueGeneratedNever()`. |
| 4 | Service | Modified | `backend/src/CulinaryBlog.Infrastructure/Persistence/RecipePurgeService.cs` | Thêm `.IgnoreQueryFilters()` vào truy vấn `_db.Recipes` để dọn sạch được công thức đã xóa mềm. |
| 5 | Repository | Modified | `backend/src/CulinaryBlog.Infrastructure/Persistence/RecipeRepository.cs` | Thêm `.IgnoreQueryFilters()` trong `GetForUpdateAsync` và `SlugExistsAsync`. |
| 6 | UnitTest | Modified | `backend/tests/CulinaryBlog.UnitTests/RecipePersistenceIntegrationTests.cs` | Bổ sung `.IgnoreQueryFilters()` khi query kiểm tra entity đã soft-delete (kích hoạt test pass 100%). |

---

### COMMIT 3: TĂNG CƯỜNG BẢO MẬT UPLOAD ẢNH VÀ CHỊU LỖI REDIS CACHE
- **Mục tiêu:** Chuẩn hóa mã phản hồi 401/422 cho upload ảnh theo RFC 7807; bảo vệ Concurrency Token; bổ sung Circuit Breaker cho Redis cache; nâng cấp thư viện an toàn bảo mật.
- **Khả năng biên dịch (Buildability):** Tự chứa độc lập. Đầy đủ Endpoints, Commands, Caching Service và csproj.
- **Thông điệp Commit (Tiếng Việt):**
  `fix(backend): tăng cường bảo mật tải lên hình ảnh và bổ sung cơ chế circuit breaker cho redis`

| STT | Loại tệp | Trạng thái Git | Đường dẫn đầy đủ | Nội dung / Hunk thay đổi |
| :---: | :---: | :---: | :--- | :--- |
| 1 | Endpoint | Modified | `backend/src/CulinaryBlog.Api/Endpoints/Recipes/RecipeEndpoints.cs` | Bổ sung kiểm tra unauthenticated trả về 401; file rỗng trả 422; validation lỗi trả 422 cho upload/patch/delete image. |
| 2 | Command | Modified | `backend/src/CulinaryBlog.Application/Features/Recipes/Commands/RecipeImages/RecipeImageCommands.cs` | Bỏ sửa đổi trực tiếp trên Recipe entity khi thao tác ảnh con để tránh xung đột Concurrency Token. |
| 3 | Caching | Modified | `backend/src/CulinaryBlog.Infrastructure/Caching/ResilientCacheService.cs` | Bổ sung Circuit Breaker (cooldown 30s, timeout 500ms) fallback database ngay lập tức khi Redis sự cố. |
| 4 | DI | Modified | `backend/src/CulinaryBlog.Infrastructure/DependencyInjection.cs` | Cấu hình Redis timeout an toàn (AbortOnConnectFail = false); kiểm tra môi trường Testing trước khi add Hangfire. |
| 5 | Project | Modified | `backend/src/CulinaryBlog.Application/CulinaryBlog.Application.csproj` | Nâng cấp `SixLabors.ImageSharp` lên `3.2.0` (Vá lỗ hổng bảo mật CVE). |
| 6 | Project | Modified | `backend/src/CulinaryBlog.Infrastructure/CulinaryBlog.Infrastructure.csproj` | Nâng cấp ImageSharp 3.2.0, thêm `System.Security.Cryptography.Pkcs 10.0.13`, nâng OpenTelemetry và MailKit. |

---

### COMMIT 4: CHUẨN HÓA FRONTEND RUNTIME, PHIÊN ĐĂNG NHẬP VÀ GIAO DIỆN
- **Mục tiêu:** Xử lý triệt để lỗi 401 khi Guest mở web; chuyển component preview sang Controlled Mode; bổ sung giao diện quản trị Admin Users và Profile; chuẩn hóa ảnh tĩnh fallback nội bộ.
- **Khả năng biên dịch (Buildability):** Tự chứa độc lập. TypeScript type check 0 lỗi, Next.js build 16/16 trang SSG/SSR thành công 100%.
- **Thông điệp Commit (Tiếng Việt):**
  `fix(frontend): chuẩn hóa phiên đăng nhập, điều hướng bảo vệ và bổ sung giao diện quản trị người dùng`

| STT | Phân nhóm | Trạng thái Git | Đường dẫn đầy đủ | Nội dung / Hunk thay đổi |
| :---: | :---: | :---: | :--- | :--- |
| 1 | Auth Pages | **Untracked** | `frontend/culinary-blog-web/app/(auth)/layout.tsx` | Layout chia cột cho các trang xác thực. |
| 2 | Auth Pages | **Untracked** | `frontend/culinary-blog-web/app/(auth)/login/page.tsx` | Trang đăng nhập. |
| 3 | Auth Pages | **Untracked** | `frontend/culinary-blog-web/app/(auth)/register/page.tsx` | Trang đăng ký. |
| 4 | Auth Pages | **Untracked** | `frontend/culinary-blog-web/app/(auth)/confirm-email/page.tsx` | Trang xác thực email qua token. |
| 5 | Auth Pages | **Untracked** | `frontend/culinary-blog-web/app/(auth)/forgot-password/page.tsx` | Trang gửi yêu cầu quên mật khẩu. |
| 6 | Auth Pages | **Untracked** | `frontend/culinary-blog-web/app/(auth)/reset-password/page.tsx` | Trang đặt lại mật khẩu với token. |
| 7 | Admin Page | **Untracked** | `frontend/culinary-blog-web/app/(dashboard)/admin/users/page.tsx` | Trang quản trị danh sách người dùng cho Admin. |
| 8 | Profile Page| **Untracked** | `frontend/culinary-blog-web/app/profile/page.tsx` | Trang hồ sơ cá nhân và đổi mật khẩu. |
| 9 | Recipe Pages| **Untracked** | `frontend/culinary-blog-web/app/(public)/recipes/page.tsx` | Trang danh sách toàn bộ công thức. |
| 10 | Recipe Pages| **Untracked** | `frontend/culinary-blog-web/app/(public)/recipes/[slug]/page.tsx` | Trang chi tiết công thức hiển thị đầy đủ nguyên liệu, bước nấu. |
| 11 | Recipe Pages| **Untracked** | `frontend/culinary-blog-web/app/(public)/recipes/create/page.tsx` | Trang tạo mới công thức. |
| 12 | App Core | Modified | `frontend/culinary-blog-web/app/components-preview/page.tsx` | Bỏ `recipeId="rec-preview-001"`, truyền `initialSteps` & `initialImages` tĩnh (Controlled Mode, triệt tiêu 404). |
| 13 | App Core | Modified | `frontend/culinary-blog-web/app/globals.css` | Bổ sung css cho thẻ công thức `.recipe-card`, `.recipe-image`, `.recipe-tag`, `.recipe-body`. |
| 14 | App Core | Modified | `frontend/culinary-blog-web/app/layout.tsx` | Bọc ứng dụng trong `AppProviders`. |
| 15 | App Core | Modified | `frontend/culinary-blog-web/app/page.tsx` | Chuẩn hóa tải danh mục, công thức trang chủ kèm skeleton và fallback ảnh. |
| 16 | App Core | Modified | `frontend/culinary-blog-web/app/search/page.tsx` | Thêm Suspense boundary cho trang tìm kiếm. |
| 17 | App Core | Modified | `frontend/culinary-blog-web/app/(public)/categories/page.tsx` | Hiển thị danh mục món ăn với số lượng công thức thực tế. |
| 18 | App Core | Modified | `frontend/culinary-blog-web/app/(public)/categories/[slug]/page.tsx` | Hiển thị công thức theo danh mục. |
| 19 | Admin UI | **Untracked** | `frontend/culinary-blog-web/components/admin/AdminUsersPage.tsx` | Component bảng người dùng, hỗ trợ tìm kiếm, lọc active và khóa tài khoản. |
| 20 | Admin UI | **Untracked** | `frontend/culinary-blog-web/components/admin/ConfirmationModal.tsx` | Modal xác nhận thao tác khóa/mở khóa người dùng. |
| 21 | Admin UI | **Untracked** | `frontend/culinary-blog-web/components/admin/Pagination.tsx` | Phân trang dùng chung chuẩn UX. |
| 22 | Admin UI | **Untracked** | `frontend/culinary-blog-web/components/admin/RoleBadge.tsx` | Badge hiển thị vai trò (Admin / Author / User). |
| 23 | Admin UI | **Untracked** | `frontend/culinary-blog-web/components/admin/UserStatusBadge.tsx` | Badge trạng thái tài khoản (Hoạt động / Bị khóa). |
| 24 | Admin UI | **Untracked** | `frontend/culinary-blog-web/components/admin/UserTableSkeleton.tsx` | Skeleton loading cho bảng người dùng. |
| 25 | Auth UI | **Untracked** | `frontend/culinary-blog-web/components/auth/LoginPage.tsx` | Form đăng nhập kèm validation. |
| 26 | Auth UI | **Untracked** | `frontend/culinary-blog-web/components/auth/RegisterPage.tsx` | Form đăng ký kèm luật mật khẩu mạnh. |
| 27 | Auth UI | **Untracked** | `frontend/culinary-blog-web/components/auth/ConfirmEmailPage.tsx` | Component xác thực email. |
| 28 | Auth UI | **Untracked** | `frontend/culinary-blog-web/components/auth/ForgotPasswordPage.tsx` | Component gửi email reset. |
| 29 | Auth UI | **Untracked** | `frontend/culinary-blog-web/components/auth/ResetPasswordPage.tsx` | Form nhập mật khẩu mới. |
| 30 | Auth UI | **Untracked** | `frontend/culinary-blog-web/components/auth/GoogleLoginButton.tsx` | Nút đăng nhập Google OAuth. |
| 31 | Auth UI | **Untracked** | `frontend/culinary-blog-web/components/auth/ProtectedRoute.tsx` | HOC bảo vệ route theo vai trò. |
| 32 | Auth UI | **Untracked** | `frontend/culinary-blog-web/components/auth/form-errors.ts` | Tiện ích ánh xạ lỗi RFC 7807 vào form fields. |
| 33 | Profile UI | **Untracked** | `frontend/culinary-blog-web/components/profile/ProfilePage.tsx` | Component cập nhật thông tin cá nhân. |
| 34 | Profile UI | **Untracked** | `frontend/culinary-blog-web/components/profile/ChangePasswordCard.tsx` | Form đổi mật khẩu bảo mật. |
| 35 | Providers | **Untracked** | `frontend/culinary-blog-web/components/providers/AppProviders.tsx` | Bọc `AuthProvider` và Toast Provider. |
| 36 | UI Primitives| **Untracked** | `frontend/culinary-blog-web/components/ui/toast.tsx` | Thông báo Toast UI nhẹ nhàng. |
| 37 | Layout UI | Modified | `frontend/culinary-blog-web/components/layout/Header.tsx` | Menu người dùng đăng nhập, nút chuyển Admin/Profile, nút Đăng xuất. |
| 38 | Layout UI | Modified | `frontend/culinary-blog-web/components/layout/Footer.tsx` | Thông tin bản quyền nhóm 12. |
| 39 | Layout UI | Modified | `frontend/culinary-blog-web/components/layout/PublicLayout.tsx` | Khung bố cục Header + Footer thống nhất. |
| 40 | Recipe UI | Modified | `frontend/culinary-blog-web/components/recipes/RecipeCard.tsx` | Fallback ảnh SVG nội bộ `/sample-images/food-01.svg` khi ảnh null/lỗi. |
| 41 | Recipe UI | Modified | `frontend/culinary-blog-web/components/recipes/RecipeDetailView.tsx` | Chi tiết công thức, danh sách nguyên liệu, các bước nấu và badge. |
| 42 | Recipe UI | Modified | `frontend/culinary-blog-web/components/recipes/DifficultyBadge.tsx` | Badge độ khó (Dễ, Trung bình, Khó) đồng bộ màu sắc. |
| 43 | Recipe UI | Modified | `frontend/culinary-blog-web/components/recipes/RecipeStatusBadge.tsx` | Badge trạng thái vòng đời công thức (Bản nháp, Công khai, Lưu trữ). |
| 44 | Recipe UI | Modified | `frontend/culinary-blog-web/components/recipes/RecipeMeta.tsx` | Hiển thị thời gian chuẩn bị và nấu nướng. |
| 45 | Recipe UI | **Untracked** | `frontend/culinary-blog-web/components/recipes/RecipeDetailSkeleton.tsx` | Skeleton loading chi tiết công thức. |
| 46 | Recipe UI | Modified | `frontend/culinary-blog-web/components/recipe/*` (4 tệp) | Đồng bộ các component thư mục `recipe` tương thích với `components-preview`. |
| 47 | Search UI | Modified | `frontend/culinary-blog-web/components/search/SearchBox.tsx` | Ô tìm kiếm gợi ý từ khóa. |
| 48 | Search UI | Modified | `frontend/culinary-blog-web/components/search/SearchContainer.tsx` | Bộ điều phối tìm kiếm FTS, lọc danh mục/độ khó, sắp xếp và phân trang. |
| 49 | Context | Modified | `frontend/culinary-blog-web/contexts/AuthContext.tsx` | Guest thuần túy không gọi `/auth/me`; kiểm tra JWT hết hạn; hỗ trợ đổi mật khẩu. |
| 50 | API Client | Modified | `frontend/culinary-blog-web/lib/api/client.ts` | Phân biệt cảnh báo 401/403/404 với lỗi 5xx; chỉ redirect `/login` khi đang ở trang bảo vệ. |
| 51 | API Service| Modified | `frontend/culinary-blog-web/lib/api/auth.ts` | Khai báo gọi API đổi/quên/reset mật khẩu và danh sách user admin. |
| 52 | Hooks | Modified | `frontend/culinary-blog-web/hooks/useRecipeSearch.ts` | Hook quản lý state tìm kiếm và debounce. |
| 53 | Types | Modified | `frontend/culinary-blog-web/types/auth.ts` | Khai báo type DTOs auth đầy đủ. |
| 54 | Types | Modified | `frontend/culinary-blog-web/types/category.ts` | Khai báo type danh mục. |
| 55 | Types | Modified | `frontend/culinary-blog-web/types/search.ts` | Khai báo type tìm kiếm. |
| 56 | Assets | **Untracked** | `frontend/culinary-blog-web/public/sample-images/food-01.svg` -> `food-08.svg` (8 tệp) | Bộ icon/ảnh SVG vector nội bộ độc lập, dùng làm ảnh đại diện món ăn an toàn. |
| 57 | Legacy Mirror| Modified | `frontend/app/*`, `frontend/components/*`, `frontend/types/*` (6 tệp) | Bản mirror đồng bộ lịch sử tại thư mục ngoài `frontend/`. |

---

### COMMIT 5: CẤU HÌNH MÔI TRƯỜNG VÀ HOÀN THIỆN 100% TEST SUITES
- **Mục tiêu:** Bổ sung cấu hình test host biệt lập cho Integration Tests; kết nối DB test riêng; hoàn thiện 100% test suites (310 Backend tests, 78 Jest tests, 5 Playwright tests PASS); cập nhật `.gitignore`.
- **Khả năng biên dịch (Buildability):** Tự chứa độc lập. Đầy đủ cấu hình appsettings, launchSettings, test classes và package tham chiếu.
- **Thông điệp Commit (Tiếng Việt):**
  `test: bổ sung kiểm thử tích hợp xác thực, cấu hình test host và hoàn thiện 100% test suites`

| STT | Loại tệp | Trạng thái Git | Đường dẫn đầy đủ | Nội dung / Hunk thay đổi |
| :---: | :---: | :---: | :--- | :--- |
| 1 | Cấu hình | Modified | `.gitignore` | Bổ sung quy tắc bỏ qua `logs/`, `*.log`, `playwright-report/`, `test-results/`. |
| 2 | Cấu hình | Modified | `frontend/culinary-blog-web/.gitignore` | Bổ sung quy tắc bỏ qua `/playwright-report/` và `/test-results/`. |
| 3 | Cấu hình | Modified | `frontend/culinary-blog-web/.env.example` | Bổ sung biến môi trường mẫu cho Google Client ID và Next.js Base URL. |
| 4 | Cấu hình | Modified | `backend/src/CulinaryBlog.Api/Program.cs` | Bọc `Database:AutoMigrateAndSeed`; cấu hình CORS cho cả 3000, 3001, 5000, 5001. |
| 5 | Cấu hình | Modified | `backend/src/CulinaryBlog.Api/appsettings.json` | Khai báo AllowedOrigins bao gồm cổng 3001 và 5001 cho môi trường dev. |
| 6 | Cấu hình | Modified | `backend/src/CulinaryBlog.Api/Properties/launchSettings.json` | Bổ sung `http://localhost:5001` vào applicationUrl. |
| 7 | Cấu hình | Modified | `frontend/culinary-blog-web/next.config.ts` | Bổ sung `remotePatterns` (localhost, 127.0.0.1, https) cho component Next.js Image. |
| 8 | Cấu hình | Modified | `frontend/culinary-blog-web/playwright.config.ts` | Bổ sung hỗ trợ `SKIP_WEBSERVER` và `NEXT_PUBLIC_BASE_URL`. |
| 9 | Test Project| Modified | `backend/tests/CulinaryBlog.IntegrationTests/CulinaryBlog.IntegrationTests.csproj` | Thêm package `Microsoft.EntityFrameworkCore.InMemory`. |
| 10 | Test Program| Modified | `backend/tests/CulinaryBlog.IntegrationTests/Program.cs` | Thêm toán tử `!` loại bỏ cảnh báo null dereference CS8602. |
| 11 | Integration | **Untracked** | `backend/tests/CulinaryBlog.IntegrationTests/Api/AuthEndpointTests.cs` | Bộ kiểm thử tích hợp tự động cho Minimal APIs Auth (Đăng ký, Đăng nhập, Token Refresh, Đổi mật khẩu). |
| 12 | Test Host | **Untracked** | `backend/tests/CulinaryBlog.IntegrationTests/TestWebApplicationFactory.cs` | Factory tạo WebApplicationFactory với cấu hình test riêng biệt. |
| 13 | Test Config | **Untracked** | `backend/tests/CulinaryBlog.IntegrationTests/appsettings.Testing.json` | Cấu hình database test và mock settings cho kiểm thử tích hợp. |
| 14 | Test Config | **Untracked** | `backend/tests/CulinaryBlog.IntegrationTests/appsettings.json` | Cấu hình Serilog và JwtSettings mặc định cho test runner. |
| 15 | Jest Test | Modified | `frontend/culinary-blog-web/components/search/__tests__/SearchUI.test.tsx` | Bổ sung các test case kiểm tra hiển thị ô tìm kiếm và bộ lọc. |
| 16 | Jest Test | **Untracked** | `frontend/culinary-blog-web/components/__tests__/home-category-integration.test.tsx` | Kiểm thử tích hợp render danh mục tại trang chủ. |
| 17 | Jest Test | **Untracked** | `frontend/components/__tests__/home-category-integration.test.tsx` | Bản mirror test tích hợp danh mục. |
| 18 | E2E Test | Modified | `frontend/culinary-blog-web/e2e/critical-flows.spec.ts` | Tinh chỉnh bộ chọn selector và thời gian chờ cho 5 kịch bản Playwright E2E cốt lõi. |

---

### COMMIT 6: TÀI LIỆU SRS V1.2.0 VÀ BÁO CÁO NGHIỆM THU MINH CHỨNG
- **Mục tiêu:** Lưu trữ tài liệu đặc tả chuẩn SRS v1.2.0 Approved Baseline; cập nhật tài liệu Task 6/7 theo format 13 phần chuẩn; lưu trữ 40 ảnh chụp Playwright làm bằng chứng nghiệm thu thực tế.
- **Khả năng biên dịch (Buildability):** Tài liệu thuần túy (Markdown & PNG), không ảnh hưởng đến build của bất kỳ project nào.
- **Thông điệp Commit (Tiếng Việt):**
  `docs: cập nhật đặc tả SRS v1.2.0 chính thức, tài liệu Task 6/7 và minh chứng nghiệm thu`

| STT | Loại tệp | Trạng thái Git | Đường dẫn đầy đủ | Nội dung / Hunk thay đổi |
| :---: | :---: | :---: | :--- | :--- |
| 1 | Baseline | **Untracked** | `docs/requirements/SRS.md` | Tài liệu đặc tả yêu cầu phần mềm v1.2.0 Approved Baseline (Nguyên trạng 100%, không sửa đổi). |
| 2 | Docs Task 6 | Modified | `docs/README_TASK6_IMAGE.md` | Chuẩn hóa template 13 phần cho chức năng Quản lý Hình ảnh. |
| 3 | Docs Task 6 | Modified | `docs/README_TASK6_SEARCH.md` | Chuẩn hóa template 13 phần cho chức năng Tìm kiếm FTS. |
| 4 | Docs Task 6 | Modified | `docs/README_TASK6_STEP.md` | Chuẩn hóa template 13 phần cho chức năng Quản lý Bước nấu. |
| 5 | Docs Task 7 | **Untracked** | `docs/README_TASK7_INTEGRATION_VALIDATION.md` | Báo cáo tích hợp và nghiệm thu toàn diện Task 7. |
| 6 | Docs Task 7 | **Untracked** | `docs/README_TASK7_SEARCH.md` | Báo cáo nghiệm thu chức năng tìm kiếm Task 7 kèm Traceability Matrix và 16 link tương đối. |
| 7 | Screenshots | **Untracked** | `docs/task7-integration/screenshots/*` (24 tệp PNG) | Ảnh chụp màn hình kiểm thử giao diện responsive desktop/tablet/mobile và các luồng nghiệp vụ. |
| 8 | Screenshots | **Untracked** | `docs/task7-search/screenshots/*` (16 tệp PNG) | Ảnh chụp màn hình 16 kịch bản kiểm thử tìm kiếm, lọc, sắp xếp, phân trang. |
| 9 | Reports | **Untracked** | `FINAL_ZERO_KNOWN_BUGS_REPORT.md` | Báo cáo nghiệm thu chính thức 0 lỗi tồn đọng của dự án. |
| 10 | Reports | **Untracked** | `PRE_COMMIT_CLEANUP_REPORT.md` | Báo cáo tiền commit và kiểm tra cú pháp Git hygiene. |
| 11 | Reports | **Untracked** | `FINAL_COMMIT_FILE_MANIFEST.md` | Bản phân bổ tệp tin và kế hoạch commit chính thức này. |
| 12 | Reports (Tùy chọn) | **Untracked** | 9 Báo cáo kỹ thuật trung gian còn lại | Giữ tại root hoặc lưu vào `docs/audit_reports/` theo quyết định của bạn. |

---

## VII. ĐỐI CHIẾU VÀ LOẠI TRỪ TỆP TIN (EXCLUSIONS)

| Tệp tin / Thư mục | Trạng thái hiện tại | Lý do LOẠI TRỪ TUYỆT ĐỐI KHÔNG COMMIT |
| :--- | :---: | :--- |
| `frontend/culinary-blog-web/playwright-report/` | **Đã ignore** | Thư mục sinh tự động khi chạy Playwright test cục bộ. |
| `frontend/culinary-blog-web/test-results/` | **Đã ignore** | Chứa `.last-run.json` và các ảnh chụp tạm thời khi chạy test tự động. |
| `frontend/culinary-blog-web/.env.local` | **Đã ignore** | Chứa biến môi trường cục bộ máy lập trình viên. |
| `*.log`, `logs/` | **Đã ignore** | Nhật ký runtime của ứng dụng. |

---

## VIII. KẾT LUẬN & ĐỀ XUẤT HÀNH ĐỘNG

Kế hoạch phân bổ tệp tin cho 6 commits trên đảm bảo:
1. **100% Tệp tin được quản lý rõ ràng**, không sót bất kỳ file dependency nào làm hỏng build.
2. **Mỗi commit độc lập và có ý nghĩa rõ ràng (Atomic)** theo chuẩn Conventional Commits tiếng Việt.
3. **Tuyệt đối không đụng chạm đến cổng 3000/5000** của đồ án LMS.
4. **Không triển khai Lab 8.**

**Báo cáo đã sẵn sàng để bạn phê duyệt. Khi bạn đồng ý với kế hoạch phân bổ này, tôi sẽ tiến hành hỗ trợ thực hiện staging và commit theo từng nhóm an toàn.**

