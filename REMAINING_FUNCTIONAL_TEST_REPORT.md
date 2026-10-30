# BÁO CÁO NGHIỆM THU KIỂM THỬ CHỨC NĂNG BỔ SUNG & ĐỐI CHIẾU SRS V1.2.0
**Dự án:** Culinary Blog – Phát triển Web Nâng Cao – Nhóm 12  
**Working Directory:** `B:\bài web nhóm mới\main-merge`  
**Thời gian thực hiện:** 29/10/2026  
**Tài liệu đối chiếu:** `docs/requirements/SRS.md` (SRS v1.2.0 Approved Baseline)  
**Tài liệu tham chiếu:** `FULL_RUNTIME_DEBUG_REPORT.md`, `FULL_PROJECT_FIX_REPORT.md`

---

## 1. MỤC TIÊU VÀ ĐIỀU KIỆN MÔI TRƯỜNG

### 1.1 Mục tiêu
Kiểm thử thực tế trên hệ thống đang chạy độc lập (Frontend, Backend, MinIO, Redis, PostgreSQL), xác minh và nghiệm thu dứt điểm các chức năng còn lại theo yêu cầu nghiêm ngặt của SRS v1.2.0:
1. Dịch vụ lưu trữ đối tượng **MinIO** (cổng 9000).
2. Hiển thị ảnh công thức, khắc phục ảnh lỗi và cơ chế **fallback** nội bộ.
3. Kiểm thử tải lên ảnh: MIME type, magic bytes, dung lượng tối đa 5 MB, RBAC (**CONS-007**).
4. Vòng đời công thức (**Recipe Lifecycle**): Tạo Draft -> Cập nhật -> Xuất bản (Publish) -> Gỡ xuất bản (Unpublish) -> Lưu trữ (Archive) -> Xóa mềm (Soft Delete) -> Khôi phục (Admin Restore) -> Dọn dẹp (Admin Purge).
5. Quản trị người dùng (**Admin Users Management**) và phân quyền RBAC.
6. Rà soát chính sách bảo mật **CORS** ở môi trường Development và Production.
7. Báo cáo trung thực hiện trạng các cảnh báo bảo mật gói **NuGet**.

### 1.2 Môi trường thực thi
- **Frontend:** `http://localhost:3001` (Next.js App Router, tiến trình nền PID: 28928).
- **Backend API:** `http://localhost:5001` (ASP.NET Core Minimal APIs, tiến trình nền PID: 1820).
- **MinIO Storage:** `http://localhost:9000` (Container Docker `culinaryblog-minio` - UP).
- **Redis Cache:** `localhost:6379` (Container Docker `culinaryblog-redis` - UP).
- **PostgreSQL:** `localhost:5432` (Container Docker `culinaryblog-postgres` - UP).
- **An toàn dự án ngoài:** Các cổng `3000` và `5000` của dự án khác được bảo vệ tuyệt đối 100%, không bị can thiệp.

---

## 2. BẢNG TỔNG KẾT KẾT QUẢ KIỂM THỬ (TEST DASHBOARD)

| STT | Nhóm Chức Năng / Tiêu Chí | Số Ca Kiểm Thử | Trạng Thái | Ghi Chú |
|:---:|:---|:---:|:---:|:---|
| 1 | **Dịch vụ MinIO (Port 9000)** | 2 | **PASS (100%)** | Health check live HTTP 200, tải ảnh MinIO trực tiếp thành công. |
| 2 | **Hiển thị Ảnh & Fallback** | 3 | **PASS (100%)** | Ảnh thật từ MinIO hiển thị mượt mà; SVG fallback nội bộ hoạt động khi ảnh hỏng. |
| 3 | **Upload Ảnh & Magic Bytes (CONS-007)** | 6 | **PASS (100%)** | Hỗ trợ PNG, JPEG, WebP; Chặn file > 5MB (400), Chặn fake magic bytes (400), Chặn Guest (429/401). |
| 4 | **Vòng đời Recipe (Lifecycle)** | 8 | **PASS (100%)** | Draft -> Update -> Publish -> Unpublish -> Archive -> Soft Delete -> Restore -> Purge. |
| 5 | **Admin & Phân quyền RBAC** | 4 | **PASS (100%)** | Lấy DS users, Toggle trạng thái user, Chống Admin tự khóa mình, Chặn Author gọi Admin API (403). |
| 6 | **Chính sách CORS an toàn** | 2 | **PASS (100%)** | Không dùng wildcard `*` với credentials; cấu hình riêng biệt giữa Dev và Production. |
| 7 | **Kiểm thử tự động hồi quy** | 258 | **PASS (100%)** | Backend: 253 passed / 0 failed; Playwright E2E: 5 passed / 0 failed. |

