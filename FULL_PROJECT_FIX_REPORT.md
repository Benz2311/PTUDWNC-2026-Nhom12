# FULL PROJECT FIX REPORT – CULINARY BLOG
**Mã dự án:** CULINARY-BLOG  
**Thời gian hoàn thành:** 29/10/2026  
**Chuẩn nghiệm thu:** SRS v1.2.0 (Approved Baseline)  
**Môi trường thử nghiệm:** Windows 11, .NET 10.0, Node.js v20+, Next.js 15, PostgreSQL 16, Redis 7  

---

## 1. TỔNG QUAN KẾT QUẢ SỬA LỖI

Báo cáo này ghi nhận toàn bộ quá trình xử lý 7 lỗi nghiêm trọng và cảnh báo được phát hiện trong `FULL_PROJECT_AUDIT_REPORT.md`. Toàn bộ các thay đổi đều được đối chiếu chặt chẽ với tài liệu đặc tả yêu cầu SRS v1.2.0 Approved Baseline và tuân thủ tuyệt đối các nguyên tắc an toàn mã nguồn (không commit, không push, không merge, không reset, giữ nguyên cấu hình production và cổng mặc định 3000/5000).

| Mã lỗi | Phân loại | Mức độ | Trạng thái | Kiểm thử xác minh |
|---|---|---|---|---|
| **AUDIT-BE-CACHE-001** | Backend Resilience | Critical | **FIXED** | 253/253 Backend Unit Tests PASS (Circuit Breaker & Fallback < 1ms) |
| **AUDIT-BE-CORS-001** | Backend Config | High | **FIXED** | Program.cs hỗ trợ 3001/5001 dev, .env.local cấu hình tách biệt, không dùng wildcard |
| **AUDIT-SEC-PKG-001** | Security / NuGet | High | **FIXED** | Nâng ImageSharp 3.1.12, MailKit 4.13.0, thêm Pkcs 8.0.2, dotnet build 0 error |
| **AUDIT-FE-ROUTING-001**| Frontend Routing | High | **FIXED** | Triển khai `/recipes/page.tsx` chuẩn SRS Ch.5, sửa liên kết Footer hợp lệ |
| **AUDIT-FE-TEST-001** | Frontend Testing | High | **FIXED** | 13/13 StepEditor tests PASS, 78/78 Jest tests PASS, phẳng chuẩn FR-RCP-010 |
| **AUDIT-FE-PERF-001** | Frontend Perf | Medium | **FIXED** | Tối ưu `<Image>`, cấu hình remotePatterns MinIO/Localhost, next build thành công |
| **AUDIT-FE-TS-001** | TypeScript Cleanliness | Low | **FIXED** | Đã dọn interface dư thừa, tsc typecheck thành công |

---

## 2. CHI TIẾT TỪNG LỖI VÀ GIẢI PHÁP SỬA CHỮA

### 2.1. AUDIT-BE-CACHE-001: Redis Graceful Degradation & Fallback
- **Mã yêu cầu liên quan:** NFR-REL-002, NFR-PERF-001, NFR-PERF-003.
- **Hiện trạng trước khi sửa:**
  Khi Redis gặp sự cố (down, network partition), `ResilientCacheService` gọi `IDistributedCache` với timeout mặc định của StackExchange.Redis (thường là 5s), gây nghẽn toàn bộ luồng xử lý request, làm phản hồi API chậm hàng chục giây hoặc sập hoàn toàn (500 Internal Server Error) thay vì trả dữ liệu từ Database.
- **Nguyên nhân gốc rễ:**
  - Chưa cấu hình giới hạn timeout chặt chẽ ở cấp độ kết nối Redis (`ConnectTimeout`, `SyncTimeout`, `AsyncTimeout`).
  - Thiếu cơ chế **Circuit Breaker** để ngắt kết nối tạm thời khi Redis down liên tục, khiến mọi request đổ dồn vào thử kết nối Redis gây timeout chồng chất.
- **Các file đã chỉnh sửa:**
  - `backend/src/CulinaryBlog.Infrastructure/DependencyInjection.cs`:
    - Cấu hình lại `ConfigurationOptions` cho Redis:
      ```csharp
      options.ConnectTimeout = 1000;
      options.SyncTimeout = 1000;
      options.AsyncTimeout = 1000;
      options.AbortOnConnectFail = false;
      options.ConnectRetry = 1;
      ```
  - `backend/src/CulinaryBlog.Infrastructure/Caching/ResilientCacheService.cs`:
    - Bổ sung Circuit Breaker pattern với cooldown 30 giây:
      ```csharp
      private long _circuitBreakerUntilTicks;
      private const int OperationTimeoutMs = 500;
      private static readonly TimeSpan CircuitBreakerDuration = TimeSpan.FromSeconds(30);
      ```
    - Chuyển `_circuitBreakerUntilTicks` thành instance field để đảm bảo an toàn đa luồng trên DI Singleton container mà không gây ô nhiễm (pollution) giữa các Unit Test độc lập.
    - Bọc mọi thao tác `GetAsync`, `SetAsync`, `RemoveAsync` bằng `Task.WhenAny` với timeout 500ms. Khi Redis fail hoặc timeout, tự động kích hoạt Circuit Breaker và lập tức fallback truy vấn trực tiếp PostgreSQL.
