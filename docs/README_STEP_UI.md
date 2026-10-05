# Module Frontend RecipeStep UI - StepEditor

> **Tác giả:** Võ Hùng Mạnh
> **Nhóm thực hiện:** Nhóm 12 - Dự án CulinaryBlog
> **Branch:** `feat/vohungmanh-step-ui`
> **Source Tree chính thức:** `frontend/culinary-blog-web/`

---

## 1. Giới thiệu tổng quan

Component `StepEditor` là module UI quản lý toàn bộ các bước thực hiện (Cooking Steps) của một công thức nấu ăn trong hệ thống CulinaryBlog. Module được thiết kế **độc lập**, **modular**, **reusable** và hỗ trợ linh hoạt cả 2 kịch bản vận hành thực tế:

1. **Direct API mode (Existing Recipe):** Khi công thức đã tồn tại trên hệ thống (đã có `recipeId`), component giao tiếp trực tiếp với Backend REST API qua các thao tác Thêm, Sửa, Xóa mềm (kèm tự động renumber) và Sắp xếp lại thứ tự (Reorder).
2. **Controlled mode (New Recipe):** Khi người dùng đang tạo mới công thức (chưa lưu vào CSDL, chưa có `recipeId`), component đóng vai trò quản lý state cục bộ mượt mà và đẩy danh sách các bước đã định dạng về form cha (`RecipeForm.tsx`) qua callback `onStepsChange`.

---

## 2. Kiến trúc & Cấu trúc thư mục

Toàn bộ mã nguồn của module được đặt trong cây thư mục chính thức của ứng dụng Next.js (`frontend/culinary-blog-web/`):

```
frontend/culinary-blog-web/
├── types/
│   └── step.ts                                    # Types & Interfaces chuẩn cho RecipeStep
├── lib/
│   ├── api.ts                                     # API abstraction helper dùng chung (apiFetch/apiJson)
│   └── api/
│       └── steps.ts                               # API Client tương tác RESTful endpoints Backend
├── components/
│   └── recipe-management/
│       ├── StepItem.tsx                           # Sub-component hiển thị từng bước độc lập
│       ├── StepEditor.tsx                         # Container component chính (quản lý 2 chế độ, validation, reorder)
│       └── __tests__/
│           └── StepEditor.test.tsx                # Test suite toàn diện 13 kịch bản kiểm thử
└── jest.config.mjs                                # Cấu hình Jest native ES Module
```

---

## 3. Các tính năng cốt lõi

### 3.1. Hiển thị & Đánh số thứ tự (StepNumber)
- Hiển thị số thứ tự bước dạng 2 chữ số (`01`, `02`, `03`...) với badge định danh trực quan.
- **Server là nguồn sự thật (Single Source of Truth):** Frontend tuyệt đối không tự tính toán hay gửi `stepNumber` khi thực hiện `Create` hoặc `Update`.

### 3.2. Sắp xếp lại thứ tự (Reorder)
- Nút Di chuyển Lên / Xuống (Up / Down) trực quan tại mỗi bước.
- Tự động vô hiệu hóa nút Lên ở bước đầu tiên và nút Xuống ở bước cuối cùng.
- Khi nhấn đổi vị trí: gửi mảng danh sách toàn bộ active Step IDs theo thứ tự mới lên endpoint:
  `PUT /api/v1/recipes/{recipeId}/steps/reorder` với payload `{ "stepIds": ["guid-1", "guid-2", ...] }`.

### 3.3. Xác thực dữ liệu (Validation Rules)
- **Title (Tiêu đề bước):** Tùy chọn (optional), tối đa 200 ký tự. Có bộ đếm ký tự trực quan.
- **Description (Nội dung hướng dẫn):** Bắt buộc (required), tối đa 2000 ký tự.
- **TimerMinutes (Thời gian hẹn giờ):** Tùy chọn, giá trị nguyên từ 0 đến 1440 phút (tương đương 24 giờ).
- **ImageUrl (Ảnh minh họa):** Tùy chọn, định dạng URL hợp lệ (`http://` hoặc `https://`), tối đa 2048 ký tự. Tự động hiển thị thumbnail preview.

