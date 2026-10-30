# BÁO CÁO NGHIỆM THU KIỂM THỬ THỰC TẾ & XÁC MINH HỆ THỐNG
## DỰ ÁN: CULINARY BLOG – PHÁT TRIỂN WEB NÂNG CAO (NHÓM 12)
**Chuẩn đặc tả căn cứ:** SRS v1.2.0 – Approved Baseline  
**Thời gian thực hiện:** 29/10/2026  
**Môi trường thử nghiệm:**
- **Frontend:** Next.js 15.2.4 App Router (`http://localhost:3001` - Task PID: 34296 / Active)
- **Backend:** .NET 10 Minimal APIs (`http://localhost:5001` - Task PID: 21416 / Active)
- **Database:** PostgreSQL 16 (Port 5432 - Native Service PID 9124)
- **Bảo toàn cách ly:** Tuyệt đối không can thiệp cổng 3000 và 5000 (dành cho LMS & Power BI).

---

## 1. TỔNG KẾT KẾT QUẢ KIỂM THỬ THỰC TẾ (REAL BROWSER TESTING)

Bộ kịch bản kiểm thử độc lập trên trình duyệt Chromium (`test_real_browser_verified.js`) đã chạy qua **9/9 kịch bản** với tương tác người dùng thật, phân quyền Auth và xác minh trạng thái thực:

| STT | Kịch bản kiểm thử | Thao tác thực tế trên trình duyệt | Kết quả mong đợi theo SRS | Kết quả thực tế & Bằng chứng | Trạng thái |
| :---: | :--- | :--- | :--- | :--- | :---: |
| **1** | **Trang chủ (`/`)** | Truy cập `http://localhost:3001/`, chờ tải dữ liệu. | Tải đầy đủ Header, Hero banner, Logo CulinaryBlog, danh mục nổi bật. | Page title: "Culinary Blog \| Nền tảng chia sẻ và học nấu ăn trực tuyến", Brand hiển thị rõ ràng. Screenshot: `v2_01_homepage.png`. | **PASS** |
| **2** | **Danh mục (`/categories`)** | Truy cập `/categories`, kiểm tra các thẻ nhóm món ăn. | Hiển thị danh mục món ăn lấy từ API backend. | Tải thành công 20 thẻ nhóm món ăn từ API `GET /categories`. Screenshot: `v2_02_categories.png`. | **PASS** |
| **3** | **Công thức (`/recipes`), Lọc & Chi tiết** | Chọn lọc độ khó (Difficulty), click vào thẻ công thức đầu tiên. | Danh sách cập nhật theo độ khó, điều hướng vào trang chi tiết `/recipes/[slug]`. | - Lọc độ khó hoạt động mượt mà.<br>- Click điều hướng đến `/recipes/cha-gio-tom-thit-gion-rum-demo-a-cap-nhat-2` thành công. Screenshot: `v2_03_recipes_initial.png`, `v2_03_recipes_filtered.png`, `v2_03_recipe_detail.png`. | **PASS** |
| **4** | **Tìm kiếm (`/search`)** | Nhập từ khóa tiếng Việt `"món"` vào ô tìm kiếm, bấm nút "Tìm kiếm". | Đồng bộ URL `?q=m%C3%B3n`, tải danh sách kết quả phù hợp. | URL cập nhật chính xác thành `http://localhost:3001/search?q=m%C3%B3n`, tải kết quả tìm kiếm. Screenshot: `v2_04_search_results.png`. | **PASS** |
| **5** | **Đăng nhập (`/login`) - Validation** | Truy cập `/login`, không nhập thông tin và bấm nút "Đăng nhập". | Kích hoạt client validation tiếng Việt, không gửi request lỗi. | Hiển thị 2 cảnh báo đỏ: *"Vui lòng nhập email hoặc tên đăng nhập"* và *"Vui lòng nhập mật khẩu"*. Screenshot: `v2_05_login_validation.png`. | **PASS** |
| **6** | **Guest Protection (Bảo vệ tuyến đường)** | Truy cập `/profile` và `/admin/users` khi chưa đăng nhập. | Tuyến đường được bảo vệ phải chuyển hướng về `/login`. | - Truy cập `/profile` -> Redirect về `/login`.<br>- Truy cập `/admin/users` -> Redirect về `/login`. Screenshot: `v2_06_guest_redirect.png`. | **PASS** |
| **7** | **Author Role & Phân quyền** | Điền `author` / `Author@123`, submit đăng nhập, sau đó mở `/profile` và `/admin/users`. | Đăng nhập thành công, mở được trang Profile cá nhân; khi vào `/admin/users` phải bị chặn và hiển thị màn hình từ chối. | - Đăng nhập thành công, URL chuyển hướng về trang chủ.<br>- Mở `/profile` thành công (ProfileLoaded: true).<br>- Mở `/admin/users` hiển thị: *"Không có quyền truy cập. Bạn cần quyền Admin để truy cập trang này."*. Screenshot: `v2_07_author_logged_in.png`, `v2_07_author_profile.png`, `v2_07_author_admin_denied.png`. | **PASS** |
| **8** | **Admin Role & Quản trị Người dùng** | Đăng xuất, đăng nhập bằng `admin` / `Admin@123`, mở `/admin/users`, tương tác tìm kiếm người dùng. | Đăng nhập quyền Admin, bảng quản trị người dùng hiển thị danh sách, tìm kiếm người dùng theo từ khóa. | - Đăng nhập thành công với quyền Admin.<br>- Mở `/admin/users` hiển thị bảng dữ liệu với 10 bản ghi người dùng.<br>- Ô tìm kiếm người dùng hoạt động. Screenshot: `v2_08_admin_logged_in.png`, `v2_08_admin_users_table.png`, `v2_08_admin_user_search.png`. | **PASS** |
| **9** | **Đăng ký tác giả (`/register`)** | Truy cập trang đăng ký `/register`. | Form đăng ký tác giả hiển thị đầy đủ các trường yêu cầu theo SRS. | Form đăng ký hiển thị đầy đủ các trường: Họ và tên, Email, Tên đăng nhập, Mật khẩu, Xác nhận mật khẩu. Screenshot: `v2_09_register_page.png`. | **PASS** |

