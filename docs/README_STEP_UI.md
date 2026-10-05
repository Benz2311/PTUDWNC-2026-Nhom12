# Võ Hùng Mạnh - Recipe Step UI

## 1. Thông tin branch
- **Thành viên:** Võ Hùng Mạnh
- **Task:** Task 4 - Frontend RecipeStep
- **Branch:** `feat/vohungmanh-step-ui`
- **Base:** `origin/main` (commit `a91589618f6460fc37e9b4861f2c5a0e946311b5`)
- **Mục tiêu branch:** Xây dựng module giao diện quản lý các bước thực hiện công thức nấu ăn (`Cooking Steps`) hoàn chỉnh, độc lập, hỗ trợ cả 2 chế độ `Direct API mode` và `Controlled mode`, đồng bộ với visual style của dự án Culinary Blog theo reference design, đảm bảo 100% kiểm thử unit tests và build thành công.

---

## 2. Phạm vi thực hiện
- **StepEditor:** Container component chính quản lý toàn bộ vòng đời của các bước, bao gồm state danh sách bước, form inline Thêm/Sửa, dialog xác nhận xóa, loading state và error banner.
- **StepItem:** Sub-component hiển thị từng bước dạng card trực quan, badge số thứ tự (`01`, `02`), timer badge, thumbnail ảnh preview, cùng các nút thao tác `↑`, `↓`, `Sửa`, `Xóa`.
- **Add Step:** Thêm bước mới (Direct API mode: gửi `POST` lên server, server tự cấp `stepNumber = N + 1`; Controlled mode: tạo bước cục bộ kèm id tạm và `stepNumber = steps.length + 1`).
- **Edit Step:** Sửa bước hiện có (Direct API mode: gửi `PUT` lên server giữ nguyên `stepNumber`; Controlled mode: cập nhật dữ liệu cục bộ).
- **Delete Step:** Xóa bước kèm hộp thoại xác nhận; sau khi xóa, backend tự đánh số lại hoặc frontend chuẩn hóa lại `stepNumber` từ 1..N.
- **Reorder Step:** Sắp xếp bước Lên/Xuống bằng nút bấm `↑` và `↓`. Tự động vô hiệu hóa nút `↑` ở bước đầu và nút `↓` ở bước cuối. Direct API mode: gửi toàn bộ mảng `stepIds` theo thứ tự mới lên endpoint `/reorder`. Controlled mode: hoán đổi vị trí và cập nhật `stepNumber` 1..N trong local state.
- **Validation:** Xác thực dữ liệu đầu vào client-side trước khi submit (Title, Description, TimerMinutes, ImageUrl).
- **Direct API mode:** Kích hoạt khi có `recipeId` hợp lệ -> tương tác trực tiếp với Backend REST API.
- **Controlled mode:** Kích hoạt khi chưa có `recipeId` (kịch bản tạo công thức mới) -> quản lý state cục bộ, phát callback `onStepsChange` lên component cha.
- **Loading / Error / Empty states:**
  - *Empty state:* Hiển thị thông điệp hướng dẫn khi chưa có bước nào được tạo.
  - *Loading state:* Trạng thái đang tải (hiển thị spinner, vô hiệu hóa nút bấm, ngăn chặn double submit).
  - *Error state:* Banner thông báo lỗi chi tiết khi gọi API thất bại hoặc lỗi xác thực từ backend.
  - *Delete confirmation dialog:* Hộp thoại xác nhận trước khi xóa nhằm tránh mất dữ liệu do thao tác nhầm.
- **Responsive UI:** Bố cục linh hoạt, tự động co giãn từ Desktop (layout hàng ngang rộng rãi) sang Tablet/Mobile (layout 1 cột, action bar chống tràn).

---

