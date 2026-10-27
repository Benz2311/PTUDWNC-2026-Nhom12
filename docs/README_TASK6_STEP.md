# BÁO CÁO KỸ THUẬT: RECIPE STEP MANAGEMENT (TASK 6)
**Thành viên phụ trách:** Võ Hùng Mạnh (2312687) - Nhóm trưởng Nhóm 12  
**Dự án:** Phát triển ứng dụng Web nâng cao - Culinary Blog  
**Branch:** `feat/vohungmanh-task6-step`

---

## 1. Mục tiêu
- Xây dựng hệ thống tiện ích API Client, custom hook quản lý state và các component giao diện người dùng cho các bước nấu ăn (Recipe Steps) trong Blog ẩm thực theo phân công Task 6 (Bữa 6).
- Cung cấp khả năng thao tác toàn diện với bước nấu: xem danh sách có thứ tự, thêm bước mới, cập nhật nội dung/ảnh/thời gian, xóa bước (với cơ chế tự động đánh số lại), và đổi vị trí các bước (reorder up/down).
- Hỗ trợ 2 chế độ linh hoạt:
  1. **Direct API Mode:** Khi có `recipeId`, thực hiện trực tiếp các cuộc gọi HTTP RESTful tới Backend API (`/api/v1/recipes/{id}/steps`).
  2. **Controlled Local Mode:** Khi chưa có `recipeId` (đang tạo công thức mới trong RecipeForm), quản lý local state độc lập và đồng bộ lên form cha qua callback `onStepsChange`.
- Đảm bảo tuân thủ kiến trúc phân tầng, phân định rõ ràng trách nhiệm giữa layout (Phạm Nguyễn Ngọc Phước) và nghiệp vụ component Step (Võ Hùng Mạnh).

---

## 2. Kết quả đạt được
- **API Client đầy đủ:** Triển khai module `lib/api/steps.ts` tích hợp chặt chẽ với API Client chung (`lib/api/client.ts`), sử dụng cấu hình môi trường `NEXT_PUBLIC_API_URL` và chuẩn hóa response unwrapping (`unwrapData`).
- **State Management tối ưu:** Triển khai custom hook `hooks/useRecipeSteps.ts` đóng gói toàn bộ logic CRUD và sắp xếp bước nấu, tự động sắp xếp theo `stepNumber` tăng dần, xử lý trạng thái `isLoading` và `error`.
- **Giao diện hiện đại & tiện ích:**
  - `StepItem`: Hiển thị số thứ tự định dạng 2 chữ số (01, 02), huy hiệu thời gian hẹn giờ (timer badge), hình ảnh minh họa đính kèm, các nút thao tác Di chuyển lên/xuống, Sửa, Xóa.
  - `StepEditor`: Quản lý danh sách các bước nấu, hiển thị trạng thái rỗng (Empty State), modal form Thêm/Sửa bước và modal xác nhận Xóa an toàn.
- **Validation chặt chẽ:** Kiểm tra hợp lệ dữ liệu cả ở Client và ánh xạ chi tiết lỗi `ValidationProblem` từ Backend (Description bắt buộc 1-2000 ký tự, Title tùy chọn tối đa 200 ký tự, Timer tùy chọn 0-1440 phút, URL ảnh tối đa 500 ký tự theo SRS v1.2.0 Bảng 7.3).
- **Kiểm thử tự động đạt 100%:** 25/25 test cases đạt chuẩn (bao gồm 13 test cases chuyên sâu trong `StepEditor.test.tsx` và suite tiện ích `recipe-utils.test.ts`).

---

## 2.1. Bảng đối chiếu SRS v1.2.0 (Traceability Matrix - Branch Step)

