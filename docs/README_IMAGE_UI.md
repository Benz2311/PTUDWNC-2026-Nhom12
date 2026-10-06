# Giao diện Quản lý hình ảnh công thức (Recipe Image UI)

## 1. Mục tiêu

Module Giao diện Quản lý hình ảnh công thức (`Recipe Image UI`) được xây dựng nhằm giải quyết các bài toán sau trên tầng Frontend của CulinaryBlog:
- **Tải lên và xem trước hình ảnh trực quan**: Cung cấp giao diện thân thiện cho tác giả công thức tải ảnh lên từ máy tính (kéo thả hoặc chọn file) kèm cơ chế xem trước (thumbnail preview) tức thì trước khi gửi lên hệ thống lưu trữ.
- **Hỗ trợ thêm ảnh linh hoạt (File Upload & Direct URL)**: Bên cạnh việc tải file lên Object Storage (MinIO), người dùng có thể thêm ảnh trực tiếp thông qua đường dẫn công khai (Public URL).
- **Quản lý bộ sưu tập ảnh công thức (Gallery Management)**: Cho phép tổ chức nhiều hình ảnh với số thứ tự hiển thị (`OrderIndex`), chú thích trợ năng/SEO (`AltText`), và cờ đánh dấu ảnh đại diện chính (`IsPrimary`).
- **Xử lý đặc thù dịch vụ lưu trữ**: Bắt và xử lý thân thiện các mã lỗi hạ tầng lưu trữ (đặc biệt là `HTTP 503 Service Unavailable` khi dịch vụ Object Storage / MinIO chưa sẵn sàng).
- **Độc lập và bảo vệ mã nguồn chung**: Đóng gói thành các component tái sử dụng (`ImageManager`, `ImageCard`), không can thiệp vào `RecipeForm.tsx` (thuộc trách nhiệm của TV3 Phạm Nguyễn Ngọc Phước) để tránh xung đột mã nguồn.

---

## 2. Kết quả đạt được

Sau khi branch `feat/vohungmanh-image-ui` được triển khai, hệ thống đạt được các năng lực UI thực tế:
- **Component ImageManager hoàn chỉnh**: Quản lý bộ sưu tập ảnh, tab chuyển đổi giữa "Tải file lên" và "Nhập URL", thanh xem trước ảnh tức thì (`URL.createObjectURL`), modal xác nhận trước khi xóa, và banner thông báo lỗi.
- **Component ImageCard trực quan**: Hiển thị card ảnh tỷ lệ chuẩn 16:9 (`aspect-video`), bo góc, viền nhẹ, badge nổi bật "Ảnh đại diện" (Xanh lá đậm), badge thứ tự `OrderIndex`, chú thích Alt text, cùng nút hành động "Đặt làm ảnh chính" và nút "Xóa".
- **Client-side Validation nhanh chóng**: Kiểm tra dung lượng tối đa 5 MB và định dạng file cho phép (JPEG, PNG, WebP, AVIF) ngay tại trình duyệt, phản hồi lập tức mà không cần gửi request lên server.
- **Cơ chế đổi ảnh đại diện (Set Primary)**: Bấm nút "Đặt làm ảnh chính", gọi API backend cập nhật CSDL và tự động chuyển cờ Primary sang ảnh mới.
- **Xóa ảnh an toàn kèm tự động chuyển cờ Primary**: Popup cảnh báo xác nhận trước khi xóa; sau khi xóa ảnh chính, hệ thống tự động gán cờ Primary cho ảnh có `OrderIndex` nhỏ nhất còn lại.
- **Xử lý lỗi HTTP 503 Storage chuyên biệt**: Bắt lỗi khi backend hoặc MinIO down và hiển thị thông điệp hướng dẫn rõ ràng cho người dùng.
- **Bộ kiểm thử tự động 100% PASS**: 14/14 unit tests chuyên biệt cho `ImageManager.test.tsx` (tổng 26/26 tests frontend) đạt kết quả thành công.

---

## 3. Luồng hoạt động

Luồng xử lý khi người dùng tương tác với module quản lý hình ảnh:

```
User (Chọn File hoặc Nhập URL)
   │
   ▼
ImageManager (Container Component)
   │
   ▼
Client-side Validation (Kiểm tra dung lượng <= 5MB, MIME: JPEG/PNG/WebP/AVIF, hoặc URL format)
   │
   ├── [Không hợp lệ] ──> Hiển thị lỗi đỏ dưới input & Chặn thao tác
   │
   └── [Hợp lệ]
         │
         ▼
      Tạo Preview tức thì (URL.createObjectURL(file) hoặc URL trực tiếp)
         │
         ▼
      User nhập AltText, tùy chỉnh OrderIndex và bấm "Tải lên & Lưu hình ảnh"
         │
         ├── [File Upload Mode]
         │      │
         │      ▼
         │   uploadImageFile(file) (gửi multipart/form-data lên /api/v1/files/upload)
         │      │
         │      ▼
         │   Backend / MinIO Object Storage lưu trữ & trả về Public URL
         │
         └── [Direct URL Mode] ──> Lấy URL trực tiếp
                │
                ▼
             addRecipeImage(recipeId, input) (gửi POST /api/v1/recipes/{recipeId}/images)
                │
                ▼
             Backend lưu bản ghi RecipeImage vào PostgreSQL
                │
                ├── [Thành công] ──> Cập nhật Gallery Grid (danh sách RecipeImage mới) & Thu hồi Blob URL
                │
                └── [Thất bại]   ──> Hiển thị Error Banner (401, 403, 404, hoặc 503 Storage Unavailable)
```

### Giải thích chi tiết các bước:
1. **Bước 1 - Người dùng tương tác**: Người dùng kéo thả file ảnh từ máy tính vào khung upload hoặc chuyển sang tab "Nhập liên kết" để paste đường dẫn ảnh.
2. **Bước 2 - Client-side Validation**:
   - Nếu là file: Kiểm tra `file.size <= 5 * 1024 * 1024` (5 MB) và `file.type` thuộc `image/jpeg`, `image/png`, `image/webp`, `image/avif`. Nếu không đạt, báo lỗi đỏ lập tức.
   - Nếu là URL: Kiểm tra chuỗi bắt đầu bằng `http://` hoặc `https://`, độ dài $\le 2048$ ký tự.
3. **Bước 3 - Preview tức thì**: Sử dụng `URL.createObjectURL(file)` để tạo blob URL cục bộ, hiển thị thumbnail ảnh xem trước ngay trên form mà chưa cần upload lên server.
4. **Bước 4 - Gửi dữ liệu**: Người dùng nhập Alt text (tối đa 200 ký tự), OrderIndex và bấm nút lưu:
   - Với file: Gọi `uploadImageFile(file)` gửi `multipart/form-data` lên `/api/v1/files/upload` để MinIO lưu trữ và nhận về URL công khai.
   - Tiếp theo, gọi `addRecipeImage` gửi `POST /api/v1/recipes/{recipeId}/images` chứa thông tin ảnh vào CSDL.
5. **Bước 5 - Cập nhật Gallery**: Khi backend trả về kết quả thành công, component cập nhật danh sách ảnh trong state, hiển thị thêm thẻ `ImageCard` trong lưới gallery, reset form nhập liệu và gọi `URL.revokeObjectURL()` để giải phóng bộ nhớ browser.

---

## 4. Các file chính