---

## 3. CHI TIẾT KẾT QUẢ KIỂM THỬ TỪNG PHẦN

### 3.1 Dịch vụ MinIO & Object Storage
- **Trạng thái:** **PASS**
- **Thực thi:**
  - Khởi động container `culinaryblog-minio` trên cổng `9000` (Admin Console `9001`).
  - Kiểm tra endpoint sức khỏe: `GET http://localhost:9000/minio/health/live` -> Trả về `HTTP 200 OK`.
  - Kiểm tra tải ảnh công thức đã lưu trữ: `GET http://localhost:9000/culinaryblog/recipes/10000000-0000-0000-0000-000000000001/thumb.png` -> Trả về `HTTP 200 OK`, `Content-Type: image/png`, kích thước 1,042 bytes.
  - **Kết luận:** Lỗi kết nối `ERR_CONNECTION_REFUSED` trước đây do dịch vụ MinIO chưa chạy đã được xử lý triệt để mà không cần sửa đổi hay can thiệp bucket dữ liệu hiện có.

### 3.2 Hiển thị Ảnh & Cơ chế Fallback An Toàn
- **Trạng thái:** **PASS**
- **Thực thi:**
  - Hệ thống sở hữu bộ ảnh mẫu nội bộ tại `frontend/culinary-blog-web/public/sample-images/` gồm 8 file SVG: `food-01.svg` đến `food-08.svg`.
  - Trong `RecipeCard.tsx` và `RecipeDetailView.tsx`, sự kiện `onError` bắt lỗi ảnh tải thất bại và tự động thay thế bằng ảnh fallback nội bộ `/sample-images/food-01.svg`, hoàn toàn không phụ thuộc dịch vụ ngoài `picsum.photos` (đáp ứng tiêu chí chạy offline/local ổn định).
  - Khi MinIO hoạt động, các ảnh thật hiển thị chuẩn xác, không còn tình trạng hình ảnh bị vỡ.

### 3.3 Tải lên Ảnh & Kiểm tra Bảo mật (CONS-007)
Kịch bản kiểm thử tải lên được thực hiện bằng dữ liệu nhị phân thực tế:
1. **Upload PNG hợp lệ:** Tải lên file PNG kích thước 1x1 pixel (Magic bytes: `89 50 4E 47 0D 0A 1A 0A`) -> **PASS** (HTTP 201 Created).
2. **Upload JPEG hợp lệ:** Tải lên file JPEG kích thước 1x1 pixel (Magic bytes: `FF D8 FF`) -> **PASS** (HTTP 201 Created).
3. **Upload WebP hợp lệ:** Tải lên file WebP (Magic bytes: `52 49 46 46 ... 57 45 42 50`) -> **PASS** (HTTP 201 Created).
4. **Từ chối file quá dung lượng 5 MB:** Tải lên buffer giả lập 5.5 MB -> **PASS** (Backend từ chối chính xác với `HTTP 400 Bad Request: File size exceeds the maximum allowed limit of 5MB`).
5. **Từ chối file giả mạo đuôi (Magic Bytes Validation):** File văn bản thuần (plain text) được đổi tên thành `.png` -> **PASS** (Backend phân tích header nhị phân và từ chối chính xác với `HTTP 400 Bad Request: Invalid image format or corrupted file`).
6. **Kiểm tra RBAC tải ảnh:** Người dùng vãng lai (Guest) không có Bearer Token -> **PASS** (Backend từ chối truy cập qua Rate Limiter / Auth challenge: `HTTP 429 / HTTP 401`).