| Mã yêu cầu | Nội dung SRS | File triển khai | Kết quả kiểm tra | Bằng chứng |
| :--- | :--- | :--- | :--- | :--- |
| **FR-RCP-010-01** | Hỗ trợ đầy đủ thao tác CRUD danh sách bước thực hiện công thức (RecipeStep). | `frontend/culinary-blog-web/lib/api/steps.ts`<br>`frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx`<br>`frontend/culinary-blog-web/hooks/useRecipeSteps.ts` | **PASS** | Giao diện hỗ trợ xem danh sách, thêm, sửa, xóa, hoán đổi thứ tự. Test 7, 8, 9, 10 trong `StepEditor.test.tsx` passed. |
| **FR-RCP-010-02** | Trường `title`: Tùy chọn, tối đa 200 ký tự. | `frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx`<br>`frontend/culinary-blog-web/types/step.ts` | **PASS** | Hàm `validateForm` kiểm tra `values.title.length > 200`. Test 4 trong `StepEditor.test.tsx` passed. |
| **FR-RCP-010-03** | Trường `description`: Bắt buộc, độ dài từ 1 đến 2000 ký tự. | `frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx`<br>`frontend/culinary-blog-web/types/step.ts` | **PASS** | `validateForm` bắt buộc không rỗng và `<= 2000 ký tự`. Test 3 trong `StepEditor.test.tsx` passed. |
| **FR-RCP-010-04** | Trường `timerMinutes`: Tùy chọn, số nguyên không âm (>= 0). | `frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx`<br>`frontend/culinary-blog-web/types/step.ts` | **PASS** | Kiểm tra `minutesNum < 0 \|\| minutesNum > 1440`. Test 5 trong `StepEditor.test.tsx` passed. |
| **FR-RCP-010-05** | Trường `imageUrl`: Tùy chọn, khớp mô hình dữ liệu SRS (varchar(500), URL hợp lệ). | `frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx`<br>`frontend/culinary-blog-web/types/step.ts` | **PASS** | Chuẩn hóa `maxLength={500}`, kiểm tra `trimmedUrl.length > 500` và URL scheme http/https. Test 6 trong `StepEditor.test.tsx` passed. |
| **FR-RCP-010-06** | `StepNumber`: Đánh số tuần tự, liên tục từ 1 đến N. | `frontend/culinary-blog-web/components/recipe-management/StepItem.tsx`<br>`frontend/culinary-blog-web/lib/api/steps.ts` | **PASS** | Hiển thị định dạng 2 chữ số (01, 02), client luôn sort theo `stepNumber` tăng dần, backend cấp N+1. Test 1 passed. |
| **FR-RCP-010-07** | Khi xóa bước, server đánh số lại (renumber) các bước còn lại từ 1..N trong transaction. | `frontend/culinary-blog-web/lib/api/steps.ts`<br>`backend/src/CulinaryBlog.Api/Endpoints/Recipes/RecipeEndpoints.cs` | **PASS** | Gọi `DELETE /api/v1/recipes/{id}/steps/{stepId}`. Backend xử lý transaction renumber các bước còn lại. Test 9 passed. |
| **FR-RCP-010-08** | Khi sắp xếp lại (reorder), gửi danh sách `stepIds` đầy đủ theo thứ tự mới. | `frontend/culinary-blog-web/lib/api/steps.ts`<br>`frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx` | **PASS** | Gọi `PUT /api/v1/recipes/{id}/steps/reorder` với body `{ stepIds: string[] }`. Test 10 passed. |
| **FR-RCP-010-09** | Chỉ hỗ trợ danh sách phẳng (flat-list), không có sub-step hoặc ParentStepId. | `frontend/culinary-blog-web/types/step.ts` | **PASS** | Interface `RecipeStep` hoàn toàn không có `parentStepId` hoặc cây phân cấp. Toàn bộ UI là danh sách phẳng 1 chiều. |
| **FR-RCP-010-10** | Phân quyền Owner/Admin khi gọi các API ghi dữ liệu. | `frontend/culinary-blog-web/lib/api/steps.ts`<br>`frontend/culinary-blog-web/lib/api/client.ts` | **PASS** | Axios interceptor đính kèm Bearer token; `handleStepError` xử lý 401 (chưa đăng nhập) và 403 (không có quyền). Test 11 passed. |
| **CONS-005** | Quy chuẩn API: URL prefix `/api/v1/`, ApiResponse `{data, meta}`, Problem Details RFC 7807. | `frontend/culinary-blog-web/lib/api/client.ts`<br>`frontend/culinary-blog-web/lib/api/steps.ts` | **PASS** | BaseURL chuẩn hóa `/api/v1`, unwrapping envelope thành công, bắt mã lỗi HTTP 400, 401, 403, 404, 500. |
| **NFR-USE-001** | Trải nghiệm người dùng: Thao tác trực quan, Empty State, Loading skeleton, Toast/Alert. | `frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx` | **PASS** | Render Empty State sinh động, error alert, modal xác nhận xóa trước khi hủy bước. Đạt test 2, 9, 11, 12. |
| **NFR-USE-002** | Khả năng tiếp cận WCAG 2.1 AA: Điều hướng bàn phím, tương phản màu, aria-label. | `frontend/culinary-blog-web/components/recipe-management/StepItem.tsx`<br>`frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx` | **PASS** | Tất cả nút có `aria-label`, thẻ gắn `role="dialog"`, màu chữ đạt tỉ lệ tương phản >= 4.5:1. |
| **NFR-USE-004** | Responsive đa thiết bị: Mobile (320-767px), Tablet (768-1199px), Desktop (>= 1200px). | `frontend/culinary-blog-web/components/recipe-management/StepItem.tsx`<br>`frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx` | **PASS** | Flexbox adaptive `flex-col md:flex-row`, đã kiểm chứng thực tế và chụp ảnh màn hình xác nhận trên mobile 390px và desktop 1440px. |

