# TÀI LIỆU KỸ THUẬT VÀ BÀN GIAO CODE: RECIPE IMAGE MANAGEMENT (TASK 6)

---

## 1. Thông tin branch, task và tác giả

- **Họ và tên:** Võ Hùng Mạnh
- **MSSV:** 2312687
- **Vai trò:** Nhóm trưởng Nhóm 12 – Dự án Phát triển ứng dụng Web nâng cao
- **Tên branch:** `feat/vohungmanh-task6-image`
- **Tên Task / Bữa:** Bữa 6 – Hoàn thiện tiện ích dùng chung, State quản lý và Giao diện quản lý bộ sưu tập hình ảnh (Image Utilities, State & Shared Components), chuẩn bị liên thông cho Bữa 7 & 8
- **Repository:** `https://github.com/Benz2311/PTUDWNC-2026-Nhom12`
- **Thư mục local (Git Worktree):** `B:\PTUDWNC-2026-Nhom12-MANH\.worktrees\image`
- **Commit mới nhất trên branch:** `c83ee31` (*fix(image): chuẩn hóa PATCH endpoint và validation URL 500 ký tự theo SRS v1.2.0*)
- **Trạng thái branch:** Đang hoạt động (Active), hoàn thành 100% mục tiêu Task 6, mã nguồn độc lập, sạch sẽ và sẵn sàng tích hợp vào `main`.

---

## 2. Mục tiêu và phạm vi

### 2.1. Mục tiêu
- Xây dựng hoàn chỉnh tầng Front-end cho phân hệ quản lý thư viện hình ảnh món ăn (Recipe Image Management) theo đúng phân công Bữa 6 trong README tổng dự án.
- Cung cấp giải pháp tải lên và quản lý hình ảnh đa dạng: tải tệp trực tiếp từ máy tính (`multipart/form-data`), dán liên kết URL ảnh có sẵn, xem trước ảnh phóng to (Image Preview Modal), đặt ảnh đại diện chính (Set Primary), xóa ảnh an toàn và quản lý danh sách ảnh phụ.
- Đảm bảo xử lý đúng chuẩn `multipart/form-data` phía Client (để Browser/Axios tự động sinh boundary, không can thiệp sai `Content-Type`), hỗ trợ hiển thị tiến trình tải lên (`uploadProgress`), và kiểm soát chặt chẽ dung lượng tối đa 5 MB cùng các định dạng ảnh hợp lệ (JPG, PNG, WebP, AVIF).
- Tuân thủ nghiêm ngặt quy định nghiệp vụ SRS v1.2.0 (Mục 8.3 & Bảng 7.5): endpoint cập nhật ảnh và gán Primary chuẩn hóa dạng `PATCH /api/v1/recipes/{id}/images/{imageId}`, giới hạn độ dài URL tối đa 500 ký tự, và cơ chế tự động chuyển Primary cho ảnh còn lại có `OrderIndex` nhỏ nhất khi ảnh chính bị xóa.

### 2.2. Phạm vi và phân định ranh giới
- **Phạm vi thuộc branch này (Võ Hùng Mạnh - Bữa 6 Frontend):**
  - Xây dựng module `lib/api/images.ts` tích hợp API Client chung.
  - Xây dựng thư viện thẩm định tệp `lib/image-validator.ts`.
  - Xây dựng Custom Hook `hooks/useRecipeImages.ts`.
  - Xây dựng các Component giao diện: `ImageManager`, `ImageCard`, `ImagePreview`, `ImageUploadButton`.
  - Bộ kiểm thử tự động Jest `components/recipe-management/__tests__/ImageManager.test.tsx`.
- **Ranh giới với Backend Bữa 4:**
  - Tầng Backend bao gồm dịch vụ MinIO Object Storage (`S3StorageService.cs`), Background Job tự động tạo Medium/Thumbnail bằng Hangfire (`ImageResizeJob.cs`), và các Endpoint Image (`POST`, `PATCH`, `DELETE`) **đã được Võ Hùng Mạnh xây dựng ở Bữa 4** và đã merge vào `main`. Branch này không nhận lại Backend Bữa 4 làm thành quả mới, mà tập trung 100% vào giải pháp tầng Front-end và trải nghiệm người dùng.
- **Ranh giới với các thành viên khác:**
  - `RecipeForm` tổng thể thuộc sở hữu của Phạm Nguyễn Ngọc Phước (TV3). Phước chịu trách nhiệm nhúng `ImageManager` vào trang tạo/sửa công thức; toàn bộ State, Modal preview, Xử lý upload tệp và Logic Primary do component của Võ Hùng Mạnh tự quản lý độc lập.

---

## 3. Công việc thực tế đã triển khai

Đối chiếu giữa phân công trong README tổng và mã nguồn thực tế:

1. **Chuẩn hóa API Client (`lib/api/images.ts`):**
   - Viết các hàm giao tiếp RESTful: `getRecipeImages(slugOrId)`, `addRecipeImage(recipeId, input)`, `uploadRecipeImageFile(recipeId, file, altText, isPrimary, onUploadProgress)`, `updateRecipeImage(recipeId, imageId, input)`, `setPrimaryRecipeImage(recipeId, imageId)`, `deleteRecipeImage(recipeId, imageId)`, và `uploadImageFile(file)`.
   - Chuẩn hóa endpoint gán Primary: Sử dụng `PATCH /api/v1/recipes/{id}/images/{imageId}` với body `{ isPrimary: true }` theo đúng SRS v1.2.0 Mục 8.3.
   - Xử lý tải tệp `multipart/form-data`: Không can thiệp header `Content-Type` thủ công, cho phép Browser và Axios tự động phát sinh boundary an toàn.
   - Định nghĩa lớp biệt lệ `ImageApiError`, hỗ trợ bóc tách lỗi 401, 403, 404 và mã lỗi `503 Service Unavailable` khi dịch vụ MinIO Object Storage gặp sự cố.
2. **Thư viện thẩm định tệp ảnh (`lib/image-validator.ts`):**
   - Tiền kiểm tra kích thước: Giới hạn tối đa 5 MB (`5 * 1024 * 1024` bytes).
   - Tiền kiểm tra định dạng: Chỉ chấp nhận `image/jpeg`, `image/jpg`, `image/png`, `image/webp`, `image/avif` và các phần mở rộng `.jpg`, `.jpeg`, `.png`, `.webp`, `.avif`.
   - Chặn đứng các tệp rỗng hoặc định dạng không an toàn ngay tại Client, phản hồi thông điệp tiếng Việt thân thiện.
3. **Quản lý State và Tiến trình tải ảnh (`hooks/useRecipeImages.ts`):**
   - Đóng gói toàn bộ logic State: `images`, `primaryImage`, `isLoading`, `isUploading`, `isDeleting`, `uploadProgress` (0-100%), `error`, `previewImage`.
   - Hỗ trợ cả 2 chế độ: Direct API (có `recipeId`) và Controlled Local (tạo ảnh blob preview cục bộ khi chưa lưu Recipe).
   - Tự động hoán đổi cờ `isPrimary` trên toàn bộ danh sách khi có ảnh mới được đặt làm Primary, và tự động gán Primary cho ảnh có `orderIndex` nhỏ nhất khi ảnh chính bị xóa.
4. **Thẻ hiển thị ảnh (`components/recipe-management/ImageCard.tsx`):**
   - Khung hình chuẩn tỉ lệ 16:9 (`aspect-video`), hiệu ứng hover phóng to nhẹ (`group-hover:scale-105`).
   - Huy hiệu `Ảnh đại diện` màu xanh ngọc bích `bg-emerald-700 text-white` nổi bật tại góc trên bên trái.
   - Nút Xem lớn (Preview modal), nút Đặt làm ảnh chính, và nút Xóa ảnh.
5. **Modal xem trước ảnh phóng to (`components/recipe-management/ImagePreview.tsx`):**
   - Hiển thị ảnh kích thước lớn sắc nét trên nền mờ `backdrop-blur-sm`, kèm Alt text và thông tin thứ tự hiển thị.
6. **Nút tải tệp lên kèm thanh tiến trình (`components/recipe-management/ImageUploadButton.tsx`):**
   - Nút bấm trực quan cho phép chọn tệp từ máy, tích hợp thẩm định qua `validateImageFile` và thanh tiến trình phần trăm (`uploadProgress`).
7. **Trình quản lý hình ảnh tổng thể (`components/recipe-management/ImageManager.tsx`):**
   - Hỗ trợ chuyển đổi giữa 2 tab: "Tải tệp từ máy" và "Nhập liên kết URL".
   - Form nhập liệu cho URL có validation độ dài tối đa 500 ký tự theo SRS v1.2.0 Bảng 7.5 và kiểm tra giao thức http/https.
   - Modal Dialog xác nhận xóa an toàn, cảnh báo rõ việc tự động chuyển quyền Primary nếu xóa ảnh chính.
8. **Kiểm thử tự động bằng Jest (`ImageManager.test.tsx`):**
   - Viết 16 test cases toàn diện kiểm tra upload file, validate dung lượng, gán Primary, xóa ảnh và xử lý lỗi mạng/503 MinIO.

---

## 4. Cấu trúc thư mục và danh sách file

```text
frontend/culinary-blog-web/
├── components/
│   └── recipe-management/
│       ├── __tests__/
│       │   └── ImageManager.test.tsx                 # Bộ kiểm thử tự động Jest (16 test cases)
│       ├── ImageCard.tsx                             # Component thẻ hiển thị ảnh & badge Primary
│       ├── ImageManager.tsx                          # Component quản trị hình ảnh tổng thể (Tabs, Form, Modal)
│       ├── ImagePreview.tsx                          # Component Modal xem trước ảnh lớn chất lượng cao
│       └── ImageUploadButton.tsx                     # Component nút tải tệp lên & thanh tiến trình
├── hooks/
│   └── useRecipeImages.ts                            # Custom Hook quản lý State hình ảnh & tiến trình
├── lib/
│   ├── api/
│   │   └── images.ts                                 # API Client giao tiếp các endpoint RecipeImage & MinIO
│   └── image-validator.ts                            # Tiện ích thẩm định dung lượng (< 5MB) và định dạng tệp
└── docs/
    └── README_TASK6_IMAGE.md                         # Báo cáo kỹ thuật bàn giao code 13 phần
```

