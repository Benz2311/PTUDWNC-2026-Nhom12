# BÁO CÁO NGHIỆM THU CUỐI CÙNG: SỬA LỖI & TUÂN THỦ TOÀN DIỆN SRS V1.2.0
**Dự án**: Culinary Blog – Phát triển Web Nâng Cao – Nhóm 12  
**Thời gian lập báo cáo**: 29/10/2026  
**Thư mục làm việc**: `B:\bài web nhóm mới\main-merge`  
**Tài liệu chuẩn đối chiếu**: `docs/requirements/SRS.md` – Phiên bản 1.2.0 Approved Baseline  
**Trạng thái kiểm thử**: **100% PASS**  

---

## 1. TỔNG QUAN VÀ CAM KẾT AN TOÀN HỆ THỐNG

1. **Cam kết tuân thủ phạm vi**:
   - **Tuyệt đối không triển khai chức năng Lab 8 mới** khi chưa có yêu cầu. Mọi điều chỉnh chỉ tập trung vào việc sửa triệt để các lỗi hiện hữu, vá lỗ hổng bảo mật dependency, chuẩn hóa HTTP Status Code và củng cố độ ổn định kiến trúc theo đúng đặc tả SRS v1.2.0.
2. **Bảo vệ môi trường & cổng dịch vụ song song**:
   - Hệ thống được cấu hình và vận hành độc lập:
     - **Frontend**: `http://localhost:3001` (Next.js Turbopack)
     - **Backend API**: `http://localhost:5001` (ASP.NET Core .NET 10)
     - **Cổng 3000 và 5000**: Được bảo vệ tuyệt đối, không bind, không can thiệp, dành trọn vẹn cho dự án Đồ án chuyên ngành (LMS).
3. **Bảo toàn dữ liệu & Git**:
   - Không thực hiện bất kỳ lệnh nguy hiểm: `git commit`, `git push`, `git merge`, `git checkout`, `git reset`, `git clean` hay `git stash`.
   - Dữ liệu thật và cấu trúc cơ sở dữ liệu PostgreSQL, Redis, MinIO được giữ nguyên vẹn; chỉ tạo và dọn dẹp các bản ghi thử nghiệm trong quá trình kiểm thử tự động.

---

## 2. BẢNG TỔNG KẾT TÌNH TRẠNG SỬA LỖI (100% FIXED)

| ID | Vấn đề / Lỗi phát hiện | Nguyên nhân gốc | Giải pháp thực hiện | Trạng thái |
| :--- | :--- | :--- | :--- | :---: |
| **SEC-01** | Lỗ hổng bảo mật NuGet (`SixLabors.ImageSharp`, `MailKit`, `MimeKit`, `OpenTelemetry.*`) | Phiên bản cũ chứa các CVE nghiêm trọng (GHSA-v76p-62qx-wwq2, GHSA-j3p4-wp97-rph4, GHSA-9j88-vvj5-vhgr, GHSA-4625-4j76-fww9). | Nâng cấp sạch: `SixLabors.ImageSharp 3.2.0`, `MailKit 4.18.2` (kéo `MimeKit 4.18.2`), `System.Security.Cryptography.Pkcs 10.0.13`, `OpenTelemetry 1.16.0`. Kiểm tra `dotnet list package --vulnerable` trả về **0 vulnerabilities**. | **FIXED** |
| **IMG-01** | Upload ảnh validation lỗi trả về HTTP 400 thay vì HTTP 422 | `RecipeEndpoints.cs` bắt `ValidationException` và trả về `Results.BadRequest()` mặc định. | Cập nhật handler trả về `StatusCodes.Status422UnprocessableEntity` cho: file rỗng (0B), file > 5MB, fake magic bytes, ảnh bị hỏng cấu trúc (corrupted payload), MIME type không hợp lệ. | **FIXED** |
| **IMG-02** | Upload ảnh chưa đăng nhập trả về HTTP 403 thay vì HTTP 401 | Endpoint thiếu bước kiểm tra ClaimsPrincipal ở đầu hàm, request lọt vào tầng Recipe ownership check và bị chặn bởi 403. | Bổ sung kiểm tra xác thực đầu tiên: `if (user?.Identity == null || !user.Identity.IsAuthenticated) return 401 Unauthorized`. | **FIXED** |
| **LC-01** | Xung đột đồng thời (Concurrency Conflict) | Thiếu kiểm chứng If-Match ETag khi cập nhật recipe. | Bổ sung kiểm tra `Version` (ETag) khớp điều kiện Optimistic Concurrency Control, trả về HTTP 409 (`RECIPE_CONCURRENCY_CONFLICT`). | **FIXED** |
| **FLT-01** | Nguy cơ rò rỉ dữ liệu soft-delete từ `.IgnoreQueryFilters()` | Rà soát các vị trí truy vấn `ApplicationDbContext`. | Đã kiểm toán: `.IgnoreQueryFilters()` chỉ tồn tại ở `RecipePurgeService` (xóa vĩnh viễn), `RestoreRecipe` (khôi phục thùng rác), `ExistsBySlugAsync` (tránh trùng slug trên unique index DB), và `GetForUpdateAsync` (kèm điều kiện tường minh `!r.IsDeleted`). Các truy vấn công khai không dùng filter bypass. | **VERIFIED** |
| **API-01** | Tuân thủ CONS-003: Chuyển MVC Controllers sang Minimal APIs | `AuthController` và `AdminUsersController` vẫn ở dạng MVC Controllers. | Đã chuyển sang `AuthEndpoints.cs` và `AdminEndpoints.cs` dạng Minimal APIs tại `/api/v1/...`, bảo toàn 100% routing, contract, RFC 7807 và auth policies. | **FIXED** |
| **WARN-01**| Cảnh báo cảnh giới (Warnings) Backend & Frontend | Backend có 761 warnings cú pháp/nullable/DI; Frontend có unused imports và cảnh báo thẻ `<img>`. | Backend dọn sạch toàn bộ, chỉ còn đúng 2 informational warnings về SixLabors license notice (0 warnings logic). Frontend gỡ bỏ unused import, chỉ còn 4 warnings thẻ `<img>` cho cơ chế fallback. | **FIXED** |
| **E2E-01** | Playwright tự động chạy trên cổng 3000 làm xung đột LMS | `playwright.config.ts` hardcoded `localhost:3000`. | Cập nhật cấu hình ưu tiên `process.env.NEXT_PUBLIC_BASE_URL` và cờ `SKIP_WEBSERVER`. Chạy mượt mà 5/5 tests trên cổng 3001. | **FIXED** |

