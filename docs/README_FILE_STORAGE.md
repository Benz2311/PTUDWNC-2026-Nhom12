# Dịch vụ lưu trữ tệp tin MinIO Object Storage (File Storage)

## 1. Mục tiêu
Branch `feat/vohungmanh-file-storage` giải quyết việc xây dựng phân hệ lưu trữ tệp tin đối tượng (**Object Storage**) chuẩn công nghiệp cho nền tảng CulinaryBlog thuộc Task 4:
- Tách rời hoàn toàn việc lưu trữ dữ liệu nhị phân (Binary Large Objects - BLOBs) như hình ảnh công thức ra khỏi cơ sở dữ liệu quan hệ PostgreSQL, giúp database nhẹ nhàng, tối ưu hóa I/O và dễ sao lưu.
- Cung cấp tầng trừu tượng `IStorageService` tương thích chuẩn **AWS S3 API**, tích hợp trực tiếp với máy chủ lưu trữ mã nguồn mở **MinIO**.
- Triển khai đầy đủ hai nghiệp vụ cốt lõi theo đặc tả hệ thống:
  - **FILE-001 Upload File**: Tải lên tệp tin và trả về đường dẫn URL công khai.
  - **FILE-002 Delete File**: Xóa tệp tin với tính chất lũy đẳng (**Idempotency**).
- Thiết lập cơ chế phòng vệ chống tấn công vượt quyền thư mục (**Path Traversal Protection**), quy tắc đặt tên khóa đối tượng duy nhất (**Unique Object Key**), và xử lý lỗi kiên cường (**Resilient Error Handling**) ánh xạ sang mã lỗi `HTTP 503 Service Unavailable` khi dịch vụ lưu trữ gặp sự cố.

---

## 2. Kết quả đạt được
Sau khi triển khai branch này:
- **Tầng trừu tượng `IStorageService` độc lập**:
  - Hỗ trợ tải luồng dữ liệu (`Stream`), xóa file, lấy URL công khai và sinh URL có chữ ký bảo mật giới hạn thời gian (**Presigned URL**).
  - Khả năng chuyển đổi linh hoạt giữa các nhà cung cấp lưu trữ đám mây (AWS S3, Cloudflare R2, MinIO on-premise) mà không cần thay đổi logic nghiệp vụ tầng Application.
