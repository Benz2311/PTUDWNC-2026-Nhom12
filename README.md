# Phát triển ứng dụng Web nâng cao

## Nhóm 12

### Thành viên

1. Võ Hùng Mạnh - 2312687 - 2312687@dlu.edu.vn - Nhóm trưởng
2. Lê Thị Ánh Nhung - 2312709 - 2312709@dlu.edu.vn
3. Phạm Nguyễn Ngọc Phước - 2312718 - 2312718@dlu.edu.vn
4. Nguyễn Văn Quốc - 2312729 - 2312729@dlu.edu.vn

---

## Quy tắc làm việc

1. Không push trực tiếp code chức năng lên `main`.
2. Luôn pull code mới nhất trước khi bắt đầu làm việc.
3. Mỗi task thực hiện trên branch riêng.
4. Không tự ý thay đổi cấu trúc project chung.
5. Không commit file `.env` chứa thông tin riêng của máy.
6. Commit message phải mô tả rõ thay đổi.
7. Kiểm tra code trước khi tạo Pull Request.
8. Khi push code cần tạo nhánh riêng từ nhánh cha và đặt tên theo chức năng đang thực hiện.

---

# Bữa 1 - Xây dựng cấu trúc dữ liệu ban đầu

**Mục tiêu:** Xác định các thành phần dữ liệu chính, thiết lập quan hệ và chuẩn bị dữ liệu nền cho hệ thống.

## Lê Thị Ánh Nhung - Category & Statistics

- Xác định cấu trúc dữ liệu liên quan đến danh mục món ăn và nhu cầu khai thác công thức.
- Hoàn thiện entity `Category` kế thừa `BaseEntity` với các thuộc tính: `Id`, `Name`, `Slug`, `Description`, `ImageUrl`, `OrderIndex`.
- Xây dựng Factory Method `Create(...)` có kiểm tra validation tên danh mục và tự động sinh slug hợp lệ bằng `SlugHelper`.
- Thiết lập mối quan hệ 1-N giữa `Category` và `Recipe` (`Category.Recipes` và `Recipe.Category`).
- Đóng gói tập hợp danh sách công thức thuộc danh mục thông qua `IReadOnlyCollection<Recipe>` để bảo toàn tính đóng gói trong Domain.
- Chuẩn bị cấu trúc dữ liệu phục vụ các tác vụ lọc danh mục, sắp xếp, phân trang và thống kê số lượng công thức (`RecipeCount`).
- Chuẩn bị danh sách dữ liệu mẫu danh mục đa dạng (món chính, món khai vị, tráng miệng, đồ uống...) phục vụ truy vấn và thống kê.

---

# Bữa 2 - Hoàn thiện cấu hình dữ liệu và tạo dữ liệu kiểm thử

**Mục tiêu:** Hoàn thiện Entity, Configuration và dữ liệu ngẫu nhiên cho từng chức năng; tích hợp vào DbContext và Migration để tạo cơ sở dữ liệu phục vụ kiểm thử.

## Lê Thị Ánh Nhung - Category & Statistics

- Hoàn thiện entity `Category` và cấu hình `CategoryConfiguration` ánh xạ bảng `Categories`.
- Cấu hình độ dài và ràng buộc thuộc tính: `Name` (max 100, Unique Index), `Slug` (max 120, Unique Index), `Description` (text), `ImageUrl` (max 500), `OrderIndex` (Index, Default 0).
- Cấu hình `RowVersion` làm Concurrency Token và Global Query Filter hỗ trợ xóa mềm: `HasQueryFilter(c => !c.IsDeleted)`.
- Cấu hình quan hệ Foreign Key giữa `Category` và `Recipe` với hành vi `DeleteBehavior.Restrict` nhằm chặn xóa danh mục khi còn công thức liên kết.
- Cấu hình chế độ truy cập trường ẩn (`PropertyAccessMode.Field`) cho collection `Recipes` để bảo đảm tính đóng gói của Domain Model.
- Xử lý các điểm chưa thống nhất giữa Entity và Configuration thuộc phạm vi Category.
- Xây dựng dữ liệu mẫu ngẫu nhiên cho danh mục.
- Đảm bảo cơ sở dữ liệu sau khi tích hợp có ít nhất **20 Categories** hoàn chỉnh phục vụ truy vấn và thống kê.

