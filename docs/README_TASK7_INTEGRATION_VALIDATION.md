# BÁO CÁO NGHIỆM THU TÍCH HỢP TOÀN DIỆN LAB 7 (TASK 7) & KIỂM TRA DOCKER POSTGRESQL – NHÓM 12

- **Dự án**: Culinary Blog – Phát triển ứng dụng Web nâng cao (`PTUDWNC-2026-Nhom12`)
- **Repository**: `https://github.com/Benz2311/PTUDWNC-2026-Nhom12`
- **Nhóm trưởng**: Võ Hùng Mạnh (`2312687`)
- **Nhánh `origin/main` hiện tại**: `6d46d1eef605039133f222bb04eb45cc7d094b96` (Giữ nguyên 100%, chưa merge vào `main`, chưa push lên GitHub)
- **Nhánh tích hợp thử nghiệm**: `integration/task7-validation` (tại worktree `B:\bài web nhóm mới\task7-worktrees\integration`)

---

## 1. Ma trận phân công & trạng thái nhánh của 4 thành viên (Lab 1 – Lab 7)

| Thành viên | MSSV | Nhánh Task 7 | HEAD Commit | Merge-base với `origin/main` | Các Module phụ trách xuyên suốt (Lab 1–7) | Trạng thái tích hợp trên `integration/task7-validation` |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **1. Võ Hùng Mạnh** *(Nhóm trưởng)* | `2312687` | `feat/vohungmanh-task7-search` | `61bdf93ae4b955296168bfd78e418fa5b3af6f4c` | `6d46d1e` | **RecipeStep** (`StepEditor`, `StepItem`, Step API), **RecipeImage** (`ImageManager`, `ImageUploadArea`, `ImagePreviewModal`, MinIO Storage, Thumbnail Job), **RecipeSearch** (`SearchBox`, `SearchContainer`, `/search`, PostgreSQL FTS `tsvector` + `unaccent` + `pg_trgm`), **Observability & Health Check** (`/health`, `/health/live`, `/health/ready`, Serilog, OpenTelemetry) | **PASS 100%** – Tích hợp sạch (0 xung đột), bảo toàn 100% Step, Image, MinIO, Search |
| **2. Lê Thị Ánh Nhung** | `2312709` | `origin/2312709-LeThiAnhNhung-Home-Category-PublicLayoutIntegration` | `720834dfeaf887706d04a57abfdd4bfc434550bf` | `6d46d1e` | **Category & Recipe Read** (`GET /api/v1/categories`, `GET /api/v1/categories/{slug}`, `GET /api/v1/recipes`), **Shared UI & Global Layout** (`Header`, `Footer`, `PublicSidebar`, `PublicLayout`, `Pagination`), **Home (`/`) & Category Pages (`/categories`, `/categories/[slug]`)**, **Rate Limiting** | **PASS 100%** – Tích hợp sạch (0 xung đột), kết nối API thật |
| **3. Nguyễn Văn Quốc** | `2312729` | `origin/feat/NguyenVanQuoc-update-buoi7` | `e788319c3c0eefbdbcc6c525c315df03571f27e9` | `6d46d1e` | **Authentication, User & Profile** (`ApplicationUser`, `RefreshToken`, JWT, `/login`, `/register`, `/forgot-password`, `/reset-password`, `/confirm-email`, `/profile`, `ChangePasswordCard`, `auth-store.ts`, `ProtectedRoute`, `/admin/users`) | **PASS (Sau khi xử lý xung đột trên nhánh tích hợp)** – Đã loại bỏ Controller trùng lặp, đồng bộ `UserRepository` với bảng `"Users"` của PostgreSQL và sửa hydration F5 |
| **4. Phạm Nguyễn Ngọc Phước** | `2312718` | `origin/2312718_PhamNguyenNgocPhuoc_Bua07` | `1f8a8eb8f0e61e92944a7ade58c8bb15518765ec` | `be592f9` *(tách từ Bữa 6 cũ)* | **Recipe, Ingredient, Nutrition & Recipe Detail** (`Recipe`, `RecipeIngredient`, `RecipeNutrition`, `RecipeCard`, `RecipeMeta`, `RecipeDetailSkeleton`, `RecipeDetailView`, trang `/recipes/[slug]` xử lý 404/403) | **PASS (Sau khi đồng bộ sang `culinary-blog-web` trên nhánh tích hợp)** – Đã tích hợp `/recipes/[slug]` và đồng bộ mã lỗi `ApiError.status` (404/403) |

---

## 2. Kiểm tra Docker, PostgreSQL, Redis và MinIO

