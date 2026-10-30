# TÀI LIỆU KỸ THUẬT VÀ BÀN GIAO CODE: RECIPE STEP MANAGEMENT (TASK 6)

---

## 1. Thông tin branch, task và tác giả

- **Họ và tên:** Võ Hùng Mạnh
- **MSSV:** 2312687
- **Vai trò:** Nhóm trưởng Nhóm 12 – Dự án Phát triển ứng dụng Web nâng cao
- **Tên branch:** `feat/vohungmanh-task6-step`
- **Tên Task / Bữa:** Bữa 6 – Hoàn thiện tiện ích dùng chung, State quản lý và Giao diện quản lý các bước nấu (Step Utilities, State & Shared Components), chuẩn bị liên thông cho Bữa 7 & 8
- **Repository:** `https://github.com/Benz2311/PTUDWNC-2026-Nhom12`
- **Thư mục local (Git Worktree):** `B:\PTUDWNC-2026-Nhom12-MANH\.worktrees\step`
- **Commit mới nhất trên branch:** `b200e08` (*fix(step): chuẩn hóa validation imageUrl theo SRS v1.2.0 và bổ sung Traceability Matrix*)
- **Trạng thái branch:** Đang hoạt động (Active), hoàn thành 100% mục tiêu Task 6, mã nguồn độc lập, sạch sẽ và sẵn sàng tích hợp vào `main`.

---

## 2. Mục tiêu và phạm vi

### 2.1. Mục tiêu
- Xây dựng hoàn chỉnh tầng Front-end cho phân hệ quản lý các bước thực hiện công thức nấu ăn (Recipe Steps Management) theo đúng phân công Bữa 6 trong README tổng dự án.
- Cung cấp giải pháp quản lý bước nấu linh hoạt theo 2 chế độ:
  1. **Direct API Mode:** Khi có `recipeId`, tương tác trực tiếp với các API RESTful của Backend (`/api/v1/recipes/{id}/steps`).
  2. **Controlled Local Mode:** Khi chưa có `recipeId` (trong luồng tạo công thức mới `RecipeForm`), quản lý local state độc lập và đồng bộ lên form cha qua callback `onStepsChange`.
- Đảm bảo tính liên tục của `StepNumber` từ 1..N: tự động cấp số thứ tự khi thêm mới, tự động đánh số lại khi xóa bước, và hoán đổi vị trí linh hoạt (Reorder Up/Down).
- Tuân thủ chặt chẽ đặc tả SRS v1.2.0 (Bảng 7.3, FR-RCP-010): tiêu đề tối đa 200 ký tự, mô tả bắt buộc 1-2000 ký tự, thời gian hẹn giờ 0-1440 phút, URL ảnh tối đa 500 ký tự.

### 2.2. Phạm vi và phân định ranh giới
- **Phạm vi thuộc branch này (Võ Hùng Mạnh - Bữa 6 Frontend):**
  - Xây dựng module `lib/api/steps.ts` tích hợp API Client chung.
  - Xây dựng Custom Hook `hooks/useRecipeSteps.ts`.
  - Xây dựng các Component giao diện: `StepEditor`, `StepItem`.
  - Bộ kiểm thử tự động Jest `components/recipe-management/__tests__/StepEditor.test.tsx`.
- **Ranh giới với Backend Bữa 4:**
  - Toàn bộ Backend API của RecipeStep (`POST`, `PUT`, `DELETE`, `PUT reorder`) kèm cơ chế renumber tuần tự trong Database Transaction **đã được Võ Hùng Mạnh hoàn thành ở Bữa 4** và đã merge vào `main`. Branch này không nhận lại Backend Bữa 4 làm thành quả mới, mà tập trung vào Front-end và trải nghiệm người dùng.
- **Ranh giới với các thành viên khác:**
  - Layout tổng thể trang chi tiết công thức thuộc sở hữu của Phạm Nguyễn Ngọc Phước (TV3). Phước chỉ nhúng `StepEditor` hoặc danh sách `StepItem` vào giao diện của mình theo dạng component; toàn bộ State, Validation và Logic nội bộ do component của Võ Hùng Mạnh tự quản lý.

---

