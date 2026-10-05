# BÁO CÁO TOÀN DIỆN & KẾ HOẠCH BẢO VỆ MODULE STEP EDITOR (FRONTEND RECIPESTEP)
## DỰ ÁN CULINARY BLOG - NHÓM 12

> **Sinh viên thực hiện:** Võ Hùng Mạnh
> **Mã tính năng:** Task 4 - Frontend RecipeStep UI (`StepEditor`)
> **Branch Git:** `feat/vohungmanh-step-ui`
> **Source Tree chính thức:** `frontend/culinary-blog-web/`
> **Thời điểm hoàn thiện:** Tháng 10/2026

---

## MỤC LỤC
1. [Giới thiệu & Mục đích của StepEditor](#1-giới-thiệu--mục-đích-của-stepeditor)
2. [Tại sao cần tách Component StepEditor độc lập?](#2-tại-sao-cần-tách-component-stepeditor-độc-lập)
3. [Phân tích 2 chế độ vận hành: Direct API mode vs Controlled mode](#3-phân-tích-2-chế-độ-vận-hành-direct-api-mode-vs-controlled-mode)
4. [Nguyên lý Single Source of Truth: Tại sao StepNumber do Server quản lý?](#4-nguyên-lý-single-source-of-truth-tại-sao-stepnumber-do-server-quản-lý)
5. [Quy trình luồng dữ liệu (Data Flows)](#5-quy-trình-luồng-dữ-liệu-data-flows)
   - 5.1. Luồng Thêm bước (Add Step Flow)
   - 5.2. Luồng Cập nhật bước (Update Step Flow)
   - 5.3. Luồng Xóa & Đánh số lại (Delete + Renumber Flow)
   - 5.4. Luồng Sắp xếp lại thứ tự (Reorder Flow)
6. [Quy tắc Xác thực (Validation Rules) & Trải nghiệm Người dùng](#6-quy-tắc-xác-thực-validation-rules--trải-nghiệm-người-dùng)
7. [Xử lý Lỗi (Error Handling) & Phục hồi Trạng thái](#7-xử-lý-lỗi-error-handling--phục-hồi-trạng-thái)
8. [Cấu trúc tệp & Phân định Source Tree Frontend](#8-cấu-trúc-tệp--phân-định-source-tree-frontend)
9. [API Contract giữa Frontend & Backend](#9-api-contract-giữa-frontend--backend)
10. [Chi tiết 13 Kịch bản Kiểm thử Unit Test](#10-chi-tiết-13-kịch-bản-kiểm-thử-unit-test)
11. [Visual Design & UI Reference (Bám sát thiết kế Culinary Blog)](#11-visual-design--ui-reference-bám-sát-thiết-kế-culinary-blog)
12. [Ranh giới Module với Thành viên khác (Phạm Nguyễn Ngọc Phước)](#12-ranh-giới-module-với-thành-viên-khác-phạm-nguyễn-ngọc-phước)
13. [Kịch bản Demo trực tiếp cho Giảng viên (3 Phút)](#13-kịch-bản-demo-trực-tiếp-cho-giảng-viên-3-phút)
14. [Bộ 10 Câu hỏi Vấn đáp Chuyên sâu & Đáp án Bảo vệ](#14-bộ-10-câu-hỏi-vấn-đáp-chuyên-sâu--đáp-án-bảo-vệ)

---

## 1. Giới thiệu & Mục đích của StepEditor

Trong một ứng dụng blog ẩm thực, **Các bước thực hiện (Cooking Steps)** là linh hồn của công thức nấu ăn. Một công thức không thể hoàn thiện nếu thiếu các chỉ dẫn chi tiết theo trình tự thời gian, thời gian hẹn giờ (timer), và hình ảnh trực quan cho từng công đoạn.

`StepEditor` được xây dựng nhằm giải quyết bài toán:
- Cho phép đầu bếp và người dùng nhập liệu, chỉnh sửa, xóa và sắp xếp thứ tự các bước nấu ăn một cách tiện lợi, mượt mà và trực quan.
- Tự động đồng bộ số thứ tự bước (`StepNumber`) chính xác với Backend C# / PostgreSQL.
- Đảm bảo tính toàn vẹn dữ liệu qua hệ thống xác thực chặt chẽ (độ dài tiêu đề, nội dung bắt buộc, khoảng thời gian hẹn giờ, định dạng URL ảnh).

---

## 2. Tại sao cần tách Component StepEditor độc lập?

Thay vì viết gộp toàn bộ logic bước nấu ăn vào `RecipeForm.tsx` (như bản mockup ban đầu của TV3), việc tách `StepEditor` và `StepItem` thành các component độc lập mang lại 5 lợi ích kiến trúc then chốt:

1. **Tuân thủ Single Responsibility Principle (SRP):** Form chính (`RecipeForm.tsx`) chỉ quản lý metadata công thức (tên món, danh mục, thời gian, khẩu phần, dinh dưỡng). Toàn bộ logic quản lý bước (reorder, timer, validation, confirmation dialog) được đóng gói bên trong `StepEditor`.
2. **Khả năng Tái sử dụng (Reusability):** `StepEditor` có thể được tái sử dụng ở nhiều màn hình khác nhau:
   - Trang tạo công thức mới (`/recipes/new`).
   - Trang chỉnh sửa công thức (`/recipes/[slug]/edit`).
   - Modal biên tập nhanh các bước nấu ăn trực tiếp trên trang chi tiết công thức (`/recipes/[slug]`).
3. **Dễ dàng Kiểm thử (Testability):** Có thể viết unit test chuyên sâu cho toàn bộ 13 hành vi của bước (add, edit, delete, reorder, error, loading) mà không cần render toàn bộ form cha đồ sộ chứa hàng chục trường nguyên liệu và dinh dưỡng.
4. **Tránh Xung đột Nhánh (Git Conflict Reduction):** TV3 (Phạm Nguyễn Ngọc Phước) phụ trách `RecipeForm.tsx`, TV4 (Võ Hùng Mạnh) phụ trách `StepEditor.tsx`. Việc tách file giúp 2 thành viên làm việc song song mà không xung đột mã nguồn.
5. **Hiệu năng Render (Performance):** Khi người dùng gõ nội dung hoặc reorder bước, chỉ có `StepEditor` và các `StepItem` liên quan được re-render, không kích hoạt re-render toàn bộ form lớn của Recipe.

---

## 3. Phân tích 2 chế độ vận hành: Direct API mode vs Controlled mode

`StepEditor` hỗ trợ 2 chế độ vận hành rõ ràng thông qua thuộc tính `recipeId`:

| Tiêu chí | A. Direct API mode (`recipeId` tồn tại) | B. Controlled mode (`recipeId` không có) |
|---|---|---|
| **Ngữ cảnh sử dụng** | Khi chỉnh sửa công thức đã có sẵn trên hệ thống (`/recipes/[slug]/edit`). | Khi tạo mới công thức lần đầu (`/recipes/new`), công thức chưa tồn tại trong CSDL. |
| **Nguồn dữ liệu** | Gọi `getRecipeSteps(recipeId)` hoặc lấy từ `initialSteps`. | Quản lý state mảng cục bộ trong React hook `useState<RecipeStep[]>`. |
| **Thao tác Thêm** | Gọi `POST /api/v1/recipes/{id}/steps`. Server cấp ID thật và `StepNumber = N + 1`. | Tạo item với ID tạm thời (`temp-...`), `stepNumber = length + 1`, đẩy vào state local. |
| **Thao tác Sửa** | Gọi `PUT /api/v1/recipes/{id}/steps/{stepId}`. | Cập nhật object tương ứng trong state local. |
| **Thao tác Xóa** | Gọi `DELETE /api/v1/recipes/{id}/steps/{stepId}`. Server xóa mềm và tự renumber. | Lọc bỏ item khỏi state local và renumber lại các bước còn lại từ `1..N`. |
| **Thao tác Reorder** | Gọi `PUT /api/v1/recipes/{id}/steps/reorder` với mảng toàn bộ active Step IDs. | Hoán đổi vị trí trong state local và cập nhật lại `stepNumber = index + 1`. |
| **Giao tiếp Parent** | Cập nhật local UI và gọi `onStepsChange?.(steps)`. | Gọi `onStepsChange(steps)` để truyền danh sách bước về `RecipeForm` phục vụ submit tạo mới. |
| **Quy tắc quan trọng**| **Không hard-code URL.** Tái sử dụng auth token từ `localStorage`. | **Tuyệt đối KHÔNG gọi API backend** trước khi Recipe được tạo ra. Không tạo API contract giả. |

---

## 4. Nguyên lý Single Source of Truth: Tại sao StepNumber do Server quản lý?

Trong kiến trúc hệ thống của nhóm: **Server (Backend .NET 8 Web API + PostgreSQL) là nguồn sự thật duy nhất (Single Source of Truth) của thuộc tính `StepNumber`.**

### Lý do kỹ thuật:
1. **Phòng chống Race Condition & Đồng thời:** Nếu nhiều phiên làm việc hoặc nhiều tab cùng thao tác chỉnh sửa bước, việc Client tự sinh `StepNumber` sẽ dẫn đến trùng số thứ tự bước hoặc tạo ra các khoảng trống (gaps) trong cơ sở dữ liệu.
2. **Bảo đảm tính tuần tự liên tục ($1..N$):** Backend đảm bảo rằng các bước của một công thức luôn tạo thành một dãy số nguyên dương liên tục từ $1$ đến $N$, không bao giờ bị đứt đoạn hay trùng lặp.
3. **Hợp đồng RESTful API trong sáng:**
   - Khi `CreateStep`: Client chỉ gửi nội dung (`title`, `description`, `timerMinutes`, `imageUrl`). Backend tự động tính `StepNumber = Max(StepNumber) + 1` và gán vào entity.
   - Khi `UpdateStep`: Client chỉ gửi nội dung cần cập nhật. `StepNumber` được giữ nguyên vẹn.
   - Khi `DeleteStep`: Backend xóa mềm bước và tự động chạy query renumber cập nhật lại toàn bộ các bước còn lại về đúng chuỗi tuần tự $1..N$.
   - Khi `ReorderSteps`: Client gửi mảng danh sách các Step IDs theo thứ tự mới mong muốn. Backend validate và cập nhật lại `StepNumber = index + 1` cho từng Step trong một Transaction duy nhất.

---

## 5. Quy trình luồng dữ liệu (Data Flows)

### 5.1. Luồng Thêm bước (Add Step Flow)
```
[User nhấn 'Thêm bước']
       │
       ▼
[Mở Inline Form] ─── (Nhập Title, Description*, Timer, ImageUrl)
       │
       ▼
[Client Validation] ─── (Nếu lỗi: hiển thị message đỏ, chặn submit)
       │ (Hợp lệ)
       ├─── Chế độ A (Có recipeId):
       │      │
       │      ▼
       │    [Hiển thị Loading Spinner & Disable Actions]
       │      │
       │      ▼
       │    [Gọi POST /api/v1/recipes/{recipeId}/steps]
       │      │
       │      ├── Thành công (201 Created):
       │      │     Backend trả về RecipeStepDto (kèm StepNumber = N + 1 mới).
       │      │     Thêm vào danh sách steps, đóng form, gọi onStepsChange.
       │      │
       │      └── Thất bại (400/401/403/500):
       │            Bắt StepApiError, hiển thị Global Error Banner hoặc Field Errors.
       │
       └─── Chế độ B (Controlled mode, không có recipeId):
              │
              ▼
            Tạo bước tạm thời: id='temp-...', stepNumber=steps.length+1
            Thêm vào state local, đóng form, gọi callback onStepsChange(newSteps).
```

### 5.2. Luồng Cập nhật bước (Update Step Flow)
```
[User nhấn 'Sửa' tại Step k]
       │
       ▼
[Mở Inline Form với dữ liệu hiện tại của Step k]
       │
       ▼
[User chỉnh sửa & Nhấn 'Lưu cập nhật']
       │
       ▼
[Client Validation]
       │ (Hợp lệ)
       ├─── Chế độ A:
       │      │
       │      ▼
       │    [Gọi PUT /api/v1/recipes/{recipeId}/steps/{stepId}]
       │      │
       │      ├── Thành công (200 OK):
       │      │     Thay thế object step cũ bằng step mới nhận từ server.
       │      │     Đóng form, gọi onStepsChange.
       │      │
       │      └── Thất bại: Hiển thị Error Banner.
       │
       └─── Chế độ B: Cập nhật object trong state local, gọi onStepsChange.
```

### 5.3. Luồng Xóa & Đánh số lại (Delete + Renumber Flow)
```
[User nhấn nút 'Xóa' tại Step k]
       │
       ▼
[Hiển thị Delete Confirmation Dialog]
       │
       ├─── [User bấm 'Hủy bỏ'] ───> Đóng Dialog, giữ nguyên dữ liệu.
       │
       └─── [User bấm 'Xóa bước']
              │
              ├─── Chế độ A:
              │      │
              │      ▼
              │    [Gọi DELETE /api/v1/recipes/{recipeId}/steps/{stepId}]
              │      │
              │      ├── Thành công (204 NoContent):
              │      │     Backend đã tự động renumber các bước còn lại.
              │      │     Frontend loại bỏ step khỏi state và renumber 1..N.
              │      │     Đóng Dialog, gọi onStepsChange.
              │      │
              │      └── Thất bại: Hiển thị Error Banner, không xóa trên UI.
              │
              └─── Chế độ B: Loại bỏ step khỏi mảng local, renumber 1..N, đóng Dialog.
```

### 5.4. Luồng Sắp xếp lại thứ tự (Reorder Flow)
```
[User nhấn nút Lên hoặc Xuống tại Step k]
       │
       ▼
[Frontend hoán đổi vị trí Step k với Step liền kề]
       │
       ├─── Chế độ A:
       │      │
       │      ▼
       │    [Lấy mảng toàn bộ active Step IDs theo thứ tự mới: [id_1, id_2, ...]]
       │      │
       │      ▼
       │    [Hiển thị Loading & Gọi PUT /api/v1/recipes/{recipeId}/steps/reorder]
       │      │  (Body: { "stepIds": ["guid-1", "guid-2", ...] })
       │      │
       │      ├── Thành công (200 OK):
       │      │     Nhận danh sách steps đã reorder từ Backend.
       │      │     Cập nhật state, gọi onStepsChange.
       │      │
       │      └── Thất bại: Hiển thị Error Banner, rollback vị trí nếu cần.
       │
       └─── Chế độ B: Cập nhật thứ tự mảng local, tính lại stepNumber = index + 1, gọi onStepsChange.
```

---

## 6. Quy tắc Xác thực (Validation Rules) & Trải nghiệm Người dùng

Tất cả các trường nhập liệu đều được kiểm tra chặt chẽ ở Client trước khi gửi request:

| Trường dữ liệu | Tính bắt buộc | Ràng buộc kỹ thuật | Thông báo lỗi hiển thị |
|---|---|---|---|
| **Title** | Tùy chọn | Chuỗi $\le 200$ ký tự | *"Tiêu đề bước không được vượt quá 200 ký tự."* |
| **Description** | **Bắt buộc** | Không rỗng sau khi trim, $\le 2000$ ký tự | *"Nội dung hướng dẫn bước là bắt buộc."* / *"Nội dung hướng dẫn không được vượt quá 2000 ký tự."* |
| **TimerMinutes** | Tùy chọn | Số nguyên $\ge 0$ và $\le 1440$ (24 giờ) | *"Thời gian hẹn giờ phải từ 0 đến 1440 phút (24 giờ)."* |
| **ImageUrl** | Tùy chọn | URL hợp lệ (`http://` hoặc `https://`), $\le 2048$ ký tự | *"Đường dẫn ảnh không hợp lệ (cần bắt đầu bằng http:// hoặc https://)."* / *"Đường dẫn ảnh không được vượt quá 2048 ký tự."* |

### Tính năng UX cao cấp:
- **Bộ đếm ký tự thời gian thực:** Hiển thị `0/200` và `0/2000` giúp người dùng chủ động căn chỉnh nội dung.
- **Xem trước ảnh (Image Thumbnail Preview):** Khi có `imageUrl`, tự động hiển thị khung ảnh bo góc với hiệu ứng phóng to nhẹ khi hover và cơ chế bắt lỗi `onError` ẩn ảnh nếu link hỏng.
- **Trạng thái vô hiệu hóa (Disabled state):** Khi đang có thao tác API chạy, toàn bộ nút Thêm, Sửa, Xóa, Lên, Xuống đều bị disable để ngăn chặn việc spam click tạo trùng lặp request.

---

## 7. Xử lý Lỗi (Error Handling) & Phục hồi Trạng thái

Module xây dựng lớp ngoại lệ riêng `StepApiError` kế thừa từ `Error` JavaScript chuẩn:
```typescript
export class StepApiError extends Error {
  status: number;
  errors?: Record<string, string[]>;
}
```

Hệ thống xử lý thông minh các mã phản hồi HTTP từ server:
- **401 Unauthorized:** Chuyển ngữ thành: *"Bạn cần đăng nhập để thực hiện thao tác này."*
- **403 Forbidden:** Chuyển ngữ thành: *"Bạn không có quyền chỉnh sửa bước của công thức này."*
- **404 Not Found:** Chuyển ngữ thành: *"Không tìm thấy công thức hoặc bước thực hiện tương ứng."*
- **400 Bad Request / ValidationProblemDetails (RFC 7807):** Tự động bóc tách các trường trong object `errors` (ví dụ `errors.Title`, `errors.Description`, `errors.TimerMinutes`) để hiển thị thông báo lỗi ngay dưới từng input tương ứng trên form.
- **Mạng chập chờn / Mất kết nối:** Hiển thị Global Error Banner màu đỏ phía trên danh sách, cho phép người dùng bấm nút "×" để đóng thông báo.

---

## 8. Cấu trúc tệp & Phân định Source Tree Frontend

### Kết quả thẩm định Source Tree:
- **Source tree thực sự đang chạy ứng dụng:** `frontend/culinary-blog-web/`
  - Chứa `package.json`, `tsconfig.json`, `next.config.ts`, `node_modules`, `jest.config.mjs`.
  - Đây là thư mục duy nhất thực thi được các lệnh `npm test` và `next build`.
- **Thư mục `frontend/` ngoài:** Chỉ chứa các file lẻ do TV3 commit trước đó, không có `package.json` và không thể build độc lập.
- **Tuân thủ chỉ thị:** Tuyệt đối không tạo file trùng lặp (duplicate) giữa hai cây thư mục, không đồng bộ thủ công. Toàn bộ mã nguồn mới được đặt trọn vẹn trong `frontend/culinary-blog-web/`.

### Danh sách tệp thuộc Võ Hùng Mạnh:
1. `frontend/culinary-blog-web/types/step.ts`: Định nghĩa kiểu dữ liệu.
2. `frontend/culinary-blog-web/lib/api.ts`: API abstraction helper dùng chung (`apiFetch`, `apiJson`, `ApiError`).
3. `frontend/culinary-blog-web/lib/api/steps.ts`: API Client RESTful RecipeStep (tái sử dụng `apiFetch`/`apiJson` từ `@/lib/api`).
4. `frontend/culinary-blog-web/components/recipe-management/StepItem.tsx`: UI item hiển thị từng bước.
5. `frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx`: Component container chính.
6. `frontend/culinary-blog-web/components/recipe-management/__tests__/StepEditor.test.tsx`: Test suite 13 kịch bản.
7. `frontend/culinary-blog-web/jest.config.mjs`: Cấu hình test runner native ES Module.
8. `docs/README_STEP_UI.md`: Tài liệu hướng dẫn sử dụng.
9. `docs/VO_HUNG_MANH_STEP_UI_COMPLAN.md`: Kế hoạch bảo vệ đồ án toàn diện.

---

## 9. API Contract giữa Frontend & Backend

Các endpoint Backend được gọi bởi `lib/api/steps.ts` (kế thừa từ `feat/vohungmanh-recipe-step`):

| Phương thức | Đường dẫn API | Payload gửi đi | Phản hồi thành công | Mô tả |
|---|---|---|---|---|
| `GET` | `/api/v1/recipes/{slugOrId}` | *(Không có)* | `200 OK` kèm `RecipeDetailDto` (chứa `steps: RecipeStepDto[]`) | Lấy danh sách các bước của công thức |
| `POST` | `/api/v1/recipes/{id}/steps` | `{ title, description, timerMinutes, imageUrl }` | `201 Created` kèm `RecipeStepDto` | Thêm bước mới (Server tự cấp `StepNumber = N + 1`) |
| `PUT` | `/api/v1/recipes/{id}/steps/{stepId}` | `{ title, description, timerMinutes, imageUrl }` | `200 OK` kèm `RecipeStepDto` | Cập nhật bước (Giữ nguyên `StepNumber`) |
| `DELETE`| `/api/v1/recipes/{id}/steps/{stepId}` | *(Không có)* | `204 NoContent` | Xóa mềm bước (Server tự động renumber lại 1..N) |
| `PUT` | `/api/v1/recipes/{id}/steps/reorder` | `{ stepIds: string[] }` | `200 OK` kèm `RecipeStepDto[]` | Sắp xếp lại thứ tự toàn bộ các bước |

---

## 10. Chi tiết 13 Kịch bản Kiểm thử Unit Test

Được triển khai trong `components/recipe-management/__tests__/StepEditor.test.tsx`:

1. **Test 1 - Render đúng StepNumber:** Kiểm tra badge số thứ tự `01`, `02` và các nội dung tiêu đề, mô tả hiển thị chính xác.
2. **Test 2 - Empty State:** Kiểm tra khi danh sách bước rỗng, UI hiển thị khối thông báo "Chưa có bước thực hiện nào" kèm hướng dẫn.
3. **Test 3 - Validation Description:** Kiểm tra thông báo lỗi khi để trống mô tả hoặc nhập vượt quá 2000 ký tự.
4. **Test 4 - Validation Title:** Kiểm tra thông báo lỗi khi tiêu đề vượt quá 200 ký tự.
5. **Test 5 - Validation TimerMinutes:** Kiểm tra thông báo lỗi khi nhập số âm (`-5`) hoặc số vượt quá 24 giờ (`1441`).
6. **Test 6 - Validation ImageUrl:** Kiểm tra thông báo lỗi khi nhập chuỗi không phải định dạng URL http/https hợp lệ.
7. **Test 7 - Add Step (Direct API mode):** Giả lập `createRecipeStep` thành công, kiểm tra gọi đúng tham số (không gửi `stepNumber`) và render bước mới `03`.
8. **Test 8 - Update Step (Direct API mode):** Giả lập `updateRecipeStep`, kiểm tra form prefill đúng dữ liệu cũ và cập nhật nội dung mới.
9. **Test 9 - Delete + Confirmation:** Kiểm tra khi nhấn Xóa, hộp thoại xác nhận hiện ra. Nếu bấm "Hủy" thì không xóa; nếu bấm "Xóa bước" thì gọi API xóa và renumber lại UI.
10. **Test 10 - Reorder Up/Down:** Kiểm tra nhấn nút Xuống, API `reorderRecipeSteps` được gọi với danh sách mảng active Step IDs đã đổi vị trí `['step-2', 'step-1']`. Nút Lên ở bước đầu tiên bị vô hiệu hóa.
11. **Test 11 - API Error:** Giả lập API trả về lỗi mạng, kiểm tra hiển thị Error Banner và khả năng đóng banner.
12. **Test 12 - Disable actions khi loading:** Kiểm tra trong lúc request đang xử lý (pending Promise), nút Lưu bị disabled và indicator loading hiển thị.
13. **Test 13 - Controlled mode:** Kiểm tra khi không có `recipeId`, thêm bước mới không gọi bất kỳ API backend nào mà gọi callback `onStepsChange` với ID tạm thời.

---

## 11. Visual Design & UI Reference (Bám sát thiết kế Culinary Blog)

Module `StepEditor` được thiết kế dựa trên ảnh giao diện tổng thể **Culinary Blog - Hành trình trải nghiệm ứng dụng** (đặc biệt bám sát các màn hình tham chiếu: **Màn hình 6 - Chi tiết công thức**, **Màn hình 7 - Quy trình nấu ăn On-rail**, **Màn hình 9 & 10 - Tạo và Chỉnh sửa công thức**).

### 11.1. Bảng màu chủ đạo (Color Palette)
- **Màu chính (Primary Color):** Màu **Xanh lá đậm Culinary Blog** (`#166534` - Green-800 / `#14532d` - Green-900). Màu sắc này xuất hiện đồng bộ ở Logo, Header và toàn bộ các nút hành động chính (`+ Thêm bước`, `Lưu bước`).
- **Màu phụ & Selected / Badge State:** Màu **Xanh lá nhạt** (`#f0fdf4` - Green-50 kết hợp viền `#bbf7d0` - Green-200 và chữ `#166534` - Green-800) được sử dụng cho badge số thứ tự bước `01`, `02` và icon empty state.
- **Nền tổng thể (Background):** Tông màu sáng, sạch sẽ, ưu tiên trắng (`#ffffff`) và off-white dịu nhẹ (`#fafafa` / `#f9fafb`), tạo cảm giác trang nhã, không gây mỏi mắt cho người đầu bếp khi đọc hướng dẫn.

### 11.2. Phong cách Card (Card Style)
- **Nền trắng thuần túy (`bg-white`):** Mỗi bước nấu ăn (`StepItem`) là một thẻ card độc lập trên nền trắng.
- **Đường viền mảnh, tinh tế (`border border-gray-200`):** Không dùng viền dày thô ráp.
- **Bo góc vừa phải (`rounded-xl`):** Tạo sự mềm mại, hiện đại nhưng chuẩn mực theo layout chung của Culinary Blog.
- **Không Shadow hoặc Shadow siêu nhẹ (`shadow-xs`):** Tuyệt đối loại bỏ hiệu ứng shadow đổ bóng nặng nề hoặc drop-shadow 3D lòe loẹt.
- **Loại bỏ hoàn toàn phong cách không phù hợp:** Không dùng gradient màu mè, không dùng glassmorphism (lớp kính mờ), không dùng hiệu ứng neon, không mang phong cách dark dashboard công nghệ rời rạc với trang blog ẩm thực.

### 11.3. Phân cấp Nút bấm (Button Hierarchy)
- **Nút chính (Primary Action):** Nút `+ Thêm bước` và `Lưu bước` có nền xanh lá đậm (`bg-green-800 hover:bg-green-900 text-white`), kích thước chữ `text-sm font-medium`, padding gọn gàng `px-4 py-2`.
- **Nút phụ (Secondary / Neutral Action):** Nút `Hủy`, nút di chuyển `↑`, `↓` và nút `Sửa` dùng phong cách **outline/neutral** (`bg-white hover:bg-gray-50 text-gray-700 border border-gray-300`).
- **Nút xóa (Destructive Action):** Nút `Xóa` được thiết kế thanh thoát (`text-gray-600 hover:text-red-600 hover:bg-red-50 border border-gray-200 hover:border-red-200`), chỉ chuyển màu đỏ cảnh báo khi hover, giúp tổng thể giao diện không bị áp đảo bởi màu đỏ. Trong hộp thoại xác nhận xóa, nút `Xóa bước` dùng nền đỏ chuẩn `bg-red-600 hover:bg-red-700 text-white`.

### 11.4. Thiết kế Form Thêm / Chỉnh sửa bước (Form Style)
- Tương thích 100% với form `RecipeForm.tsx`:
  - **Tiêu đề bước:** Text input với counter độ dài `0/200`.
  - **Hướng dẫn thực hiện *:** Textarea đa dòng với counter độ dài `0/2000`.
  - **Thời gian (phút):** Input number gọn gàng có phạm vi $0 - 1440$.
  - **URL ảnh minh họa:** Input url kèm tự động render thumbnail xem trước trực quan.
- **Trạng thái Focus:** Viền chuyển sang màu xanh lá đậm (`focus:border-green-800 focus:ring-1 focus:ring-green-800`).
- **Thông báo xác thực (Validation Message):** Đặt ngay dưới ô input tương ứng với màu đỏ rõ nét (`text-xs text-red-600 mt-1`), biến mất ngay khi người dùng bắt đầu chỉnh sửa lại nội dung.

### 11.5. Khả năng thích ứng đa màn hình (Responsive Behavior)
- **Màn hình Desktop / Laptop:** Khớp hoàn hảo với form soạn thảo Recipe, card Step hiển thị theo chiều ngang thoáng đãng (số bước và nút di chuyển bên trái, nội dung ở giữa, nút Sửa/Xóa ở bên phải).
- **Màn hình Tablet / Mobile:**
  - Card Step tự động chuyển sang layout 1 cột (vertical stacked layout).
  - Thanh nút hành động (Sửa, Xóa) tự động căn chỉnh xuống dưới cùng của card, co giãn linh hoạt, không bị tràn màn hình (horizontal overflow).
  - Bảng form nhập liệu chuyển từ 2 cột sang 1 cột đơn để dễ dàng nhập liệu trên bàn phím ảo điện thoại.

### 11.6. Tổng kết mức độ bám sát Reference
Khi đặt `StepEditor` cạnh Màn hình 6, 7, 9 và 10 của ảnh reference Culinary Blog:
- Người dùng cảm nhận ngay đây là một phần tự nhiên, liền mạch của cùng một sản phẩm.
- Sự đồng nhất về màu xanh lá đặc trưng thương hiệu, font chữ, icon đồng hồ hẹn giờ `⏱`, và khung ảnh minh họa bo góc giúp giao diện đạt tính thẩm mỹ chuyên nghiệp cao nhất.

---

## 12. Ranh giới Module với Thành viên khác (Phạm Nguyễn Ngọc Phước)

- File `RecipeForm.tsx` thuộc trách nhiệm của **Phạm Nguyễn Ngọc Phước (TV3)**.
- **Nguyên tắc bảo toàn ranh giới:**
  - Không sửa đổi bừa bãi vào file của Phước.
  - Không thay đổi phần Thông tin cơ bản (Section 01), Nguyên liệu (Section 02), Dinh dưỡng (Section 04), Ảnh món ăn (Section 05).
  - Không can thiệp vào logic lưu trữ bản nháp hay submit Recipe của Phước.
- **Điểm tích hợp tối thiểu (Integration Point):**
  - Section 03 trong `RecipeForm.tsx` (hiện đang là mockup hiển thị thẻ input thô sơ) sẽ được thay thế bằng thẻ `<StepEditor ... />`.
  - Dữ liệu trả về qua callback `onStepsChange` hoàn toàn tương thích với mảng `steps: { title: string; description: string }[]` mà form của Phước đang sử dụng để gửi lên API tạo Recipe.
- **Đánh giá Rủi ro Tích hợp thực tế (Integration Assessment):**
  - Component `StepEditor` và `StepItem` đã hoàn thành độc lập, vượt qua 100% các bài kiểm thử tự động (Unit Tests) và kiểm tra đóng gói (Production Build).
  - Hiện tại, `StepEditor` **chưa được tích hợp trực tiếp vào `RecipeForm.tsx`** của TV3 để đảm bảo an toàn tuyệt đối cho branch của thành viên khác và tránh phát sinh xung đột Git (merge conflict).
  - Quá trình tích hợp thực tế (End-to-End integration) sẽ cần kiểm thử và xác minh lại cẩn thận khi TV3 chính thức mount component `<StepEditor />` vào `RecipeForm.tsx`.

---

## 13. Kịch bản Demo trực tiếp cho Giảng viên (3 Phút)

Khi giảng viên yêu cầu demo trực tiếp tính năng:

- **Bước 1 (1 phút) - Trình bày Controlled mode (Tạo Recipe mới):**
  - Mở trang tạo công thức mới.
  - Quan sát danh sách bước ở trạng thái Empty State.
  - Bấm "Thêm bước", cố tình để trống mô tả -> chỉ ra Client Validation báo lỗi.
  - Nhập tiêu đề "Sơ chế", mô tả "Thái thịt bò mỏng", thời gian 15 phút, link ảnh hợp lệ -> Lưu. Bước `01` xuất hiện ngay lập tức.
  - Thêm tiếp bước `02` "Xào thịt". Nhấn nút Lên ở bước `02` -> hai bước hoán đổi vị trí thành `01` và `02` hoàn hảo mà không hề gọi API trước khi Recipe tồn tại.
- **Bước 2 (1 phút) - Trình bày Direct API mode (Chỉnh sửa Recipe đã có):**
  - Mở công thức đã có `recipeId`.
  - Mở Tab Network trong DevTools (F12).
  - Bấm "Thêm bước" -> Quan sát request `POST /api/v1/recipes/{id}/steps` với payload không có `stepNumber`. Server trả về mã `201 Created` kèm `stepNumber = 3`.
  - Bấm nút Đổi thứ tự -> Quan sát request `PUT /api/v1/recipes/{id}/steps/reorder` gửi mảng IDs.
  - Bấm nút "Xóa" -> Hộp thoại cảnh báo xuất hiện -> Bấm Xác nhận -> Quan sát request `DELETE` và các bước còn lại được tự động đánh số lại liên tục $1..N$.
- **Bước 3 (1 phút) - Báo cáo chất lượng kiểm thử:**
  - Chạy lệnh `npm test` trong terminal -> Show 25 test cases PASS 100%.
  - Chạy lệnh `next build` -> Show production build compile thành công rực rỡ.

---

## 13. Bộ 10 Câu hỏi Vấn đáp Chuyên sâu & Đáp án Bảo vệ

### Câu hỏi 1: Tại sao em không để Frontend tự sinh `stepNumber` khi thêm mới một bước để tiết kiệm thời gian?
**Đáp án:**
"Thưa thầy/cô, trong một hệ thống đa người dùng hoặc khi người dùng mở nhiều tab, việc Client tự sinh `stepNumber` sẽ gây ra xung đột dữ liệu (Race Condition). Giả sử client A và client B cùng thêm một bước mới, cả hai sẽ cùng tự gán số 3, dẫn đến vi phạm tính duy nhất hoặc sai lệch thứ tự. Việc quy định **Server là nguồn sự thật duy nhất (Single Source of Truth)** đảm bảo tính toàn vẹn của dữ liệu: Backend sẽ chạy logic `Max(StepNumber) + 1` trong database transaction và trả về số thứ tự chuẩn xác cho Frontend."

---

### Câu hỏi 2: Sự khác biệt bản chất giữa Direct API mode và Controlled mode trong component của em là gì?
**Đáp án:**
"Thưa thầy/cô, điểm khác biệt bản chất nằm ở **sự tồn tại của thực thể cha (Recipe)**:
- Ở **Controlled mode**, Recipe chưa hề được tạo trong CSDL (người dùng đang ở form tạo mới). Do đó không thể gọi bất kỳ Step API nào vì các endpoint đều yêu cầu `recipeId` làm khóa ngoại. StepEditor sẽ đóng vai trò quản lý state cục bộ và trả danh sách qua callback `onStepsChange` để form cha gom vào payload tạo Recipe.
- Ở **Direct API mode**, Recipe đã tồn tại (`recipeId` hợp lệ). Mọi hành động Thêm, Sửa, Xóa, Reorder đều được gọi trực tiếp bằng các HTTP request độc lập (fine-grained RESTful operations), giúp lưu dữ liệu ngay lập tức (realtime persistence) mà người dùng không cần phải bấm Lưu toàn bộ Recipe."

---

### Câu hỏi 3: Khi người dùng đổi thứ tự các bước (Reorder), tại sao em lại gửi danh sách toàn bộ ID thay vì chỉ gửi 2 ID hoán đổi cho nhau?
**Đáp án:**
"Thưa thầy/cô, việc gửi mảng toàn bộ active Step IDs theo thứ tự mới (`{ stepIds: Guid[] }`) là thiết kế chuẩn theo nguyên lý **Idempotency** của RESTful API. Nếu chỉ gửi 2 ID hoán đổi, nếu một request bị mất gói tin hoặc gửi lại (retry), thứ tự trong database sẽ bị đảo lộn không thể kiểm soát. Khi gửi toàn bộ danh sách ID, Backend chỉ cần duyệt qua mảng và gán `step.StepNumber = index + 1`. Đồng thời, Backend cũng dễ dàng validate xem danh sách ID gửi lên có đầy đủ và thuộc về đúng Recipe đó hay không."

---

### Câu hỏi 4: Em xử lý tình huống người dùng đang gọi API thêm bước nhưng mạng bị lag hoặc mất kết nối như thế nào?
**Đáp án:**
"Thưa thầy/cô, em quản lý state `isLoading` và `errorMessage`. Ngay khi bắt đầu gọi API, `isLoading = true`, toàn bộ các nút thao tác (Thêm, Sửa, Xóa, Reorder) trên giao diện đều được gắn thuộc tính `disabled`, ngăn chặn triệt để hành vi spam click gây duplicate dữ liệu. Nếu mạng bị ngắt kết nối, hàm fetch ném ngoại lệ, `StepEditor` bắt lỗi thông qua khối `catch`, kích hoạt `errorMessage` và hiển thị banner thông báo lỗi màu đỏ kèm nút đóng. Dữ liệu trên form nhập liệu của người dùng vẫn được bảo lưu, không bị mất để họ có thể thử gửi lại sau khi mạng ổn định."

---

### Câu hỏi 5: Tại sao em không dùng thư viện kéo thả như `react-beautiful-dnd` hay `@dnd-kit` mà lại dùng nút Lên/Xuống?
**Đáp án:**
"Thưa thầy/cô, việc sử dụng các nút Lên/Xuống (Up/Down) mang lại 3 ưu thế vượt trội:
1. **Khả năng tiếp cận (Accessibility - a11y):** Các nút bấm chuẩn ngữ nghĩa HTML với đầy đủ nhãn `aria-label` cho phép người dùng điều hướng hoàn hảo bằng bàn phím (Keyboard navigation) hoặc trình đọc màn hình cho người khiếm thị, điều mà các thư viện kéo thả bằng chuột rất khó đảm bảo.
2. **Tối ưu trên thiết bị di động (Mobile UX):** Trên màn hình cảm ứng điện thoại, việc kéo thả một card dài có thể bị xung đột với thao tác cuộn trang (scroll gesture) của trình duyệt. Nút bấm Lên/Xuống giúp thao tác chính xác và không bị giật lag.
3. **Độ ổn định cao và kích thước bundle gọn nhẹ:** Không làm phình to dung lượng bundle JavaScript của Next.js và tránh các lỗi hydration/SSR thường gặp với thư viện kéo thả."

---

### Câu hỏi 6: Khi xóa một bước ở giữa (ví dụ xóa bước 2 trên tổng số 3 bước), làm thế nào để bước 3 trở thành bước 2?
**Đáp án:**
"Thưa thầy/cô:
- Ở Backend (Task 4 Backend của em), khi lệnh `DELETE /recipes/{id}/steps/{stepId}` được gọi, service xóa mềm bước đó và thực thi câu lệnh đánh số lại tuần tự cho các bước có `StepNumber > stepNumberBịXóa`: giảm `StepNumber` đi 1 đơn vị.
- Ở Frontend, sau khi nhận phản hồi `204 NoContent` thành công từ Backend, Frontend thực hiện lọc bỏ step đã xóa khỏi state và dùng hàm `.map((s, idx) => ({ ...s, stepNumber: idx + 1 }))` để đồng bộ ngay lập tức với kết quả trong database mà không cần phải tốn thêm một request `GET` để tải lại toàn bộ trang."

---

### Câu hỏi 7: Em phân định ranh giới mã nguồn giữa em và bạn Phạm Nguyễn Ngọc Phước (TV3) như thế nào?
**Đáp án:**
"Thưa thầy/cô, bạn Phước phụ trách toàn bộ module biên soạn công thức cha (`RecipeForm.tsx`), quản lý các thông tin cơ bản, nguyên liệu và dinh dưỡng. Em phụ trách độc lập module các bước thực hiện (`StepEditor.tsx`, `StepItem.tsx`, `steps.ts`).
File `RecipeForm.tsx` là điểm tích hợp duy nhất (Integration Point): bạn Phước chỉ cần nhúng `<StepEditor />` vào Section 03 thay cho phần mockup cũ. Toàn bộ logic validation, dialog xóa, API steps đều do component của em tự chịu trách nhiệm. Nếu em thay đổi UI hay logic bên trong StepEditor, file `RecipeForm.tsx` của bạn Phước hoàn toàn không bị ảnh hưởng."

---

### Câu hỏi 8: Em giải quyết bài toán hai cây thư mục `frontend/` và `frontend/culinary-blog-web/` trong dự án như thế nào?
**Đáp án:**
"Thưa thầy/cô, sau khi em kiểm tra kỹ các file cấu hình `package.json`, `tsconfig.json`, `next.config.ts` và cơ chế build của Next.js, em xác định **`frontend/culinary-blog-web/` là source tree duy nhất thực tế của ứng dụng**. Cây thư mục `frontend/` bên ngoài không có `package.json`, không thể cài đặt dependency và không thể build được. Vì vậy, em chỉ triển khai toàn bộ mã nguồn của mình vào `frontend/culinary-blog-web/`, tuyệt đối không tạo duplicate file giữa 2 cây thư mục để giữ cho repository luôn sạch sẽ và nhất quán."

---

### Câu hỏi 9: Tại sao em lại thêm thuộc tính `noValidate` vào thẻ `<form>` trong `StepEditor`?
**Đáp án:**
"Thưa thầy/cô, trên các thẻ input như `<textarea>` hay `<input type="number">`, nếu ta đặt thuộc tính HTML5 constraint như `required` hay `min/max`, trình duyệt sẽ kích hoạt cơ chế validation mặc định trước khi event `onSubmit` của React được kích hoạt. Cơ chế này thường hiển thị tooltip mặc định của trình duyệt rất xấu, không đồng bộ với design system của ứng dụng, và gây khó khăn khi viết automated testing bằng React Testing Library. Bằng cách thêm `noValidate`, em vô hiệu hóa tooltip mặc định của trình duyệt và trao toàn quyền kiểm soát cho hệ thống validation tùy biến bằng TypeScript của mình, hiển thị các thông báo lỗi chuẩn xác dưới từng trường nhập liệu."

---

### Câu hỏi 10: Test suite của em kiểm tra những gì và đạt tỷ lệ bao phủ ra sao?
**Đáp án:**
"Thưa thầy/cô, test suite của em được viết bằng **Jest** và **React Testing Library** trong file `StepEditor.test.tsx` gồm đầy đủ **13 kịch bản kiểm thử độc lập**:
1. Render đúng số thứ tự `01`, `02`.
2. Hiển thị Empty State khi chưa có bước nào.
3. Validate bắt buộc và độ dài tối đa 2000 ký tự của Description.
4. Validate độ dài tối đa 200 ký tự của Title.
5. Validate khoảng thời gian 0 - 1440 phút của Timer.
6. Validate URL ảnh hợp lệ.
7. Thêm bước trong Direct API mode (kiểm tra không gửi `stepNumber`).
8. Cập nhật bước.
9. Xóa bước kèm mở hộp thoại xác nhận (Confirmation Dialog).
10. Sắp xếp lại thứ tự Up/Down và vô hiệu hóa nút biên.
11. Bắt và hiển thị lỗi từ API Backend.
12. Vô hiệu hóa nút bấm khi request đang xử lý (loading state).
13. Chế độ Controlled mode không gọi API khi chưa có `recipeId`.
Toàn bộ 13/13 test cases đều đạt **100% PASS** mượt mà."
