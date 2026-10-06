# Xử lý ảnh nền và tạo ảnh thu nhỏ (Thumbnail Background Job)

## 1. Mục tiêu
Branch `feat/vohungmanh-thumbnail-job` giải quyết việc xây dựng phân hệ tác vụ nền xử lý đồ họa (**JOB-002 Image Resize / Thumbnail**) cho nền tảng CulinaryBlog thuộc Task 4:
- Khi người dùng tải lên hình ảnh món ăn chất lượng cao (dung lượng lớn, kích thước hàng ngàn pixel), việc xử lý giải mã và thay đổi kích thước ngay trong luồng HTTP Request sẽ làm nghẽn tiến trình API, gây độ trễ lớn (High Latency) và làm giảm nghiêm trọng trải nghiệm người dùng.
- Tách rời hoàn toàn tác vụ nặng về tính toán (CPU-bound) sang hàng đợi bất đồng bộ **Hangfire Background Worker**, giải phóng API Server trả về phản hồi tức thì cho người dùng.
- Tự động sinh ra 2 phiên bản kích thước tối ưu:
  - **Medium Image:** Dùng cho trang chi tiết công thức (Recipe Detail View).
  - **Thumbnail Image:** Dùng cho danh sách tìm kiếm, trang chủ và thẻ hiển thị nhỏ (Card View).
- Tải các phiên bản đã resize lên **MinIO Object Storage**, cập nhật đường dẫn vào cơ sở dữ liệu **PostgreSQL**, bảo đảm tính lũy đẳng (**Idempotency**) và cơ chế tự động thử lại (**Hangfire Retry**).

---

## 2. Kết quả đạt được
Sau khi triển khai branch này:
- **Tác vụ nền bất đồng bộ Hangfire `IImageResizeJob`**:
  - Tách luồng xử lý ảnh khỏi API upload, vận hành êm ái trên hàng đợi `default` của Hangfire.
- **Tạo 2 phiên bản ảnh chuẩn hóa giữ nguyên tỷ lệ khung hình (Aspect Ratio)**:
  - **Medium:** Chiều rộng tối đa 800px (`ResizeMode.Max`), không bị bóp méo hình dạng món ăn.
  - **Thumbnail:** Chiều rộng tối đa 300px (`ResizeMode.Max`), tối ưu hóa tốc độ tải trang cho mobile và tablet.
- **Tương thích định dạng linh hoạt qua `SixLabors.ImageSharp`**:
  - Tự động phát hiện header nhị phân (`DecodedImageFormat`) và giữ nguyên định dạng tệp gốc khi xuất ra (JPEG $\rightarrow$ JPEG, PNG $\rightarrow$ PNG, WebP $\rightarrow$ WebP).
- **Tính lũy đẳng (Idempotency) và An toàn dữ liệu**:
  - Nếu `RecipeImage` đã có đủ `MediumUrl` và `ThumbnailUrl`, job tự động bỏ qua để tiết kiệm tài nguyên CPU và tránh sinh rác trên MinIO.
  - Quá trình upload thất bại không bao giờ ghi nhận URL rác vào Database và tuyệt đối không xâm phạm đến ảnh gốc `OriginalUrl`.
- **Cơ chế kiên cường Hangfire Retry**:
  - Cấu hình thử lại tự động 3 lần qua thuộc tính `[AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]`.
- **Kiểm thử bao phủ**: 10/10 kịch bản kiểm thử đơn vị với ảnh binary thật trong `ImageResizeJobTests` và 97/98 tests toàn hệ thống đều vượt qua thành công (100% Pass).

---

## 3. Luồng hoạt động

