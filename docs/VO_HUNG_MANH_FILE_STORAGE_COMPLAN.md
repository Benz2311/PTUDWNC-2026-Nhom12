# BÁO CÁO GIẢI TRÌNH BẢO VỆ ĐỒ ÁN (COMPLAN)
## PHÂN HỆ FILE STORAGE / MINIO OBJECT STORAGE (TASK 4)
**Sinh viên thực hiện:** Võ Hùng Mạnh  
**Branch:** `feat/vohungmanh-file-storage`  
**Dự án:** CulinaryBlog - Nền tảng chia sẻ công thức nấu ăn  

---

## 1. TỔNG QUAN VÀ BẢN CHẤT KIẾN TRÚC FILE STORAGE

### A. MinIO là gì? So sánh trực quan với PostgreSQL (Ví dụ Món Phở Bò)
* **MinIO là gì?** MinIO là hệ thống lưu trữ đối tượng mã nguồn mở (Object Storage) tương thích chuẩn AWS S3 API, được thiết kế tối ưu để lưu trữ dữ liệu phi cấu trúc (Unstructured Data) như hình ảnh, video, tài liệu, và bản sao lưu với tốc độ cực cao.
* **So sánh với PostgreSQL qua ví dụ món "Phở bò truyền thống":**
  * **PostgreSQL (Cơ sở dữ liệu quan hệ - Relational Database):** Đóng vai trò là "Sổ quản lý danh mục". Nó lưu trữ các thông tin có cấu trúc (metadata), bao gồm: `Id` (GUID), `Title` ("Phở bò Hà Nội"), `RecipeId`, `IsPrimary` (true), `OrderIndex` (1), kích thước file (2.4 MB), thời gian tải lên và đặc biệt là **chuỗi đường dẫn URL** trỏ tới file ảnh (`http://localhost:9000/culinaryblog/recipes/123/images/abc-xyz.jpg`). Nếu lưu trực tiếp file nhị phân (BLOB / bytea) vào PostgreSQL, dung lượng database sẽ phình to nhanh chóng, làm giảm hiệu năng lập chỉ mục (index), sao lưu (backup) chậm chạp và tiêu tốn nhiều RAM buffer của database server.
  * **MinIO (Object Storage):** Đóng vai trò là "Kho chứa hàng thực tế". Nơi đây trực tiếp giữ các byte dữ liệu nhị phân của bức ảnh tô phở thơm ngon. MinIO chịu tải việc truyền tải file dung lượng lớn (I/O streaming) trực tiếp tới người dùng hoặc Content Delivery Network (CDN) mà không làm nghẽn Database chính.

---

### B. Object Storage là gì?
Khác với File Storage truyền thống (tổ chức theo cây thư mục ổ đĩa có phân cấp cứng nhắc của hệ điều hành như `C:\Windows\...`) hay Block Storage (chia nhỏ ổ đĩa thành các block 4KB thô), **Object Storage** quản lý dữ liệu dưới dạng các "đối tượng" (Objects) độc lập trong một không gian phẳng (Flat Namespace). Mỗi đối tượng gồm có 3 phần:
1. **Data:** Dữ liệu nhị phân thực tế (nội dung bức ảnh JPEG/PNG).
2. **Key:** Khóa định danh duy nhất (chuỗi string dùng để tra cứu).
3. **Metadata:** Thông tin mô tả đối tượng (Content-Type, kích thước, thời gian tạo, ETag MD5 hash).

---

### C. Bucket là gì?
**Bucket** (nghĩa đen: chiếc xô, thùng chứa) là vùng không gian lưu trữ cấp cao nhất trong MinIO/S3, đóng vai trò như một kho chứa logic riêng biệt.
* Trong dự án CulinaryBlog, chúng ta sử dụng bucket mặc định có tên: `culinaryblog`.
* Mọi file tải lên của toàn bộ hệ thống đều nằm trong bucket này và được phân nhóm logic thông qua tiền tố (prefix) của Object Key.

---

### D. Object Key là gì? Cấu trúc phân cấp logic
Trong Object Storage, không có khái niệm "thư mục con" vật lý như trên ổ cứng. Ký tự gạch chéo `/` chỉ là một phần của chuỗi định danh (Object Key) để tạo cảm giác phân cấp logic.
* **Cấu trúc Object Key chuẩn của hệ thống CulinaryBlog:**
```text
culinaryblog (Bucket Name)
└── recipes/
    └── {recipeId}/
        └── images/
            └── {unique-generated-guid}.jpg
```
* **Ví dụ thực tế:**  
  `recipes/a4f3c218-50e3-4f27-b673-8a3d162f4bc9/images/9c8b7a6e5d4c3b2a10987654321fedcb.jpg`

---

### E & F. Vai trò của `IStorageService` và `S3StorageService`
* **`IStorageService` (Tầng Application - `CulinaryBlog.Application.Interfaces`):** Là hợp đồng trừu tượng (Interface Abstraction). Tầng Application chỉ định nghĩa nghiệp vụ: "Tôi cần tải lên luồng file này (UploadAsync), tôi cần xóa file này (DeleteAsync)". Tầng Application hoàn toàn không biết và không phụ thuộc vào việc file được lưu ở MinIO, AWS S3 thật, Google Cloud Storage hay Local Disk.
* **`S3StorageService` (Tầng Infrastructure - `CulinaryBlog.Infrastructure.Storage`):** Là lớp triển khai cụ thể (Concrete Implementation) sử dụng thư viện `AWSSDK.S3` để kết nối vào MinIO server qua giao thức HTTP REST API S3-compatible.

---

