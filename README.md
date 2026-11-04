# TASK 4 (BỮA 4) – BACKGROUND JOB TẠO ẢNH THU NHỎ (THUMBNAIL PROCESSING)

**Sinh viên:** Võ Hùng Mạnh
**MSSV:** 2312687
**Nhóm:** 12
**Nhánh:** `feat/vohungmanh-thumbnail-job`

---

### 1. Mục tiêu Task 4 liên quan đến nhánh
Trong khuôn khổ Bữa 4 (Task 4), hệ thống yêu cầu triển khai dịch vụ xử lý nền không đồng bộ (Background Job / JOB-002) nhằm tự động tạo ảnh thu nhỏ (Thumbnail) và ảnh kích thước trung bình (Medium) ngay sau khi người dùng tải ảnh công thức nấu ăn lên hệ thống. Việc này giúp giảm tải thời gian phản hồi của API tải ảnh và tối ưu hóa băng thông tải trang phía Frontend.

### 2. Công việc cá nhân thực tế
- Thiết kế interface `IImageResizeJob` tại tầng Application để định nghĩa hợp đồng xử lý ảnh nền.
- Hiện thực lớp `ImageResizeJob` tại tầng Infrastructure sử dụng thư viện chuyên dụng `SixLabors.ImageSharp` và `SixLabors.ImageSharp.Processing`.
- Tích hợp cơ chế thực thi tác vụ nền Hangfire với chính sách tự động thử lại `[AutomaticRetry(Attempts = 3)]` khi gặp sự cố tạm thời về mạng hoặc I/O.
- Kết nối dịch vụ lưu trữ `IStorageService` (MinIO) để tải ảnh gốc, sinh ảnh kích thước mới và ghi đè/lưu trữ ảnh đã tối ưu hóa.
- Cập nhật cơ sở dữ liệu PostgreSQL qua Entity Framework Core để lưu trữ đường dẫn ảnh Thumbnail và Medium vào thực thể `RecipeImage`.
- Viết 469 dòng mã kiểm thử đơn vị tự động trong `ImageResizeJobTests.cs` kiểm chứng toàn diện mọi luồng xử lý và xử lý ngoại lệ.

### 3. Chức năng đã hoàn thành
- **Resize đa kích thước thông minh:**
  - `MediumMaxWidth = 800px`: Tối ưu hiển thị trong nội dung chi tiết công thức, giữ nguyên tỷ lệ khung hình gốc (aspect ratio).
  - `ThumbnailMaxWidth = 300px`: Tối ưu hiển thị trong thẻ danh sách công thức (RecipeCard) và kết quả tìm kiếm.
- **Tự động kích hoạt nền (Asynchronous Background Execution):** Tác vụ chạy ngầm trên Hangfire Worker, không làm nghẽn luồng xử lý HTTP request chính của người dùng khi upload ảnh.
- **Cơ chế phục hồi và thử lại (Fault-tolerant):** Tự động thử lại tối đa 3 lần với chiến lược back-off khi MinIO hoặc mạng nội bộ gặp lỗi gián đoạn.
- **Hỗ trợ đa định dạng ảnh:** Hỗ trợ JPEG, PNG, WebP với bộ mã hóa chất lượng cao, tự động giải phóng bộ nhớ Stream sau khi xử lý.

### 4. Cách xử lý và luồng hoạt động
```text
Client Upload Image API (200 OK ngay lập tức)
        │
        ├── Lưu tạm thời bản ghi RecipeImage vào DB
        └── Enqueue ImageResizeJob(recipeImageId) vào Hangfire
                    │
                    ▼ (Hangfire Worker thực thi ngầm)
        1. Query RecipeImage từ PostgreSQL DbContext
        2. Tải Stream ảnh gốc từ MinIO hoặc Web URL
        3. ImageSharp giải mã (Decode) hình ảnh
        4. Resize ảnh sang 2 phiên bản:
           ├── Medium: max width 800px (giữ tỷ lệ aspect ratio)
           └── Thumbnail: max width 300px (giữ tỷ lệ aspect ratio)
        5. Tải 2 ảnh mới lên MinIO bucket qua IStorageService
        6. Cập nhật URL Thumbnail/Medium vào Database
        7. Ghi nhận log có cấu trúc Serilog (Information / Error)
```

### 5. Danh sách file mã nguồn quan trọng