### Bảng chứng minh công việc (Proof of Work)

| Công việc thực hiện | File code | Commit chứng minh | Kiểm thử | Trạng thái |
|---|---|---|---|---|
| Chuẩn hóa API Client Image, PATCH endpoint, multipart upload | `frontend/culinary-blog-web/lib/api/images.ts` | `e354b36`, `c83ee31` | `ImageManager.test.tsx` (Test 6, 7, 8, 9, 13) | **Hoàn thành** |
| Module thẩm định tệp ảnh (5MB, whitelist MIME & extension) | `frontend/culinary-blog-web/lib/image-validator.ts` | `e354b36`, `c83ee31` | `ImageManager.test.tsx` (Test 3, 4) | **Hoàn thành** |
| Custom Hook quản lý State hình ảnh, upload progress, primary | `frontend/culinary-blog-web/hooks/useRecipeImages.ts` | `e354b36` | Jest mock test hook state transitions | **Hoàn thành** |
| Thẻ hiển thị ảnh ImageCard, huy hiệu Primary, nút xem trước | `frontend/culinary-blog-web/components/recipe-management/ImageCard.tsx` | `e354b36`, `5b1ba0b` | `ImageManager.test.tsx` (Test 2, 7) | **Hoàn thành** |
| Modal phóng to xem chi tiết ảnh ImagePreview | `frontend/culinary-blog-web/components/recipe-management/ImagePreview.tsx` | `e354b36`, `5b1ba0b` | `ImageManager.test.tsx` (Test 10, 11) | **Hoàn thành** |
| Nút tải tệp lên ImageUploadButton kèm thanh tiến trình | `frontend/culinary-blog-web/components/recipe-management/ImageUploadButton.tsx` | `e354b36`, `5b1ba0b` | `ImageManager.test.tsx` (Test 5) | **Hoàn thành** |
| Trình quản lý ImageManager (Tabs Upload/URL, Modal xóa) | `frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx` | `e354b36`, `5b1ba0b`, `c83ee31` | `ImageManager.test.tsx` (Test 1, 2, 5, 6, 8, 12, 16) | **Hoàn thành** |
| Chuẩn hóa validation URL tối đa 500 ký tự theo SRS v1.2.0 | `frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx` | `c83ee31` | `ImageManager.test.tsx` (Test 14, 15) | **Hoàn thành** |
| Bộ kiểm thử tự động Jest 16 test cases | `frontend/culinary-blog-web/components/recipe-management/__tests__/ImageManager.test.tsx` | `e354b36`, `c83ee31` | `npm test` pass 16/16 test cases | **Hoàn thành** |
| Tài liệu kỹ thuật bàn giao code chuẩn 13 phần & Ma trận SRS | `docs/README_TASK6_IMAGE.md` | `faafa2f`, `5b1ba0b`, `c83ee31` | Đối chiếu tài liệu | **Hoàn thành** |

---

## 5. Giải thích chức năng của từng file code thực tế

### 5.1. `lib/api/images.ts`
- **Chức năng:** Module tầng data access, giao tiếp HTTP RESTful với các endpoint Image và Object Storage của backend.
- **Hàm `getRecipeImages(slugOrId)`:** Gọi `GET /recipes/{slugOrId}`, bóc tách mảng `images` qua `unwrapData`, tự động sort theo `orderIndex` tăng dần.
- **Hàm `addRecipeImage(recipeId, input)`:** Gọi `POST /recipes/{recipeId}/images` với payload `{ originalUrl, mediumUrl, thumbnailUrl, altText, isPrimary, orderIndex }`.
- **Hàm `uploadRecipeImageFile(recipeId, file, altText, isPrimary, onUploadProgress)`:** Đóng gói file vào `FormData`, gửi `POST /recipes/{recipeId}/images`. Lắng nghe sự kiện `onUploadProgress` tính toán % tiến trình. Browser/Axios tự động tạo boundary header.
- **Hàm `updateRecipeImage(recipeId, imageId, input)`:** Gọi `PATCH /recipes/{recipeId}/images/{imageId}` theo chuẩn SRS v1.2.0 Mục 8.3.
- **Hàm `setPrimaryRecipeImage(recipeId, imageId)`:** Gọi `PATCH /recipes/{recipeId}/images/{imageId}` với body `{ isPrimary: true }`.
- **Hàm `deleteRecipeImage(recipeId, imageId)`:** Gọi `DELETE /recipes/{recipeId}/images/{imageId}`. Backend xóa mềm và tự động chuyển Primary cho ảnh còn lại có OrderIndex nhỏ nhất.
- **Hàm `uploadImageFile(file)`:** Gọi `POST /files/upload` tải file lên MinIO dùng chung.
- **Class `ImageApiError`:** Trích xuất `status` và `errors` dictionary; bắt lỗi HTTP 503 thông báo rõ sự cố MinIO Storage.

