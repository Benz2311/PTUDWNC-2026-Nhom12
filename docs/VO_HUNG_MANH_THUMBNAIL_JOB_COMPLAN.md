# BÁO CÁO GIẢI TRÌNH BẢO VỆ ĐỒ ÁN (COMPLAN)
## PHÂN HỆ THUMBNAIL JOB / IMAGE RESIZE (JOB-002 - TASK 4)
**Sinh viên thực hiện:** Võ Hùng Mạnh (MSSV: 2312687)  
**Nhánh Git:** `feat/vohungmanh-thumbnail-job`  
**Dự án:** CulinaryBlog - Nền tảng chia sẻ công thức ẩm thực  

---

## MỤC LỤC
1. [Thumbnail Job giải quyết vấn đề gì?](#1-thumbnail-job-giải-quyết-vấn-đề-gì)
2. [Tại sao không trả Original Image cho mọi trường hợp?](#2-tại-sao-không-trả-original-image-cho-mọi-trường-hợp)
3. [Background Job là gì?](#3-background-job-là-gì)
4. [Tại sao Image Resize phải chạy ở Background?](#4-tại-sao-image-resize-phải-chạy-ở-background)
5. [Nguyên lý hoạt động của Hangfire trong dự án](#5-nguyên-lý-hoạt-động-của-hangfire-trong-dự-án)
6. [Job được Enqueue ở đâu?](#6-job-được-enqueue-ở-đâu)
7. [Hangfire Worker lấy và thực thi Job như thế nào?](#7-hangfire-worker-lấy-và-thực-thi-job-như-thế-nào)
8. [Mối quan hệ với thực thể RecipeImage](#8-mối-quan-hệ-với-thực-thể-recipeimage)
9. [Bản chất của OriginalUrl](#9-bản-chất-của-originalurl)
10. [Bản chất của MediumUrl](#10-bản-chất-của-mediumurl)
11. [Bản chất của ThumbnailUrl](#11-bản-chất-của-thumbnailurl)
12. [Cơ chế trừu tượng hóa lưu trữ (Storage Abstraction)](#12-cơ-chế-trừu-tượng-hóa-lưu-trữ-storage-abstraction)
13. [MinIO đóng vai trò gì?](#13-minio-đóng-vai-trò-gì)
14. [Bản chất của thao tác Decode ảnh thật](#14-bản-chất-của-thao-tác-decode-ảnh-thật)
15. [Thuật toán Resize hoạt động như thế nào?](#15-thuật-toán-resize-hoạt-động-như-thế-nào)
16. [Tỷ lệ khung hình (Aspect Ratio)](#16-tỷ-lệ-khung-hình-aspect-ratio)
17. [Định dạng ảnh (Image Formats: JPEG, PNG, WebP)](#17-định-dạng-ảnh-image-formats-jpeg-png-webp)
18. [Cơ chế Retry](#18-cơ-chế-retry)
19. [Tại sao cấu hình Retry tối đa 3 lần?](#19-tại-sao-cấu-hình-retry-tối-đa-3-lần)
20. [Tính lũy đẳng (Idempotency)](#20-tính-lũy-đẳng-idempotency)
21. [Tính nhất quán dữ liệu (Data Consistency)](#21-tính-nhất-quán-dữ-liệu-data-consistency)
22. [Chiến lược xử lý lỗi (Error Handling)](#22-chiến-lược-xử-lý-lỗi-error-handling)
23. [Hệ thống ghi nhật ký (Logging)](#23-hệ-thống-ghi-nhật-ký-logging)
24. [An toàn thông tin và Bảo mật (Security)](#24-an-toàn-thông-tin-và-bảo-mật-security)
25. [Code Walkthrough theo từng File](#25-code-walkthrough-theo-từng-file)
26. [Code Walkthrough theo từng Class](#26-code-walkthrough-theo-từng-class)
27. [Code Walkthrough phương thức ProcessAsync](#27-code-walkthrough-phương-thức-processasync)
28. [Bảng ma trận kiểm thử (Test Matrix)](#28-bảng-ma-trận-kiểm-thử-test-matrix)
29. [Kịch bản hướng dẫn Demo thực tế](#29-kịch-bản-hướng-dẫn-demo-thực-tế)
30. [Các giới hạn kỹ thuật (Limitations)](#30-các-giới-hạn-kỹ-thuật-limitations)
31. [Những lỗi thường gặp và cách khắc phục](#31-những-lỗi-thường-gặp-và-cách-khắc-phục)
32. [Bộ 20 câu hỏi vấn đáp chuyên sâu cho Giảng viên](#32-bộ-20-câu-hỏi-vấn-đáp-chuyên-sâu-cho-giảng-viên)
33. [Bài phát biểu bảo vệ đồ án 2-3 phút](#33-bài-phát-biểu-bảo-vệ-đồ-án-2-3-phút)

---

## 1. THUMBNAIL JOB GIẢI QUYẾT VẤN ĐỀ GÌ?
Trong một nền tảng ẩm thực như CulinaryBlog, người dùng tải lên hình ảnh món ăn từ điện thoại thông minh hoặc máy ảnh kỹ thuật số với độ phân giải rất cao (4K, 8K) và dung lượng tệp tin từ 5MB đến 20MB.
* **Vấn đề đặt ra:** Nếu trang chủ hiển thị danh sách 20 công thức nấu ăn mà mỗi công thức đều tải bức ảnh gốc 10MB, trình duyệt của người dùng sẽ phải tải xuống 200MB dữ liệu chỉ để hiển thị các ô thumbnail nhỏ kích thước 150x150px. Điều này dẫn đến:
  1. Tốc độ tải trang cực kỳ chậm (Lag, đơ giao diện), làm giảm điểm SEO và trải nghiệm người dùng (UX).
  2. Lãng phí băng thông mạng của cả máy chủ và thiết bị di động (người dùng 4G/5G tốn dung lượng).
  3. Tràn bộ nhớ RAM trên trình duyệt của các thiết bị yếu.
* **Giải pháp của Thumbnail Job (`JOB-002`):** Tự động tạo ra các phiên bản ảnh thu nhỏ có kích thước và dung lượng tối ưu ngay sau khi ảnh gốc được tải lên:
  * **Medium (chiều rộng tối đa 800px):** Dung lượng ~100KB - 250KB, hiển thị sắc nét trong trang chi tiết món ăn.
  * **Thumbnail (chiều rộng tối đa 300px):** Dung lượng ~20KB - 50KB, hiển thị siêu tốc trên danh sách tìm kiếm, trang chủ và mobile card.

---

## 2. TẠI SAO KHÔNG TRẢ ORIGINAL IMAGE CHO MỌI TRƯỜNG HỢP?
* **Băng thông (Bandwidth):** Một ảnh gốc 10MB nặng gấp 200 lần một ảnh Thumbnail 50KB. Khi có 10.000 người truy cập, chi phí truyền tải ảnh gốc là 100GB thay vì chỉ 500MB.
* **Thời gian phản hồi (Latency / First Contentful Paint - FCP):** Mạng 4G trung bình mất 3-5 giây để tải xong một ảnh 10MB, nhưng chỉ mất dưới 0.1 giây để tải ảnh thumbnail 50KB.
* **Kích thước DOM và Layout Shift:** Trình duyệt khi tải ảnh quá lớn sẽ phải tự co kéo (downscale) bằng GPU của client, gây nóng máy và hiện tượng giật khung hình (Cumulative Layout Shift - CLS).

---

## 3. BACKGROUND JOB LÀ GÌ?
**Background Job (Tác vụ nền)** là cơ chế chuyển giao một công việc tốn nhiều thời gian hoặc tài nguyên tính toán ra khỏi chu kỳ vòng đời của một HTTP Request-Response thông thường, để nó được thực thi ngầm bởi một tiến trình (worker thread) riêng biệt.
* Thay vì bắt người dùng phải đợi trên màn hình:
  `Upload ảnh -> Chờ nén -> Chờ resize Medium -> Chờ resize Thumbnail -> Xong mới báo thành công (mất 5 - 10 giây)`
* Với Background Job:
  `Upload ảnh gốc -> Lưu DB -> Bắn tác vụ resize vào hàng đợi -> Trả về HTTP 201 Created ngay lập tức (chỉ mất ~200ms)`. Người dùng có thể tiếp tục lướt web trong khi worker chạy ngầm phía sau.

---

## 4. TẠI SAO IMAGE RESIZE PHẢI CHẠY Ở BACKGROUND?
1. **Tiêu tốn CPU và RAM:** Giải mã ma trận điểm ảnh (Pixel Matrix Decode) và áp dụng các bộ lọc nội suy (Bicubic / Lanczos resampling) là tác vụ cực kỳ nặng về CPU (CPU-bound). Nếu chạy trực tiếp trên luồng HTTP của Web Server (Kestrel thread), nó sẽ chiếm dụng thread pool, khiến các request tra cứu khác của người dùng khác bị tắc nghẽn (Thread Starvation).
2. **Nguy cơ Timeout:** Nếu mạng chậm hoặc ảnh quá lớn, request HTTP có thể bị timeout (504 Gateway Timeout) giữa chừng.
3. **Tính chịu lỗi (Fault Tolerance):** Nếu resize chạy đồng bộ và bị lỗi, toàn bộ thao tác thêm ảnh của người dùng bị hủy bỏ. Chạy ở background cho phép tự động thử lại (Retry) mà không làm ảnh hưởng tới dữ liệu ảnh gốc đã lưu.

---

## 5. NGUYÊN LÝ HOẠT ĐỘNG CỦA HANGFIRE TRONG DỰ ÁN
Hangfire là framework xử lý tác vụ nền mạnh mẽ và đáng tin cậy trong hệ sinh thái .NET.
* **Lưu trữ trạng thái bền vững (Persistent Storage):** Hangfire lưu toàn bộ thông tin công việc (Job Metadata, arguments, trạng thái) vào database PostgreSQL trong schema `hangfire`. Dù web server có bị restart đột ngột, các job chưa chạy vẫn được bảo toàn và tiếp tục thực thi khi server bật lại.
* **Kiến trúc của Hangfire trong CulinaryBlog:**
  * **Hangfire Client:** Được gọi qua `IBackgroundJobService.Enqueue` để đóng gói lời gọi hàm `job => job.ProcessAsync(imageId)` thành chuỗi JSON và ghi vào bảng `hangfire.job`.
  * **Hangfire Server:** Các tiến trình worker chạy ngầm song song (`services.AddHangfireServer()`), liên tục thăm dò (polling) hàng đợi trong PostgreSQL để lấy job ra xử lý.
  * **Hangfire Dashboard:** Giao diện trực quan tại `/hangfire` cho phép admin theo dõi trạng thái các job: Enqueued, Processing, Succeeded, Failed, Retrying.

---

## 6. JOB ĐƯỢC ENQUEUE Ở ĐÂU?
* Hợp đồng trừu tượng: `IBackgroundJobService.Enqueue(Expression<Func<Task>> methodCall)`.
* Khi một ảnh mới được tải lên và lưu vào bảng `RecipeImages`:
  ```csharp
  // Sau khi lưu RecipeImage vào DbContext thành công:
  _backgroundJobService.Enqueue<IImageResizeJob>(job => job.ProcessAsync(newImage.Id, CancellationToken.None));
  ```
* Lời gọi hàm trên được Hangfire chuyển thành một biểu thức cây (Expression Tree), tuần tự hóa kiểu dữ liệu, phương thức và tham số (`newImage.Id`), sau đó lưu vào database.

---

## 7. HANGFIRE WORKER LẤY VÀ THỰC THI JOB NHƯ THẾ NÀO?
1. Hangfire Server duy trì một nhóm luồng (Worker Thread Pool).
2. Khi có job mới trong hàng đợi `default`, một worker rảnh rỗi sẽ giành quyền khóa (lock) job đó trong database và chuyển trạng thái sang `Processing`.
3. Hangfire sử dụng cơ chế Dependency Injection của ASP.NET Core để tạo một Scope mới (`IServiceScope`), resolve thể hiện `IImageResizeJob` (chính là `ImageResizeJob`), và truyền các service phụ thuộc (`ApplicationDbContext`, `IStorageService`, `ILogger`).
4. Gọi phương thức `job.ProcessAsync(recipeImageId)`.
5. Nếu hàm chạy thành công không có ngoại lệ: Job chuyển sang trạng thái `Succeeded`.
6. Nếu ném ra Exception: Hangfire bắt lại, ghi nhận lỗi, và đưa vào hàng đợi `Retrying` để thử lại theo cấu hình `AutomaticRetry`.

---

## 8. MỐI QUAN HỆ VỚI THỰC THỂ RECIPEIMAGE
Thực thể `RecipeImage` (`CulinaryBlog.Domain.Entities.RecipeImage`) quản lý trạng thái của ảnh:
```csharp
public class RecipeImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecipeId { get; set; }
    public string OriginalUrl { get; set; } = string.Empty; // Luôn luôn có sau khi upload ban đầu
    public string? MediumUrl { get; set; }                  // Được gán sau khi Thumbnail Job chạy xong
    public string? ThumbnailUrl { get; set; }               // Được gán sau khi Thumbnail Job chạy xong
    public bool IsDeleted { get; set; }                     // Cờ xóa mềm
    public DateTime? UpdatedAt { get; set; }
}
```
* Khi vừa upload: `OriginalUrl` có giá trị, `MediumUrl` và `ThumbnailUrl` là `null`.
* Khi Job hoàn tất: Cả 3 cột đều có giá trị URL đầy đủ trên MinIO.

---

## 9. BẢN CHẤT CỦA ORIGINALURL
* Là đường dẫn trỏ tới tệp tin nhị phân nguyên bản do người dùng tải lên MinIO.
* Định dạng chuẩn: `http://localhost:9000/culinaryblog/recipes/{recipeId}/images/{uniqueId}.jpg`.
* **Quy tắc bất di bất dịch:** `OriginalUrl` là bản gốc duy nhất, không bao giờ bị ghi đè, không bị resize, và không bị xóa bởi Thumbnail Job. Nó đóng vai trò là "chân lý gốc" để nếu sau này hệ thống muốn đổi thuật toán nén hoặc đổi kích thước thumbnail, ta luôn có thể chạy lại job từ file gốc này.

---

## 10. BẢN CHẤT CỦA MEDIUMURL
* Là đường dẫn trỏ tới bức ảnh phiên bản Medium (chiều rộng tối đa 800px).
* Tên tệp tin có hậu tố `_medium`: `recipes/{recipeId}/images/{imageId}_medium.jpg`.
* Dành riêng cho trang xem chi tiết công thức nấu ăn, đảm bảo người xem thấy rõ từng thớ thịt, cọng hành nhưng dung lượng chỉ chiếm khoảng 150KB.

---

## 11. BẢN CHẤT CỦA THUMBNAILURL
* Là đường dẫn trỏ tới bức ảnh phiên bản thu nhỏ Thumbnail (chiều rộng tối đa 300px).
* Tên tệp tin có hậu tố `_thumb`: `recipes/{recipeId}/images/{imageId}_thumb.jpg`.
* Dành riêng cho các thành phần UI nhỏ: Card món ăn trên danh sách tìm kiếm, trang chủ, mục "Món ăn tương tự". Tối ưu tốc độ tải và cuộn trang mượt mà (60fps).

---

## 12. CƠ CHẾ TRỪU TƯỢNG HÓA LƯU TRỮ (STORAGE ABSTRACTION)
Tầng Application và `ImageResizeJob` chỉ giao tiếp thông qua hợp đồng trừu tượng `IStorageService`:
* `DownloadAsync(string bucketName, string objectKey, CancellationToken ct)`: Đọc file nhị phân.
* `UploadAsync(string bucketName, string objectKey, Stream content, string contentType, CancellationToken ct)`: Đẩy file nhị phân lên kho.
Nhờ trừu tượng hóa này, `ImageResizeJob` hoàn toàn độc lập với việc file đang nằm ở MinIO cục bộ, AWS S3 đám mây, hay Azure Blob.

---

## 13. MINIO ĐÓNG VAI TRÒ GÌ?
MinIO đóng vai trò là hệ thống lưu trữ đối tượng (Object Storage Server), tiếp nhận và lưu giữ vật lý các byte nhị phân của cả 3 phiên bản: Original, Medium, Thumbnail. Cơ sở dữ liệu PostgreSQL chỉ lưu URL string, hoàn toàn không phải gánh dữ liệu file nặng.

---

## 14. BẢN CHẤT CỦA THAO TÁC DECODE ẢNH THẬT
* **Decode (Giải mã)** là quá trình đọc dữ liệu nhị phân thô (byte stream) từ file JPEG, PNG hoặc WebP, phân tích cú pháp header và giải nén các khối nén thành một **ma trận điểm ảnh hai chiều (2D Pixel Buffer)** trong bộ nhớ RAM.
* Mỗi pixel gồm các kênh màu: Đỏ (R), Lục (G), Lam (B), và Độ trong suốt (Alpha - A).
* Thư viện `SixLabors.ImageSharp.Image.LoadAsync` đọc toàn bộ thông số: Chiều rộng thực tế, chiều cao thực tế, không gian màu và định dạng chuẩn (`DecodedImageFormat`). Nếu file bị hỏng (corrupt) hoặc bị giả mạo đuôi file (ví dụ đổi tên file `.exe` thành `.jpg`), quá trình decode sẽ ném lỗi ngay lập tức.

---

## 15. THUẬT TOÁN RESIZE HOẠT ĐỘNG NHƯ THẾ NÀO?
* Sử dụng phương thức `ctx.Resize(new ResizeOptions { Size = new Size(maxWidth, 0), Mode = ResizeMode.Max })`.
* **Cơ chế:** Khi chiều cao được đặt là `0` và chế độ là `ResizeMode.Max`, ImageSharp sẽ tính toán kích thước mới dựa trên tỷ lệ co giãn của chiều rộng, sao cho chiều rộng không vượt quá `maxWidth` và chiều cao co giãn tương ứng theo công thức:
  $$\text{NewHeight} = \text{OriginalHeight} \times \left( \frac{\text{NewWidth}}{\text{OriginalWidth}} \right)$$
* Nếu ảnh gốc nhỏ hơn `maxWidth` (ví dụ ảnh gốc chỉ rộng 500px so với Medium 800px), chế độ `ResizeMode.Max` thông minh sẽ giữ nguyên kích thước 500px, không thực hiện phóng đại (upscale) để tránh làm vỡ hạt và mờ ảnh.

---

## 16. TỶ LỆ KHUNG HÌNH (ASPECT RATIO)
* **Tỷ lệ khung hình:** Là tỷ số giữa chiều rộng và chiều cao của bức ảnh ($\text{Width} : \text{Height}$, ví dụ 4:3, 16:9, 1:1).
* **Bảo vệ tỷ lệ:** Code thực tế sử dụng `ResizeMode.Max` thay vì `ResizeMode.Stretch` hay `ResizeMode.Crop`. Tuyệt đối không kéo dãn méo mó khuôn hình món ăn, đảm bảo tính thẩm mỹ cao nhất cho giao diện ẩm thực.

---

## 17. ĐỊNH DẠNG ẢNH (IMAGE FORMATS: JPEG, PNG, WEBP)
* **Giữ nguyên định dạng gốc:** Khi ảnh gốc là PNG trong suốt, Medium và Thumbnail sinh ra cũng là PNG trong suốt. Khi ảnh gốc là WebP, Medium và Thumbnail sinh ra cũng là WebP siêu nhẹ.
* Code trích xuất trực tiếp `detectedFormat.DefaultMimeType` (ví dụ `image/png`, `image/webp`) và `detectedFormat.FileExtensions` để đảm bảo tính đồng nhất tuyệt đối về định dạng.

---

## 18. CƠ CHẾ RETRY
Khi gặp sự cố mạng chập chờn hoặc MinIO khởi động chậm, Hangfire tự động kích hoạt cơ chế Retry nhờ thuộc tính khai báo trên method:
```csharp
[AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
public async Task ProcessAsync(Guid recipeImageId, CancellationToken ct = default)
```

---

## 19. TẠI SAO CẤU HÌNH RETRY TỐI ĐA 3 LẦN?
* **3 lần là con số vàng trong kỹ thuật phần mềm:** Đủ để vượt qua các sự cố nghẽn mạng ngắn hạn (Transient Network Glitches).
* **Tránh Retry vô hạn:** Nếu ảnh gốc bị hỏng (corrupt data) hoặc lỗi logic vĩnh viễn, việc retry vô hạn sẽ làm nghẽn hàng đợi, ngốn CPU và tiêu tốn tài nguyên hệ thống.
* Khi hết 3 lần thử, `OnAttemptsExceeded = AttemptsExceededAction.Fail` sẽ đánh dấu job thất bại và chuyển vào tab `Failed` trên Hangfire Dashboard để lập trình viên theo dõi.

---

## 20. TÍNH LŨY ĐẲNG (IDEMPOTENCY)
* **Khái niệm:** Dù job bị chạy lại nhiều lần (do mạng chập chờn làm Hangfire gửi lại, hoặc admin ấn nút "Re-queue"), trạng thái hệ thống vẫn không bị sai lệch, không bị trùng lặp.
* **Code thực tế kiểm tra:**
  ```csharp
  if (!string.IsNullOrWhiteSpace(image.MediumUrl) && !string.IsNullOrWhiteSpace(image.ThumbnailUrl))
  {
      _logger.LogInformation("RecipeImage {Id} đã có đầy đủ MediumUrl và ThumbnailUrl. Bỏ qua để đảm bảo tính Idempotent.", recipeImageId);
      return;
  }
  ```
* Nếu ảnh đã được xử lý xong từ trước, job lập tức thoát ra êm đềm, không tốn CPU tính toán lại và không ghi đè file vô ích.

---

## 21. TÍNH NHẤT QUÁN DỮ LIỆU (DATA CONSISTENCY)
* **Nguyên tắc "Tất cả hoặc không có gì" (Atomic Update):**
  Chỉ khi cả ảnh Medium và ảnh Thumbnail đều đã được upload thành công lên MinIO thì service mới cập nhật `MediumUrl` và `ThumbnailUrl` vào thực thể và gọi `await _dbContext.SaveChangesAsync(ct)`.
* Nếu upload Medium thành công nhưng upload Thumbnail bị rớt mạng, hàm sẽ ném ngoại lệ $\rightarrow$ DB không lưu URL rác nửa vời $\rightarrow$ Hangfire kích hoạt retry lại từ đầu.

---

## 22. CHIẾN LƯỢC XỬ LÝ LỖI (ERROR HANDLING)
1. **RecipeImage không tìm thấy:** Log warning và return ngay (tránh lỗi NullReferenceException).
2. **RecipeImage đã bị xóa mềm (`IsDeleted = true`):** Log warning và bỏ qua, không lãng phí tài nguyên xử lý một bài viết đã xóa.
3. **Ảnh hỏng / Không giải mã được:** Ném ngoại lệ có thông điệp rõ ràng, không lưu URL giả vào database.
4. **OriginalUrl không hợp lệ:** Kiểm tra ngay từ đầu và báo lỗi logic.

---

## 23. HỆ THỐNG GHI NHẬT KÝ (LOGGING)
* Sử dụng `ILogger<ImageResizeJob>` tích hợp Serilog.
* Mọi bước quan trọng đều có log có cấu trúc (Structured Logging): Bắt đầu xử lý, kích thước và định dạng ảnh gốc sau khi decode, hoàn tất tạo và upload ảnh.
* Khi gặp sự cố, log ghi rõ ID của RecipeImage và nguyên nhân chi tiết.

---

## 24. AN TOÀN THÔNG TIN VÀ BẢO MẬT (SECURITY)
* **Không hard-code thông tin nhạy cảm:** Toàn bộ AccessKey, SecretKey, ConnectionString được lấy từ cấu hình.
* **Không in Secret ra Log:** Log chỉ ghi nhận ID, Key, kích thước và URL công khai.
* **Chống tải file thực thi nguy hại:** Quá trình decode qua `ImageSharp` chỉ chấp nhận các header nhị phân ảnh hợp lệ; nếu kẻ xấu tải lên mã độc PHP/EXE giả dạng file ảnh, `ImageSharp` sẽ từ chối giải mã ngay lập tức.

---

## 25. CODE WALKTHROUGH THEO TỪNG FILE

### A. `IImageResizeJob.cs` (`CulinaryBlog.Application/Interfaces`)
* Khai báo hợp đồng `Task ProcessAsync(Guid recipeImageId, CancellationToken ct = default);`.
* Tách biệt tầng giao tiếp để các handler của MediatR có thể kích hoạt mà không phụ thuộc vào thư viện ImageSharp.

### B. `IStorageService.cs` (`CulinaryBlog.Application/Interfaces`)
* Mở rộng thêm phương thức `Task<Stream> DownloadAsync(string bucketName, string objectKey, CancellationToken ct = default);`.

### C. `S3StorageService.cs` (`CulinaryBlog.Infrastructure/Storage`)
* Triển khai `DownloadAsync` sử dụng `_s3Client.GetObjectAsync(...)`, sau đó copy sang `MemoryStream` có `Position = 0` để đóng ngay kết nối socket của AWS S3 và cho phép ImageSharp seek tự do.

### D. `ImageResizeJob.cs` (`CulinaryBlog.Infrastructure/BackgroundJobs`)
* Chứa toàn bộ nghiệp vụ tải ảnh, decode, resize, upload và cập nhật database.

### E. `DependencyInjection.cs` (`CulinaryBlog.Infrastructure`)
* Đăng ký `services.AddHttpClient();` và `services.AddScoped<IImageResizeJob, ImageResizeJob>();`.

---

## 26. CODE WALKTHROUGH THEO TỪNG CLASS
* **`ImageResizeJob`:** Triển khai `IImageResizeJob`.
  * Thuộc tính hằng số: `MediumMaxWidth = 800`, `ThumbnailMaxWidth = 300`, `DefaultBucket = "culinaryblog"`.
  * Các trường phụ thuộc: `_dbContext`, `_storageService`, `_logger`, `_httpClient`.

---

## 27. CODE WALKTHROUGH PHƯƠNG THỨC PROCESSASYNC
```csharp
[AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
public async Task ProcessAsync(Guid recipeImageId, CancellationToken ct = default)
{
    // B1: Lấy bản ghi từ DB
    var image = await _dbContext.RecipeImages.FirstOrDefaultAsync(x => x.Id == recipeImageId, ct);
    if (image == null || image.IsDeleted) return;

    // B2: Kiểm tra Idempotency
    if (!string.IsNullOrWhiteSpace(image.MediumUrl) && !string.IsNullOrWhiteSpace(image.ThumbnailUrl)) return;

    // B3: Tải ảnh gốc (Hỗ trợ cả MinIO Storage lẫn HTTP fallback)
    using var originalStream = await GetOriginalImageStreamAsync(image.OriginalUrl, ct);

    // B4: Decode bằng ImageSharp
    using var imageSharp = await Image.LoadAsync(originalStream, ct);
    var detectedFormat = imageSharp.Metadata.DecodedImageFormat!;

    // B5: Resize Medium (800px) & Thumbnail (300px)
    using var mediumImage = imageSharp.Clone(ctx => ctx.Resize(new ResizeOptions { Size = new Size(800, 0), Mode = ResizeMode.Max }));
    using var thumbImage = imageSharp.Clone(ctx => ctx.Resize(new ResizeOptions { Size = new Size(300, 0), Mode = ResizeMode.Max }));

    // B6: Lưu vào Stream nhị phân
    using var mediumMs = new MemoryStream();
    await mediumImage.SaveAsync(mediumMs, detectedFormat, ct);
    mediumMs.Position = 0;

    using var thumbMs = new MemoryStream();
    await thumbImage.SaveAsync(thumbMs, detectedFormat, ct);
    thumbMs.Position = 0;

    // B7: Upload lên MinIO
    var ext = detectedFormat.FileExtensions.FirstOrDefault() ?? "jpg";
    var mediumKey = $"recipes/{image.RecipeId}/images/{image.Id}_medium.{ext}";
    var thumbKey = $"recipes/{image.RecipeId}/images/{image.Id}_thumb.{ext}";

    var mediumUrl = await _storageService.UploadAsync(DefaultBucket, mediumKey, mediumMs, detectedFormat.DefaultMimeType, ct);
    var thumbUrl = await _storageService.UploadAsync(DefaultBucket, thumbKey, thumbMs, detectedFormat.DefaultMimeType, ct);

    // B8: Cập nhật Database
    image.MediumUrl = mediumUrl;
    image.ThumbnailUrl = thumbUrl;
    image.UpdatedAt = DateTime.UtcNow;
    await _dbContext.SaveChangesAsync(ct);
}
```

---

## 28. BẢNG MA TRẬN KIỂM THỬ (TEST MATRIX)

Toàn bộ **10 bài kiểm thử đơn vị** của `ImageResizeJobTests` đều đạt kết quả **PASS 100%**:

| STT | Tên Test Case | Đầu vào (Input) | Kết quả kỳ vọng | Kết quả thực tế | Trạng thái |
| :---: | :--- | :--- | :--- | :--- | :---: |
| 1 | `ProcessAsync_WithValidJpegImage` | Ảnh JPEG thật 1200x900px | Tạo đúng Medium & Thumbnail, lưu DB, giữ nguyên OriginalUrl | Đầy đủ 2 URL, OriginalUrl nguyên vẹn | **PASS** |
| 2 | `ProcessAsync_WithValidPngImage` | Ảnh PNG thật 1000x500px | Tạo ảnh PNG, giữ đúng đuôi `.png` | Lưu trữ đúng đuôi `_medium.png`, `_thumb.png` | **PASS** |
| 3 | `ProcessAsync_WithValidWebpImage` | Ảnh WebP thật 1600x1200px | Tạo ảnh WebP, giữ đúng đuôi `.webp` | Lưu trữ đúng đuôi `_medium.webp`, `_thumb.webp` | **PASS** |
| 4 | `ProcessAsync_WhenImageIsCorrupt` | Chuỗi byte bị hỏng `0x00, 0x11...` | Ném Exception, không lưu URL giả | Ném ngoại lệ, `MediumUrl` & `ThumbnailUrl` là `null` | **PASS** |
| 5 | `ProcessAsync_WhenStorageUploadFails` | MinIO upload ném `HttpRequestException` | Ném Exception, không commit DB | Ném ngoại lệ, DB không lưu URL rác | **PASS** |
| 6 | `ProcessAsync_WhenRecipeImageDoesNotExist` | ID ngẫu nhiên không tồn tại | Thoát êm đềm, không ném lỗi | Thoát an toàn, Storage không bị gọi | **PASS** |
| 7 | `ProcessAsync_WhenRecipeImageIsSoftDeleted` | Bản ghi có `IsDeleted = true` | Thoát êm đềm, không resize | Bỏ qua xử lý, Storage không bị gọi | **PASS** |
| 8 | `ProcessAsync_WhenAlreadyHasMediumAndThumbnail` | Bản ghi đã có đủ 2 URL | Bỏ qua xử lý (Idempotent) | Bỏ qua xử lý, không upload lại | **PASS** |
| 9 | `ImageResizeJob_HasAutomaticRetryAttribute` | Reflection kiểm tra class | Có `AutomaticRetryAttribute` với Attempts = 3 | Attempts = 3, OnAttemptsExceeded = Fail | **PASS** |
| 10 | `ProcessAsync_WhenCancellationRequested` | CancellationToken bị hủy | Ném `OperationCanceledException` | Ném `OperationCanceledException` ngay lập tức | **PASS** |

---

## 29. KỊCH BẢN HƯỚNG DẪN DEMO THỰC TẾ
1. **Khởi động hạ tầng:** Chạy `docker compose up -d` để khởi động MinIO và PostgreSQL.
2. **Khởi động API:** Chạy `dotnet run --project backend/src/CulinaryBlog.Api`.
3. **Mở Hangfire Dashboard:** Truy cập trình duyệt tại `http://localhost:5000/hangfire`.
4. **Tải lên một ảnh mới:** Sử dụng Postman hoặc Swagger để thêm ảnh công thức nấu ăn.
5. **Quan sát luồng xử lý:**
   * Trong Hangfire Dashboard: Xuất hiện job `ImageResizeJob.ProcessAsync`. Job chuyển từ `Enqueued` sang `Processing` trong vòng 100ms, rồi sang `Succeeded`.
   * Trong MinIO Console (`http://localhost:9001`): Mở bucket `culinaryblog/recipes/{recipeId}/images/`, thấy đủ 3 file:
     * `abc.jpg` (ảnh gốc)
     * `abc_medium.jpg` (ảnh 800px)
     * `abc_thumb.jpg` (ảnh 300px)
   * Trong database: Xem bảng `RecipeImages`, cả 3 cột `OriginalUrl`, `MediumUrl`, `ThumbnailUrl` đều được cập nhật giá trị.

---

## 30. CÁC GIỚI HẠN KỸ THUẬT (LIMITATIONS)
* **Định dạng AVIF:** Thư viện `SixLabors.ImageSharp` thuần C# phiên bản 4.1.2 hiện tại chưa tích hợp sẵn codec giải mã AVIF. Do đó, hệ thống đã chuẩn hóa backend contract từ chối định dạng AVIF ngay từ khâu upload (HTTP 400), đảm bảo toàn bộ ảnh được lưu vào hệ thống đều được Thumbnail Job xử lý thành công 100%.
* **Ảnh SVG:** SVG là đồ họa vector dưới dạng mã XML, không thể áp dụng thuật toán resize ma trận điểm ảnh raster của ImageSharp.

---

## 31. NHỮNG LỖI THƯỜNG GẶP VÀ CÁCH KHẮC PHỤC
1. **Lỗi `UnknownImageFormatException`:** Do file tải lên bị hỏng hoặc người dùng đổi tên file đuôi `.exe`/`.txt` thành `.jpg`. Khắc phục: ImageSharp tự động phát hiện và ném ngoại lệ, job chuyển sang trạng thái lỗi và không ghi URL giả vào DB.
2. **Lỗi `SocketException` / Mất kết nối MinIO:** MinIO server bị tắt hoặc quá tải. Khắc phục: Hangfire tự động kích hoạt retry tối đa 3 lần sau các khoảng thời gian chờ tăng dần.
3. **Lỗi bộ nhớ khi xử lý ảnh khổng lồ (OOM):** Khắc phục: Sử dụng luồng (Stream) và giải phóng bộ nhớ (`using var`) ngay sau khi hoàn tất từng tác vụ.

---

## 32. BỘ 20 CÂU HỎI VẤN ĐÁP CHUYÊN SÂU CHO GIẢNG VIÊN

### Câu 1: Em hãy giải thích bản chất của Thumbnail Job là gì?
> **Trả lời:** Thumbnail Job là một tác vụ chạy nền (Background Job) bất đồng bộ. Nhiệm vụ của nó là đọc ảnh gốc của bài viết công thức, giải mã và co nhỏ kích thước thành 2 phiên bản: Medium (chiều rộng tối đa 800px) phục vụ trang chi tiết và Thumbnail (chiều rộng tối đa 300px) phục vụ trang danh mục, sau đó tải lên MinIO và cập nhật URL vào database.

### Câu 2: Tại sao phải resize ở background mà không làm trực tiếp trong lúc người dùng gửi request upload ảnh?
> **Trả lời:** Quá trình giải mã và tái tạo điểm ảnh (pixel resampling) là tác vụ cực kỳ nặng về CPU và tốn thời gian (từ vài trăm mili-giây đến vài giây). Nếu làm trực tiếp, người dùng sẽ phải chờ lâu, làm giảm trải nghiệm, và có nguy cơ chiếm dụng thread pool của Kestrel server khiến hệ thống bị nghẽn (Thread Starvation) khi nhiều người upload cùng lúc.

### Câu 3: Kích thước Medium và Thumbnail trong code của em là bao nhiêu? Con số này lấy từ đâu?
> **Trả lời:** Trong code của em, Medium có chiều rộng tối đa là 800px và Thumbnail có chiều rộng tối đa là 300px. Đây là **Implementation Decision (Quyết định triển khai kỹ thuật)** của em, vì tài liệu SRS không quy định kích thước pixel cụ thể. Em chọn 800px và 300px vì đây là tỷ lệ vàng tiêu chuẩn cho giao diện web và thiết bị di động.

### Câu 4: Làm thế nào em đảm bảo bức ảnh không bị méo mó (bảo toàn Aspect Ratio)?
> **Trả lời:** Em sử dụng `ResizeOptions` của thư viện `SixLabors.ImageSharp` với kích thước `Size(800, 0)` và chế độ `ResizeMode.Max`. Tham số chiều cao bằng 0 báo cho ImageSharp tự động tính toán chiều cao tương ứng dựa trên tỷ lệ gốc của bức ảnh, giúp bảo toàn tỷ lệ khung hình tuyệt đối.

### Câu 5: Em sử dụng thư viện xử lý ảnh nào? Tại sao lại chọn thư viện đó?
> **Trả lời:** Em sử dụng thư viện `SixLabors.ImageSharp` (phiên bản 4.1.2). Đây là thư viện đồ họa 2D thuần C# (managed code), hoàn toàn độc lập nền tảng (chạy mượt mà trên cả Linux, Windows, macOS và trong Docker container), không phụ thuộc vào thư viện C++ native của hệ điều hành như `System.Drawing.Common` (vốn đã bị Microsoft khai tử trên non-Windows từ .NET 6).

### Câu 6: Hệ thống của em hỗ trợ những định dạng ảnh nào? Định dạng AVIF thì sao?
> **Trả lời:** Hệ thống hỗ trợ đầy đủ các định dạng phổ biến gồm: JPEG, PNG, WebP và GIF. Khi resize, định dạng gốc được bảo toàn (ảnh gốc PNG sẽ xuất ra Medium/Thumbnail dạng PNG). Riêng định dạng AVIF, do ImageSharp chưa có decoder tích hợp sẵn, backend contract đã được tinh chỉnh để từ chối AVIF ngay từ tầng upload (HTTP 400), tránh tình trạng lưu ảnh gốc mà không sinh được thumbnail.

### Câu 7: Hangfire lưu trữ dữ liệu hàng đợi ở đâu?
> **Trả lời:** Hangfire trong dự án của em lưu trữ toàn bộ metadata, thông số job và trạng thái vào cơ sở dữ liệu PostgreSQL trong schema có tên là `hangfire`. Nhờ đó, nếu server có bị sập hoặc khởi động lại, các job đang chờ vẫn không bị mất (Persistent Job Queue).

### Câu 8: Cơ chế thử lại (Retry) của Hangfire hoạt động như thế nào? Cấu hình ở đâu?
> **Trả lời:** Cơ chế retry được cấu hình trực tiếp trên phương thức bằng thuộc tính `[AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]`. Nếu xảy ra ngoại lệ trong quá trình thực thi, Hangfire sẽ thử lại tối đa 3 lần với thời gian chờ tăng dần. Nếu sau 3 lần vẫn lỗi, job sẽ chuyển sang trạng thái `Failed`.

### Câu 9: Tại sao lại giới hạn retry ở 3 lần mà không để retry vô hạn?
> **Trả lời:** Nếu gặp lỗi do ảnh bị hỏng (corrupt file) hoặc lỗi logic dữ liệu, việc retry vô hạn sẽ làm kẹt hàng đợi của worker, gây quá tải CPU và khiến các tác vụ khác không được xử lý. Giới hạn 3 lần vừa đủ để khắc phục lỗi mạng chập chờn, đồng thời bảo vệ hệ thống không bị kiệt sức.

### Câu 10: Tính lũy đẳng (Idempotency) được hiện thực trong code như thế nào?
> **Trả lời:** Trong phương thức `ProcessAsync`, trước khi tiến hành tải ảnh, em kiểm tra xem bản ghi `RecipeImage` đã có cả `MediumUrl` và `ThumbnailUrl` hay chưa. Nếu đã có đầy đủ, job sẽ ghi log thông báo và lập tức thoát ra, không tải và không resize lại, tránh tạo ra các file rác trên MinIO.

### Câu 11: Nếu quá trình upload ảnh Thumbnail lên MinIO bị đứt mạng, database có bị ghi nhận dữ liệu sai không?
> **Trả lời:** Hoàn toàn không. Code của em tuân thủ nguyên tắc tính toàn vẹn trạng thái: Chỉ khi cả ảnh Medium và Thumbnail đều đã được upload thành công lên MinIO, hệ thống mới gán URL vào entity và gọi `_dbContext.SaveChangesAsync()`. Nếu upload thất bại, ngoại lệ được ném ra và database hoàn toàn giữ nguyên trạng thái cũ.

### Câu 12: Bản chất của phương thức `DownloadAsync` trong `S3StorageService` là gì?
> **Trả lời:** Phương thức sử dụng `_s3Client.GetObjectAsync()` để nhận luồng dữ liệu mạng từ MinIO, sau đó copy toàn bộ sang một `MemoryStream` có `Position = 0` và đóng ngay luồng mạng gốc. Việc này giúp đóng socket S3 sớm, giải phóng kết nối cho connection pool và cho phép ImageSharp có thể seek (tua) luồng dữ liệu tự do khi giải mã.

### Câu 13: Làm thế nào `ImageResizeJob` có thể lấy được ảnh gốc từ cả MinIO lẫn URL bên ngoài (seed data)?
> **Trả lời:** Em xây dựng hàm `GetOriginalImageStreamAsync`. Hàm này phân tích URL: Nếu là URL nội bộ MinIO hoặc chứa bucket `culinaryblog`, nó gọi `IStorageService.DownloadAsync`. Nếu là URL HTTP bên ngoài (ví dụ ảnh seed từ Picsum/Unsplash), nó sẽ dùng `HttpClient` để stream tải về. Nhờ vậy, job hoạt động trơn tru trong mọi môi trường.

### Câu 14: Ảnh gốc `OriginalUrl` có bao giờ bị sửa đổi hay bị xóa trong quá trình resize không?
> **Trả lời:** Tuyệt đối không. `OriginalUrl` là tài sản dữ liệu gốc, không bao giờ bị can thiệp. Thuật toán chỉ đọc stream từ nó để sinh ra hai phiên bản mới với tên file có hậu tố `_medium` và `_thumb`.

### Câu 15: Nếu người dùng xóa bài viết hoặc xóa ảnh (`IsDeleted = true`), job xử lý ra sao?
> **Trả lời:** Trong code, em kiểm tra cờ `if (image.IsDeleted)`. Nếu ảnh đã bị xóa mềm, job ghi log cảnh báo và thoát ngay lập tức, không tốn thời gian và băng thông để xử lý một tài nguyên không còn sử dụng.

### Câu 16: Tham số `CancellationToken` có tác dụng gì trong Background Job?
> **Trả lời:** Khi Hangfire server chuẩn bị tắt hoặc ứng dụng bị shutdown (Graceful Shutdown), Hangfire sẽ kích hoạt `CancellationToken`. Việc kiểm tra token trong các phương thức I/O bất đồng bộ giúp dừng tác vụ an toàn ngay lập tức, không để lại tiến trình zombie hay khóa tài nguyên database.

### Câu 17: Em đã viết những bài kiểm thử nào để chứng minh Thumbnail Job hoạt động đúng?
> **Trả lời:** Em đã viết 10 bài unit test tự động trong `ImageResizeJobTests`, sử dụng ảnh nhị phân thật do ImageSharp tạo trực tiếp trong bộ nhớ. Bộ test bao phủ: Resize ảnh JPEG, PNG, WebP, kiểm tra bảo toàn format, kiểm tra aspect ratio, kiểm tra từ chối ảnh hỏng (corrupt image), kiểm tra rollback khi upload MinIO lỗi, kiểm tra tính lũy đẳng, và kiểm tra cấu hình retry 3 lần.

### Câu 18: Tại sao trong Unit Test em không dùng ảnh mẫu (demo image) lưu cứng trên ổ đĩa?
> **Trả lời:** Việc commit ảnh lớn vào Git repository sẽ làm phình to lịch sử commit và làm chậm quá trình clone code. Thay vào đó, em dùng ImageSharp sinh trực tiếp các ma trận pixel nhị phân trong bộ nhớ RAM (In-Memory Image Fixture). Cách này giúp test chạy siêu tốc (chỉ mất vài chục mili-giây) mà vẫn đảm bảo tính chân thực 100% của tệp ảnh nhị phân.

### Câu 19: Làm sao người quản trị có thể theo dõi được tình trạng hoạt động của các Thumbnail Job?
> **Trả lời:** Người quản trị có thể truy cập vào giao diện web Hangfire Dashboard tại đường dẫn `/hangfire`. Tại đây có đầy đủ biểu đồ trực quan, danh sách các job đang chạy, lịch sử các job thành công và chi tiết stack trace của các job bị lỗi.

### Câu 20: Tên file của ảnh Medium và Thumbnail được đặt theo quy tắc nào để không bị trùng lặp?
> **Trả lời:** Tên file được đặt theo quy tắc gắn liền với ID duy nhất (UUID) của bản ghi `RecipeImage`:
> - Medium: `recipes/{recipeId}/images/{imageId}_medium.{extension}`
> - Thumbnail: `recipes/{recipeId}/images/{imageId}_thumb.{extension}`
> Vì mỗi bản ghi ảnh có một UUIDv4 duy nhất, tên file sinh ra đảm bảo tính duy nhất tuyệt đối trong toàn bộ hệ thống lưu trữ MinIO.

---

## 33. BÀI PHÁT BIỂU BẢO VỆ ĐỒ ÁN (2 - 3 PHÚT)

> *"Kính thưa Thầy/Cô và Hội đồng chấm đồ án,*  
> *Em tên là **Võ Hùng Mạnh**. Hôm nay, em xin đại diện nhóm trình bày về phân hệ **Thumbnail Job / Image Resize (JOB-002)** thuộc Task 4 mà em trực tiếp phụ trách.*  
>  
> *Trong các ứng dụng chia sẻ ẩm thực, hình ảnh là yếu tố quan trọng nhất thu hút người xem. Tuy nhiên, nếu người dùng tải lên các bức ảnh dung lượng lớn từ 5MB đến 10MB và hệ thống tải nguyên bản ảnh đó về trang chủ hay danh mục tìm kiếm, website sẽ bị giật lag nghiêm trọng và tiêu tốn rất nhiều băng thông.*  
>  
> *Để giải quyết bài toán này, em đã xây dựng phân hệ xử lý ảnh nền bất đồng bộ với 4 ưu điểm kỹ thuật vượt trội:*  
>  
> * **Thứ nhất, Tách biệt tác vụ nặng bằng Hangfire Background Worker:** Quá trình co giãn ảnh tiêu tốn rất nhiều CPU. Em đã đưa toàn bộ luồng xử lý ra khỏi HTTP request, giao cho Hangfire quản lý trong schema bền vững của PostgreSQL. Người dùng tải ảnh lên nhận được phản hồi thành công ngay lập tức chỉ sau 200ms, trong khi worker chạy ngầm phía sau.*  
> * **Thứ hai, Tối ưu hóa kích thước và Bảo toàn tỷ lệ khung hình:** Em sử dụng thư viện đồ họa thuần C# hiện đại `SixLabors.ImageSharp`. Hệ thống tự động tạo ra phiên bản Medium (chiều rộng tối đa 800px) cho trang chi tiết và Thumbnail (chiều rộng tối đa 300px) cho danh sách tìm kiếm. Thuật toán `ResizeMode.Max` giúp giữ nguyên tỷ lệ khung hình gốc, tuyệt đối không làm méo mó hình ảnh món ăn.*  
> * **Thứ ba, Bảo toàn định dạng và Tương thích đa định dạng:** Hệ thống nhận diện chính xác header nhị phân của ảnh thật, hỗ trợ JPEG, PNG, WebP và bảo toàn đúng định dạng đó khi xuất xưởng sang MinIO.*  
> * **Thứ tư, Khả năng chịu lỗi và Tính lũy đẳng:** Em cấu hình thuộc tính `[AutomaticRetry(Attempts = 3)]` giúp tự động phục hồi khi gặp sự cố mạng tạm thời. Đồng thời, code kiểm tra tính lũy đẳng (Idempotency) để nếu job bị kích hoạt lại, hệ thống sẽ tự động bỏ qua, không gây lãng phí tài nguyên và không sinh rác trên MinIO.*  
>  
> *Toàn bộ phân hệ đã được em kiểm chứng bằng bộ 10 Unit Tests tự động với ảnh binary thật, đạt tỷ lệ Pass 100%.*  
> *Em xin trân trọng cảm ơn Thầy/Cô đã chú ý lắng nghe và em rất mong nhận được câu hỏi từ Hội đồng ạ!"*
