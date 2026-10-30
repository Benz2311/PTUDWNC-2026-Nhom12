# BÁO CÁO NGHIỆM THU CUỐI CÙNG: ZERO KNOWN BUGS – CULINARY BLOG
**Dự án**: Culinary Blog – Phát triển Web Nâng Cao – Nhóm 12  
**Thời gian lập báo cáo**: 29/10/2026  
**Thư mục làm việc**: `B:\bài web nhóm mới\main-merge`  
**Tài liệu chuẩn đối chiếu**: `docs/requirements/SRS.md` – Phiên bản 1.2.0 Approved Baseline  
**Trạng thái kiểm thử tổng thể**: **100% READY – ZERO CRITICAL/HIGH BUGS**  

---

## 1. TỔNG QUAN VÀ CAM KẾT TUÂN THỦ PHẠM VI

1. **Bảo toàn phạm vi dự án**:
   - **Tuyệt đối không triển khai Lab 8**. Mọi hoạt động chỉ tập trung sửa triệt để các lỗi tồn đọng, kích hoạt và chạy thành công 100% các bài test từng bị skip, giải quyết các cảnh báo giao diện, dọn sạch mã lỗi mạng 404 trên trang preview và kiểm chứng ma trận an ninh theo SRS v1.2.0.
2. **Bảo vệ môi trường & Cổng dịch vụ song song**:
   - **Frontend Culinary Blog**: `http://localhost:3001` (Next.js Turbopack)
   - **Backend Culinary Blog API**: `http://localhost:5001` (ASP.NET Core .NET 10)
   - **Cổng 3000 và 5000**: Giữ nguyên vẹn cho dự án Đồ án chuyên ngành (LMS).
3. **Bảo toàn dữ liệu & Git**:
   - Không thực hiện bất kỳ lệnh nguy hiểm: `git commit`, `git push`, `git merge`, `git checkout`, `git reset`, `git clean` hay `git stash`.
   - Cơ sở dữ liệu thật `culinaryblog` được giữ nguyên vẹn 100%. Các bài test persistence chạy trên database riêng `culinaryblog_test`.

---

## 2. KẾT QUẢ GIẢI QUYẾT TOÀN BỘ CÁC BÀI TEST TỪNG BỊ SKIP

Trước đợt xử lý:
- UnitTests: 253 Passed, **1 Skipped**.
- IntegrationTests: 55 Passed, **1 Skipped**.

### 2.1. Phân tích nguyên nhân gốc:
1. **`RecipePersistenceIntegrationTests.cs:160` (UnitTests)**:
   - Thuộc tính kiểm tra `CULINARYBLOG_TEST_CONNECTION` bắt buộc kết thúc bằng `_test`.
   - Lỗi logic tiềm ẩn trong bài test: Tại dòng 145, test kiểm tra trạng thái xóa mềm sau khi gọi `DeleteAsync`, nhưng truy vấn trực tiếp qua `db.Recipes.Where(...)` mà không có `.IgnoreQueryFilters()`. Vì entity `Recipe` có global query filter `!r.IsDeleted`, query đã lọc bỏ bản ghi dẫn đến ngoại lệ `Sequence contains no elements`.
   - **Giải pháp**:
     - Cung cấp kết nối database kiểm thử riêng biệt: `Host=localhost;Port=5432;Database=culinaryblog_test;Username=culinaryadmin;Password=123456`.
     - Bổ sung `.IgnoreQueryFilters()` tại dòng 146 trước khi query kiểm tra `item.IsDeleted` đúng chuẩn kỹ thuật EF Core.
2. **`CategoriesEndpointTests.GetCategories_ReturnsSuccessStatusCode` (IntegrationTests)**:
   - Bị skip do thiếu biến môi trường `CULINARYBLOG_TEST_CONNECTION` khi khởi động fixture.
   - **Giải pháp**: Cung cấp biến môi trường trỏ vào `culinaryblog_test`.

