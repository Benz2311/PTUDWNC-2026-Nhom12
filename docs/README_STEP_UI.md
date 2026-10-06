# Giao diện Quản lý các bước nấu ăn (Recipe Step UI)

## 1. Mục tiêu

Module Giao diện Quản lý các bước thực hiện công thức (`Recipe Step UI`) được xây dựng nhằm giải quyết các bài toán sau trên tầng Frontend của CulinaryBlog:
- **Tương tác trực quan cho các bước nấu ăn**: Giúp tác giả công thức dễ dàng tạo mới, chỉnh sửa, xóa và sắp xếp thứ tự các bước thực hiện một cách trực quan, sinh động với badge số thứ tự (`01`, `02`), thời gian hẹn giờ (`timerMinutes`) và ảnh minh họa (`imageUrl`).
- **Hỗ trợ linh hoạt cả hai kịch bản Tạo mới và Chỉnh sửa công thức**:
  - Khi tạo công thức mới (chưa có `recipeId` trong CSDL), component hoạt động ở **Controlled mode** (quản lý state cục bộ, gửi dữ liệu lên component cha qua callback `onStepsChange`).
  - Khi chỉnh sửa công thức hiện có (đã có `recipeId`), component hoạt động ở **Direct API mode** (gọi trực tiếp REST API `/api/v1/recipes/{recipeId}/steps` để đồng bộ CSDL tức thì).
- **Tránh race condition và bảo toàn tính toàn vẹn thứ tự**: Khi đổi thứ tự các bước (Reorder), hệ thống gửi toàn bộ danh sách `stepIds` lên server để backend đánh số lại từ `1..N`, ngăn chặn hiện tượng trùng lặp hoặc nhảy bước.
- **Tách biệt ranh giới trách nhiệm (Separation of Concerns)**: Module được đóng gói thành các component độc lập (`StepEditor`, `StepItem`), tuyệt đối không sửa đổi mã nguồn `RecipeForm.tsx` (thuộc sở hữu của thành viên khác - TV3 Phạm Nguyễn Ngọc Phước) để tránh conflict.

---

## 2. Kết quả đạt được

Sau khi branch `feat/vohungmanh-step-ui` được triển khai, hệ thống đạt được các năng lực UI thực tế:
- **Component StepEditor hoàn chỉnh**: Quản lý toàn bộ vòng đời của danh sách bước, form inline thêm/sửa, modal xác nhận xóa, các trạng thái loading, empty và error banner.
- **Component StepItem trực quan**: Hiển thị card từng bước với số thứ tự định dạng 2 chữ số (`01`, `02`), timer badge hiển thị phút/giờ, ảnh thumbnail preview, nút di chuyển lên (`↑`), xuống (`↓`), nút sửa và nút xóa.
- **Thao tác thêm bước (Add Step)**: Hỗ trợ form nhập liệu inline với client-side validation đầy đủ.
- **Thao tác sửa bước (Edit Step)**: Chuyển card thành form chỉnh sửa ngay tại chỗ, giữ nguyên vị trí và thứ tự bước.
- **Thao tác xóa kèm xác nhận (Delete Step & Confirmation)**: Hiển thị dialog xác nhận trước khi xóa, tự động đánh số lại các bước còn lại từ `1..N`.
- **Thao tác đổi thứ tự (Reorder)**: Bấm nút `↑` và `↓` để hoán đổi vị trí, tự động vô hiệu hóa nút `↑` ở bước đầu và nút `↓` ở bước cuối.
- **Client-side Validation thời gian thực**: Kiểm tra độ dài tiêu đề ($\le 200$), mô tả (bắt buộc, $\le 2000$), thời gian hẹn giờ ($0 - 1440$ phút), URL ảnh hợp lệ.
- **Bộ kiểm thử tự động 100% PASS**: 13/13 unit tests chuyên biệt cho `StepEditor.test.tsx` (tổng 25/25 tests frontend) đạt kết quả thành công.