## 3. Giao diện
Giao diện của module `StepEditor` được thiết kế bám sát visual design của dự án Culinary Blog:
- **Xanh lá đậm:** Màu chủ đạo `#166534` (`bg-green-800`, hover `hover:bg-green-900`) sử dụng cho toàn bộ các nút hành động chính (`+ Thêm bước thực hiện`, `Lưu bước thực hiện`).
- **Nền trắng / off-white:** Toàn bộ khu vực soạn thảo và danh sách các bước sử dụng tone màu sáng, sạch sẽ (`bg-white` hoặc nền xám nhạt `bg-gray-50/50`).
- **Card trắng:** Mỗi bước được hiển thị trong một thẻ card riêng biệt (`bg-white`), bo góc vừa phải (`rounded-xl`), viền mảnh nhẹ (`border border-gray-200`, hover `hover:border-gray-300`).
- **Border nhẹ:** Sử dụng viền mảnh tinh tế (`border-gray-200`, `border-gray-300`), không sử dụng shadow đậm để giữ nét thanh lịch của một blog ẩm thực.
- **Button hierarchy:**
  - *Primary button:* Nền xanh lá đậm (`bg-green-800 text-white`).
  - *Secondary / Neutral button:* Dạng outline nền trắng viền xám (`bg-white border-gray-300 text-gray-700 hover:bg-gray-50`).
  - *Destructive button:* Nút xóa tinh tế (`bg-white border-gray-200 text-gray-600 hover:text-red-600 hover:bg-red-50 hover:border-red-200`), không gây chói mắt.
  - *Reorder buttons:* Cặp nút `↑` và `↓` gọn gàng, tự động làm mờ và vô hiệu hóa khi ở vị trí biên.
- **Responsive:** Hiển thị tối ưu trên màn hình nhỏ: badge số thứ tự và các nút di chuyển được gom nhóm hợp lý, nội dung text tự động xuống dòng và chống tràn.

---

## 4. Cấu trúc file
Dưới đây là danh sách CHÍNH XÁC các file thuộc branch `feat/vohungmanh-step-ui` và chức năng từng file:

- `frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx`: Container component chính quản lý toàn bộ vòng đời bước thực hiện, 2 chế độ hoạt động (Direct API & Controlled), form inline Thêm/Sửa, dialog xác nhận xóa, loading và error states.
- `frontend/culinary-blog-web/components/recipe-management/StepItem.tsx`: Sub-component hiển thị từng bước dưới dạng card, badge số thứ tự (`01`, `02`), timer badge, thumbnail ảnh, các nút thao tác `↑`, `↓`, `Sửa`, `Xóa`.
- `frontend/culinary-blog-web/components/recipe-management/__tests__/StepEditor.test.tsx`: Test suite gồm 13 kịch bản kiểm thử bao quát toàn bộ hành vi của `StepEditor`.
- `frontend/culinary-blog-web/lib/api.ts`: Module abstraction dùng chung cho HTTP request (`apiFetch`, `apiJson`, `ApiError`), tự động đọc API URL, xử lý authentication header và parse lỗi RFC 7807.
- `frontend/culinary-blog-web/lib/api/steps.ts`: API Client giao tiếp với các RESTful endpoints của RecipeStep Backend, tái sử dụng `apiFetch`/`apiJson`, mapping lỗi sang `StepApiError`.
- `frontend/culinary-blog-web/types/step.ts`: Định nghĩa TypeScript interfaces/types chuẩn cho `RecipeStep`, `CreateStepInput`, `UpdateStepInput`, `ReorderStepsInput`, `StepFormValues`, `StepValidationError`.
- `frontend/culinary-blog-web/jest.config.mjs`: Cấu hình Jest native ES Module cho dự án Next.js 15.
- `frontend/culinary-blog-web/package.json` & `package-lock.json`: Cấu hình dependencies bổ sung phục vụ test suite (`@testing-library/react`, `@testing-library/jest-dom`, `@testing-library/user-event`, `ts-jest`, `jest-environment-jsdom`).
- `docs/README_STEP_UI.md`: Tài liệu kỹ thuật chi tiết của branch UI RecipeStep.
- `docs/VO_HUNG_MANH_STEP_UI_COMPLAN.md`: Báo cáo giải trình toàn diện, câu hỏi bảo vệ và ma trận đánh giá rủi ro tích hợp.

---

## 5. RecipeStep API sử dụng
Module giao tiếp với các endpoints RESTful sau trên Backend:

- **Lấy danh sách các bước của công thức:**
  - `GET /api/v1/recipes/{slugOrId}`
  - Response: Đối tượng `RecipeDetail` chứa mảng `steps: RecipeStep[]`.
- **Thêm bước mới:**
  - `POST /api/v1/recipes/{recipeId}/steps`
  - Request Body:
    ```json
    {
      "title": "Sơ chế nguyên liệu",
      "description": "Rửa sạch hành lá và thái nhỏ.",
      "timerMinutes": 10,
      "imageUrl": "https://example.com/step1.jpg"
    }
    ```
  - Response: Đối tượng `RecipeStep` mới được tạo với `stepNumber = N + 1`.