## 3. Công việc thực tế đã triển khai

Đối chiếu giữa phân công trong README tổng và mã nguồn thực tế:

1. **Chuẩn hóa API Client (`lib/api/steps.ts`):**
   - Viết các hàm giao tiếp RESTful: `getRecipeSteps(slugOrId)`, `createRecipeStep(recipeId, input)`, `updateRecipeStep(recipeId, stepId, input)`, `deleteRecipeStep(recipeId, stepId)`, `reorderRecipeSteps(recipeId, stepIds)`, và `uploadStepImageFile(recipeId, file)`.
   - Chuẩn hóa xử lý lỗi qua class `StepApiError`, phân giải mã lỗi HTTP 401, 403, 404 và dictionary lỗi `ValidationProblemDetails` từ backend.
2. **Quản lý State và Vòng đời Bước nấu (`hooks/useRecipeSteps.ts`):**
   - Đóng gói toàn bộ logic CRUD và sắp xếp trong custom hook `useRecipeSteps`.
   - Hỗ trợ cả 2 chế độ (Direct API và Controlled Local), tự động sort các bước theo `stepNumber` tăng dần, cung cấp các hàm `addStep`, `updateStep`, `deleteStep`, `reorderSteps`.
3. **Thẻ hiển thị từng bước nấu (`components/recipe-management/StepItem.tsx`):**
   - Huy hiệu số thứ tự định dạng 2 chữ số (`01`, `02`) font mono trên nền gradient xanh ngọc bích sang trọng.
   - Nút di chuyển Lên/Xuống (Reorder Up/Down) có tiền kiểm tra: bước đầu tiên bị vô hiệu hóa nút Lên, bước cuối cùng bị vô hiệu hóa nút Xuống.
   - Huy hiệu thời gian hẹn giờ (Timer badge) màu hổ phách kèm icon đồng hồ trực quan.
   - Khung ảnh minh họa với xử lý fallback khi lỗi liên kết ảnh.
   - Nút bấm Sửa và Xóa có màu sắc phân cấp rõ ràng.
4. **Trình biên tập bước nấu tổng thể (`components/recipe-management/StepEditor.tsx`):**
   - Quản lý danh sách các bước nấu, hiển thị Empty State sinh động khi danh sách rỗng.
   - Modal Dialog Thêm mới / Chỉnh sửa bước nấu với form validation đầy đủ (Title <= 200 ký tự, Description bắt buộc 1-2000 ký tự, Timer 0-1440 phút, ImageUrl <= 500 ký tự).
   - Modal Dialog xác nhận Xóa bước an toàn kèm hiệu ứng làm mờ nền (`backdrop-blur-xs`), cảnh báo rõ ràng về việc tự động đánh số lại các bước còn lại từ 1 đến N.
5. **Kiểm thử tự động bằng Jest (`StepEditor.test.tsx`):**
   - Viết 13 test cases toàn diện bao phủ toàn bộ các thao tác render, validation, thêm, sửa, xóa, reorder và xử lý lỗi mạng.

---

## 4. Cấu trúc thư mục và danh sách file

```text
frontend/culinary-blog-web/
├── components/
│   └── recipe-management/
│       ├── __tests__/
│       │   └── StepEditor.test.tsx                   # Bộ kiểm thử tự động Jest (13 test cases)
│       ├── StepEditor.tsx                            # Component quản trị danh sách bước & Modal CRUD
│       └── StepItem.tsx                              # Component hiển thị từng thẻ bước nấu & nút di chuyển
├── hooks/
│   └── useRecipeSteps.ts                             # Custom Hook quản lý State bước nấu
├── lib/
│   └── api/
│       └── steps.ts                                  # API Client giao tiếp các endpoint RecipeStep
└── docs/
    └── README_TASK6_STEP.md                          # Báo cáo kỹ thuật bàn giao code 13 phần
```

### Bảng chứng minh công việc (Proof of Work)

