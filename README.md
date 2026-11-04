# TASK 4 (BỮA 4) – TÌM KIẾM TOÀN VĂN & FUZZY SEARCH POSTGRESQL

**Sinh viên:** Võ Hùng Mạnh
**MSSV:** 2312687
**Nhóm:** 12
**Nhánh:** `feat/vohungmanh-full-text-search`

---

### 1. Mục tiêu Task 4 liên quan đến nhánh
Trong khuôn khổ Bữa 4 (Task 4), hệ thống yêu cầu xây dựng công cụ tìm kiếm công thức nấu ăn chuyên sâu trên cơ sở dữ liệu PostgreSQL 16 (SRS FR-SRCH-001). Công cụ phải xử lý được tiếng Việt có dấu và không dấu, hỗ trợ xếp hạng độ liên quan (Relevance Ranking), tích hợp bộ lọc đa tiêu chí (danh mục, độ khó, thời gian nấu) và cơ chế tự động Fallback tìm kiếm mờ (Fuzzy Search) khi người dùng nhập sai lỗi chính tả nhẹ.

### 2. Công việc cá nhân thực tế
- Xây dựng lớp truy vấn `SearchRecipesQuery` và handler `SearchRecipesHandler` xử lý tìm kiếm 2 giai đoạn theo mô hình CQRS / MediatR.
- Tích hợp tiện ích chuẩn hóa tiếng Việt `VietnameseTextNormalizer.Normalize`: loại bỏ dấu thanh, chuyển về lowercase nhằm tương thích với từ điển `simple` của PostgreSQL.
- Cấu hình truy vấn Full-Text Search qua thuộc tính Shadow `SearchVectorFts` kiểu `NpgsqlTsVector` và hàm `EF.Functions.PlainToTsQuery`.
- Hiện thực cơ chế tính điểm xếp hạng độ liên quan `ts_rank` kết hợp sắp xếp theo thời gian xuất bản.
- Xây dựng giải thuật dự phòng mờ (Fuzzy Search Fallback) sử dụng extension `pg_trgm` (`TrigramsSimilarity > 0.3f`) kết hợp hàm `unaccent()` khi giai đoạn Full-Text Search không tìm thấy kết quả.
- Ánh xạ endpoint tìm kiếm `GET /api/v1/recipes/search` trong `RecipeEndpoints.cs` hỗ trợ phân trang và lọc kết quả.
- Viết 454 dòng mã Unit Test trong `SearchRecipesUnitTests.cs` kiểm tra toàn bộ logic chuẩn hóa, phân trang, lọc đa tiêu chí và fallback fuzzy matching.

### 3. Chức năng đã hoàn thành
- **Tìm kiếm toàn văn tiếng Việt (FTS Stage 1):**
  - Tìm chính xác cụm từ có dấu: `"Phở bò Hà Nội"` tìm ra công thức chứa `"Phở bò Hà Nội"`.
  - Tìm kiếm không dấu (Unaccent): gõ `"pho bo"` tìm ra công thức `"Phở bò"`.
  - Xếp hạng theo độ liên quan (`ts_rank` giảm dần), cho phép ưu tiên các công thức có từ khóa xuất hiện nhiều lần ở Tiêu đề hoặc Mô tả.
- **Dự phòng tìm kiếm mờ (Fuzzy Fallback Stage 2):**
  - Khi FTS trả về 0 kết quả (ví dụ gõ sai nhẹ `"phoo bo"` hoặc `"bún chiken"`), hệ thống tự động kích hoạt truy vấn `pg_trgm` với ngưỡng tương đồng `similarity > 0.3`.
  - Phân loại rõ nguồn kết quả trong response qua cờ `MatchType`: `"FullTextSearch"` hoặc `"FuzzySearch"`.