| File | Vai trò | Xử lý gì |
| :--- | :--- | :--- |
| `frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx` | Container Component chính | Quản lý state danh sách ảnh, tab Upload/URL, preview ảnh, client validation (5MB, format), modal xác nhận xóa, xử lý loading và error states (401/403/404/503) |
| `frontend/culinary-blog-web/components/recipe-management/ImageCard.tsx` | Sub-component Card | Hiển thị thẻ card từng ảnh: thumbnail tỷ lệ 16:9, badge "Ảnh đại diện" (nền xanh lá đậm), badge thứ tự `OrderIndex`, thông tin Alt text, nút "Đặt làm ảnh chính", nút "Xóa" |
| `frontend/culinary-blog-web/lib/api/images.ts` | API Client Module | Chứa các hàm giao tiếp HTTP: `getRecipeImages`, `addRecipeImage`, `setPrimaryRecipeImage`, `deleteRecipeImage`, `uploadImageFile` (gửi multipart/form-data) |
| `frontend/culinary-blog-web/lib/api.ts` | Core HTTP Client | Hàm tiện ích `apiFetch`, `apiJson`, tự động gắn Authorization Header và parse lỗi RFC 7807 |
| `frontend/culinary-blog-web/types/image.ts` | TypeScript Interfaces | Định nghĩa kiểu `RecipeImage`, `AddRecipeImageInput`, `ImageFormValues`, `ImageValidationError` |
| `frontend/culinary-blog-web/components/recipe-management/__tests__/ImageManager.test.tsx` | Unit Test Suite | 14 test cases kiểm tra render empty/gallery, validation dung lượng/định dạng, preview, Add, Set Primary, Delete (có confirm), Error banner, và HTTP 503 handling |
| `frontend/culinary-blog-web/jest.config.mjs` | Test Configuration | Cấu hình Jest ES Module cho Next.js 15 |
| `docs/README_IMAGE_UI.md` | Tài liệu kỹ thuật | Hướng dẫn kiến trúc, luồng hoạt động, validation và kết quả kiểm thử của branch Image UI |
| `docs/VO_HUNG_MANH_IMAGE_UI_COMPLAN.md` | Tài liệu giải trình | Báo cáo chi tiết, câu hỏi bảo vệ và ma trận đánh giá rủi ro |

---

## 5. API / Interface

### Component Props & Interface

| Component | Input / Props | API sử dụng | Output UI |
| :--- | :--- | :--- | :--- |
| **ImageManager** | `recipeId: string`<br>`initialImages?: RecipeImage[]`<br>`disabled?: boolean` | `uploadImageFile`<br>`addRecipeImage`<br>`setPrimaryRecipeImage`<br>`deleteRecipeImage` | Grid lưới các `ImageCard`, tab Upload file/Nhập URL, form nhập Alt text & OrderIndex, preview ảnh, modal xác nhận xóa, banner lỗi |
| **ImageCard** | `image: RecipeImage`<br>`onSetPrimary: (imageId: string) => void`<br>`onDelete: (imageId: string) => void`<br>`disabled?: boolean` | Không gọi trực tiếp (gửi callback lên cha) | Card ảnh 16:9, badge "Ảnh đại diện", nhãn `Thứ tự: #`, Alt text, nút "Đặt làm ảnh chính", nút "Xóa" |

### REST Endpoints mà `lib/api/images.ts` giao tiếp:

| Method | Endpoint | Input Body | Output | Authorization |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/files/upload` | `multipart/form-data` (file) | `{ url: string, ... }` | Bearer Token (Author/Admin) |
| `POST` | `/api/v1/recipes/{recipeId}/images` | `{ originalUrl, mediumUrl?, thumbnailUrl?, altText?, isPrimary?, orderIndex? }` | `RecipeImage` (bản ghi đã lưu CSDL) | Bearer Token (Author/Admin) |
| `PATCH` | `/api/v1/recipes/{recipeId}/images/{imageId}/primary` | Không | `200 OK` (cập nhật cờ Primary) | Bearer Token (Author/Admin) |
| `DELETE` | `/api/v1/recipes/{recipeId}/images/{imageId}` | Không | `200 OK` / `204 No Content` | Bearer Token (Author/Admin) |

---

## 6. Business Rules

1. **Giới hạn dung lượng và định dạng hình ảnh Client-side**:
   - Dung lượng tối đa của một file ảnh là **5 MB** (`file.size <= 5 * 1024 * 1024`). File vượt quá giới hạn bị từ chối ngay lập tức.
   - Định dạng ảnh cho phép: **JPEG (`image/jpeg`)**, **PNG (`image/png`)**, **WebP (`image/webp`)**, và **AVIF (`image/avif`)**. Các định dạng khác (PDF, GIF, SVG, thực thi) bị chặn ở client.
2. **Quy tắc ảnh đại diện chính (IsPrimary)**:
   - Trong một công thức, tại một thời điểm chỉ có duy nhất **1 ảnh mang cờ `IsPrimary = true`**.
   - Khi thêm ảnh đầu tiên vào công thức, hệ thống tự động gán cờ `IsPrimary = true` cho ảnh đó.
   - Khi người dùng bấm "Đặt làm ảnh chính" cho một ảnh khác: Backend thực hiện transaction hạ cờ các ảnh khác và set cờ cho ảnh được chọn; Frontend cập nhật badge đại diện ngay lập tức.
3. **Quy tắc tự động kế thừa cờ Primary khi xóa**:
   - Khi xóa một ảnh đang mang cờ `IsPrimary = true`, backend tự động tìm ảnh có `OrderIndex` nhỏ nhất còn lại để nâng thành ảnh chính mới.
   - Frontend cập nhật lại danh sách và tự động hiển thị badge "Ảnh đại diện" trên ảnh kế tiếp đó.
4. **Bảo vệ xác nhận trước khi xóa (Delete Confirmation)**:
   - Thao tác xóa bắt buộc phải mở popup xác nhận (`"Bạn có chắc chắn muốn xóa hình ảnh này?"`).
   - Nếu ảnh cần xóa đang là ảnh chính, hộp thoại bổ sung cảnh báo: `"Hình ảnh này đang là ảnh đại diện chính của công thức. Sau khi xóa, ảnh có thứ tự nhỏ nhất còn lại sẽ trở thành ảnh đại diện mới."`
5. **Xử lý chuyên biệt lỗi HTTP 503 MinIO/Storage**:
   - Khi dịch vụ lưu trữ Object Storage không sẵn sàng hoặc chưa bật container, hệ thống bắt mã lỗi HTTP 503 và hiển thị banner thông báo thân thiện: `"Dịch vụ lưu trữ hình ảnh (MinIO / Object Storage) hiện không khả dụng (HTTP 503). Vui lòng thử lại sau."` thay vì báo lỗi hệ thống chung chung.

---

## 7. Ví dụ hoạt động

### Ví dụ: Tải ảnh từ thiết bị lên làm ảnh đại diện chính

```
INPUT:
- RecipeId: "8a129df0-e3a1-45ef-bc21-72a084efb901"
- Người dùng chọn file: "pho-bo-dac-biet.jpg" (Dung lượng: 2.4 MB, Type: image/jpeg)
- Client kiểm tra: 2.4 MB <= 5 MB && JPEG -> HỢP LỆ.
- Giao diện tạo blob preview tức thì: blob:http://localhost:3000/a82b-42ef...
- Người dùng nhập Alt text: "Bát phở bò tái nạm thơm ngon"
- Bấm "Tải lên & Lưu hình ảnh"

↓ PROCESS:
1. ImageManager kích hoạt loading state (hiển thị spinner, disable form).
2. Gọi uploadImageFile(file) gửi FormData chứa file lên /api/v1/files/upload:
   - Dịch vụ Storage lưu file vào bucket MinIO "culinaryblog".
   - Trả về URL: "http://localhost:9000/culinaryblog/recipes/pho-bo-dac-biet.jpg".
3. Gọi addRecipeImage("8a129df0-...", {
     originalUrl: "http://localhost:9000/culinaryblog/recipes/pho-bo-dac-biet.jpg",
     altText: "Bát phở bò tái nạm thơm ngon",
     orderIndex: 0
   }):
   - Backend thêm bản ghi RecipeImage vào database. Vì là ảnh đầu tiên, backend tự động gán isPrimary = true.
4. Backend trả về đối tượng RecipeImage thành công.
5. ImageManager cập nhật state images, thu hồi blob URL tạm thời, reset form upload.