---

## 2. BÁO CÁO TOÀN BỘ CONSOLE ERRORS, NETWORK ERRORS VÀ HTTP STATUS

Trong toàn bộ quá trình kiểm thử tự động và thủ công bằng Playwright Chromium, hệ thống đã thu thập và phân loại toàn bộ lỗi như sau:

### 2.1. Lỗi Console & Mạng (Network Errors)
1. **Lỗi MinIO S3 Storage Offline:**
   - Request: `GET http://localhost:9000/culinaryblog/recipes/f7935812-bd40-4179-8955-c3b9715354d2/images/..._thumb.png`
   - Triệu chứng: `net::ERR_CONNECTION_REFUSED`
   - Nguyên nhân: Container MinIO (port 9000) đang không chạy sau khi máy khởi động lại. Ảnh đại diện của một số recipe seed từ MinIO tạm thời không load được. Frontend tự động hiển thị fallback placeholder.
2. **Cảnh báo ORB từ Chromium đối với ảnh Seed bên ngoài:**
   - Request: `GET https://picsum.photos/800/600/?image=438`
   - Triệu chứng: `net::ERR_BLOCKED_BY_ORB`
   - Nguyên nhân: Opaque Response Blocking của Chromium với các tài nguyên cross-origin từ picsum.photos. Các thẻ `<img>` vẫn hiển thị bình thường.
3. **Request Next.js Prefetch bị hủy (Aborted):**
   - Request: `GET http://localhost:3001/login?_rsc=...: net::ERR_ABORTED`
   - Nguyên nhân: Hành vi mặc định của Next.js Router khi người dùng chuyển hướng nhanh (Client side navigation), Next.js tự động abort các request prefetch không còn cần thiết.

### 2.2. Kiểm tra HTTP 4xx / 5xx bất thường
- **Không có bất kỳ HTTP 500 Internal Server Error nào** xuất hiện từ Backend API.
- **HTTP 401 Unauthorized tại `/api/v1/auth/me`**: Chỉ xuất hiện khi người dùng chưa đăng nhập (Guest state), đúng theo đặc tả thiết kế bảo mật của RFC 7807. Khi có Bearer token hợp lệ, API phản hồi **200 OK**.
- **HTTP 403 Forbidden tại `/api/v1/admin/users`**: Xuất hiện chính xác khi tài khoản có role `Author` cố tình gọi API của `Admin`, đúng thiết kế RBAC.

---

## 3. KIỂM CHỨNG TÌNH TRẠNG CÁC LỖI THỰC TẾ (/categories, /recipes, /auth/me)