---

## 3. Luồng hoạt động

Luồng dữ liệu và tương tác của module Step UI:

```
User Thao tác (Thêm / Sửa / Xóa / Đổi vị trí)
   │
   ▼
StepEditor (Container Component)
   │
   ▼
Client-side Validation (Kiểm tra Title, Description, Timer, ImageUrl)
   │
   ├── [Không hợp lệ] ──> Hiển thị lỗi đỏ dưới input & Chặn gửi request
   │
   └── [Hợp lệ]
         │
         ├── [Controlled Mode (recipeId rỗng)]
         │      │
         │      ▼
         │   Cập nhật local state (gán tempId, tính stepNumber)
         │      │
         │      ▼
         │   Phát callback onStepsChange(newSteps) lên Component cha
         │
         └── [Direct API Mode (có recipeId)]
                │
                ▼
             Step API Client (lib/api/steps.ts via apiFetch / apiJson)
                │
                ▼
             Backend REST API (/api/v1/recipes/{recipeId}/steps)
                │
                ├── [Thành công] ──> Cập nhật UI State (danh sách bước mới từ Server)
                │
                └── [Thất bại]   ──> Hiển thị Error Banner / Problem Details
```

### Giải thích chi tiết các bước:
1. **Bước 1 - Người dùng tương tác**: Người dùng bấm nút thêm bước mới, sửa bước, bấm mũi tên di chuyển (`↑`, `↓`), hoặc bấm nút xóa một bước trên giao diện.
2. **Bước 2 - Container Component StepEditor tiếp nhận**:
   - Nếu là thêm/sửa: Form inline mở ra cho người dùng nhập Tiêu đề, Mô tả, Thời gian hẹn giờ, và URL ảnh.
   - Nếu là xóa: StepEditor mở modal xác nhận xóa trước khi thực hiện.
3. **Bước 3 - Client-side Validation**: Trước khi gửi request, form kiểm tra tính hợp lệ của dữ liệu. Nếu có trường vi phạm (vd: mô tả để trống, hẹn giờ âm hoặc vượt 1440), thông báo lỗi màu đỏ xuất hiện ngay bên dưới ô nhập liệu và thao tác bị chặn lại.
4. **Bước 4 - Phân nhánh chế độ hoạt động**:
   - **Direct API Mode** (khi có `recipeId`): Gọi hàm tương ứng trong `lib/api/steps.ts` (`createRecipeStep`, `updateRecipeStep`, `deleteRecipeStep`, hoặc `reorderRecipeSteps`) gửi HTTP request lên Backend.
   - **Controlled Mode** (khi chưa có `recipeId`): Thao tác trực tiếp trên mảng state cục bộ `steps`, sinh ID tạm thời (`temp-...`), tự tính toán `stepNumber` từ 1..N, và gọi callback `onStepsChange(newSteps)` để đồng bộ lên form cha.
5. **Bước 5 - Phản hồi và cập nhật UI State**:
   - Khi API thành công: State `steps` được cập nhật với dữ liệu mới từ server, form đóng lại, giao diện render danh sách bước cập nhật.
   - Khi API thất bại: State `error` lưu thông điệp lỗi, hiển thị banner cảnh báo màu đỏ ở đầu danh sách bước.

---

## 4. Các file chính