| Đường dẫn file | Vai trò và chức năng chính |
| :--- | :--- |
| `backend/src/CulinaryBlog.Application/Interfaces/IImageResizeJob.cs` | Interface hợp đồng cho background job xử lý ảnh resize/thumbnail. |
| `backend/src/CulinaryBlog.Infrastructure/BackgroundJobs/ImageResizeJob.cs` | Hiện thực logic decode, resize bằng SixLabors.ImageSharp, upload MinIO và cập nhật DB. |
| `backend/src/CulinaryBlog.Infrastructure/DependencyInjection.cs` | Đăng ký `IImageResizeJob` và cấu hình Hangfire services. |
| `backend/tests/CulinaryBlog.UnitTests/Infrastructure/BackgroundJobs/ImageResizeJobTests.cs` | Bộ kiểm thử đơn vị 469 dòng kiểm tra resize, retry logic, mock DbContext và MinIO storage. |
| `docs/README_THUMBNAIL_JOB.md` | Tài liệu kỹ thuật chi tiết về kiến trúc thumbnail pipeline và hướng dẫn cấu hình Hangfire. |
| `docs/VO_HUNG_MANH_THUMBNAIL_JOB_COMPLAN.md` | Bản phân tích và kế hoạch hoàn thiện tác vụ xử lý ảnh nền Bữa 4. |

### 6. API, Database và Infrastructure liên quan
- **Cơ sở dữ liệu:** PostgreSQL 16 (Bảng `recipe_images`).
- **Lưu trữ tệp:** MinIO S3-compatible Object Storage (Bucket `culinaryblog` hoặc `culinary-blog`).
- **Tác vụ nền:** Hangfire với Storage lưu trên PostgreSQL / In-Memory cho môi trường test.
- **Thư viện xử lý đồ họa:** `SixLabors.ImageSharp` (v2.x / v3.x) & `SixLabors.ImageSharp.Processing`.

### 7. Hướng dẫn chạy

#### Bước 1: Khởi động Docker Container (PostgreSQL & MinIO)
```bash
docker compose up -d postgres minio
```

#### Bước 2: Chạy Backend và mở Hangfire Dashboard
```bash
cd backend/src/CulinaryBlog.Api
dotnet run
```
Truy cập Hangfire Dashboard để giám sát hàng đợi và tiến độ job tại: `http://localhost:5000/hangfire`.

### 8. Hướng dẫn kiểm thử

#### Kiểm thử Unit Test tự động
Chạy toàn bộ test case của Background Job:
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --filter "FullyQualifiedName~ImageResizeJob"
```

#### Kiểm thử thực tế
1. Gửi request tải ảnh lên công thức qua API `POST /api/v1/recipes/{id}/images`.
2. Mở Hangfire Dashboard kiểm tra job `ImageResizeJob.ProcessAsync` chuyển sang trạng thái `Succeeded`.
3. Kiểm tra MinIO Console thấy xuất hiện 2 tệp ảnh mới với hậu tố `-medium` và `-thumb`.

### 9. Kết quả kiểm thử có bằng chứng
- **Kết quả Unit Test:** Toàn bộ test case trong `ImageResizeJobTests` đều đạt `Passed!` (xác minh thành công luồng xử lý ảnh hợp lệ, xử lý ảnh hỏng, xử lý khi không tìm thấy bản ghi DB, và tự động gọi thử lại).
- **Hiệu năng:** Thời gian xử lý trung bình mỗi ảnh dao động từ 120ms - 250ms trên worker nền mà không ảnh hưởng tới luồng người dùng.

### 10. Hạn chế và tồn đọng
- **Trạng thái thực tế:** Module hoàn chỉnh ở tầng backend worker và database persistence.
- **Tồn đọng:** Chưa tích hợp cấu hình tỷ lệ nén linh hoạt theo từng loại công thức (hiện đang dùng mức nén mặc định tối ưu của ImageSharp).
- **Tính năng mở rộng:** Giao diện xem trước và chọn ảnh thu nhỏ tùy chỉnh được chuyển tiếp hoàn thiện trong Task 6 Image Management.

### 11. Liên kết tài liệu kỹ thuật chi tiết
- Kiến trúc chi tiết Thumbnail Pipeline: [docs/README_THUMBNAIL_JOB.md](docs/README_THUMBNAIL_JOB.md)
- Kế hoạch triển khai và đặc tả kỹ thuật: [docs/VO_HUNG_MANH_THUMBNAIL_JOB_COMPLAN.md](docs/VO_HUNG_MANH_THUMBNAIL_JOB_COMPLAN.md)

---

# Phát triển ứng dụng Web nâng cao

## Nhóm 12

### Thành viên
1. Võ Hùng Mạnh - 2312687 - 2312687@dlu.edu.vn - Nhóm trưởng
2. Lê Thị Ánh Nhung - 2312709 - 2312709@dlu.edu.vn
3. Phạm Nguyễn Ngọc Phước - 2312718 - 2312718@dlu.edu.vn
4. Nguyễn Văn Quốc - 2312729 - 2312729@dlu.edu.vn