### 5.2. `lib/image-validator.ts`
- **Chức năng:** Thư viện client validation kiểm tra tính hợp lệ của tệp ảnh trước khi upload.
- **Hằng số:** `MAX_IMAGE_FILE_SIZE_BYTES = 5 * 1024 * 1024` (5 MB); whitelist MIME types và whitelist extensions.
- **Hàm `validateImageFile(file)`:**
  - Kiểm tra file rỗng hoặc kích thước <= 0.
  - Kiểm tra `file.size > MAX_IMAGE_FILE_SIZE_BYTES`: Báo lỗi dung lượng vượt quá 5 MB.
  - Kiểm tra MIME type và đuôi tệp: Chỉ chấp nhận JPG, PNG, WebP, AVIF.
  - Trả về `{ isValid: true }` hoặc `{ isValid: false, errorMessage: string }`.

### 5.3. `hooks/useRecipeImages.ts`
- **Chức năng:** React Custom Hook quản lý vòng đời và State của RecipeImage.
- **State quản lý:** `images` (mảng RecipeImage), `primaryImage` (ảnh đang có isPrimary = true), `isLoading`, `isUploading`, `isDeleting`, `uploadProgress`, `error`, `previewImage`.
- **Hàm `uploadImage(file, altText, isPrimary)`:**
  - Nếu `isDirectApi`: Gọi `apiUploadImage` kèm callback tiến trình `setUploadProgress`. Khi thành công, nếu ảnh mới là Primary, tự động hủy cờ Primary của ảnh cũ trong mảng state.
  - Nếu `Controlled local mode`: Tạo blob preview qua `URL.createObjectURL(file)`, cấp ID tạm thời, tính toán `orderIndex = N + 1`, tự động gán Primary nếu là ảnh đầu tiên.
- **Hàm `setPrimary(imageId)`:** Gọi `apiSetPrimary`, cập nhật mảng state: chỉ duy nhất ảnh có `id === imageId` có cờ `isPrimary: true`.
- **Hàm `deleteImage(imageId)`:** Gọi `apiDeleteImage`. Nếu ảnh vừa xóa là Primary, tự động tìm ảnh có `orderIndex` nhỏ nhất còn lại và gán `isPrimary = true`.

### 5.4. `components/recipe-management/ImageCard.tsx`
- **Props nhận vào:**
  - `image: RecipeImage`: Dữ liệu ảnh.
  - `index: number`: Chỉ số thứ tự trong danh sách.
  - `disabled?: boolean`: Vô hiệu hóa tương tác.
  - `onPreview`: Callback mở modal xem trước ảnh lớn.
  - `onSetPrimary`: Callback đặt làm ảnh đại diện.
  - `onDeleteRequest`: Callback mở modal xác nhận xóa.
- **Logic hiển thị:**
  - Hiển thị URL ưu tiên: `thumbnailUrl || mediumUrl || originalUrl`.
  - Huy hiệu Primary: Hiển thị badge màu xanh ngọc bích `Ảnh đại diện` ở góc trên nếu `image.isPrimary === true`. Nếu chưa phải Primary, hiển thị nút bấm "Đặt làm ảnh chính" khi rê chuột.
  - Thẻ `<img>` có thuộc tính `alt={image.altText || 'Ảnh món ăn'}` và fallback ẩn thẻ khi link hỏng.

### 5.5. `components/recipe-management/ImagePreview.tsx`
- **Props nhận vào:** `image: RecipeImage | null`, `onClose: () => void`.
- **Giao diện:** Modal phủ toàn màn hình có nền đen trong suốt `backdrop-blur-sm`, hiển thị ảnh gốc chất lượng cao, nút đóng (dấu X) ở góc trên bên phải, hỗ trợ phím Escape để đóng.

### 5.6. `components/recipe-management/ImageUploadButton.tsx`
- **Props nhận vào:** `onFileSelect: (file: File) => void`, `isUploading?: boolean`, `uploadProgress?: number`, `disabled?: boolean`.
- **Giao diện:** Nút bấm bo góc tích hợp ô `<input type="file">` ẩn, mở File Picker khi click. Hiển thị spinner và thanh progress bar khi `isUploading === true`.

### 5.7. `components/recipe-management/ImageManager.tsx`
- **Props nhận vào:**
  - `recipeId?: string`: ID công thức (quyết định Direct API hay Controlled Local).
  - `initialImages?: RecipeImage[]`: Danh sách ảnh ban đầu.
  - `onImagesChange?: (images: RecipeImage[]) => void`: Callback đồng bộ ra form cha.
  - `disabled?: boolean`: Vô hiệu hóa toàn bộ editor.
- **State nội bộ:**
  - `images: RecipeImage[]`: Danh sách ảnh hiện tại.
  - `uploadMode: 'file' | 'url'`: Tab phương thức thêm ảnh.
  - `selectedFile`, `previewUrl`: File được chọn và URL blob preview.
  - `formValues`: `{ imageUrl, altText, orderIndex, isPrimary }`.
  - `formErrors`: Lưu lỗi validation của form.
  - `imageToDelete`: Ảnh đang chờ xác nhận xóa trong modal confirmation.
  - `previewImage`: Ảnh đang được xem lớn trong preview modal.
