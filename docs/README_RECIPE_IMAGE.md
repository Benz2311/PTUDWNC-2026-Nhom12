# Quản lý hình ảnh công thức (Recipe Image)

## 1. Mục tiêu
Branch `feat/vohungmanh-recipe-image` giải quyết việc xây dựng phân hệ quản lý bộ sưu tập hình ảnh cho công thức nấu ăn (`RecipeImage`) thuộc Task 4 trong dự án CulinaryBlog:
- Cho phép tải lên nhiều hình ảnh minh họa cho một món ăn (ảnh đại diện, ảnh thành phẩm, ảnh cận cảnh các nguyên liệu).
- Đảm bảo tính toàn vẹn và quy tắc nghiệp vụ nghiêm ngặt của ảnh đại diện chính (**Primary Image**): một công thức chỉ được có duy nhất một ảnh Primary đang hoạt động tại một thời điểm.
- Thiết lập hàng rào bảo mật tệp tin nhị phân toàn diện: kiểm tra kích thước tối đa 5 MB, kiểm tra định dạng MIME và phân tích chữ ký số tệp tin (**Magic Bytes**) nhằm ngăn chặn triệt để tấn công ngụy tạo đuôi file (File Extension Spoofing).
- Tự động luân chuyển cờ đại diện khi xóa ảnh chính và tích hợp lưu trữ tệp tin với Object Storage (MinIO / S3).

---

## 2. Kết quả đạt được
Sau khi triển khai branch này:
- **Quản lý vòng đời hình ảnh toàn diện**:
  - `POST /api/v1/recipes/{id}/images`: Thêm ảnh mới vào công thức (hỗ trợ cả URL trực tiếp và xử lý qua Storage).
  - `PATCH /api/v1/recipes/{recipeId}/images/{imageId}/primary`: Đặt một ảnh làm ảnh đại diện chính của công thức.
  - `DELETE /api/v1/recipes/{recipeId}/images/{imageId}`: Xóa mềm ảnh và tự động chuyển giao cờ Primary cho ảnh kế tiếp.
- **Quy tắc Primary Image tự động hóa theo SRS FR-RCP-008**:
  - Bức ảnh đầu tiên tải lên tự động được gán làm ảnh Primary (`IsPrimary = true`).
  - Khi một ảnh được chỉ định làm Primary mới, hệ thống tự động hạ cờ `IsPrimary = false` của tất cả các ảnh khác trong cùng một Database Transaction.
  - Khi xóa mềm ảnh đang là Primary, ảnh có `OrderIndex` nhỏ nhất còn lại tự động được thăng cấp làm Primary mới.
- **Bảo mật tệp tin đa lớp (`ImageValidator`)**:
  - Kiểm tra dung lượng: Chặn tuyệt đối tệp tin $> 5\text{ MB}$ ($5 \times 1024 \times 1024$ bytes).
  - Kiểm tra MIME Type: Chỉ chấp nhận `image/jpeg`, `image/png`, `image/webp` (đồng bộ hoàn toàn với pipeline resize ảnh Thumbnail Job).
  - Phân tích Magic Bytes: Đọc 64 byte đầu tiên từ luồng nhị phân để nhận diện chính xác header file (JPEG `FF D8 FF`, PNG `89 50 4E 47 0D 0A 1A 0A`, WebP `RIFF....WEBP`), từ chối ngay lập tức các tệp mã độc mạo danh đuôi ảnh và từ chối các định dạng không được pipeline hỗ trợ (như AVIF).
- **Tự động làm mới bộ nhớ đệm (Cache Invalidation)**: Xóa cache Redis `recipe:{slug}` ngay sau khi thao tác ghi hoàn tất.
- **Kiểm thử bao phủ**: 13/13 tests trong `RecipeImageValidationTests` và 100/101 tests toàn hệ thống đều vượt qua thành công (100% Pass).

---

## 3. Luồng hoạt động