### G. Tại sao dùng Interface thay vì gọi MinIO trực tiếp từ Endpoint?
1. **Tuân thủ nguyên lý đảo ngược phụ thuộc (Dependency Inversion Principle - chữ D trong SOLID):** Module cấp cao (Application/API) không phụ thuộc trực tiếp vào module cấp thấp (Infrastructure / AWS SDK). Cả hai đều phụ thuộc vào trừu tượng (`IStorageService`).
2. **Khả năng kiểm thử (Testability & Mocking):** Cho phép dễ dàng viết Unit Test bằng `Moq` giả lập `IStorageService` hoặc `IAmazonS3` mà không cần bật MinIO server thật, đảm bảo test suite chạy trong mili-giây trên CI/CD pipeline.
3. **Tính linh hoạt (Maintainability):** Khi dự án chuyển từ chạy MinIO trên máy chủ nội bộ (on-premise) lên AWS S3 trên Cloud hoặc Azure Blob Storage, chúng ta chỉ cần viết một Service mới hoặc đổi cấu hình trong `appsettings.json`, hoàn toàn không phải sửa một dòng code nào trong Controller/Endpoint hay Business Logic.

---

### H. Luồng xử lý Upload Stream
Hệ thống sử dụng cơ chế truyền luồng (Stream) từ HTTP Request trực tiếp tới MinIO để tối ưu bộ nhớ RAM:

```text
[ Client (Trình duyệt/Mobile App) ]
               │
               │ HTTP Multipart Form-Data Stream
               ▼
[ CulinaryBlog.API (Endpoint / Controller) ]
               │
               │ Stream (không nạp toàn bộ vào byte[])
               ▼
[ IStorageService (Interface trừu tượng) ]
               │
               ▼
[ S3StorageService (Infrastructure) ]
               │  - SanitizeKey chống Path Traversal
               │  - ExecuteWithRetryAsync (tự động thử lại nếu lỗi tạm thời)
               │  - Gán PutObjectRequest (InputStream = content, DisablePayloadSigning = true)
               ▼
[ MinIO / Object Storage Server (Port 9000) ]
               │
               │ Trả về HTTP 200 OK + ETag
               ▼
[ Trả về Public URL cho Caller / Database lưu trữ ]
```

---

### I. Tại sao không lưu nguyên tên file của Client (`anh-pho-bo.jpg`)?
Nếu máy chủ lưu trực tiếp tên file do Client gửi lên:
1. **Xung đột tên file (Collision):** Người dùng A tải lên `anh-pho-bo.jpg`, người dùng B cũng tải lên `anh-pho-bo.jpg` -> bức ảnh của người dùng A sẽ bị ghi đè (overwrite), dẫn đến mất dữ liệu.
2. **Lỗ hổng tấn công Path Traversal:** Client gửi file tên `../../../../etc/passwd` hoặc `..\\windows\\system32\\cmd.exe`.
3. **Ký tự đặc biệt gây lỗi URL:** Client đặt tên file có dấu tiếng Việt, khoảng trắng, ký tự unicode lạ (như `ảnh phở bò & chả cá #1.jpg`) gây lỗi khi phân giải URL trên trình duyệt web.
4. **Giải pháp trong code:** Server tự sinh định danh duy nhất ngẫu nhiên (UUIDv4/GUID không chứa dấu gạch nối) thông qua `StoragePathHelper.GenerateRecipeImageKey(recipeId, extension)`. Tên file client gửi lên chỉ được trích xuất lấy phần mở rộng an toàn (`.jpg`, `.png`, `.webp`).

---

