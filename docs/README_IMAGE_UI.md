# Võ Hùng Mạnh - Quản lý hình ảnh công thức (Recipe Image UI)

## 1. Thông tin branch
- **Thành viên:** Võ Hùng Mạnh
- **Task:** Task 4 - Frontend RecipeImage UI
- **Tên branch:** `feat/vohungmanh-image-ui`
- **Base branch / Base commit:** `origin/main` (`a91589618f6460fc37e9b4861f2c5a0e946311b5`)
- **Mục tiêu branch:** Xây dựng module giao diện quản lý bộ sưu tập hình ảnh của công thức nấu ăn (`ImageManager`), hỗ trợ tải ảnh từ thiết bị kèm preview tức thì, thêm ảnh qua URL, thiết lập ảnh đại diện chính (`Set Primary`), xóa mềm ảnh kèm tự động chuyển cờ đại diện, xử lý lỗi lưu trữ MinIO 503, bám sát visual design của dự án Culinary Blog.

---

## 2. Branch này làm gì?
Branch triển khai đầy đủ các chức năng quản lý hình ảnh công thức thực tế trong mã nguồn:
- **Tải ảnh từ thiết bị (Upload):** Kéo thả hoặc chọn file ảnh trực tiếp từ máy tính.
- **Preview trước khi upload:** Hiển thị tức thì ảnh xem trước (thumbnail preview) bằng Object URL ngay khi người dùng chọn file hợp lệ.
- **Thêm ảnh qua URL:** Cho phép nhập đường dẫn ảnh trực tiếp kèm kiểm tra định dạng URL `http://` / `https://`.
- **Bộ sưu tập hình ảnh (Gallery Grid):** Hiển thị danh sách ảnh theo dạng lưới responsive (1 cột trên mobile, 2 cột trên tablet, 3 cột trên desktop) được sắp xếp tuần tự theo `OrderIndex`.
- **Chú thích ảnh (AltText):** Cho phép nhập và hiển thị chú thích mô tả hình ảnh hỗ trợ SEO và Accessibility (tối đa 200 ký tự).
- **Thứ tự hiển thị (OrderIndex):** Quản lý và tùy chỉnh thứ tự hiển thị của các ảnh trong bộ sưu tập.
- **Ảnh đại diện chính (IsPrimary):** Hiển thị badge nổi bật "Ảnh đại diện" cho hình ảnh chính của công thức. Quy tắc: ảnh đầu tiên được thêm sẽ tự động trở thành ảnh đại diện chính.
- **Đặt làm ảnh chính (Set Primary):** Nút hành động cho phép chuyển đổi ảnh đại diện. Khi bấm, frontend gọi API PATCH; Backend tự động hạ cờ primary của các ảnh khác trong cùng database transaction và invalidate cache. Giao diện cập nhật state ngay lập tức.
- **Xóa hình ảnh (Delete):** Nút xóa ảnh có hộp thoại popup xác nhận (`Delete Confirmation Dialog`). Sau khi xóa thành công ở backend, hệ thống tự động gán cờ Primary cho ảnh có `OrderIndex` nhỏ nhất còn lại.
- **Client Validation:** Kiểm tra dung lượng tối đa 5 MB và định dạng file cho phép (JPEG, PNG, WebP, AVIF) ngay ở client-side nhằm tối ưu UX.
- **Xử lý trạng thái (UI States):** Hỗ trợ đầy đủ Empty State, Loading State (spinner, disabled form), Error Banner (xử lý lỗi 401, 403, 404 và 503 Storage Service).

---

## 3. Branch này KHÔNG làm gì?
Nhằm đảm bảo ranh giới phân công độc lập trong nhóm 12:
- KHÔNG chỉnh sửa `RecipeForm.tsx` (thuộc quyền quản lý của TV3 - Phạm Nguyễn Ngọc Phước).
- KHÔNG chỉnh sửa module `StepEditor` (đã nằm trên branch riêng `feat/vohungmanh-step-ui`).
- KHÔNG can thiệp vào form thông tin cơ bản của công thức, nguyên liệu (IngredientEditor) hay dinh dưỡng (Nutrition).
- KHÔNG thay thế cơ chế xác thực Magic Bytes bảo mật ở tầng backend (client validation chỉ phục vụ trải nghiệm người dùng tức thì).
- KHÔNG làm giao diện tìm kiếm (Search UI - nằm trên branch riêng `feat/vohungmanh-search-ui`).