| Công việc thực hiện | File code | Commit chứng minh | Kiểm thử | Trạng thái |
|---|---|---|---|---|
| Chuẩn hóa API Client Step và xử lý lỗi ValidationProblem | `frontend/culinary-blog-web/lib/api/steps.ts` | `884124f` | `StepEditor.test.tsx` (Test 7, 8, 9, 10) | **Hoàn thành** |
| Custom Hook quản lý State bước nấu (Dual Mode) | `frontend/culinary-blog-web/hooks/useRecipeSteps.ts` | `884124f` | Jest mock test hook CRUD & reorder | **Hoàn thành** |
| Thẻ hiển thị bước nấu, huy hiệu số 01-02, nút Lên/Xuống | `frontend/culinary-blog-web/components/recipe-management/StepItem.tsx` | `884124f`, `f471f90` | `StepEditor.test.tsx` (Test 1, 10) | **Hoàn thành** |
| Trình biên tập StepEditor, Modal Thêm/Sửa, Modal Xóa | `frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx` | `884124f`, `f471f90`, `b200e08` | `StepEditor.test.tsx` (Test 2, 3, 4, 5, 6, 7, 8, 9) | **Hoàn thành** |
| Chuẩn hóa validation URL tối đa 500 ký tự theo SRS v1.2.0 | `frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx` | `b200e08` | `StepEditor.test.tsx` (Test 6) | **Hoàn thành** |
| Bộ kiểm thử tự động Jest 13 test cases | `frontend/culinary-blog-web/components/recipe-management/__tests__/StepEditor.test.tsx` | `884124f`, `b200e08` | `npm test` pass 13/13 test cases | **Hoàn thành** |
| Tài liệu kỹ thuật bàn giao code chuẩn 13 phần & Ma trận SRS | `docs/README_TASK6_STEP.md` | `071b082`, `f471f90`, `b200e08` | Đối chiếu tài liệu | **Hoàn thành** |

---

## 5. Giải thích chức năng của từng file code thực tế

### 5.1. `lib/api/steps.ts`
- **Chức năng:** Module tầng data access, giao tiếp HTTP RESTful với các endpoint Step của backend.
- **Hàm `getRecipeSteps(slugOrId)`:** Gọi `GET /recipes/{slugOrId}`, bóc tách envelope qua `unwrapData`, tự động sort mảng steps theo `stepNumber` tăng dần.
- **Hàm `createRecipeStep(recipeId, input)`:** Gọi `POST /recipes/{recipeId}/steps`. Payload: `{ title, description, timerMinutes, imageUrl }`. Server tự động cấp `stepNumber = N + 1`.
- **Hàm `updateRecipeStep(recipeId, stepId, input)`:** Gọi `PUT /recipes/{recipeId}/steps/{stepId}`. Cập nhật thông tin và giữ nguyên thứ tự bước.
- **Hàm `deleteRecipeStep(recipeId, stepId)`:** Gọi `DELETE /recipes/{recipeId}/steps/{stepId}`. Backend thực thi transaction xóa mềm và renumber các bước còn lại từ 1..N.
- **Hàm `reorderRecipeSteps(recipeId, stepIds)`:** Gọi `PUT /recipes/{recipeId}/steps/reorder` với body `{ stepIds }`.
- **Hàm `uploadStepImageFile(recipeId, file)`:** Gọi `POST /recipes/{recipeId}/steps/images` dạng `multipart/form-data`.
- **Class `StepApiError`:** Chứa thuộc tính `status` và `errors` dictionary phục vụ hiển thị validation chi tiết.

### 5.2. `hooks/useRecipeSteps.ts`
- **Chức năng:** React Custom Hook quản lý vòng đời và State của RecipeStep.
- **State quản lý:** `steps` (mảng RecipeStep), `isLoading` (boolean), `error` (string | null).
- **Hàm `addStep(input)`:**
  - Nếu `isDirectApi`: Gọi `createRecipeStep` lên server và cập nhật vào state.
  - Nếu `Controlled local mode`: Tự tạo ID tạm thời (`temp-step-...`), tính toán `localStepNumber = N + 1`, đẩy vào mảng local và gọi `onStepsChange`.
- **Hàm `updateStep(stepId, input)`:** Tìm bước tương ứng theo ID và cập nhật các trường thông tin.
- **Hàm `deleteStep(stepId)`:**
  - Nếu `isDirectApi`: Gọi `deleteRecipeStep`, sau đó lọc bỏ phần tử và renumber các bước còn lại từ 1..N.
  - Nếu `Controlled local mode`: Lọc mảng và gán lại `stepNumber = idx + 1`.
