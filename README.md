# PTUDWNC-2026-Nhom12 - Culinary Blog

Ung dung blog am thuc gom frontend Next.js, backend ASP.NET Core Minimal API, PostgreSQL, Redis va MinIO. Backend da co cac luong xac thuc, quan ly danh muc, cong thuc, upload anh va dashboard; frontend van dang hoan thien, mot so trang hien con dung du lieu mau.

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

- Chia backend thanh `CulinaryBlog.Api`, `CulinaryBlog.Application`, `CulinaryBlog.Domain` va `CulinaryBlog.Infrastructure`.
- Co entity cho user, refresh token, category va recipe cung cac thanh phan ingredient, step, image va nutrition.
- Minimal API co endpoint auth, category, recipe, dashboard, upload file va health check; mot so route duoc cung cap ca duoi `/api` va `/api/v1`.
- Dang ky/dang nhap phat hanh JWT; refresh token duoc hash truoc khi luu va co rotation/revocation.
- Luong ghi Recipe va cac thao tac child entity duoc dieu phoi qua repository, `RecipeWriteService` va `IUnitOfWork`.
- Rate limit, CORS, exception handling cho concurrency conflict, OpenAPI trong Development va migrate/seed khi API khoi dong.

### Database

- PostgreSQL 16 chay tai `localhost:5432`; database phat trien mac dinh: `PTUDWNC-2026-Nhom12`.
- User duoc map vao `AspNetUsers`; `RecipeNutrition` la owned data nam trong bang `Recipes`, khong phai bang rieng.
- Chuoi migration dang duoc EF su dung: `20260915015345_InitialCreate`, `20260923102544_UnifyUserIdsAndApplicationConcurrency`, `20260923103026_RemoveUnusedImageVariants`, `20260923104756_AddRefreshTokenConcurrency` va `20260926025747_OptimizeRecipeIndexesAndSearch`.
- Migration `OptimizeRecipeIndexesAndSearch` them index cho list/filter va child ordering, extension `pg_trgm`, GIN trigram indexes cho title/description va cot `CoverImageUrl`.
- Database integration test rieng co ten ket thuc bang `_test`; khong dung database phat trien lam target test.

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
DATABASE_URL="postgresql://postgres:change-me@localhost:5432/PTUDWNC-2026-Nhom12"
POSTGRES_DB=PTUDWNC-2026-Nhom12
POSTGRES_USER=postgres
POSTGRES_PASSWORD=change-me
POSTGRES_PORT=5432
JWT_KEY=replace-with-a-long-random-signing-key
MINIO_ROOT_USER=change-me
MINIO_ROOT_PASSWORD=change-me
MINIO_ACCESS_KEY=change-me
MINIO_SECRET_KEY=change-me
DEMO_PASSWORD=change-this-demo-password
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

Da co o backend:

- Auth register/login/refresh/logout/profile; JWT va refresh-token rotation.
- CRUD Category; Redis distributed cache cho danh sach/chi tiet category.
- Recipe list/search/detail, CRUD, publish/unpublish/archive, child ingredient/step va primary/delete image.
- Upload anh toi MinIO voi gioi han kich thuoc, MIME va file-signature checks.
- Dashboard statistics cho Admin; readiness check PostgreSQL, Redis va MinIO.
- PostgreSQL schema/migrations va seed du lieu demo.

Frontend da co route group va giao dien cho auth, discovery, recipe va admin. Login/register/profile va mot so recipe detail requests da noi API. Cac man hinh kham pha/list/form/dashboard va nhieu trang admin van la mock/placeholder, chua phai luong san pham hoan chinh.

Ngoai pham vi hien thuc hien: Google OAuth, full-text search `tsvector`, email/background jobs, va rating/comments. Unit tests co san; PostgreSQL persistence integration test can duoc bat bang `CULINARYBLOG_TEST_CONNECTION`.

## 11. Phan cong thuc hien: Recipe Persistence - Pham Nguyen Ngoc Phuoc

Phan viec nay tap trung vao cau hinh luu tru va cac thao tac ghi Recipe. Muc tieu la de Recipe va cac entity con duoc ghi qua mot ranh gioi repository/unit-of-work ro rang, bao toan quan he EF Core, va co migration/test kiem chung tren PostgreSQL.

### 11.1 Mo hinh va quan he du lieu

Cac cau hinh hieu luc nam trong `ApplicationDbContext.OnModelCreating`:

- `Recipe` co FK den `ApplicationUser` qua `AuthorId` va den `Category` qua `CategoryId`; hai quan he dung `DeleteBehavior.Restrict` de khong xoa nham tac gia, danh muc hoac recipe lien quan.
- `RecipeIngredient`, `RecipeStep` va `RecipeImage` la cac collection cua Recipe, FK `RecipeId`; quan he cascade phuc vu viec purge vat ly, con DELETE nghiep vu Recipe la soft-delete.
- `RecipeNutrition` duoc map bang `OwnsOne`. Cac gia tri Calories, Protein, Carbohydrates, Fat, Fiber va Sodium nam trong cac cot `Nutrition_*` cua bang `Recipes`; decimal precision la `(8,2)`.
- `RecipeIngredient.Quantity` dung precision `(10,3)`; `SortOrder` map vao cot `OrderIndex`.
- Entity `ApplicationUser` duoc map vao `AspNetUsers`; khong phu thuoc ASP.NET Identity `UserManager` cho luong auth hien tai.

