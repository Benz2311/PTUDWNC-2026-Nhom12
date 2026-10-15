# Tài liệu API và hợp đồng Recipe cho Front-end

Tài liệu này mô tả các hạng mục API Documentation, TypeScript Types và thư
viện front-end đã được hoàn thiện cho dự án.

## OpenAPI và Scalar

API sử dụng OpenAPI generator tích hợp trong ASP.NET Core và package
`Scalar.AspNetCore` có sẵn trong dự án. Khi chạy ở môi trường Development,
tài liệu OpenAPI có tại `GET /openapi/v1.json` và giao diện tương tác Scalar
có tại `GET /scalar`.

Docker Compose mặc định chạy API ở môi trường Production. Để bật giao diện tài
liệu API cho stack Docker Compose, đặt `API_DOCUMENTATION_ENABLED=true` trong
file `.env` cục bộ rồi khởi động lại API. Hai endpoint tài liệu vẫn được tắt
theo mặc định ở Production.

Tài liệu khai báo xác thực HTTP Bearer bằng JWT. Các endpoint có yêu cầu xác
thực hiển thị security requirement Bearer và các phản hồi `401 Unauthorized`,
`403 Forbidden`. Endpoint công khai không yêu cầu access token. Trong Scalar,
sử dụng access token nhận được từ `POST /api/v1/auth/login`; chỉ nhập giá trị
token, không thêm tiền tố `Bearer `. Không lưu hoặc commit token thật vào mã
nguồn.

OpenAPI tạo thông tin request body, query/path parameter và response từ các
Minimal API handler cùng DTO. Các DTO request/response của Recipe có XML
summary để schema hiển thị dễ hiểu. Những thao tác Recipe có kiểm soát đồng
thời khai báo header `If-Match` bắt buộc. Gửi giá trị `ETag` mới nhất của
Recipe; nếu nhận `409 Conflict`, hãy tải lại Recipe trước khi thử lại. File
`.http` của API có các ví dụ request công khai và cần xác thực.

## Hợp đồng TypeScript cho Recipe

Frontend được Docker Compose sử dụng nằm trong thư mục `frontend/`. Các schema
và kiểu dữ liệu dùng chung được định nghĩa tại `frontend/types/recipe.ts`:

- `Recipe` / `RecipeDetail`: chi tiết Recipe, gồm nguyên liệu, các bước nấu,
  dinh dưỡng, ảnh, danh mục và tác giả.
- `RecipeSummary`: một Recipe trong kết quả danh sách và tìm kiếm.
- `RecipeIngredient`: dữ liệu nguyên liệu trả về từ API, sử dụng trường
  `sortOrder` (request gửi `orderIndex`).
- `RecipeNutrition`: các chỉ số dinh dưỡng dạng số có thể nhận `null`, gồm cả
  `Sodium`.
- `RecipeStatus`: `Draft`, `Published` hoặc `Archived`.
- `DifficultyLevel`: `Easy`, `Medium`, `Hard` hoặc `Expert`. Backend hiện có
  enum `Expert`, vì vậy giá trị này được giữ để bảo đảm tương thích.
- `RecipePage`: cấu trúc phân trang của API gồm `items`, `page`, `pageSize` và
  `totalCount`.

API tuần tự hóa enum thành chuỗi. Timestamp trong JSON được biểu diễn bằng
chuỗi trong TypeScript. Kiểu response phía client không khai báo
`RecipeNutrition.Source`, vì API hiện không trả trường do server quản lý này.

`recipeDraftRequestSchema` kiểm tra payload tạo/cập nhật ở ranh giới form và
phản ánh cấu trúc request hiện tại của API. Cụ thể, trình duyệt không tự gửi
`AuthorId` khi tạo Recipe; thứ tự nguyên liệu trong request được gửi bằng
`orderIndex`.

## Lựa chọn và phân loại thư viện front-end

Frontend đang chạy là `frontend/` (đây cũng là thư mục được Docker Compose
build). Dependency được phân loại trong `frontend/package.json` như sau:

| Package | Phân loại | Mục đích |
|---|---|---|
| `next`, `react`, `react-dom`, `zod` | dependencies | Runtime giao diện Next.js; Zod kiểm tra payload và hợp đồng Recipe |
| `@types/node`, `@types/react`, `@types/react-dom`, `typescript` | devDependencies | Trình biên dịch TypeScript và khai báo kiểu |
| `eslint`, `eslint-config-next`, `vitest` | devDependencies | Lint và kiểm thử hợp đồng API |

Frontend tiếp tục gọi API bằng wrapper `frontend/lib/api.ts` dựa trên `fetch`
có sẵn; wrapper này xử lý việc làm mới JWT, ETag và lỗi API. Form dùng state
được quản lý bởi React, giao diện dùng các component CSS hiện có. Không thêm
Axios, thư viện cache truy vấn, thư viện quản lý form hoặc UI kit vì dự án
không cần hành vi bổ sung đó; thêm chúng sẽ trùng lặp chức năng hiện tại.

## Cách kiểm tra

Chạy các lệnh sau từ thư mục gốc repository:

```powershell
dotnet build backend/CulinaryBlog.sln --nologo
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --nologo
Set-Location frontend
npm install
npm test
npm run build
```