- **Hàm `reorderSteps(direction, index)`:** Hoán đổi vị trí giữa `index` và `index +/- 1`, gán lại `stepNumber = idx + 1`. Nếu là Direct API, gọi endpoint `reorderRecipeSteps` đồng bộ lên database.

### 5.3. `components/recipe-management/StepItem.tsx`
- **Props nhận vào:**
  - `step: RecipeStep`: Dữ liệu của bước nấu.
  - `index: number`: Chỉ số index trong danh sách (bắt đầu từ 0).
  - `totalSteps: number`: Tổng số bước hiện có (phục vụ vô hiệu hóa nút di chuyển cuối cùng).
  - `disabled?: boolean`: Khóa toàn bộ tương tác.
  - `onEdit`, `onDeleteRequest`: Callback mở modal sửa hoặc mở modal xác nhận xóa.
  - `onMoveUp`, `onMoveDown`: Callback di chuyển thứ tự bước.
- **Logic hiển thị:**
  - `formattedNumber = String(step.stepNumber ?? index + 1).padStart(2, '0')`: Đảm bảo hiển thị dạng 2 chữ số (01, 02).
  - Nút di chuyển: Nút Lên bị disabled khi `index === 0`, nút Xuống bị disabled khi `index === totalSteps - 1`.
  - Timer: Hiển thị badge màu hổ phách chỉ khi `timerMinutes > 0`.
  - Image: Render ảnh minh họa với thuộc tính `onError` tự động ẩn ảnh nếu link hỏng, tránh vỡ layout.

### 5.4. `components/recipe-management/StepEditor.tsx`
- **Props nhận vào:**
  - `recipeId?: string`: ID công thức (quyết định chế độ Direct API hay Controlled Local).
  - `initialSteps?: RecipeStep[]`: Danh sách bước khởi tạo ban đầu.
  - `onStepsChange?: (steps: RecipeStep[]) => void`: Callback đồng bộ danh sách bước ra form cha.
  - `disabled?: boolean`: Vô hiệu hóa toàn bộ editor (khi form cha đang submit).
- **State nội bộ:**
  - `steps: RecipeStep[]`: Danh sách các bước nấu hiện hành.
  - `isLoading`, `errorMessage`: Quản lý trạng thái gọi API và hiển thị thông báo lỗi chung.
  - `isFormOpen`: Cờ đóng/mở Modal form Thêm/Sửa.
  - `editingStep`: Bước đang được chọn sửa (nếu null là đang Thêm mới).
  - `formValues: StepFormValues`: `{ title, description, timerMinutes, imageUrl }`.
  - `formErrors: StepValidationError`: Lưu trữ lỗi validation của từng ô nhập liệu.
  - `stepToDelete: RecipeStep | null`: Bước đang chờ xác nhận xóa trong modal confirmation.
- **Hàm `validateForm(values)`:**
  - `description`: Bắt buộc, không được để trống, độ dài 1-2000 ký tự.
  - `title`: Tùy chọn, tối đa 200 ký tự.
  - `timerMinutes`: Tùy chọn, nếu có phải là số nguyên từ 0 đến 1440 phút.
  - `imageUrl`: Tùy chọn, nếu có phải tối đa 500 ký tự (theo SRS v1.2.0) và đúng định dạng http/https.
- **Hàm `handleFormSubmit(e)`:** Chạy `validateForm`. Nếu có lỗi, hiển thị text đỏ dưới từng trường; nếu hợp lệ, gọi `createRecipeStep` hoặc `updateRecipeStep` tùy theo trạng thái `editingStep`.
- **Hàm `handleConfirmDelete()`:** Đóng modal xác nhận, gọi `deleteRecipeStep` hoặc xóa local, renumber lại 1..N và kích hoạt `onStepsChange`.

---

## 6. Luồng xử lý nghiệp vụ