### 2.2. Kết quả kiểm thử thực tế sau xử lý:
- **`CulinaryBlog.UnitTests`**:
  ```text
  Passed! - Failed: 0, Passed: 254, Skipped: 0, Total: 254, Duration: 3 s
  ```
- **`CulinaryBlog.IntegrationTests`**:
  ```text
  Passed! - Failed: 0, Passed: 56, Skipped: 0, Total: 56, Duration: 1 m 4 s
  ```
- **TỔNG CỘNG BACKEND TESTS: 310/310 PASSED (100%), 0 SKIPPED, 0 FAILED**.

---

## 3. XÁC MINH VÀ GIẢI QUYẾT GIẤY PHÉP IMAGESHARP

### 3.1. Thông tin từ nguồn chính thức Six Labors (`https://sixlabors.com/pricing/`)
- Gói `SixLabors.ImageSharp` từ phiên bản 3.0 trở lên áp dụng **Six Labors Split License** (Apache 2.0 with Commons Clause kết hợp Commercial License).
- **Điều kiện sử dụng miễn phí (Community License)**:
  - Dự án phi thương mại, nghiên cứu khoa học, mã nguồn mở hoặc tổ chức giáo dục.
  - Doanh nghiệp/tổ chức có tổng doanh thu hàng năm dưới 1.000.000 USD.
- **Dự án của Nhóm 12**: Là đồ án môn học Web Nâng Cao thuộc trường Đại học, mang tính chất học thuật và phi thương mại 100%, do đó **hoàn toàn đủ điều kiện sử dụng miễn phí hợp pháp** theo chính sách của Six Labors.

### 3.2. Phân biệt cơ chế Debug vs Release
- Trong file `SixLabors.ImageSharp.targets`, cơ chế kiểm tra bản quyền hoạt động như sau:
  - Khi build ở chế độ **Debug**: Target phát ra cảnh báo thông tin (`warning: No Six Labors license found. This Debug build can continue, but Release builds require a valid license.`). Quá trình build thành công bình thường.
  - Khi build ở chế độ **Release**: Nếu dự án thương mại triển khai sản xuất, cần đăng ký key miễn phí tại Six Labors và khai báo thuộc tính `<SixLaborsLicenseKey>` trong file `.csproj` hoặc biến môi trường.
- **Cam kết tuân thủ**:
  - Không tạo license giả mạo.
  - Không hạ cấp xuống ImageSharp 2.x hoặc 3.1.x (vì chứa các CVE bảo mật nghiêm trọng).
  - Không chèn cấu hình tắt analyzer trái phép.

---

## 4. DỌN SẠCH MÃ LỖI HTTP 404 TRÊN TRANG PREVIEW (`/components-preview`)

### 4.1. Nguyên nhân gốc:
- Trang `/components-preview` là trang môi trường phát triển (preview sandbox) dùng để kiểm thử bố cục giao diện của 4 thành viên.
- Trang truyền mã ID giả định `recipeId="rec-preview-001"` vào `<StepEditor />` và `<ImageManager />`.
- Khi nhận được `recipeId`, hai component này tự động chuyển sang chế độ "Direct API mode" và gửi request `GET /api/v1/recipes/rec-preview-001/steps` và `/images` đến Backend. Vì `rec-preview-001` không tồn tại trong database, Backend trả về HTTP **404 Not Found**.

### 4.2. Giải pháp kỹ thuật an toàn:
- Theo đúng thiết kế trong `StepEditor.tsx` và `ImageManager.tsx`: Khi không truyền `recipeId`, component hoạt động ở chế độ **Controlled Mode (quản lý state cục bộ)**.
- Đã cập nhật file `app/components-preview/page.tsx` truyền dữ liệu mẫu có sẵn `sampleDetail.steps` và `sampleDetail.images` vào props `initialSteps` và `initialImages` mà không truyền `recipeId`.
- **Kết quả kiểm thử**: Trang `/components-preview` tải thành công với HTTP 200, hiển thị đầy đủ giao diện tương tác, **0 lỗi console và 0 request 404**.