| File | Vai trò | Xử lý gì |
| :--- | :--- | :--- |
| `frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx` | Container Component chính | Quản lý state danh sách bước, chuyển đổi 2 chế độ (Direct API & Controlled), form inline Thêm/Sửa, dialog xác nhận xóa, loading state và error banner |
| `frontend/culinary-blog-web/components/recipe-management/StepItem.tsx` | Sub-component Card | Render card hiển thị từng bước: badge số thứ tự (`01`, `02`), timer badge, preview ảnh, các nút thao tác `↑`, `↓`, `Sửa`, `Xóa` |
| `frontend/culinary-blog-web/lib/api/steps.ts` | API Client Module | Chứa các hàm gọi REST API: `getRecipeSteps`, `createRecipeStep`, `updateRecipeStep`, `deleteRecipeStep`, `reorderRecipeSteps`, xử lý lỗi và mapping sang `StepApiError` |
| `frontend/culinary-blog-web/lib/api.ts` | Core HTTP Client | Cung cấp hàm tiện ích `apiFetch`, `apiJson`, tự động gắn headers, parse lỗi RFC 7807 `ValidationProblemDetails` |
| `frontend/culinary-blog-web/types/step.ts` | TypeScript Interfaces | Định nghĩa kiểu dữ liệu `RecipeStep`, `CreateStepInput`, `UpdateStepInput`, `ReorderStepsInput`, `StepFormValues`, `StepValidationError` |
| `frontend/culinary-blog-web/components/recipe-management/__tests__/StepEditor.test.tsx` | Unit Test Suite | 13 test cases kiểm tra render, validation, Add, Edit, Delete (có confirm), Reorder, Loading, Error handling, và Controlled mode |
| `frontend/culinary-blog-web/jest.config.mjs` | Test Configuration | Cấu hình Jest ES Module cho Next.js 15 |
| `docs/README_STEP_UI.md` | Tài liệu kỹ thuật | Hướng dẫn kiến trúc, luồng hoạt động, API và kết quả kiểm thử của branch UI RecipeStep |
| `docs/VO_HUNG_MANH_STEP_UI_COMPLAN.md` | Tài liệu giải trình | Báo cáo chi tiết, câu hỏi bảo vệ và ma trận đánh giá rủi ro |

---

## 5. API / Interface

### Component Props & Interface

| Component | Input / Props | API sử dụng | Output UI |
| :--- | :--- | :--- | :--- |
| **StepEditor** | `recipeId?: string`<br>`initialSteps?: RecipeStep[]`<br>`onStepsChange?: (steps: RecipeStep[]) => void`<br>`readOnly?: boolean` | `getRecipeSteps`<br>`createRecipeStep`<br>`updateRecipeStep`<br>`deleteRecipeStep`<br>`reorderRecipeSteps` | Danh sách các `StepItem`, nút `+ Thêm bước thực hiện`, form inline Thêm/Sửa, modal xác nhận xóa, banner báo lỗi |
| **StepItem** | `step: RecipeStep`<br>`index: number`<br>`totalSteps: number`<br>`onEdit: () => void`<br>`onDelete: () => void`<br>`onMoveUp: () => void`<br>`onMoveDown: () => void`<br>`disabled?: boolean` | Không gọi trực tiếp (gửi callback lên cha) | Card hiển thị số thứ tự dạng `01`, `02`, tiêu đề, mô tả, timer badge, thumbnail ảnh, các nút `↑`, `↓`, `Sửa`, `Xóa` |

### REST Endpoints mà `lib/api/steps.ts` giao tiếp:

| Method | Endpoint | Input Body | Output | Authorization |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/recipes/{recipeId}/steps` | Không | `RecipeStep[]` | Không bắt buộc |
| `POST` | `/api/v1/recipes/{recipeId}/steps` | `{ title?, description, timerMinutes?, imageUrl? }` | `RecipeStep` (với `stepNumber = N + 1`) | Bearer Token (Author/Admin) |
| `PUT` | `/api/v1/recipes/{recipeId}/steps/{stepId}` | `{ title?, description, timerMinutes?, imageUrl? }` | `RecipeStep` (giữ nguyên `stepNumber`) | Bearer Token (Author/Admin) |
| `DELETE` | `/api/v1/recipes/{recipeId}/steps/{stepId}` | Không | `200 OK` / `204 No Content` | Bearer Token (Author/Admin) |
| `PUT` | `/api/v1/recipes/{recipeId}/steps/reorder` | `{ stepIds: string[] }` | `RecipeStep[]` (đã đánh số lại `1..N`) | Bearer Token (Author/Admin) |

---

## 6. Business Rules

1. **Server là nguồn sự thật duy nhất cho `stepNumber`**:
   - Khi tạo mới (`Create`) hoặc cập nhật (`Update`), Frontend **tuyệt đối không gửi trường `stepNumber`** lên Backend.
   - Backend tự động cấp phát `stepNumber = N + 1` khi tạo mới và giữ nguyên khi cập nhật.
2. **Reorder gửi toàn bộ danh sách `stepIds`**:
   - Khi người dùng bấm di chuyển lên/xuống, Frontend tính toán mảng `stepIds` theo thứ tự mới hoàn chỉnh và gửi lên endpoint `/reorder`. Backend sẽ đánh số lại toàn bộ từ 1 đến N trong một transaction duy nhất.
   - Nút di chuyển lên (`↑`) tự động vô hiệu hóa ở bước đầu tiên (`index === 0`).
   - Nút di chuyển xuống (`↓`) tự động vô hiệu hóa ở bước cuối cùng (`index === totalSteps - 1`).
3. **Quy tắc xác thực Client-side**:
   - `Title`: Tùy chọn, tối đa 200 ký tự.
   - `Description`: Bắt buộc, không được để trống hoặc chỉ chứa khoảng trắng, tối đa 2000 ký tự.
   - `TimerMinutes`: Tùy chọn, nếu có giá trị phải là số nguyên trong khoảng từ `0` đến `1440` phút (24 giờ).
   - `ImageUrl`: Tùy chọn, nếu có phải là URL hợp lệ bắt đầu bằng `http://` hoặc `https://`, độ dài tối đa 2048 ký tự.
4. **Bảo vệ dữ liệu khi xóa (Delete Confirmation)**:
   - Thao tác xóa không được phép thực hiện ngay lập tức mà bắt buộc phải mở dialog xác nhận (`Bạn có chắc chắn muốn xóa bước này?`).
   - Chỉ khi người dùng bấm xác nhận xóa thì mới kích hoạt gọi API hoặc xóa khỏi state.
5. **Chống Double Submit khi Loading**:
   - Trong quá trình gửi request mạng (`isSubmitting === true`), toàn bộ các nút hành động (Lưu, Hủy, Xóa, Di chuyển) đều bị vô hiệu hóa (`disabled`) và hiển thị biểu tượng loading spinner.

---

## 7. Ví dụ hoạt động

### Ví dụ: Thêm bước thực hiện mới trong Direct API Mode

```
INPUT:
- RecipeId: "f47ac10b-58cc-4372-a567-0e02b2c3d479"
- Người dùng bấm nút "+ Thêm bước thực hiện"
- Điền form:
  * Title: "Sơ chế nguyên liệu"
  * Description: "Rửa sạch xương bò, trần qua nước sôi 5 phút để khử mùi."
  * TimerMinutes: 15
  * ImageUrl: "https://example.com/images/so-che.jpg"
- Bấm "Lưu bước thực hiện"

↓ PROCESS:
1. StepEditor chạy hàm validate:
   - Description có 56 ký tự (hợp lệ).
   - Timer 15 nằm trong khoảng [0, 1440] (hợp lệ).
   - ImageUrl hợp lệ.
2. StepEditor chuyển sang trạng thái isSubmitting = true (hiển thị spinner, disable nút).
3. Gọi API createRecipeStep("f47ac10b-58cc-4372-a567-0e02b2c3d479", {
     title: "Sơ chế nguyên liệu",
     description: "Rửa sạch xương bò...",
     timerMinutes: 15,
     imageUrl: "https://example.com/images/so-che.jpg"
   }):
   - Gửi HTTP POST /api/v1/recipes/f47ac10b-58cc-4372-a567-0e02b2c3d479/steps.
4. Backend trả về 201 Created với đối tượng:
   {
     "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
     "recipeId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
     "stepNumber": 1,
     "title": "Sơ chế nguyên liệu",
     "description": "Rửa sạch xương bò...",
     "timerMinutes": 15,
     "imageUrl": "https://example.com/images/so-che.jpg"
   }
5. StepEditor cập nhật mảng steps = [...steps, newStep], đóng form thêm, reset input, isSubmitting = false.

↓ OUTPUT:
- Giao diện render card StepItem mới:
  * Badge số thứ tự: "01" (nền xanh lá nhạt)
  * Tiêu đề: "Sơ chế nguyên liệu"
  * Nội dung: "Rửa sạch xương bò, trần qua nước sôi 5 phút để khử mùi."
  * Badge thời gian: "⏱ 15 phút"
  * Ảnh preview: hiển thị thumbnail ảnh từ URL
  * Các nút thao tác: ↑ (bị disable vì là bước đầu), ↓, Sửa, Xóa
```