---

# Bữa 3 - Thiết kế dữ liệu và tối ưu truy vấn

**Mục tiêu:** Hoàn thiện Repository, Unit of Work, tối ưu các truy vấn dữ liệu, kiểm tra Index và triển khai Full-Text Search theo nội dung Chương 3; đảm bảo từng thành viên tiếp tục xử lý đúng nhóm Entity đã được phân công.

## Lê Thị Ánh Nhung - Category & Recipe Read

- Rà soát `Category` và toàn bộ các truy vấn đọc dữ liệu liên quan đến Recipe.
- Hoàn thiện `CategoryRepository` và `RecipeRepository` (phần đọc) kết hợp Unit of Work cho các nghiệp vụ truy vấn.
- Hoàn thiện các truy vấn danh sách Recipe, chi tiết Recipe theo Slug và danh sách Recipe theo Category.
- Hoàn thiện Filter (theo trạng thái, danh mục, độ khó), Sort (theo ngày tạo, ngày xuất bản) và Pagination (`Skip`/`Take`) cho danh sách Recipe.
- Sử dụng `AsNoTracking()` cho tất cả các truy vấn chỉ đọc để giảm thiểu chi phí theo dõi đối tượng của EF Core.
- Sử dụng Projection/DTO để chỉ trích xuất đúng các cột dữ liệu cần thiết từ PostgreSQL.
- Kiểm tra và xử lý triệt để N+1 Query Problem khi tải danh mục kèm số lượng công thức (`RecipeCount`) hoặc tải danh sách bài viết.
- Sử dụng `Include()` hợp lý và kết hợp `AsSplitQuery()` khi nạp nhiều quan hệ 1-N (Ingredients, Steps, Images) nhằm tránh bùng nổ dữ liệu tích Descartes (Cartesian Explosion).
- Kiểm tra các Index phục vụ truy vấn như `CategoryId`, `Status`, `CreatedAt` và `Slug`.
- Sử dụng `EXPLAIN ANALYZE` trên PostgreSQL để đánh giá execution plan và đo đạc chi phí thực tế của các truy vấn `GetAll`, `GetById` và `GetByCategory`.
- Viết tài liệu phân tích hiệu năng truy vấn trong `docs/database/explain-analyze-category-recipe.md`.
- Kiểm thử Filter, Sort, Pagination và các truy vấn đọc sau khi tối ưu.

---

# Bữa 4 - Hoàn thiện API Endpoints và Giao diện

**Mục tiêu:** Hoàn thiện các API Endpoint và giao diện tương ứng theo SRS v1.2.0; mỗi thành viên tiếp tục phụ trách đúng module/entity đã được giao, kết nối giao diện với API thật và kiểm thử các luồng chính trước khi tích hợp.

## Lê Thị Ánh Nhung - Category / Recipe Read / Dashboard