- **Bộ lọc đa tiêu chí & Sắp xếp:**
  - Lọc theo danh mục (`CategoryId` hoặc `CategorySlug`).
  - Lọc theo độ khó (`Difficulty`: Easy, Medium, Hard).
  - Lọc theo thời gian nấu tối đa (`MaxCookTimeMinutes`).
  - Sắp xếp linh hoạt: theo độ liên quan mặc định, theo ngày mới nhất (`newest`), hoặc thời gian nấu tăng dần (`cooktime`).
- **Phân trang an toàn:** Giới hạn từ khóa từ 2 đến 100 ký tự (chuẩn SRS); phân trang `Page` và `PageSize` (tối đa 100 kết quả mỗi trang).

### 4. Cách xử lý và luồng hoạt động
```text
GET /api/v1/recipes/search?q=pho+bo&difficulty=Easy&sortBy=newest
        │
        ▼
Kiểm tra độ dài từ khóa (2 <= len <= 100)
        │
        ▼
Chuẩn hóa tiếng Việt: VietnameseTextNormalizer.Normalize(q) -> "pho bo"
        │
        ▼
Base Query: Recipe.Status == Published && !IsDeleted + Các bộ lọc (Category, Difficulty, CookTime)
        │
        ├── GIAI ĐOẠN 1: PostgreSQL Full-Text Search (tsvector @@ plainto_tsquery)
        │     │
        │     ├── CÓ KẾT QUẢ (> 0) ──> Sắp xếp ts_rank / newest / cooktime
        │     │                        Gán MatchType = "FullTextSearch"
        │     │                        Trả về PagedResult DTO
        │     │
        │     └── KHÔNG CÓ KẾT QUẢ (0)
        │           │
        │           ▼
        └── GIAI ĐOẠN 2: Fuzzy Search Fallback (pg_trgm)
                    │
                    ├── Query: TrigramsSimilarity(lower(unaccent(Title)), "pho bo") > 0.3
                    ├── Gán MatchType = "FuzzySearch"
                    └── Trả về PagedResult DTO
```

### 5. Danh sách file mã nguồn quan trọng

| Đường dẫn file | Vai trò và chức năng chính |
| :--- | :--- |
| `backend/src/CulinaryBlog.Application/Features/Search/Queries/SearchRecipes/SearchRecipesQuery.cs` | 82 dòng mã bổ sung xử lý truy vấn tìm kiếm FTS 2 giai đoạn, lọc đa tiêu chí và Fallback trigram. |
| `backend/src/CulinaryBlog.Api/Endpoints/Recipes/RecipeEndpoints.cs` | Ánh xạ endpoint `GET /api/v1/recipes/search` nhận query parameters và trả về `PagedResult<RecipeSearchResultDto>`. |
| `backend/src/CulinaryBlog.Application/Common/Utilities/VietnameseTextNormalizer.cs` | Tiện ích bóc tách dấu thanh tiếng Việt và chuyển đổi sang ASCII lowercase an toàn. |
| `backend/src/CulinaryBlog.Infrastructure/Data/ApplicationDbContext.cs` | Đăng ký PostgreSQL extensions `unaccent`, `pg_trgm` và cấu hình Shadow Property `SearchVectorFts`. |
| `backend/tests/CulinaryBlog.UnitTests/Application/Features/Search/SearchRecipesUnitTests.cs` | 454 dòng kiểm thử đơn vị cho mọi nhánh logic FTS, Unaccent, Fuzzy matching và phân trang. |
| `docs/README_FULL_TEXT_SEARCH.md` | Tài liệu kỹ thuật chi tiết về kiến trúc Full-Text Search và tối ưu hóa chỉ mục GIN. |
| `docs/VO_HUNG_MANH_FULL_TEXT_SEARCH_COMPLAN.md` | Bản đặc tả kỹ thuật và kế hoạch kiểm thử hiệu năng tìm kiếm Bữa 4. |