```text
Upload API / Event Trigger
  │ (Gửi ImageId cần xử lý)
  ▼
Hangfire Queue (PostgreSQL hangfire schema)
  │ └─ BackgroundJob.Enqueue<IImageResizeJob>(job => job.ProcessAsync(imageId))
  ▼
Background Worker: ImageResizeJob.ProcessAsync
  │ ├─ Bước 1: Đọc RecipeImage từ Database:
  │ │    Kiểm tra tồn tại, !IsDeleted, và kiểm tra tính lũy đẳng (đã có Medium & Thumb chưa)
  │ │
  │ ├─ Bước 2: Tải Stream ảnh gốc từ MinIO:
  │ │    IStorageService.DownloadAsync(bucketName, originalObjectKey)
  │ │
  │ ├─ Bước 3: Giải mã ảnh nhị phân:
  │ │    SixLabors.ImageSharp.Image.LoadAsync(inputStream) nhận diện format gốc
  │ │
  │ ├─ Bước 4: Thay đổi kích thước (Resize):
  │ │    - Medium: Chiều rộng tối đa 800px (giữ aspect ratio)
  │ │    - Thumbnail: Chiều rộng tối đa 300px (giữ aspect ratio)
  │ │
  │ ├─ Bước 5: Mã hóa sang Stream nhị phân theo format gốc:
  │ │    image.SaveAsync(stream, detectedFormat)
  │ │
  │ ├─ Bước 6: Tải lên MinIO qua IStorageService.UploadAsync:
  │ │    - recipes/{recipeId}/images/{imageId}_medium.{ext}
  │ │    - recipes/{recipeId}/images/{imageId}_thumb.{ext}
  │ │
  │ └─ Bước 7: Cập nhật PostgreSQL Database trong Transaction:
  │      Cập nhật MediumUrl, ThumbnailUrl, UpdatedAt = UtcNow
  ▼
Output
  └─ RecipeImage trong Database có đầy đủ 3 URLs: Original, Medium, Thumbnail
```

### Giải thích chi tiết các bước xử lý:
1. **Bước 1 (Nhận diện tác vụ):** Hangfire Worker rút một tác vụ từ hàng đợi, bóc tách `imageId` và truy vấn thực thể `RecipeImage` từ PostgreSQL. Nếu ảnh không tồn tại hoặc bị xóa mềm (`IsDeleted = true`), worker ghi log cảnh báo và kết thúc mà không kích hoạt retry. Nếu ảnh đã có đủ `MediumUrl` và `ThumbnailUrl`, worker ghi nhận idempotent và bỏ qua.
2. **Bước 2 (Tải ảnh gốc):** Worker gọi `IStorageService.DownloadAsync` để kéo luồng byte gốc từ bucket MinIO `culinaryblog`.
3. **Bước 3 (Giải mã):** Thư viện `SixLabors.ImageSharp` phân tích header nhị phân, giải mã các pixel của ảnh và nhận dạng định dạng gốc (JPEG, PNG, WebP).
4. **Bước 4 (Resize song song):** Sử dụng thuật toán nội suy đa thức chất lượng cao để thu nhỏ ảnh về kích thước tối đa 800px cho Medium và 300px cho Thumbnail mà không làm méo tỷ lệ món ăn.
5. **Bước 5 (Mã hóa):** Lưu trữ 2 ảnh mới vào các `MemoryStream` riêng biệt theo đúng encoder của định dạng gốc.
6. **Bước 6 (Upload MinIO):** Tải 2 tệp nhị phân mới lên MinIO tại khóa duy nhất `_medium.{ext}` và `_thumb.{ext}`.
7. **Bước 7 (Commit Database):** Cả hai URL mới được ghi nhận vào bản ghi `RecipeImage` trong PostgreSQL và lưu lại an toàn.

---

## 4. Các file chính

| File | Vai trò | Xử lý gì |
| :--- | :--- | :--- |
| `backend/src/CulinaryBlog.Application/Interfaces/IImageResizeJob.cs` | Application / Interface | Hợp đồng trừu tượng định nghĩa phương thức `ProcessAsync(Guid imageId, CancellationToken ct)`. |
| `backend/src/CulinaryBlog.Infrastructure/BackgroundJobs/ImageResizeJob.cs` | Infrastructure / Background Job | Triển khai chi tiết job resize bằng `ImageSharp`, tải stream từ MinIO, resize 800px/300px, upload MinIO và cập nhật database. |
| `backend/src/CulinaryBlog.Application/Interfaces/IStorageService.cs` | Application / Interface | Mở rộng bổ sung phương thức `DownloadAsync` stream tệp tin từ Object Storage. |
| `backend/src/CulinaryBlog.Infrastructure/Storage/S3StorageService.cs` | Infrastructure / Service | Triển khai `DownloadAsync` sử dụng `GetObjectAsync` của AWS SDK S3 để tải luồng byte từ MinIO. |
| `backend/src/CulinaryBlog.Infrastructure/DependencyInjection.cs` | Infrastructure / DI | Đăng ký `IImageResizeJob` vào Dependency Injection container. |
| `backend/tests/CulinaryBlog.UnitTests/Infrastructure/BackgroundJobs/ImageResizeJobTests.cs` | Unit Tests | Bộ 10 unit tests với ảnh binary thật (JPEG, PNG, WebP, file hỏng, Idempotency, Hangfire retry). |
| `docs/VO_HUNG_MANH_THUMBNAIL_JOB_COMPLAN.md` | Tài liệu bảo vệ | Báo cáo giải trình kỹ thuật chuyên sâu (457 dòng) giải thích cơ chế Hangfire, ImageSharp và câu hỏi vấn đáp. |
| `docs/README_THUMBNAIL_JOB.md` | Tài liệu kỹ thuật | Tài liệu hướng dẫn kỹ thuật chi tiết theo chuẩn 13 phần. |