- Hoàn thiện API danh mục: Lấy danh sách danh mục (`GET /api/v1/categories`) kèm số lượng công thức (`recipeCount`) và xem chi tiết danh mục theo slug (`GET /api/v1/categories/{slug}`).
- Hoàn thiện các API quản trị danh mục dành cho Admin: Tạo danh mục (`POST /api/v1/categories`), Cập nhật danh mục (`PUT /api/v1/categories/{id}`) và Xóa danh mục (`DELETE /api/v1/categories/{id}`).
- Xử lý ràng buộc nghiệp vụ xóa danh mục: kiểm tra các công thức trực thuộc chưa bị xóa mềm, chặn xóa và trả về mã lỗi `409 Conflict` nếu danh mục còn công thức.
- Hoàn thiện API danh sách công thức (`GET /api/v1/recipes`) hỗ trợ phân trang (`page`, `pageSize`), bộ lọc (danh mục, độ khó, trạng thái), sắp xếp đa tiêu chí và hỗ trợ cờ `mine=true` để tác giả xem công thức của chính mình (hoặc Admin lọc theo `authorId`).
- Hoàn thiện API xem chi tiết công thức (`GET /api/v1/recipes/{slug}`): nạp đầy đủ thông tin danh mục, tác giả, nguyên liệu (theo `OrderIndex`), các bước nấu (theo `StepNumber`), bộ sưu tập ảnh (theo `OrderIndex` và đánh dấu `IsPrimary`) cùng thông tin dinh dưỡng.
- Bảo đảm quy tắc bảo mật trong API xem chi tiết: công thức `Published` được xem công khai; công thức `Draft` hoặc `Archived` chỉ cho phép tác giả sở hữu hoặc Admin truy cập.
- Hoàn thiện API thùng rác công thức cho Admin (`GET /api/v1/admin/recipes/trash`) truy vấn các công thức đã bị xóa mềm (`IsDeleted = true`), hỗ trợ phân trang và sắp xếp giảm dần theo thời gian xóa.
- Hoàn thiện API Dashboard/Thống kê (`GET /api/v1/dashboard/stats`): tổng số Category, tổng số Recipe, thống kê công thức theo trạng thái (Published/Draft/Archived), số lượng công thức theo từng danh mục, Top Category và biểu đồ công thức mới theo thời gian.
- Tối ưu truy vấn đọc, hạn chế tối đa N+1 Query và áp dụng bộ nhớ đệm phân tán Redis (TTL thích hợp cho danh mục và danh sách công thức public).
- Xây dựng giao diện Trang chủ và Khám phá công thức (Home/Recipe List UI) với các thẻ Recipe Card trực quan, thanh lọc danh mục, bộ sắp xếp và thanh phân trang.
- Xây dựng giao diện Chi tiết công thức (Recipe Detail UI) hiển thị đầy đủ thông tin bài viết, ảnh đại diện chính, thư viện ảnh phụ, danh sách nguyên liệu, các bước nấu có thời gian và bảng thông tin dinh dưỡng.
- Xây dựng giao diện Danh sách danh mục, Chi tiết danh mục và trang Quản lý danh mục dành cho Admin (Admin Category Management UI).
- Xây dựng giao diện Tổng quan Quản trị (Dashboard) với các thẻ tóm tắt số liệu (Summary Cards) và biểu đồ trực quan hóa dữ liệu thống kê từ API thật.
- Xây dựng giao diện Thùng rác công thức cho Admin (Admin Trash List UI) hiển thị danh sách bài viết đã xóa mềm (phần hành động Khôi phục/Xóa vĩnh viễn do TV3 phụ trách API).
- Phân định ranh giới rõ ràng: Giao diện `My Recipes` (danh sách công thức của tôi) sử dụng API đọc `GET /api/v1/recipes?mine=true` do Lê Thị Ánh Nhung phụ trách phần nạp dữ liệu đọc; các nút bấm thao tác tạo mới/chỉnh sửa do Phạm Nguyễn Ngọc Phước phụ trách.
- Xử lý các trạng thái giao diện: Loading skeleton, Empty state khi không có dữ liệu và hiển thị lỗi từ API trên tất cả các màn hình phụ trách.
- Kiểm thử Category CRUD, Recipe List/Detail, Filter/Sort/Pagination, Dashboard và Trash List.

---

# Bữa 5 - Hoàn thiện bảo mật, tài liệu API và nền tảng Front-end

**Mục tiêu:** Hoàn thành các cấu hình bảo mật gồm CORS, Rate Limiting và Authentication/Authorization; hoàn thiện tài liệu đặc tả API và API Versioning; đồng thời xây dựng môi trường, cấu trúc dự án Front-end, cài đặt các thư viện cần thiết và các kiểu dữ liệu TypeScript phục vụ ứng dụng.

## Lê Thị Ánh Nhung - Rate Limiting / Front-end Environment / Category Types

- Cấu hình Rate Limiting bằng cơ chế Rate Limiter của ASP.NET Core.
- Thiết lập policy giới hạn request cho các API thông thường.
- Kiểm tra trường hợp vượt giới hạn request trả về `429 Too Many Requests`.
- Khởi tạo và cấu hình môi trường phát triển Front-end bằng Next.js và TypeScript.
- Thiết lập các file cấu hình:
  - `package.json`
  - `tsconfig.json`
  - `next.config.*`
  - ESLint
  - `.env`
  - `.env.example`
