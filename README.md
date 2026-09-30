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

- Xác định dữ liệu liên quan đến danh mục và khai thác công thức.
- Hoàn thiện `Category` và các thông tin cần thiết.
- Thiết lập liên kết giữa `Category` và `Recipe`.
- Chuẩn bị dữ liệu phục vụ lọc, sắp xếp, phân trang và thống kê.
- Chuẩn bị dữ liệu mẫu danh mục phục vụ truy vấn và thống kê.
---

# Bữa 2 - Hoàn thiện cấu hình dữ liệu và tạo dữ liệu kiểm thử

**Mục tiêu:** Hoàn thiện Entity, Configuration và dữ liệu ngẫu nhiên cho từng chức năng; tích hợp vào DbContext và Migration để tạo cơ sở dữ liệu phục vụ kiểm thử.

## Lê Thị Ánh Nhung - Category & Statistics

- Hoàn thiện `Category` và Configuration tương ứng.
- Kiểm tra và hoàn thiện quan hệ giữa `Category` và `Recipe`.
- Xử lý các điểm chưa thống nhất giữa Entity và Configuration thuộc chức năng Category.
- Xây dựng dữ liệu ngẫu nhiên cho danh mục.
- Đảm bảo cơ sở dữ liệu sau khi tích hợp có ít nhất **20 Categories** phục vụ truy vấn và thống kê.
---

# Bữa 3 - Thiết kế dữ liệu và tối ưu truy vấn

**Mục tiêu:** Hoàn thiện Repository, Unit of Work, tối ưu các truy vấn dữ liệu, kiểm tra Index và triển khai Full-Text Search theo nội dung Chương 3; đảm bảo từng thành viên tiếp tục xử lý đúng nhóm Entity đã được phân công.

## Lê Thị Ánh Nhung - Category & Recipe Read

- Rà soát `Category` và các truy vấn đọc dữ liệu liên quan đến Recipe.
- Hoàn thiện các truy vấn danh sách Recipe, chi tiết Recipe và Recipe theo Category.
- Hoàn thiện Filter, Sort và Pagination cho danh sách Recipe.
- Sử dụng `AsNoTracking()` cho các truy vấn chỉ đọc.
- Sử dụng Projection/DTO để chỉ lấy các trường dữ liệu cần thiết.
- Kiểm tra và xử lý N+1 Query Problem trong các truy vấn Category và Recipe.
- Sử dụng `Include()` và `AsSplitQuery()` khi cần lấy nhiều dữ liệu liên quan.
- Kiểm tra các Index phục vụ truy vấn như `CategoryId`, `Status`, `CreatedAt` và `Slug`.
- Sử dụng `EXPLAIN ANALYZE` để kiểm tra các truy vấn `GetAll`, `GetById` và `GetByCategory`.
- Kiểm thử Filter, Sort, Pagination và các truy vấn đọc sau khi tối ưu.
---

# Bữa 4 - Hoàn thiện API Endpoints

## Mục tiêu

Hoàn thành việc cài đặt tất cả API Endpoints.

Việc triển khai Backend tuân theo tài liệu SRS v1.2.0 của dự án.

### Nguyên tắc phân công

- Mỗi thành viên tiếp tục phụ trách đúng module/entity đã được giao.
- Hạn chế chỉnh sửa trực tiếp phần code của thành viên khác.
- Tất cả API phải tuân theo Clean Architecture + CQRS + MediatR.
- Validation thực hiện bằng FluentValidation.
- API lỗi trả theo RFC 7807.
- Mỗi endpoint phải có ít nhất:
  - 1 Happy Path.
  - 1 Error Case.
- Kiểm thử API bằng Scalar.
- Kiểm tra dữ liệu thực tế trên PostgreSQL sau các thao tác ghi.

---

# Lê Thị Ánh Nhung - Category / Recipe Read / Dashboard

## Entity phụ trách

- `Category`
- Được phép đọc `Recipe` phục vụ:
  - List.
  - Detail.
  - Filter.
  - Sort.
  - Pagination.
  - Statistics.

> Thành viên 2 không phụ trách nghiệp vụ tạo/cập nhật trạng thái Recipe.

## API Endpoints

- [ ] `GET /api/v1/categories`
- [ ] `GET /api/v1/categories/{slug}`
- [ ] `POST /api/v1/categories`
- [ ] `PUT /api/v1/categories/{id}`
- [ ] `DELETE /api/v1/categories/{id}`
- [ ] `GET /api/v1/recipes`
- [ ] `GET /api/v1/recipes/{slug}`
- [ ] `GET /api/v1/admin/recipes/trash`
- [ ] API Dashboard/Statistics của nhóm.