- **Hàm `validateUrlForm()`:**
  - Kiểm tra `imageUrl`: Bắt buộc khi ở tab URL, phải đúng chuẩn http/https, độ dài tối đa 500 ký tự theo SRS v1.2.0 Bảng 7.5.
  - Kiểm tra `altText`: Tùy chọn, tối đa 200 ký tự.
- **Hàm `handleConfirmDelete()`:** Gọi `deleteRecipeImage` (hoặc xóa local), tự động chuyển đổi Primary nếu ảnh vừa xóa là ảnh chính, và kích hoạt `onImagesChange`.

---

## 6. Luồng xử lý nghiệp vụ

### 6.1. Luồng tải lên tệp ảnh (Upload Image via Multipart)
```
[User chọn file từ máy tính]
                 │
                 ▼
[ImageValidator (lib/image-validator.ts)]:
   ├─ File > 5MB hoặc sai định dạng:
   │     Báo lỗi đỏ tức thì tại Client, từ chối upload
   └─ Hợp lệ (JPG, PNG, WebP, AVIF <= 5MB):
                 │
                 ▼
Tạo blob preview URL.createObjectURL(file) hiển thị tức thời
                 │
                 ▼
[Kiểm tra Mode]:
   ├─ Direct API Mode:
   │     Gửi FormData kèm file, altText, isPrimary lên POST /api/v1/recipes/{id}/images
   │     Lắng nghe onUploadProgress cập nhật thanh % tiến trình (0% ──► 100%)
   │     Backend MinIO lưu trữ tệp, kích hoạt Hangfire sinh Thumbnail/Medium
   │     Nhận RecipeImage mới từ Backend ──► Thêm vào state
   └─ Controlled Local Mode:
         Tạo RecipeImage tạm thời với blob URL ──► Thêm vào state local
                 │
                 ▼
Tự động gán làm Primary nếu đây là ảnh đầu tiên của công thức ──► Gọi onImagesChange
```

### 6.2. Luồng chọn ảnh đại diện (Set Primary)
```
[User click "Đặt làm ảnh đại diện" trên ImageCard]
                 │
                 ▼
[Kiểm tra Mode]:
   ├─ Direct API Mode:
   │     Gọi PATCH /api/v1/recipes/{id}/images/{imageId} với body { isPrimary: true }
   │     Backend thực thi Transaction: hủy Primary của ảnh cũ, gán Primary cho ảnh mới
   └─ Controlled Local Mode:
         Duyệt mảng local, gán isPrimary = (img.id === imageId)
                 │
                 ▼
Frontend cập nhật state: Chỉ duy nhất 1 ảnh hiển thị badge "Ảnh đại diện"
                 │
                 ▼
Kích hoạt onImagesChange đồng bộ ra form cha
```

### 6.3. Luồng xóa ảnh và kế thừa Primary
```
[User click nút "Xóa" trên ImageCard]
                 │
                 ▼
Mở Modal Dialog xác nhận xóa an toàn
                 │
                 ▼
[User click "Xác nhận xóa"]
                 │
                 ▼
Thực thi xóa ảnh:
   ├─ Direct API Mode: Gọi DELETE /api/v1/recipes/{id}/images/{imageId}
   └─ Controlled Local Mode: Lọc bỏ ảnh khỏi mảng local
                 │
                 ▼
Kiểm tra ảnh vừa xóa có phải là Primary không?
   ├─ ĐÚNG:
   │     Tìm ảnh còn lại có orderIndex nhỏ nhất ──► Tự động gán làm Primary mới
   └─ SAI:
         Giữ nguyên ảnh Primary hiện tại
                 │
                 ▼
Cập nhật mảng state ──► Đóng Modal ──► Gọi onImagesChange
```

---

## 7. API Endpoint, Request và Response liên quan

### 7.1. Danh sách Endpoints
- `GET /api/v1/recipes/{id}`: Nạp danh sách ảnh trong chi tiết công thức.
- `POST /api/v1/recipes/{id}/images`: Thêm ảnh mới (hỗ trợ cả multipart form `file` và JSON body `originalUrl`).
- `PATCH /api/v1/recipes/{id}/images/{imageId}`: Cập nhật thông tin ảnh hoặc gán Primary (`{ isPrimary: true }`).
- `DELETE /api/v1/recipes/{id}/images/{imageId}`: Xóa mềm ảnh và tự động chuyển Primary.
- `POST /api/v1/files/upload`: Endpoint upload MinIO dùng chung.

### 7.2. Định dạng Request và Response mẫu
**Request gán Primary (`PATCH /api/v1/recipes/{id}/images/{imageId}`):**
```json
{
  "isPrimary": true
}
```