---

## 3. BÁO CÁO CHI TIẾT SỬA LỖI BẢO MẬT NUGET DEPENDENCIES

Đã thực hiện rà soát chuyên sâu theo lệnh `dotnet list package --vulnerable --include-transitive` trên toàn bộ 6 projects trong solution:

| Project | Package được nâng cấp | Phiên bản cũ | Phiên bản mới đã vá | CVE / GHSA đã triệt tiêu |
| :--- | :--- | :---: | :---: | :--- |
| **CulinaryBlog.Application** | `SixLabors.ImageSharp` | 3.1.12 | **3.2.0** | GHSA-v76p-62qx-wwq2, GHSA-j3p4-wp97-rph4, GHSA-j9gm-c75j-xc9q, GHSA-jjfr-hcj7-qf5w, GHSA-gwg2-r3hj-4w44, GHSA-wmxv-xphr-5c9g |
| **CulinaryBlog.Infrastructure** | `SixLabors.ImageSharp` | 3.1.12 | **3.2.0** | Tương thích trọn vẹn với `HeyRed.ImageSharp.Heif 2.1.3` |
| **CulinaryBlog.Infrastructure** | `MailKit` | 4.13.0 | **4.18.2** | GHSA-9j88-vvj5-vhgr |
| **CulinaryBlog.Infrastructure** | `MimeKit` (Transitive) | 4.13.0 | **4.18.2** | GHSA-g7hc-96xr-gvvx |
| **CulinaryBlog.Infrastructure** | `System.Security.Cryptography.Pkcs` | 8.0.2 | **10.0.13** | Khắc phục cảnh báo hạ cấp NU1605 trên .NET 10 |
| **CulinaryBlog.API** | `OpenTelemetry.Api` | 1.12.0 | **1.16.0** | GHSA-4625-4j76-fww9 |
| **CulinaryBlog.API** | `OpenTelemetry.Exporter.OpenTelemetryProtocol` | 1.12.0 | **1.16.0** | GHSA-g94r-2vxg-569j |

### Kết quả kiểm tra lỗ hổng bảo mật:
```text
Project 'CulinaryBlog.Domain' has no vulnerable packages.
Project 'CulinaryBlog.Application' has no vulnerable packages.
Project 'CulinaryBlog.Infrastructure' has no vulnerable packages.
Project 'CulinaryBlog.API' has no vulnerable packages.
Project 'CulinaryBlog.UnitTests' has no vulnerable packages.
Project 'CulinaryBlog.IntegrationTests' has no vulnerable packages.
=> TỔNG CỘNG: 0 VULNERABILITIES (100% SẠCH)
```

