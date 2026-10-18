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
- **Validation đồng bộ Backend:** Triển khai `lib/image-validator.ts` bám sát `ImageValidator.cs` của Backend (giới hạn 5 MB, kiểm tra MIME type và phần mở rộng hợp lệ).
- **Custom Hook chuyên sâu:** Triển khai `hooks/useRecipeImages.ts` quản lý đầy đủ các cờ `isLoading`, `isUploading`, `isDeleting`, `uploadProgress` (0-100%), `primaryImage`, `previewImage`.
- **Giao diện người dùng phong phú:**
  - `ImageCard`: Hiển thị thumbnail/medium URL, badge `Ảnh đại diện` nổi bật, nút xem trước, nút đặt Primary và nút xóa.
  - `ImagePreview`: Modal phóng to xem chi tiết ảnh kèm Alt text và thông tin kích thước.
  - `ImageUploadButton`: Nút bấm kích hoạt chọn tệp từ máy tính, tích hợp validation tức thì và hiển thị thanh tiến trình tải lên.
  - `ImageManager`: Giao diện quản trị toàn diện, hỗ trợ 2 chế độ thêm ảnh (Upload tệp và Nhập URL), Empty State trực quan, dialog xác nhận xóa an toàn.
- **Kiểm thử tự động đạt 100%:** 26/26 test cases đạt chuẩn (bao gồm 8 integration test suites trong `ImageManager.test.tsx` và suite tiện ích `recipe-utils.test.ts`).

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
Gọi API: PATCH /api/v1/recipes/{id}/images/{imageId}/primary
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

## 13. Kết luận
Branch `feat/vohungmanh-task6-image` đã hoàn thành đầy đủ, chuẩn mực và độc lập toàn bộ các yêu cầu của Task 6 dành cho phần Quản lý hình ảnh (Recipe Image Management). Mã nguồn đạt chất lượng cao, kiểm thử tự động đạt 100%, tuân thủ chặt chẽ ranh giới module của nhóm và sẵn sàng nghiệm thu.