- Thiết lập biến `NEXT_PUBLIC_API_URL` để Front-end kết nối Backend.
- Xây dựng các TypeScript Types thuộc phạm vi Category gồm:
  - `Category`
  - `CategoryDetail`
  - `CategoryListItem`
  - `CategoryRequest`
- Cài đặt các thư viện cơ bản cần thiết để chạy dự án Front-end.
- Kiểm tra Front-end khởi động thành công và có thể gọi Backend API.

---

# Bữa 6 - Hoàn thiện nền tảng Front-end, tiện ích dùng chung và giao diện tĩnh

**Mục tiêu:** Hoàn thiện các lớp tiện ích phục vụ xử lý lỗi và gọi API; cài đặt thư viện hỗ trợ thiết kế giao diện; xây dựng store quản lý trạng thái; hoàn thiện giao diện tĩnh, các component dùng chung và bố cục tổng thể cho Blog App nhằm chuẩn bị nền tảng cho các chức năng nghiệp vụ ở Bữa 7 và Bữa 8.

## Lê Thị Ánh Nhung - UI Library / Shared Components / Global Layout

- Cài đặt và cấu hình thư viện hỗ trợ thiết kế giao diện cho Front-end, ưu tiên `shadcn/ui` hoặc thư viện UI được nhóm thống nhất sử dụng trong toàn bộ Blog App.
- Thiết lập các component giao diện cơ bản như Button, Input, Textarea, Select, Dialog, Alert, Badge, Card, Skeleton và Dropdown Menu phục vụ các màn hình nghiệp vụ ở những bữa tiếp theo.
- Xây dựng các component dùng chung gồm `Header`, `Footer`, `Sidebar`, `Pagination`, `LoadingSpinner`, `LoadingSkeleton`, `ErrorState`, `EmptyState` và `ConfirmDialog`.
- Thiết kế `Header` hiển thị Logo/Tên Blog, liên kết Trang chủ, danh mục, khu vực tìm kiếm, các nút đăng nhập/đăng ký hoặc menu người dùng tùy theo trạng thái Authentication.
- Thiết kế `Footer` hiển thị thông tin dự án, liên kết điều hướng chính, thông tin nhóm và nội dung bản quyền cần thiết.
- Xây dựng `Sidebar` dùng chung cho các trang Public, chuẩn bị khu vực hiển thị danh sách Category và các liên kết điều hướng liên quan.
- Hoàn thiện component `Pagination` có khả năng nhận trang hiện tại, tổng số trang và callback hoặc URL chuyển trang để tái sử dụng cho Recipe List, Category, Search và Admin List.
- Hoàn thiện bố cục tổng thể của Blog App bằng Next.js Layout với Header phía trên, vùng nội dung chính, Sidebar ở các trang cần thiết và Footer phía dưới.
- Thiết kế giao diện tĩnh responsive cho Desktop, Tablet và Mobile; chuẩn hóa spacing, typography, container, card và grid để các thành viên khác sử dụng thống nhất.
- Chuẩn bị giao diện tĩnh cho Trang chủ, Category và các màn hình Public mà chưa gắn business logic hoặc dữ liệu thật.
- Không hard-code dữ liệu Recipe/Category trực tiếp trong Shared Components và không can thiệp logic nghiệp vụ của module thành viên khác.
- Kiểm tra khả năng tái sử dụng Shared Components và chạy TypeScript compile, lint, production build sau khi hoàn thiện UI Foundation.

---

# Bữa 7 - Hoàn thiện chức năng người dùng và các trang Public chính

**Mục tiêu:** Hoàn thiện các chức năng Authentication và Profile; xây dựng các trang Public chính của Blog App gồm Trang chủ, Danh mục, Chi tiết công thức và Tìm kiếm; kết nối giao diện với API thật và hoàn thiện đầy đủ các trạng thái Loading, Error, Empty, Filter và Pagination cần thiết.

## Lê Thị Ánh Nhung - Home / Category / Public Layout Integration