---

## 5. XỬ LÝ TRIỆT ĐỂ CÁC CẢNH BÁO FRONTEND (0 WARNINGS)

### 5.1. Vấn đề 4 cảnh báo `@next/next/no-img-element`:
- Tồn tại tại `AdminUsersPage.tsx` (1 warning) và `ProfilePage.tsx` (3 warnings) khi hiển thị avatar người dùng bằng thẻ `<img>`.
- Next.js yêu cầu dùng component `<Image />`, nhưng nếu avatar trỏ đến domain ngoài chưa khai báo trong `next.config.ts`, `<Image />` sẽ ném ngoại lệ runtime.

### 5.2. Giải pháp chuyển đổi an toàn:
- Sử dụng component `<Image />` từ `next/image` kết hợp thuộc tính **`unoptimized={true}`**:
  ```tsx
  <Image
    src={user.avatarUrl}
    alt={user.displayName}
    width={40}
    height={40}
    unoptimized
    className="h-10 w-10 rounded-full object-cover"
  />
  ```
- **Lợi ích**:
  1. Thỏa mãn 100% quy tắc ESLint `@next/next/no-img-element`.
  2. Next.js không tối ưu hóa qua server nên chấp nhận mọi URL avatar động bên ngoài mà không cần cấu hình trước domain.
  3. Giữ nguyên vẹn handler `onError` để tự động ẩn ảnh hỏng và chuyển sang avatar chữ cái đầu (`getInitials`).
- **Kết quả kiểm tra ESLint**:
  ```text
  > eslint
  (0 problems, 0 errors, 0 warnings)
  ```

---

## 6. KẾT QUẢ KIỂM THỬ BẢO MẬT & XÁC THỰC TOÀN DIỆN (12/12 PASS)

Kịch bản kiểm thử tự động `test_comprehensive_security.js` đã thực hiện và xác minh 100% các tiêu chí bảo mật theo SRS:

| ID | Kịch bản kiểm thử bảo mật | Kỳ vọng | Kết quả thực tế | Trạng thái |
| :--- | :--- | :---: | :---: | :---: |
| **SEC-01** | Khách vãng lai (Guest) truy cập endpoint bảo vệ `/auth/me` | HTTP 401 | **401 Unauthorized** | **PASS** |
| **SEC-02** | Tác giả (Author) truy cập endpoint hợp lệ của mình `/auth/me` | HTTP 200 | **200 OK** | **PASS** |
| **SEC-03** | Tác giả (Author) cố tình truy cập trang quản trị `/admin/users` | HTTP 403 | **403 Forbidden** | **PASS** |
| **SEC-04** | Quản trị viên (Admin) truy cập trang quản trị `/admin/users` | HTTP 200 | **200 OK** | **PASS** |
| **SEC-05** | Token JWT giả mạo, hết hạn hoặc sai chữ ký số | HTTP 401 | **401 Unauthorized** | **PASS** |
| **SEC-06** | Đăng nhập với mật khẩu không chính xác | HTTP 401 | **401 Unauthorized** | **PASS** |
| **SEC-07** | Đăng ký tài khoản với mật khẩu quá ngắn/yếu | HTTP 400 | **400 Bad Request** | **PASS** |
| **SEC-08** | Refresh Token Rotation (Xoay vòng token phiên) | HTTP 200 | **200 OK (New Token)** | **PASS** |
| **SEC-09** | Tái sử dụng Refresh Token cũ đã bị xoay vòng (Reuse Attack) | HTTP 401 | **401 (Family Revoked)** | **PASS** |
| **SEC-10** | Ngăn chặn tấn công IDOR: Sửa Recipe của tác giả khác | HTTP 403 | **403 Forbidden** | **PASS** |
| **SEC-11** | Cấu trúc phản hồi lỗi tuân thủ chuẩn RFC 7807 ProblemDetails | Có Type/Title | **ProblemDetails Chữ ký chuẩn** | **PASS** |
| **SEC-12** | Chặn CORS Origin lạ (ngăn chặn tấn công cross-origin độc hại) | Không cho phép | **Preflight Reject / Safe Origin** | **PASS** |

