# Thumbnail Job

## Người thực hiện
**Võ Hùng Mạnh** (Nhóm 12)

## Branch
`feat/vohungmanh-thumbnail-job`

## Mục tiêu
Triển khai hoàn thiện tính năng **JOB-002 Image Resize / Thumbnail** thuộc Task 4 trong đồ án CulinaryBlog:
* Tiếp nhận ảnh gốc (`OriginalUrl`) của bài viết (`RecipeImage`).
* Chạy bất đồng bộ qua **Hangfire Background Worker**.
* Tự động giải mã (decode) và thay đổi kích thước (resize) tạo hai phiên bản:
  * **Medium Image:** Dùng cho trang chi tiết công thức nấu ăn.
  * **Thumbnail Image:** Dùng cho danh sách tìm kiếm, trang chủ và thẻ hiển thị nhỏ.
* Giữ nguyên tỷ lệ khung hình (Aspect Ratio), không bóp méo ảnh.
* Tải các phiên bản đã resize lên **MinIO Object Storage**.
* Cập nhật `MediumUrl` và `ThumbnailUrl` vào cơ sở dữ liệu **PostgreSQL** (`RecipeImages`).
* Tự động thử lại tối đa 3 lần với Hangfire `[AutomaticRetry]`.

---

## Chức năng đã hoàn thành
* [x] Tạo hợp đồng `IImageResizeJob` trong tầng Application.
* [x] Triển khai `ImageResizeJob` trong tầng Infrastructure sử dụng `SixLabors.ImageSharp`.
* [x] Mở rộng `IStorageService` và `S3StorageService` với phương thức `DownloadAsync` stream nhị phân.
* [x] Xử lý tương thích định dạng linh hoạt: Tự động giữ nguyên định dạng gốc của file (JPEG $\rightarrow$ JPEG, PNG $\rightarrow$ PNG, WebP $\rightarrow$ WebP).
* [x] Đảm bảo tính lũy đẳng (Idempotency): Nếu ảnh đã có `MediumUrl` và `ThumbnailUrl`, job tự động bỏ qua, tránh tạo rác trên MinIO.
* [x] An toàn dữ liệu: Nếu quá trình xử lý hoặc upload MinIO thất bại, Database không ghi nhận URL rác, và `OriginalUrl` tuyệt đối không bị ảnh hưởng.
* [x] Cấu hình thử lại tự động 3 lần qua thuộc tính `[AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]`.
* [x] Viết bộ 10 Unit Tests tự động với ảnh binary thật, bao phủ 100% các kịch bản cốt lõi.

---

## Kiến trúc
Hệ thống tuân thủ mô hình kiến trúc phân lớp Clean Architecture:
* **Domain Layer (`CulinaryBlog.Domain`):** Chứa entity `RecipeImage` với 3 thuộc tính URL (`OriginalUrl`, `MediumUrl`, `ThumbnailUrl`) và cờ `IsDeleted`.
* **Application Layer (`CulinaryBlog.Application`):** Định nghĩa hợp đồng trừu tượng `IImageResizeJob`, `IBackgroundJobService`, `IStorageService`, `IApplicationDbContext`.
* **Infrastructure Layer (`CulinaryBlog.Infrastructure`):** Triển khai cụ thể `ImageResizeJob`, kết nối thư viện `SixLabors.ImageSharp` để xử lý đồ họa, `AWSSDK.S3` để upload MinIO, và `Hangfire` để quản lý hàng đợi tác vụ nền.

---

## Luồng xử lý (Data Flow)

```text
[ Client / User ]
       │ Upload ảnh gốc
       ▼
[ RecipeImage (Database) ]  ── (Lưu OriginalUrl, MediumUrl = null, ThumbnailUrl = null)
       │
       │ Enqueue(job => job.ProcessAsync(image.Id))
       ▼
[ Hangfire Queue (PostgreSQL hangfire schema) ]
       │
       ▼ (Worker bốc job chạy nền)
[ ImageResizeJob.ProcessAsync ]
       │
       ├─► 1. Đọc RecipeImage từ DB (kiểm tra tồn tại, !IsDeleted, chưa xử lý)
       ├─► 2. Tải Stream ảnh gốc từ MinIO qua IStorageService.DownloadAsync
       ├─► 3. Decode ảnh thật bằng SixLabors.ImageSharp.Image.LoadAsync
       ├─► 4. Resize song song:
       │      ├─ Medium: max-width 800px (ResizeMode.Max, giữ aspect ratio)
       │      └─ Thumbnail: max-width 300px (ResizeMode.Max, giữ aspect ratio)
       ├─► 5. Encode sang Stream giữ nguyên format gốc
       ├─► 6. Upload lên MinIO qua IStorageService.UploadAsync:
       │      ├─ recipes/{recipeId}/images/{imageId}_medium.{ext}
       │      └─ recipes/{recipeId}/images/{imageId}_thumb.{ext}
       ├─► 7. Cập nhật RecipeImage: MediumUrl, ThumbnailUrl, UpdatedAt
       └─► 8. Commit vào PostgreSQL qua SaveChangesAsync
```