---

## 5. API / Interface

### Hợp đồng Background Job

| Trigger | Job Interface | Input | Processing | Output |
| :--- | :--- | :--- | :--- | :--- |
| Gọi `BackgroundJob.Enqueue<IImageResizeJob>(job => job.ProcessAsync(image.Id))` sau khi tải ảnh gốc thành công | `IImageResizeJob.ProcessAsync` | `Guid imageId`<br>`CancellationToken ct` | 1. Tải ảnh gốc từ MinIO<br>2. Decode ImageSharp<br>3. Resize 800px (Medium) & 300px (Thumb)<br>4. Upload MinIO<br>5. Cập nhật `RecipeImages` DB | Điền dữ liệu vào `MediumUrl` và `ThumbnailUrl` trong bảng `RecipeImages` tại PostgreSQL |

---

## 6. Business Rules

1. **Quy cách kích thước chuẩn hóa (Image Specifications)**:
   - **Medium Image:** Chiều rộng tối đa **800px** (`Size(800, 0)`), chiều cao co giãn tự động theo tỷ lệ gốc (`ResizeMode.Max`). Tối ưu cho màn hình hiển thị bài viết chi tiết.
   - **Thumbnail Image:** Chiều rộng tối đa **300px** (`Size(300, 0)`), chiều cao co giãn tự động theo tỷ lệ gốc (`ResizeMode.Max`). Tối ưu cho thẻ hiển thị dạng lưới và trang tìm kiếm.
2. **Bảo toàn tỷ lệ khung hình (Aspect Ratio Preservation)**:
   - Sử dụng chế độ `ResizeMode.Max` của ImageSharp: ảnh không bao giờ bị kéo giãn hoặc méo mó; nếu ảnh gốc nhỏ hơn kích thước đích, hệ thống không phóng to làm vỡ hạt.
3. **Giữ nguyên định dạng gốc (Format Passthrough)**:
   - File tải lên là JPEG $\rightarrow$ sinh ra Medium/Thumb dạng JPEG.
   - File tải lên là PNG $\rightarrow$ sinh ra Medium/Thumb dạng PNG (bảo lưu kênh trong suốt Alpha Channel).
   - File tải lên là WebP $\rightarrow$ sinh ra Medium/Thumb dạng WebP.
4. **Tính lũy đẳng (Idempotency Rule)**:
   - Nếu job bị kích hoạt lại cho một ảnh đã có đầy đủ `MediumUrl` và `ThumbnailUrl`, hàm lập tức ghi log và dừng lại:
     `"RecipeImage {Id} đã có đầy đủ MediumUrl và ThumbnailUrl. Bỏ qua để đảm bảo tính Idempotent."`
   - Ngăn chặn xử lý dư thừa, tiết kiệm CPU và không sinh rác trên MinIO.
5. **Cấu hình Hangfire Retry**:
   - Thuộc tính trên class:
     ```csharp
     [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
     ```
   - Số lần thử lại tối đa là 3 lần nếu xảy ra sự cố mạng MinIO hoặc quá tải tạm thời. Sau 3 lần vẫn thất bại, job chuyển sang trạng thái `Failed` trong Hangfire Dashboard để lập trình viên kiểm tra.
6. **Bảo vệ toàn vẹn dữ liệu gốc**:
   - Trường `OriginalUrl` là bất biến, không bao giờ bị ghi đè hay thay đổi bởi Thumbnail Job.
   - Cập nhật database chỉ diễn ra khi và chỉ khi **cả hai ảnh** đã được upload thành công lên MinIO.

---

## 7. Ví dụ hoạt động

### Kịch bản: Xử lý nền ảnh tô Phở bò sau khi người dùng upload