---

## 7. KẾT QUẢ KIỂM TRA CONSOLE & NETWORK TRÊN 10 TRANG CHROMIUM

Kiểm tra tự động trên trình duyệt Chromium profile sạch, không extension qua kịch bản `test_full_pages_console_network.js`. Tất cả ảnh chụp màn hình được lưu trữ tại `frontend/culinary-blog-web/test-results/page-audit/`:

| STT | Tuyến đường (Route) | Mã HTTP | Console Errors | Network 4xx/5xx | Ảnh chụp minh chứng | Trạng thái |
| :-: | :--- | :---: | :---: | :---: | :--- | :---: |
| 1 | `/` (Trang chủ) | 200 | 0 | 0 | `01_home.png` | **PASS** |
| 2 | `/recipes` (Khám phá công thức) | 200 | 0 | 0 | `02_recipes.png` | **PASS** |
| 3 | `/categories` (Danh mục món ăn) | 200 | 0 | 0 | `03_categories.png` | **PASS** |
| 4 | `/search?q=phở` (Tìm kiếm công thức) | 200 | 0 | 0 | `04_search.png` | **PASS** |
| 5 | `/login` (Đăng nhập) | 200 | 0 | 0 | `05_login.png` | **PASS** |
| 6 | `/register` (Đăng ký tài khoản) | 200 | 0 | 0 | `06_register.png` | **PASS** |
| 7 | `/profile` (Hồ sơ người dùng) | 200 | 0 | 0 | `07_profile.png` | **PASS** |
| 8 | `/admin/users` (Quản trị người dùng) | 200 | 0 | 0 | `08_admin_users.png` | **PASS** |
| 9 | `/dashboard/categories` (Dashboard Danh mục) | 200 | 0 | 0 | `09_dashboard_categories.png` | **PASS** |
| 10 | `/components-preview` (Xem trước Component) | 200 | 0 | 0 | `10_components_preview.png` | **PASS** |

**Tổng kết**: **10/10 trang hoàn toàn sạch lỗi console, không có lỗi mạng 4xx/5xx và không có ảnh hỏng**.

---

## 8. BẢNG TỔNG HỢP KIỂM THỬ HỒI QUY CUỐI CÙNG (TUẦN TỰ)

| Bước | Nhóm kiểm thử | Lệnh thực thi | Exit Code | Kết quả chi tiết | Đánh giá |
| :-: | :--- | :--- | :---: | :--- | :---: |
| **1** | Backend Build | `dotnet build` | **0** | 0 Errors, 2 SixLabors license notices | **PASS** |
| **2** | Backend UnitTests | `dotnet test tests/CulinaryBlog.UnitTests` | **0** | **254 Passed**, 0 Skipped, 0 Failed | **PASS** |
| **3** | Backend IntegrationTests | `dotnet test tests/CulinaryBlog.IntegrationTests` | **0** | **56 Passed**, 0 Skipped, 0 Failed | **PASS** |
| **4** | Frontend TypeScript | `npx tsc --noEmit` | **0** | 0 Type Errors | **PASS** |
| **5** | Frontend ESLint | `npm run lint` | **0** | **0 Errors, 0 Warnings** | **PASS** |
| **6** | Frontend Jest | `npm run test` | **0** | **6 Suites Passed, 78/78 Tests Passed** | **PASS** |
| **7** | Next.js Production Build | `npm run build` | **0** | **16/16 Pages Generated Successfully** | **PASS** |
| **8** | Playwright E2E Tests | `npx playwright test e2e/critical-flows.spec.ts` | **0** | **5/5 Tests Passed** (12.4s) | **PASS** |
| **9** | Live API Security Matrix | `node test_comprehensive_security.js` | **0** | **12/12 Scenarios Passed** | **PASS** |
| **10** | Live Browser Audit | `node test_full_pages_console_network.js` | **0** | **10/10 Pages 100% Clean** | **PASS** |

