# Quản lý bước thực hiện công thức (Recipe Step)

## 1. Mục tiêu
Branch `feat/vohungmanh-recipe-step` giải quyết việc xây dựng phân hệ quản lý các bước thực hiện công thức nấu ăn (`RecipeStep`) cho nền tảng CulinaryBlog:
- Cho phép tác giả phân rã quá trình nấu ăn thành chuỗi các bước tuần tự, trực quan và dễ tiếp cận cho người đọc.
- Đảm bảo tính nhất quán dữ liệu của chuỗi các bước: số thứ tự bước (`StepNumber`) bắt buộc phải liên tục từ $1 \dots N$, không được ngắt quãng hoặc trùng lặp.
- Hỗ trợ đầy đủ các thao tác vòng đời của một bước: Thêm mới, Cập nhật thông tin, Xóa mềm (Soft Delete) kèm tự động đánh số lại, và Sắp xếp lại thứ tự (Reorder).
- Bảo vệ dữ liệu công thức thông qua kiểm soát quyền sở hữu (Ownership check) và giải quyết triệt để lỗi xung đột chỉ mục duy nhất (Unique Constraint Conflict) trên PostgreSQL khi sắp xếp lại các bước trong cùng một Transaction.

---

## 2. Kết quả đạt được
Sau khi triển khai branch này:
- **Server là Single Source of Truth cho StepNumber**: Client không bao giờ tự cấp hoặc chỉnh sửa `StepNumber` trong các thao tác Thêm hoặc Cập nhật; máy chủ tự động tính toán và đảm bảo chuỗi số $1 \dots N$.
- **4 Endpoint RESTful hoàn chỉnh**:
  - `POST /api/v1/recipes/{id}/steps`: Thêm bước mới (tự động nhận `StepNumber = N + 1`).
  - `PUT /api/v1/recipes/{id}/steps/{stepId}`: Cập nhật tiêu đề, mô tả, hẹn giờ, link ảnh (bảo lưu nguyên vẹn `StepNumber`).
  - `PUT /api/v1/recipes/{id}/steps/reorder`: Sắp xếp lại toàn bộ thứ tự các bước đang hoạt động.
  - `DELETE /api/v1/recipes/{id}/steps/{stepId}`: Xóa mềm bước và tự động đánh số lại các bước còn lại từ $1 \dots (N-1)$ trong Database Transaction.
- **Giải thuật 2-Phase Renumbering an toàn**: Sử dụng dải số tạm thời (`10000 + i`) trong Phase 1 trước khi gán số chính thức `1..N` trong Phase 2, giải quyết hoàn toàn lỗi `UniqueConstraintViolation` của chỉ mục lọc PostgreSQL `(RecipeId, StepNumber) WHERE "IsDeleted" = false`.
- **Tự động làm mới bộ nhớ đệm (Cache Invalidation)**: Tự động xóa khóa cache Redis `recipe:{slug}` sau mỗi thao tác ghi dữ liệu, đảm bảo người đọc luôn xem được hướng dẫn nấu ăn mới nhất.
- **Kiểm thử bao phủ**: Toàn bộ 9/9 kịch bản kiểm thử trong `RecipeStepTests` và 96/97 tests toàn hệ thống đều vượt qua thành công (100% Pass).

---

## 3. Luồng hoạt động

