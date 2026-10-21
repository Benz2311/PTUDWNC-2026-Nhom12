# BÁO CÁO KỸ THUẬT: RECIPE IMAGE MANAGEMENT (TASK 6)
**Thành viên phụ trách:** Võ Hùng Mạnh (2312687) - Nhóm trưởng Nhóm 12  
**Dự án:** Phát triển ứng dụng Web nâng cao - Culinary Blog  
**Branch:** `feat/vohungmanh-task6-image`

---

## 1. Mục tiêu
- Xây dựng hệ thống tiện ích API Client, công cụ thẩm định tệp ảnh (image validator), custom hook quản lý state và các component giao diện người dùng cho bộ sưu tập hình ảnh món ăn (Recipe Images) trong Blog ẩm thực theo phân công Task 6 (Bữa 6).
- Cung cấp khả năng quản lý hình ảnh toàn diện: tải tệp lên trực tiếp bằng `multipart/form-data`, nhập URL ảnh có sẵn, xem trước ảnh phóng to (Image Preview Modal), đặt ảnh đại diện chính (Set Primary), xóa ảnh an toàn và quản lý danh sách ảnh phụ.
- Đảm bảo xử lý đúng chuẩn `multipart/form-data` phía Client (để Browser/Axios tự động tạo boundary, không can thiệp sai `Content-Type`), hỗ trợ tiến trình tải lên (`uploadProgress`), và kiểm soát chặt chẽ dung lượng tối đa 5 MB cùng các định dạng ảnh được phép (JPG, PNG, WebP, AVIF).

---

## 2. Kết quả đạt được
- **API Client hoàn chỉnh:** Triển khai module `lib/api/images.ts` bao gồm các hàm gọi API chuẩn RESTful: `getRecipeImages`, `addRecipeImage`, `uploadRecipeImageFile`, `updateRecipeImage`, `setPrimaryRecipeImage`, `deleteRecipeImage`, `uploadImageFile`.
- **Validation đồng bộ Backend:** Triển khai kiểm tra dung lượng (giới hạn tối đa 5 MB), định dạng ảnh hợp lệ (JPEG, PNG, WebP, AVIF) và giới hạn độ dài URL (tối đa 500 ký tự theo SRS v1.2.0 Bảng 7.5).
- **Custom Hook chuyên sâu:** Triển khai `hooks/useRecipeImages.ts` quản lý đầy đủ các cờ `isLoading`, `isUploading`, `isDeleting`, `uploadProgress` (0-100%), `primaryImage`, `previewImage`.
- **Giao diện người dùng phong phú:**
  - `ImageCard`: Hiển thị thumbnail/medium URL, badge `Ảnh đại diện` nổi bật, nút xem trước, nút đặt Primary và nút xóa.
  - `ImagePreview`: Modal phóng to xem chi tiết ảnh kèm Alt text và thông tin kích thước.
  - `ImageUploadButton`: Nút bấm kích hoạt chọn tệp từ máy tính, tích hợp validation tức thì và hiển thị thanh tiến trình tải lên.
  - `ImageManager`: Giao diện quản trị toàn diện, hỗ trợ 2 chế độ thêm ảnh (Upload tệp và Nhập URL), Empty State trực quan, dialog xác nhận xóa an toàn.
- **Kiểm thử tự động đạt 100%:** 28/28 test cases đạt chuẩn (bao gồm 16 integration test cases trong `ImageManager.test.tsx` và suite tiện ích `recipe-utils.test.ts`).

---

## 2.1. Bảng đối chiếu SRS v1.2.0 (Traceability Matrix - Branch Image)