---

## Các file chính

| File | Vai trò |
| :--- | :--- |
| `IImageResizeJob.cs` | Interface định nghĩa hợp đồng background job xử lý ảnh ở tầng Application. |
| `ImageResizeJob.cs` | Triển khai chi tiết job resize bằng `ImageSharp`, tích hợp tải stream, upload MinIO và cập nhật database. |
| `IStorageService.cs` | Interface dịch vụ lưu trữ, bổ sung phương thức `DownloadAsync` tải stream tệp tin. |
| `S3StorageService.cs` | Triển khai `DownloadAsync` sử dụng `GetObjectAsync` của AWS SDK S3 để stream file từ MinIO. |
| `DependencyInjection.cs` | Đăng ký `HttpClient` và `IImageResizeJob` vào DI container. |
| `ImageResizeJobTests.cs` | Bộ kiểm thử đơn vị với ảnh binary thật (JPEG, PNG, WebP, Corrupt bytes, Idempotency, Retry). |

---

## Kích thước ảnh (Image Sizes)
* **Medium Image:**
  * **Quy cách:** Chiều rộng tối đa **800px** (`Size(800, 0)`), chiều cao co giãn tự động theo tỷ lệ gốc (`ResizeMode.Max`).
  * **Nguồn:** **Implementation Decision** (SRS không quy định số pixel cứng nhắc; đây là kích thước tiêu chuẩn tối ưu cho màn hình web và tablet mà không làm vỡ bố cục).
* **Thumbnail Image:**
  * **Quy cách:** Chiều rộng tối đa **300px** (`Size(300, 0)`), chiều cao co giãn tự động theo tỷ lệ gốc (`ResizeMode.Max`).
  * **Nguồn:** **Implementation Decision** (Kích thước tối ưu cho Card bài viết, danh sách tìm kiếm và avatar đại diện công thức).

---

## Định dạng ảnh thực tế (Image Formats)
* **Hỗ trợ đầy đủ:**
  * **JPEG (`image/jpeg`):** Định dạng phổ biến nhất cho ảnh chụp món ăn, dung lượng nén tốt.
  * **PNG (`image/png`):** Hỗ trợ ảnh có độ trong suốt (alpha channel) và đồ họa sắc nét.
  * **WebP (`image/webp`):** Định dạng web hiện đại, dung lượng nhẹ hơn JPEG 30% với chất lượng tương đương.
  * **GIF (`image/gif`):** Hỗ trợ ảnh động ngắn.
* **Cơ chế:** Thư viện `SixLabors.ImageSharp` tự động phát hiện header nhị phân (`DecodedImageFormat`), và lưu file ảnh resize đúng theo định dạng gốc.

---

## Cơ chế Hangfire Retry
* Thuộc tính trên class:
  ```csharp
  [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
  ```
* **Số lần thử:** Tối đa 3 lần.
* **Cách thức hoạt động:** Nếu trong quá trình tải ảnh, giải mã, hoặc upload lên MinIO xảy ra sự cố (ví dụ mạng rớt tạm thời hoặc MinIO quá tải), Hangfire sẽ giữ job trong hàng đợi và tự động kích hoạt retry theo khoảng cách thời gian tăng dần. Sau 3 lần vẫn thất bại, job sẽ chuyển sang trạng thái `Failed` trong Hangfire Dashboard để lập trình viên kiểm tra, không làm sập tiến trình máy chủ.

---

## Lưu trữ Object Storage (MinIO)
* Hai phiên bản ảnh mới được lưu trữ tại bucket `culinaryblog` với khóa định danh duy nhất do máy chủ kiểm soát:
  * `recipes/{recipeId}/images/{imageId}_medium.{ext}`
  * `recipes/{recipeId}/images/{imageId}_thumb.{ext}`