- **Tiện ích an toàn đường dẫn `StoragePathHelper`**:
  - Tự động làm sạch và kiểm tra Object Key: chặn đứng các chuỗi tấn công Path Traversal (`..`, `/`, `\`, ký tự điều khiển, ký tự ổ đĩa `:`).
  - Quy chuẩn hóa cấu trúc Object Key: `recipes/{recipeId}/images/{fileId}{extension}`, ngăn chặn hoàn toàn nguy cơ trùng lặp tên file hoặc ghi đè dữ liệu người dùng khác.
- **Tính lũy đẳng khi xóa tệp (Idempotent Deletion)**:
  - Khi yêu cầu xóa một tệp không tồn tại (hoặc đã bị xóa trước đó) trên MinIO, hệ thống không báo lỗi sập ứng dụng mà phản hồi thành công êm đềm (`HTTP 204 / return true`).
- **Middleware xử lý lỗi chuyên biệt `ExceptionHandlingMiddleware`**:
  - Bắt ngoại lệ `StorageServiceUnavailableException` (khi MinIO ngắt kết nối hoặc mạng chập chờn) và phản hồi `HTTP 503 Service Unavailable` kèm thông báo JSON thân thiện, che giấu tuyệt đối các thông tin nhạy cảm (AccessKey, SecretKey, Hostname nội bộ).
  - Bắt ngoại lệ `InvalidStoragePathException` và phản hồi `HTTP 400 Bad Request`.
- **Kiểm thử bao phủ**: 43/43 tests chuyên biệt trong 3 test suites (`S3StorageServiceTests`, `StoragePathHelperTests`, `ExceptionHandlingMiddlewareTests`) và 130/131 tests toàn hệ thống đều vượt qua thành công (100% Pass).

---

## 3. Luồng hoạt động

```text
Client / Application Service (Ví dụ: RecipeImageCommandHandler)
  │ (Gửi Stream nhị phân + ObjectKey hoặc Tên file)
  ▼
StoragePathHelper (Application / Utilities)
  │ ├─ 1. SanitizeKey:
  │ │    Kiểm tra ký tự điều khiển, drive letter ':', ký tự '../'
  │ │    Ném InvalidStoragePathException nếu phát hiện Path Traversal
  │ │
  │ └─ 2. GenerateUniqueKey:
  │      Sinh objectKey = recipes/{recipeId}/images/{fileId}.jpg
  ▼
Infrastructure Layer: S3StorageService (AWS SDK S3 -> MinIO)
  │ ├─ Kiểm tra và tải luồng nhị phân: PutObjectRequest
  │ ├─ Bọc trong cơ chế tự động thử lại (Retry) khi mạng chập chờn
  │ └─ Sinh URL công khai từ ServiceUrl hoặc PublicBaseUrl
  ▼
MinIO Object Storage Server (Docker Container: culinaryblog-minio)
  │ ├─ Lưu các byte dữ liệu nhị phân vào Bucket: culinaryblog
  │ └─ Gán Metadata (Content-Type, ETag, Size)
  ▼
Xử lý lỗi kiên cường (ExceptionHandlingMiddleware):
  │ ├─ Nếu MinIO sập hoặc timeout kết nối:
  │ │    Bắt AmazonS3Exception / TimeoutException
  │ │    Ném StorageServiceUnavailableException
  │ └─ Middleware chuyển thành HTTP 503 Service Unavailable an toàn
  ▼
Output
  └─ Trả về Public URL: http://localhost:9000/culinaryblog/recipes/{recipeId}/images/{fileId}.jpg
```

---

## 4. Các file chính

| File | Vai trò | Xử lý gì |
| :--- | :--- | :--- |
| `backend/src/CulinaryBlog.Application/Interfaces/IStorageService.cs` | Application / Interface | Khai báo hợp đồng dịch vụ lưu trữ: `UploadAsync`, `DeleteAsync`, `GetPresignedUrlAsync`, hỗ trợ cả bucket tùy biến và bucket mặc định. |
| `backend/src/CulinaryBlog.Infrastructure/Storage/S3StorageService.cs` | Infrastructure / Service | Triển khai `IStorageService` giao tiếp với MinIO qua AWS SDK S3: tải stream lên bucket, xóa tệp idempotent, sinh public URL, và retry khi lỗi mạng. |
| `backend/src/CulinaryBlog.Infrastructure/Storage/StorageSettings.cs` | Infrastructure / Options | Cấu hình lưu trữ: `ServiceUrl`, `AccessKey`, `SecretKey`, `Region`, `DefaultBucket`, `PublicBaseUrl`. |
| `backend/src/CulinaryBlog.Application/Common/Utilities/StoragePathHelper.cs` | Application / Utility | Tiện ích làm sạch object key, chặn Path Traversal (`..`, `/`), và sinh khóa duy nhất `recipes/{recipeId}/images/{fileId}.ext`. |
| `backend/src/CulinaryBlog.Application/Exceptions/StorageExceptions.cs` | Application / Exceptions | Định nghĩa các ngoại lệ đặc tả: `StorageServiceUnavailableException` (lỗi 503) và `InvalidStoragePathException` (lỗi 400). |
| `backend/src/CulinaryBlog.Api/Middleware/ExceptionHandlingMiddleware.cs` | API / Middleware | Bắt toàn bộ ngoại lệ lưu trữ, ánh xạ mã trạng thái HTTP 503/400 chuẩn RFC 7807, ngăn chặn rò rỉ credential MinIO ra client. |
| `backend/tests/CulinaryBlog.UnitTests/Infrastructure/Storage/S3StorageServiceTests.cs` | Unit Tests | 19 kịch bản kiểm thử: upload thành công, xóa idempotent, presigned URL, retry khi timeout, ném lỗi khi MinIO sập. |
| `backend/tests/CulinaryBlog.UnitTests/Application/Common/StoragePathHelperTests.cs` | Unit Tests | 16 kịch bản kiểm thử: chặn ký tự `..`, chặn đường dẫn ổ đĩa, chặn ký tự null, sinh key đúng cấu trúc. |
| `backend/tests/CulinaryBlog.UnitTests/API/ExceptionHandlingMiddlewareTests.cs` | Unit Tests | 8 kịch bản kiểm thử: bắt ngoại lệ lưu trữ, kiểm tra mã HTTP 503, kiểm tra format RFC 7807, che giấu secrets. |
| `docs/VO_HUNG_MANH_FILE_STORAGE_COMPLAN.md` | Tài liệu bảo vệ | Báo cáo giải trình chuyên sâu (350 dòng) về kiến trúc Object Storage, so sánh MinIO vs PostgreSQL, và câu hỏi vấn đáp. |
| `docs/README_FILE_STORAGE.md` | Tài liệu kỹ thuật | Tài liệu hướng dẫn kỹ thuật chi tiết theo chuẩn 13 phần. |

---

## 5. API / Interface

### Hợp đồng `IStorageService`

```csharp
namespace CulinaryBlog.Application.Interfaces;

public interface IStorageService
{
    // Upload file lên bucket chỉ định và trả về public URL
    Task<string> UploadAsync(
        string bucketName,
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken ct = default);

    // Xóa file khỏi bucket chỉ định (Lũy đẳng - Idempotent)
    Task DeleteAsync(string bucketName, string objectKey, CancellationToken ct = default);

    // Sinh presigned URL có thời hạn truy cập tạm thời
    Task<string> GetPresignedUrlAsync(
        string bucketName,
        string objectKey,
        TimeSpan expiry,
        CancellationToken ct = default);

    // Upload file sử dụng bucket mặc định ("culinaryblog")
    Task<string> UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken ct = default);

    // Xóa file sử dụng bucket mặc định ("culinaryblog")
    Task DeleteAsync(string objectKey, CancellationToken ct = default);
}
```

### Điểm cuối API liên quan

| Method | Endpoint | Input | Output | Authorization |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/recipes/{id}/images` | File Stream / URL qua Multipart Form hoặc JSON | HTTP 201 Created<br>Body: `RecipeImageDto` (chứa `originalUrl` trỏ tới MinIO) | Yêu cầu đăng nhập; Tác giả công thức hoặc Admin. |
| `DELETE` | `/api/v1/recipes/{recipeId}/images/{imageId}` | Path: `recipeId`, `imageId` | HTTP 204 No Content (Xóa mềm metadata và trigger xóa tệp) | Yêu cầu đăng nhập; Tác giả công thức hoặc Admin. |

