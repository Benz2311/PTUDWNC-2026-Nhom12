# TASK 4 (BỮA 4) – GIAO DIỆN QUẢN LÝ CÁC BƯỚC NẤU ĂN (RECIPE STEP UI PROTOTYPE)

**Sinh viên:** Võ Hùng Mạnh
**MSSV:** 2312687
**Nhóm:** 12
**Nhánh:** `feat/vohungmanh-step-ui`

---

### 1. Mục tiêu Task 4 liên quan đến nhánh
Trong khuôn khổ Bữa 4 (Task 4), song song với việc xây dựng các API Backend, nhánh này đảm nhận nghiên cứu và phát triển nguyên mẫu giao diện người dùng (UI Prototype) cho module Quản lý các bước thực hiện công thức nấu ăn (`StepEditor`). Giao diện phải hỗ trợ người dùng nhập liệu trực quan, xác thực form phía máy khách (Client-side Validation), và hỗ trợ cơ chế hoạt động linh hoạt (Dual Mode) cho cả trang tạo mới và trang cập nhật công thức.

### 2. Công việc cá nhân thực tế
- Xây dựng hệ thống kiểu dữ liệu TypeScript cho bước nấu tại `frontend/.../types/step.ts` (`RecipeStep`, `CreateStepInput`, `UpdateStepInput`, `StepFormValues`).
- Hiện thực API client helper trong `frontend/.../lib/api/steps.ts` tích hợp Axios để gọi các endpoint CRUD và Reorder của Backend.
- Phát triển component chính `StepEditor.tsx` (672 dòng mã) hỗ trợ 2 chế độ:
  - *Direct API Mode:* Thao tác trực tiếp với Backend API khi đã có `recipeId`.
  - *Controlled Mode:* Quản lý mảng dữ liệu trên Local State và truyền qua callback `onChange` khi đang tạo mới công thức.
- Tách nhỏ component hiển thị thẻ bước nấu `StepItem.tsx` (170 dòng mã) có các nút điều hướng di chuyển vị trí (Move Up / Move Down), nút chỉnh sửa nội tuyến và xóa bước.
- Triển khai logic xác thực form trực tiếp: kiểm tra tiêu đề, mô tả bắt buộc, thời gian hẹn giờ không âm và định dạng URL ảnh.
- Viết 381 dòng mã kiểm thử tự động với Jest và React Testing Library trong `StepEditor.test.tsx`.

### 3. Chức năng đã hoàn thành (UI Prototype)
- **Thêm bước nấu mới trực quan:** Form nhập liệu gồm Tiêu đề, Mô tả chi tiết, Thời gian hẹn giờ (phút) và URL ảnh minh họa kèm hiển thị lỗi thời gian thực.
- **Sắp xếp thứ tự linh hoạt (Reorder):** Cung cấp hai nút bấm "Di chuyển lên" và "Di chuyển xuống" cho từng bước; hệ thống tự động cập nhật lại `stepNumber` tuần tự.
- **Chỉnh sửa nội tuyến (Inline Editing):** Cho phép chuyển đổi nhanh giữa chế độ xem và chế độ sửa nội dung của từng bước mà không làm mất dữ liệu các bước khác.
- **Xóa bước và tự động đánh số lại:** Khi xóa một bước, các bước tiếp theo tự động giảm số thứ tự để duy trì tính liên tục (1, 2, 3...).
- **Cơ chế Dual-mode linh hoạt:** Cho phép tái sử dụng component `StepEditor` trong cả màn hình Tạo mới công thức (lưu tạm state) và màn hình Sửa công thức (gọi API Backend lưu ngay).

### 4. Cách xử lý và luồng hoạt động
```text
Người dùng thao tác trên giao diện StepEditor
        │
        ├── Nhập form thêm/sửa bước
        │     │
        │     ▼
        │   Client Validation (Kiểm tra độ dài, số phút timer >= 0)
        │
        ├── Bấm Move Up / Move Down
        │     │
        │     ▼
        │   Hoán đổi vị trí trong mảng state + cập nhật lại stepNumber
        │
        └── Tùy thuộc vào chế độ hoạt động (Props):
              │
              ├── Controlled Mode (Tạo mới công thức):
              │     └── Cập nhật state nội bộ và phát sự kiện onChange(steps) lên form cha
              │
              └── Direct API Mode (Đã có recipeId):
                    └── Gọi lib/api/steps.ts (Axios) tương tác trực tiếp với Backend:
                          ├── createRecipeStep()
                          ├── updateRecipeStep()
                          ├── deleteRecipeStep()
                          └── reorderRecipeSteps()
```

### 5. Danh sách file mã nguồn quan trọng