* Không bao giờ ghi đè lên ảnh gốc `OriginalUrl`.

---

## Cập nhật Database
* Thao tác cập nhật `MediumUrl` và `ThumbnailUrl` chỉ diễn ra khi và chỉ khi **cả hai ảnh** đã được upload thành công lên MinIO.
* Cập nhật kèm `UpdatedAt = DateTime.UtcNow`.

---

## Xử lý lỗi (Error Handling)
1. **RecipeImage không tồn tại hoặc bị xóa mềm (`IsDeleted = true`):** Ghi log cảnh báo (`LogWarning`) và kết thúc êm đềm (không ném exception để tránh kích hoạt retry vô ích).
2. **Ảnh gốc bị hỏng (Corrupt Image):** `ImageSharp` ném ngoại lệ khi giải mã. Hệ thống ghi log lỗi, không lưu URL rác và kích hoạt retry/fail.
3. **MinIO Upload thất bại:** Ném ngoại lệ mạng, không commit DB, đảm bảo tính toàn vẹn trạng thái.

---

## Tính lũy đẳng (Idempotency)
Nếu một job bị kích hoạt lại cho một `RecipeImage` đã có đủ `MediumUrl` và `ThumbnailUrl`, hàm kiểm tra và log thông tin:
`"RecipeImage {Id} đã có đầy đủ MediumUrl và ThumbnailUrl. Bỏ qua để đảm bảo tính Idempotent."`
Nhờ đó ngăn chặn việc xử lý dư thừa, tiết kiệm CPU và không sinh file rác trên MinIO.

---

## Cấu hình (Configuration)
Trong `appsettings.json`:
```json
"Storage": {
  "ServiceUrl": "http://localhost:9000",
  "AccessKey": "minioadmin",
  "SecretKey": "minioadmin",
  "Region": "us-east-1",
  "DefaultBucket": "culinaryblog"
}
```
*Lưu ý: Môi trường Production ghi đè qua biến môi trường. Không hardcode bí mật trong mã nguồn.*

---

## Build & Test
* **Lệnh Build:**
  ```bash
  dotnet build backend/CulinaryBlog.sln
  ```
  *Kết quả: 0 Error(s), Build thành công.*
* **Lệnh Chạy Test:**
  ```bash
  dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --filter FullyQualifiedName~ImageResizeJobTests
  ```
  *Kết quả: 10/10 Tests PASSED (Thời gian: ~3s).*

---

## Hướng dẫn Demo bằng ảnh thật
1. Khởi chạy MinIO và PostgreSQL:
   ```bash
   docker compose up -d
   ```
2. Khởi chạy Backend API:
   ```bash
   dotnet run --project backend/src/CulinaryBlog.Api
   ```
3. Truy cập Hangfire Dashboard tại: `http://localhost:5000/hangfire` (ở môi trường Development).
4. Quan sát hàng đợi `default`: Khi có ảnh mới được tải lên, job `ImageResizeJob.ProcessAsync` sẽ xuất hiện trong danh sách `Enqueued` $\rightarrow$ `Processing` $\rightarrow$ `Succeeded`.
5. Mở MinIO Console (`http://localhost:9001`) tại bucket `culinaryblog/recipes/{recipeId}/images/`: Thấy xuất hiện đủ 3 file:
   * `{id}.jpg` (Ảnh gốc)
   * `{id}_medium.jpg` (Ảnh 800px)
   * `{id}_thumb.jpg` (Ảnh 300px)
6. Kiểm tra Database PostgreSQL trong bảng `RecipeImages`: Cả 3 cột `OriginalUrl`, `MediumUrl`, `ThumbnailUrl` đều chứa đường dẫn đầy đủ.

---

## Hạn chế kỹ thuật (Limitations)
* **Định dạng AVIF:** Thư viện `SixLabors.ImageSharp` thuần C# hiện tại chưa hỗ trợ bộ giải mã (decoder) cho định dạng AVIF mặc định (yêu cầu thư viện native C++ như libheif). Nếu người dùng tải ảnh AVIF, hệ thống sẽ lưu trữ ảnh gốc nhưng tính năng Thumbnail Job sẽ từ chối giải mã và giữ nguyên URL gốc.

---

## Phần tiếp theo
* Tích hợp trigger kích hoạt job từ `AddRecipeImageCommand` và `UploadRecipeImageCommand` khi kết hợp nhánh `feat/vohungmanh-recipe-image`.