---

## 6. Business Rules

1. **FILE-001 Upload File**:
   - Dữ liệu nhị phân của tệp tin phải được lưu trữ trực tiếp vào Object Storage (MinIO) bên trong bucket được cấu hình (`culinaryblog`).
   - Đường dẫn công khai (`PublicUrl`) trả về có định dạng chuẩn: `{ServiceUrl}/{BucketName}/{ObjectKey}` hoặc sử dụng CDN Base URL nếu có cấu hình.
2. **FILE-002 Delete File & Idempotency**:
   - Thao tác xóa tệp tin phải mang tính lũy đẳng: nếu tệp tin không tồn tại trên MinIO (do đã xóa trước đó hoặc chưa từng được upload), dịch vụ vẫn kết thúc êm đềm và không ném ngoại lệ sập luồng.
3. **Phòng chống tấn công Path Traversal**:
   - `StoragePathHelper.SanitizeKey` kiểm tra nghiêm ngặt:
     - Không cho phép chứa phân đoạn `..` hoặc `.`.
     - Không cho phép bắt đầu bằng ký tự gạch chéo `/`.
     - Không chứa ký tự phân tách ổ đĩa `:`.
     - Không chứa các ký tự điều khiển (Control Characters như `\0`, `\r`, `\n`).
     - Tự động chuẩn hóa dấu gạch chéo ngược Windows `\` thành gạch chéo xuôi `/`.
4. **Quy tắc sinh Object Key duy nhất**:
   - Hệ thống không sử dụng tên file nguyên bản của người dùng để lưu trữ trên MinIO (tránh ghi đè file có cùng tên như `image.jpg`).
   - Cấu trúc chuẩn hóa: `recipes/{recipeId}/images/{fileId}{extension}` với `fileId` là một `Guid.NewGuid()`.
5. **Cơ chế kiên cường và Che giấu bí mật (Resiliency & Security)**:
   - Khi dịch vụ MinIO gặp sự cố hoặc timeout kết nối, `ExceptionHandlingMiddleware` bắt ngoại lệ và trả về `HTTP 503 Service Unavailable`.
   - Thông điệp phản hồi tuyệt đối không để lộ `AccessKey`, `SecretKey`, chuỗi kết nối hay IP nội bộ của máy chủ MinIO.

---

## 7. Ví dụ hoạt động

### Kịch bản: Tải lên và Lưu trữ ảnh cho món "Phở bò Hà Nội"

1. **Input từ tầng ứng dụng**:
   - `recipeId = "9a7f3e1b-0000-0000-0000-000000000001"`
   - `fileName = "to-pho-dac-biet.jpg"`
   - `content = MemoryStream` chứa 2.4 MB dữ liệu JPEG
   - `contentType = "image/jpeg"`

2. **Xử lý qua StoragePathHelper & S3StorageService**:
   - `StoragePathHelper.GenerateRecipeImageKey`:
     - Khóa sinh ra: `recipes/9a7f3e1b-0000-0000-0000-000000000001/images/f47ac10b-58cc-4372-a567-0e02b2c3d479.jpg`
   - `S3StorageService.UploadAsync`:
     - Gửi `PutObjectRequest` đến MinIO bucket `culinaryblog`.
     - MinIO ghi nhận luồng byte, tạo ETag và phản hồi HTTP 200 OK.
     - Dịch vụ trả về URL: `http://localhost:9000/culinaryblog/recipes/9a7f3e1b-.../images/f47ac10b-...jpg`.