---

## 8. Error Handling

| Tình huống lỗi | Hành vi UI | Phản hồi người dùng |
| :--- | :--- | :--- |
| Để trống trường mô tả (Description) | Chặn submit | Hiển thị thông báo đỏ: `"Nội dung hướng dẫn là bắt buộc"` ngay dưới ô textarea |
| Nhập thời gian hẹn giờ âm hoặc $> 1440$ | Chặn submit | Hiển thị thông báo đỏ: `"Thời gian hẹn giờ phải từ 0 đến 1440 phút"` |
| Nhập URL ảnh không hợp lệ (không có `http://` hoặc `https://`) | Chặn submit | Hiển thị thông báo đỏ: `"URL hình ảnh không hợp lệ"` |
| Gọi API thất bại do mất mạng hoặc server lỗi 500 | Bắt `ApiError` trong `catch` block | Hiển thị Banner cảnh báo màu đỏ ở đầu component với thông điệp lỗi chi tiết |
| Backend trả về lỗi xác thực chi tiết RFC 7807 (400 Problem Details) | Trích xuất trường `errors` từ response | Hiển thị lỗi tương ứng vào đúng từng trường nhập liệu |
| Người dùng bấm nhầm nút Xóa | Không xóa ngay | Mở Dialog xác nhận: `"Bạn có chắc chắn muốn xóa bước này không?"` với 2 nút: `Hủy` và `Xóa bước` |
| Người dùng bấm Lưu nhiều lần liên tục | Vô hiệu hóa nút khi `isSubmitting === true` | Ngăn chặn double submit, nút chuyển sang icon Spinner quay |

---

## 9. Cách chạy và Demo

### Điều kiện tiên quyết
- Node.js version 18+ hoặc 20+
- Dependencies frontend đã cài đặt đầy đủ

### Lệnh chạy môi trường phát triển
```bash
cd frontend/culinary-blog-web
npm run dev
```
Truy cập trình duyệt tại: `http://localhost:3000`.

### Kịch bản Demo cho Giảng viên (7 bước):
1. **Demo Empty State**: Mở trang quản lý công thức ở trạng thái chưa có bước nào -> quan sát thông điệp hướng dẫn rõ ràng và nút `+ Thêm bước thực hiện`.
2. **Demo Thêm bước mới (Add Step)**: Bấm nút thêm bước -> nhập Tiêu đề, Mô tả, Hẹn giờ 15 phút, nhập URL ảnh -> bấm `Lưu bước thực hiện` -> Card bước mới xuất hiện với badge số thứ tự `01` và thumbnail ảnh.
3. **Demo Chỉnh sửa (Edit Step)**: Bấm nút `Sửa` ở bước 1 -> sửa thời gian hẹn giờ từ 15 thành 20 phút -> bấm `Lưu` -> card cập nhật nội dung tức thì.
4. **Demo Đổi vị trí (Reorder)**: Thêm bước thứ 2 (badge `02`) -> tại bước 2 bấm nút mũi tên Lên (`↑`) -> hai bước hoán đổi vị trí, badge số thứ tự tự động cập nhật lại đúng thứ tự `01` và `02`.
5. **Demo Validation**: Mở form thêm bước, xóa trắng ô Mô tả hoặc nhập hẹn giờ 9999 -> hệ thống báo lỗi đỏ trực quan ngay dưới ô nhập và không cho phép lưu.
6. **Demo Xóa an toàn (Delete Confirmation)**: Bấm nút `Xóa` ở một bước -> Dialog xác nhận hiển thị cảnh báo -> bấm `Xóa bước` -> bước bị xóa và các bước còn lại được tự động đánh số lại từ `1..N`.
7. **Demo Error Handling**: Bật chế độ offline trong DevTools và bấm lưu -> Banner thông báo lỗi màu đỏ xuất hiện phía trên danh sách.