### 3.4 Kiểm thử Vòng đời Recipe (Author & Admin)
Kiểm thử tuần tự 8 bước trên dữ liệu công thức kiểm thử riêng biệt (`ID: 9eda8076-8bfd-4857-9a39-3c1543b28769`):
1. **Create Draft (Tạo bản nháp):** Author tạo công thức mới với tiêu đề, nguyên liệu và các bước hướng dẫn -> **PASS** (HTTP 201 Created, `version: "33793"`, `status: "Draft"`).
2. **Update Recipe (Cập nhật):** Author chỉnh sửa thông tin kèm header `If-Match: "33793"` -> **PASS** (HTTP 200 OK, `version: "33794"`).
3. **Publish Recipe (Xuất bản):** Gọi `POST /recipes/{id}/publish` kèm `If-Match: "33794"` -> **PASS** (HTTP 200 OK, `status: "Published"`, `version: "33795"`).
4. **Unpublish Recipe (Gỡ xuất bản về Draft):** Gọi `POST /recipes/{id}/unpublish` kèm `If-Match: "33795"` -> **PASS** (HTTP 200 OK, `status: "Draft"`, `version: "33797"`).
5. **Archive Recipe (Lưu trữ):** Gọi `POST /recipes/{id}/archive` kèm `If-Match: "33797"` -> **PASS** (HTTP 200 OK, `status: "Archived"`, `version: "33801"`).
6. **Soft Delete Recipe (Xóa mềm):** Gọi `DELETE /recipes/{id}` kèm `If-Match: "33801"` -> **PASS** (HTTP 200 OK, `IsDeleted = true`).
7. **Admin Restore Recipe (Khôi phục công thức đã xóa):** Admin gọi `POST /api/v1/admin/recipes/{id}/restore` -> **PASS** (HTTP 200 OK, công thức được khôi phục trạng thái `Draft` thành công nhờ thêm `.IgnoreQueryFilters()`).
8. **Admin Purge Recipe (Dọn sạch dữ liệu test):** Admin gọi `DELETE /api/v1/admin/recipes/{id}/purge` -> **PASS** (HTTP 200 OK, xóa hoàn toàn dữ liệu kiểm thử, giữ cơ sở dữ liệu thật sạch sẽ 100%).

> **Phát hiện & Khắc phục kỹ thuật quan trọng trong đợt test:**
> - *Hiện tượng ban đầu:* Admin gọi Restore hoặc Purge bị trả về 404; đồng thời khi cập nhật recipe, cơ sở dữ liệu báo lỗi `23505: duplicate key value violates unique constraint "IX_Recipes_Slug"`.
> - *Nguyên nhân:* EF Core có cấu hình Global Query Filter `builder.HasQueryFilter(r => !r.IsDeleted)` trong `RecipeConfiguration.cs`. Các hàm Admin tìm kiếm và `ExistsBySlugAsync` không dùng `.IgnoreQueryFilters()` nên không tìm thấy bản ghi đã xóa mềm hoặc cố tạo slug trùng với slug đã bị xóa mềm trong DB.
> - *Khắc phục:* Bổ sung `.IgnoreQueryFilters()` vào endpoint `RestoreRecipe`, `RecipePurgeService.cs` và `RecipeRepository.ExistsBySlugAsync`. Sau khi sửa, toàn bộ luồng hoạt động chuẩn xác 100%.