3. **Output lưu vào Database PostgreSQL**:
   - Bảng `RecipeImages` lưu đường dẫn URL trên vào trường `OriginalUrl`.
   - Database chỉ tốn vài chục byte để lưu chuỗi string thay vì phải gánh 2.4 MB dữ liệu nhị phân.

---

## 8. Error Handling

| Tình huống ngoại lệ | Mã lỗi HTTP | Phản hồi (Behavior & JSON Payload) |
| :--- | :--- | :--- |
| **Path Traversal Attack**: Object key chứa `../../etc/passwd` hoặc bắt đầu bằng `/`. | `HTTP 400 Bad Request` | Trả về `ProblemDetails` RFC 7807:<br>```json<br>{<br>  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",<br>  "title": "Invalid Storage Path",<br>  "status": 400,<br>  "detail": "Object key phát hiện nguy cơ Path Traversal (chứa '..'): ../../secret.jpg"<br>}<br>``` |
| **MinIO Storage Down**: Container MinIO bị dừng, mất điện hoặc sai mật khẩu kết nối. | `HTTP 503 Service Unavailable` | Trả về `ProblemDetails` RFC 7807 an toàn, che giấu secrets:<br>```json<br>{<br>  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.4",<br>  "title": "Service Unavailable",<br>  "status": 503,<br>  "detail": "Dịch vụ lưu trữ tệp (Object Storage) hiện không khả dụng. Vui lòng thử lại sau."<br>}<br>``` |
| **Xóa tệp không tồn tại**: File đã bị xóa trên MinIO từ trước. | `HTTP 204 No Content` | Hệ thống bắt lỗi 404 từ S3 và bỏ qua, phản hồi thành công (Idempotent Deletion). |

---

## 9. Cách chạy và Demo

### Bước 1: Khởi động MinIO và Tạo Bucket bằng Docker
```bash
docker compose up -d minio
```
- MinIO API lắng nghe tại: `http://localhost:9000`
- MinIO Web Console tại: `http://localhost:9001` (User: `minioadmin`, Pass: `minioadmin`)

### Bước 2: Cấu hình trong `appsettings.json`
```json
{
  "Storage": {
    "ServiceUrl": "http://localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "Region": "us-east-1",
    "DefaultBucket": "culinaryblog"
  }
}
```

### Bước 3: Kịch bản Demo thực tế cho Giảng viên

1. **Demo Tải file thành công lên MinIO**:
   - Khởi động Backend API: `dotnet run --project backend/src/CulinaryBlog.Api`.
   - Gửi yêu cầu tải ảnh công thức qua API `POST /api/v1/recipes/{id}/images`.
   - Mở MinIO Console (`http://localhost:9001`) tại bucket `culinaryblog`: Chỉ cho Giảng viên thấy thư mục `recipes/{recipeId}/images/` xuất hiện file ảnh thật với đúng kích thước và metadata.
2. **Demo Chống Path Traversal**:
   - Sử dụng unit test hoặc gọi phương thức với object key `../../boot.ini`.
   - Quan sát `StoragePathHelper` lập tức ném `InvalidStoragePathException` và API chặn ngay với mã `HTTP 400 Bad Request`.