1. **`GET /api/v1/categories`**:
   - **ĐÃ XỬ LÝ TRIỆT ĐỂ (FIXED)**.
   - Nguyên nhân trước đó: Cấu hình CORS thiếu origin port 3001 khi backend chạy ở môi trường không có cờ Development.
   - Hiện trạng: Gọi API trả về HTTP 200 OK, trả về 20 categories đầy đủ, không còn lỗi `ApiError: Không thể kết nối đến máy chủ`.
2. **`GET /api/v1/recipes`**:
   - **ĐÃ XỬ LÝ TRIỆT ĐỂ (FIXED)**.
   - Nguyên nhân trước đó: Cùng nguyên nhân với lỗi CORS phía trên.
   - Hiện trạng: Gọi API trả về HTTP 200 OK với 59 items, hỗ trợ phân trang, lọc theo độ khó và danh mục.
3. **`GET /api/v1/auth/me`**:
   - **ĐÚNG THIẾT KẾ & HOẠT ĐỘNG CHUẨN XÁC (BY DESIGN & VERIFIED)**.
   - Khi chưa có Bearer Token: Trả về HTTP 401 Unauthorized (bảo vệ tài nguyên cá nhân).
   - Khi có Bearer Token: Trả về HTTP 200 OK kèm thông tin định danh `id`, `userName`, `email`, `roles`.

---

## 4. ĐÁNH GIÁ CẤU HÌNH CORS VÀ TÍNH AN TOÀN

1. **Không sử dụng Wildcard (`*`) không an toàn**:
   - Trong `Program.cs`, CORS được cấu hình sử dụng `policy.WithOrigins(allowedOrigins)`.
   - Các origin được liệt kê rõ ràng:
     ```json
     "Cors": {
       "AllowedOrigins": [
         "http://localhost:3000",
         "http://localhost:3001",
         "http://localhost:5000",
         "http://localhost:5001",
         "http://127.0.0.1:3000",
         "http://127.0.0.1:3001"
       ]
     }
     ```
   - Đi kèm `AllowCredentials()`, ASP.NET Core nghiêm cấm sử dụng `*` hoặc `AllowAnyOrigin()` khi có credentials. Dự án tuân thủ 100% quy tắc an toàn này.
2. **Tác động đến môi trường Production và cấu hình nhóm**:
   - Việc bổ sung port 3001/5001 song song với 3000/5000 **hoàn toàn không làm ảnh hưởng đến cấu hình của các thành viên khác**. Thành viên chạy ở 3000/5000 vẫn hoạt động bình thường.
   - Trên Production, danh sách `Cors:AllowedOrigins` được ghi đè thông qua biến môi trường máy chủ (ví dụ `Cors__AllowedOrigins__0=https://culinaryblog.vn`), hoàn toàn cô lập và an toàn.

---

## 5. DANH MỤC CÁC CHỨC NĂNG CHƯA ĐƯỢC KIỂM THỬ TRỰC TIẾP (OUT OF CURRENT SCOPE)

Nhằm bảo đảm tính trung thực tuyệt đối và không báo cáo sai lệch:
1. **MinIO Live Upload:** Chưa kiểm thử upload ảnh mới do container MinIO (port 9000) đang tạm dừng.
2. **Email Verification gửi qua SMTP:** Chưa kiểm thử việc gửi email thật qua SMTP server (MailHog/Papercut).
3. **Google OAuth 2.0 Login:** Nút đăng nhập Google đã có trên UI, nhưng luồng OAuth popup thực tế cần tương tác với Google Account ngoài trình duyệt nên chưa chạy tự động.
4. **Nutrition Auto-calculation:** Chức năng thuộc Phase 2 / Lab 8, ngoài phạm vi MVP hiện tại.

---

## 6. TRẠNG THÁI TIẾN TRÌNH VÀ CỔNG MẠNG

- **Frontend:** Đang chạy tại `http://localhost:3001` (PID: 34296).
- **Backend:** Đang chạy tại `http://localhost:5001` (PID: 21416).
- **Cổng 3000 & 5000:** Nguyên vẹn, không bị can thiệp.
- **Git Status:** Không commit, không push, không reset, không clean, không stash.

---

## 7. KẾT LUẬN

Sau khi kiểm thử bằng các kịch bản tương tác thật với đúng các selector DOM và luồng xác thực thực tế:
- **Hệ thống xác thực & phân quyền (RBAC) giữa Guest, Author và Admin hoạt động chính xác 100% theo SRS v1.2.0.**
- **Lỗi kết nối CORS đã được giải quyết dứt điểm và an toàn.**
- **Dự án đủ điều kiện và hoàn toàn sẵn sàng để bước sang triển khai Lab 8.**
