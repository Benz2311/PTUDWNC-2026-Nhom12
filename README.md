# TASK 4 (BỮA 4) – DỊCH VỤ LƯU TRỮ TỆP TIN MINIO OBJECT STORAGE

**Sinh viên:** Võ Hùng Mạnh
**MSSV:** 2312687
**Nhóm:** 12
**Nhánh:** `feat/vohungmanh-file-storage`

---

### 1. Mục tiêu Task 4 liên quan đến nhánh
Trong khuôn khổ Bữa 4 (Task 4) của học phần Phát triển ứng dụng Web nâng cao, hệ thống yêu cầu xây dựng hạ tầng lưu trữ tệp tin đa phương tiện tập trung, độc lập với web server, có khả năng mở rộng và tương thích chuẩn S3 API để phục vụ cho các module tải ảnh món ăn (`RecipeImage`) và hình ảnh các bước thực hiện (`RecipeStep`).

### 2. Công việc cá nhân thực tế
- Thiết kế interface trừu tượng `IStorageService` tại tầng Application để tách biệt nghiệp vụ khỏi nhà cung cấp lưu trữ cụ thể.
- Hiện thực lớp dịch vụ `S3StorageService` tại tầng Infrastructure sử dụng `AWSSDK.S3` để giao tiếp với cụm MinIO Server chạy trên Docker.
- Xử lý cấu hình tương thích MinIO HTTP endpoint thông qua thiết lập Signed Payloads (`DisablePayloadSigning = false`), giải quyết triệt để lỗi từ chối kết nối HTTP signature khi chạy nội bộ qua Docker.
- Xây dựng tiện ích `StoragePathHelper` tạo đường dẫn chuẩn hóa, ngăn chặn path traversal và chống trùng lặp tên tệp.
- Định nghĩa hệ thống ngoại lệ chuyên biệt `StorageExceptions` và tích hợp `ExceptionHandlingMiddleware` chuyển đổi thành mã lỗi chuẩn RFC 7807.
- Xây dựng bộ Unit Test tự động cho `S3StorageService`, `StoragePathHelper` và middleware xử lý ngoại lệ.

### 3. Chức năng đã hoàn thành
- **Upload tệp tin (`UploadAsync`):** Hỗ trợ upload Stream kèm MIME type, tự động kiểm tra và khởi tạo bucket nếu chưa tồn tại (`EnsureBucketExistsAsync`), trả về URL công khai truy cập tệp tin.
- **Xóa tệp tin (`DeleteAsync`):** Hỗ trợ xóa an toàn, đảm bảo tính idempotent (không quăng lỗi nếu tệp không còn tồn tại trên bucket).
- **Presigned URL (`GetPresignedUrlAsync`):** Tạo đường dẫn tải tệp bảo mật có thời hạn (expiry), hỗ trợ chia sẻ tài nguyên an toàn mà không cần mở public toàn bộ bucket.
- **Bảo mật và chuẩn hóa đường dẫn:** Ngăn chặn tuyệt đối lỗ hổng Directory Traversal (`../`), tự động gán GUID/hash để phân tách thư mục theo món ăn: `recipes/{recipeId}/steps/{stepId}_{filename}` hoặc `recipes/{recipeId}/images/{filename}`.

### 4. Cách xử lý và luồng hoạt động
```text
Client Upload Request
        │
        ▼
ExceptionHandlingMiddleware (bắt lỗi StorageExceptions -> ProblemDetails RFC 7807)
        │
        ▼
StoragePathHelper (kiểm tra định dạng, sanitize tên file, sinh path an toàn)
        │
        ▼
IStorageService / S3StorageService
        │
        ├── Kiểm tra / tạo Bucket MinIO (nếu chưa có)
        ├── Ký Signed Payloads (tương thích MinIO HTTP endpoint)
        └── Gửi PutObjectRequest tới MinIO Server (Port 9000)
        │
        ▼
MinIO Container lưu file vào persistent storage và trả về Object URL
```

### 5. Danh sách file mã nguồn quan trọng