3. **Demo Xử lý lỗi kiên cường (503 Service Unavailable)**:
   - Tạm dừng container MinIO: `docker stop culinaryblog-minio`.
   - Gửi lại request upload ảnh: Hệ thống không bị crash tiến trình, mà phản hồi ngay lập tức `HTTP 503 Service Unavailable` với JSON thông báo rõ ràng: `"Dịch vụ lưu trữ tệp (Object Storage) hiện không khả dụng"`.
   - Bật lại MinIO: `docker start culinaryblog-minio` $\rightarrow$ API tự động phục hồi và upload thành công.

---

## 10. Testing

### Bộ kiểm thử chuyên biệt phân hệ Storage (3 Suites - 43 Tests)
Chạy lệnh kiểm thử đơn vị:
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --filter "FullyQualifiedName~Storage|FullyQualifiedName~ExceptionHandling"
```

**Kết quả kiểm thử thực tế:**
- **43/43 tests PASSED (100%)** (Thời gian chạy: ~400ms).
- **Phân bổ 3 bộ kiểm thử chi tiết:**
  1. `S3StorageServiceTests` (19 tests):
     - Upload file thành công trả về đúng URL cấu trúc.
     - Xóa file idempotent không ném lỗi khi S3 trả về 404 NoSuchKey.
     - Sinh Presigned URL với thời hạn hợp lệ.
     - Tự động thử lại (Retry) khi gặp sự cố mạng tạm thời.
     - Bắt lỗi ngoại lệ và ném `StorageServiceUnavailableException` khi S3 sập hoàn toàn.
  2. `StoragePathHelperTests` (16 tests):
     - Sanitize loại bỏ khoảng trắng thừa và chuẩn hóa `\` thành `/`.
     - Chặn các chuỗi Path Traversal `..` ở đầu, giữa hoặc cuối đường dẫn.
     - Chặn các đường dẫn bắt đầu bằng dấu `/`.
     - Chặn ký tự ổ đĩa Windows `:` và ký tự điều khiển.
     - Sinh Unique Key đúng định dạng `recipes/{recipeId}/images/{fileId}.ext`.
  3. `ExceptionHandlingMiddlewareTests` (8 tests):
     - Bắt `StorageServiceUnavailableException` và ghi HTTP 503.
     - Bắt `InvalidStoragePathException` và ghi HTTP 400.
     - Đảm bảo header `Content-Type: application/problem+json`.
     - Bảo vệ an toàn thông tin: không để lọt access key hay stack trace ra ngoài response body.

### Tổng hợp Unit Tests toàn bộ solution:
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj
```
**Kết quả thực tế:**
- `Passed: 130, Failed: 0, Skipped: 1, Total: 131` (100% Pass).

---

## 11. Build
Thực hiện lệnh biên dịch solution:
```bash
dotnet build backend/CulinaryBlog.sln
```

**Kết quả biên dịch thực tế:**
- **Thành công (Exit code 0)**.
- **0 Error(s)**, 428 Warning(s) (chủ yếu là quy tắc format StyleCop).

---

## 12. Limitations

1. **Phạm vi xử lý tệp tin**:
   - Hiện tại hệ thống tập trung tối ưu cho các tệp hình ảnh công thức nấu ăn (dung lượng $\le 5\text{ MB}$). Chưa hỗ trợ Multipart Chunked Upload cho các tệp video dung lượng cực lớn ($> 100\text{ MB}$).
2. **Kênh truyền tải trực tiếp**:
   - Luồng upload hiện tại đi qua API Server làm trung gian trước khi tới MinIO. Trong tương lai với lượng người dùng lớn, có thể mở rộng cơ chế Client tải trực tiếp lên MinIO thông qua Presigned URL để giảm tải băng thông cho API Server.

---

## 13. Kết luận
Branch `feat/vohungmanh-file-storage` đã hoàn thành xuất sắc và toàn diện phân hệ Lưu trữ tệp tin đối tượng:
- Thiết kế chuẩn mẫu kiến trúc Clean Architecture với interface `IStorageService` và triển khai `S3StorageService` tương thích chuẩn AWS S3 / MinIO.
- Cơ chế bảo mật vững chắc chống Path Traversal và kiểm soát tên file duy nhất.
- Khả năng chịu lỗi và phản hồi thân thiện với mã HTTP 503 khi hạ tầng lưu trữ gặp sự cố.
- Bộ 43 unit test cases đạt tỷ lệ vượt qua 100% và biên dịch sạch sẽ 0 lỗi.