---

## 4. Cấu trúc file
Dưới đây là danh sách CHÍNH XÁC các file được tạo hoặc sửa đổi trên branch:

- `frontend/culinary-blog-web/components/recipe-management/ImageCard.tsx`: Sub-component hiển thị từng thẻ ảnh trong bộ sưu tập, bao gồm thumbnail, badge "Ảnh đại diện", nhãn thứ tự, thông tin Alt text, nút "Đặt làm ảnh chính" và nút "Xóa".
- `frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx`: Container component chính quản lý toàn bộ bộ sưu tập ảnh, form upload/URL, preview ảnh, modal xác nhận xóa, client validation và error states.
- `frontend/culinary-blog-web/components/recipe-management/__tests__/ImageManager.test.tsx`: Test suite gồm 14 kịch bản kiểm thử toàn diện cho ImageManager.
- `frontend/culinary-blog-web/lib/api.ts`: Module abstraction dùng chung cho HTTP request (`apiFetch`, `apiJson`, `ApiError`), tự động gắn token và parse lỗi RFC 7807.
- `frontend/culinary-blog-web/lib/api/images.ts`: API Client giao tiếp với các endpoint Backend của RecipeImage và upload file Object Storage.
- `frontend/culinary-blog-web/types/image.ts`: Định nghĩa TypeScript interfaces chuẩn (`RecipeImage`, `AddRecipeImageInput`, `ImageFormValues`, `ImageValidationError`).
- `frontend/culinary-blog-web/jest.config.mjs`: Cấu hình Jest ES Module native cho ứng dụng Next.js.
- `frontend/culinary-blog-web/package.json`: Cập nhật script test trỏ tới `jest.config.mjs`.
- `docs/README_IMAGE_UI.md`: Tài liệu kỹ thuật chi tiết của branch Image UI.
- `docs/VO_HUNG_MANH_IMAGE_UI_COMPLAN.md`: Báo cáo giải trình kỹ thuật bảo vệ đồ án, 10 câu hỏi phản biện và phân tích rủi ro.

---

## 5. Component Architecture
Module hình ảnh được phân chia cấu trúc rõ ràng:

```
[ RecipeForm (TV3) ]
         │ (Tích hợp trong tương lai qua recipeId)
         ▼
 ┌────────────────────────────────────────────────────────┐
 │                   ImageManager.tsx                     │
 │  - Quản lý state danh sách ảnh (images: RecipeImage[]) │
 │  - Form Upload / Direct URL + Client Validation        │
 │  - Preview ảnh (URL.createObjectURL)                  │
 │  - Modal xác nhận xóa ảnh (Delete Confirmation)       │
 │  - Error Banner (401, 403, 404, 503)                   │
 └───────────────────────┬────────────────────────────────┘
                         │ renders
                         ▼
        ┌──────────────────────────────────┐
        │          ImageCard.tsx           │  (Lặp qua từng ảnh)
        │  - Thumbnail (object-cover)      │
        │  - Badge Primary (Xanh lá đậm)   │
        │  - Badge OrderIndex              │
        │  - Nút "Đặt làm ảnh chính"       │
        │  - Nút "Xóa"                     │
        └──────────────────────────────────┘
```

---

## 6. API được sử dụng
Module giao tiếp với các RESTful endpoints sau trên Backend:

| Method | Endpoint | Request Body | Response / Mục đích |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/recipes/{slugOrId}` | *None* | Trả về `RecipeDetailDto` chứa mảng `images` sắp xếp theo `OrderIndex`. |
| `POST` | `/api/v1/recipes/{recipeId}/images` | `{ originalUrl, mediumUrl?, thumbnailUrl?, altText?, isPrimary?, orderIndex? }` | Thêm ảnh mới vào công thức. Ảnh đầu tiên tự động thành Primary. |
| `PATCH` | `/api/v1/recipes/{recipeId}/images/{imageId}/primary` | *None* | Đặt làm ảnh đại diện chính. Backend hạ cờ các ảnh khác và commit trong 1 transaction. |
| `DELETE` | `/api/v1/recipes/{recipeId}/images/{imageId}` | *None* | Xóa mềm ảnh. Backend tự động gán Primary cho ảnh có OrderIndex nhỏ nhất còn lại. |
| `POST` | `/api/v1/files/upload` | `multipart/form-data` (file) | Tải file nhị phân lên dịch vụ Object Storage / MinIO. Trả về `{ url }`. |

---

## 7. Luồng hoạt động

### Luồng 1: Tải lên và thêm ảnh mới
1. Người dùng chọn file từ máy tính hoặc nhập liên kết ảnh trực tiếp.
2. Tại Client:
   - Nếu chọn file: kích hoạt `validateFile()` kiểm tra dung lượng (<= 5MB) và loại MIME (`image/jpeg, image/png, image/webp, image/avif`).
   - Nếu hợp lệ: tạo preview tức thì qua `URL.createObjectURL(file)`.
3. Người dùng nhập chú thích (Alt text), tùy chỉnh OrderIndex và bấm nút `Tải lên & Lưu hình ảnh`.
4. Client gọi `uploadImageFile(file)` (gửi multipart/form-data lên Storage).
5. Nhận URL công khai từ Storage, sau đó gọi `POST /api/v1/recipes/{recipeId}/images`.
6. Backend lưu bản ghi vào PostgreSQL, trả về đối tượng `RecipeImage`.
7. Client cập nhật danh sách ảnh trong gallery, reset form và giải phóng bộ nhớ blob URL qua `URL.revokeObjectURL()`.

### Luồng 2: Đổi ảnh đại diện (Set Primary)
1. Người dùng bấm nút `Đặt làm ảnh chính` trên thẻ `ImageCard`.
2. Client kích hoạt trạng thái loading và gọi `PATCH /api/v1/recipes/{recipeId}/images/{imageId}/primary`.
3. Backend hạ cờ `IsPrimary = false` của tất cả các ảnh khác và set `IsPrimary = true` cho ảnh đích trong cùng 1 transaction, đồng thời xóa cache Redis của Recipe Detail.
4. Client cập nhật UI: badge "Ảnh đại diện" chuyển sang ảnh mới được chọn.

### Luồng 3: Xóa hình ảnh và tự động chuyển cờ đại diện
1. Người dùng bấm nút `Xóa` trên thẻ ảnh -> Modal popup hiển thị cảnh báo xác nhận.
2. Khi người dùng xác nhận: Client gọi `DELETE /api/v1/recipes/{recipeId}/images/{imageId}`.
3. Backend xóa mềm (`IsDeleted = true`). Nếu ảnh bị xóa là ảnh Primary, backend tự động tìm ảnh có `OrderIndex` nhỏ nhất còn lại để nâng cờ `IsPrimary = true`.
4. Client cập nhật danh sách ảnh, loại bỏ ảnh đã xóa và kích hoạt badge Primary cho ảnh kế tiếp.

---

## 8. Validation
Validation được chia thành 2 tầng rõ rệt:

1. **Client-side Validation (Phục vụ trải nghiệm người dùng):**
   - Dung lượng file tối đa: `5 MB` (`file.size <= 5 * 1024 * 1024`).
   - Định dạng ảnh cho phép: `JPEG`, `PNG`, `WebP`, `AVIF`.
   - Đường dẫn ảnh (URL mode): Bắt buộc, hợp lệ theo chuẩn URL, bắt đầu bằng `http://` hoặc `https://`, độ dài tối đa 2048 ký tự.
   - Chú thích ảnh (Alt text): Tùy chọn, tối đa 200 ký tự (có bộ đếm ký tự trực quan).
   - Thứ tự hiển thị (OrderIndex): Tùy chọn, số nguyên không âm (>= 0).
2. **Backend Validation & Security:**
   - Kiểm tra quyền sở hữu công thức (AuthorId == CurrentUserId hoặc vai trò Admin).
   - Kiểm tra Magic Bytes file nhị phân thực tế để ngăn chặn việc đổi đuôi file độc hại (vd: shell script giả mạo `.jpg`).

---

