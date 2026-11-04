# TASK 4 (BỮA 4) – QUẢN LÝ BƯỚC THỰC HIỆN CÔNG THỨC (RECIPE STEP BACKEND)

**Sinh viên:** Võ Hùng Mạnh
**MSSV:** 2312687
**Nhóm:** 12
**Nhánh:** `feat/vohungmanh-recipe-step`

---

### 1. Mục tiêu Task 4 liên quan đến nhánh
Trong khuôn khổ Bữa 4 (Task 4), module Quản lý các bước thực hiện công thức nấu ăn (`RecipeStep`) cần được hoàn thiện toàn diện ở tầng Backend, cung cấp đầy đủ các thao tác CRUD, logic sắp xếp thứ tự bước nấu (Reorder), ràng buộc dữ liệu toàn vẹn, bảo vệ chỉ mục duy nhất và xử lý tự động đánh số lại bước (Re-indexing) khi xóa.

### 2. Công việc cá nhân thực tế
- Thiết kế và triển khai các Command & Handler theo mô hình CQRS qua MediatR:
  - `CreateRecipeStepCommand`: Tạo mới bước nấu ăn kèm số thứ tự.
  - `UpdateRecipeStepCommand`: Cập nhật chi tiết nội dung bước nấu, hẹn giờ và ảnh đính kèm.
  - `DeleteRecipeStepCommand`: Xóa bước nấu và tự động re-index thứ tự các bước còn lại.
  - `ReorderRecipeStepsCommand`: Đổi vị trí hàng loạt các bước nấu ăn an toàn.
- Xây dựng 4 Minimal API endpoints trong `RecipeEndpoints.cs` phục vụ cho việc thao tác với các bước nấu.
- Đóng gói logic kiểm tra tính hợp lệ dữ liệu (Domain Validation): tiêu đề (tối đa 200 ký tự), mô tả (tối đa 4000 ký tự), thời gian timer không âm, URL ảnh hợp lệ.
- Viết 160 dòng mã Unit Test trong `RecipeStepTests.cs` và 273 dòng kịch bản kiểm thử tích hợp trong `CulinaryBlog.IntegrationTests/Program.cs`.

### 3. Chức năng đã hoàn thành
- **Tạo bước nấu mới (`POST /api/v1/recipes/{id}/steps`):** Hỗ trợ thêm bước mới vào công thức, tự động tính toán `StepNumber` tiếp theo nếu không chỉ định.
- **Cập nhật bước nấu (`PUT /api/v1/recipes/{id}/steps/{stepId}`):** Cho phép sửa tiêu đề, mô tả hướng dẫn, thời gian hẹn giờ (`TimerMinutes`) và URL hình ảnh minh họa bước nấu.
- **Sắp xếp lại thứ tự (`PUT /api/v1/recipes/{id}/steps/reorder`):** Nhận danh sách các cặp `{ stepId, newOrder }`, thực thi cập nhật thứ tự trong một Database Transaction duy nhất, tránh vi phạm Unique Index `(RecipeId, StepNumber)`.
- **Xóa bước nấu & Tự động Re-index (`DELETE /api/v1/recipes/{id}/steps/{stepId}`):** Khi xóa một bước ở giữa (ví dụ bước 2 trên tổng số 4 bước), hệ thống tự động dịch chuyển các bước phía sau (bước 3 -> 2, bước 4 -> 3) để đảm bảo chuỗi thứ tự luôn liên tục 1..N.

### 4. Cách xử lý và luồng hoạt động
```text
Client Request (Thêm / Sửa / Xóa / Reorder Step)
        │
        ▼
RecipeEndpoints (Validate Route Guid, Deserialize DTO)
        │
        ▼
MediatR -> RecipeStepCommandHandler
        │
        ├── Kiểm tra sự tồn tại của Recipe trong Database
        ├── Thực thi nghiệp vụ trên Domain Entity RecipeStep
        ├── Xử lý sắp xếp / đánh số lại StepNumber (nếu xóa hoặc reorder)
        ├── Kiểm tra vi phạm ràng buộc Unique Index (RecipeId, StepNumber)
        └── UnitOfWork.SaveChangesAsync() commit transaction
        │
        ▼
Response JSON (200 OK / 201 Created kèm dữ liệu bước nấu đã cập nhật)
```

### 5. Danh sách file mã nguồn quan trọng