- **Cập nhật bước thực hiện:**
  - `PUT /api/v1/recipes/{recipeId}/steps/{stepId}`
  - Request Body:
    ```json
    {
      "title": "Sơ chế nguyên liệu (Cập nhật)",
      "description": "Rửa sạch hành lá, để ráo nước và thái nhỏ.",
      "timerMinutes": 12,
      "imageUrl": "https://example.com/step1.jpg"
    }
    ```
  - Response: Đối tượng `RecipeStep` sau khi cập nhật (giữ nguyên `stepNumber`).
- **Xóa mềm bước thực hiện:**
  - `DELETE /api/v1/recipes/{recipeId}/steps/{stepId}`
  - Response: HTTP 200 OK hoặc 204 No Content. Backend tự động đánh số lại các bước còn lại từ 1..N.
- **Sắp xếp lại thứ tự các bước:**
  - `PUT /api/v1/recipes/{recipeId}/steps/reorder`
  - Request Body:
    ```json
    {
      "stepIds": [
        "42b781e8-7013-41ec-b2c6-01824c30c920",
        "18e38d7a-1fc1-4da2-a63e-72c086d99b1a"
      ]
    }
    ```
  - Response: Danh sách `RecipeStep[]` đã được sắp xếp lại với `stepNumber` mới từ 1..N.

### Nguyên tắc dữ liệu quan trọng:
- Khi tạo mới (`Create`) hoặc cập nhật (`Update`), Frontend **tuyệt đối không gửi `stepNumber`**.
- `stepNumber` hoàn toàn do Backend quản lý theo nguyên tắc **Server là nguồn sự thật duy nhất (Single Source of Truth)**.
- Khi thay đổi thứ tự (`Reorder`), Frontend gửi toàn bộ mảng `stepIds` theo thứ tự mới mong muốn.

---

## 6. Hai chế độ StepEditor

### Direct API Mode
- **Điều kiện kích hoạt:** Khi truyền prop `recipeId` có giá trị hợp lệ (khác chuỗi rỗng).
- **Cơ chế hoạt động:** Component tự động fetch danh sách bước từ backend khi khởi tạo nếu chưa có `initialSteps`. Mỗi thao tác Thêm, Sửa, Xóa hoặc Đổi vị trí đều thực hiện gọi trực tiếp đến Backend REST API tương ứng.
- **Ứng dụng:** Sử dụng trong màn hình Chỉnh sửa công thức (Edit Recipe) khi công thức đã có định danh trong CSDL.

### Controlled Mode
- **Điều kiện kích hoạt:** Khi prop `recipeId` là `undefined` hoặc rỗng `""`.
- **Cơ chế hoạt động:** Component quản lý danh sách bước trong bộ nhớ cục bộ (local state). Khi thêm mới, component tự gán ID tạm (`temp-...`) và tính `stepNumber = steps.length + 1`. Mỗi khi có thay đổi (Thêm, Sửa, Xóa, Sắp xếp), component đẩy mảng `RecipeStep[]` mới lên form cha qua callback `onStepsChange`. Tuyệt đối không gọi RecipeStep API.
- **Ứng dụng:** Sử dụng trong màn hình Tạo mới công thức (Create Recipe) khi công thức chưa được lưu vào CSDL và chưa có `recipeId`.

### Lý do cần hai chế độ:
Tránh việc gọi API mồ côi (orphaned steps) khi công thức chưa tồn tại trên database, đồng thời cho phép tái sử dụng duy nhất một component `StepEditor` cho cả 2 kịch bản Tạo mới và Chỉnh sửa công thức.

---

## 7. Validation
Xác thực form được kiểm tra chặt chẽ ở client-side trước khi submit dữ liệu:
- **Title (Tiêu đề bước):** Tùy chọn (optional), độ dài tối đa 200 ký tự. Có bộ đếm ký tự trực quan theo thời gian thực.
- **Description (Nội dung hướng dẫn):** Bắt buộc (required), không được để trống sau khi trim, độ dài tối đa 2000 ký tự.
- **TimerMinutes (Thời gian hẹn giờ):** Tùy chọn (optional), nếu nhập thì giá trị nguyên phải nằm trong khoảng từ `0` đến `1440` phút (tương đương 24 giờ).
- **ImageUrl (Đường dẫn ảnh minh họa):** Tùy chọn (optional), nếu nhập thì độ dài tối đa 2048 ký tự, phải là URL hợp lệ bắt đầu bằng `http://` hoặc `https://`. Hệ thống tự động ẩn thumbnail preview nếu URL ảnh bị hỏng hoặc không tải được.