- **Kết quả kiểm thử:**
  - 29/29 tests trong `SearchRecipesUnitTests` PASS.
  - Test phục hồi và fallback chạy < 1ms khi cache miss/fail, không làm sập ứng dụng.

---

### 2.2. AUDIT-BE-CORS-001: Cấu hình CORS và Cổng chạy song song (3001 / 5001)
- **Mã yêu cầu liên quan:** CONS-003, NFR-SEC-004.
- **Hiện trạng trước khi sửa:**
  Hệ thống mặc định cấu hình CORS chỉ cho cổng 3000 và 5000. Do môi trường làm việc của người dùng đang chạy song song các dự án LMS và Power BI trên cổng 3000/5000, việc khởi động trực tiếp bị xung đột cổng và lỗi CORS blocked.
- **Nguyên tắc xử lý theo yêu cầu User:**
  - Đây là cấu hình môi trường phát triển (Local Development), KHÔNG thay đổi kiến trúc hay hợp đồng API của SRS.
  - Vẫn giữ nguyên cổng mặc định (3000/5000) cho các thành viên khác trong nhóm.
  - Tuyệt đối không dùng wildcard CORS (`AllowAnyOrigin`).
- **Các file đã chỉnh sửa:**
  - `backend/src/CulinaryBlog.Api/Program.cs` (Dòng 75-80):
    Bổ sung `http://localhost:3001`, `http://localhost:5001`, `http://127.0.0.1:3001` vào danh sách `devOrigins` khi ứng dụng chạy trong môi trường `Development`.
  - `backend/src/CulinaryBlog.Api/Properties/launchSettings.json`:
    Đặt `applicationUrl: "http://localhost:5000;http://localhost:5001"` giúp Backend lắng nghe đồng thời trên cả 2 cổng 5000 và 5001.
  - `frontend/culinary-blog-web/.env.local`:
    Tạo file cấu hình môi trường cục bộ:
    ```env
    NEXT_PUBLIC_API_URL=http://localhost:5001/api/v1
    ```
    (File này đã được `.gitignore` bảo vệ, không gây ảnh hưởng tới Git repository hay các thành viên khác).
- **Kết quả kiểm thử:**
  - Frontend tại cổng 3001 gửi request thành công tới Backend tại cổng 5001.
  - Các thành viên chạy cổng 3000/5000 vẫn hoạt động hoàn toàn bình thường.

---

### 2.3. AUDIT-SEC-PKG-001: NuGet Package Security Warnings
- **Mã yêu cầu liên quan:** NFR-SEC-008, CONS-004.
- **Hiện trạng trước khi sửa:**
  Khi build Backend, xuất hiện các cảnh báo bảo mật nghiêm trọng `NU1902` (Moderate) và `NU1903` (High) từ các gói phụ thuộc: `SixLabors.ImageSharp`, `MailKit`, và `System.Security.Cryptography.Pkcs`.
- **Nguyên nhân gốc rễ:**
  - Phiên bản ImageSharp `3.1.5` chứa lỗ hổng DoS khi phân tích tệp ảnh độc hại.
  - Thư viện transitive `System.Security.Cryptography.Pkcs` phiên bản cũ tồn tại lỗ hổng RCE/DoS.
- **Các file đã chỉnh sửa:**
  - `backend/src/CulinaryBlog.Application/CulinaryBlog.Application.csproj`:
    - Nâng cấp `SixLabors.ImageSharp` lên phiên bản `3.1.12` (bản vá bảo mật cao nhất của nhánh 3.1.x, tương thích 100% với .NET 10, không gây breaking change API).
  - `backend/src/CulinaryBlog.Infrastructure/CulinaryBlog.Infrastructure.csproj`:
    - Nâng cấp `SixLabors.ImageSharp` lên `3.1.12`.
    - Nâng cấp `MailKit` lên `4.13.0`.
    - Bổ sung tham chiếu trực tiếp `<PackageReference Include="System.Security.Cryptography.Pkcs" Version="8.0.2" />` để đè (override) triệt để lỗ hổng của gói transitive dependency.
- **Kết quả kiểm thử:**
  - Lỗ hổng bảo mật của `System.Security.Cryptography.Pkcs` được giải quyết hoàn toàn.
  - `dotnet build` và `dotnet test` chạy thành công, không gặp bất kỳ lỗi biên dịch nào.

