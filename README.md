# Phát triển ứng dụng web nâng cao 
# ##  Nhóm 12 thành viên gồm :
1. Võ Hùng Mạnh - 2312687 - 2312687@dlu.edu.vn ( Nhóm trưởng )
2. Lê Thị Ánh Nhung - 2312709 - 2312709@dlu.edu.vn
3. Phạm Nguyễn Ngọc Phước - 2312718 - 2312718@dlu.edu.vn
4. Nguyễn Văn Quốc - 2312729 - 2312729@dlu.edu.vn
# ## Quy tắc làm việc
1. Không push trực tiếp code chức năng lên `main`.
2. Luôn pull code mới nhất trước khi bắt đầu làm việc.
3. Mỗi task thực hiện trên branch riêng.
4. Không tự ý thay đổi cấu trúc project chung.
5. Không commit file `.env` chứa thông tin bí mật.
6. Commit message phải mô tả rõ thay đổi.
7. Kiểm tra code trước khi tạo Pull Request.
8. Khi push lên nhớ tạo nhánh con từ nhánh cha ( nhớ ghi chức năng mình làm )


# Bữa 1 - Xây dựng cấu trúc dữ liệu ban đầu

**Mục tiêu:** Xác định các thành phần dữ liệu chính, thiết lập quan hệ và chuẩn bị dữ liệu nền cho hệ thống.

## Lê Thị Ánh Nhung - Category & Statistics

- Xác định dữ liệu liên quan đến danh mục và khai thác công thức.
- Hoàn thiện `Category` và các thông tin cần thiết.
- Thiết lập liên kết giữa `Category` và `Recipe`.
- Chuẩn bị dữ liệu phục vụ lọc, sắp xếp, phân trang và thống kê.
- Chuẩn bị dữ liệu mẫu danh mục phục vụ truy vấn và thống kê.

---
### Bữa 2 - Hoàn thiện cấu hình dữ liệu và tạo dữ liệu kiểm thử

**Mục tiêu:** Hoàn thiện Entity, Configuration và dữ liệu ngẫu nhiên cho từng chức năng; tích hợp vào DbContext và Migration để tạo cơ sở dữ liệu phục vụ kiểm thử.

#### Lê Thị Ánh Nhung - Category & Statistics

- Hoàn thiện `Category` và Configuration tương ứng.
- Kiểm tra và hoàn thiện quan hệ giữa `Category` và `Recipe`.
- Xử lý các điểm chưa thống nhất giữa Entity và Configuration thuộc chức năng Category.
- Xây dựng dữ liệu ngẫu nhiên cho danh mục.
- Đảm bảo cơ sở dữ liệu sau khi tích hợp có ít nhất **20 Categories** phục vụ truy vấn và thống kê.

--------------------------------------------------------------------------------------------
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
- Sử dụng `EXPLAIN ANALYZE` để kiểm tra các truy vấn GetAll, GetById và GetByCategory.
- Kiểm thử Filter, Sort, Pagination và các truy vấn đọc sau khi tối ưu.
------------------------------------------------------------------------------------------------
# Cài đặt PostgreSQL bằng Docker

Project sử dụng **PostgreSQL 16** chạy bằng Docker.

Cả nhóm sử dụng chung file `docker-compose.yml`. Mỗi thành viên có file `.env` riêng trên máy để thiết lập mật khẩu và port PostgreSQL.

## 1. Yêu cầu

Trước khi chạy project cần cài:

- Docker Desktop
- Git
- pgAdmin 4 (nếu muốn quản lý database bằng giao diện)

Mở Docker Desktop và chờ Docker Engine chạy.

Có thể kiểm tra bằng:

```powershell
docker info
```

Nếu Docker hoạt động bình thường, lệnh sẽ hiển thị thông tin của cả Client và Server.

---

## 2. Lấy code mới nhất

Clone repository nếu chưa có project:

```powershell
git clone <URL_REPOSITORY>
cd PTUDWNC-2026-Nhom12
```

Nếu đã có project:

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

Mỗi thành viên cần tạo một file `.env` riêng.

Trên PowerShell:

```powershell
Copy-Item .env.example .env
```

Sau đó mở `.env`.

Ví dụ:

```env
POSTGRES_DB=culinary_blog
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_password
POSTGRES_PORT=5432
```

Trong đó:

- `POSTGRES_DB`: tên database của project.
- `POSTGRES_USER`: tài khoản PostgreSQL.
- `POSTGRES_PASSWORD`: mật khẩu PostgreSQL do mỗi thành viên tự đặt.
- `POSTGRES_PORT`: port PostgreSQL được mở trên máy.

Ví dụ:

```env
POSTGRES_DB=culinary_blog
POSTGRES_USER=postgres
POSTGRES_PASSWORD=abc123
POSTGRES_PORT=5432
```

> Không commit file `.env` lên Git.

File `.env` đã được thêm vào `.gitignore`.

---

## 4. Trường hợp port 5432 đã được sử dụng

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

sau đó chọn **Save**.

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

Khởi động lại:

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

**Lưu ý:** `docker compose down -v` sẽ xóa Docker volume và toàn bộ dữ liệu PostgreSQL local.

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

Không ghi trực tiếp mật khẩu cá nhân vào `docker-compose.yml`.

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

## Docker Engine chưa chạy

Nếu gặp lỗi dạng:

```text
failed to connect to the docker API
dockerDesktopLinuxEngine
```

mở Docker Desktop và chờ Docker Engine chạy.

Sau đó kiểm tra:

```powershell
docker info
```

---

## Port 5432 bị trùng

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

## Thay đổi password nhưng password mới không hoạt động

Các biến:

```text
POSTGRES_DB
POSTGRES_USER
POSTGRES_PASSWORD
```

được PostgreSQL sử dụng khi database được khởi tạo lần đầu.

Nếu Docker volume cũ đã tồn tại, chỉ sửa `.env` sẽ không tự thay đổi user/password bên trong database cũ.

Trong trường hợp database local chưa có dữ liệu cần giữ và muốn khởi tạo lại hoàn toàn:

```powershell
docker compose down -v
docker compose up -d
```

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