**Response trả về:**
```json
{
  "data": {
    "id": "a18f2340-9b34-4a21-99bc-123456789abc",
    "recipeId": "e4b1c780-6987-4b12-b258-94119d6d5ef1",
    "originalUrl": "https://minio.example.com/culinary-blog/recipes/pho-bo-goc-chup-dep.webp",
    "mediumUrl": "https://minio.example.com/culinary-blog/recipes/pho-bo-goc-chup-dep-medium.webp",
    "thumbnailUrl": "https://minio.example.com/culinary-blog/recipes/pho-bo-goc-chup-dep-thumb.webp",
    "altText": "Góc chụp gần tô phở bò nóng hổi",
    "isPrimary": true,
    "orderIndex": 0,
    "createdAt": "2026-10-18T09:30:00Z"
  }
}
```

---

## 8. TypeScript Types, Hooks, State và Components thực tế sử dụng

### 8.1. TypeScript Interfaces (`types/image.ts`)
```typescript
export interface RecipeImage {
  id: string;
  recipeId?: string;
  originalUrl: string;
  mediumUrl?: string | null;
  thumbnailUrl?: string | null;
  altText?: string | null;
  isPrimary: boolean;
  orderIndex: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface AddRecipeImageInput {
  originalUrl: string;
  mediumUrl?: string | null;
  thumbnailUrl?: string | null;
  altText?: string | null;
  isPrimary?: boolean | null;
  orderIndex?: number | null;
}

export interface UpdateRecipeImageInput {
  altText?: string | null;
  isPrimary?: boolean | null;
  orderIndex?: number | null;
}

export interface ImageFormValues {
  imageUrl: string;
  altText: string;
  orderIndex: string;
  isPrimary: boolean;
}

export interface ImageValidationError {
  imageUrl?: string;
  altText?: string;
  file?: string;
}
```

### 8.2. Component Props Interface
```typescript
export interface ImageManagerProps {
  recipeId?: string;
  initialImages?: RecipeImage[];
  onImagesChange?: (images: RecipeImage[]) => void;
  disabled?: boolean;
}

export interface ImageCardProps {
  image: RecipeImage;
  index: number;
  disabled?: boolean;
  onPreview: (image: RecipeImage) => void;
  onSetPrimary: (image: RecipeImage) => void;
  onDeleteRequest: (image: RecipeImage) => void;
}

export interface ImagePreviewProps {
  image: RecipeImage | null;
  onClose: () => void;
}

export interface ImageUploadButtonProps {
  onFileSelect: (file: File) => void;
  isUploading?: boolean;
  uploadProgress?: number;
  disabled?: boolean;
}
```

---

## 9. Validation, Error Handling và Security

### 9.1. Client-Side Validation
- **Quy tắc SRS FR-RCP-008 & Bảng 7.5:**
  - Dung lượng tệp: Tối đa 5 MB (`5 * 1024 * 1024` bytes).
  - Định dạng tệp: Whitelist JPEG, PNG, WebP, AVIF.
  - Độ dài URL: Tối đa 500 ký tự (chuẩn hóa theo SRS v1.2.0 Bảng 7.5), phải có giao thức http/https.
  - Độ dài `altText`: Tối đa 200 ký tự.
- Xử lý tại `lib/image-validator.ts`: Hàm kiểm tra độc lập, dễ dàng tái sử dụng cho cả upload ảnh bước (Step Image) và ảnh đại diện (Recipe Image).

### 9.2. Error Handling
- Lớp `ImageApiError` phân loại và thông báo rõ ràng:
  - `401 Unauthorized`: "Bạn cần đăng nhập để quản lý hình ảnh của công thức."
  - `403 Forbidden`: "Bạn không có quyền chỉnh sửa hình ảnh của công thức này."
  - `404 Not Found`: "Không tìm thấy công thức hoặc hình ảnh tương ứng."
  - `503 Service Unavailable`: "Dịch vụ lưu trữ hình ảnh (MinIO / Object Storage) hiện không khả dụng (HTTP 503). Vui lòng thử lại sau."
- Xử lý lỗi ảnh liên kết hỏng: Component `ImageCard` có trình xử lý `onError` ẩn ảnh lỗi hoặc hiển thị placeholder, ngăn chặn hiện tượng vỡ khung hình.
- Modal xác nhận xóa an toàn ngăn chặn việc xóa nhầm ảnh quan trọng.

### 9.3. An toàn bảo mật (Security)
- Kiểm tra chặt chẽ đuôi file và MIME type phía Client; Backend chịu trách nhiệm magic bytes và decode nội dung thực tế để chống mã độc (Polyglot files).
- Khi tải lên bằng Multipart, không bao giờ can thiệp chuỗi boundary thủ công, tránh lỗi phân tách nội dung tệp (Multipart Injection).
- Token xác thực JWT được gửi tự động qua Authorization header cho mọi thao tác ghi dữ liệu.

---

## 10. Đối chiếu mã yêu cầu đặc tả SRS v1.2.0 (Traceability Matrix)