---

## 9. DANH SÁCH FILE ĐÃ THAY ĐỔI & TRẠNG THÁI GIT

Tất cả các thay đổi đều nằm an toàn trong working tree, **không commit/push/merge/reset**:

### Backend:
- `backend/tests/CulinaryBlog.UnitTests/RecipePersistenceIntegrationTests.cs`: Bổ sung `.IgnoreQueryFilters()` để hỗ trợ kiểm tra xóa mềm trên database test.
- `backend/src/CulinaryBlog.Application/CulinaryBlog.Application.csproj`: Nâng cấp ImageSharp 3.2.0.
- `backend/src/CulinaryBlog.Infrastructure/CulinaryBlog.Infrastructure.csproj`: Nâng cấp ImageSharp 3.2.0, MailKit 4.18.2, Pkcs 10.0.13.
- `backend/src/CulinaryBlog.Api/Endpoints/Recipes/RecipeEndpoints.cs`: Chuẩn hóa HTTP 422, 401, 403, 503 cho upload ảnh.
- `backend/src/CulinaryBlog.Api/Endpoints/Auth/AuthEndpoints.cs`: Minimal APIs thay thế MVC AuthController.
- `backend/src/CulinaryBlog.Api/Endpoints/Admin/`: Minimal APIs thay thế MVC AdminUsersController.

### Frontend:
- `frontend/culinary-blog-web/app/components-preview/page.tsx`: Chuyển StepEditor và ImageManager sang Controlled Mode với mock data, triệt tiêu 100% lỗi 404.
- `frontend/culinary-blog-web/components/admin/AdminUsersPage.tsx`: Chuyển sang `<Image unoptimized ... />` triệt tiêu cảnh báo ESLint.
- `frontend/culinary-blog-web/components/profile/ProfilePage.tsx`: Chuyển 3 thẻ `<img>` sang `<Image unoptimized ... />` kèm `onError` fallback.
- `frontend/culinary-blog-web/playwright.config.ts`: Cấu hình cổng độc lập 3001 và cờ `SKIP_WEBSERVER`.

---

## 10. KẾT LUẬN & ĐỀ NGHỊ BÀN GIAO

1. **Trạng thái hệ thống**:
   - Hệ thống **Culinary Blog – Web Nâng Cao – Nhóm 12** đã đạt tiêu chuẩn **ZERO KNOWN BUGS** đối với toàn bộ các tính năng hiện có thuộc phạm vi yêu cầu.
   - 100% các bài test (310 Backend tests, 78 Jest tests, 5 Playwright tests, 12 Security tests) đều đạt trạng thái **PASS**, không còn bài test nào bị Skip.
   - Frontend đạt trạng thái **0 Errors, 0 Warnings** tuyệt đối.
   - Trang `/components-preview` và toàn bộ 10 trang chính đều vận hành sạch lỗi Console và Network.
2. **Dịch vụ trực tuyến**:
   - Frontend đang chạy tại: `http://localhost:3001`
   - Backend API đang chạy tại: `http://localhost:5001`
   - Dự án LMS tại các cổng 3000 và 5000 được bảo vệ an toàn 100%.
3. **Sẵn sàng chuyển giao**: Hệ thống đã sẵn sàng 100% cho việc nghiệm thu cuối kỳ hoặc tiếp tục các giai đoạn phát triển tiếp theo theo kế hoạch của người dùng.