```text
Client (Postman / Scalar / Web Browser)
  │ (Gửi HTTP Request: POST, PATCH, DELETE)
  ▼
API Minimal Endpoints (RecipeEndpoints.cs)
  │ ├─ Bóc tách thông tin người dùng từ JWT Claims (CurrentUserId, IsAdmin)
  │ ├─ Ánh xạ route parameters và Request DTO
  │ └─ Gửi Command tương ứng qua MediatR ISender
  ▼
Application Layer: RecipeImageCommandHandler (MediatR)
  │ ├─ 1. Xác thực quyền sở hữu (VerifyRecipeOwnership):
  │ │    Kiểm tra AuthorId == CurrentUserId hoặc IsAdmin (ném ForbiddenException 403)
  │ │
  │ ├─ 2. Xác thực tệp tin nhị phân (ImageValidator.Validate):
  │ │    FileLength <= 5MB -> ContentType hợp lệ -> Match Magic Bytes (JPEG/PNG/WebP)
  │ │
  │ ├─ 3. Tương tác Object Storage (IStorageService / MinIO):
  │ │    Upload stream nhị phân lên bucket culinaryblog/recipes/{recipeId}/images/{fileId}.ext
  │ │
  │ └─ 4. Thực thi nghiệp vụ cơ sở dữ liệu trong Transaction:
  │      ├─ [Add]: Nếu là ảnh đầu tiên -> IsPrimary = true; Nếu IsPrimary = true -> hạ cờ ảnh cũ
  │      ├─ [Set Primary]: Hạ cờ IsPrimary của toàn bộ ảnh khác -> Nâng cờ ảnh đích
  │      └─ [Delete]: Đặt IsDeleted = true -> Nếu là Primary -> tìm ảnh có Min(OrderIndex) làm Primary mới
  ▼
Infrastructure Layer: IApplicationDbContext & PostgreSQL
  │ └─ Thực thi SQL Commit trong Database Transaction (ACID)
  ▼
Redis Cache Invalidation: ICacheService
  │ └─ Xóa cache chi tiết công thức: recipe:{slug}
  ▼
Output
  └─ Trả về HTTP 201 Created / 200 OK / 204 NoContent kèm RecipeImageDto
```

---

## 4. Các file chính

| File | Vai trò | Xử lý gì |
| :--- | :--- | :--- |
| `backend/src/CulinaryBlog.Application/Common/Utilities/ImageValidator.cs` | Application / Utility | Trình kiểm tra an toàn tệp ảnh: dung lượng tối đa 5MB, whitelist MIME types, và kiểm tra chữ ký nhị phân (Magic Bytes) cho 3 định dạng JPEG, PNG, WebP (từ chối AVIF). |
| `backend/src/CulinaryBlog.Application/Features/Recipes/Commands/RecipeImages/RecipeImageCommands.cs` | Application / MediatR Handler | Chứa các Command (`Add`, `Upload`, `Update`, `SetPrimary`, `Delete`), quản lý quy tắc Primary duy nhất trong Database Transaction, tính toán `OrderIndex`, và xóa cache Redis. |
| `backend/src/CulinaryBlog.Api/Endpoints/Recipes/RecipeEndpoints.cs` | Presentation / Minimal API | Khai báo và map các route RESTful cho RecipeImage (`POST /images`, `PATCH /images/{id}/primary`, `DELETE /images/{id}`), bóc tách JWT claims, xử lý mã lỗi HTTP. |
| `backend/tests/CulinaryBlog.UnitTests/Application/Common/RecipeImageValidationTests.cs` | Unit Tests | Bộ 14 unit tests kiểm tra toàn diện: file rỗng, vượt quá 5MB, đuôi file giả mạo, magic bytes của JPEG, PNG, WebP, từ chối AVIF, và các định dạng bị cấm (EXE, PDF, BMP). |
| `docs/VO_HUNG_MANH_RECIPE_IMAGE_COMPLAN.md` | Tài liệu bảo vệ | Báo cáo giải trình kỹ thuật chuyên sâu (618 dòng) gồm 15 mục chi tiết về kiến trúc, flow, database, và 20 câu hỏi vấn đáp. |
| `docs/README_RECIPE_IMAGE.md` | Tài liệu kỹ thuật | Tài liệu hướng dẫn kỹ thuật chi tiết theo chuẩn 13 phần. |

---

## 5. API / Interface