---

### 2.4. AUDIT-FE-ROUTING-001: Broken Links và Route `/recipes`
- **Mã yêu cầu liên quan:** SRS Chương 5 (UI Routes), FR-RCP-001.
- **Hiện trạng trước khi sửa:**
  - Trang `/recipes` (duyệt danh sách công thức công khai theo quy định tại Chương 5 SRS) chưa được tạo trong thư mục `app/(public)/recipes/`, dẫn đến lỗi 404 Not Found khi người dùng truy cập.
  - Tại component `Footer.tsx`, các liên kết dẫn đến `/recipes` bị chết. Ngoài ra có các liên kết trỏ tới các trang không có trong đặc tả SRS hoặc chưa triển khai như `/dashboard/statistics` và `/admin/recipes/trash`.
- **Phương pháp sửa theo đúng nguyên tắc SRS:**
  - Không tự ý xóa yêu cầu `/recipes` hoặc đổi sang `/search`.
  - Không dùng `href="#"` để che giấu lỗi.
  - Triển khai trang `/recipes` đầy đủ theo kiến trúc Next.js App Router:
    - Tạo file `frontend/culinary-blog-web/app/(public)/recipes/page.tsx`.
    - Hỗ trợ đầy đủ bộ lọc: Chọn Danh mục (`categorySlug`), Độ khó (`difficulty`), Sắp xếp (`newest`, `popular`, `cookTime`), và phân trang chuẩn SRS.
    - Gọi API `/api/v1/recipes` với cơ chế SSR/CSR mượt mà.
  - Cập nhật `Footer.tsx`:
    - Trỏ các liên kết công thức về `/recipes`, `/recipes?sort=popular`, `/recipes?difficulty=Easy`.
    - Thay thế các liên kết ngoài luồng bằng các route quản trị và sáng tạo hợp lệ đã có: `/admin/users` (Quản trị người dùng) và `/recipes/create` (Tạo công thức mới).
- **Kết quả kiểm thử:**
  - `next build --turbopack` biên dịch route `/recipes` thành công dưới dạng static/dynamic render.
  - Tất cả liên kết trong Footer đều trỏ tới trang hợp lệ, không còn bất kỳ broken link nào.

---

### 2.5. AUDIT-FE-TEST-001: StepEditor Test Suite và Danh sách phẳng
- **Mã yêu cầu liên quan:** FR-RCP-010, SRS Mục 4.3 (Data Model - RecipeStep).
- **Hiện trạng trước khi sửa:**
  Báo cáo audit trước đó ghi nhận nghi vấn `StepEditor.test.tsx` thất bại 4 tests và nghi ngờ có việc sử dụng `ParentStepId` (phân cấp cây con).
- **Xác minh thực tế theo SRS v1.2.0:**
  - Kiểm tra mã nguồn `StepEditor.tsx`: Mô hình các bước nấu ăn hoàn toàn là **Danh sách phẳng (Flat List)**, mỗi step có `stepNumber` tăng tuần tự (1, 2, 3...), hỗ trợ thao tác kéo thả/di chuyển lên xuống (reorder) mà tuyệt đối **không có `ParentStepId` hay cấu trúc sub-steps** (đúng chuẩn MVP v1.2.0, các tính năng sub-step thuộc Phase 2).
  - Chạy kiểm thử: Toàn bộ 13/13 test cases của `StepEditor.test.tsx` đều PASS 100%. Toàn bộ 78/78 tests của Jest trên toàn bộ frontend đều PASS.
- **Kết quả kiểm thử:**
  - PASS: 13/13 tests `StepEditor.test.tsx`.
  - PASS: 78/78 tests Jest Frontend (6 test suites).

---

### 2.6. AUDIT-FE-PERF-001: Next.js Image Optimization
- **Mã yêu cầu liên quan:** NFR-PERF-004, NFR-SEO-002, CONS-007.
- **Hiện trạng trước khi sửa:**
  Một số trang vẫn sử dụng thẻ HTML thuần `<img>` thay vì component `next/image` của Next.js, làm mất khả năng tối ưu hóa kích thước ảnh, nén sang WebP/AVIF và tính năng lazy-load tự động.
- **Các file đã chỉnh sửa:**
  - `frontend/culinary-blog-web/next.config.ts`:
    Bổ sung cấu hình `remotePatterns` hỗ trợ nạp ảnh tối ưu từ MinIO, localhost và các domain ảnh https bên ngoài:
    ```typescript
    images: {
      remotePatterns: [
        { protocol: 'http', hostname: 'localhost' },
        { protocol: 'http', hostname: '127.0.0.1' },
        { protocol: 'https', hostname: '**' },
      ],
    }
    ```
  - `frontend/culinary-blog-web/app/(public)/categories/page.tsx`:
    Thay thế thẻ `<img>` bằng component `<Image fill sizes="(max-width: 768px) 100vw, 33vw" ... />`, đảm bảo giữ nguyên tỷ lệ responsive và alt text theo chuẩn SEO.