### 6. API, Database và Infrastructure liên quan
- **Cơ sở dữ liệu:** PostgreSQL 16.
- **Tiện ích mở rộng PostgreSQL:** `CREATE EXTENSION IF NOT EXISTS unaccent;` và `CREATE EXTENSION IF NOT EXISTS pg_trgm;`.
- **Chỉ mục tăng tốc:**
  - GIN Index trên `SearchVectorFts` (tăng tốc độ khớp từ khóa FTS).
  - GiST / GIN Trigram Index trên `unaccent(lower(title))` (tăng tốc tính toán hàm `similarity`).
- **Endpoint HTTP:** `GET /api/v1/recipes/search?q={query}&page={page}&pageSize={size}&categoryId={id}&difficulty={level}&maxCookTimeMinutes={mins}&sortBy={sort}`.

### 7. Hướng dẫn chạy

#### Bước 1: Khởi động PostgreSQL 16 Container
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
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --filter "FullyQualifiedName~SearchRecipes"
```

#### Kiểm thử trực tiếp qua cURL
```bash
# 1. Tìm kiếm có dấu
curl -G "http://localhost:5000/api/v1/recipes/search" --data-urlencode "q=Phở bò"

# 2. Tìm kiếm không dấu
curl -G "http://localhost:5000/api/v1/recipes/search" --data-urlencode "q=pho bo"

# 3. Tìm kiếm mờ khi gõ sai lỗi chính tả nhẹ
curl -G "http://localhost:5000/api/v1/recipes/search" --data-urlencode "q=phoo bo"

# 4. Tìm kiếm kết hợp bộ lọc độ khó
curl -G "http://localhost:5000/api/v1/recipes/search" --data-urlencode "q=bò" -d "difficulty=Medium"
```

### 9. Kết quả kiểm thử có bằng chứng
- **Kết quả Unit Test:** Toàn bộ test case trong `SearchRecipesUnitTests` đều đạt `Passed!` (kiểm thử từ khóa dưới 2 ký tự trả về rỗng, từ khóa không dấu tìm ra bản ghi tiếng Việt, và kiểm thử trường hợp gõ sai kích hoạt fallback fuzzy).
- **Kết quả thực tế trên Database:**
  - Từ khóa `"pho bo"` khớp chính xác công thức `"Phở bò Hà Nội"` (`MatchType: FullTextSearch`).
  - Từ khóa `"phoo bo"` vẫn tìm ra `"Phở bò Hà Nội"` (`MatchType: FuzzySearch`).

### 10. Hạn chế và tồn đọng
- **Trạng thái thực tế:** Hoàn thiện 100% tầng Backend API, xử lý unaccent, ranking và fallback trigram.
- **Tồn đọng:** Giao diện người dùng Search UI trên Next.js (ô tìm kiếm tức thì có debounce, bộ lọc sidebar và đồng bộ URL query params) thuộc phạm vi Frontend, được phát triển nguyên mẫu tại Task 4/6 và hoàn thiện nghiệm thu toàn diện tại Task 7.

### 11. Liên kết tài liệu kỹ thuật chi tiết
- Chi tiết kiến trúc Full-Text Search: [docs/README_FULL_TEXT_SEARCH.md](docs/README_FULL_TEXT_SEARCH.md)
- Kế hoạch triển khai và ma trận kiểm thử: [docs/VO_HUNG_MANH_FULL_TEXT_SEARCH_COMPLAN.md](docs/VO_HUNG_MANH_FULL_TEXT_SEARCH_COMPLAN.md)

---

# Phát triển ứng dụng Web nâng cao

## Nhóm 12

### Thành viên
1. Võ Hùng Mạnh - 2312687 - 2312687@dlu.edu.vn - Nhóm trưởng
2. Lê Thị Ánh Nhung - 2312709 - 2312709@dlu.edu.vn
3. Phạm Nguyễn Ngọc Phước - 2312718 - 2312718@dlu.edu.vn
4. Nguyễn Văn Quốc - 2312729 - 2312729@dlu.edu.vn