---

## 4. KẾT QUẢ KIỂM THỬ MA TRẬN UPLOAD ẢNH (SRS CONS-007, FR-FILE, FR-RCP-008)

Kiểm thử tự động thực tế trên endpoint `POST /api/v1/recipes/{id}/images`:

| Kịch bản kiểm thử | Dữ liệu đầu vào / Tác nhân | HTTP Code kỳ vọng (SRS) | HTTP Code thực tế | Kết luận |
| :--- | :--- | :---: | :---: | :---: |
| **Upload PNG hợp lệ** | PNG 1x1 hợp lệ, < 5MB | **201 Created** | **201** | **PASS** |
| **Upload JPEG hợp lệ** | JPEG 1x1 hợp lệ, < 5MB | **201 Created** | **201** | **PASS** |
| **Upload WebP hợp lệ** | WebP 1x1 hợp lệ, < 5MB | **201 Created** | **201** | **PASS** |
| **Chặn File quá kích thước** | File ảnh giả lập kích thước 5.5 MB | **422 Unprocessable** | **422** | **PASS** |
| **Chặn Fake Magic Bytes** | File text đổi đuôi `.png` (magic bytes giả mạo) | **422 Unprocessable** | **422** | **PASS** |
| **Chặn Ảnh hỏng cấu trúc** | Header PNG cụt / corrupted byte stream | **422 Unprocessable** | **422** | **PASS** |
| **Chặn File rỗng** | File rỗng 0 bytes | **422 Unprocessable** | **422** | **PASS** |
| **Chặn Sai định dạng MIME** | Tài liệu PDF đổi đuôi hoặc gửi MIME `application/pdf` | **422 Unprocessable** | **422** | **PASS** |
| **Chặn Guest Upload** | Request không kèm JWT Bearer Token | **401 Unauthorized** | **401** | **PASS** |
| **Chặn Author không sở hữu** | Recipe của Author A, upload bằng token của Author B | **403 Forbidden** | **403** | **PASS** |
| **Cho phép Admin upload** | Recipe của Author A, upload bằng token của Admin | **201 Created** | **201** | **PASS** |
| **Đổi ảnh chính (Primary)** | `PATCH /recipes/{id}/images/{imageId}/primary` | **200 OK** | **200** | **PASS** |
| **Xóa mềm ảnh (Soft Delete)** | `DELETE /recipes/{id}/images/{imageId}` | **204 No Content** | **204** | **PASS** |

---

## 5. KẾT QUẢ KIỂM THỬ VÒNG ĐỜI RECIPE (LIFECYCLE MATRIX)

| Bước kiểm thử | Hành động & Endpoint | Kết quả thực tế | Trạng thái SRS |
| :--- | :--- | :--- | :---: |
| **1.1 Create Draft** | `POST /api/v1/recipes` | Tạo thành công recipe draft với `Status = Draft`, khởi tạo ETag `Version = 34425`. | **PASS (201)** |
| **1.2 Update Draft** | `PUT /api/v1/recipes/{id}` | Cập nhật thông tin recipe, tăng ETag lên `Version = 34426`. | **PASS (200)** |
| **1.3 Concurrency Conflict**| `PUT /api/v1/recipes/{id}` kèm ETag cũ | Hệ thống chặn thành công xung đột phiên bản với HTTP **409 Conflict** (`RECIPE_CONCURRENCY_CONFLICT`). | **PASS (409)** |
| **1.4 Publish Recipe** | `POST /api/v1/recipes/{id}/publish` | Đổi trạng thái sang `Status = Published`, hiển thị công khai trên catalog. | **PASS (200)** |
| **1.5 Unpublish Recipe** | `POST /api/v1/recipes/{id}/unpublish` | Rút khỏi hiển thị công khai, đưa về trạng thái `Status = Draft`. | **PASS (200)** |
| **1.6 Archive Recipe** | `POST /api/v1/recipes/{id}/archive` | Đưa vào lưu trữ, trạng thái `Status = Archived`. | **PASS (200)** |
| **1.7 Unarchive Recipe** | `POST /api/v1/recipes/{id}/unarchive` | Khôi phục từ lưu trữ, chuyển về `Status = Draft`. | **PASS (200)** |
| **1.8 Soft Delete Recipe** | `DELETE /api/v1/recipes/{id}` | Xóa mềm công thức (`IsDeleted = true`), ẩn khỏi toàn bộ API public. | **PASS (204)** |
| **1.9 Admin Trash** | `GET /api/v1/admin/recipes/trash` | Chỉ Admin xem được danh sách thùng rác chứa recipe đã xóa mềm. | **PASS (200)** |
| **1.10 Admin Restore** | `POST /api/v1/admin/recipes/{id}/restore` | Admin khôi phục recipe đã xóa mềm trở lại hoạt động bình thường. | **PASS (200)** |
| **1.11 Admin Purge** | `DELETE /api/v1/admin/recipes/{id}/purge` | Admin dọn dẹp xóa vĩnh viễn dữ liệu kiểm thử, trả về HTTP 204. | **PASS (204)** |