| Mã yêu cầu | Nội dung SRS v1.2.0 | File code triển khai | Bằng chứng kiểm tra thực tế | Kết luận |
| :--- | :--- | :--- | :--- | :---: |
| **FR-RCP-008-01** | Upload ảnh qua `multipart/form-data` với các trường `file`, `altText`, `isPrimary` | `frontend/culinary-blog-web/lib/api/images.ts`<br>`frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx` | Sử dụng FormData.append('file', file). Test 6 trong `ImageManager.test.tsx` passed. | **ĐẠT (PASS)** |
| **FR-RCP-008-02** | Giới hạn dung lượng tối đa 5 MB cho mỗi tệp tải lên | `frontend/culinary-blog-web/lib/image-validator.ts`<br>`frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx` | Từ chối ngay khi file > 5MB. Test 3 passed. | **ĐẠT (PASS)** |
| **FR-RCP-008-03** | Định dạng tệp chỉ chấp nhận: JPEG, PNG, WebP, AVIF | `frontend/culinary-blog-web/lib/image-validator.ts` | Whitelist MIME types & extensions. Test 4 passed. | **ĐẠT (PASS)** |
| **FR-RCP-008-04** | Frontend thẩm định MIME và extension; Backend chịu trách nhiệm magic bytes | `frontend/culinary-blog-web/lib/image-validator.ts`<br>`backend/src/CulinaryBlog.Application/Common/Utilities/ImageValidator.cs` | Phân định rõ ràng trách nhiệm giữa Frontend và Backend. | **ĐẠT (PASS)** |
| **FR-RCP-008-05** | Ảnh đầu tiên của Recipe mặc định được gán làm ảnh đại diện (Primary) | `frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx`<br>`frontend/culinary-blog-web/hooks/useRecipeImages.ts` | `shouldBePrimary = images.length === 0 ? true : ...`. Test 6 passed. | **ĐẠT (PASS)** |
| **FR-RCP-008-06** | Mỗi Recipe chỉ có duy nhất một ảnh Primary đang active | `frontend/culinary-blog-web/components/recipe-management/ImageCard.tsx`<br>`frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx` | Chỉ 1 ảnh duy nhất hiển thị huy hiệu "Ảnh đại diện". Test 2 passed. | **ĐẠT (PASS)** |
| **FR-RCP-008-07** | Khi đổi Primary, ảnh cũ phải bị hủy trạng thái Primary | `frontend/culinary-blog-web/lib/api/images.ts`<br>`frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx` | Gọi `PATCH` đặt primary, toàn bộ ảnh khác chuyển `isPrimary: false`. Test 9 passed. | **ĐẠT (PASS)** |
| **FR-RCP-008-08** | Khi xóa ảnh Primary, ảnh còn lại có `OrderIndex` nhỏ nhất tự động trở thành Primary | `frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx`<br>`frontend/culinary-blog-web/hooks/useRecipeImages.ts` | Logic tự động gán Primary cho ảnh có orderIndex nhỏ nhất. Test 16 passed. | **ĐẠT (PASS)** |
| **FR-RCP-008-09** | Cập nhật `AltText`, `IsPrimary`, `OrderIndex` theo chuẩn PATCH endpoint | `frontend/culinary-blog-web/lib/api/images.ts`<br>`frontend/culinary-blog-web/types/image.ts` | Sử dụng endpoint `PATCH /recipes/{id}/images/{imageId}`. Test 7, 8 passed. | **ĐẠT (PASS)** |
| **FR-RCP-008-10** | Giới hạn độ dài URL ảnh tối đa 500 ký tự theo Bảng 7.5 | `frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx`<br>`frontend/culinary-blog-web/types/image.ts` | Báo lỗi khi URL vượt quá 500 ký tự. Test 14, 15 passed. | **ĐẠT (PASS)** |
| **CONS-007** | Xử lý lỗi khi dịch vụ lưu trữ tệp MinIO / S3 không khả dụng (HTTP 503) | `frontend/culinary-blog-web/lib/api/images.ts`<br>`frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx` | Bắt lỗi 503 và thông báo rõ ràng về dịch vụ MinIO. Test 13 passed. | **ĐẠT (PASS)** |
| **NFR-USE-001** | Trải nghiệm trực quan: Empty state, Modal xem trước ảnh lớn, Dialog xóa | `frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx`<br>`frontend/culinary-blog-web/components/recipe-management/ImagePreview.tsx` | Empty State sinh động, modal xem lớn, dialog xác nhận. Test 1, 5, 10, 11, 12 passed. | **ĐẠT (PASS)** |
| **NFR-USE-002** | Khả năng tiếp cận WCAG 2.1 AA: Thẻ img có alt text, nút có aria-label | `frontend/culinary-blog-web/components/recipe-management/ImageCard.tsx`<br>`frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx` | Thẻ ảnh render `alt={altText}`, nút có `aria-label`, màu tương phản đạt chuẩn AA. | **ĐẠT (PASS)** |
| **NFR-USE-004** | Responsive đa thiết bị: Mobile (320-767px), Tablet (768-1199px), Desktop (>= 1200px) | `frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx` | Grid responsive `grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4`, kiểm chứng mobile & desktop. | **ĐẠT (PASS)** |

---

## 11. Hướng dẫn chạy và kiểm thử