↓ OUTPUT:
- Thẻ ImageCard xuất hiện trên Gallery:
  * Hình ảnh hiển thị tỷ lệ 16:9 sắc nét.
  * Badge "Ảnh đại diện" màu xanh lá đậm (#166534) ở góc trên.
  * Nhãn thứ tự: "#0".
  * Dòng chú thích: "Alt: Bát phở bò tái nạm thơm ngon".
  * Nút "Xóa" sẵn sàng phục vụ thao tác.
```

---

## 8. Error Handling

| Tình huống lỗi | Hành vi UI | Thông điệp hiển thị |
| :--- | :--- | :--- |
| Chọn file dung lượng $> 5$ MB | Chặn tải lên ngay tại client | Chữ đỏ: `"Dung lượng file vượt quá giới hạn cho phép (tối đa 5 MB)"` |
| Chọn file sai định dạng (vd: file `.gif`, `.pdf`, `.zip`) | Chặn tải lên ngay tại client | Chữ đỏ: `"Định dạng file không được hỗ trợ. Chỉ chấp nhận JPG, PNG, WebP hoặc AVIF"` |
| Nhập URL không đúng chuẩn (thiếu `http://` hoặc `https://`) | Chặn submit form | Chữ đỏ: `"URL hình ảnh không hợp lệ"` |
| Nhập AltText vượt quá 200 ký tự | Chặn submit form | Chữ đỏ: `"Chú thích ảnh không được vượt quá 200 ký tự"` |
| Dịch vụ MinIO chưa bật hoặc mất kết nối (HTTP 503) | Bắt lỗi từ API response | Banner đỏ: `"Dịch vụ lưu trữ hình ảnh (MinIO / Object Storage) hiện không khả dụng (HTTP 503). Vui lòng thử lại sau."` |
| Phiên đăng nhập hết hạn (HTTP 401) | Bắt lỗi từ API response | Banner đỏ: `"Bạn cần đăng nhập để quản lý hình ảnh của công thức."` |
| Không có quyền tác giả (HTTP 403) | Bắt lỗi từ API response | Banner đỏ: `"Bạn không có quyền chỉnh sửa hình ảnh của công thức này."` |
| Người dùng bấm nhầm nút Xóa | Không xóa trực tiếp | Hiển thị Dialog xác nhận xóa kèm cảnh báo nếu là ảnh đại diện |

---

## 9. Cách chạy và Demo

### Điều kiện tiên quyết
- Node.js 18+ hoặc 20+
- Dependencies frontend đã cài đặt đầy đủ

### Lệnh chạy môi trường phát triển
```bash
cd frontend/culinary-blog-web
npm run dev
```
Mở trình duyệt truy cập: `http://localhost:3000`.

### Kịch bản Demo cho Giảng viên (8 bước):
1. **Demo Empty State**: Mở component `ImageManager` khi chưa có ảnh nào -> Quan sát giao diện thông báo "Chưa có hình ảnh nào cho công thức" và vùng kéo thả upload.
2. **Demo Chọn ảnh & Xem trước (Preview)**: Chọn một file ảnh `.jpg` dung lượng 2 MB -> Giao diện lập tức hiển thị thumbnail ảnh xem trước, tên file và kích thước mà chưa cần upload.
3. **Demo Nhập Alt text & Tải lên**: Nhập chú thích "Bát phở bò truyền thống", bấm `Tải lên & Lưu hình ảnh` -> Thẻ ảnh xuất hiện trong gallery với badge "Ảnh đại diện".
4. **Demo Thêm ảnh qua URL**: Chuyển sang tab "Nhập liên kết (URL)", dán link ảnh thứ hai, nhập OrderIndex = 1 -> Thêm thành công, hiển thị 2 ảnh cạnh nhau trên grid.
5. **Demo Đổi ảnh đại diện (Set Primary)**: Bấm nút `Đặt làm ảnh chính` ở ảnh thứ 2 -> Badge "Ảnh đại diện" chuyển sang ảnh thứ 2, ảnh đầu tiên tự động mất cờ đại diện.
6. **Demo Xóa ảnh & Modal Xác nhận**: Bấm nút `Xóa` trên ảnh thứ 2 (đang là ảnh chính) -> Dialog cảnh báo hiển thị nhắc nhở ảnh này đang là ảnh chính. Bấm xác nhận -> Ảnh bị xóa và ảnh đầu tiên tự động được thăng cấp lại thành ảnh đại diện.
7. **Demo Validation quá dung lượng**: Chọn một file ảnh dung lượng $> 5$ MB -> Hệ thống lập tức từ chối và hiển thị thông báo lỗi màu đỏ.
8. **Demo Validation sai định dạng**: Chọn một file `.pdf` hoặc `.txt` -> Hệ thống hiển thị thông báo lỗi định dạng không được hỗ trợ.

---

## 10. Testing

### Bộ kiểm thử tự động Frontend
Chạy kiểm thử bằng Jest:
```powershell
cd frontend/culinary-blog-web
npm test
```

### Kết quả kiểm thử thực tế
- **Test Suites**: `2 passed, 2 total`
- **Tests**: `26 passed, 26 total`
- **Snapshots**: `0 total`
- **Time**: ~9.15 s

### 14 kịch bản kiểm thử trong `ImageManager.test.tsx` (14/14 PASS):
1. `renders empty state when no images provided`: PASS
2. `renders image list with primary badge and alt text`: PASS
3. `validates max file size 5MB`: PASS
4. `validates allowed file extensions (JPEG, PNG, WebP, AVIF)`: PASS
5. `shows preview when valid image file is selected`: PASS
6. `adds image via file upload successfully`: PASS
7. `validates alt text max length 200 chars`: PASS
8. `allows specifying custom orderIndex`: PASS
9. `sets image as primary and updates UI`: PASS
10. `opens delete confirmation modal when clicking delete`: PASS
11. `deletes image after confirmation`: PASS
12. `displays error banner when API returns error (401/403/404)`: PASS
13. `displays friendly error message on 503 Storage Unavailable`: PASS
14. `disables all interactions when disabled prop is true`: PASS

---

## 11. Build

### Lệnh đóng gói build
```powershell
cd frontend/culinary-blog-web
$env:NODE_OPTIONS="--max-old-space-size=4096"; npm run build
```

### Kết quả build thực tế
- **Framework**: Next.js 15.5.25 (Turbopack)
- **Status**: Compiled successfully (exit code 0)
- **Static Pages Generated**: `5/5` routes
- **Type Checking**: Zero TypeScript error
- **Bundle Output**:
  - Route `/`: 5.47 kB (First Load JS: 120 kB)
  - Route `/categories`: 0 B (First Load JS: 118 kB)
  - Shared JS chunks: 123 kB

---

## 12. Limitations

1. **Phân biệt Unit Tests và End-to-End MinIO Storage thật**:
   - Bộ kiểm thử `ImageManager.test.tsx` là **Unit Tests** chạy trên môi trường Node.js/jsdom giả lập các phản hồi HTTP (bao gồm cả mock response 503).
   - Để thực hiện tải file nhị phân thật và lưu trữ vật lý trên MinIO bucket, hệ thống đòi hỏi Backend .NET API và container Docker MinIO phải đang hoạt động đồng thời.
2. **Ranh giới tích hợp với `RecipeForm.tsx`**:
   - File `RecipeForm.tsx` thuộc trách nhiệm của **Thành viên 3 (Phạm Nguyễn Ngọc Phước)**.
   - Để tránh xung đột mã nguồn, `ImageManager` hiện tại là component độc lập, chưa nhúng trực tiếp vào `RecipeForm.tsx` trong branch này.
   - Giao diện đã sẵn sàng nhận `recipeId` để nhúng vào form chính khi tiến hành tích hợp liên thông nhóm.

---

## 13. Kết luận

Branch `feat/vohungmanh-image-ui` đã hoàn thiện toàn diện module giao diện quản lý hình ảnh công thức (`ImageManager` & `ImageCard`):
- Hỗ trợ đầy đủ trải nghiệm tải ảnh trực tiếp, xem trước (preview), và thêm qua URL.
- Quản lý bộ sưu tập ảnh linh hoạt với đầy đủ tính năng: Đặt làm ảnh chính (`Set Primary`), xóa mềm an toàn (có dialog xác nhận), và tự động chuyển cờ đại diện.
- Xác thực client-side chuẩn xác (giới hạn 5 MB, 4 định dạng MIME cho phép), bắt và hiển thị lỗi thân thiện (kể cả HTTP 503 Storage Unavailable).
- Đạt chuẩn chất lượng với **14/14 unit tests passed** và biên dịch production build thành công 0 lỗi.