---

## 6. KẾT QUẢ KIỂM THỬ TRÌNH DUYỆT THẬT (REAL CHROMIUM ACCEPTANCE)

Kiểm thử tự động trên trình duyệt thật độc lập Chromium profile, kiểm tra kết xuất giao diện, dữ liệu API, điều hướng và console log. Toàn bộ hình ảnh minh chứng được lưu tại:  
`frontend/culinary-blog-web/test-results/final-acceptance/`

| # | Trang kiểm thử | URL kiểm thử | Kết quả | Tệp Screenshot minh chứng |
| :-: | :--- | :--- | :---: | :--- |
| **1** | Trang chủ (Home Page) | `http://localhost:3001` | **PASS (200)** | `01_home_page.png` |
| **2** | Khám phá công thức | `http://localhost:3001/recipes` | **PASS (200)** | `02_recipes_catalog.png` |
| **3** | Danh mục món ăn | `http://localhost:3001/categories` | **PASS (200)** | `03_categories_list.png` |
| **4** | Tìm kiếm công thức | `http://localhost:3001/search?q=phở` | **PASS (200)** | `04_search_results.png` |
| **5** | Đăng nhập hệ thống | `http://localhost:3001/login` | **PASS (200)** | `05_login_page.png` |
| **6** | Đăng ký tài khoản | `http://localhost:3001/register` | **PASS (200)** | `06_register_page.png` |
| **7** | Quản trị người dùng (Admin) | `http://localhost:3001/admin/users` | **PASS (200)** | `07_admin_users.png` |
| **8** | Tạo mới công thức (Author) | `http://localhost:3001/recipes/create` | **PASS (200)** | `08_recipe_create.png` |
| **9** | Hồ sơ cá nhân (Profile) | `http://localhost:3001/profile` | **PASS (200)** | `09_profile_page.png` |

---

## 7. KẾT QUẢ HỒI QUY TOÀN DIỆN HỆ THỐNG (REGRESSION SUITE)

Hệ thống đã trải qua kiểm thử tuần tự không phát sinh bất kỳ lỗi hồi quy nào:

1. **Backend .NET Build (`dotnet build`)**:
   - `0 Error(s)`
   - `2 Warning(s)` (Chỉ là thông báo cộng đồng về SixLabors license notice, sạch hoàn toàn cảnh báo mã nguồn).
2. **Backend Unit Tests (`dotnet test UnitTests`)**:
   - **253 Passed**, 0 Failed, 1 Skipped (100% Pass).
3. **Backend Integration Tests (`dotnet test IntegrationTests`)**:
   - **55 Passed**, 0 Failed, 1 Skipped (100% Pass).
4. **Frontend TypeScript Typecheck (`npx tsc --noEmit`)**:
   - **0 Error(s)** (Kiểm tra kiểu dữ liệu tĩnh nghiêm ngặt hoàn tất sạch sẽ).
5. **Frontend ESLint (`npm run lint`)**:
   - **0 Error(s)**, 4 Warning(s) (Chỉ là cảnh báo `<img>` có kiểm soát cho chức năng dynamic image fallback).
6. **Frontend Jest Tests (`npm run test`)**:
   - **6 Test Suites passed**, **78 tests passed** (100% Pass).
7. **Frontend Production Build (`npm run build`)**:
   - **16/16 Static pages generated successfully** (100% Static & Dynamic routes tối ưu hóa hoàn chỉnh).
8. **Playwright Critical Flows (`npx playwright test e2e/critical-flows.spec.ts`)**:
   - **5/5 Tests passed** (Navigation, Recipe Catalog, Category Page, Search Input, Route Transitions).

---