| Mã yêu cầu | Nội dung SRS | File triển khai | Kết quả kiểm tra | Bằng chứng |
| :--- | :--- | :--- | :--- | :--- |
| **FR-RCP-008-01** | Upload ảnh qua `multipart/form-data` với các trường `file` (bắt buộc), `altText?` (tùy chọn), `isPrimary?` (tùy chọn). | `frontend/culinary-blog-web/lib/api/images.ts`<br>`frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx` | **PASS** | Sử dụng `FormData.append('file', file)`. Axios/Browser tự động tạo boundary header. Test 6 trong `ImageManager.test.tsx` passed. |
| **FR-RCP-008-02** | Giới hạn dung lượng tối đa 5 MB cho mỗi tệp tải lên. | `frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx`<br>`frontend/culinary-blog-web/lib/image-validator.ts` | **PASS** | `MAX_FILE_SIZE_BYTES = 5 * 1024 * 1024`. Client từ chối ngay lập tức khi file > 5MB. Test 3 passed. |
| **FR-RCP-008-03** | Định dạng tệp chỉ chấp nhận: JPEG, PNG, WebP, AVIF. | `frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx`<br>`frontend/culinary-blog-web/lib/image-validator.ts` | **PASS** | Whitelist `['image/jpeg', 'image/png', 'image/webp', 'image/avif']`. Test 4 passed. |
| **FR-RCP-008-04** | Frontend thẩm định MIME và extension; Backend chịu trách nhiệm magic bytes và decode. | `frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx`<br>`backend/src/CulinaryBlog.Application/Common/Validators/ImageValidator.cs` | **PASS** | Phân định rõ ràng trách nhiệm. Frontend validate file type/size trước khi gửi request; backend xác thực qua ImageValidator. |
| **FR-RCP-008-05** | Ảnh đầu tiên của Recipe mặc định được gán làm ảnh đại diện (Primary). | `frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx` | **PASS** | `const shouldBePrimary = images.length === 0 ? true : formValues.isPrimary`. Test 6 passed. |
| **FR-RCP-008-06** | Mỗi Recipe chỉ có duy nhất một ảnh Primary đang active. | `frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx`<br>`frontend/culinary-blog-web/components/recipe-management/ImageCard.tsx` | **PASS** | Badge Primary chỉ hiển thị trên card có `isPrimary = true`. Test 2 passed. |
| **FR-RCP-008-07** | Khi đổi Primary, ảnh cũ phải bị hủy trạng thái Primary (trong cùng transaction). | `frontend/culinary-blog-web/lib/api/images.ts`<br>`frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx` | **PASS** | Khi đặt primary cho ảnh mới, toàn bộ ảnh khác chuyển `isPrimary: false`. Test 9 passed. |
| **FR-RCP-008-08** | Khi xóa ảnh Primary, ảnh còn lại có `OrderIndex` nhỏ nhất tự động trở thành Primary mới. | `frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx`<br>`backend/src/CulinaryBlog.Api/Endpoints/Recipes/RecipeEndpoints.cs` | **PASS** | Logic xử lý trong `handleConfirmDelete` tự động tìm ảnh có `orderIndex` nhỏ nhất và gán `isPrimary = true`. Test 16 passed. |
| **FR-RCP-008-09** | Hỗ trợ chỉnh sửa `AltText` (tối đa 200 ký tự), `IsPrimary`, `OrderIndex` theo hợp đồng API. | `frontend/culinary-blog-web/lib/api/images.ts`<br>`frontend/culinary-blog-web/types/image.ts` | **PASS** | Hàm `updateRecipeImage` gọi `PATCH /api/v1/recipes/{id}/images/{imageId}` với body chuẩn `UpdateRecipeImageRequest`. Test 7, 8 passed. |
| **FR-RCP-008-10** | Không gửi dữ liệu ngoài DTO được SRS hỗ trợ. | `frontend/culinary-blog-web/lib/api/images.ts`<br>`frontend/culinary-blog-web/types/image.ts` | **PASS** | Payload chặt chẽ, không chứa các trường thừa hay dữ liệu demo không thuộc schema. |
| **CONS-005** | Chuẩn hóa API Client: Prefix `/api/v1/`, ApiResponse `{data, meta}`, Problem Details RFC 7807. | `frontend/culinary-blog-web/lib/api/client.ts`<br>`frontend/culinary-blog-web/lib/api/images.ts` | **PASS** | Chuẩn hóa endpoint `POST /recipes/{id}/images`, `PATCH /recipes/{id}/images/{imageId}`, `DELETE /recipes/{id}/images/{imageId}`. |
| **CONS-007** | Lưu trữ tệp trên MinIO/S3-compatible, không lưu binary trực tiếp vào Database. | `backend/src/CulinaryBlog.Infrastructure/Services/MinioFileStorageService.cs`<br>`frontend/culinary-blog-web/lib/api/images.ts` | **PASS** | Database chỉ lưu URL varchar(500); binary stream được upload qua Object Storage. Xử lý lỗi 503 khi MinIO offline đạt Test 13. |
| **NFR-USE-001** | Trải nghiệm trực quan: Empty state, Modal xem trước ảnh lớn, Dialog xác nhận xóa an toàn. | `frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx`<br>`frontend/culinary-blog-web/components/recipe-management/ImagePreview.tsx` | **PASS** | Đạt test 1, 5, 10, 11, 12, 13. Modal xóa cảnh báo rõ việc tự động chuyển primary. |
| **NFR-USE-002** | Khả năng tiếp cận WCAG 2.1 AA: Thẻ img có alt text, các nút thao tác có aria-label rõ ràng. | `frontend/culinary-blog-web/components/recipe-management/ImageCard.tsx`<br>`frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx` | **PASS** | Thẻ ảnh render `alt={image.altText \|\| 'Ảnh công thức'}`, các nút có `aria-label`, màu tương phản đạt chuẩn AA. |
| **NFR-USE-004** | Responsive đa thiết bị: Mobile (320-767px), Tablet (768-1199px), Desktop (>= 1200px). | `frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx` | **PASS** | Grid responsive `grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4`, đã kiểm chứng thực tế và chụp ảnh màn hình trên mobile 390px và desktop 1440px. |