- Hoàn thiện Trang chủ của Blog App bằng cách tích hợp Header, Footer, Sidebar và Main Content từ các Shared Components đã xây dựng ở Bữa 6.
- Sidebar của Trang chủ hiển thị danh sách Category lấy từ API thật, không sử dụng dữ liệu hard-code.
- Main Content hiển thị danh sách Recipe Published mới nhất bằng `RecipeCard`, hỗ trợ page, pageSize và sắp xếp phù hợp theo API.
- Hoàn thiện các section Category nổi bật hoặc Recipe mới nếu API hiện tại cung cấp dữ liệu tương ứng.
- Xây dựng trang xem danh sách tất cả Category, hiển thị tên danh mục, hình ảnh, mô tả và `recipeCount`.
- Hoàn thiện trang Category Detail theo slug, hiển thị thông tin Category và danh sách Recipe thuộc Category.
- Tích hợp Pagination cho danh sách Recipe trên Trang chủ và Category Detail.
- Hoàn thiện liên kết điều hướng từ Category Sidebar/Category Card đến trang Category Detail và từ Recipe Card đến Recipe Detail.
- Tái sử dụng các Shared Components Loading Skeleton, Error State, Empty State và Pagination thay vì viết riêng cho từng màn hình.
- Bảo đảm Trang chủ và Category hoạt động responsive trên Desktop, Tablet và Mobile.
- Kiểm tra người dùng chưa đăng nhập vẫn có thể truy cập Trang chủ, danh sách Category và các Recipe Published.
- Không hard-code dữ liệu Category hoặc Recipe trong UI.
- Kiểm thử việc tải Trang chủ, danh sách Category, Category Detail, Pagination, Empty State và API Error State.

---

# Bữa 8 - Hoàn thiện quản lý Recipe, Upload Image, Admin và Logging

**Mục tiêu:** Hoàn thiện các chức năng nâng cao gồm upload ảnh, tạo/cập nhật và quản lý vòng đời Recipe, quản trị người dùng, Category, Logging và các chức năng còn thiếu theo SRS; tích hợp đầy đủ các module trước khi thực hiện tổng kiểm thử toàn hệ thống.

## Lê Thị Ánh Nhung - Category Completion / Home & Layout Finalization

- Hoàn thiện các chức năng Category còn thiếu theo SRS, bao gồm giao diện quản lý Category dành cho Admin.
- Xây dựng và hoàn thiện chức năng tạo Category với các trường Name, Description, ImageUrl, OrderIndex và các trường Backend thực tế yêu cầu.
- Xây dựng chức năng cập nhật Category và đồng bộ dữ liệu mới trên UI sau khi thao tác thành công.
- Xây dựng chức năng xóa Category, hiển thị Confirmation Dialog và xử lý lỗi `409 Conflict` khi Category vẫn còn Recipe liên kết.
- Rà soát Category List và Category Detail sau khi Recipe Create/Edit được tích hợp, bảo đảm `recipeCount` được cập nhật đúng.
- Rà soát Trang chủ sau khi các chức năng Recipe hoàn thiện, kiểm tra danh sách Recipe mới, Sidebar Category, Pagination, Header và Footer.
- Hoàn thiện giao diện tổng thể Blog App về responsive, spacing, typography, card layout và mobile navigation.
- Rà soát các Shared Components gồm Header, Footer, Sidebar, Pagination, Loading, Error và Empty State để bảo đảm cách hiển thị thống nhất giữa các page.
- Hoàn thiện các chức năng Category, Recipe Read hoặc Dashboard còn thiếu theo SRS thuộc phạm vi Lê Thị Ánh Nhung.
- Không can thiệp business logic Recipe Create/Edit thuộc Phạm Nguyễn Ngọc Phước.
- Kiểm thử Category CRUD, Home, Layout và responsive sau khi tích hợp toàn hệ thống.

---

# Cài đặt PostgreSQL bằng Docker

Project sử dụng **PostgreSQL 16** chạy bằng Docker.

Cả nhóm sử dụng chung file `docker-compose.yml`. Mỗi thành viên có file `.env` riêng trên máy để thiết lập mật khẩu và port PostgreSQL.

---

## 1. Yêu cầu

Trước khi chạy project cần cài:

- Docker Desktop
- Git
- pgAdmin 4 *(nếu muốn quản lý database bằng giao diện)*

Mở Docker Desktop và chờ Docker Engine chạy.