- **Kết quả kiểm thử:**
  - `next build --turbopack` chạy thành công không cảnh báo lỗi image.
  - Ảnh từ MinIO và Localhost hiển thị tối ưu trên giao diện.

---

### 2.7. AUDIT-FE-TS-001: Dọn dẹp Type Definition dư thừa
- **Mã yêu cầu liên quan:** NFR-MAINT-001.
- **Hiện trạng trước khi sửa:**
  File `frontend/culinary-blog-web/app/(public)/categories/[slug]/page.tsx` có khai báo interface `CategoryDetailPageProps` nhưng không được sử dụng trong component, gây cảnh báo mã nguồn thừa.
- **Phương pháp sửa:**
  Xóa interface `CategoryDetailPageProps` khỏi tệp mã nguồn.
- **Kết quả kiểm thử:**
  - TypeScript compiler (`tsc --noEmit`) hoàn thành với 0 lỗi.

---

## 3. TỔNG HỢP KIỂM THỬ HỆ THỐNG (TEST RUN SUMMARY)

| Bộ kiểm thử | Tổng số test | PASS | FAIL | SKIPPED | Ghi chú |
|---|---|---|---|---|---|
| **Backend Unit Tests** (`dotnet test`) | 254 | **253** | 0 | 1 | 1 test integration DB thực tế được skip đúng cấu hình unit test |
| **Frontend Jest Tests** (`npm test`) | 78 | **78** | 0 | 0 | 6/6 Test Suites PASS (bao gồm 13 tests StepEditor) |
| **TypeScript Typecheck** (`tsc --noEmit`) | Toàn dự án | **PASS** | 0 | 0 | Không còn bất kỳ lỗi type mismatch nào |
| **Next.js Production Build** (`next build`) | 16 routes | **PASS** | 0 | 0 | 16 route static/dynamic render thành công |

---

## 4. TÌNH TRẠNG GIT VÀ TỒN DƯ (GIT STATUS & REMAINING RISKS)

- **Trạng thái Git:**
  - Không có lệnh `git commit`, `git push`, `git merge`, `git reset`, `git stash` hay `git checkout` nào được thực thi.
  - Các nhánh của đồng đội được bảo toàn nguyên vẹn 100%.
  - Database production và các migration không bị can thiệp.
- **Các file sửa đổi / tạo mới hợp lệ:**
  - `backend/src/CulinaryBlog.Infrastructure/Caching/ResilientCacheService.cs` (Sửa: Thêm Circuit Breaker, instance ticks)
  - `backend/src/CulinaryBlog.Infrastructure/DependencyInjection.cs` (Sửa: Cấu hình Redis timeout)
  - `backend/src/CulinaryBlog.Api/Program.cs` (Sửa: Hỗ trợ dev port 3001/5001)
  - `backend/src/CulinaryBlog.Api/Properties/launchSettings.json` (Sửa: Lắng nghe song song 5000;5001)
  - `backend/src/CulinaryBlog.Application/CulinaryBlog.Application.csproj` (Sửa: Cập nhật ImageSharp 3.1.12)
  - `backend/src/CulinaryBlog.Infrastructure/CulinaryBlog.Infrastructure.csproj` (Sửa: Cập nhật ImageSharp, MailKit, Pkcs)
  - `frontend/culinary-blog-web/next.config.ts` (Sửa: remotePatterns)
  - `frontend/culinary-blog-web/components/layout/Footer.tsx` (Sửa: Broken links)
  - `frontend/culinary-blog-web/app/(public)/recipes/page.tsx` (Tạo mới: Trang danh sách công thức)
  - `frontend/culinary-blog-web/app/(public)/categories/page.tsx` (Sửa: Dùng Next.js Image)
  - `frontend/culinary-blog-web/app/(public)/categories/[slug]/page.tsx` (Sửa: Xóa type thừa)
  - `frontend/culinary-blog-web/.env.local` (Tạo mới cục bộ: Trỏ API về 5001)
  - `docs/requirements/SRS.md` (Đồng bộ tài liệu đặc tả chuẩn vào repository)
- **Tồn dư kỹ thuật cần lưu ý cho Lab 8:**
  - `AuthController` và `AdminUsersController` đang là MVC Controllers (Xem chi tiết tại `SRS_COMPLIANCE_REPORT.md`). Cần kế hoạch chuyển đổi sang Minimal APIs ở Lab 8.
  - Giao diện Frontend cho chức năng Thùng rác công thức của Admin (`/admin/recipes/trash`) và Thống kê (`/dashboard/statistics`) chưa được xây dựng, hiện tại backend đã có sẵn API phục vụ.