---

## 10. Testing

### Bộ kiểm thử tự động Frontend
Chạy bộ kiểm thử Jest bằng lệnh:
```powershell
cd frontend/culinary-blog-web
npm test
```

### Kết quả kiểm thử thực tế
- **Test Suites**: `2 passed, 2 total`
- **Tests**: `25 passed, 25 total`
- **Snapshots**: `0 total`
- **Time**: ~11.087 s

### 13 kịch bản kiểm thử trong `StepEditor.test.tsx` (13/13 PASS):
1. `renders empty state when no steps are provided`: PASS
2. `renders steps correctly with stepNumber and content`: PASS
3. `validates required description`: PASS
4. `validates max length for description (2000 chars)`: PASS
5. `validates max length for title (200 chars)`: PASS
6. `validates timerMinutes range (0 - 1440)`: PASS
7. `validates imageUrl format`: PASS
8. `calls createRecipeStep in Direct API mode and adds new step`: PASS
9. `calls updateRecipeStep in Direct API mode and updates step`: PASS
10. `opens delete confirmation modal and calls deleteRecipeStep`: PASS
11. `calls reorderRecipeSteps when moving step up or down`: PASS
12. `displays error banner when API call fails`: PASS
13. `supports controlled mode without calling API when recipeId is missing`: PASS

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
  - Shared JS chunks: 122 kB

---

## 12. Limitations

1. **Ranh giới tích hợp với `RecipeForm.tsx`**:
   - File `RecipeForm.tsx` thuộc quyền sở hữu của **Thành viên 3 (Phạm Nguyễn Ngọc Phước)**.
   - Để tuân thủ nguyên tắc cách ly branch và tránh merge conflict, branch `feat/vohungmanh-step-ui` **chủ động KHÔNG sửa đổi `RecipeForm.tsx`**.
   - Component `StepEditor` được thiết kế dưới dạng self-contained module, sẵn sàng để TV3 import vào `RecipeForm.tsx` ở giai đoạn tích hợp chung.
2. **Kiểm thử E2E trên trình duyệt thật**:
   - Kiểm thử trong branch này là **Unit Tests** sử dụng Jest và React Testing Library với mock HTTP client.
   - Kiểm thử E2E liên thông toàn diện giữa trình duyệt thật, Next.js server và Backend CSDL sẽ được thực hiện khi cả 2 module Frontend và Backend được triển khai trên môi trường staging.

---

## 13. Kết luận

Branch `feat/vohungmanh-step-ui` đã hoàn thành xuất sắc module giao diện quản lý các bước nấu ăn (`StepEditor` & `StepItem`):
- Cung cấp trải nghiệm người dùng mượt mà, trực quan với đầy đủ thao tác Thêm, Sửa, Xóa (có dialog xác nhận) và Đổi vị trí.
- Hỗ trợ linh hoạt cả **Direct API mode** (chỉnh sửa công thức) và **Controlled mode** (tạo công thức mới).
- Tuân thủ thiết kế kiến trúc chuẩn, xác thực client-side chặt chẽ, xử lý lỗi toàn diện.
- Đạt chất lượng kiểm thử cao với **13/13 unit tests passed** và build production thành công 0 lỗi.