### 11.2 Index va migration

`Recipe.Slug` co unique index de bao ve tinh duy nhat tai database. Ngoai index theo `AuthorId`, `CategoryId`, `Status`, `CreatedAt`, model con co composite indexes cho danh sach theo trang thai/ngay va category/trang thai/ngay. Recipe title/description dung GIN voi `gin_trgm_ops` cho truy van substring `Contains`; ingredient co index theo Recipe, step co unique index `(RecipeId, StepNumber)`, image co index `(RecipeId, SortOrder)`.

Migration dang ap dung la `20260926025747_OptimizeRecipeIndexesAndSearch`, tiep theo chuoi lich su da ton tai trong PostgreSQL. Migration nay duoc tao tren dung baseline local; cac migration alternate khong thuoc chuoi EF runtime van duoc giu trong repository de tranh xoa dau vet. Migration da duoc ap dung vao database local phat trien va database `_test`; khong can chay lai migration nay tren database da o cung version.

### 11.3 Repository, Unit of Work va ghi du lieu

- `IRecipeRepository` khai bao query/add/update/delete va cac thao tac ingredient, step, image; `RecipeRepository` thuc thi bang EF Core.
- `GetForUpdateAsync` tai Recipe cung child collections o trang thai tracked va dung `AsSplitQuery` de tranh nhan ban dong ket qua khi load nhieu collection.
- `ExistsBySlugAsync` su dung `AnyAsync`, co ho tro exclude Id khi update. Unique index van la lop bao ve cuoi neu co hai request tao cung slug dong thoi.
- `IUnitOfWork.SaveChangesAsync` duoc implement boi `ApplicationUnitOfWork`. `RecipeWriteService` dieu phoi create/update, thay child collections, soft-delete va cac thao tac ingredient/step/image.
- Khi thay aggregate, repository danh dau cu the child cu la Deleted va cac child moi la Added. Khong danh dau ca graph la Modified: cac entity moi co GUID duoc tao san, neu EF nhan nham la Modified se phat sinh concurrency conflict.
- Moi nghiep vu ghi trong service chi goi mot `SaveChangesAsync`. EF Core/Npgsql tu dong bao cac lenh SQL cua mot SaveChanges trong transaction. Hien khong co explicit transaction bao nhieu lan SaveChanges trong cung mot use case; neu sau nay tach thanh nhieu lan save, can them transaction tuong ung.

### 11.4 Truy van Create/Update/Delete

- Create kiem tra category/author/slug va validation truoc khi tao Recipe; request khong duoc tu chon author khac voi user dang dang nhap.
- Update lay aggregate tracked bang Id, kiem tra owner/Admin, slug conflict va thay ingredient/step/image trong cung mot lan save.
- Delete Recipe la soft-delete; child records khong bi xoa vat ly boi endpoint nghiep vu.
- Read theo slug chi load graph chi tiet khi can; list/search chieu cac cot summary, dem tong so truoc phan trang va them Id lam tie-breaker de thu tu trang on dinh.

### 11.5 Kiem thu va cach chay

`RecipeWriteServiceTests` kiem tra delegation va so lan save; `RecipePersistenceModelTests` kiem tra index/provider metadata. `RecipePersistenceIntegrationTests` chay tren PostgreSQL that, kiem chung FK User/Category, nutrition owned, ingredient create/update/delete, thay child graph va soft-delete. Test tu goi migrate, yeu cau ten database ket thuc bang `_test`, va rollback transaction sau khi chay.

Tao database test rieng (khong dung database trong `DATABASE_URL` truc tiep). Vi du database test la `PTUDWNC-2026-Nhom12_test`; dat `CULINARYBLOG_TEST_CONNECTION` bang credentials local tu `.env`, sau do chay:

```powershell
docker exec culinaryblog-postgres createdb -U postgres PTUDWNC-2026-Nhom12_test
$env:CULINARYBLOG_TEST_CONNECTION = "Host=localhost;Port=5432;Database=PTUDWNC-2026-Nhom12_test;Username=postgres;Password=<POSTGRES_PASSWORD trong .env>"
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --nologo
```

Neu database test da duoc tao thi bo qua lenh `createdb`. Khi khong dat bien moi truong, integration test duoc Skip co chu dich; unit tests van chay. Test tu goi migrations va se tu choi connection string neu ten database khong ket thuc bang `_test`. Moi ghi du lieu trong test duoc rollback khi test ket thuc.

Ket qua kiem chung sau phan viec:

- Toan bo test suite, bao gom PostgreSQL persistence integration test: 14 passed, 0 failed, 0 skipped.
- API build: `dotnet build backend/src/CulinaryBlog.Api/CulinaryBlog.Api.csproj --nologo`.
- EF Core model check: khong con thay doi model dang cho migration.

## 12. Quy tac lam viec nhom

1. Khong push truc tiep code chuc nang len `main`.
2. Pull code moi nhat truoc khi bat dau lam viec.
3. Moi task thuc hien tren branch rieng.
4. Khong tu y thay doi cau truc project chung.
5. Khong commit file `.env` chua thong tin bi mat.
6. Commit message phai mo ta ro thay doi.
7. Kiem tra code truoc khi tao Pull Request.
8. Tao branch con co ten the hien chuc nang dang thuc hien.
