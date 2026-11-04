# TASK 4 (BỮA 4) – GIAO DIỆN QUẢN LÝ HÌNH ẢNH CÔNG THỨC (RECIPE IMAGE UI PROTOTYPE)

**Sinh viên:** Võ Hùng Mạnh
**MSSV:** 2312687
**Nhóm:** 12
**Nhánh:** `feat/vohungmanh-image-ui`

---

### 1. Mục tiêu Task 4 liên quan đến nhánh
Trong khuôn khổ Bữa 4 (Task 4), song song với hạ tầng MinIO và API Backend, nhánh này chịu trách nhiệm nghiên cứu và phát triển nguyên mẫu giao diện người dùng (UI Prototype) cho module Quản lý hình ảnh công thức nấu ăn (`ImageManager`). Giao diện cung cấp giải pháp tải tệp từ máy tính, thêm ảnh từ URL ngoài, hiển thị lưới ảnh (Grid Layout), gắn cờ ảnh đại diện chính và kiểm tra ràng buộc tệp tin an toàn phía client.

### 2. Công việc cá nhân thực tế
- Khai báo hệ thống kiểu dữ liệu TypeScript tại `frontend/.../types/image.ts` (`RecipeImage`, `AddRecipeImageInput`, `ImageFormValues`, `ImageValidationError`).
- Xây dựng thư viện giao tiếp API `frontend/.../lib/api/images.ts` hỗ trợ tải tệp multipart qua Axios, gọi API thêm ảnh, gán ảnh chính và xóa ảnh.
- Phát triển component chính `ImageManager.tsx` (776 dòng mã):
  - Hỗ trợ tải tệp kéo thả hoặc duyệt từ thư mục máy tính.
  - Hỗ trợ nhập trực tiếp đường dẫn URL ngoài kèm mô tả (Alt Text).
  - Tích hợp thanh tiến trình tải lên và hiển thị lỗi xác thực thời gian thực.
- Xây dựng component thẻ ảnh `ImageCard.tsx` (124 dòng mã) hiển thị hình ảnh responsive, badge "Ảnh chính", nút "Đặt làm ảnh chính" và nút "Xóa ảnh".
- Triển khai xác thực phía máy tính: chặn tệp vượt quá 5 MB, chỉ chấp nhận định dạng ảnh hợp lệ (`image/jpeg`, `image/png`, `image/webp`, `image/avif`).
- Viết 317 dòng mã kiểm thử giao diện tự động bằng Jest trong `ImageManager.test.tsx`.

### 3. Chức năng đã hoàn thành (UI Prototype)
- **Tải ảnh từ máy tính & Xem trước (Upload Preview):** Sử dụng `URL.createObjectURL` để hiển thị ảnh xem trước ngay lập tức khi người dùng chọn file trước khi gửi lên server.
- **Thêm ảnh qua URL ngoài:** Cho phép dán trực tiếp link ảnh online có kiểm tra định dạng giao thức HTTP/HTTPS.
- **Quản lý ảnh chính duy nhất (Set Primary):** Bấm nút "Đặt làm ảnh chính" sẽ cập nhật badge màu vàng nổi bật và tự động hủy cờ chính của các ảnh khác trong danh sách.
- **Xóa ảnh khỏi danh sách:** Nút xóa có hộp thoại xác nhận nhanh, ngăn chặn việc vô tình xóa nhầm ảnh quan trọng.
- **Xác thực Client an toàn:** Cảnh báo tức thì nếu người dùng chọn tệp tin dung lượng lớn hơn 5 MB hoặc định dạng tệp không được hỗ trợ.

### 4. Cách xử lý và luồng hoạt động
```text
Người dùng chọn tệp ảnh hoặc nhập URL
        │
        ├── Client Validation:
        │     ├── Kiểm tra dung lượng (File Size <= 5MB)
        │     └── Kiểm tra MIME type hợp lệ (JPEG / PNG / WebP / AVIF)
        │
        ├── Hiển thị xem trước ảnh (Preview)
        │
        └── Thực thi hành động:
              │
              ├── Tải file tệp tin:
              │     └── uploadImageFile() qua Axios FormData gửi tới POST /api/v1/recipes/{id}/images
              │
              ├── Thêm qua URL:
              │     └── addRecipeImage() gửi JSON tới Backend
              │
              ├── Bấm "Đặt làm ảnh chính":
              │     └── setPrimaryRecipeImage() gọi PUT /api/v1/recipes/{id}/images/{imageId}/primary
              │
              └── Bấm "Xóa ảnh":
                    └── deleteRecipeImage() gọi DELETE /api/v1/recipes/{id}/images/{imageId}
```

### 5. Danh sách file mã nguồn quan trọng