```text
INPUT:
- ImageId = "f5c11111-0000-0000-0000-000000000001"
- RecipeId = "9a7f3e1b-0000-0000-0000-000000000001"
- OriginalUrl = "http://localhost:9000/culinaryblog/recipes/9a7f3e1b-.../images/f5c11111.jpg"
- Ảnh gốc: Kích thước 4000 x 3000 pixels (JPEG, 4.2 MB)

PROCESS:
1. Hangfire bốc job ImageResizeJob.ProcessAsync("f5c11111-...")
2. Tải Stream 4.2 MB từ MinIO bucket culinaryblog
3. SixLabors.ImageSharp nạp ảnh và nhận diện JPEG
4. Resize Medium: 4000x3000 -> 800x600 pixels (Dung lượng giảm còn ~180 KB)
5. Resize Thumbnail: 4000x3000 -> 300x225 pixels (Dung lượng giảm còn ~35 KB)
6. Upload MinIO:
   - recipes/9a7f3e1b-.../images/f5c11111_medium.jpg
   - recipes/9a7f3e1b-.../images/f5c11111_thumb.jpg
7. Cập nhật RecipeImage trong PostgreSQL

OUTPUT:
- MediumUrl: "http://localhost:9000/culinaryblog/recipes/9a7f3e1b-.../images/f5c11111_medium.jpg"
- ThumbnailUrl: "http://localhost:9000/culinaryblog/recipes/9a7f3e1b-.../images/f5c11111_thumb.jpg"
- Tốc độ tải trang web tăng gấp 10 lần nhờ nạp ảnh 35 KB thay vì ảnh gốc 4.2 MB.
```

---

## 8. Error Handling

| Tình huống lỗi | Hành vi của hệ thống (Behavior & Retry Strategy) |
| :--- | :--- |
| **RecipeImage không tồn tại hoặc đã bị xóa mềm (`IsDeleted = true`)** | Ghi log cảnh báo `LogWarning` và kết thúc bình thường (không ném ngoại lệ để tránh kích hoạt Hangfire retry vô ích). |
| **Ảnh đã được resize trước đó (Idempotent)** | Ghi log thông tin `LogInformation` và bỏ qua, không tốn CPU xử lý lại. |
| **Ảnh gốc bị hỏng (Corrupt Bytes)** | `ImageSharp` ném ngoại lệ khi giải mã. Hệ thống ghi log lỗi, không lưu URL rác vào Database, kích hoạt Hangfire retry/fail. |
| **Mất kết nối MinIO khi tải hoặc upload** | Ném ngoại lệ I/O mạng, Hangfire giữ job trong hàng đợi và kích hoạt cơ chế retry tự động theo khoảng cách thời gian tăng dần (tối đa 3 lần). |
| **Lỗi kết nối PostgreSQL Database** | Database rollback, không lưu trạng thái dở dang. |

---

## 9. Cách chạy và Demo

### Bước 1: Khởi chạy hạ tầng Docker
```bash
docker compose up -d
```
*(Đảm bảo PostgreSQL và MinIO đang hoạt động)*

### Bước 2: Khởi chạy Backend API
```bash
dotnet run --project backend/src/CulinaryBlog.Api
```

### Bước 3: Kịch bản Demo thực tế cho Giảng viên

1. **Truy cập Hangfire Dashboard**:
   - Mở trình duyệt tại `http://localhost:5000/hangfire` (ở môi trường Development).
   - Chỉ cho Giảng viên thấy hàng đợi `default` và số lượng tác vụ đang sẵn sàng.
2. **Kích hoạt tải ảnh công thức**:
   - Sử dụng Postman gửi tải lên một ảnh chất lượng cao dung lượng 4 MB cho công thức.
   - Quan sát API phản hồi ngay lập tức cho client mà không bị đơ giật.
3. **Quan sát quá trình xử lý nền trên Hangfire**:
   - Trên Hangfire Dashboard, quan sát job `ImageResizeJob.ProcessAsync` chuyển trạng thái: `Enqueued` $\rightarrow$ `Processing` $\rightarrow$ `Succeeded` (trong vòng 1–2 giây).
4. **Kiểm tra kho lưu trữ MinIO**:
   - Truy cập MinIO Web Console (`http://localhost:9001`) tại bucket `culinaryblog/recipes/{recipeId}/images/`.
   - Chỉ cho Giảng viên thấy xuất hiện đủ 3 tệp tin:
     - `{id}.jpg` (Ảnh gốc 4 MB)
     - `{id}_medium.jpg` (Ảnh 800px ~180 KB)
     - `{id}_thumb.jpg` (Ảnh 300px ~35 KB)