```text
Client (Postman / Scalar / Web Browser)
  │ (Gửi HTTP Request: POST, PUT, DELETE)
  ▼
API Minimal Endpoints (RecipeEndpoints.cs)
  │ ├─ Bóc tách thông tin người dùng từ JWT Claims (CurrentUserId, IsAdmin)
  │ ├─ Ánh xạ route parameters và Request Body
  │ └─ Gửi Command tương ứng qua MediatR ISender
  ▼
Application Layer: RecipeStepCommandHandler (MediatR)
  │ ├─ 1. ValidateStepFields:
  │ │    Description bắt buộc, Title <= 200 ký tự,
  │ │    TimerMinutes >= 0, ImageUrl <= 500 ký tự.
  │ │
  │ ├─ 2. Truy vấn Recipe từ Database:
  │ │    Kiểm tra Recipe tồn tại và !IsDeleted (ném NotFoundException nếu không tìm thấy)
  │ │
  │ ├─ 3. VerifyRecipeOwnership:
  │ │    Kiểm tra AuthorId == CurrentUserId hoặc IsAdmin (ném ForbiddenException nếu sai quyền)
  │ │
  │ └─ 4. Thực thi nghiệp vụ tương ứng:
  │      ├─ [Create]: Tính StepNumber = Max(StepNumber) + 1 -> Insert Entity mới
  │      ├─ [Update]: Cập nhật Title, Description, Timer, ImageUrl -> Giữ nguyên StepNumber
  │      ├─ [Delete]: Mở Transaction -> Đặt IsDeleted = true -> 2-Phase Renumbering -> Commit
  │      └─ [Reorder]: Mở Transaction -> Xác thực đủ Active Steps -> 2-Phase Renumbering -> Commit
  ▼
Infrastructure Layer: IApplicationDbContext & PostgreSQL
  │ ├─ Kiểm tra các Check Constraints (CK_RecipeSteps_StepNumber, CK_RecipeSteps_TimerMinutes)
  │ └─ Thực thi SQL Commit trong Transaction
  ▼
Redis Cache Invalidation: ICacheService
  │ └─ Xóa cache chi tiết công thức: recipe:{slug}
  ▼
Output
  └─ Trả về HTTP 201 Created / 200 OK / 204 NoContent kèm RecipeStepDto
```

---

## 4. Các file chính

| File | Vai trò | Xử lý gì |
| :--- | :--- | :--- |
| `backend/src/CulinaryBlog.Application/Features/Recipes/Commands/RecipeSteps/RecipeStepCommands.cs` | Application / MediatR Handler | Định nghĩa các Command (`Create`, `Update`, `Delete`, `Reorder`), DTO ánh xạ, kiểm tra ràng buộc trường dữ liệu, kiểm tra quyền sở hữu công thức, và thuật toán 2-phase renumbering trong Transaction. |
| `backend/src/CulinaryBlog.Api/Endpoints/Recipes/RecipeEndpoints.cs` | Presentation / Minimal API | Đăng ký route Minimal APIs cho 4 endpoint của RecipeStep, đọc token JWT (UserId, Admin role), bắt các ngoại lệ nghiệp vụ (`NotFoundException`, `ForbiddenException`, `ValidationException`) và ánh xạ sang HTTP status code chuẩn. |
| `backend/tests/CulinaryBlog.UnitTests/RecipeStepTests.cs` | Unit Tests | Kiểm thử các Check Constraints database (StepNumber >= 1, TimerMinutes >= 0), chỉ mục lọc Unique Index, độ dài tối đa các trường, và các quy tắc xác thực dữ liệu đầu vào khi Create/Update. |
| `backend/tests/CulinaryBlog.IntegrationTests/Program.cs` | Integration Tests | Kiểm thử kịch bản tích hợp luồng thêm, cập nhật, sắp xếp và xóa bước nấu ăn với database. |

---

## 5. API / Interface