### 3.4. Trạng thái giao diện (UI States)
- **Empty State:** Hiển thị hướng dẫn khi chưa có bước nào được thêm.
- **Loading State:** Spinner và overlay disable toàn bộ nút bấm trong khi đang gọi API.
- **Error State:** Error banner thông báo lỗi từ Backend (401 Unauthorized, 403 Forbidden, 404 NotFound, hoặc RFC 7807 ValidationProblemDetails).
- **Delete Confirmation Dialog:** Hộp thoại xác nhận trước khi thực hiện xóa bước, tránh xóa nhầm dữ liệu của đầu bếp.

---

## 4. Visual Design & UI Reference (Đồng bộ nhận diện Culinary Blog)

Module `StepEditor` được thiết kế tương thích 100% với phong cách giao diện của dự án Culinary Blog:
- **Tone màu chủ đạo:** Màu xanh lá đậm (`#166534` / `bg-green-800` / `hover:bg-green-900`) dùng cho toàn bộ nút chính (`+ Thêm bước`, `Lưu bước`). Màu xanh lá nhạt (`bg-green-50 text-green-800 border-green-200`) cho badge số thứ tự bước `01`, `02`.
- **Card style:** Nền trắng (`bg-white`), viền mảnh nhẹ (`border-gray-200`), bo góc vừa phải (`rounded-xl`), không shadow hoặc shadow siêu nhẹ (`shadow-xs`).
- **Nút hành động gọn gàng:** Nút Lên `↑`, Xuống `↓`, `Sửa` dạng outline/neutral (`bg-white border-gray-300 text-gray-700`). Nút `Xóa` dạng destructive tinh tế (`hover:text-red-600 hover:bg-red-50 hover:border-red-200`).
- **Responsive:** Co giãn linh hoạt từ Desktop (hàng ngang thoáng đãng) sang Mobile/Tablet (layout 1 cột, action bar chống tràn màn hình).
- **Thẩm mỹ:** Không dùng gradient sặc sỡ, không dùng glassmorphism, không dùng neon; giữ trọn vẻ đẹp sáng sủa, thanh lịch của một blog ẩm thực hiện đại.

---

## 5. Tích hợp với `RecipeForm.tsx` (Ranh giới TV3)

File [RecipeForm.tsx](file:///b:/PTUDWNC-2026-Nhom12-MANH/frontend/components/recipe-management/RecipeForm.tsx) thuộc quyền phụ trách của **Phạm Nguyễn Ngọc Phước (TV3)**.

Để tích hợp `StepEditor` vào `RecipeForm.tsx`, chỉ cần thay thế Section 03 (mockup hiện tại) bằng mã sau:

```tsx
import StepEditor from '@/components/recipe-management/StepEditor';

// Trong RecipeForm:
<div className="form-section">
  <StepEditor
    recipeId={recipeId}
    initialSteps={initialRecipe?.steps.map((s, idx) => ({
      id: `step-${idx}`,
      recipeId: recipeId ?? '',
      stepNumber: idx + 1,
      title: s.title,
      description: s.description,
      timerMinutes: null,
      imageUrl: null,
    }))}
    onStepsChange={(updatedSteps) => {
      setSteps(
        updatedSteps.map((s) => ({
          title: s.title ?? '',
          description: s.description,
        }))
      );
    }}
    disabled={saving}
  />
</div>
```

---

## 6. Hướng dẫn chạy Test và Build

### Chạy kiểm thử Unit Tests:
```powershell
cd frontend/culinary-blog-web
npm test
```
*Kết quả:* Chạy toàn bộ 25 unit tests (13 test cases của `StepEditor` và 12 test cases của utils), đạt 100% PASS.

### Chạy kiểm tra đóng gói Production Build:
```powershell
cd frontend/culinary-blog-web
$env:NODE_OPTIONS="--max-old-space-size=4096"; npx next build --no-lint
```
*Kết quả:* Compile thành công 100%, type checking hoàn hảo, không có lỗi runtime.