## Công việc chi tiết

### Category List

- [ ] Query tất cả Category chưa bị xóa.
- [ ] `AsNoTracking()`.
- [ ] Sort theo Name.
- [ ] Tính `recipeCount` chỉ với Recipe Published.
- [ ] Redis Cache.
- [ ] Cache key `categories:all`.
- [ ] TTL 30 phút.

### Category Detail

- [ ] Tìm Category theo Slug.
- [ ] Không tồn tại → `404`.
- [ ] Trả Recipe thuộc Category.
- [ ] Pagination.
- [ ] Guest chỉ thấy Published.
- [ ] Author có thể thấy thêm Recipe Draft của chính mình.

### Create Category

- [ ] Chỉ Admin.
- [ ] Validate Name.
- [ ] Generate Slug.
- [ ] Nếu Slug trùng → tự thêm suffix.
- [ ] Kiểm tra Name duplicate.
- [ ] Save Database.
- [ ] Invalidate Redis Cache.
- [ ] Trả `201 Created`.

### Update Category

- [ ] Chỉ Admin.
- [ ] Update Name.
- [ ] Update Description.
- [ ] Không thay đổi Slug khi đổi Name.
- [ ] Invalidate Cache.

### Delete Category

- [ ] Chỉ Admin.
- [ ] Kiểm tra Recipe chưa Soft Delete.
- [ ] Nếu còn Recipe → `409`.
- [ ] Nếu không còn → Soft Delete Category.

### Recipe List

- [ ] Public chỉ trả Recipe `Published`.
- [ ] Hỗ trợ Pagination.
- [ ] Hỗ trợ Category Filter.
- [ ] Hỗ trợ Difficulty Filter.
- [ ] Hỗ trợ các Filter theo SRS.
- [ ] Hỗ trợ Sort.
- [ ] Hỗ trợ `mine=true`.
- [ ] Khi `mine=true`, Author xem Recipe của chính mình.
- [ ] Hỗ trợ Status Filter khi xem Recipe của mình.
- [ ] Admin được Filter bằng `authorId`.
- [ ] Author thường không được dùng `authorId`.
- [ ] Public List Redis Cache 1 phút.
- [ ] Private List không dùng Shared Cache.

### Recipe Detail

- [ ] Load Recipe.
- [ ] Load Category.
- [ ] Load Author.
- [ ] Load Ingredients.
- [ ] Load Steps.
- [ ] Load Images.
- [ ] Load Nutrition.
- [ ] Steps sort theo `StepNumber`.
- [ ] Ingredients sort theo `OrderIndex`.
- [ ] Images sort theo `OrderIndex`.
- [ ] `AsNoTracking()`.
- [ ] Sử dụng Split Query khi phù hợp.
- [ ] Kiểm tra không xảy ra N+1 Query.
- [ ] Published → Public.
- [ ] Draft/Archived → chỉ Owner/Admin.
- [ ] Public Recipe Detail Cache 5 phút.

### Admin Trash List

- [ ] Chỉ Admin.
- [ ] Query Recipe có `IsDeleted = true`.
- [ ] Ignore Global Query Filter có kiểm soát.
- [ ] Sort `DeletedAt DESC`.
- [ ] Pagination.
- [ ] Không Restore/Purge ở phần TV2.

### Dashboard / Statistics

- [ ] Tổng số Category.
- [ ] Tổng số Recipe.
- [ ] Tổng số Published Recipe.
- [ ] Tổng số Draft Recipe.
- [ ] Tổng số Archived Recipe.
- [ ] Số Recipe theo Category.
- [ ] Top Category có nhiều Recipe.
- [ ] Tỷ lệ Recipe theo Category.
- [ ] Recipe mới theo thời gian.
- [ ] Projection DTO.
- [ ] `AsNoTracking()`.
- [ ] Không N+1 Query.

## Kiểm thử bắt buộc

- [ ] Category List.
- [ ] Category Detail.
- [ ] Create Category.
- [ ] Update Category.
- [ ] Delete Category.
- [ ] Delete Category còn Recipe → `409`.
- [ ] Redis Cache Hit/Miss.
- [ ] Cache Invalidation.
- [ ] Recipe List.
- [ ] Pagination.
- [ ] Filter.
- [ ] Sort.
- [ ] `mine=true`.
- [ ] Admin `authorId`.
- [ ] Recipe Detail Published.
- [ ] Recipe Detail Draft.
- [ ] Recipe Detail Archived.
- [ ] Admin Trash.
- [ ] Dashboard Statistics.
- [ ] Kiểm tra SQL Log / N+1.

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