## 9. Error Handling
- **401 Unauthorized:** Thông báo "Bạn cần đăng nhập để quản lý hình ảnh của công thức."
- **403 Forbidden:** Thông báo "Bạn không có quyền chỉnh sửa hình ảnh của công thức này."
- **404 Not Found:** Thông báo "Không tìm thấy công thức hoặc hình ảnh tương ứng."
- **503 Service Unavailable (Storage/MinIO):** Bắt lỗi khi dịch vụ MinIO chưa được khởi động hoặc mất kết nối: "Dịch vụ lưu trữ hình ảnh (MinIO / Object Storage) hiện không khả dụng (HTTP 503). Vui lòng thử lại sau."
- **Validation Errors (RFC 7807):** Bắt lỗi chi tiết từng trường từ backend và hiển thị chữ đỏ ngay dưới ô nhập liệu tương ứng.
- **Delete Confirmation:** Popup xác nhận ngăn chặn nguy cơ người dùng bấm nhầm nút xóa.

---

## 10. Visual Design
Giao diện bám sát 100% phong cách của dự án Culinary Blog:
- **Tone màu chủ đạo:** Màu xanh lá đậm (`#166534` / `bg-green-800` / `hover:bg-green-900`) dùng cho nút chính `Tải lên & Lưu hình ảnh` và badge `Ảnh đại diện`.
- **Nền sáng sạch sẽ:** Nền container và form sử dụng màu trắng / off-white (`bg-white`, `bg-gray-50/50`).
- **Thẻ Card:** Nền trắng, viền xám mảnh nhẹ (`border-gray-200`), bo góc vừa phải (`rounded-xl`), tỉ lệ ảnh 16:9 (`aspect-video`), hiệu ứng hover zoom nhẹ tinh tế (`hover:scale-102`).
- **Hệ thống nút bấm (Button Hierarchy):**
  - Primary button: Màu xanh lá đậm (`bg-green-800 text-white`).
  - Outline button: Viền xám nền trắng (`border-gray-300 text-gray-700 hover:bg-gray-50`).
  - Destructive button: Nút xóa màu xám, chỉ chuyển đỏ khi hover (`hover:text-red-600 hover:bg-red-50 hover:border-red-200`).
- **Responsive:** Co giãn linh hoạt từ 1 cột (Mobile) đến 2-3 cột (Tablet, Desktop).

---

## 11. Testing
- **File kiểm thử:** `frontend/culinary-blog-web/components/recipe-management/__tests__/ImageManager.test.tsx`
- **Lệnh chạy kiểm thử:**
  ```powershell
  cd frontend/culinary-blog-web
  npm test
  ```
- **Danh sách 14 kịch bản kiểm thử đã thực hiện:**
  1. Hiển thị Empty State khi chưa có ảnh nào.
  2. Render danh sách ảnh và hiển thị đúng badge Ảnh đại diện cùng Alt text.
  3. Từ chối file có kích thước vượt quá 5 MB.
  4. Từ chối file có định dạng không được hỗ trợ (chỉ nhận JPEG, PNG, WebP, AVIF).
  5. Hiển thị preview ảnh khi chọn file hợp lệ.
  6. Thêm ảnh thành công và gọi `addRecipeImage` trong Direct API mode.
  7. Validation AltText không vượt quá 200 ký tự.
  8. Cho phép tùy chỉnh OrderIndex khi thêm ảnh.
  9. Đặt làm ảnh đại diện gọi `setPrimaryRecipeImage` và cập nhật UI.
  10. Bấm nút xóa mở modal yêu cầu xác nhận trước khi thực hiện.
  11. Xác nhận xóa ảnh gọi `deleteRecipeImage` và loại bỏ khỏi danh sách.
  12. Hiển thị Error Banner màu đỏ khi API trả về lỗi (401/403/404).
  13. Hiển thị thông báo thân thiện khi dịch vụ lưu trữ trả về HTTP 503.
  14. Vô hiệu hóa nút và form khi prop `disabled` được truyền từ bên ngoài.
- **Kết quả kiểm thử thực tế mới nhất:**
  - `Test Suites:` **2 passed, 2 total**
  - `Tests:` **26 passed, 26 total** (14/14 test cases của `ImageManager.test.tsx` + 12 test cases của `recipe-utils.test.ts` PASS 100%)
  - `Time:` 7.424 s

---

## 12. Build
- **Lệnh đóng gói build:**
  ```powershell
  cd frontend/culinary-blog-web
  $env:NODE_OPTIONS="--max-old-space-size=4096"; npm run build
  ```
- **Kết quả build thực tế:**
  - Next.js 15.5.25 (Turbopack)
  - `Compiled successfully in 2.1s`
  - `Generating static pages (5/5)` hoàn thành
  - Zero TypeScript error, exit code 0.