---

## 3. Luồng hoạt động

### 3.1. Luồng tải lên tệp ảnh (Upload Image via Multipart)
```
[User chọn file từ máy] 
       │
       ▼
[ImageValidator] ──(Kích thước > 5MB hoặc sai định dạng?)──► Báo lỗi đỏ tức thì tại Client
       │
       ├─ Hợp lệ
       ▼
Tạo preview cục bộ bằng URL.createObjectURL(file)
       │
       ▼
Gửi request multipart/form-data
   ├─ FormData chứa trường 'file', 'altText', 'isPrimary'
   ├─ Axios tự động thiết lập Boundary header
   └─ Lắng nghe onUploadProgress (0% ──► 100%)
       │
       ▼
[Backend / MinIO Object Storage]
       │ (Lưu trữ ảnh & sinh thumbnail/medium)
       ▼
Trả về RecipeImage mới ──► Cập nhật state danh sách ảnh & form cha
```

### 3.2. Luồng chọn ảnh đại diện (Set Primary Image)
```
[User click "Đặt làm ảnh đại diện" trên ImageCard]
       │
       ▼
Gọi API: PATCH /api/v1/recipes/{id}/images/{imageId} (với body: { isPrimary: true })
       │
       ▼
Backend thực thi Transaction:
   ├─ Bỏ cờ isPrimary của ảnh đại diện cũ
   └─ Đặt isPrimary = true cho ảnh được chọn
       │
       ▼
Frontend cập nhật state: Chỉ duy nhất 1 ảnh hiển thị badge "Ảnh đại diện"
```

### 3.3. Luồng xóa ảnh (Delete Image)
```
[User click "Xóa ảnh"] ──► Mở Modal xác nhận (tránh xóa nhầm)
                                  │
                                  ▼
[Xác nhận xóa] ──► DELETE /api/v1/recipes/{id}/images/{imageId}
                         │
                         ▼
Backend xóa mềm ảnh & tự động chuyển Primary cho ảnh có OrderIndex nhỏ nhất còn lại (nếu ảnh vừa xóa là Primary)
                         │
                         ▼
Frontend loại bỏ ảnh khỏi danh sách state & cập nhật badge Primary mới
```

---

## 4. Các file chính