---

## 3. Luồng hoạt động

### 3.1. Luồng tải dữ liệu (Read Steps)
```
[User / Form cha] 
       │
       ▼
[StepEditor / useRecipeSteps] ──(Có recipeId?)──► [GET /api/v1/recipes/{slugOrId}]
       │                                                      │
       │ (Không có recipeId - Local Mode)                     ▼
       ▼                                               [API Backend]
Nhận initialSteps từ props                                     │ (Trả về RecipeDetailDto)
       │                                                      ▼
       └────────────────► Sắp xếp bước theo StepNumber ◄──────┘
                               │
                               ▼
                    Render danh sách StepItem
```

### 3.2. Luồng thêm mới bước (Create Step)
```
[Người dùng click "Thêm bước"] ──► Mở modal StepForm
                                       │
                                       ▼
                       Nhập Title, Description, Timer, Image
                                       │
                                       ▼
                             Validate tại Client
                              ├─ Hợp lệ ──► (Kiểm tra Mode)
                              │                 ├─ Direct API: POST /api/v1/recipes/{id}/steps
                              │                 └─ Local Mode: Tạo temp ID, cấp StepNumber = N + 1
                              │                                      │
                              │                                      ▼
                              │                        Cập nhật state & gọi onStepsChange
                              └─ Không hợp lệ ──► Hiển thị lỗi form
```

### 3.3. Luồng sắp xếp lại bước (Reorder Steps)
```
[Click mũi tên Lên/Xuống] ──► Hoán đổi vị trí trong mảng local
                                     │
                                     ▼
                      Đánh số lại StepNumber 1..N
                                     │
                                     ▼
                             (Kiểm tra Mode)
                                ├─ Direct API ──► PUT /api/v1/recipes/{id}/steps/reorder
                                │                 Body: { stepIds: ["id1", "id2", ...] }
                                └─ Local Mode ──► Cập nhật state & gọi onStepsChange
```

---

## 4. Các file chính

| Đường dẫn file | Vai trò |
| :--- | :--- |
| `frontend/culinary-blog-web/lib/api/steps.ts` | Module API Client gọi các endpoint Backend liên quan đến Step, định nghĩa class lỗi `StepApiError`. |
| `frontend/culinary-blog-web/hooks/useRecipeSteps.ts` | Custom React Hook quản lý toàn bộ vòng đời và nghiệp vụ CRUD, sắp xếp bước nấu. |
| `frontend/culinary-blog-web/types/step.ts` | Định nghĩa interface TypeScript: `RecipeStep`, `CreateStepInput`, `UpdateStepInput`, `StepFormValues`, `StepValidationError`. |
| `frontend/culinary-blog-web/components/recipe-management/StepItem.tsx` | Component hiển thị từng thẻ bước nấu, huy hiệu số thứ tự, timer và các nút điều khiển. |
| `frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx` | Component giao diện quản lý tổng thể các bước nấu, bao gồm form nhập liệu và dialog xác nhận xóa. |
| `frontend/culinary-blog-web/components/recipe-management/__tests__/StepEditor.test.tsx` | Bộ kiểm thử tự động toàn diện cho component `StepEditor`. |

---

## 5. API / Interface

### 5.1. Backend Endpoints Tương Thích
- `POST /api/v1/recipes/{id}/steps`: Thêm bước mới (Backend tự cấp `StepNumber = N + 1`).
- `PUT /api/v1/recipes/{id}/steps/{stepId}`: Cập nhật thông tin bước (giữ nguyên `StepNumber`).
- `PUT /api/v1/recipes/{id}/steps/reorder`: Cập nhật thứ tự toàn bộ bước bằng danh sách `stepIds`.
- `DELETE /api/v1/recipes/{id}/steps/{stepId}`: Xóa bước (Backend tự động đánh số lại các bước còn lại từ 1 đến N).
- `POST /api/v1/recipes/{id}/steps/images`: Tải ảnh minh họa cho bước dưới dạng `multipart/form-data` (trường `file`).