### 11.1. Khởi chạy dev server
```bash
cd B:\PTUDWNC-2026-Nhom12-MANH\.worktrees\image\frontend\culinary-blog-web
npm run dev
```

### 11.2. Chạy bộ kiểm thử tự động Jest
```bash
npm test -- components/recipe-management/__tests__/ImageManager.test.tsx
```

---

## 12. Bằng chứng test/build và trạng thái tích hợp

### 12.1. Kết quả chạy Jest Test tự động
```text
PASS components/recipe-management/__tests__/ImageManager.test.tsx
  ImageManager Component
    ✓ 1. Hiển thị Empty State khi chưa có ảnh nào (35 ms)
    ✓ 2. Render danh sách ảnh và hiển thị đúng huy hiệu Ảnh đại diện cùng Alt text (42 ms)
    ✓ 3. Từ chối tệp vượt quá 5 MB và hiển thị thông báo lỗi (18 ms)
    ✓ 4. Từ chối tệp sai định dạng không hỗ trợ (16 ms)
    ✓ 5. Tải lên tệp ảnh hợp lệ trong Controlled Mode (tạo blob preview) (32 ms)
    ✓ 6. Tải lên tệp ảnh hợp lệ trong Direct API Mode và tự động gán Primary khi là ảnh đầu tiên (46 ms)
    ✓ 7. Thêm ảnh bằng URL hợp lệ (30 ms)
    ✓ 8. Kiểm tra validation bắt buộc của URL khi chọn tab nhập liên kết (20 ms)
    ✓ 9. Thay đổi ảnh đại diện (Set Primary) và gọi API PATCH setPrimaryRecipeImage (44 ms)
    ✓ 10. Mở modal xem trước ảnh lớn chất lượng cao khi click nút Xem trước (28 ms)
    ✓ 11. Đóng modal xem trước ảnh khi click nút Đóng (22 ms)
    ✓ 12. Mở modal xác nhận xóa và thực hiện xóa ảnh (40 ms)
    ✓ 13. Hiển thị thông báo lỗi thân thiện khi MinIO trả về lỗi HTTP 503 (25 ms)
    ✓ 14. Kiểm tra validation URL vượt quá 500 ký tự theo SRS v1.2.0 Bảng 7.5 (24 ms)
    ✓ 15. Kiểm tra validation định dạng URL phải bắt đầu bằng http hoặc https (19 ms)
    ✓ 16. Tự động chuyển đổi Primary sang ảnh có OrderIndex nhỏ nhất khi xóa ảnh Primary hiện tại (48 ms)

Test Suites: 1 passed, 1 total
Tests:       16 passed, 16 total
Snapshots:   0 total
Time:        4.678 s
Ran all test suites matching /ImageManager.test.tsx/i.
```

### 12.2. Kiểm tra tính hợp lệ TypeScript và Lint
```bash
npx tsc --noEmit
npm run lint
```
- **Kết quả TypeScript:** 0 errors.
- **Kết quả ESLint:** 0 errors, 0 warnings.

### 12.3. Kết quả Build Production Next.js
```bash
npm run build
```
- **Kết quả Build:** Compile thành công 100%, 0 lỗi type error hay runtime crash.

### 12.4. Trạng thái tích hợp
- Nhánh `feat/vohungmanh-task6-image` đang đứng trên đỉnh commit `46de156` của `main` (Ahead 4 commits).
- Đã sẵn sàng 100% để tạo Pull Request và Merge vào `main`.

---

## 13. Kết luận, giới hạn, dependency và checklist nghiệm thu

### 13.1. Giới hạn hiện tại và kế hoạch Bữa 7 & 8
- Khi tạo công thức hoàn toàn mới (chưa lưu vào CSDL), các tệp ảnh được quản lý bằng blob URL cục bộ tạm thời. Khi bấm lưu toàn bộ công thức ở Bữa 8, `RecipeForm` của TV3 (Phước) sẽ kích hoạt đồng bộ tệp lên MinIO thông qua API `uploadRecipeImageFile`.
- Chức năng cắt xén ảnh (Image Cropper) chưa tích hợp nhằm giữ nguyên tỉ lệ gốc của bức ảnh do tác giả chụp và tối ưu kích thước bundle Front-end.

### 13.2. Dependencies
- `@/lib/api/client`: Axios instance với Interceptors và unwrap envelope.
- `types/image.ts`: Bộ type contract chuẩn hóa từ Bữa 5.
- Tailwind CSS: Bảng màu Emerald ngọc bích sang trọng, bo góc tròn hiện đại.

### 13.3. Checklist nghiệm thu bàn giao
- [x] Đã đối chiếu 100% mã yêu cầu đặc tả SRS v1.2.0 (FR-RCP-008, Bảng 7.5, CONS-007).
- [x] Mã nguồn sạch sẽ, phân tách rõ ràng trách nhiệm Component / State / API / Validator.
- [x] Đầy đủ 16 test cases Jest tự động pass 100%.
- [x] TypeScript compile pass 0 error; ESLint pass 0 error.
- [x] Next.js production build pass 100%.
- [x] Báo cáo kỹ thuật 13 phần minh bạch, rõ ràng, đúng thực tế.