### J. Tấn công Path Traversal là gì? Cơ chế phòng vệ
* **Bản chất:** Path Traversal (vượt qua đường dẫn thư mục) là kỹ thuật tấn công chèn các chuỗi đại diện cho thư mục cha (`../` trên Linux, `..\` trên Windows) hoặc đường dẫn tuyệt đối (`/etc/...`, `C:\...`) vào tham số đường dẫn tệp tin. Mục tiêu là truy cập hoặc ghi đè các tệp nhạy cảm nằm ngoài phạm vi thư mục được chỉ định.
* **Ví dụ:** Kẻ tấn công yêu cầu xóa object key: `recipes/../../appsettings.Production.json`. Nếu không kiểm tra, câu lệnh xóa có thể ảnh hưởng tới các tài nguyên khác.
* **Code thực tế xử lý trong `StoragePathHelper.SanitizeKey(key)`:**
  1. Kiểm tra rỗng/khoảng trắng (`IsNullOrWhiteSpace`).
  2. Chặn ký tự điều khiển (`char.IsControl`), ký tự null byte `\0`.
  3. Chặn ký tự ổ đĩa Windows `:` (`C:\...`).
  4. Chuẩn hóa dấu gạch chéo ngược Windows `\` thành `/`.
  5. Chặn đường dẫn bắt đầu bằng `/` (đường dẫn tuyệt đối).
  6. Phân tích từng đoạn (segment) qua `Split('/')`, nếu có bất kỳ segment nào bằng `..` hoặc `.` -> ném `ArgumentException` ngay lập tức.
  7. Kiểm tra sự tồn tại của chuỗi con `..` trong toàn bộ key.

---

### K. Tính lũy đẳng (Idempotence) trong thao tác Xóa (Delete)
* **Khái niệm:** Một thao tác được gọi là "lũy đẳng" (Idempotent) nếu việc thực hiện nó 1 lần hay nhiều lần liên tiếp đều dẫn đến cùng một trạng thái hệ thống mong muốn mà không gây ra tác dụng phụ hoặc lỗi không cần thiết.
* **Áp dụng vào File Storage:**
  * **Lần gọi DELETE 1:** Object `recipes/123/images/img1.jpg` tồn tại trên MinIO -> MinIO xóa file thành công -> Trạng thái: File không còn trên kho.
  * **Lần gọi DELETE 2 (hoặc do mạng lag gửi lại request):** Object đã không còn tồn tại -> Trạng thái hiện tại: File không còn trên kho. Mục tiêu mong muốn ("File không còn trên kho") đã đạt được!
* **Code thực tế trong `S3StorageService.DeleteAsync`:**
  Nếu MinIO trả về lỗi `AmazonS3Exception` với mã lỗi `NotFound` hoặc `NoSuchKey`, hàm bắt ngoại lệ này, ghi nhận log debug và xem như thao tác xóa đã hoàn tất thành công. Hệ thống **không** ném lỗi ra ngoài làm sập luồng nghiệp vụ.

---

### L. Cơ chế Retry có giới hạn (Bounded Retry với Exponential Backoff)
* **Tại sao cần Retry?** Khi giao tiếp qua mạng (HTTP) với MinIO, có thể xảy ra các sự cố tạm thời (Transient Faults) như nghẽn mạng ngắn hạn, MinIO server quá tải trong vài mili-giây, hoặc socket timeout.
* **Tại sao không retry vô hạn?** Retry vô hạn sẽ gây hiện tượng cạn kiệt tài nguyên (Thread Pool Starvation), treo luồng của ứng dụng và gây bão request (Retry Storm) khiến MinIO server không bao giờ có cơ hội phục hồi.
* **Cấu hình thuật toán trong `S3StorageService.cs`:**
  * **Số lần thử tối đa (`MaxRetries`):** 3 lần.
  * **Khoảng thời gian chờ (`BaseDelayMs`):** 100ms.
  * **Công thức lũy tiến nhị phân:** $\text{delay} = \text{BaseDelayMs} \times 2^{\text{attempt} - 1}$.
    * Lần 1 thất bại $\rightarrow$ Chờ $100\text{ms} \times 2^0 = 100\text{ms}$.
    * Lần 2 thất bại $\rightarrow$ Chờ $100\text{ms} \times 2^1 = 200\text{ms}$.
    * Lần 3 thất bại $\rightarrow$ Kiệt số lần thử $\rightarrow$ Ném `StorageUnavailableException`.
  * **Phân loại lỗi:**
    * *Lỗi tạm thời (Transient Errors - Cho phép retry):* Lỗi mạng (`HttpRequestException`, `SocketException`, `TimeoutException`), lỗi máy chủ MinIO 500, 502, 503, 504, 408 (RequestTimeout), hoặc mã lỗi `SlowDown`, `ServiceUnavailable`.
    * *Lỗi vĩnh viễn (Permanent Errors - Dừng ngay lập tức, KHÔNG retry):* 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found, `ArgumentException` (lỗi đầu vào).
    * *Huỷ bỏ (Cancellation):* Nếu `CancellationToken.IsCancellationRequested` bằng true, dừng vòng lặp ngay lập tức và ném `OperationCanceledException`.

---

### M. Ánh xạ lỗi HTTP 503 (Service Unavailable) và Problem Details
* **Phân biệt mã HTTP Status Code:**
  * **400 Bad Request:** Lỗi do phía Client gửi dữ liệu sai cấu trúc hoặc vi phạm định dạng.
  * **404 Not Found:** Tài nguyên được yêu cầu không tồn tại trong hệ sinh thái.
  * **500 Internal Server Error:** Lỗi logic không mong muốn trong mã nguồn backend.
  * **503 Service Unavailable:** Máy chủ backend vẫn sống, nhưng một dịch vụ phụ thuộc quan trọng (ở đây là MinIO Object Storage) tạm thời bị sập, không thể kết nối hoặc quá tải.
* **Chuẩn Problem Details (RFC 7807):**
  Khi MinIO sập hoặc mạng ngắt kết nối, `S3StorageService` ném ra `StorageUnavailableException`. `ExceptionHandlingMiddleware` bắt ngoại lệ này và trả về client response chuẩn RESTful:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.4",
  "title": "Service Unavailable",
  "status": 503,
  "detail": "Dịch vụ lưu trữ tệp (MinIO) tạm thời không khả dụng sau 3 lần thử (Upload: culinaryblog/recipes/123/images/sample.jpg)."
}
```
* **Bảo mật:** Không bao giờ để lộ Connection String, AccessKey, SecretKey hay Stack Trace ra ngoài response của Client.

---

### N & Q. Bảo mật thông tin xác thực (Credentials) & Cấu hình (Settings)
* **Các tham số cấu hình trong `appsettings.json`:**
  * `ServiceUrl`: Địa chỉ MinIO server (ví dụ `http://localhost:9000`).
  * `AccessKey`: Tên tài khoản định danh quản trị lưu trữ (đọc từ biến môi trường/file config, mặc định trong môi trường dev là `minioadmin`).
  * `SecretKey`: Mật mã bí mật kết nối (tương tự password).
  * `Region`: Vùng AWS S3 (`us-east-1` cho MinIO).
  * `DefaultBucket`: Tên bucket lưu trữ (`culinaryblog`).
* **Quy tắc an toàn tuyệt đối:**
  * Không hard-code `AccessKey` và `SecretKey` vào mã nguồn C#.
  * Trong các file log của Serilog và thông báo ngoại lệ (Exception Message), không bao giờ in giá trị `SecretKey`.

---

### O. Đăng ký Dependency Injection (DI)
Trong `CulinaryBlog.Infrastructure/DependencyInjection.cs`:
```csharp
// Đăng ký IStorageService với vòng đời Singleton
services.AddSingleton<IStorageService, S3StorageService>();
```
* **Tại sao là Singleton?** `IAmazonS3` client quản lý kết nối HTTP nội bộ thông qua `HttpClientFactory` và các thread pool an toàn (thread-safe). Việc dùng `Singleton` giúp tái sử dụng socket connection, tránh cạn kiệt cổng (TCP Socket Exhaustion) khi có nhiều request đồng thời.

---

### P. CancellationToken
* **Cơ chế:** Mọi phương thức I/O bất đồng bộ trong `IStorageService` và `S3StorageService` đều nhận tham số `CancellationToken ct = default`.
* **Tác dụng:** Nếu người dùng hủy tải lên (ví dụ tắt trình duyệt hoặc bấm nút Cancel), CancellationToken sẽ gửi tín hiệu kích hoạt. Quá trình truyền luồng file và các lần retry đang chờ trong `Task.Delay` sẽ bị hủy ngay lập tức, giải phóng băng thông mạng và tài nguyên máy chủ.

---

## 2. BẢNG TỔNG HỢP CÁC FILE ĐÃ TRIỂN KHAI

| File | Layer | Class / Interface | Chức năng | Vì sao sửa / tạo |
| :--- | :--- | :--- | :--- | :--- |
| `IStorageService.cs` | Application | `IStorageService` | Khai báo các hợp đồng lưu trữ tệp tin trừu tượng (Upload, Delete, PresignedUrl) | Mở rộng các overload tiện ích với `DefaultBucket` |
| `StoragePathHelper.cs` | Application | `StoragePathHelper` | Sinh UUID key duy nhất, làm sạch đường dẫn, chống Path Traversal, whitelist extension | Tạo mới để đảm bảo tính an toàn và tính duy nhất của file |
| `StorageExceptions.cs` | Application | `StorageException`, `StorageUnavailableException` | Định nghĩa các ngoại lệ nghiệp vụ lưu trữ | Tạo mới để tầng API có thể phân biệt lỗi và map thành HTTP 503 |
| `S3StorageService.cs` | Infrastructure | `S3StorageService` | Triển khai giao tiếp MinIO qua `AWSSDK.S3`, xử lý retry lũy tiến, idempotent delete | Nâng cấp toàn diện đáp ứng đầy đủ yêu cầu FILE-001 & FILE-002 |
| `StorageSettings.cs` | Infrastructure | `StorageSettings` | Chứa cấu hình kết nối MinIO (`ServiceUrl`, `AccessKey`, `MaxRetries`, ...) | Tách riêng file để tuân thủ quy chuẩn StyleCop SA1402 |
| `ExceptionHandlingMiddleware.cs` | API | `ExceptionHandlingMiddleware` | Bắt các ngoại lệ toàn cục, chuyển đổi `StorageUnavailableException` thành HTTP 503 Problem Details | Tạo mới để API trả về mã lỗi chuẩn RFC 7807, không leak stack trace |
| `Program.cs` | API | `Program` | Pipeline khởi tạo ứng dụng ASP.NET Core | Đăng ký `ExceptionHandlingMiddleware` vào đầu pipeline HTTP |
| `CulinaryBlog.UnitTests.csproj` | Tests | Project File | Cấu hình dự án Unit Test | Thêm ProjectReference đến `CulinaryBlog.API` để test middleware |
| `StoragePathHelperTests.cs` | Tests | `StoragePathHelperTests` | Kiểm thử tiện ích sinh key, làm sạch path và chống Path Traversal | Tạo mới phục vụ kiểm thử đơn vị |
| `S3StorageServiceTests.cs` | Tests | `S3StorageServiceTests` | Kiểm thử nghiệp vụ MinIO storage, retry, idempotent delete, cancel token | Tạo mới kiểm tra 12 kịch bản yêu cầu |
| `ExceptionHandlingMiddlewareTests.cs` | Tests | `ExceptionHandlingMiddlewareTests` | Kiểm thử middleware ánh xạ lỗi sang HTTP 503 / 422 / 404 Problem Details | Tạo mới kiểm tra tích hợp HTTP response |

---

## 3. BẢNG KẾT QUẢ KIỂM THỬ (UNIT TESTS)

Toàn bộ **130 test cases** đều chạy thành công (`PASS: 130, FAILED: 0, SKIPPED: 1`).

| STT | Tên Test Case | Đầu vào (Input) | Kết quả kỳ vọng (Expected) | Thực tế (Actual) | Trạng thái | Mục đích & Ý nghĩa |
| :---: | :--- | :--- | :--- | :--- | :---: | :--- |
| **1** | `UploadAsync_WithValidStream_UploadsSuccessfullyAndReturnsPublicUrl` | Stream hợp lệ, key `recipes/123/images/pho-bo-1.jpg`, `image/jpeg` | Trả về URL đúng định dạng `http://.../culinaryblog/...` | URL trả về chính xác, S3Client gọi 1 lần | **PASS** | Kiểm tra FILE-001 Upload thành công |
| **2** | `GenerateRecipeImageKey_GeneratesUniqueServerControlledKey` | `recipeId` ngẫu nhiên, extension `.jpg` | Hai lần gọi sinh ra 2 key khác nhau, bắt đầu bằng `recipes/{id}/images/` | 2 key duy nhất, không trùng lặp | **PASS** | Đảm bảo tính duy nhất (Unique Key) |
| **3** | `GenerateRecipeImageKey_DoesNotUseClientRawFileName` | Tên file client: `../../malicious-pho-bo-hack.php` | Key không chứa `malicious`, `hack`, `..`, đuôi `.jpg` | Tên file client bị loại bỏ hoàn toàn | **PASS** | Không tin tưởng tên file từ client |
| **4** | `UploadAsync_WithPathTraversalInKey_ThrowsArgumentException` | Key chứa `../../secret.txt`, `/root/passwords.txt` | Ném `ArgumentException` có thông báo về Path Traversal | Ném `ArgumentException`, S3Client không bị gọi | **PASS** | Ngăn chặn tấn công Path Traversal khi upload |
| **5** | `DeleteAsync_WhenObjectExists_CallsDeleteObjectAsyncSuccessfully` | Key `recipes/123/images/pho-bo.jpg` | S3Client gọi `DeleteObjectAsync` thành công | `DeleteObjectAsync` được gọi chính xác 1 lần | **PASS** | Kiểm tra FILE-002 Delete thành công |
| **6** | `DeleteAsync_WhenObjectDoesNotExist_IsIdempotentAndDoesNotThrow` | S3Client ném lỗi 404 `NoSuchKey` | Hàm hoàn thành êm đềm, không ném ngoại lệ | Thao tác thành công, không ném lỗi ra ngoài | **PASS** | Đảm bảo tính Idempotent của thao tác Delete |
| **7** | `UploadAsync_WhenTransientErrorOccursThenSucceeds_RetriesAndReturnsUrl` | Lần 1: MinIO lỗi 503. Lần 2: Thành công | Thử lại sau backoff và upload thành công | S3Client được gọi 2 lần, URL trả về đúng | **PASS** | Kiểm tra cơ chế tự phục hồi (Retry) khi gặp lỗi tạm thời |
| **8** | `UploadAsync_WhenTransientErrorPersists_StopsAtMaxRetriesAndThrowsStorageUnavailableException` | MinIO liên tục trả lỗi 500 | Dừng lại đúng 3 lần thử và ném `StorageUnavailableException` | Gọi đúng 3 lần, ném `StorageUnavailableException` | **PASS** | Kiểm tra giới hạn retry, không retry vô hạn |
| **9** | `UploadAsync_WhenPermanentErrorOccurs_DoesNotRetryAndThrowsImmediately` | MinIO trả lỗi 403 `AccessDenied` | Ném ngoại lệ ngay lập tức ở lần đầu, không retry | Gọi đúng 1 lần, ném lỗi ra ngay | **PASS** | Lỗi vĩnh viễn không được lãng phí tài nguyên để retry |
| **10** | `UploadAsync_WhenMinIOUnavailableDueToNetwork_ThrowsStorageUnavailableException` | Lỗi mạng `HttpRequestException` (mất kết nối) | Sau 3 lần thử ném `StorageUnavailableException` | Ném `StorageUnavailableException` chuẩn | **PASS** | Biểu diễn MinIO sập bằng Exception phù hợp |
| **11** | `UploadAsync_WhenFails_NeverLeaksSecretCredentialsInException` | Quá trình upload thất bại | Thông điệp ngoại lệ không chứa `AccessKey` hay `SecretKey` | Chuỗi ngoại lệ sạch hoàn toàn thông tin bảo mật | **PASS** | Chống rò rỉ thông tin đăng nhập nhạy cảm |
| **12** | `UploadAsync_WhenCancellationTokenCanceled_StopsImmediatelyWithoutRetry` | `CancellationToken` đã bị hủy từ trước | Ném `OperationCanceledException` ngay, không gọi S3Client | Ném `OperationCanceledException`, S3 gọi 0 lần | **PASS** | Hỗ trợ hủy tác vụ bất đồng bộ đúng chuẩn |
| **13** | `InvokeAsync_WhenStorageUnavailableExceptionThrown_Returns503ProblemDetails` | Downstream ném `StorageUnavailableException` | Middleware trả HTTP 503 Problem Details | HTTP 503, Title "Service Unavailable", không leak stack trace | **PASS** | Ánh xạ lỗi MinIO thành mã HTTP 503 trên API |
| **14** | `InvokeAsync_WhenValidationExceptionThrown_Returns422ProblemDetails` | Downstream ném `ValidationException` | Middleware trả HTTP 422 Problem Details chứa lỗi | HTTP 422, trả về danh sách lỗi validation tiếng Việt | **PASS** | Phân biệt rõ lỗi validation người dùng với lỗi hệ thống |

---

## 4. HƯỚNG DẪN DEMO CHO GIẢNG VIÊN

### Mối quan hệ giữa các branch và điểm Demo thực tế:
* **Tính chất phân hệ:** Phân hệ `File Storage / MinIO` (Task 4) là dịch vụ hạ tầng nền tảng (Infrastructure Service). Nó cung cấp khả năng lưu trữ tệp tin cho toàn bộ các tính năng khác trong dự án (như RecipeImage, User Avatar, Step Image,...).
* **Điểm tích hợp:**
  1. Trong branch `feat/vohungmanh-file-storage` hiện tại: Tính năng được chứng minh và kiểm chứng toàn diện thông qua bộ Unit Tests tự động (`dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj`) bao phủ toàn bộ luồng Upload, Delete, Retry, Sanitize, Idempotent, và Middleware ánh xạ HTTP 503.
  2. Khi tích hợp với branch `feat/vohungmanh-recipe-image` (đã hoàn thành trước đó): File Storage sẽ trực tiếp tiếp nhận luồng tải ảnh từ API Endpoint `POST /api/v1/recipes/{recipeId}/images`, đẩy ảnh thật lên MinIO container, và lưu URL trả về vào PostgreSQL.

### Các bước demo trực tiếp:
1. **Khởi chạy MinIO bằng Docker:**
   ```bash
   docker compose up -d minio
   ```
   Truy cập MinIO Console tại `http://localhost:9001` (User: `minioadmin`, Pass: `minioadmin`), kiểm tra bucket `culinaryblog` đã sẵn sàng.
2. **Chạy kiểm thử tự động:**
   ```bash
   dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj
   ```
   Toàn bộ 130 bài kiểm thử chạy qua màu xanh (PASS 100%).
3. **Demo kịch bản MinIO Unavailable (HTTP 503):**
   * Dừng container MinIO: `docker stop culinaryblog-minio`.
   * Gửi request tải ảnh lên API: Hệ thống tự động thử lại 3 lần qua exponential backoff, sau đó trả về ngay mã lỗi **HTTP 503 Service Unavailable** cùng Problem Details mô tả "Dịch vụ lưu trữ tệp tạm thời không khả dụng", tuyệt đối không bị mã lỗi 400 hay 500, không lộ stack trace.

---

## 5. BỘ CÂU HỎI VẤN ĐÁP VỚI GIẢNG VIÊN (20 CÂU HỎI TRỌNG TÂM)

### Câu 1: MinIO là gì? Tại sao nhóm chọn MinIO thay vì lưu file trực tiếp vào thư mục wwwroot trên server?
> **Trả lời:** MinIO là hệ thống Object Storage tương thích hoàn toàn với chuẩn AWS S3 API. Nhóm không lưu vào `wwwroot` cục bộ vì:
> 1. Lưu file trên web server vi phạm nguyên lý thiết kế Stateless (phi trạng thái) trong hệ thống phân tán. Khi scale hệ thống ra nhiều instance chạy song song (load balancing), file lưu ở server này sẽ không thể truy cập từ server khác.
> 2. MinIO tách rời tải I/O truyền file nặng ra khỏi web server, có khả năng mở rộng dung lượng không giới hạn và dễ dàng tích hợp CDN.

### Câu 2: Object Storage khác biệt gì so với cơ sở dữ liệu quan hệ (PostgreSQL)?
> **Trả lời:** PostgreSQL tối ưu cho dữ liệu có cấu trúc, quan hệ chặt chẽ, truy vấn phức tạp (JOIN, index B-Tree) và đảm bảo giao dịch ACID. Ngược lại, Object Storage tối ưu cho dữ liệu phi cấu trúc dung lượng lớn (ảnh, video) với không gian lưu trữ phẳng (Flat Namespace), truy xuất nhanh qua HTTP REST API và chi phí lưu trữ rẻ hơn rất nhiều.

### Câu 3: Bucket và Object Key là gì?
> **Trả lời:** 
> - **Bucket** là thùng chứa cấp cao nhất để nhóm các đối tượng trong MinIO (trong bài là `culinaryblog`).
> - **Object Key** là chuỗi định danh duy nhất của đối tượng trong bucket. Mặc dù có dấu `/` (ví dụ `recipes/123/images/img.jpg`), nhưng thực chất trong Object Storage không có thư mục con vật lý mà chỉ là một chuỗi khóa phẳng dùng để tra cứu.

### Câu 4: Tại sao không dùng tên file gốc do người dùng tải lên (ví dụ: `anh-pho-bo.jpg`)?
> **Trả lời:**
> 1. Tránh xung đột (Collision) khi nhiều người dùng cùng upload file trùng tên.
> 2. Tránh lỗi mã hóa URL khi tên file chứa dấu tiếng Việt hoặc ký tự đặc biệt.
> 3. Tránh các nguy cơ bảo mật như tấn công chèn mã hoặc Path Traversal (`../../malicious.exe`).

### Câu 5: Tấn công Path Traversal là gì? Em đã ngăn chặn nó trong code như thế nào?
> **Trả lời:** Path Traversal là kỹ thuật kẻ xấu chèn các ký tự `../` hoặc `..\` vào đường dẫn nhằm thoát khỏi thư mục chỉ định để đọc trộm hoặc xóa file hệ thống. Trong code, em tạo lớp `StoragePathHelper.SanitizeKey()`: loại bỏ ký tự điều khiển, cấm dấu hai chấm `:`, chuẩn hóa `\` thành `/`, cấm bắt đầu bằng `/`, và bẻ nhỏ chuỗi kiểm tra từng đoạn; nếu phát hiện `..` sẽ lập tức ném `ArgumentException`.

### Câu 6: Tính chất Idempotent trong thao tác Xóa (Delete) nghĩa là gì?
> **Trả lời:** Idempotent (lũy đẳng) nghĩa là gọi phương thức xóa 1 lần hay gọi 10 lần liên tiếp thì kết quả cuối cùng đều như nhau (tài nguyên không còn tồn tại trên kho) và không sinh lỗi. Trong `S3StorageService.DeleteAsync`, nếu MinIO báo file không tìm thấy (`NotFound` hoặc `NoSuchKey`), service vẫn coi như thành công chứ không ném exception làm gián đoạn nghiệp vụ.

### Câu 7: Cơ chế Retry hoạt động như thế nào? Tại sao lại dùng Exponential Backoff?
> **Trả lời:** Khi gặp sự cố mạng chập chờn hoặc MinIO quá tải tạm thời, service sẽ tự động thử lại tối đa 3 lần. Nhóm sử dụng thuật toán Exponential Backoff ($\text{delay} = 100\text{ms} \times 2^{\text{attempt}-1}$) để tăng dần thời gian chờ (100ms, 200ms, 400ms). Việc này giúp MinIO có thời gian hồi phục, tránh tạo ra bão request (Retry Storm) làm sập hệ thống nặng hơn.

### Câu 8: Những lỗi nào cho phép Retry, những lỗi nào KHÔNG được Retry?
> **Trả lời:**
> - **Cho phép Retry (Transient):** Mã lỗi 5xx (500, 502, 503, 504), mã 408 (Request Timeout), `HttpRequestException`, `SocketException`.
> - **KHÔNG Retry (Permanent):** Mã lỗi 4xx (400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found), `ArgumentException`. Vì các lỗi này xuất phát từ sai sót dữ liệu hoặc phân quyền, có thử lại bao nhiêu lần cũng sẽ thất bại.

### Câu 9: Vì sao khi MinIO sập lại trả về mã HTTP 503 mà không phải 400 hay 500?
> **Trả lời:**
> - Mã **400** là lỗi Client gửi sai dữ liệu (ở đây Client gửi đúng, lỗi do hạ tầng).
> - Mã **500** là lỗi lập trình nội bộ trong code API.
> - Mã **503 (Service Unavailable)** thông báo rằng API vẫn hoạt động nhưng dịch vụ phụ thuộc bên ngoài (MinIO Storage) tạm thời gián đoạn. Đây là cách phân loại chuẩn mực theo RFC 7231, giúp phía frontend hoặc client biết để hiển thị thông báo "Hệ thống lưu trữ đang bảo trì, vui lòng thử lại sau".

### Câu 10: Nhóm đã cấu hình đăng ký `IStorageService` trong Dependency Injection theo vòng đời nào? Tại sao?
> **Trả lời:** Được đăng ký dưới dạng `Singleton` (`services.AddSingleton<IStorageService, S3StorageService>()`). Vì `AmazonS3Client` là thread-safe và quản lý kết nối HTTP nội bộ qua connection pool. Sử dụng Singleton giúp tối ưu tài nguyên, tránh tạo mở socket liên tục gây cạn kiệt cổng (TCP port exhaustion).

### Câu 11: Tại sao lại dùng Stream khi Upload mà không đọc hết file vào một mảng `byte[]`?
> **Trả lời:** Nếu người dùng tải lên nhiều file dung lượng lớn (hoặc video/ảnh chất lượng cao) và hệ thống đọc hết vào mảng `byte[]` trong bộ nhớ, toàn bộ dung lượng đó sẽ nằm trên RAM (thuộc Large Object Heap - LOH). Khi nhiều người tải đồng thời, server sẽ bị tràn bộ nhớ (Out of Memory - OOM) và Garbage Collector sẽ phải dừng ứng dụng để dọn dẹp (GC Pause). Dùng Stream truyền trực tiếp từng chunk dữ liệu từ Request tới MinIO giúp dung lượng RAM luôn ổn định ở mức tối thiểu.

### Câu 12: Khi thực hiện Retry một Stream Upload, có điểm gì cần đặc biệt chú ý?
> **Trả lời:** Khi upload một Stream, con trỏ vị trí (`Stream.Position`) sẽ bị dịch chuyển về cuối stream. Nếu lần upload đầu thất bại và thực hiện retry, nếu không tua lại vị trí ban đầu (`content.Position = initialPosition`), MinIO sẽ nhận một luồng rỗng 0 byte. Trong code `S3StorageService`, em đã kiểm tra `content.CanSeek` và reset `content.Position` về vị trí xuất phát trước mỗi lần retry.

### Câu 13: Thông tin bảo mật (AccessKey, SecretKey) được quản lý như thế nào?
> **Trả lời:** Thông tin được đặt trong `appsettings.json` cho môi trường local và ghi đè bằng biến môi trường (Environment Variables) trong môi trường production. Tuyệt đối không hard-code vào code C#. Đồng thời, trong toàn bộ các câu lệnh log và thông điệp ngoại lệ, em đã kiểm tra để không bao giờ in `SecretKey`.

### Câu 14: Tại sao trong cấu hình AmazonS3Config cần bật `ForcePathStyle = true`?
> **Trả lời:** AWS S3 mặc định sử dụng Virtual-Hosted Style URL (`http://bucket-name.s3.amazonaws.com/object`). Tuy nhiên các hệ thống Object Storage cục bộ như MinIO chạy trên IP/localhost không có DNS wildcard cho từng bucket, do đó bắt buộc phải sử dụng Path-Style URL (`http://localhost:9000/bucket-name/object`).

### Câu 15: Tại sao cần `DisablePayloadSigning = true` trong PutObjectRequest?
> **Trả lời:** Mặc định AWS SDK sẽ tính toán mã băm SHA256 trên toàn bộ nội dung của payload stream để ký xác thực. Việc này buộc SDK phải đọc toàn bộ stream trước khi gửi, làm mất đi lợi thế truyền stream trực tiếp và có thể gây lỗi với stream không seek được. Tắt ký payload giúp tương thích tối đa và tăng tốc độ truyền tệp lên MinIO.

### Câu 16: Phương thức `GetPresignedUrlAsync` dùng để làm gì?
> **Trả lời:** Dùng để tạo một đường dẫn URL có chữ ký bảo mật và có thời hạn sử dụng (ví dụ hết hạn sau 15 phút). Tính năng này cho phép Client tải trực tiếp file riêng tư từ MinIO mà không cần mở public toàn bộ bucket.

### Câu 17: Phân biệt việc xóa ảnh trong MinIO với việc Soft Delete bản ghi RecipeImage trong Database?
> **Trả lời:**
> - Soft Delete trong PostgreSQL: Chỉ cập nhật cờ `IsDeleted = true` để lưu vết lịch sử dữ liệu và bảo toàn tính toàn vẹn quan hệ.
> - Xóa file trong MinIO: Giải phóng dung lượng lưu trữ vật lý thực tế trên đĩa cứng khi bức ảnh bị xóa vĩnh viễn hoặc khi người dùng thay thế ảnh mới.

### Câu 18: Tham số `CancellationToken` đóng vai trò gì trong các tác vụ Storage?
> **Trả lời:** Giúp hủy bỏ sớm thao tác I/O khi Client ngắt kết nối đột ngột hoặc timeout. Nhờ đó, máy chủ không tiếp tục tốn CPU và băng thông mạng để tải tiếp một file mà không còn ai chờ nhận.

### Câu 19: Lớp `ExceptionHandlingMiddleware` hoạt động theo nguyên lý nào?
> **Trả lời:** Đây là một Middleware nằm ở đầu pipeline xử lý của ASP.NET Core, bọc toàn bộ các controller/endpoint trong khối `try-catch`. Khi bất kỳ service nào ném ngoại lệ chưa xử lý, middleware sẽ bắt lại, nhận diện kiểu ngoại lệ (`StorageUnavailableException`, `ValidationException`,...) và sinh ra phản hồi JSON chuẩn Problem Details (RFC 7807) với HTTP status code tương ứng.

### Câu 20: Nếu sau này hệ thống chuyển từ MinIO sang CloudFlare R2 hoặc AWS S3 thật thì phải sửa những gì?
> **Trả lời:** Hoàn toàn không cần sửa mã nguồn logic của ứng dụng. Do toàn bộ hệ thống giao tiếp qua interface `IStorageService` và `S3StorageService` sử dụng thư viện chuẩn `AWSSDK.S3`, ta chỉ cần thay đổi `ServiceUrl`, `AccessKey`, `SecretKey` và `Region` trong file cấu hình `appsettings.json` hoặc biến môi trường là hệ thống sẽ tự động kết nối sang dịch vụ mới.

---

## 6. KỊCH BẢN BÀI NÓI THUYẾT MINH BẢO VỆ (2 - 3 PHÚT)

> *"Kính thưa Thầy/Cô và Hội đồng chấm đồ án,*  
> *Em là **Võ Hùng Mạnh**. Trong đồ án xây dựng nền tảng ẩm thực CulinaryBlog, em phụ trách triển khai phân hệ **File Storage / MinIO Object Storage** thuộc Task 4.*  
>  
> *Về mặt kiến trúc, hệ thống của nhóm áp dụng mô hình phân tách lưu trữ hiện đại: Cơ sở dữ liệu PostgreSQL chỉ đóng vai trò lưu trữ metadata và đường dẫn URL; trong khi toàn bộ dữ liệu tệp tin ảnh nhị phân dung lượng lớn được ủy thác cho MinIO - một hệ thống Object Storage tương thích hoàn toàn chuẩn AWS S3 API.*  
>  
> *Trong quá trình hiện thực hóa, em đã giải quyết 5 bài toán kỹ thuật trọng tâm:*  
>  
> * **Thứ nhất, Tối ưu hóa bộ nhớ với Luồng (Stream Processing):** Thay vì nạp toàn bộ file vào RAM dưới dạng byte array gây nguy cơ tràn bộ nhớ và quá tải Garbage Collector, em triển khai upload bằng stream truyền trực tiếp từ HTTP request lên MinIO.*  
> * **Thứ hai, An toàn bảo mật và Chống Path Traversal:** Em xây dựng tiện ích `StoragePathHelper` nhằm sinh Object Key duy nhất bằng UUID do máy chủ kiểm soát theo cấu trúc `recipes/{id}/images/{uuid}.jpg`. Hệ thống tuyệt đối không tin tưởng tên file gốc của client và chủ động chặn đứng mọi hành vi chèn ký tự nguy hiểm như dấu hai chấm hay hai dấu chấm `../`.*  
> * **Thứ ba, Khả năng tự phục hồi với Bounded Retry:** Để đối phó với hiện tượng mạng chập chờn hay máy chủ MinIO quá tải tạm thời, em hiện thực cơ chế thử lại lũy tiến (Exponential Backoff) tối đa 3 lần cho các lỗi transient (5xx, 408, mất mạng), và dừng ngay lập tức đối với các lỗi vĩnh viễn (4xx).*  
> * **Thứ tư, Tính lũy đẳng trong thao tác Xóa (Idempotent Delete):** Khi xóa một file đã không còn tồn tại trên MinIO, service sẽ xử lý êm đềm thay vì ném lỗi, đảm bảo tính lũy đẳng chuẩn mực theo thiết kế RESTful.*  
> * **Cuối cùng, Xử lý lỗi tập trung và Chuẩn hóa HTTP 503:** Em xây dựng `ExceptionHandlingMiddleware`. Khi MinIO gặp sự cố không thể truy cập sau 3 lần thử, hệ thống ném `StorageUnavailableException` và tự động ánh xạ thành mã lỗi **HTTP 503 Service Unavailable** theo chuẩn RFC 7807 Problem Details, cam kết không làm lộ bất kỳ thông tin nhạy cảm hay stack trace nào ra bên ngoài.*  
>  
> *Toàn bộ phân hệ đã được kiểm chứng toàn diện qua bộ 130 Unit Tests tự động đạt tỷ lệ Pass 100%.*  
> *Em xin chân thành cảm ơn Thầy/Cô và rất mong nhận được câu hỏi cũng như đóng góp ý kiến từ Hội đồng!"*