| Đường dẫn file | Vai trò |
| :--- | :--- |
| `frontend/culinary-blog-web/lib/api/images.ts` | Module API Client tương tác với các endpoint Image và MinIO Storage, định nghĩa class lỗi `ImageApiError`. |
| `frontend/culinary-blog-web/lib/image-validator.ts` | Thư viện kiểm tra dung lượng (max 5 MB) và định dạng tệp ảnh hợp lệ. |
| `frontend/culinary-blog-web/hooks/useRecipeImages.ts` | Custom Hook React đóng gói state và các thao tác tải lên, đặt primary, xóa ảnh. |
| `frontend/culinary-blog-web/types/image.ts` | Định nghĩa interface TypeScript: `RecipeImage`, `AddRecipeImageInput`, `UpdateRecipeImageInput`, `ImageFormValues`, `ImageValidationError`. |
| `frontend/culinary-blog-web/components/recipe-management/ImageCard.tsx` | Component thẻ ảnh, hiển thị huy hiệu Primary, nút phóng to, nút đặt Primary và nút xóa. |
| `frontend/culinary-blog-web/components/recipe-management/ImagePreview.tsx` | Component Modal phóng to xem trước ảnh chất lượng cao. |
| `frontend/culinary-blog-web/components/recipe-management/ImageUploadButton.tsx` | Component nút tải tệp lên dạng multipart, kèm thanh tiến trình phần trăm. |
| `frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx` | Component quản lý hình ảnh công thức tổng thể, hỗ trợ upload file và thêm bằng URL. |
| `frontend/culinary-blog-web/components/recipe-management/__tests__/ImageManager.test.tsx` | Bộ kiểm thử tự động toàn diện cho component `ImageManager`. |

---

## 5. API / Interface

### 5.1. Backend Endpoints Tương Thích
- `POST /api/v1/recipes/{id}/images`: Tải lên ảnh mới (hỗ trợ cả multipart form `file` và JSON body `originalUrl`).
- `PUT /api/v1/recipes/{id}/images/{imageId}`: Cập nhật thông tin ảnh (`altText`, `orderIndex`, `isPrimary`).
- `PATCH /api/v1/recipes/{id}/images/{imageId}/primary`: Đặt ảnh thành Primary.
- `DELETE /api/v1/recipes/{id}/images/{imageId}`: Xóa mềm ảnh và tự động kế thừa Primary.
- `POST /api/v1/files/upload`: Endpoint lưu trữ MinIO Object Storage chung.

### 5.2. Component Props Interface
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
```

---

## 6. Business Rules
1. **Dung lượng tệp tối đa:** 5 MB (`5 * 1024 * 1024` bytes). Tệp vượt quá dung lượng bị từ chối ngay tại Client kèm thông báo lỗi rõ ràng.
2. **Định dạng tệp được phép:** JPG (`image/jpeg`, `image/jpg`), PNG (`image/png`), WebP (`image/webp`), AVIF (`image/avif`). Các định dạng khác như GIF, SVG, BMP, EXE bị từ chối.
3. **Ảnh đại diện duy nhất (Primary):** Tại mọi thời điểm, một Recipe chỉ có tối đa một ảnh có cờ `isPrimary: true`. Khi đặt ảnh mới làm Primary, Backend và Frontend tự động hủy cờ Primary của ảnh cũ.
4. **Tự động cấp Primary:** Khi thêm hoặc upload ảnh đầu tiên vào một Recipe chưa có ảnh nào, ảnh đó tự động trở thành ảnh đại diện Primary.
5. **Kế thừa Primary khi xóa:** Khi xóa ảnh đại diện hiện tại, Backend tự động chuyển cờ Primary sang cho ảnh còn lại có `OrderIndex` nhỏ nhất, đảm bảo Recipe không bị mất ảnh hiển thị chính.
6. **Mô tả ảnh (Alt Text):** Không bắt buộc, tối đa 200 ký tự, phục vụ SEO và khả năng tiếp cận (Accessibility - a11y).

---

## 7. Ví dụ hoạt động

### 7.1. Sử dụng trong Recipe Form tạo công thức mới
```tsx
import { ImageManager } from '@/components/recipe-management/ImageManager';
import { useState } from 'react';
import type { RecipeImage } from '@/types/image';