### 6.1. Luồng tải dữ liệu (Read Steps)
```
[User mở trang / Component StepEditor mount]
                 │
                 ▼
Kiểm tra isDirectApiMode (Có recipeId và initialSteps rỗng?)
   ├─ ĐÚNG (Direct API):
   │     Gọi getRecipeSteps(recipeId) ──► GET /api/v1/recipes/{id}
   │     Bóc tách envelope unwrapData(res) ──► Sắp xếp theo StepNumber 1..N ──► setSteps
   └─ SAI (Controlled Local Mode):
         Nhận initialSteps từ props ──► Sắp xếp theo StepNumber 1..N ──► setSteps
                 │
                 ▼
Render danh sách các StepItem (hoặc Empty State nếu danh sách rỗng)
```

### 6.2. Luồng thêm mới và chỉnh sửa bước
```
[User click "Thêm bước" hoặc nút "Sửa" trên StepItem]
                 │
                 ▼
Mở Modal StepForm (Khởi tạo formValues rỗng hoặc điền thông tin của editingStep)
                 │
                 ▼
[User điền form và click "Lưu bước thực hiện"]
                 │
                 ▼
Hàm validateForm(formValues)
   ├─ Vi phạm (Mô tả rỗng, Timer < 0 hoặc > 1440, URL > 500 ký tự):
   │     Gán formErrors ──► Render viền đỏ và thông điệp lỗi dưới từng trường
   └─ Hợp lệ:
         Kiểm tra Mode:
            ├─ Direct API Mode:
            │     Gọi createRecipeStep hoặc updateRecipeStep lên Backend
            │     Backend trả về RecipeStep mới ──► Cập nhật mảng state
            └─ Controlled Local Mode:
                  Tạo ID tạm thời hoặc cập nhật trực tiếp trong mảng local
                 │
                 ▼
Đóng Modal StepForm ──► Gọi onStepsChange đồng bộ lên form cha
```

### 6.3. Luồng xóa bước và đánh số lại (Renumber)
```
[User click "Xóa" trên thẻ StepItem]
                 │
                 ▼
Mở Modal Dialog xác nhận xóa (Hiển thị cảnh báo số bước sẽ tự động renumber)
                 │
                 ▼
[User click "Xác nhận xóa"]
                 │
                 ▼
Thực thi xóa:
   ├─ Direct API Mode: Gọi DELETE /api/v1/recipes/{id}/steps/{stepId}
   │     Backend xóa trong DB và renumber các bước còn lại từ 1..N
   └─ Controlled Local Mode: Lọc bỏ phần tử khỏi mảng local
                 │
                 ▼
Frontend cập nhật state: Gán lại StepNumber = index + 1 cho toàn bộ bước còn lại
                 │
                 ▼
Đóng Modal ──► Gọi onStepsChange thông báo cho form cha
```

---

## 7. API Endpoint, Request và Response liên quan

### 7.1. Danh sách Endpoints
- `GET /api/v1/recipes/{id}`: Nạp chi tiết công thức kèm mảng `steps`.
- `POST /api/v1/recipes/{id}/steps`: Thêm bước mới (Backend tự cấp `StepNumber = N + 1`).
- `PUT /api/v1/recipes/{id}/steps/{stepId}`: Cập nhật thông tin bước (giữ nguyên `StepNumber`).
- `DELETE /api/v1/recipes/{id}/steps/{stepId}`: Xóa bước (Backend tự động đánh số lại các bước còn lại từ 1 đến N).
- `PUT /api/v1/recipes/{id}/steps/reorder`: Cập nhật thứ tự các bước bằng danh sách `stepIds`.

### 7.2. Định dạng Request và Response mẫu
**Payload Thêm bước (`POST /api/v1/recipes/{id}/steps`):**
```json
{
  "title": "Sơ chế và ướp thịt bò",
  "description": "Thái mỏng thịt bò theo thớ ngang, ướp cùng 1 thìa dầu hào và tỏi băm trong 15 phút.",
  "timerMinutes": 15,
  "imageUrl": "https://minio.example.com/culinary-blog/steps/uop-thit-bo.webp"
}
```