## 8. ĐỐI CHIẾU TUÂN THỦ ĐẶC TẢ SRS V1.2.0

| Mục SRS | Nội dung yêu cầu | Đánh giá tuân thủ |
| :--- | :--- | :---: |
| **CONS-003** | Toàn bộ API nghiệp vụ phải tuân thủ kiến trúc Minimal APIs | **ĐẠT (100%)** – Chuyển đổi Auth và Admin Users sang Minimal APIs |
| **CONS-007** | Upload ảnh: Giới hạn 5MB, xác thực định dạng, magic bytes, lưu trữ S3/MinIO | **ĐẠT (100%)** – Kiểm tra magic bytes và MIME chặt chẽ, HTTP 422 khi sai phạm |
| **FR-RCP-008**| Quản lý ảnh công thức: thêm, sửa, đặt ảnh chính, xóa mềm | **ĐẠT (100%)** – Hỗ trợ đầy đủ endpoint và xác thực quyền sở hữu |
| **FR-FILE** | Xử lý tệp tin và mã trạng thái chuẩn RFC 7807 (422, 401, 403, 503) | **ĐẠT (100%)** – Trả về đúng mã HTTP chuẩn theo từng trường hợp lỗi |
| **NFR-SEC-01**| Bảo mật phụ thuộc, ngăn chặn khai thác mã độc và DoS qua file ảnh/mail | **ĐẠT (100%)** – 0 lỗ hổng bảo mật NuGet, ImageSharp 3.2.0 & MailKit 4.18.2 |
| **DATA-ISO** | Cô lập dữ liệu xóa mềm, không rò rỉ dữ liệu qua các bộ lọc query | **ĐẠT (100%)** – Xác minh toàn bộ repository query tôn trọng Soft Delete |

---

## 9. DANH SÁCH FILE ĐÃ THAY ĐỔI & TRẠNG THÁI GIT

Toàn bộ các thay đổi được giữ nguyên trong working tree, **không commit/push/reset**:

### Backend:
- `backend/src/CulinaryBlog.Application/CulinaryBlog.Application.csproj` (Nâng cấp SixLabors.ImageSharp 3.2.0)
- `backend/src/CulinaryBlog.Infrastructure/CulinaryBlog.Infrastructure.csproj` (Nâng cấp ImageSharp 3.2.0, MailKit 4.18.2, System.Security.Cryptography.Pkcs 10.0.13)
- `backend/src/CulinaryBlog.Api/Endpoints/Recipes/RecipeEndpoints.cs` (Chuẩn hóa HTTP 422, 401, 403, 503 cho upload ảnh)
- `backend/src/CulinaryBlog.Api/Endpoints/Auth/AuthEndpoints.cs` (Minimal APIs thay thế AuthController)
- `backend/src/CulinaryBlog.Api/Endpoints/Admin/` (Minimal APIs thay thế AdminUsersController)
- `backend/src/CulinaryBlog.Api/Program.cs` (Cấu hình CORS an toàn không dùng wildcard, map minimal endpoints)

### Frontend:
- `frontend/culinary-blog-web/playwright.config.ts` (Hỗ trợ cấu hình cổng linh hoạt 3001, SKIP_WEBSERVER)
- `frontend/culinary-blog-web/components/admin/AdminUsersPage.tsx` (Dọn dẹp unused imports, chuẩn hóa giao diện)
- `frontend/culinary-blog-web/test-results/final-acceptance/` (Lưu 9 ảnh screenshot minh chứng và file tổng hợp kết quả JSON)

---

## 10. KẾT LUẬN & ĐỀ NGHỊ

1. Dự án **Culinary Blog – Web Nâng Cao Nhóm 12** đã hoàn tất việc sửa toàn bộ các lỗi phát hiện, xóa bỏ 100% lỗ hổng bảo mật dependency, chuẩn hóa toàn diện mã trạng thái HTTP theo đặc tả SRS v1.2.0 và vượt qua toàn bộ 8 bài kiểm thử hồi quy tự động cũng như trình duyệt thật.
2. Các dịch vụ Frontend (`:3001`) và Backend (`:5001`) đang được duy trì chạy nền sẵn sàng để kiểm tra thực tế bất kỳ lúc nào.
3. Không có bất kỳ chức năng Lab 8 mới nào bị triển khai ngoài ý muốn. Hệ thống đã đạt trạng thái sẵn sàng cao nhất để bàn giao hoặc tiến hành giai đoạn tiếp theo theo chỉ đạo của người dùng.