### 2.1. Danh sách Docker Containers & Volumes đang hoạt động
| Container Name | Container ID | Image | Cổng kết nối | Volume gắn kết | Trạng thái |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `culinary-blog-postgres` | `a5085edd9a67` | `postgres:16` | `0.0.0.0:5433->5432/tcp` | `ptudwnc-2026-nhom12_postgres_data` | **Up (Healthy)** – Database chuẩn `culinary_blog` theo `docker-compose.yml` |
| `culinaryblog-postgres` | `cd45fe9075a6` | `postgres:16-alpine` | `0.0.0.0:5432->5432/tcp` | `ptudwnc-phuoc_postgres_data` | **Up (Healthy)** – Database `culinaryblog` & `culinaryblog_test` |
| `culinaryblog-redis` | `562f5ae73b2e` | `redis:alpine` | `0.0.0.0:6379->6379/tcp` | *(In-memory)* | **Up (Healthy – Latency 1.0ms)** |
| `culinaryblog-minio` | `89ffad91708d` | `cgr.dev/chainguard/minio` | `0.0.0.0:9000-9001->9000-9001/tcp` | `culinaryblog-minio-data` | **Up (Healthy – Bucket `culinaryblog` với 15+ thư mục ảnh công thức)** |

### 2.2. Kiểm tra EF Core Migrations & Toàn vẹn dữ liệu PostgreSQL (`culinary_blog`)
- **Extensions đã cài đặt**: `pg_trgm` (`1.6`), `unaccent` (`1.1`), `plpgsql` (`1.0`).
- **7 EF Core Migrations đã áp dụng trong `__EFMigrationsHistory`**:
  1. `20260922105930_InitialIntegrated`
  2. `20260922160049_GroupIntegrationUpdate`
  3. `20260928080623_AddStepImageAndFullTextSearch`
  4. `20260928144742_AddCategoryConfiguration`
  5. `20260929164148_SyncRecipeReadIndexes`
  6. `20260930053854_AddUserAuthOptimizedIndexes`
  7. `20261007054354_SyncIntegratedTeamModels`
- **Đối chiếu số lượng bản ghi trước và sau khi kiểm thử**:
  - `Users`: **3** (Trước: 3 / Sau: 3 – 0 bản ghi mồ côi)
  - `Categories`: **20** (Trước: 20 / Sau: 20)
  - `Recipes`: **67** (59 `Published` + 8 `Draft` – Trước: 67 / Sau: 67 – 0 bản ghi mồ côi)
  - `RecipeSteps`: **77** (Trước: 77 / Sau: 77 – 0 bản ghi mồ côi)
  - `RecipeImages`: **35** (11 `Primary` + 24 `Secondary` – Trước: 35 / Sau: 35 – 0 bản ghi mồ côi)
  - `RefreshTokens`: **9** (Trước: 9 / Sau: 9 – 0 bản ghi mồ côi)

---

## 3. Các lỗi phát hiện và đã sửa có kiểm soát trên `integration/task7-validation`

1. **Xung đột Route giữa Controller cũ và Minimal API (Nhánh Nguyễn Văn Quốc)**:
   - *Nguyên nhân*: Nhánh Quốc mang theo các Controller cũ (`CategoriesController.cs`, `RecipesController.cs`, `RecipeImagesController.cs`) trùng route `/api/v1/categories` và `/api/v1/recipes` với `CategoryEndpoints.cs` và `RecipeEndpoints.cs`, gây `AmbiguousMatchException`.
   - *Cách sửa*: Chỉ giữ `AuthController.cs` và `AdminUsersController.cs`, loại bỏ các Controller trùng lặp với Minimal API.
2. **Lệch cấu trúc bảng `"Users"` trong `UserRepository.cs` (Nhánh Nguyễn Văn Quốc)**:
   - *Nguyên nhân*: Nhánh Quốc truy vấn cột `NormalizedEmail` / `NormalizedUserName` của Identity, không tồn tại trong bảng `"Users"` chuẩn (11 cột) trên PostgreSQL.
   - *Cách sửa*: Đồng bộ `UserRepository.cs` sử dụng `x.Email.ToLower()` và `x.UserName.ToLower()` khớp 100% với bảng `"Users"` và Index hiện có trên PostgreSQL.
3. **Lỗi Hydration khi nhấn F5 (Reload) tại trang `/profile` (`AuthContext.tsx` & `app/layout.tsx`)**:
   - *Nguyên nhân*: `useAuthStore` khởi tạo `isLoading: false`, khiến `ProtectedRoute` chạy `useEffect` chuyển hướng về `/login` trước khi `AuthProvider` kịp chạy `hydrate()` từ `localStorage`. Кроме đó, `<Header />` nằm ngoài `<AppProviders>`.
   - *Cách sửa*: Bổ sung cờ `isInitializing` trong `AuthProvider` (`contexts/AuthContext.tsx`) giữ trạng thái `isLoading = true` cho tới khi `hydrate()` hoàn tất, và bọc `<AppProviders>` ra ngoài `<Header />` trong `app/layout.tsx`.