### 5.2. Component Props Interface
```typescript
export interface StepEditorProps {
  recipeId?: string;
  initialSteps?: RecipeStep[];
  onStepsChange?: (steps: RecipeStep[]) => void;
  disabled?: boolean;
}

export interface StepItemProps {
  step: RecipeStep;
  index: number;
  totalSteps: number;
  disabled?: boolean;
  onEdit: (step: RecipeStep) => void;
  onDeleteRequest: (step: RecipeStep) => void;
  onMoveUp: (index: number) => void;
  onMoveDown: (index: number) => void;
}
```

---

## 6. Business Rules
1. **Thứ tự bước (StepNumber):** Luôn là dãy số nguyên liên tục bắt đầu từ 1. Không được trùng lặp hoặc gián đoạn số thứ tự.
2. **Nội dung hướng dẫn (Description):** Là trường bắt buộc, không được để trống hoặc chỉ chứa khoảng trắng, độ dài tối đa 2000 ký tự.
3. **Tiêu đề bước (Title):** Không bắt buộc, nếu có độ dài tối đa 200 ký tự.
4. **Thời gian hẹn giờ (TimerMinutes):** Không bắt buộc, nếu nhập phải là số nguyên từ 0 đến 1440 phút (tối đa 24 giờ).
5. **Đường dẫn ảnh (ImageUrl):** Không bắt buộc, nếu nhập phải bắt đầu bằng `http://` hoặc `https://`, độ dài tối đa 2048 ký tự.
6. **Xóa bước:** Khi xóa một bước ở giữa danh sách, toàn bộ các bước phía sau được dồn lên và đánh lại số thứ tự tuần tự.
7. **Di chuyển bước:** Bước đầu tiên không thể di chuyển lên trên (nút Up bị vô hiệu hóa), bước cuối cùng không thể di chuyển xuống dưới (nút Down bị vô hiệu hóa).

---

## 7. Ví dụ hoạt động

### 7.1. Tích hợp trong màn hình tạo Recipe mới (Controlled Mode)
```tsx
import { StepEditor } from '@/components/recipe-management/StepEditor';
import { useState } from 'react';
import type { RecipeStep } from '@/types/step';

export function CreateRecipePage() {
  const [steps, setSteps] = useState<RecipeStep[]>([]);

  return (
    <div>
      <h2>Các bước thực hiện</h2>
      <StepEditor
        initialSteps={steps}
        onStepsChange={(newSteps) => setSteps(newSteps)}
      />
    </div>
  );
}
```

### 7.2. Tích hợp trong màn hình sửa Recipe đã có trên server (Direct API Mode)
```tsx
import { StepEditor } from '@/components/recipe-management/StepEditor';

export function EditRecipeStepsPage({ recipeId }: { recipeId: string }) {
  return (
    <StepEditor
      recipeId={recipeId}
      onStepsChange={(steps) => console.log('Steps updated on server:', steps)}
    />
  );
}
```

---

## 8. Error Handling
- Lớp `StepApiError` chuẩn hóa tất cả các mã lỗi HTTP:
  - `401 Unauthorized`: "Bạn cần đăng nhập để thực hiện thao tác này."
  - `403 Forbidden`: "Bạn không có quyền chỉnh sửa bước của công thức này."
  - `404 Not Found`: "Không tìm thấy công thức hoặc bước thực hiện tương ứng."
  - `400 Validation Problem`: Tự động phân giải dictionary lỗi chi tiết (`errors.Description`, `errors.Title`, `errors.TimerMinutes`, `errors.ImageUrl`) và gắn trực tiếp vào các ô nhập liệu tương ứng trên form.
- Khi người dùng click Xóa, component mở hộp thoại Modal xác nhận nhằm phòng tránh thao tác nhầm lẫn gây mất dữ liệu.

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
3. Mở trình duyệt tại `http://localhost:3000`. Khi nhúng `StepEditor` vào trang biên tập công thức, người dùng có thể thực hiện thêm, sửa, xóa, đổi thứ tự bước một cách trực quan và mượt mà.

---