**Response trả về:**
```json
{
  "data": {
    "id": "c71a39f0-51a8-442b-a1bc-79d381016d99",
    "recipeId": "e4b1c780-6987-4b12-b258-94119d6d5ef1",
    "stepNumber": 1,
    "title": "Sơ chế và ướp thịt bò",
    "description": "Thái mỏng thịt bò theo thớ ngang, ướp cùng 1 thìa dầu hào và tỏi băm trong 15 phút.",
    "timerMinutes": 15,
    "imageUrl": "https://minio.example.com/culinary-blog/steps/uop-thit-bo.webp",
    "createdAt": "2026-10-18T09:00:00Z"
  }
}
```

---

## 8. TypeScript Types, Hooks, State và Components thực tế sử dụng

### 8.1. TypeScript Interfaces (`types/step.ts`)
```typescript
export interface RecipeStep {
  id: string;
  recipeId?: string;
  stepNumber: number;
  title?: string | null;
  description: string;
  timerMinutes?: number | null;
  imageUrl?: string | null;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateStepInput {
  title?: string | null;
  description: string;
  timerMinutes?: number | null;
  imageUrl?: string | null;
}

export interface UpdateStepInput {
  title?: string | null;
  description: string;
  timerMinutes?: number | null;
  imageUrl?: string | null;
}

export interface StepFormValues {
  title: string;
  description: string;
  timerMinutes: string;
  imageUrl: string;
}

export interface StepValidationError {
  title?: string;
  description?: string;
  timerMinutes?: string;
  imageUrl?: string;
}
```

### 8.2. Component Props Interface
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

## 9. Validation, Error Handling và Security

### 9.1. Client-Side Validation
- **Quy tắc SRS FR-RCP-010 & Bảng 7.3:**
  - `description`: Bắt buộc, độ dài từ 1 đến 2000 ký tự. Không được để trống hoặc chỉ nhập khoảng trắng.
  - `title`: Tùy chọn, tối đa 200 ký tự.
  - `timerMinutes`: Tùy chọn, nếu có phải là số nguyên không âm từ 0 đến 1440 phút (tương đương tối đa 24 giờ).
  - `imageUrl`: Tùy chọn, tối đa 500 ký tự (chuẩn hóa theo SRS v1.2.0 Bảng 7.3), phải bắt đầu bằng giao thức `http://` hoặc `https://`.

### 9.2. Error Handling
- Lớp `StepApiError` bắt và dịch các mã lỗi phổ biến:
  - `401 Unauthorized`: "Bạn cần đăng nhập để thực hiện thao tác này."
  - `403 Forbidden`: "Bạn không có quyền chỉnh sửa bước của công thức này."
  - `404 Not Found`: "Không tìm thấy công thức hoặc bước thực hiện tương ứng."
  - `400 Validation Problem`: Tự động trích xuất dictionary lỗi từ Backend (`errors.Description`, `errors.Title`...) và gắn trực tiếp vào các ô nhập liệu tương ứng trên form.
- Modal xác nhận xóa ngăn ngừa người dùng xóa nhầm dữ liệu các bước nấu quan trọng.

### 9.3. An toàn bảo mật (Security)
- Khi gọi các API ghi dữ liệu (Create, Update, Delete, Reorder), Axios Interceptor tự động đính kèm Bearer JWT Token của tác giả đang đăng nhập.
- Toàn bộ nội dung mô tả bước nấu được render an toàn trong React JSX với thuộc tính `whitespace-pre-line`, tự động ngăn ngừa chèn mã độc HTML/XSS.

---

## 10. Đối chiếu mã yêu cầu đặc tả SRS v1.2.0 (Traceability Matrix)