Có thể kiểm tra bằng:

```powershell
docker info
```

Nếu Docker hoạt động bình thường, lệnh sẽ hiển thị thông tin của cả **Client** và **Server**.

---

## 2. Lấy code mới nhất

### Trường hợp chưa có project

Clone repository:

```powershell
git clone <URL_REPOSITORY>
cd PTUDWNC-2026-Nhom12
```

### Trường hợp đã có project

Lấy code mới nhất:

```powershell
git pull
```

---

## 3. Tạo file `.env`

Project có sẵn file:

```text
.env.example
```

File này chỉ chứa cấu hình mẫu và được đưa lên Git.

Mỗi thành viên cần tạo một file `.env` riêng trên máy.

Trên PowerShell:

```powershell
Copy-Item .env.example .env
```

Sau đó mở file `.env` và cấu hình.

Ví dụ:

```env
POSTGRES_DB=culinary_blog
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_password
POSTGRES_PORT=5432
```

### Ý nghĩa các biến

| Biến | Ý nghĩa |
|---|---|
| `POSTGRES_DB` | Tên database của project |
| `POSTGRES_USER` | Tài khoản PostgreSQL |
| `POSTGRES_PASSWORD` | Mật khẩu PostgreSQL do mỗi thành viên tự đặt |
| `POSTGRES_PORT` | Port PostgreSQL được mở trên máy |

Ví dụ cấu hình thực tế:

```env
POSTGRES_DB=culinary_blog
POSTGRES_USER=postgres
POSTGRES_PASSWORD=abc123
POSTGRES_PORT=5432
```

> **Lưu ý:** Không commit file `.env` lên Git.

File `.env` đã được thêm vào `.gitignore`.

---

## 4. Trường hợp port `5432` đã được sử dụng

Một số máy đã cài PostgreSQL trực tiếp nên port `5432` có thể đang được sử dụng.

Khi đó chỉ cần đổi trong `.env`:

```env
POSTGRES_PORT=5433
```

Docker sẽ kết nối theo dạng:

```text
localhost:5433 -> PostgreSQL Docker:5432
```

Không cần sửa `docker-compose.yml`.

Ví dụ `.env`:

```env
POSTGRES_DB=culinary_blog
POSTGRES_USER=postgres
POSTGRES_PASSWORD=abc123
POSTGRES_PORT=5433
```

Mỗi thành viên có thể sử dụng port khác nhau tùy cấu hình máy.

---

## 5. Khởi động PostgreSQL

Tại thư mục gốc của project chạy:

```powershell
docker compose up -d
```

Kiểm tra:

```powershell
docker compose ps
```

Nếu sử dụng port `5432`, kết quả sẽ có dạng:

```text
0.0.0.0:5432->5432/tcp
```

Nếu sử dụng port `5433`:

```text
0.0.0.0:5433->5432/tcp
```

Container PostgreSQL của project có tên:

```text
culinary-blog-postgres
```

---

## 6. Kiểm tra database

Kiểm tra danh sách database bên trong container:

```powershell
docker exec -it culinary-blog-postgres psql -U postgres -l
```

Nếu khởi tạo thành công sẽ có database:

```text
culinary_blog
```

Có thể truy cập trực tiếp database bằng:

```powershell
docker exec -it culinary-blog-postgres psql -U postgres -d culinary_blog
```

Khi thấy:

```text
culinary_blog=#
```

nghĩa là đã truy cập được PostgreSQL.

Để thoát:

```text
\q
```

Nếu màn hình đang hiển thị danh sách và phía dưới có:

```text
(END)
```

nhấn phím:

```text
q
```

để quay lại terminal.

---

# Kết nối PostgreSQL Docker bằng pgAdmin 4

## 1. Tạo Server

Trong pgAdmin chọn:

```text
Servers
→ Register
→ Server...
```

### Tab General

Nhập:

```text
Name: Culinary Blog Docker
```

### Tab Connection

Nhập:

```text
Host name/address: localhost
Port: POSTGRES_PORT trong file .env
Maintenance database: culinary_blog
Username: postgres
Password: POSTGRES_PASSWORD trong file .env
```

Có thể bật:

```text
Save password
```