---

## 13. Cách chạy branch
Khởi chạy branch từ đầu trên môi trường phát triển:

```bash
git switch feat/vohungmanh-image-ui
cd frontend/culinary-blog-web
npm install
npm run dev
```

- **URL truy cập local:** `http://localhost:3000`

---

## 14. Cách demo với giảng viên
Trình tự demo 8 bước cụ thể:

1. **Khởi tạo & Empty State:** Mở component `ImageManager` khi chưa có ảnh nào -> Trình bày giao diện thông báo "Chưa có hình ảnh nào cho công thức" và hướng dẫn tải lên ảnh đầu tiên.
2. **Chọn ảnh hợp lệ & Preview:** Chọn một file ảnh `.jpg` dung lượng 2 MB -> Giao diện lập tức hiển thị thumbnail ảnh xem trước, dung lượng và tên file.
3. **Thêm chú thích & Tải lên:** Nhập Alt text: "Bát phở bò truyền thống", bấm `Tải lên & Lưu hình ảnh` -> Ảnh được đưa vào gallery và tự động mang badge "Ảnh đại diện".
4. **Thêm ảnh thứ 2:** Chuyển sang tab "Nhập liên kết (URL)", dán link ảnh thứ hai, nhập OrderIndex = 1 -> Thêm thành công, hiển thị 2 ảnh cạnh nhau trên grid.
5. **Đổi ảnh đại diện (Set Primary):** Bấm nút `Đặt làm ảnh chính` ở ảnh thứ 2 -> Badge "Ảnh đại diện" chuyển sang ảnh thứ 2, ảnh đầu tiên tự động mất cờ đại diện.
6. **Xóa ảnh và Modal Confirmation:** Bấm nút `Xóa` trên ảnh thứ 2 (đang là ảnh chính) -> Modal cảnh báo hiện ra thông báo ảnh này đang là ảnh chính. Bấm `Xóa hình ảnh` -> Ảnh biến mất, và ảnh đầu tiên tự động được thăng cấp lại thành ảnh đại diện.
7. **Kiểm tra Validation vượt quá 5 MB:** Chọn một file ảnh dung lượng 6 MB -> Hệ thống lập tức từ chối và báo lỗi đỏ: "Dung lượng ảnh vượt quá giới hạn 5 MB."
8. **Kiểm tra File sai định dạng:** Chọn file `.pdf` hoặc `.txt` -> Hệ thống báo lỗi đỏ: "Định dạng ảnh không hợp lệ. Chỉ chấp nhận JPEG, PNG, WebP hoặc AVIF."

---

## 15. Ranh giới ownership
- **Mã nguồn do Võ Hùng Mạnh sở hữu:** `ImageManager.tsx`, `ImageCard.tsx`, `lib/api/images.ts`, `types/image.ts`, `ImageManager.test.tsx`.
- **Điểm tích hợp với thành viên khác:** `ImageManager` sẵn sàng để nhúng vào Section Upload Hình ảnh của `RecipeForm.tsx` (thuộc TV3 - Phạm Nguyễn Ngọc Phước) qua prop `recipeId`.
- **Cam kết:** Branch này **tuyệt đối không chỉnh sửa `RecipeForm.tsx`**, không sửa các module Step, Category hay Search của thành viên khác.

---

## 16. Hạn chế hiện tại
- `ImageManager` hiện tại là component độc lập, chưa mount trực tiếp vào `RecipeForm.tsx` để bảo vệ mã nguồn của TV3.
- Cần có backend (.NET Web API) và MinIO container đang hoạt động để chạy được API upload file thực tế trên môi trường live.
- Sẽ thực hiện integration test chung toàn bộ quy trình khi TV3 ráp nối form hoàn chỉnh.

---

## 17. Tài liệu COMPLAN
Báo cáo giải trình kỹ thuật chuyên sâu về kiến trúc MinIO, phân tích rủi ro và 10 câu hỏi bảo vệ đồ án được lưu trữ tại:
- [VO_HUNG_MANH_IMAGE_UI_COMPLAN.md](file:///b:/PTUDWNC-2026-Nhom12-MANH/docs/VO_HUNG_MANH_IMAGE_UI_COMPLAN.md)