| Đường dẫn file | Vai trò và chức năng chính |
| :--- | :--- |
| `backend/src/CulinaryBlog.Application/Interfaces/IStorageService.cs` | Interface hợp đồng trừu tượng cho thao tác upload, delete, presigned URL. |
| `backend/src/CulinaryBlog.Application/Common/Utilities/StoragePathHelper.cs` | Tiện ích chuẩn hóa đường dẫn, kiểm tra định dạng và chống path traversal. |
| `backend/src/CulinaryBlog.Application/Exceptions/StorageExceptions.cs` | Định nghĩa các ngoại lệ lưu trữ: `StorageServiceException`, `FileNotFoundException`, `InvalidFileExtensionException`, `FileSizeExceededException`. |
| `backend/src/CulinaryBlog.Infrastructure/Storage/S3StorageService.cs` | Triển khai MinIO client qua AWSSDK.S3, xử lý Signed Payloads và bucket management. |
| `backend/src/CulinaryBlog.Infrastructure/Storage/StorageSettings.cs` | Model liên kết cấu hình Storage từ `appsettings.json` hoặc biến môi trường. |
| `backend/src/CulinaryBlog.Api/Middleware/ExceptionHandlingMiddleware.cs` | Middleware bắt ngoại lệ lưu trữ và trả về mã lỗi chuẩn RFC 7807. |
| `backend/tests/CulinaryBlog.UnitTests/Infrastructure/Storage/S3StorageServiceTests.cs` | 300 dòng mã Unit Test kiểm thử mock S3 client, upload, delete, presigned URL. |
| `backend/tests/CulinaryBlog.UnitTests/Application/Common/StoragePathHelperTests.cs` | Bộ kiểm thử chuẩn hóa tên file và kiểm tra tính an toàn chống path traversal. |
| `backend/tests/CulinaryBlog.UnitTests/API/ExceptionHandlingMiddlewareTests.cs` | Bộ kiểm thử middleware ánh xạ lỗi lưu trữ sang HTTP response chuẩn. |
| `docs/README_FILE_STORAGE.md` | Tài liệu kỹ thuật chuyên sâu về kiến trúc và cấu hình MinIO Storage. |

### 6. API, Database và Infrastructure liên quan
- **Dịch vụ Infrastructure:** MinIO Object Storage (Image: `minio/minio:latest`).
- **Cấu hình môi trường:**
  - `Storage:ServiceUrl`: Endpoint MinIO (mặc định `http://localhost:9000` hoặc `http://minio:9000` trong Docker network).
  - `Storage:AccessKey`: Khóa truy cập cấu hình qua `.env` (`MINIO_ROOT_USER`).
  - `Storage:SecretKey`: Khóa bí mật cấu hình qua `.env` (`MINIO_ROOT_PASSWORD`).
  - `Storage:BucketName`: Bucket mặc định lưu trữ hình ảnh (`culinary-blog`).
- **Thư viện tích hợp:** `AWSSDK.S3` v3.7, `AWSSDK.Core`.

### 7. Hướng dẫn chạy

#### Bước 1: Khởi động MinIO Server qua Docker
```bash
docker compose up -d minio
```
Kiểm tra MinIO Console hoạt động tại: `http://localhost:9001` (đăng nhập bằng tài khoản trong `.env`).

#### Bước 2: Chạy Backend API
```bash
cd backend/src/CulinaryBlog.Api
dotnet run
```

### 8. Hướng dẫn kiểm thử

#### Kiểm thử Unit Test tự động
Chạy các bài kiểm thử đơn vị cho module Storage:
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --filter "FullyQualifiedName~Storage"
```

#### Kiểm thử tích hợp qua Postman / cURL
Kiểm tra endpoint upload tệp hoặc sinh presigned URL thông qua các API quản lý ảnh (`RecipeImage`).

### 9. Kết quả kiểm thử có bằng chứng
- **Unit Test thực tế:** Toàn bộ test case trong `S3StorageServiceTests` và `StoragePathHelperTests` đều đạt kết quả `Passed!`.
- **MinIO Container:** Tệp tin hình ảnh được ghi nhận thực tế trong bucket `culinary-blog`, hiển thị đầy đủ metadata MIME type (`image/jpeg`, `image/png`, `image/webp`).

### 10. Hạn chế và tồn đọng
- **Trạng thái thực tế:** Module hoàn thành toàn diện ở tầng Backend Infrastructure và Application Service.
- **Tồn đọng:** Chưa tích hợp giao diện quản trị bucket chuyên dụng cho quản trị viên (hiện tại việc quản trị bucket trực tiếp qua MinIO Web Console).
- **Tính năng mở rộng:** Việc tự động sinh thumbnail ảnh ở các độ phân giải khác nhau được tách sang Background Job độc lập trên nhánh `feat/vohungmanh-thumbnail-job`.

### 11. Liên kết tài liệu kỹ thuật chi tiết
- Chi tiết kiến trúc lưu trữ và cấu hình: [docs/README_FILE_STORAGE.md](docs/README_FILE_STORAGE.md)
- Kế hoạch triển khai kỹ thuật: [docs/VO_HUNG_MANH_FILE_STORAGE_COMPLAN.md](docs/VO_HUNG_MANH_FILE_STORAGE_COMPLAN.md)

---

# Phát triển ứng dụng Web nâng cao

## Nhóm 12

### Thành viên
1. Võ Hùng Mạnh - 2312687 - 2312687@dlu.edu.vn - Nhóm trưởng
2. Lê Thị Ánh Nhung - 2312709 - 2312709@dlu.edu.vn
3. Phạm Nguyễn Ngọc Phước - 2312718 - 2312718@dlu.edu.vn
4. Nguyễn Văn Quốc - 2312729 - 2312729@dlu.edu.vn