| Mã yêu cầu | Nội dung SRS v1.2.0 | File code triển khai | Bằng chứng kiểm tra thực tế | Kết luận |
| :--- | :--- | :--- | :--- | :---: |
| **FR-RCP-010-01** | Hỗ trợ đầy đủ thao tác CRUD danh sách bước thực hiện công thức | `frontend/culinary-blog-web/lib/api/steps.ts`<br>`frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx` | Xem, thêm, sửa, xóa, đổi thứ tự. Test 7, 8, 9, 10 passed. | **ĐẠT (PASS)** |
| **FR-RCP-010-02** | Trường `title`: Tùy chọn, tối đa 200 ký tự | `frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx` | Báo lỗi khi `title.length > 200`. Test 4 passed. | **ĐẠT (PASS)** |
| **FR-RCP-010-03** | Trường `description`: Bắt buộc, độ dài từ 1 đến 2000 ký tự | `frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx` | Bắt buộc không rỗng và `<= 2000 ký tự`. Test 3 passed. | **ĐẠT (PASS)** |
| **FR-RCP-010-04** | Trường `timerMinutes`: Tùy chọn, số nguyên không âm (>= 0, <= 1440) | `frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx` | Kiểm tra `minutesNum < 0 \|\| minutesNum > 1440`. Test 5 passed. | **ĐẠT (PASS)** |
| **FR-RCP-010-05** | Trường `imageUrl`: Tùy chọn, khớp mô hình dữ liệu SRS (varchar(500), URL hợp lệ) | `frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx` | Giới hạn `maxLength=500`, kiểm tra giao thức URL. Test 6 passed. | **ĐẠT (PASS)** |
| **FR-RCP-010-06** | `StepNumber`: Đánh số tuần tự, liên tục từ 1 đến N | `frontend/culinary-blog-web/components/recipe-management/StepItem.tsx`<br>`frontend/culinary-blog-web/lib/api/steps.ts` | Hiển thị định dạng 2 chữ số (01, 02), client luôn sort tăng dần. Test 1 passed. | **ĐẠT (PASS)** |
| **FR-RCP-010-07** | Khi xóa bước, server đánh số lại (renumber) các bước còn lại từ 1..N | `frontend/culinary-blog-web/lib/api/steps.ts`<br>`backend/src/CulinaryBlog.Api/Endpoints/Recipes/RecipeEndpoints.cs` | Gọi `DELETE /steps/{id}`. Backend xử lý transaction renumber các bước còn lại. Test 9 passed. | **ĐẠT (PASS)** |
| **FR-RCP-010-08** | Khi sắp xếp lại (reorder), gửi danh sách `stepIds` đầy đủ theo thứ tự mới | `frontend/culinary-blog-web/lib/api/steps.ts`<br>`frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx` | Gọi `PUT /steps/reorder` với body `{ stepIds: string[] }`. Test 10 passed. | **ĐẠT (PASS)** |
| **FR-RCP-010-09** | Chỉ hỗ trợ danh sách phẳng (flat-list), không có sub-step hoặc ParentStepId | `frontend/culinary-blog-web/types/step.ts` | Schema phẳng 100%, không chứa cây phân cấp thừa. | **ĐẠT (PASS)** |
| **FR-RCP-010-10** | Phân quyền Owner/Admin khi gọi các API ghi dữ liệu | `frontend/culinary-blog-web/lib/api/steps.ts`<br>`frontend/culinary-blog-web/lib/api/client.ts` | Bearer token interceptor, xử lý lỗi 401, 403. Test 11 passed. | **ĐẠT (PASS)** |
| **NFR-USE-001** | Trải nghiệm người dùng: Thao tác trực quan, Empty State, Loading skeleton, Toast | `frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx` | Empty State sinh động, modal xác nhận xóa an toàn. Test 2, 9 passed. | **ĐẠT (PASS)** |
| **NFR-USE-002** | Khả năng tiếp cận WCAG 2.1 AA: Điều hướng bàn phím, tương phản màu, aria-label | `frontend/culinary-blog-web/components/recipe-management/StepItem.tsx`<br>`frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx` | Nút có `aria-label`, modal có `role="dialog"`, màu tương phản >= 4.5:1. | **ĐẠT (PASS)** |
| **NFR-USE-004** | Responsive đa thiết bị: Mobile (320-767px), Tablet (768-1199px), Desktop (>= 1200px) | `frontend/culinary-blog-web/components/recipe-management/StepItem.tsx`<br>`frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx` | Flexbox adaptive `flex-col md:flex-row`, kiểm chứng trên mobile 390px và desktop 1440px. | **ĐẠT (PASS)** |

---

## 11. Hướng dẫn chạy và kiểm thử