4. **Lỗi bắt mã `404` trên trang Recipe Detail (`app/(public)/recipes/[slug]/page.tsx`)**:
   - *Nguyên nhân*: `apiClient` ném ra đối tượng `ApiError` có thuộc tính `status` (`err.status = 404`), nhưng trang `recipes/[slug]/page.tsx` chỉ đọc `err.statusCode` hoặc `err.response?.status`, dẫn tới hiển thị thông báo lỗi chung thay vì màn hình `404 Không tìm thấy công thức`.
   - *Cách sửa*: Cập nhật `const statusCode = anyErr?.status ?? anyErr?.statusCode ?? anyErr?.response?.status;` trong `app/(public)/recipes/[slug]/page.tsx`.
5. **Bảo vệ Database Docker thật khi khởi động Backend (`Program.cs`)**:
   - *Nguyên nhân*: `Program.cs` tự động chạy `MigrateAsync()` và seed thêm user khi khởi động ở môi trường khác `Testing`.
   - *Cách sửa*: Đặt điều kiện cấu hình `Database:AutoMigrateAndSeed` (mặc định `false`) để không tự động ghi vào cơ sở dữ liệu thật khi chưa được bật tường minh.

---

## 4. Kết quả kiểm thử tự động & Kiểm thử trình duyệt thật (Playwright E2E)

### 4.1. Kiểm thử tự động (Frontend & Backend)
- **Frontend TypeScript (`npx tsc --noEmit`)**: **PASS** (0 errors)
- **Frontend ESLint (`npm run lint`)**: **PASS** (0 errors, 5 warnings `<img>`)
- **Frontend Jest (`npm test -- --runInBand`)**: **PASS** (6/6 test suites, **78/78 tests passed** – bao gồm `StepEditor.test.tsx`, `ImageManager.test.tsx`, `SearchUI.test.tsx`, `home-category-integration.test.tsx`, `shared-components.test.tsx`, `recipe-utils.test.ts`)
- **Frontend Production Build (`npx next build`)**: **PASS** (14/14 routes: `/`, `/categories`, `/categories/[slug]`, `/recipes/[slug]`, `/search`, `/login`, `/register`, `/forgot-password`, `/reset-password`, `/confirm-email`, `/profile`, `/admin/users`, `/components-preview`, `/_not-found`)
- **Backend Build (`dotnet build backend/CulinaryBlog.sln`)**: **PASS** (0 errors)
- **Backend Unit Tests (`CulinaryBlog.UnitTests`)**: **PASS** (**253/253 passed**, 1 skipped, 0 failed)
- **Backend Integration Tests (`CulinaryBlog.IntegrationTests`)**: **PASS** (**55/55 passed**, 1 skipped, 0 failed)

### 4.2. Kiểm thử trình duyệt thật Playwright E2E (15 luồng + Ảnh chụp màn hình)
Toàn bộ ảnh chụp màn hình được lưu tại `docs/task7-integration/screenshots/`:
1. `01_home_desktop_1440.png` – Trang chủ & Sidebar danh mục từ API thật (**PASS**)
2. `02_all_categories.png` – Trang tất cả danh mục `/categories` (**PASS**)
3. `03_category_detail_an_vat.png` – Trang chi tiết danh mục `/categories/an-vat` (**PASS**)
4. `04_recipe_detail_pho_bo.png` – Trang chi tiết công thức `/recipes/pho-bo-ha-noi` hiển thị đầy đủ ảnh chính/phụ, nguyên liệu, dinh dưỡng, các bước thực hiện (**PASS**)
5. `05_recipe_detail_404.png` – Xử lý `404 Not Found` khi truy cập `/recipes/khong-ton-tai-999` (**PASS**)
6. `06_search_unaccent_pho_bo.png` – Tìm kiếm tiếng Việt không dấu `"pho bo"` và có dấu `"Phở bò"` ra đúng `"Phở bò Hà Nội"` (**PASS**)
7. `07_search_filter_sort.png` – Bộ lọc danh mục, độ khó, thời gian nấu, sắp xếp và đồng bộ URL `/search?q=pho%20bo&sort=cookTime&page=1` (**PASS**)
8. `08_search_empty_state.png` – Trạng thái Empty State khi tìm từ khóa không khớp `/search?q=khongtontaixyz999` (**PASS**)
9. `09_step_editor.png` – Giao diện `StepEditor` của Võ Hùng Mạnh hoạt động đầy đủ (**PASS**)
10. `10_image_manager.png` – Giao diện `ImageManager` của Võ Hùng Mạnh hoạt động đầy đủ (**PASS**)
11. `11_login_page.png` – Giao diện Đăng nhập `/login` & kiểm tra `ProtectedRoute` chặn khách chưa đăng nhập tại `/profile` (**PASS**)
12. `12_profile_authenticated.png` – Trang `/profile` sau khi đăng nhập và nhấn F5 reload giữ nguyên phiên đăng nhập (**PASS**)
13. `13_home_tablet_768.png` – Giao diện Responsive Tablet `768×1024` (`overflow = false`) (**PASS**)
14. `14_home_mobile_390.png` – Giao diện Responsive Mobile `390×844` (`overflow = false`) (**PASS**)