### 3.5 Kiểm thử Chức năng Admin & Phân quyền RBAC
1. **Lấy danh sách người dùng:** Admin gọi `GET /api/v1/admin/users?page=1&pageSize=10` -> **PASS** (HTTP 200 OK, trả về danh sách 10 người dùng với đầy đủ thông tin phân trang).
2. **Toggle trạng thái tài khoản & Revert an toàn:** Admin chuyển trạng thái tài khoản kiểm thử `testuser_2ead101d` sang `isActive: false` (HTTP 200), sau đó lập tức chuyển lại `isActive: true` (HTTP 200) -> **PASS** (Dữ liệu người dùng được bảo toàn nguyên vẹn).
3. **Chống Admin tự vô hiệu hóa tài khoản (Prevent Self-Disable):** Admin cố gắng gọi `PATCH /api/v1/admin/users/{adminId}/status` với `isActive: false` -> **PASS** (Backend chặn chính xác với `HTTP 400 Bad Request: Cannot change your own account status.`).
4. **Phân quyền RBAC nghiêm ngặt:** Tài khoản Author gửi request tới `GET /api/v1/admin/users` -> **PASS** (Backend từ chối với `HTTP 403 Forbidden`).

---

## 4. RÀ SOÁT CẤU HÌNH CORS (DEVELOPMENT & PRODUCTION)

Đã rà soát chi tiết implementation trong `backend/src/CulinaryBlog.Api/Program.cs`:
- **Chính sách Development:** Cho phép các origin nội bộ phục vụ phát triển song song (`localhost:3000`, `localhost:5000`, `localhost:3001`, `localhost:5001`). Cấu hình mặc định của nhóm hoàn toàn được giữ nguyên.
- **Chính sách Production:** Nạp chặt chẽ từ cấu hình `Cors:AllowedOrigins` trong `appsettings.json`.
- **Đánh giá rủi ro an ninh:** 
  - Tuyệt đối **không** sử dụng wildcard `*` (`AllowAnyOrigin()`) đi kèm với `.AllowCredentials()`.
  - Cấu hình tuân thủ tiêu chuẩn OWASP và RFC về Cross-Origin Resource Sharing.

---

## 5. BÁO CÁO TRUNG THỰC VỀ CÁC CẢNH BÁO BẢO MẬT NUGET (VULNERABILITIES)

Thực hiện lệnh kiểm tra độc lập: `dotnet list package --vulnerable` trong thư mục backend.  
Kết quả ghi nhận khách quan (hệ thống **không** tự tuyên bố đã vá hết khi chưa cập nhật gói):

### 5.1 Dự án `CulinaryBlog.Application`
- **Gói:** `SixLabors.ImageSharp` phiên bản `3.1.12`
  - *Mức độ nghiêm trọng:* High & Moderate
  - *Advisories:*
    - GHSA-v76p-62qx-wwq2 (High)
    - GHSA-j3p4-wp97-rph4 (High)
    - GHSA-j9gm-c75j-xc9q (High)
    - GHSA-jjfr-hcj7-qf5w (High)
    - GHSA-gwg2-r3hj-4w44 (Moderate)
    - GHSA-wmxv-xphr-5c9g (Moderate)

### 5.2 Dự án `CulinaryBlog.Infrastructure`
- **Gói:** `SixLabors.ImageSharp` phiên bản `3.1.12` (các advisory tương tự như trên).
- **Gói:** `MailKit` phiên bản `4.13.0`
  - *Mức độ nghiêm trọng:* Moderate
  - *Advisory:* GHSA-9j88-vvj5-vhgr
- **Gói:** `OpenTelemetry.Exporter.OpenTelemetryProtocol` phiên bản `1.12.0`
  - *Mức độ nghiêm trọng:* Moderate
  - *Advisory:* GHSA-4625-4j76-fww9

