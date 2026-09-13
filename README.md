# PTUDWNC-2026-Nhom12 - Culinary Blog

Ung dung blog am thuc gom frontend Next.js, backend ASP.NET Core Web API, PostgreSQL, Redis va MinIO. Repository hien dang o giai doan hoan thien nen tang kien truc, database schema va bo khung giao dien frontend theo cac luong nghiep vu.

## 1. Thanh vien

1. Vo Hung Manh - 2312687 - 2312687@dlu.edu.vn (Nhom truong)
2. Le Thi Anh Nhung - 2312709 - 2312709@dlu.edu.vn
3. Pham Nguyen Ngoc Phuoc - 2312718 - 2312718@dlu.edu.vn
4. Nguyen Van Quoc - 2312729 - 2312729@dlu.edu.vn

## 2. Cong nghe

- Frontend: Next.js 14, React 18, TypeScript 5.5
- Backend: ASP.NET Core Web API tren .NET 10
- Kien truc backend: Domain, Application, Infrastructure, API
- Database: PostgreSQL 16
- ORM: Entity Framework Core 10 va Npgsql
- Ha tang: Docker Compose, Redis 7, MinIO
- Giao dien: Next.js App Router, CSS toan cuc, du lieu mock o giai doan UI

## 3. Noi dung da hoan thanh

### Backend

- Tao cac project `CulinaryBlog.Api`, `CulinaryBlog.Application`, `CulinaryBlog.Domain` va `CulinaryBlog.Infrastructure`.
- Tao cac entity: `ApplicationUser`, `RefreshToken`, `Category`, `Recipe`, `RecipeIngredient`, `RecipeStep`, `RecipeImage`, `RecipeNutrition`.
- Tao `ApplicationDbContext` va cau hinh khoa chinh, khoa ngoai, index, quan he cascade/restrict va precision cho dinh duong.
- Dang ky PostgreSQL va cac service Infrastructure trong `DependencyInjection`.
- API hien co endpoint kiem tra `GET /` va `GET /health`.
- JWT service hien moi la bo khung placeholder, chua phai xac thuc JWT hoan chinh.

### Database

- PostgreSQL container chay tai `localhost:5432`.
- Database: `PTUDWNC-2026-Nhom12`.
- Migration dau tien: `InitialCreate`.
- Schema da tao cac bang: `Users`, `RefreshTokens`, `Categories`, `Recipes`, `RecipeIngredients`, `RecipeSteps`, `RecipeImages`, `RecipeNutritions`, `__EFMigrationsHistory`.

### Frontend

Frontend duoc to chuc theo bon route group nghiep vu:

```text
frontend/app/
├── layout.tsx                         # Root layout cua toan ung dung
├── globals.css                        # CSS toan cuc
├── page.tsx                           # Trang chu /
├── (authentication-user-management)/  # Xac thuc & quan ly nguoi dung
│   ├── layout.tsx
│   ├── login/page.tsx
│   ├── register/page.tsx
│   └── profile/page.tsx
├── (discovery-search)/                # Kham pha & tim kiem
│   ├── layout.tsx
│   ├── explore/page.tsx
│   └── search/page.tsx
├── (recipe-management)/              # Quan ly cong thuc
│   ├── layout.tsx
│   └── recipes/
│       ├── page.tsx
│       ├── new/page.tsx
│       └── [slug]/page.tsx
└── (admin-system)/                    # Quan tri vien - He thong
    ├── layout.tsx
    └── admin/
        ├── page.tsx
        ├── users/page.tsx
        ├── recipes/page.tsx
        ├── reviews/page.tsx
        ├── reports/page.tsx
        └── settings/page.tsx
```

Cac thu muc route group trong ngoac chi de to chuc code va khong xuat hien trong URL. Vi du `app/(discovery-search)/explore/page.tsx` co URL la `/explore`.

## 4. Yeu cau cai dat tren may moi

### Bat buoc

- Git
- Docker Desktop co Docker Compose
- Node.js 20 LTS neu chay frontend ngoai Docker
- .NET SDK 10 neu chay backend ngoai Docker hoac tao migration

### Khuyen nghi

- VS Code
- C# Dev Kit
- ESLint
- Docker
- PostgreSQL client hoac pgAdmin

Kiem tra cong cu:

```powershell
git --version
docker version
docker compose version
node --version
npm --version
dotnet --version
```

Neu chi chay toan bo he thong bang Docker thi chi can Git va Docker Desktop; Node.js va .NET SDK khong bat buoc tren may client.

## 5. Cai dat lan dau bang Docker Compose

### Buoc 1: Clone repository

```powershell
git clone <URL_REPOSITORY>
cd PTUDWNC-2026-Nhom12
```

Neu lam viec tren branch rieng:

```powershell
git fetch --all
git checkout <ten-branch>
```

### Buoc 2: Tao file moi truong

Tao `.env` tu file mau:

```powershell
Copy-Item .env.example .env
```

Noi dung mac dinh:

```env
DATABASE_URL="postgresql://postgres:2552005@localhost:5432/PTUDWNC-2026-Nhom12"
POSTGRES_DB=PTUDWNC-2026-Nhom12
POSTGRES_USER=postgres
POSTGRES_PASSWORD=2552005
POSTGRES_PORT=5432
```

Khong commit `.env` neu file chua password hoac secret rieng cua may.

### Buoc 3: Khoi dong Docker Desktop

Mo Docker Desktop va cho Docker Engine o trang thai Running. Kiem tra:

```powershell
docker info
```

Ket qua phai co ca phan `Client` va `Server`. Neu chi co `Client`, Docker Engine chua chay.

### Buoc 4: Khoi dong toan bo stack

```powershell
docker compose up -d --build
```