| Đường dẫn file | Vai trò và chức năng chính |
| :--- | :--- |
| `frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx` | 672 dòng mã component chính điều khiển danh sách bước, form nhập liệu và xử lý dual-mode. |
| `frontend/culinary-blog-web/components/recipe-management/StepItem.tsx` | 170 dòng mã hiển thị từng thẻ bước nấu, các nút hành động (lên, xuống, sửa, xóa). |
| `frontend/culinary-blog-web/types/step.ts` | Khai báo các interface dữ liệu TypeScript: `RecipeStep`, `CreateStepInput`, `StepFormValues`. |
| `frontend/culinary-blog-web/lib/api/steps.ts` | 143 dòng mã thư viện gọi API Backend bằng Axios (`getRecipeSteps`, `createRecipeStep`, v.v.). |
| `frontend/culinary-blog-web/__tests__/StepEditor.test.tsx` | 381 dòng kiểm thử đơn vị giao diện với Jest và React Testing Library. |
| `frontend/culinary-blog-web/jest.config.mjs` | Cấu hình môi trường kiểm thử Jest cho Next.js TypeScript. |
| `docs/README_STEP_UI.md` | Tài liệu kỹ thuật chi tiết về thiết kế giao diện StepEditor. |
| `docs/VO_HUNG_MANH_STEP_UI_COMPLAN.md` | Kế hoạch triển khai và đặc tả các kịch bản tương tác người dùng Bữa 4. |

### 6. API, Database và Infrastructure liên quan
- **Framework Frontend:** Next.js 14+ (App Router), React 18, TypeScript, Tailwind CSS.
- **Thư viện kiểm thử:** Jest, `@testing-library/react`, `@testing-library/jest-dom`.
- **API Utilities:** Axios Client (`lib/api.ts`).
- **Endpoints kết nối (khi chạy Direct API Mode):**
  - `POST /api/v1/recipes/{id}/steps`
  - `PUT /api/v1/recipes/{id}/steps/{stepId}`
  - `PUT /api/v1/recipes/{id}/steps/reorder`
  - `DELETE /api/v1/recipes/{id}/steps/{stepId}`

### 7. Hướng dẫn chạy

#### Bước 1: Cài đặt dependencies (nếu chưa có)
```bash
cd frontend/culinary-blog-web
npm install
```

#### Bước 2: Chạy ứng dụng Next.js trên môi trường phát triển
```bash
npm run dev
```
Truy cập giao diện tại: `http://localhost:3000`.

### 8. Hướng dẫn kiểm thử

#### Kiểm thử giao diện tự động với Jest
```bash
cd frontend/culinary-blog-web
npm test -- StepEditor.test.tsx
```

#### Kiểm thử tương tác thủ công
1. Nhúng `<StepEditor initialSteps={sampleSteps} />` vào trang test.
2. Thử nhập bước nấu mới với tiêu đề > 200 ký tự -> quan sát thông báo lỗi.
3. Thử bấm nút di chuyển bước lên/xuống -> kiểm tra số thứ tự bước tự động hoán đổi.
4. Bấm nút xóa bước -> kiểm tra số thứ tự các bước còn lại tự động re-index.

### 9. Kết quả kiểm thử có bằng chứng
- **Kết quả Unit Test (Jest):** Toàn bộ test suite trong `StepEditor.test.tsx` đạt kết quả `PASS` (bao phủ các kịch bản: render danh sách ban đầu, thêm bước hợp lệ, validation lỗi, di chuyển thứ tự bước, xóa bước và gọi callback `onChange`).
- **Độ tin cậy giao diện:** Hoạt động ổn định trên các kích thước màn hình responsive nhờ Tailwind CSS.

### 10. Hạn chế và tồn đọng
- **Trạng thái thực tế:** Đây là **Nguyên mẫu giao diện (UI Prototype)** hoàn chỉnh về mặt logic tương tác component, validation và mock API.
- **Tồn đọng:** Ở mốc Task 4, giao diện sử dụng nút bấm (Move Up / Down) để hoán đổi vị trí bước; tính năng kéo thả mượt mà (Drag-and-Drop) bằng thư viện `@hello-pangea/dnd` và chế độ Cooking Guide tương tác toàn màn hình được chuyển tiếp hoàn thiện tại Task 6 và Task 8.

### 11. Liên kết tài liệu kỹ thuật chi tiết
- Chi tiết thiết kế giao diện StepEditor: [docs/README_STEP_UI.md](docs/README_STEP_UI.md)
- Kế hoạch triển khai và ma trận tương tác: [docs/VO_HUNG_MANH_STEP_UI_COMPLAN.md](docs/VO_HUNG_MANH_STEP_UI_COMPLAN.md)

---

# Phát triển ứng dụng Web nâng cao

## Nhóm 12

### Thành viên
1. Võ Hùng Mạnh - 2312687 - 2312687@dlu.edu.vn - Nhóm trưởng
2. Lê Thị Ánh Nhung - 2312709 - 2312709@dlu.edu.vn
3. Phạm Nguyễn Ngọc Phước - 2312718 - 2312718@dlu.edu.vn
4. Nguyễn Văn Quốc - 2312729 - 2312729@dlu.edu.vn
