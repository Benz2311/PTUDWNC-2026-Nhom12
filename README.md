# TASK 4 (BỮA 4) – QUẢN LÝ HÌNH ẢNH CÔNG THỨC (RECIPE IMAGE BACKEND)

**Sinh viên:** Võ Hùng Mạnh
**MSSV:** 2312687
**Nhóm:** 12
**Nhánh:** `feat/vohungmanh-recipe-image`

---

### 1. Mục tiêu Task 4 liên quan đến nhánh
Trong khuôn khổ Bữa 4 (Task 4), module Quản lý hình ảnh công thức (`RecipeImage`) cần được hoàn thiện ở tầng Backend nhằm cung cấp giải pháp tải lên và quản lý ảnh an toàn, bảo vệ trước các cuộc tấn công mã độc ngụy tạo đuôi file, quản trị tính duy nhất của ảnh đại diện chính (`IsPrimary`) và kết nối liền mạch với hạ tầng lưu trữ đối tượng MinIO.

### 2. Công việc cá nhân thực tế
- Xây dựng tiện ích bảo mật `ImageValidator`: kiểm tra kích thước tệp (tối đa 5 MB), thẩm định MIME type và phân tích chữ ký số tệp (Magic Bytes) để ngăn chặn việc ngụy tạo tệp tin thực thi (.exe, .sh) thành ảnh (.jpg, .png).
- Triển khai các Command & Handler qua MediatR:
  - `UploadRecipeImageCommand`: Tiếp nhận Stream tệp tin tải lên, gọi `IStorageService` lưu trữ trên MinIO, ghi nhận bản ghi vào cơ sở dữ liệu.
  - `SetPrimaryRecipeImageCommand`: Thiết lập ảnh chính cho công thức với tính toàn vẹn tuyệt đối (chỉ có duy nhất 1 ảnh `IsPrimary = true` trong một công thức).
  - `DeleteRecipeImageCommand`: Xóa bản ghi trong Database và xóa tệp tin tương ứng trên MinIO Storage.
  - `AddRecipeImageCommand`: Bổ sung ảnh từ đường dẫn URL ngoài.
- Tích hợp các Minimal API endpoints trong `RecipeEndpoints.cs` phục vụ tải ảnh, xóa ảnh và gán ảnh chính.
- Viết 224 dòng mã kiểm thử trong `RecipeImageValidationTests.cs` và cập nhật kiểm thử tích hợp trong `CulinaryBlog.IntegrationTests/Program.cs`.

### 3. Chức năng đã hoàn thành
- **Tải lên ảnh đa định dạng an toàn (`POST /api/v1/recipes/{id}/images`):** Tiếp nhận multipart form data, kiểm tra Magic Bytes cho các định dạng JPEG, PNG, WebP. Tự động từ chối các tệp vượt quá 5 MB hoặc tệp tin giả mạo.
- **Quản lý ảnh đại diện duy nhất (`PUT /api/v1/recipes/{id}/images/{imageId}/primary`):** Tự động hạ cờ `IsPrimary = false` cho tất cả ảnh hiện có của công thức trước khi bật `IsPrimary = true` cho ảnh được chọn.
- **Xóa ảnh an toàn (`DELETE /api/v1/recipes/{id}/images/{imageId}`):** Đồng bộ xóa bản ghi khỏi bảng `recipe_images` trong PostgreSQL và xóa đối tượng tương ứng trên MinIO Object Storage, tránh hiện tượng rác dữ liệu mồ côi (orphaned files).
- **Sắp xếp thứ tự (`OrderIndex`):** Hỗ trợ chỉ mục sắp xếp hiển thị ảnh trong thư viện ảnh công thức.

### 4. Cách xử lý và luồng hoạt động
```text
Client Upload Image (Multipart Form File)
        │
        ▼
RecipeEndpoints (Trích xuất RecipeId và Stream tệp tin)
        │
        ▼
ImageValidator (Kiểm tra Magic Bytes & Kích thước <= 5MB)
  ├── Nếu file giả mạo hoặc > 5MB --> Ném InvalidImageException (HTTP 400 ProblemDetails)
  └── Nếu hợp lệ --> Chuyển tiếp tới RecipeImageCommandHandler
        │
        ▼
RecipeImageCommandHandler
  ├── Gọi IStorageService.UploadAsync() tải lên MinIO bucket 'culinary-blog'
  ├── Khởi tạo Entity RecipeImage(ImageUrl, AltText, OrderIndex, IsPrimary)
  ├── Nếu IsPrimary = true --> Reset toàn bộ ảnh khác trong Recipe về IsPrimary = false
  ├── Lưu thực thể vào PostgreSQL qua DbContext
  └── Kích hoạt background job tạo thumbnail (nếu có)
        │
        ▼
Response JSON (201 Created kèm thông tin RecipeImageDto)
```

### 5. Danh sách file mã nguồn quan trọng