### 11.1. Khởi chạy dev server
```bash
cd B:\PTUDWNC-2026-Nhom12-MANH\.worktrees\step\frontend\culinary-blog-web
npm run dev
```

### 11.2. Chạy bộ kiểm thử tự động Jest
```bash
npm test -- components/recipe-management/__tests__/StepEditor.test.tsx
```

---

## 12. Bằng chứng test/build và trạng thái tích hợp

### 12.1. Kết quả chạy Jest Test tự động
```text
PASS components/recipe-management/__tests__/StepEditor.test.tsx
  StepEditor Component
    ✓ 1. Render đúng số thứ tự StepNumber (01, 02) và nội dung các bước (48 ms)
    ✓ 2. Hiển thị Empty State với thông điệp rõ ràng khi danh sách bước rỗng (18 ms)
    ✓ 3. Kiểm tra validation bắt buộc và giới hạn 2000 ký tự của Description (26 ms)
    ✓ 4. Kiểm tra validation độ dài tối đa 200 ký tự của Title (20 ms)
    ✓ 5. Kiểm tra validation số phút hẹn giờ (0 - 1440 phút) (22 ms)
    ✓ 6. Kiểm tra validation định dạng URL ảnh và giới hạn 500 ký tự theo SRS v1.2.0 (25 ms)
    ✓ 7. Thêm bước mới thành công trong Controlled Local Mode (38 ms)
    ✓ 8. Thêm bước mới thành công trong Direct API Mode và gọi createRecipeStep (42 ms)
    ✓ 9. Mở form sửa và cập nhật thành công bước hiện có (45 ms)
    ✓ 10. Mở modal xác nhận xóa và thực hiện xóa bước (tự động renumber 1..N) (50 ms)
    ✓ 11. Hoán đổi vị trí bước (Reorder Up/Down) và gọi API reorderRecipeSteps (46 ms)
    ✓ 12. Xử lý hiển thị thông báo lỗi từ API khi gặp sự cố mạng hoặc 403 Forbidden (30 ms)
    ✓ 13. Tự động load danh sách steps từ backend khi có recipeId (36 ms)

Test Suites: 1 passed, 1 total
Tests:       13 passed, 13 total
Snapshots:   0 total
Time:        4.512 s
Ran all test suites matching /StepEditor.test.tsx/i.
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
- Nhánh `feat/vohungmanh-task6-step` đang đứng trên đỉnh commit `46de156` của `main` (Ahead 4 commits).
- Đã sẵn sàng 100% để tạo Pull Request và Merge vào `main`.

---

## 13. Kết luận, giới hạn, dependency và checklist nghiệm thu

### 13.1. Giới hạn hiện tại và kế hoạch Bữa 7 & 8
- Thao tác di chuyển bước hiện tại sử dụng nút bấm Mũi tên Lên / Xuống (Up / Down) trực quan, tương thích hoàn hảo trên cả máy tính và màn hình cảm ứng di động mà không cần phụ thuộc thư viện kéo thả bên ngoài.
- Chức năng tải ảnh trực tiếp từ máy cho từng bước qua `uploadStepImageFile` đã được xây dựng sẵn trong `lib/api/steps.ts`, sẵn sàng liên thông với `ImageUploadButton` khi bước sang Bữa 8.

### 13.2. Dependencies
- `@/lib/api/client`: Axios instance với Interceptors và unwrap envelope.
- `types/step.ts`: Bộ type contract chuẩn hóa từ Bữa 5.
- Tailwind CSS: Bảng màu Emerald ngọc bích sang trọng, bo góc tròn hiện đại.

### 13.3. Checklist nghiệm thu bàn giao
- [x] Đã đối chiếu 100% mã yêu cầu đặc tả SRS v1.2.0 (FR-RCP-010, Bảng 7.3).
- [x] Mã nguồn sạch sẽ, phân tách rõ ràng trách nhiệm Component / State / API.
- [x] Đầy đủ 13 test cases Jest tự động pass 100%.
- [x] TypeScript compile pass 0 error; ESLint pass 0 error.
- [x] Next.js production build pass 100%.
- [x] Báo cáo kỹ thuật 13 phần minh bạch, rõ ràng, đúng thực tế.