| Đường dẫn file | Vai trò và chức năng chính |
| :--- | :--- |
| `backend/src/CulinaryBlog.Application/Features/Recipes/Commands/RecipeSteps/RecipeStepCommands.cs` | 374 dòng mã định nghĩa các records Command và Handler xử lý Create, Update, Delete, Reorder Step. |
| `backend/src/CulinaryBlog.Api/Endpoints/Recipes/RecipeEndpoints.cs` | 223 dòng mã ánh xạ các HTTP endpoints CRUD và Reorder cho RecipeStep. |
| `backend/src/CulinaryBlog.Domain/Entities/RecipeStep.cs` | Domain Entity chứa validation logic, factory methods và phương thức cập nhật nội dung. |
| `backend/src/CulinaryBlog.Infrastructure/Data/Configurations/RecipeStepConfiguration.cs` | Cấu hình bảng `recipe_steps`, khóa ngoại `RecipeId` và Unique Index `(RecipeId, StepNumber)`. |
| `backend/tests/CulinaryBlog.UnitTests/RecipeStepTests.cs` | 160 dòng kiểm thử đơn vị kiểm tra validation, số thứ tự và các nghiệp vụ của Entity. |
| `backend/tests/CulinaryBlog.IntegrationTests/Program.cs` | 273 dòng kiểm thử tích hợp tự động qua HTTP API trực tiếp với PostgreSQL. |
| `docs/README_RECIPE_STEP.md` | Tài liệu kỹ thuật chuyên sâu về module RecipeStep Backend. |
| `docs/VO_HUNG_MANH_RECIPE_STEP_COMPLAN.md` | Kế hoạch triển khai và đặc tả chi tiết các trường hợp kiểm thử Bữa 4. |

### 6. API, Database và Infrastructure liên quan
- **Cơ sở dữ liệu:** PostgreSQL 16 (Bảng `recipe_steps`).
- **Ràng buộc Database:** Unique Index trên `(recipe_id, step_number)`, Foreign Key xóa tầng (`ON DELETE CASCADE`).
- **REST Endpoints:**
  - `POST /api/v1/recipes/{id}/steps`: Tạo bước nấu mới.
  - `PUT /api/v1/recipes/{id}/steps/{stepId}`: Cập nhật bước nấu.
  - `PUT /api/v1/recipes/{id}/steps/reorder`: Sắp xếp lại danh sách các bước.
  - `DELETE /api/v1/recipes/{id}/steps/{stepId}`: Xóa bước nấu.

### 7. Hướng dẫn chạy

#### Bước 1: Khởi động cơ sở dữ liệu PostgreSQL
```bash
docker compose up -d postgres
```

#### Bước 2: Chạy Backend Web API
```bash
cd backend/src/CulinaryBlog.Api
dotnet run
```

### 8. Hướng dẫn kiểm thử

#### Kiểm thử Unit Test tự động
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --filter "FullyQualifiedName~RecipeStep"
```

#### Kiểm thử tích hợp tự động (Integration Test)
```bash
dotnet run --project backend/tests/CulinaryBlog.IntegrationTests/CulinaryBlog.IntegrationTests.csproj
```

#### Kiểm thử trực tiếp qua cURL
```bash
# Thêm bước nấu vào công thức
curl -X POST "http://localhost:5000/api/v1/recipes/{recipeId}/steps" \
  -H "Content-Type: application/json" \
  -d '{"title":"Sơ chế nguyên liệu","description":"Rửa sạch thịt bò và hành ngò","timerMinutes":15}'
```

### 9. Kết quả kiểm thử có bằng chứng
- **Kết quả Unit Test:** Toàn bộ test case trong `RecipeStepTests` đều đạt `Passed!` (kiểm thử thành công tiêu đề quá dài, mô tả trống, timer âm và tạo hợp lệ).
- **Kết quả Integration Test:** Toàn bộ kịch bản kiểm thử API Step trong `CulinaryBlog.IntegrationTests` chạy thành công 100%, ghi nhận dữ liệu chính xác trong PostgreSQL.

### 10. Hạn chế và tồn đọng
- **Trạng thái thực tế:** Hoàn thiện 100% backend API, logic reorder và database constraints.
- **Tồn đọng:** Giao diện kéo thả trực quan (Drag-and-drop Step Item) và tính năng đếm ngược đồng hồ (Interactive Cooking Mode Timer) phía Frontend được phân bổ thực hiện ở Task 6 và hoàn thiện tối ưu tại Task 8.

### 11. Liên kết tài liệu kỹ thuật chi tiết
- Chi tiết API và nghiệp vụ Step: [docs/README_RECIPE_STEP.md](docs/README_RECIPE_STEP.md)
- Kế hoạch triển khai và ma trận kiểm thử: [docs/VO_HUNG_MANH_RECIPE_STEP_COMPLAN.md](docs/VO_HUNG_MANH_RECIPE_STEP_COMPLAN.md)

---

# Phát triển ứng dụng Web nâng cao

## Nhóm 12

### Thành viên
1. Võ Hùng Mạnh - 2312687 - 2312687@dlu.edu.vn - Nhóm trưởng
2. Lê Thị Ánh Nhung - 2312709 - 2312709@dlu.edu.vn
3. Phạm Nguyễn Ngọc Phước - 2312718 - 2312718@dlu.edu.vn
4. Nguyễn Văn Quốc - 2312729 - 2312729@dlu.edu.vn