| Đường dẫn file | Vai trò và chức năng chính |
| :--- | :--- |
| `backend/src/CulinaryBlog.Application/Common/Utilities/ImageValidator.cs` | 216 dòng mã thẩm định an toàn tệp, kiểm tra Magic Bytes, chặn fake MIME type và file > 5MB. |
| `backend/src/CulinaryBlog.Application/Features/Recipes/Commands/RecipeImages/RecipeImageCommands.cs` | 321 dòng mã định nghĩa các Command và Handler: Upload, Add, SetPrimary, Delete Image. |
| `backend/src/CulinaryBlog.Api/Endpoints/Recipes/RecipeEndpoints.cs` | 151 dòng mã định nghĩa endpoints tải ảnh, quản lý ảnh chính và xóa ảnh. |
| `backend/src/CulinaryBlog.Domain/Entities/RecipeImage.cs` | Thực thể Domain Image chứa nghiệp vụ duy nhất cờ `IsPrimary` và validation thuộc tính. |
| `backend/src/CulinaryBlog.Infrastructure/Data/Configurations/RecipeImageConfiguration.cs` | Cấu hình bảng `recipe_images`, Index `(RecipeId, OrderIndex)` và khóa ngoại `RecipeId`. |
| `backend/tests/CulinaryBlog.UnitTests/Application/Common/RecipeImageValidationTests.cs` | 224 dòng kiểm thử đơn vị kiểm tra validation magic bytes, dung lượng và định dạng cho phép. |
| `backend/tests/CulinaryBlog.IntegrationTests/Program.cs` | Kịch bản kiểm thử tích hợp thực tế upload và gán ảnh chính. |
| `docs/README_RECIPE_IMAGE.md` | Tài liệu kỹ thuật chi tiết về module RecipeImage Backend. |
| `docs/VO_HUNG_MANH_RECIPE_IMAGE_COMPLAN.md` | Bản phân tích và đặc tả ma trận kiểm thử chi tiết Bữa 4. |

### 6. API, Database và Infrastructure liên quan
- **Cơ sở dữ liệu:** PostgreSQL 16 (Bảng `recipe_images`).
- **Lưu trữ đối tượng:** MinIO Object Storage (Bucket `culinary-blog`).
- **Endpoints REST API:**
  - `POST /api/v1/recipes/{id}/images`: Tải lên hình ảnh mới cho công thức.
  - `PUT /api/v1/recipes/{id}/images/{imageId}/primary`: Đặt làm ảnh đại diện chính.
  - `DELETE /api/v1/recipes/{id}/images/{imageId}`: Xóa ảnh khỏi hệ thống.

### 7. Hướng dẫn chạy

#### Bước 1: Khởi động cơ sở dữ liệu và MinIO
```bash
docker compose up -d postgres minio
```

#### Bước 2: Chạy Backend Web API
```bash
cd backend/src/CulinaryBlog.Api
dotnet run
```

### 8. Hướng dẫn kiểm thử

#### Kiểm thử Unit Test tự động
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --filter "FullyQualifiedName~RecipeImage"
```

#### Kiểm thử thực tế bằng cURL
```bash
# Tải ảnh lên công thức
curl -X POST "http://localhost:5000/api/v1/recipes/{recipeId}/images" \
  -F "file=@mon-an.jpg" \
  -F "isPrimary=true" \
  -F "altText=Hình ảnh tô phở bò đặc biệt"
```

### 9. Kết quả kiểm thử có bằng chứng
- **Kết quả Unit Test:** Toàn bộ test case trong `RecipeImageValidationTests` đều đạt `Passed!` (xác nhận chặn thành công file exe đổi đuôi .jpg, chặn file 6MB, và nhận diện chính xác header JPEG/PNG/WebP).
- **Kết quả Database & Storage:** Sau khi upload, bản ghi được tạo trong bảng `recipe_images`, đồng thời tệp tin xuất hiện trên MinIO console.

### 10. Hạn chế và tồn đọng
- **Trạng thái thực tế:** Hoàn thiện 100% backend API, validation magic bytes và xóa đồng bộ MinIO.
- **Tồn đọng:** Giao diện xem trước ảnh (ImagePreview modal) và kéo thả tải lên nhiều ảnh (Drag-and-Drop Image Gallery) phía Frontend được phân bổ thực hiện ở Task 6 và hoàn thiện tối ưu tại Task 8.

### 11. Liên kết tài liệu kỹ thuật chi tiết
- Chi tiết API và nghiệp vụ hình ảnh: [docs/README_RECIPE_IMAGE.md](docs/README_RECIPE_IMAGE.md)
- Kế hoạch triển khai và ma trận kiểm thử: [docs/VO_HUNG_MANH_RECIPE_IMAGE_COMPLAN.md](docs/VO_HUNG_MANH_RECIPE_IMAGE_COMPLAN.md)

---

# Phát triển ứng dụng Web nâng cao

## Nhóm 12

### Thành viên
1. Võ Hùng Mạnh - 2312687 - 2312687@dlu.edu.vn - Nhóm trưởng
2. Lê Thị Ánh Nhung - 2312709 - 2312709@dlu.edu.vn
3. Phạm Nguyễn Ngọc Phước - 2312718 - 2312718@dlu.edu.vn
4. Nguyễn Văn Quốc - 2312729 - 2312729@dlu.edu.vn