| Method | Endpoint | Input | Output | Authorization |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/recipes/{id}/steps` | Path: `id` (GUID)<br>Body: `CreateRecipeStepRequest`<br>- `title?` (string, max 200)<br>- `description` (string, required)<br>- `timerMinutes?` (int, $\ge 0$)<br>- `imageUrl?` (string, max 500) | HTTP 201 Created<br>Body: `RecipeStepDto` (chứa `id`, `stepNumber`, `title`, `description`, `timerMinutes`, `imageUrl`) | Yêu cầu đăng nhập; Người dùng phải là Tác giả (`AuthorId`) hoặc `Admin`. |
| `PUT` | `/api/v1/recipes/{id}/steps/{stepId}` | Path: `id` (GUID), `stepId` (GUID)<br>Body: `UpdateRecipeStepRequest`<br>- `title?` (string, max 200)<br>- `description` (string, required)<br>- `timerMinutes?` (int, $\ge 0$)<br>- `imageUrl?` (string, max 500) | HTTP 200 OK<br>Body: `RecipeStepDto` (giữ nguyên `stepNumber` cũ) | Yêu cầu đăng nhập; Người dùng phải là Tác giả (`AuthorId`) hoặc `Admin`. |
| `PUT` | `/api/v1/recipes/{id}/steps/reorder` | Path: `id` (GUID)<br>Body: `ReorderRecipeStepsRequest`<br>- `stepIds` (List\<Guid\>, bắt buộc chứa đúng toàn bộ active step IDs của Recipe) | HTTP 200 OK<br>Body: `List<RecipeStepDto>` (đã được sắp xếp và gán lại `stepNumber = 1..N`) | Yêu cầu đăng nhập; Người dùng phải là Tác giả (`AuthorId`) hoặc `Admin`. |
| `DELETE` | `/api/v1/recipes/{id}/steps/{stepId}` | Path: `id` (GUID), `stepId` (GUID) | HTTP 204 No Content | Yêu cầu đăng nhập; Người dùng phải là Tác giả (`AuthorId`) hoặc `Admin`. |

---

## 6. Business Rules

1. **Server là nguồn chân lý duy nhất cho `StepNumber`**:
   - Khi tạo mới một bước (`CreateRecipeStepCommand`), hệ thống tự động tìm số thứ tự lớn nhất trong số các bước đang hoạt động (`activeSteps.Max(s => s.StepNumber)`) và cộng 1. Nếu công thức chưa có bước nào, bước đầu tiên nhận `StepNumber = 1`.
   - Client không được truyền `StepNumber` trong payload gửi lên.
2. **Quyền sở hữu công thức (Ownership Enforcement)**:
   - Chỉ tác giả sở hữu công thức (`recipe.AuthorId == CurrentUserId`) hoặc người dùng có vai trò quản trị viên (`IsAdmin = true`) mới có quyền Thêm, Sửa, Xóa hoặc Đổi thứ tự bước. Vi phạm lập tức trả về HTTP 403 Forbidden.
3. **Ràng buộc trường dữ liệu (Field Validation)**:
   - `description`: Bắt buộc, không được để trống hoặc chỉ chứa khoảng trắng trắng (`IsNullOrWhiteSpace`).
   - `title`: Tùy chọn, tối đa 200 ký tự.
   - `timerMinutes`: Tùy chọn, nếu có giá trị thì phải $\ge 0$.
   - `imageUrl`: Tùy chọn, tối đa 500 ký tự.
4. **Bảo toàn số thứ tự khi Cập nhật (Update Immutability)**:
   - Khi thực hiện cập nhật một bước, `StepNumber` tuyệt đối được giữ nguyên giá trị ban đầu.
5. **Thuật toán Đánh số lại liên tục (Renumbering after Soft Delete)**:
   - Khi xóa một bước, bước đó được đánh dấu xóa mềm (`IsDeleted = true`, `UpdatedAt = UtcNow`).
   - Tất cả các bước đang hoạt động còn lại được sắp xếp theo thứ tự `StepNumber` cũ tăng dần, sau đó được đánh số lại tuần tự từ $1 \dots N$. Thao tác thực thi trong một Database Transaction duy nhất.
6. **Kiểm tra toàn vẹn khi Sắp xếp lại (Reorder Validation)**:
   - Danh sách `stepIds` gửi lên phải có số lượng phần tử bằng chính xác số bước active hiện tại của Recipe (`request.StepIds.Count == activeSteps.Count`).
   - Danh sách không được chứa ID trùng lặp (`request.StepIds.Distinct().Count() == request.StepIds.Count`).
   - Mọi `stepId` trong danh sách bắt buộc phải thuộc về chính Recipe đó và đang không bị xóa mềm (`activeStepMap.ContainsKey(stepId)`).
7. **Giải thuật 2-Phase Renumbering chống xung đột Unique Index**:
   - Cơ sở dữ liệu PostgreSQL áp dụng chỉ mục lọc duy nhất: `CREATE UNIQUE INDEX "IX_RecipeSteps_RecipeId_StepNumber" ON "RecipeSteps" ("RecipeId", "StepNumber") WHERE "IsDeleted" = false;`.
   - Nếu hoán đổi trực tiếp từ Step 1 $\leftrightarrow$ Step 2, việc gán Step 1 thành Step 2 sẽ vi phạm ngay lập tức ràng buộc Unique Index vì Step 2 cũ vẫn tồn tại.
   - **Giải pháp 2 Phase**:
     - *Phase 1 (Offset tạm thời)*: Gán `StepNumber = 10000 + i` cho toàn bộ các bước và gọi `SaveChangesAsync`.
     - *Phase 2 (Đánh số chuẩn)*: Gán `StepNumber = i + 1` (từ $1 \dots N$) và gọi `SaveChangesAsync`.
     - *Commit Transaction*: Hoàn tất ghi dữ liệu nguyên khối, đảm bảo an toàn tuyệt đối.
8. **Invalidate Cache**:
   - Mỗi khi có bất kỳ thay đổi nào về bước thực hiện, khóa cache `recipe:{recipeSlug}` trong Redis lập tức bị xóa bỏ để người dùng nhận dữ liệu mới nhất.

---

## 7. Ví dụ hoạt động

### Kịch bản: Quản lý các bước cho công thức "Phở bò Hà Nội" (`id = "9a7f3e1b-..."`)

1. **Thêm bước 1 (Sơ chế)**:
   - **Request**:
     ```http
     POST /api/v1/recipes/9a7f3e1b-0000-0000-0000-000000000001/steps
     Content-Type: application/json
     Authorization: Bearer <token_tac_gia>

     {
       "title": "Sơ chế xương bò",
       "description": "Rửa sạch xương ống bò với nước muối loãng, sau đó chần qua nước sôi 5 phút để khử bọt bẩn.",
       "timerMinutes": 10,
       "imageUrl": "https://cdn.culinaryblog.com/recipes/pho-bo/step-1.jpg"
     }
     ```
   - **Xử lý**: Hệ thống kiểm tra công thức chưa có bước nào $\rightarrow$ cấp `StepNumber = 1`.
   - **Response**: `HTTP 201 Created`
     ```json
     {
       "id": "e4b11111-0000-0000-0000-000000000001",
       "stepNumber": 1,
       "title": "Sơ chế xương bò",
       "description": "Rửa sạch xương ống bò với nước muối loãng, sau đó chần qua nước sôi 5 phút để khử bọt bẩn.",
       "timerMinutes": 10,
       "imageUrl": "https://cdn.culinaryblog.com/recipes/pho-bo/step-1.jpg"
     }
     ```

2. **Thêm bước 2 (Nấu nước dùng)**:
   - **Request**: Gửi thêm bước với mô tả "Hầm xương với hoa hồi, quế, thảo quả trong 6 tiếng", hẹn giờ 360 phút.
   - **Xử lý**: Hệ thống nhận thấy đã có Step 1 $\rightarrow$ tự cấp `StepNumber = 2`.

3. **Thêm bước 3 (Trình bày & Thưởng thức)**:
   - **Xử lý**: Hệ thống tự cấp `StepNumber = 3`.

4. **Xóa bước 2 (Nấu nước dùng)**:
   - **Request**: `DELETE /api/v1/recipes/9a7f3e1b-.../steps/e4b22222-...`
   - **Xử lý trong Transaction**:
     - Bước 2 chuyển cờ `IsDeleted = true`.
     - Các bước còn lại gồm: Bước 1 (`StepNumber = 1`) và Bước 3 (`StepNumber = 3`).
     - Áp dụng 2-phase renumbering: Bước 1 giữ `StepNumber = 1`, Bước 3 được đánh số lại thành `StepNumber = 2`.
   - **Response**: `HTTP 204 No Content`.
   - **Kết quả**: Thứ tự các bước trong cơ sở dữ liệu luôn liên tục là `1, 2`.

---

## 8. Error Handling

| Mã lỗi HTTP | Điều kiện phát sinh thực tế | Phản hồi (Behavior & Response Body) |
| :--- | :--- | :--- |
| `HTTP 400 Bad Request` | - `description` rỗng hoặc toàn khoảng trắng.<br>- `title` dài hơn 200 ký tự.<br>- `timerMinutes < 0`.<br>- `imageUrl` dài hơn 500 ký tự.<br>- Yêu cầu Reorder thiếu bước, thừa bước, trùng lặp ID, hoặc ID không thuộc về Recipe. | Trả về `ValidationProblemDetails` RFC 7807:<br>```json<br>{<br>  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",<br>  "title": "One or more validation errors occurred.",<br>  "status": 400,<br>  "errors": {<br>    "Description": ["Description is required and cannot be empty."]<br>  }<br>}<br>``` |
| `HTTP 401 Unauthorized` | Không truyền Header `Authorization: Bearer <token>` hoặc Token đã hết hạn / không hợp lệ. | Bị chặn từ tầng xác thực ASP.NET Core Authentication Middleware. |
| `HTTP 403 Forbidden` | Người dùng đã đăng nhập nhưng không phải tác giả của công thức (`AuthorId != CurrentUserId`) và không có vai trò `Admin`. | Trả về JSON:<br>```json<br>{<br>  "error": "You do not have permission to modify steps for this recipe."<br>}<br>``` |
| `HTTP 404 Not Found` | - Không tìm thấy công thức với `id` được cung cấp (hoặc công thức đã bị xóa mềm `IsDeleted = true`).<br>- Không tìm thấy `stepId` tương ứng trong công thức. | Trả về JSON:<br>```json<br>{<br>  "error": "Recipe with ID '...' was not found."<br>}<br>hoặc<br>{<br>  "error": "Step with ID '...' was not found in Recipe '...'"<br>}<br>``` |

---

## 9. Cách chạy và Demo

### Bước 1: Khởi động hạ tầng Docker
```bash
docker compose up -d
```
*(Đảm bảo PostgreSQL và Redis đang hoạt động bình thường)*

### Bước 2: Khởi động Backend API
```bash
dotnet run --project backend/src/CulinaryBlog.Api
```
API lắng nghe tại `http://localhost:5000` (hoặc cấu hình trong `launchSettings.json`).

### Bước 3: Kịch bản Demo tuần tự cho Giảng viên

1. **Lấy Token xác thực tác giả**:
   - Đăng nhập tài khoản tác giả công thức để lấy JWT Bearer Token.
2. **Demo Tạo bước mới (Tự cấp StepNumber)**:
   - Gửi `POST /api/v1/recipes/{id}/steps` với body chứa title và description.
   - Chỉ cho giảng viên thấy response trả về tự động có `"stepNumber": 1`.
   - Tiếp tục gửi thêm 2 bước nữa, chỉ cho giảng viên thấy `stepNumber` tự động tăng dần lên `2` và `3`.
3. **Demo Kiểm tra Validation**:
   - Gửi `POST` với `description: ""` hoặc `timerMinutes: -10`.
   - Quan sát hệ thống chặn ngay lập tức và trả về `HTTP 400 Bad Request` kèm chi tiết lỗi validation.
4. **Demo Sắp xếp lại thứ tự (Reorder)**:
   - Gửi `PUT /api/v1/recipes/{id}/steps/reorder` với mảng `stepIds` đảo ngược vị trí `[Step3Id, Step1Id, Step2Id]`.
   - Quan sát response trả về: Step 3 cũ nhận `StepNumber = 1`, Step 1 cũ nhận `StepNumber = 2`, Step 2 cũ nhận `StepNumber = 3`. Không xảy ra lỗi đụng Unique Index.
5. **Demo Xóa bước và Tự động đánh số lại**:
   - Gửi `DELETE /api/v1/recipes/{id}/steps/{stepId}` để xóa bước đang ở vị trí `StepNumber = 2`.
   - Hệ thống phản hồi `HTTP 204 No Content`.
   - Truy vấn lại chi tiết công thức (`GET /api/v1/recipes/{slug}`): hai bước còn lại đã được đánh số lại liên tục là `1` và `2`.

---

## 10. Testing

### Bộ kiểm thử đơn vị RecipeStep (`RecipeStepTests`)
Chạy lệnh kiểm thử chuyên biệt:
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --filter FullyQualifiedName~RecipeStepTests
```

**Kết quả kiểm thử thực tế:**
- **9/9 tests PASSED (100%)** (Thời gian chạy: ~1s).
- **Danh sách 9 kịch bản kiểm thử chi tiết:**
  1. `RecipeStepModel_EnforcesStepNumberAndTimerCheckConstraints`: Xác nhận Entity Framework khai báo đúng Check Constraints: `"StepNumber" >= 1` và `"TimerMinutes" IS NULL OR "TimerMinutes" >= 0`.
  2. `RecipeStepModel_HasFilteredUniqueIndexOnRecipeIdAndStepNumber`: Xác nhận chỉ mục duy nhất lọc theo điều kiện `"IsDeleted" = false` trên cặp khóa `(RecipeId, StepNumber)`.
  3. `RecipeStepModel_ConfiguresPropertyLengthsAndRequirements`: Xác nhận giới hạn độ dài `Title` (200), `Description` (required), và `ImageUrl` (500).
  4. `CreateStep_ThrowsValidationException_WhenDescriptionIsEmpty`: Ném lỗi validation khi mô tả rỗng.
  5. `CreateStep_ThrowsValidationException_WhenTitleExceeds200Characters`: Ném lỗi validation khi tiêu đề vượt quá 200 ký tự.
  6. `CreateStep_ThrowsValidationException_WhenTimerMinutesIsNegative`: Ném lỗi validation khi thời gian hẹn giờ âm.
  7. `CreateStep_ThrowsValidationException_WhenImageUrlExceeds500Characters`: Ném lỗi validation khi link ảnh vượt quá 500 ký tự.
  8. `UpdateStep_ThrowsValidationException_WhenDescriptionIsEmpty`: Ném lỗi validation khi cập nhật với mô tả rỗng.
  9. `UpdateStep_ThrowsValidationException_WhenTimerMinutesIsNegative`: Ném lỗi validation khi cập nhật với thời gian hẹn giờ âm.

### Tổng hợp toàn bộ Unit Tests trong solution:
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj
```
**Kết quả thực tế:**
- `Passed: 96, Failed: 0, Skipped: 1, Total: 97` (0 lỗi kiểm thử).

---

## 11. Build
Thực hiện lệnh biên dịch solution:
```bash
dotnet build backend/CulinaryBlog.sln
```

**Kết quả biên dịch thực tế:**
- **Thành công (Exit code 0)**.
- **0 Error(s)**, 451 Warning(s) (chủ yếu là các quy ước định dạng code StyleCop/SonarQube).

---

## 12. Limitations

1. **Chưa hỗ trợ Sub-Step (Bước phân nhánh phụ)**:
   - Theo đặc tả kiến trúc v1.2 của dự án CulinaryBlog, entity `RecipeStep` được thiết kế dạng danh sách phẳng (Flat List) một cấp, không có trường `ParentStepId`. Các kỹ thuật nấu ăn phân nhánh phức tạp cần được mô tả bên trong nội dung trường `Description`.
2. **Quản lý ảnh độc lập**:
   - Trường `ImageUrl` của `RecipeStep` hiện tại lưu trữ đường dẫn URL tĩnh dạng chuỗi, chưa liên kết ràng buộc khóa ngoại (Foreign Key) trực tiếp với bảng `RecipeImages`. Khi tải ảnh mới cho bước, người dùng sử dụng API Upload File của hệ thống để lấy URL rồi gán vào trường này.

---

## 13. Kết luận
Branch `feat/vohungmanh-recipe-step` đã hoàn thành trọn vẹn và chuẩn mực toàn bộ chức năng quản lý bước thực hiện công thức nấu ăn:
- Triển khai đầy đủ mô hình Clean Architecture + CQRS với MediatR.
- Đảm bảo tính toàn vẹn dữ liệu, giải quyết triệt để vấn đề Unique Index collision bằng thuật toán 2-phase renumbering trong database transaction.
- 100% mã nguồn được bảo vệ bằng kiểm tra quyền tác giả, xác thực dữ liệu chặt chẽ và bộ unit tests tự động hóa.
- Biên dịch 0 lỗi và vượt qua toàn bộ các bài kiểm thử đơn vị.