---

## 8. Error Handling
- **API Error:** Bắt các mã lỗi HTTP từ Backend (`401 Unauthorized`, `403 Forbidden`, `404 NotFound`, hoặc lỗi kết nối mạng). Hiển thị banner thông báo lỗi màu đỏ rõ ràng phía trên danh sách bước. Nếu Backend trả về lỗi chi tiết theo chuẩn RFC 7807 (`ValidationProblemDetails`), các lỗi từng trường dữ liệu sẽ được hiển thị ngay bên dưới ô nhập liệu tương ứng.
- **Validation Error:** Kiểm tra tính hợp lệ của dữ liệu trước khi gửi request. Nếu có lỗi, hiển thị thông báo màu đỏ ngay dưới từng trường và chặn gửi request.
- **Loading State:** Hiển thị spinner và trạng thái vô hiệu hóa (disabled) toàn bộ các nút hành động trong khi request đang được xử lý, ngăn ngừa hiện tượng bấm nhiều lần (double submit).
- **Delete Confirmation:** Hiển thị dialog xác nhận (`Bạn có chắc chắn muốn xóa bước này?`) trước khi thực hiện xóa, ngăn ngừa nguy cơ xóa nhầm dữ liệu.

---

## 9. Testing
- **Lệnh chạy kiểm thử:**
  ```powershell
  cd frontend/culinary-blog-web
  npm test
  ```
- **Danh sách 13 kịch bản kiểm thử trong `StepEditor.test.tsx`:**
  1. Render đúng số thứ tự StepNumber (`01`, `02`) và nội dung các bước.
  2. Hiển thị Empty State khi danh sách bước rỗng.
  3. Validation Description: Bắt buộc và không vượt quá 2000 ký tự.
  4. Validation Title: Không vượt quá 200 ký tự.
  5. Validation TimerMinutes: Hợp lệ trong khoảng 0 - 1440 phút.
  6. Validation ImageUrl: Phải là URL hợp lệ `http`/`https` và không quá 2048 ký tự.
  7. Add Step trong Direct API mode: Gọi `createRecipeStep` và render bước mới.
  8. Update Step trong Direct API mode: Gọi `updateRecipeStep` và cập nhật nội dung.
  9. Delete + Confirmation: Mở modal hỏi xác nhận trước khi xóa bước.
  10. Reorder Up/Down: Gửi toàn bộ danh sách active step IDs theo thứ tự mới lên server.
  11. Hiển thị Error Banner khi gọi API thất bại.
  12. Disable các nút thao tác khi đang trong trạng thái loading.
  13. Controlled mode: Quản lý local state và không gọi API khi chưa có `recipeId`.
- **Kết quả kiểm thử thực tế mới nhất:**
  - `Test Suites:` 2 passed, 2 total (`StepEditor.test.tsx` + `recipe-utils.test.ts`)
  - `Tests:` 25 passed, 25 total (13/13 test cases của StepEditor PASS)
  - `Snapshots:` 0 total
  - `Time:` 14.347 s

---

## 10. Build
- **Lệnh đóng gói build:**
  ```powershell
  cd frontend/culinary-blog-web
  $env:NODE_OPTIONS="--max-old-space-size=4096"; npm run build
  ```
- **Kết quả build thực tế:**
  - Next.js 15.5.25 (Turbopack)
  - Compiled successfully in 7.4s
  - Generating static pages (5/5) hoàn thành
  - Zero type error, exit code 0

---

## 11. Cách chạy
Thực hiện các bước sau để khởi chạy ứng dụng Next.js trên môi trường phát triển:

```bash
cd frontend/culinary-blog-web
npm install
npm run dev
```

- **URL truy cập local:** `http://localhost:3000`

---

## 12. Cách demo với giảng viên
Trình tự demo các chức năng theo 7 bước rõ ràng:

1. **Empty State:** Mở component ở trạng thái chưa có bước nào, trình bày giao diện thông báo hướng dẫn và nút `+ Thêm bước thực hiện`.
2. **Add Step:** Bấm nút thêm bước, điền dữ liệu mẫu (Tiêu đề: `Sơ chế nguyên liệu`, Mô tả: `Rửa sạch rau củ và thái nhỏ`, Hẹn giờ: `10` phút, ImageUrl hợp lệ), bấm `Lưu bước thực hiện` -> bước xuất hiện với badge `01`.
3. **Edit Step:** Bấm nút `Sửa` ở bước vừa tạo, cập nhật mô tả hoặc thời gian hẹn giờ -> giao diện cập nhật nội dung tức thì.
4. **Reorder:** Thêm bước thứ hai (badge `02`), bấm nút Di chuyển Lên (`↑`) ở bước 2 -> hai bước hoán đổi vị trí, badge tự động cập nhật lại đúng thứ tự `01` và `02`.
5. **Delete:** Bấm nút `Xóa` ở một bước -> modal popup xác nhận hiển thị cảnh báo. Bấm `Xóa bước` -> bước bị xóa và các bước còn lại được tự động đánh số lại tuần tự.
6. **Validation:** Mở form thêm bước, bỏ trống ô mô tả hoặc nhập thời gian hẹn giờ âm / vượt quá 1440 -> hệ thống báo lỗi đỏ ngay dưới ô nhập liệu và chặn submit.
7. **Error Handling:** Trình diễn khi API trả về lỗi hoặc ngắt kết nối mạng -> banner thông báo lỗi màu đỏ hiển thị rõ ràng trên giao diện.

---

## 13. Ranh giới với thành viên khác
- **Module phụ trách:** Võ Hùng Mạnh (TV4) chịu trách nhiệm toàn bộ mã nguồn của `StepEditor.tsx`, `StepItem.tsx`, `lib/api/steps.ts`, `types/step.ts` và test suite liên quan.
- **Module của thành viên khác:** File `RecipeForm.tsx` thuộc trách nhiệm của **Phạm Nguyễn Ngọc Phước (TV3)**.
- **Cam kết branch:** Branch `feat/vohungmanh-step-ui` **tuyệt đối không chỉnh sửa `RecipeForm.tsx`** hoặc bất kỳ component nào của thành viên khác để tránh rủi ro merge conflict.
- **Trạng thái hiện tại:** `StepEditor` là một component độc lập, có thể nhúng trực tiếp vào bất kỳ form nào thông qua interface props chuẩn hóa.
- **Kế hoạch phối hợp:** Sau khi TV3 hoàn thiện `RecipeForm.tsx`, hai thành viên sẽ tiến hành tích hợp và thực hiện integration test chung.

---

## 14. Những gì branch này KHÔNG làm
Để giữ ranh giới phân công rành mạch, branch này KHÔNG thực hiện:
- `ImageManager`: Quản lý upload file ảnh trực tiếp lên Cloudinary (thuộc module Media).
- `Search UI`: Giao diện tìm kiếm, lọc và phân trang công thức (thuộc Task Tìm kiếm).
- `Recipe Basic Information`: Form nhập tiêu đề, mô tả tóm tắt, thời gian chuẩn bị, khẩu phần của công thức (thuộc TV3).
- `IngredientEditor`: Quản lý danh sách nguyên liệu của công thức (thuộc TV3).
- `Nutrition`: Quản lý bảng thành phần dinh dưỡng.
- `Auth`: Giao diện đăng nhập, đăng ký, JWT token management.
- `Category / Dashboard`: Quản trị danh mục công thức.

---

## 15. Hạn chế / Việc tiếp theo
Ghi nhận trung thực tình trạng hiện tại:
- `StepEditor` hiện chưa được nhúng trực tiếp vào `RecipeForm.tsx` trong branch này (nhằm bảo vệ mã nguồn của TV3).
- Chưa có End-to-End (E2E) test liên thông toàn diện giữa `RecipeForm` và `StepEditor` trên trình duyệt thật.
- **Kế hoạch tiếp theo:** Phối hợp cùng TV3 để mount `StepEditor` vào `RecipeForm.tsx` (dùng Controlled mode cho New Recipe và Direct API mode cho Edit Recipe), sau đó chạy kiểm thử liên thông toàn bộ quy trình.

---

## 16. Tài liệu giải trình
Báo cáo giải trình chi tiết về kiến trúc kỹ thuật, 10 câu hỏi bảo vệ đồ án và ma trận phân tích rủi ro có thể tham khảo tại:
- [VO_HUNG_MANH_STEP_UI_COMPLAN.md](file:///b:/PTUDWNC-2026-Nhom12-MANH/docs/VO_HUNG_MANH_STEP_UI_COMPLAN.md)