Sau đó chọn **Save**.

---

## 2. Ví dụ kết nối

Nếu `.env`:

```env
POSTGRES_DB=culinary_blog
POSTGRES_USER=postgres
POSTGRES_PASSWORD=abc123
POSTGRES_PORT=5433
```

thì pgAdmin sử dụng:

```text
Host: localhost
Port: 5433
Maintenance database: culinary_blog
Username: postgres
Password: abc123
```

Sau khi kết nối thành công sẽ thấy:

```text
Culinary Blog Docker
└── Databases
    ├── culinary_blog
    └── postgres
```

---

# Quản lý Docker

## Kiểm tra container

```powershell
docker compose ps
```

## Dừng container nhưng giữ database

```powershell
docker compose down
```

Dữ liệu PostgreSQL vẫn được giữ trong Docker volume.

## Khởi động lại

```powershell
docker compose up -d
```

## Xem log PostgreSQL

```powershell
docker compose logs postgres
```

Theo dõi log liên tục:

```powershell
docker compose logs -f postgres
```

---

# Reset database

Chỉ sử dụng khi thật sự muốn xóa database local và tạo lại từ đầu:

```powershell
docker compose down -v
docker compose up -d
```

> **Lưu ý:** `docker compose down -v` sẽ xóa Docker volume và toàn bộ dữ liệu PostgreSQL local.

Không sử dụng lệnh này nếu database đang có dữ liệu cần giữ.

---

# Cấu hình Docker Compose

File `docker-compose.yml` của project sử dụng các biến trong `.env`:

```yaml
services:
  postgres:
    image: postgres:16
    container_name: culinary-blog-postgres
    restart: unless-stopped

    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}

    ports:
      - "${POSTGRES_PORT}:5432"

    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

> Không ghi trực tiếp mật khẩu cá nhân vào `docker-compose.yml`.

---

# Cấu hình `.env.example`

File `.env.example` được commit lên Git để các thành viên sử dụng làm mẫu:

```env
POSTGRES_DB=culinary_blog
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_password
POSTGRES_PORT=5432
```

Mỗi thành viên copy file này thành `.env` rồi thay đổi thông tin phù hợp với máy của mình.

---

# Một số lỗi thường gặp

## 1. Docker Engine chưa chạy

Nếu gặp lỗi dạng:

```text
failed to connect to the docker API
dockerDesktopLinuxEngine
```

Mở Docker Desktop và chờ Docker Engine chạy.

Sau đó kiểm tra:

```powershell
docker info
```

---

## 2. Port `5432` bị trùng

Nếu máy đang có PostgreSQL khác chạy trên port `5432`, đổi trong `.env`:

```env
POSTGRES_PORT=5433
```

Sau đó chạy lại:

```powershell
docker compose down
docker compose up -d
```

Kiểm tra:

```powershell
docker compose ps
```

---

## 3. Thay đổi password nhưng password mới không hoạt động

Các biến:

```text
POSTGRES_DB
POSTGRES_USER
POSTGRES_PASSWORD
```

được PostgreSQL sử dụng khi database được khởi tạo lần đầu.

Nếu Docker volume cũ đã tồn tại, chỉ sửa `.env` sẽ không tự thay đổi User/Password bên trong database cũ.

Trong trường hợp database local chưa có dữ liệu cần giữ và muốn khởi tạo lại hoàn toàn:

```powershell
docker compose down -v
docker compose up -d
```

> **Cảnh báo:** Lệnh `docker compose down -v` sẽ xóa database local hiện tại.

---

# Lưu ý khi làm việc nhóm

- Cả nhóm sử dụng chung `docker-compose.yml`.
- Mỗi thành viên sử dụng `.env` riêng.
- Không push `.env` lên Git.
- Không đưa password cá nhân vào `docker-compose.yml`.
- `.env.example` được phép push lên Git vì chỉ chứa cấu hình mẫu.
- Tên database của project thống nhất là `culinary_blog`.
- Port có thể khác nhau trên từng máy.
- Trước khi làm task mới cần pull code mới nhất.
- Mỗi chức năng thực hiện trên branch riêng.
- Không sử dụng `docker compose down -v` nếu đang có dữ liệu cần giữ.