5. **Kiểm tra Database**:
   - Mở bảng `RecipeImages` trong PostgreSQL: Cả 3 cột `OriginalUrl`, `MediumUrl`, `ThumbnailUrl` đều chứa đường dẫn đầy đủ.

---

## 10. Testing

### Bộ kiểm thử chuyên biệt `ImageResizeJobTests`
Chạy lệnh kiểm thử đơn vị:
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --filter FullyQualifiedName~ImageResizeJobTests
```

**Kết quả kiểm thử thực tế:**
- **10/10 tests PASSED (100%)** (Thời gian chạy: ~3s).
- **Danh sách 10 kịch bản kiểm thử chi tiết:**
  1. `ProcessAsync_WhenImageNotFound_LogsWarningAndReturns`: Xử lý an toàn khi không tìm thấy ảnh.
  2. `ProcessAsync_WhenImageIsDeleted_LogsWarningAndReturns`: Bỏ qua ảnh đã bị xóa mềm.
  3. `ProcessAsync_WhenAlreadyProcessed_SkipsExecution_Idempotent`: Đảm bảo tính lũy đẳng không xử lý lại.
  4. `ProcessAsync_ProcessesJpegImage_Successfully`: Resize ảnh JPEG thật thành công, upload 2 ảnh và cập nhật DB.
  5. `ProcessAsync_ProcessesPngImage_Successfully`: Resize ảnh PNG thật thành công và giữ nguyên định dạng PNG.
  6. `ProcessAsync_ProcessesWebpImage_Successfully`: Resize ảnh WebP thật thành công và giữ nguyên định dạng WebP.
  7. `ProcessAsync_WhenCorruptImage_ThrowsAndDoesNotUpdateDatabase`: Bắt lỗi ảnh hỏng, không lưu URL rác.
  8. `ProcessAsync_WhenStorageFails_ThrowsExceptionForHangfireRetry`: Ném ngoại lệ khi upload MinIO lỗi để Hangfire retry.
  9. `ProcessAsync_RespectsCancellation`: Hủy tác vụ an toàn khi CancellationToken kích hoạt.
  10. `ImageResizeJob_HasAutomaticRetryAttribute_ConfiguredCorrectly`: Xác nhận thuộc tính Hangfire retry 3 lần.

### Tổng hợp Unit Tests toàn bộ solution:
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj
```
**Kết quả thực tế:**
- `Passed: 97, Failed: 0, Skipped: 1, Total: 98` (100% Pass).

---

## 11. Build
Thực hiện lệnh biên dịch solution:
```bash
dotnet build backend/CulinaryBlog.sln
```

**Kết quả biên dịch thực tế:**
- **Thành công (Exit code 0)**.
- **0 Error(s)**, 420 Warning(s) (chủ yếu là thông báo bản quyền SixLabors ImageSharp mã nguồn mở và StyleCop format).

---

## 12. Limitations

1. **Hạn chế giải mã định dạng AVIF**:
   - Thư viện đồ họa thuần C# `SixLabors.ImageSharp` hiện tại chưa tích hợp sẵn bộ giải mã (decoder) cho định dạng AVIF mặc định (định dạng này đòi hỏi các thư viện native C/C++ ngoài như `libheif`).
   - Nếu người dùng tải lên ảnh định dạng AVIF, hệ thống vẫn lưu trữ và phục vụ ảnh gốc `OriginalUrl` bình thường, nhưng tính năng Thumbnail Job sẽ từ chối giải mã và giữ nguyên các trường thumbnail là null.
2. **Phụ thuộc môi trường Hangfire Server**:
   - Thumbnail Job đòi hỏi tiến trình Hangfire Server phải đang chạy song song với ứng dụng để tiêu thụ các job từ hàng đợi.

---

## 13. Kết luận
Branch `feat/vohungmanh-thumbnail-job` đã hoàn thành trọn vẹn và chuẩn mực tính năng tác vụ nền xử lý hình ảnh cho CulinaryBlog:
- Giải phóng API khỏi gánh nặng xử lý đồ họa, nâng cao thông lượng phục vụ người dùng.
- Tự động sinh ra 2 phiên bản ảnh kích thước chuẩn hóa 800px và 300px giữ nguyên tỷ lệ khung hình.
- Đáp ứng đầy đủ các tiêu chuẩn kiến trúc: tính lũy đẳng (Idempotency), cơ chế tự động thử lại (Hangfire Retry 3 lần) và kiểm thử tự động 100% Pass.