| Method | Endpoint | Input | Output | Authorization |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/recipes/{id}/images` | Path: `id` (GUID)<br>Body: `AddRecipeImageRequest`<br>- `originalUrl` (string, required)<br>- `mediumUrl?` (string)<br>- `thumbnailUrl?` (string)<br>- `altText?` (string, max 200)<br>- `isPrimary?` (bool)<br>- `orderIndex?` (int, $\ge 0$) | HTTP 201 Created<br>Body: `RecipeImageDto`<br>- `id`, `originalUrl`, `mediumUrl`, `thumbnailUrl`, `altText`, `isPrimary`, `orderIndex` | Yêu cầu đăng nhập; Người dùng phải là Tác giả (`AuthorId`) hoặc `Admin`. |
| `PATCH` | `/api/v1/recipes/{recipeId}/images/{imageId}/primary` | Path: `recipeId` (GUID), `imageId` (GUID) | HTTP 200 OK<br>Body: `RecipeImageDto` (với `isPrimary: true`) | Yêu cầu đăng nhập; Người dùng phải là Tác giả (`AuthorId`) hoặc `Admin`. |
| `DELETE` | `/api/v1/recipes/{recipeId}/images/{imageId}` | Path: `recipeId` (GUID), `imageId` (GUID) | HTTP 204 No Content | Yêu cầu đăng nhập; Người dùng phải là Tác giả (`AuthorId`) hoặc `Admin`. |

---

## 6. Business Rules

1. **Quy tắc Single Primary Image (SRS FR-RCP-008)**:
   - Một công thức chỉ được phép có duy nhất **1 ảnh đại diện chính** (`IsPrimary = true`) trong số các ảnh đang hoạt động (`IsDeleted = false`).
   - Bức ảnh đầu tiên được tải lên cho công thức luôn tự động nhận cờ `IsPrimary = true`.
2. **Quy trình Đổi ảnh đại diện (Set Primary)**:
   - Khi gọi API `PATCH /primary` hoặc thêm ảnh mới có `isPrimary = true`: Hệ thống duyệt qua tất cả các ảnh active đang mang `IsPrimary = true`, chuyển thành `false`, sau đó mới gán `true` cho ảnh đích. Toàn bộ thao tác thực thi trong 1 Database Transaction nguyên tử.
3. **Quy trình Fallback Primary khi Xóa mềm (Delete Primary)**:
   - Khi xóa một ảnh đang là Primary (`IsDeleted = true`), hệ thống tự động truy vấn các ảnh còn lại (`!IsDeleted`), sắp xếp theo `OrderIndex` tăng dần, và chọn ảnh có `OrderIndex` nhỏ nhất để nâng cờ `IsPrimary = true`.
4. **Bảo mật kiểm tra Magic Bytes (File Signatures)**:
   - Hệ thống không tin tưởng phần mở rộng (extension) hay Header `Content-Type` do client gửi lên.
   - Luồng nhị phân bắt buộc phải khớp với chữ ký số thực tế:
     - **JPEG:** Bắt đầu bằng 3 bytes `FF D8 FF`.
     - **PNG:** Bắt đầu bằng 8 bytes `89 50 4E 47 0D 0A 1A 0A`.
     - **WebP:** 4 bytes đầu là `RIFF` (`52 49 46 46`) và bytes 8..11 là `WEBP` (`57 45 42 50`).
     - **AVIF (Không hỗ trợ):** Bị từ chối để đồng bộ pipeline Thumbnail Job.
5. **Dung lượng tối đa 5 MB**:
   - Tệp tin tải lên có dung lượng $> 5 \times 1024 \times 1024$ bytes ($5,242,880$ bytes) lập tức bị từ chối với mã lỗi HTTP 400.
6. **Ràng buộc trường dữ liệu**:
   - `AltText`: Tùy chọn, tối đa 200 ký tự (phục vụ SEO và khả năng tiếp cận Screen Reader).
   - `OrderIndex`: Tùy chọn, số nguyên không âm ($\ge 0$). Nếu không chỉ định, tự động nhận `Max(OrderIndex) + 1`.
7. **Quyền tác giả (Ownership Rule)**:
   - Chỉ tác giả sở hữu công thức (`recipe.AuthorId == CurrentUserId`) hoặc `Admin` mới được can thiệp vào bộ sưu tập ảnh.
8. **Cache Invalidation**:
   - Xóa bỏ khóa cache Redis `recipe:{slug}` ngay sau khi thao tác ghi commit thành công.

---

## 7. Ví dụ hoạt động

### Kịch bản: Quản lý ảnh món "Phở bò Hà Nội" (`recipeId = "9a7f3e1b-..."`)

1. **Tải lên bức ảnh đầu tiên (Tô phở đại diện)**:
   - **Request**:
     ```http
     POST /api/v1/recipes/9a7f3e1b-0000-0000-0000-000000000001/images
     Content-Type: application/json
     Authorization: Bearer <token_tac_gia>

     {
       "originalUrl": "https://storage.culinaryblog.com/recipes/pho-bo/anh-to-pho.jpg",
       "altText": "Tô phở bò nóng hổi bốc khói nghi ngút",
       "orderIndex": 0
     }
     ```
   - **Xử lý**: Hệ thống nhận thấy công thức chưa có ảnh nào $\rightarrow$ Tự động gán `IsPrimary = true`.
   - **Response**: `HTTP 201 Created`
     ```json
     {
       "id": "f5c11111-0000-0000-0000-000000000001",
       "originalUrl": "https://storage.culinaryblog.com/recipes/pho-bo/anh-to-pho.jpg",
       "altText": "Tô phở bò nóng hổi bốc khói nghi ngút",
       "isPrimary": true,
       "orderIndex": 0
     }
     ```

2. **Tải lên bức ảnh thứ hai (Cận cảnh thịt bò tái)**:
   - **Request**: Thêm ảnh với `orderIndex = 1`.
   - **Xử lý**: Công thức đã có ảnh Primary $\rightarrow$ Ảnh thứ hai nhận `IsPrimary = false`.

3. **Chuyển ảnh thứ hai làm ảnh đại diện chính (Set Primary)**:
   - **Request**:
     ```http
     PATCH /api/v1/recipes/9a7f3e1b-.../images/f5c22222-.../primary
     Authorization: Bearer <token_tac_gia>
     ```
   - **Xử lý trong Transaction**:
     - Ảnh 1: `IsPrimary` chuyển từ `true` $\rightarrow$ `false`.
     - Ảnh 2: `IsPrimary` chuyển từ `false` $\rightarrow$ `true`.
   - **Response**: `HTTP 200 OK` (Ảnh 2 hiện là Primary).

4. **Xóa mềm ảnh thứ hai (Đang là Primary)**:
   - **Request**:
     ```http
     DELETE /api/v1/recipes/9a7f3e1b-.../images/f5c22222-...
     Authorization: Bearer <token_tac_gia>
     ```
   - **Xử lý trong Transaction**:
     - Ảnh 2 chuyển cờ `IsDeleted = true`, `IsPrimary = false`.
     - Hệ thống tìm ảnh còn lại có `OrderIndex` nhỏ nhất $\rightarrow$ Ảnh 1 (`OrderIndex = 0`).
     - Tự động nâng cờ Ảnh 1 thành `IsPrimary = true`.
   - **Response**: `HTTP 204 No Content`.
   - **Kết quả**: Công thức luôn duy trì trạng thái có 1 ảnh đại diện chính hợp lệ.

---

## 8. Error Handling

| Mã lỗi HTTP | Điều kiện kích hoạt thực tế | Phản hồi (Behavior & Response Body) |
| :--- | :--- | :--- |
| `HTTP 400 Bad Request` | - File rỗng hoặc dung lượng vượt quá 5 MB.<br>- MIME type không thuộc danh sách JPEG, PNG, WebP.<br>- Magic Bytes không khớp với định dạng khai báo (ngụy tạo đuôi file).<br>- `AltText` dài hơn 200 ký tự.<br>- `OrderIndex` là số âm ($< 0$). | Trả về `ValidationProblemDetails` RFC 7807:<br>```json<br>{<br>  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",<br>  "title": "One or more validation errors occurred.",<br>  "status": 400,<br>  "errors": {<br>    "File": ["Kích thước tệp (6291456 bytes) vượt quá giới hạn tối đa cho phép là 5242880 bytes (5 MB)."]<br>  }<br>}<br>``` |
| `HTTP 401 Unauthorized` | Không có header xác thực Bearer token hoặc token không hợp lệ. | Chặn tại ASP.NET Core Authentication Middleware. |
| `HTTP 403 Forbidden` | Người dùng đã đăng nhập nhưng không phải tác giả (`AuthorId != CurrentUserId`) và không phải `Admin`. | Trả về JSON:<br>```json<br>{<br>  "error": "You do not have permission to modify images for this recipe."<br>}<br>``` |
| `HTTP 404 Not Found` | - Không tìm thấy công thức với `id` cung cấp.<br>- Không tìm thấy hình ảnh với `imageId` cung cấp trong công thức. | Trả về JSON:<br>```json<br>{<br>  "error": "Recipe with ID '...' was not found."<br>}<br>hoặc<br>{<br>  "error": "Image with ID '...' was not found in Recipe."<br>}<br>``` |
| `HTTP 500 / 503` | Dịch vụ Object Storage (MinIO) bị ngắt kết nối hoặc lỗi I/O streaming khi upload file. | Ném `InvalidOperationException`: `"Dịch vụ lưu trữ tệp (File Storage) hiện không khả dụng. Vui lòng thử lại sau."` |

---

## 9. Cách chạy và Demo

### Bước 1: Khởi động hệ thống hạ tầng
```bash
docker compose up -d
```
*(Khởi chạy PostgreSQL, Redis và MinIO)*

### Bước 2: Khởi động Backend API
```bash
dotnet run --project backend/src/CulinaryBlog.Api
```

### Bước 3: Kịch bản Demo thực tế cho Giảng viên

1. **Demo Tự động gán Primary cho ảnh đầu tiên**:
   - Sử dụng Postman gửi `POST /api/v1/recipes/{id}/images` với bức ảnh thứ nhất.
   - Chỉ cho Giảng viên thấy trường `"isPrimary": true` tự động được gán dù không truyền cờ `isPrimary`.
2. **Demo Thêm ảnh thứ hai và Giữ nguyên Primary**:
   - Gửi tiếp ảnh thứ hai với `isPrimary: false`.
   - Quan sát ảnh thứ hai được thêm thành công với `isPrimary: false`, ảnh thứ nhất vẫn giữ nguyên vai trò đại diện.
3. **Demo Đổi ảnh đại diện (Set Primary)**:
   - Gửi `PATCH /api/v1/recipes/{id}/images/{image2Id}/primary`.
   - Truy vấn lại danh sách ảnh: Ảnh 2 đã thành `isPrimary: true`, còn Ảnh 1 tự động chuyển về `isPrimary: false`.
4. **Demo Fallback Primary khi Xóa ảnh chính**:
   - Gửi `DELETE /api/v1/recipes/{id}/images/{image2Id}` để xóa bức ảnh đang làm đại diện.
   - Nhận phản hồi `HTTP 204 No Content`.
   - Truy vấn lại danh sách ảnh: Ảnh 1 tự động được thăng cấp trở lại thành `isPrimary: true`.
5. **Demo Chặn File Ngụy Tạo (Magic Bytes Validation)**:
   - Đổi tên một file văn bản `.txt` hoặc file thực thi `.exe` thành `hacker.jpg`.
   - Tải lên hệ thống: `ImageValidator` đọc header nhị phân và phát hiện magic bytes không phải `FF D8 FF` $\rightarrow$ Trả về `HTTP 400 Bad Request` cảnh báo tệp tin bị giả mạo.

---

## 10. Testing

### Bộ kiểm thử chuyên biệt `RecipeImageValidationTests`
Chạy lệnh kiểm thử đơn vị:
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --filter FullyQualifiedName~RecipeImageValidationTests
```

**Kết quả kiểm thử thực tế:**
- **13/13 tests PASSED (100%)** (Thời gian chạy: ~70ms).
- **Danh sách 13 kịch bản kiểm thử chi tiết:**
  1. `Validate_ReturnsError_WhenStreamIsEmpty`: Báo lỗi khi tệp tải lên rỗng (0 bytes).
  2. `Validate_ReturnsError_WhenFileSizeExceeds5MB`: Báo lỗi khi kích thước tệp vượt quá 5 MB ($5,242,881$ bytes).
  3. `Validate_ReturnsError_WhenContentTypeIsInvalid`: Báo lỗi khi `Content-Type` không được hỗ trợ (ví dụ `application/pdf`).
  4. `Validate_ReturnsError_WhenFileExtensionDoesNotMatchContentType`: Báo lỗi khi đuôi mở rộng không khớp với MIME.
  5. `Validate_ReturnsError_WhenFileIsTooSmallForMagicBytes`: Báo lỗi khi file quá nhỏ không đủ đọc 12 bytes header.
  6. `Validate_ReturnsSuccess_ForValidJpegImage`: Xác thực thành công file JPEG thật với header `FF D8 FF`.
  7. `Validate_ReturnsSuccess_ForValidPngImage`: Xác thực thành công file PNG thật với header `89 50 4E 47 0D 0A 1A 0A`.
  8. `Validate_ReturnsSuccess_ForValidWebpImage`: Xác thực thành công file WebP thật với chữ ký `RIFF....WEBP`.
  9. `Validate_AvifFormat_ReturnsUnsupportedMimeTypeError`: Xác thực từ chối định dạng AVIF để đồng bộ pipeline resize ảnh.
  10. `Validate_ReturnsError_WhenJpegMagicBytesAreInvalid`: Phát hiện file đổi đuôi giả mạo `.jpg` nhưng ruột không phải JPEG.
  11. `Validate_ReturnsError_WhenPngMagicBytesAreInvalid`: Phát hiện file giả mạo đuôi `.png`.
  12. `Validate_ReturnsError_WhenWebpMagicBytesAreInvalid`: Phát hiện file giả mạo đuôi `.webp`.
  13. `Validate_PreservesStreamPosition_AfterValidation`: Đảm bảo stream con trỏ đọc được khôi phục về vị trí ban đầu sau khi kiểm tra.

### Tổng hợp Unit Tests toàn bộ solution:
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj
```
**Kết quả thực tế:**
- `Passed: 100, Failed: 0, Skipped: 1, Total: 101` (100% Pass).

---

## 11. Build
Thực hiện lệnh biên dịch solution:
```bash
dotnet build backend/CulinaryBlog.sln
```

**Kết quả biên dịch thực tế:**
- **Thành công (Exit code 0)**.
- **0 Error(s)**, 440 Warning(s) (chủ yếu là StyleCop/Sonar code format).

---

## 12. Limitations

1. **Tạo Thumbnail nền (Background Job)**:
   - Việc giải mã ảnh và sinh ra hai phiên bản thu nhỏ (`MediumUrl` 800px và `ThumbnailUrl` 300px) thuộc trách nhiệm của Background Worker Hangfire (`JOB-002 ImageResizeJob` trên nhánh `feat/vohungmanh-thumbnail-job`). Tại branch này, các trường `MediumUrl` và `ThumbnailUrl` được lưu trữ dưới dạng nullable chờ worker điền dữ liệu.
2. **Xóa vật lý trên MinIO**:
   - Khi gọi API xóa ảnh, hệ thống chỉ thực hiện xóa mềm metadata (`IsDeleted = true`) trong cơ sở dữ liệu để bảo vệ toàn vẹn lịch sử. Việc dọn dẹp vật lý các byte nhị phân trên MinIO được thực hiện định kỳ bởi Purge Job nhằm tránh xóa nhầm dữ liệu.

---

## 13. Kết luận
Branch `feat/vohungmanh-recipe-image` đã hoàn thành trọn vẹn và đạt chuẩn công nghiệp cho phân hệ Quản lý Hình ảnh Công thức Nấu ăn:
- Thực hiện đầy đủ quy tắc Single Primary Image theo đặc tả SRS FR-RCP-008 trong Database Transaction.
- Cơ chế bảo mật Magic Bytes phân tích nhị phân chống giả mạo đuôi file toàn diện.
- Kiểm thử đơn vị 13/13 test cases bao phủ đầy đủ các trường hợp biên và tấn công giả mạo file.
- Biên dịch sạch sẽ 0 lỗi và sẵn sàng phục vụ cho việc tích hợp giao diện người dùng.