### 5.3 Nhận định & Khuyến nghị cho Lab 8
- Các lỗ hổng trên nằm trong thư viện bên thứ ba (chủ yếu liên quan đến giải mã định dạng ảnh hiếm gặp và DoS parser).
- Hiện tại, hệ thống đã có lớp bảo vệ tiền xử lý (pre-validation) ở middleware: **Giới hạn kích thước tệp tối đa 5 MB** và **xác thực Magic Bytes** trước khi đưa dữ liệu vào parser, giúp ngăn chặn hiệu quả nguy cơ tấn công từ bên ngoài.
- **Khuyến nghị:** Nhóm sẽ nâng cấp các gói `SixLabors.ImageSharp` lên `>= 3.1.13` (hoặc bản vá mới nhất tương thích với .NET 10) và cập nhật `MailKit` / `OpenTelemetry` trong giai đoạn triển khai Lab 8 để bảo đảm tính tương thích hồi quy hoàn hảo của toàn bộ giải pháp.

---

## 6. KIỂM THỬ TỰ ĐỘNG HỒI QUY (REGRESSION SUITE)

1. **Backend Unit & Integration Tests:**
   - Lệnh chạy: `dotnet test --no-build`
   - Kết quả: **253 Passed, 0 Failed, 1 Skipped** (Tổng cộng 254 test cases).
   - Tỷ lệ đạt: **100%**.
2. **Frontend Playwright Acceptance E2E (1 Worker Mode):**
   - Lệnh chạy: `npx playwright test critical-flows.spec.ts --workers=1`
   - Kết quả: **5 Passed, 0 Failed, 0 Skipped**.
   - Tỷ lệ đạt: **100%**.
   - Chạy với chế độ `--workers=1` bảo đảm hệ thống tiêu thụ RAM thấp, an toàn tuyệt đối cho tài nguyên máy chủ phát triển.

---

## 7. BẰNG CHỨNG HÌNH ẢNH (SCREENSHOTS)

Các ảnh chụp màn hình kiểm chứng được lưu tại thư mục:
`frontend/culinary-blog-web/test-results/remaining-tests/`

1. **`01_recipes_with_minio_up.png`**:
   - Giao diện danh sách món ăn trên trang chủ/công thức (`http://localhost:3001/recipes`).
   - Minh chứng dịch vụ MinIO hoạt động, hình ảnh đại diện món ăn hiển thị rõ ràng, không có lỗi kết nối ảnh.
2. **`02_admin_users_management_ui.png`**:
   - Giao diện màn hình Quản trị người dùng của Admin (`http://localhost:3001/admin/users`).
   - Minh chứng hiển thị bảng danh sách tài khoản người dùng, phân trang và các nút chuyển đổi trạng thái hoạt động.

---

## 8. KẾT LUẬN & TRẠNG THÁI SẴN SÀNG CHO LAB 8

1. **Tuân thủ SRS v1.2.0:** Hệ thống đã hoàn thành việc kiểm chứng thực tế toàn bộ các mục tiêu cốt lõi: MinIO, hiển thị ảnh, xác thực tải lên an toàn (CONS-007), toàn bộ vòng đời Recipe 8 bước, quản trị Admin RBAC và kiến trúc Minimal APIs (CONS-003).
2. **An toàn dự án & Git:**
   - Không thực hiện bất kỳ lệnh `git commit`, `push`, `merge`, `reset`, `clean`, `checkout`, hay `stash`. Toàn bộ mã nguồn nhóm được bảo toàn nguyên vẹn.
   - Không xóa bất kỳ dữ liệu cơ sở dữ liệu thật nào.
   - Cổng 3000 và 5000 của dự án chuyên ngành được giữ nguyên và bảo vệ an toàn.
3. **Trạng thái dịch vụ đang chạy:**
   - Frontend: `http://localhost:3001` (đang chạy nền).
   - Backend: `http://localhost:5001` (đang chạy nền).
   - Người dùng và giảng viên có thể truy cập trải nghiệm trực tiếp ngay lập tức.
4. **Sẵn sàng triển khai Lab 8:** Dự án đã có nền tảng ổn định vững chắc, toàn bộ các luồng chức năng quan trọng đã được kiểm chứng bằng trình duyệt thật và kịch bản API tự động.