Kiem tra:

```powershell
docker compose ps
```

Cac cong su dung:

| Thanh phan | URL/cong |
|---|---|
| Frontend | http://localhost:3000 |
| Backend | http://localhost:5000 |
| PostgreSQL | localhost:5432 |
| Redis | localhost:6379 |
| MinIO API | http://localhost:9000 |
| MinIO Console | http://localhost:9001 |

### Buoc 5: Kiem tra he thong

```powershell
Invoke-WebRequest http://localhost:3000 -UseBasicParsing
Invoke-WebRequest http://localhost:5000/health -UseBasicParsing
```

Xem log:

```powershell
docker compose logs -f backend
docker compose logs -f frontend
docker compose logs -f postgres
```

Dung he thong nhung giu du lieu:

```powershell
docker compose down
```

Dung he thong va xoa volume database. Lenh nay xoa du lieu local, chi dung khi muon reset database:

```powershell
docker compose down -v
```

## 6. Chay frontend ngoai Docker

```powershell
cd frontend
npm install
npm run dev
```

Frontend mac dinh chay tai `http://localhost:3000`. Neu cong 3000 dang duoc su dung, Next.js co the tu chuyen sang 3001; hay mo URL ma terminal hien thi.

Build production frontend:

```powershell
npm run build
npm run start
```

## 7. Chay backend ngoai Docker

Dam bao PostgreSQL dang chay:

```powershell
docker compose up -d postgres
```

Sau do chay API:

```powershell
cd backend/src
dotnet restore CulinaryBlog.Api/CulinaryBlog.Api.csproj
dotnet build CulinaryBlog.Api/CulinaryBlog.Api.csproj
dotnet run --project CulinaryBlog.Api/CulinaryBlog.Api.csproj
```

Backend local dung connection string trong `backend/src/CulinaryBlog.Api/appsettings.json` va launch profile tai `http://localhost:5108`.

Endpoint kiem tra:

```text
GET http://localhost:5108/
GET http://localhost:5108/health
```

## 8. Tao hoac cap nhat database schema

Cai EF Core CLI mot lan:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.0
```

Neu may khong nhan lenh `dotnet-ef`:

```powershell
& "$HOME\.dotnet\tools\dotnet-ef.exe" --version
```

Tao migration moi:

```powershell
cd backend/src
& "$HOME\.dotnet\tools\dotnet-ef.exe" migrations add <MigrationName> `
  --project CulinaryBlog.Infrastructure/CulinaryBlog.Infrastructure.csproj `
  --startup-project CulinaryBlog.Api/CulinaryBlog.Api.csproj `
  --output-dir Persistence/Migrations
```

Ap dung migration:

```powershell
& "$HOME\.dotnet\tools\dotnet-ef.exe" database update `
  --project CulinaryBlog.Infrastructure/CulinaryBlog.Infrastructure.csproj `
  --startup-project CulinaryBlog.Api/CulinaryBlog.Api.csproj
```

Migration hien co nam tai `backend/src/CulinaryBlog.Infrastructure/Persistence/Migrations/`.

## 9. Xu ly loi thuong gap

### Docker Engine chua chay

Loi `failed to connect to the docker API` cho biet Docker Desktop chua chay. Mo Docker Desktop, cho trang thai Running roi chay lai `docker info`.

### Cong 5432 da duoc su dung

Kiem tra container:

```powershell
docker ps
```

Neu PostgreSQL khac dang chiem cong 5432, dung container do hoac doi port mapping trong `docker-compose.yml`. Neu doi sang 5433, phai cap nhat connection string tuong ung.

### Cong 3000 da duoc su dung

Mo URL ma Next.js hien thi, thuong la `http://localhost:3001`, hoac dung tien trinh frontend cu.

### Loi `Cannot find module './xxx.js'` tu Next.js

Xoa cache `.next`:

```powershell
cd frontend
Remove-Item .next -Recurse -Force
npm run dev
```

### TypeScript bao khong tim thay `globals.css`

Dam bao co hai file:

```text
frontend/app/globals.css
frontend/global.d.ts
```

Sau do trong VS Code chon `TypeScript: Restart TS Server`.

### Docker build backend khong tim thay solution

Dockerfile da duoc cau hinh build truc tiep project that:

```text
backend/src/CulinaryBlog.Api/CulinaryBlog.Api.csproj
```

Khong dung file solution gia dinh `backend/CulinaryBlog.slnx`.

## 10. Trang thai chuc nang

Da co:

- Kien truc backend theo cac layer.
- PostgreSQL, EF Core va migration dau tien.
- Docker Compose cho PostgreSQL, Redis, MinIO, backend va frontend.
- Trang chu frontend.
- Bon route group frontend va layout rieng theo muc dich.
- Cac page khung cho authentication, discovery/search, recipe management va admin system.

Chua hoan thien:

- API dang ky, dang nhap va xac thuc JWT thuc te.
- API CRUD Category va Recipe.
- Ket noi frontend voi backend.
- Seed du lieu mau.
- Upload file thuc te len MinIO.
- Search, phan trang, danh gia va binh luan.
- Dashboard admin voi so lieu thuc te.
- Test integration/unit cho nghiep vu.

## 11. Quy tac lam viec nhom

1. Khong push truc tiep code chuc nang len `main`.
2. Pull code moi nhat truoc khi bat dau lam viec.
3. Moi task thuc hien tren branch rieng.
4. Khong tu y thay doi cau truc project chung.
5. Khong commit file `.env` chua thong tin bi mat.
6. Commit message phai mo ta ro thay doi.
7. Kiem tra code truoc khi tao Pull Request.
8. Tao branch con co ten the hien chuc nang dang thuc hien.