| Đường dẫn file | Vai trò và chức năng chính |
| :--- | :--- |
| `frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx` | 776 dòng mã component điều khiển upload tệp, form URL, hiển thị danh sách ảnh và quản lý state. |
| `frontend/culinary-blog-web/components/recipe-management/ImageCard.tsx` | 124 dòng mã thẻ hiển thị từng hình ảnh, badge ảnh chính và các nút hành động. |
| `frontend/culinary-blog-web/types/image.ts` | Định nghĩa interface TypeScript: `RecipeImage`, `AddRecipeImageInput`, `ImageValidationError`. |
| `frontend/culinary-blog-web/lib/api/images.ts` | 133 dòng mã thư viện Axios gọi API quản lý ảnh (`uploadImageFile`, `setPrimaryRecipeImage`, v.v.). |
| `frontend/culinary-blog-web/__tests__/ImageManager.test.tsx` | 317 dòng kiểm thử giao diện với Jest và React Testing Library. |
| `docs/README_IMAGE_UI.md` | Tài liệu kỹ thuật chi tiết về thiết kế giao diện ImageManager. |
| `docs/VO_HUNG_MANH_IMAGE_UI_COMPLAN.md` | Kế hoạch triển khai và đặc tả các kịch bản kiểm thử giao diện Bữa 4. |

### 6. API, Database và Infrastructure liên quan
- **Framework Frontend:** Next.js 14+ (App Router), React 18, TypeScript, Tailwind CSS.
- **Thư viện kiểm thử:** Jest, `@testing-library/react`.
- **API Utilities:** Axios Client (`lib/api.ts`).
- **Endpoints kết nối:**
  - `POST /api/v1/recipes/{id}/images`
  - `PUT /api/v1/recipes/{id}/images/{imageId}/primary`
  - `DELETE /api/v1/recipes/{id}/images/{imageId}`

### 7. Hướng dẫn chạy

#### Bước 1: Cài đặt dependencies (nếu chưa có)
```bash
cd frontend/culinary-blog-web
npm install
```

#### Bước 2: Chạy ứng dụng Next.js trên môi trường dev
```bash
npm run dev
```
Mở trình duyệt tại: `http://localhost:3000`.

### 8. Hướng dẫn kiểm thử

#### Kiểm thử giao diện tự động với Jest
```bash
cd frontend/culinary-blog-web
npm test -- ImageManager.test.tsx
```

#### Kiểm thử tương tác thủ công
1. Nhúng component `<ImageManager initialImages={sampleImages} />` vào màn hình thử nghiệm.
2. Thử chọn một tệp ảnh dung lượng > 5MB -> quan sát thông báo lỗi đỏ `"Kích thước tệp không được vượt quá 5MB"`.
3. Nhập một URL ảnh hợp lệ và nhấn "Thêm ảnh" -> kiểm tra ảnh mới xuất hiện trong danh sách.
4. Bấm "Đặt làm ảnh chính" ở một ảnh bất kỳ -> kiểm tra badge "Ảnh chính" chuyển sang ảnh đó.

### 9. Kết quả kiểm thử có bằng chứng
- **Kết quả Unit Test (Jest):** Toàn bộ test suite trong `ImageManager.test.tsx` đạt kết quả `PASS` (bao phủ: render lưới ảnh, upload file hợp lệ, chặn file quá cỡ, đổi ảnh chính và xóa ảnh).
- **Trải nghiệm người dùng:** Giao diện phản hồi mượt mà, layout lưới tự động co giãn theo kích thước màn hình.

### 10. Hạn chế và tồn đọng
- **Trạng thái thực tế:** Đây là **Nguyên mẫu giao diện (UI Prototype)** hoàn chỉnh về mặt component đơn lẻ và validation client.
- **Tồn đọng:** Ở mốc Task 4, giao diện chưa tích hợp modal xem ảnh phóng to toàn màn hình (Lightbox) và tính năng kéo thả đổi thứ tự ảnh (Drag & Drop Reorder); các tính năng cao cấp này được chuyển tiếp hoàn thiện tại Task 6 và Task 8.

### 11. Liên kết tài liệu kỹ thuật chi tiết
- Chi tiết thiết kế giao diện ImageManager: [docs/README_IMAGE_UI.md](docs/README_IMAGE_UI.md)
- Kế hoạch triển khai kỹ thuật Bữa 4: [docs/VO_HUNG_MANH_IMAGE_UI_COMPLAN.md](docs/VO_HUNG_MANH_IMAGE_UI_COMPLAN.md)

---

# Phát triển ứng dụng Web nâng cao

## Nhóm 12

### Thành viên
1. Võ Hùng Mạnh - 2312687 - 2312687@dlu.edu.vn - Nhóm trưởng
2. Lê Thị Ánh Nhung - 2312709 - 2312709@dlu.edu.vn
3. Phạm Nguyễn Ngọc Phước - 2312718 - 2312718@dlu.edu.vn
4. Nguyễn Văn Quốc - 2312729 - 2312729@dlu.edu.vn