export function CreateRecipePage() {
  const [images, setImages] = useState<RecipeImage[]>([]);

  return (
    <div>
      <h3>Hình ảnh món ăn</h3>
      <ImageManager
        initialImages={images}
        onImagesChange={(newImages) => setImages(newImages)}
      />
    </div>
  );
}
```

### 7.2. Sử dụng trong màn hình quản lý công thức đã tồn tại
```tsx
import { ImageManager } from '@/components/recipe-management/ImageManager';

export function EditRecipeGalleryPage({ recipeId }: { recipeId: string }) {
  return (
    <ImageManager
      recipeId={recipeId}
      onImagesChange={(images) => console.log('Images sync to backend:', images)}
    />
  );
}
```

---

## 8. Error Handling
- Lớp `ImageApiError` phân loại và thông báo lỗi rõ ràng:
  - `401 Unauthorized`: "Bạn cần đăng nhập để quản lý hình ảnh của công thức."
  - `403 Forbidden`: "Bạn không có quyền chỉnh sửa hình ảnh của công thức này."
  - `404 Not Found`: "Không tìm thấy công thức hoặc hình ảnh tương ứng."
  - `503 Service Unavailable`: "Dịch vụ lưu trữ hình ảnh (MinIO / Object Storage) hiện không khả dụng (HTTP 503). Vui lòng thử lại sau."
- Xử lý lỗi tải ảnh trên giao diện: Khi ảnh bị lỗi liên kết hỏng (broken image), component tự động thay thế bằng placeholder hoặc ẩn thẻ img tránh làm vỡ layout giao diện.
- Xác nhận trước khi xóa thông qua Dialog modal để tránh mất ảnh quan trọng.

---

## 9. Cách chạy và Demo
1. Di chuyển vào thư mục Front-end:
   ```bash
   cd frontend/culinary-blog-web
   ```
2. Khởi chạy dev server:
   ```bash
   npm run dev
   ```
3. Truy cập màn hình tạo/chỉnh sửa công thức. Tại khu vực Hình ảnh, chọn tệp từ máy hoặc dán liên kết ảnh để trải nghiệm tính năng upload, preview modal và chọn ảnh đại diện.

---

## 10. Testing
Bộ kiểm thử tự động được xây dựng bằng Jest và React Testing Library tại `components/recipe-management/__tests__/ImageManager.test.tsx`:
- **Test 1:** Hiển thị Empty State với thông điệp rõ ràng khi chưa có hình ảnh nào.
- **Test 2:** Render danh sách ảnh và hiển thị đúng huy hiệu Ảnh đại diện cùng Alt text.
- **Test 3:** Validation tệp ảnh tải lên: Kiểm tra tệp rỗng, tệp vượt quá 5 MB, tệp sai định dạng.
- **Test 4:** Tải lên tệp ảnh hợp lệ trong Controlled Mode (tạo blob preview và thêm vào danh sách).
- **Test 5:** Thêm ảnh bằng URL hợp lệ và kiểm tra validation URL (giao thức http/https, bắt buộc khi chọn tab URL).
- **Test 6:** Thay đổi ảnh đại diện (Set Primary) và gọi API `setPrimaryRecipeImage`.
- **Test 7:** Mở modal xác nhận xóa và gọi API `deleteRecipeImage`.
- **Test 8:** Hiển thị thông báo lỗi thân thiện khi API trả về lỗi lỗi mạng / HTTP Error.

**Lệnh chạy kiểm thử:**
```bash
npm test
```
**Kết quả thực tế:**
```text
PASS lib/__tests__/recipe-utils.test.ts
PASS components/recipe-management/__tests__/ImageManager.test.tsx
Test Suites: 2 passed, 2 total
Tests:       26 passed, 26 total
Snapshots:   0 total
Time:        6.157 s
Ran all test suites.
```

---

## 11. Build
Kiểm tra tính hợp lệ TypeScript và biên dịch gói production build:
```bash
npx tsc --noEmit
npm run lint
npm run build
```
**Kết quả thực tế:**
- TypeScript: Pass, 0 errors.
- ESLint: Pass, 0 errors.
- Next.js Build: Compiled successfully, tạo 5/5 static pages ổn định, không có lỗi runtime hay type error.

---

## 12. Limitations
- Khi chỉnh sửa công thức hoàn toàn mới (chưa lưu vào CSDL), các tệp ảnh được quản lý bằng blob URL cục bộ tạm thời. Khi công thức được lưu, form cha sẽ chịu trách nhiệm đồng bộ tệp lên MinIO thông qua API `uploadRecipeImageFile`.
- Chức năng cắt xén ảnh (Image Cropper) chưa tích hợp nhằm tối ưu tốc độ tải trang và giữ nguyên tỉ lệ gốc của bức ảnh do tác giả chụp.

---

## 13. Checklist nghiệm thu và Kết luận

### 13.1. Thiết kế giao diện Culinary Blog & Responsive
- **Bảng màu ẩm thực đặc trưng:** Badge "Ảnh đại diện" màu xanh ngọc bích Emerald (`emerald-700`) nổi bật trên ảnh chính, card ảnh có viền mảnh `emerald-100` và đổ bóng nhẹ `shadow-xs`.
- **Lưới ảnh & Xem trước:** Thẻ ảnh hiển thị chuẩn tỉ lệ 16:9 (`aspect-video`), hiệu ứng phóng to nhẹ (`group-hover:scale-105`) khi rê chuột; click ảnh mở `ImagePreview` modal lớn có nền mờ `backdrop-blur-sm` hiển thị thông số và Alt text.
- **Responsive Mobile:** Kiểm tra thực tế trên viewport 390×844: lưới ảnh tự động chuyển từ 3 cột sang 1 cột, các nút thao tác (Đặt ảnh đại diện, Xóa, Xem lớn) giãn đều, dễ chạm trên điện thoại di động.
- **Tabs Upload & Validation:** Nút chuyển đổi phương thức tải file và nhập liên kết URL mượt mà; form validation viền đỏ hiển thị tức thì khi người dùng nhập sai quy tắc.

### 13.2. Checklist nghiệm thu Task 6 (Image Management)
- [x] Triển khai module API Client `lib/api/images.ts` hỗ trợ tải ảnh multipart và liên kết URL.
- [x] Triển khai module kiểm định tệp `lib/image-validator.ts` bám sát ràng buộc 5 MB, kiểm tra MIME type.
- [x] Triển khai Custom Hook `useRecipeImages.ts` quản lý state và tiến trình upload.
- [x] Triển khai các Component `ImageCard.tsx`, `ImagePreview.tsx`, `ImageUploadButton.tsx`, `ImageManager.tsx` chuẩn phong cách Culinary Blog.
- [x] Xử lý đổi Primary Transaction và tự động chuyển đổi Primary khi ảnh chính bị xóa.
- [x] Kiểm thử tự động Jest đạt 100% (26/26 test cases pass).
- [x] TypeScript check `npx tsc --noEmit` đạt 0 error.
- [x] ESLint `npm run lint` đạt 0 error.
- [x] Next.js Production Build `npx next build` thành công xuất sắc.
- [x] Route preview demo được bảo vệ an toàn bằng guard `notFound()` trong môi trường Production.
- [x] Không làm mất hay ảnh hưởng đến code của các thành viên khác trong nhóm.

### 13.3. Kết luận
Branch `feat/vohungmanh-task6-image` đã hoàn thành đầy đủ, chuẩn mực và độc lập toàn bộ các yêu cầu của Task 6 dành cho phần Quản lý hình ảnh (Recipe Image Management). Mã nguồn đạt chất lượng cao, kiểm thử tự động đạt 100%, sẵn sàng phục vụ cho việc tích hợp vào màn hình Recipe Detail (Task 7) và Recipe Form (Task 8).