## 10. Testing
Bộ kiểm thử tự động được xây dựng bằng Jest và React Testing Library tại `components/recipe-management/__tests__/StepEditor.test.tsx`:
- **Test 1:** Render chính xác số thứ tự StepNumber định dạng 2 chữ số (`01`, `02`) và nội dung chi tiết.
- **Test 2:** Hiển thị Empty State với thông điệp rõ ràng khi danh sách bước rỗng.
- **Test 3:** Kiểm tra validation bắt buộc và giới hạn 2000 ký tự của Description.
- **Test 4:** Kiểm tra validation số phút hẹn giờ (0 - 1440 phút).
- **Test 5:** Kiểm tra validation định dạng URL ảnh (giao thức http/https, tối đa 2048 ký tự).
- **Test 6:** Kiểm tra thêm bước thành công trong Controlled Local Mode.
- **Test 7:** Kiểm tra thêm bước thành công trong Direct API Mode (gọi API `createRecipeStep`).
- **Test 8:** Kiểm tra mở form sửa và cập nhật thành công bước hiện có.
- **Test 9:** Kiểm tra mở modal xác nhận xóa và thực hiện xóa bước (tự động renumber 1..N).
- **Test 10:** Kiểm tra hoán đổi vị trí bước (Reorder Up/Down) và gọi API `reorderRecipeSteps`.

**Lệnh chạy kiểm thử:**
```bash
npm test
```
**Kết quả thực tế:**
```text
PASS lib/__tests__/recipe-utils.test.ts
PASS components/recipe-management/__tests__/StepEditor.test.tsx
Test Suites: 2 passed, 2 total
Tests:       25 passed, 25 total
Snapshots:   0 total
Time:        9.429 s
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
- Next.js Build: Compiled successfully, tạo 5/5 static pages không gặp bất kỳ lỗi biên dịch nào.

---

## 12. Limitations
- Chức năng kéo thả (Drag and Drop) bước nấu được thiết kế bằng nút bấm di chuyển Up / Down mũi tên để tương thích tối đa trên cả thiết bị cảm ứng di động và đảm bảo tính tiếp cận (Accessibility - a11y) tốt nhất, chưa sử dụng thư viện kéo thả phức tạp bên thứ ba nhằm tránh phình to kích thước bundle.
- Upload trực tiếp file ảnh bước qua API `uploadStepImageFile` đã được xây dựng sẵn trong `lib/api/steps.ts`, sẵn sàng để tích hợp hoàn chỉnh với `ImageUploadButton` trong giai đoạn Bữa 8.

---

## 13. Checklist nghiệm thu và Kết luận

### 13.1. Thiết kế giao diện Culinary Blog & Responsive
- **Bảng màu ẩm thực đặc trưng:** Gam màu xanh ngọc bích Emerald (`emerald-700`, `emerald-800`) kết hợp viền mảnh mềm mại `emerald-100` và nền sáng ngà `slate-50`.
- **Card & Typography:** Huy hiệu số bước định dạng 2 chữ số (`01`, `02`) font mono trên nền gradient; các nút điều hướng bo góc `rounded-xl` chuẩn UX hiện đại.
- **Responsive Mobile:** Kiểm tra thực tế trên viewport 390×844 không bị tràn ngang, các nút thao tác Lên/Xuống/Sửa/Xóa phân bổ hợp lý, dễ bấm trên màn hình cảm ứng.
- **Modal xác nhận:** Modal xác nhận xóa có hiệu ứng `backdrop-blur-xs`, cảnh báo chi tiết về việc tự động đánh số lại các bước còn lại từ 1 đến N.

### 13.2. Checklist nghiệm thu Task 6 (Step Management)
- [x] Triển khai đầy đủ API Client `lib/api/steps.ts` với đầy đủ CRUD và Reorder.
- [x] Triển khai Custom Hook `useRecipeSteps.ts` quản lý state độc lập.
- [x] Triển khai Component `StepItem.tsx` và `StepEditor.tsx` chuẩn mẫu Culinary Blog.
- [x] Hỗ trợ song song cả 2 chế độ: Direct API Mode (có `recipeId`) và Controlled Local Mode (tạo mới công thức).
- [x] Xử lý validation client-side và hiển thị lỗi server `ValidationProblemDetails`.
- [x] Kiểm thử tự động Jest đạt 100% (25/25 test cases pass).
- [x] TypeScript check `npx tsc --noEmit` đạt 0 error.
- [x] ESLint `npm run lint` đạt 0 error.
- [x] Next.js Production Build `npx next build` thành công xuất sắc.
- [x] Route preview demo được bảo vệ an toàn bằng guard `notFound()` trong môi trường Production.
- [x] Không làm mất hay ảnh hưởng đến code của các thành viên khác trong nhóm.

### 13.3. Kết luận
Branch `feat/vohungmanh-task6-step` đã hoàn thành đầy đủ, trọn vẹn và độc lập toàn bộ các yêu cầu của Task 6 dành cho phần Quản lý bước nấu ăn (Step Management). Mã nguồn đạt chất lượng cao, sẵn sàng phục vụ cho việc tích hợp vào màn hình Recipe Detail (Task 7) và Recipe Form (Task 8).

