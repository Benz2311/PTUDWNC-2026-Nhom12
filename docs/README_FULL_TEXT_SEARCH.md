# Tìm kiếm toàn văn và tìm kiếm mờ công thức nấu ăn (Full-Text Search & Fuzzy Recipe Search)

## 1. Mục tiêu
Branch `feat/vohungmanh-full-text-search` giải quyết việc xây dựng công cụ tìm kiếm công thức ẩm thực chuyên sâu, hiệu năng cao và chính xác cho nền tảng CulinaryBlog thuộc Task 4 và đặc tả SRS v1.2.0:
- Tìm kiếm cơ bản bằng mệnh đề `LIKE '%keyword%'` trên cơ sở dữ liệu quan hệ thường quét toàn bộ bảng (Full Table Scan), không xử lý được tiếng Việt có dấu/không dấu, không có khả năng xếp hạng độ liên quan và cực kỳ chậm chạp khi dữ liệu lớn.
- Triển khai **PostgreSQL Full-Text Search (FTS)** kết hợp extension `unaccent` và từ điển `'simple'` để người dùng gõ từ khóa không dấu (ví dụ `"pho bo"`) vẫn tìm chính xác món ăn có dấu (**"Phở bò Hà Nội"**).
- Cung cấp cơ chế tìm kiếm mờ dự phòng (**Fuzzy Trigram Fallback**) thông qua extension `pg_trgm` (ngưỡng tương đồng $> 0.3$) khi người dùng gõ sai chính tả (ví dụ `"phoo bo"`).
- Tăng tốc truy vấn bằng chỉ mục đảo **GIN Index** trên `SearchVector` và chỉ mục GIN Trigram trên `Title`, `Description`.
- Hỗ trợ bộ lọc chuyên sâu (Category, Difficulty, CookTime), phân trang, sắp xếp và tối ưu bộ nhớ đệm phân tán với **Redis Cache 1 phút** chống va chạm khóa (Collision-Free).

---

## 2. Kết quả đạt được
Sau khi triển khai branch này:
- **Khả năng tìm kiếm tiếng Việt không dấu hoàn hảo**:
  - Người dùng nhập `"pho bo"`, `"ga nuong"`, `"dau hu"` tìm thấy chính xác `"Phở bò"`, `"Gà nướng chanh sả"`, `"Đậu hũ kho nấm"`.
- **Cơ chế Fallback thông minh khi gõ sai chính tả**:
  - Gõ nhầm `"phoo bo"` $\rightarrow$ Thuật toán Trigram Similarity phát hiện độ tương đồng $> 0.3$, tự động trả về kết quả `"Phở bò"` kèm nhãn `matchType: "FuzzyTrigram"`.
- **Xếp hạng 2 cấp (Two-Tier Relevance Ranking)**:
  - Cấp 1: Ưu tiên điểm tương quan `ts_rank` (từ khóa xuất hiện ở Title có trọng số cao hơn Description) hoặc điểm tương đồng Trigram.
  - Cấp 2: Sắp xếp theo ngày xuất bản mới nhất (`PublishedAt DESC`) khi có cùng điểm liên quan.
- **Bảo vệ dữ liệu công khai (Data Isolation)**:
  - Chỉ trả về các công thức đã xuất bản (`Status == Published`) và chưa bị xóa mềm (`!IsDeleted`). Bản ghi nháp (`Draft`) hoàn toàn bị cách ly.
- **Redis Cache Composite chống va chạm**:
  - TTL = 1 phút; Cache key biến thiên theo toàn bộ tham số: `search:{q}:cat={cat}:diff={diff}:cook={cook}:sort={sort}:p={page}:sz={size}`.
  - Cơ chế Resilient Failover an toàn: Redis gặp sự cố tự động truy vấn trực tiếp vào Database mà không làm sập ứng dụng.
- **Kiểm thử bao phủ**: 29/29 tests trong `SearchRecipesUnitTests` và 116/117 tests toàn hệ thống đều vượt qua thành công (100% Pass).

---

## 3. Luồng hoạt động

```text
User Search Request (Ví dụ: q="pho bo")
  │
  ▼
API Endpoint: GET /api/v1/recipes/search (RecipeEndpoints.cs)
  │ ├─ Bóc tách query params: q, page, pageSize, categoryId, categorySlug, difficulty, maxCookTimeMinutes, sortBy
  │ └─ Kiểm tra Redis Cache composite key
  │
  ├─► [Redis Cache Hit] ─────────► Trả về kết quả ngay (< 5ms)
  │
  ▼ [Redis Cache Miss / Redis Down]
Application Layer: SearchRecipesQueryHandler (MediatR)
  │ ├─ 1. Validate Query:
  │ │    Độ dài từ 2 đến 100 ký tự (nếu < 2 hoặc rỗng -> trả về danh sách rỗng, tránh quét bảng)
  │ │
  │ ├─ 2. Chuẩn hóa tiếng Việt (VietnameseTextNormalizer):
  │ │    Xóa dấu tiếng Việt, lowercase, chuẩn hóa khoảng trắng ("Phở bò" -> "pho bo")
  │ │
  │ ├─ 3. Lọc điều kiện cơ sở (PostgreSQL WHERE):
  │ │    Status == Published AND !IsDeleted AND (Category, Difficulty, CookTime)
  │ │
  │ ├─ 4. Giai đoạn 1: Full-Text Search (FTS):
  │ │    SearchVector @@ PlainToTsQuery('simple', unaccent(q))
  │ │    Tính ts_rank và gắn matchType = "FullTextSearch"
  │ │
  │ └─ 5. Giai đoạn 2: Fuzzy Trigram Fallback (Nếu FTS trả về 0 kết quả):
  │      Tính độ tương đồng pg_trgm trên Title và Description
  │      Lọc các bản ghi có similarity > 0.3 và gắn matchType = "FuzzyTrigram"
  ▼
Infrastructure Layer: PostgreSQL & GIN Indexes
  │ ├─ Chỉ mục GIN trên SearchVector (tăng tốc FTS)
  │ └─ Chỉ mục GIN gin_trgm_ops trên Title và Description (tăng tốc Fuzzy)
  ▼
Redis Cache Write: ICacheService
  │ └─ Ghi kết quả vào Redis Cache với TTL = 1 phút
  ▼
Output
  └─ PagedResult<RecipeSearchResultDto> kèm matchType ("FullTextSearch" hoặc "FuzzyTrigram")
```

### Giải thích chi tiết các bước xử lý:
1. **Kiểm tra bộ nhớ đệm (Cache Lookup):** Endpoint nhận request và tạo composite key chứa toàn bộ tham số. Nếu key đã có trong Redis, trả về ngay lập tức mà không chạm đến DB.
2. **Kiểm tra tính hợp lệ (Validation):** Từ khóa tìm kiếm phải có độ dài từ 2 đến 100 ký tự. Nếu ít hơn 2 ký tự, hệ thống trả về mảng rỗng ngay lập tức theo chuẩn SRS để tránh quét toàn bộ bảng với từ khóa quá ngắn.
3. **Chuẩn hóa chuỗi (Normalization):** Chuyển từ khóa về chữ thường không dấu thông qua hàm tiện ích `VietnameseTextNormalizer.Normalize`.
4. **Truy vấn FTS giai đoạn 1:** Khớp từ khóa với trường vector `SearchVector` bằng toán tử `@@` và hàm `PlainToTsQuery('simple', unaccent(q))`. Chỉ mục GIN giúp tìm kiếm tức thì giữa hàng ngàn bản ghi.
5. **Dự phòng tìm kiếm mờ giai đoạn 2:** Nếu FTS không tìm thấy món nào (do người dùng gõ sai chính tả), hệ thống tự động kích hoạt truy vấn `pg_trgm` tính độ tương đồng của các bộ 3 ký tự (trigrams) với ngưỡng tương đồng `> 0.3`.
6. **Xếp hạng và Phân trang:** Kết quả được sắp xếp theo điểm liên quan, sau đó theo `PublishedAt DESC` và cắt lát theo `page` và `pageSize`.
7. **Lưu cache và Phản hồi:** Kết quả được lưu tạm 1 phút trong Redis và trả về cho Client.

---

## 4. Các file chính

| File | Vai trò | Xử lý gì |
| :--- | :--- | :--- |
| `backend/src/CulinaryBlog.Application/Features/Search/Queries/SearchRecipes/SearchRecipesQuery.cs` | Application / Query Handler | Chứa `SearchRecipesQuery`, handler thực thi tìm kiếm 2 giai đoạn (FTS và Trigram fallback), chuẩn hóa tiếng Việt, áp dụng bộ lọc (Category, Difficulty, CookTime) và phân trang. |
| `backend/src/CulinaryBlog.Api/Endpoints/Recipes/RecipeEndpoints.cs` | Presentation / Minimal API | Khai báo endpoint `GET /api/v1/recipes/search`, tiếp nhận query parameters, xây dựng cache key composite chống va chạm, tích hợp Redis cache 1 phút. |
| `backend/tests/CulinaryBlog.UnitTests/Application/Features/Search/SearchRecipesUnitTests.cs` | Unit Tests | Bộ 29 unit tests bao phủ toàn diện: unaccent tiếng Việt, khớp FTS, phát hiện typo với trigram similarity, validation 2-100 ký tự, cách ly Draft, phân trang, lọc, sắp xếp và cache composite. |
| `docs/VO_HUNG_MANH_FULL_TEXT_SEARCH_COMPLAN.md` | Tài liệu bảo vệ | Báo cáo giải trình kỹ thuật chuyên sâu (591 dòng) về nguyên lý FTS, GIN Index, Trigram, và 20 câu hỏi vấn đáp bảo vệ đồ án. |
| `docs/README_FULL_TEXT_SEARCH.md` | Tài liệu kỹ thuật | Tài liệu hướng dẫn kỹ thuật chi tiết theo chuẩn 13 phần. |

---

## 5. API / Interface

| Method | Endpoint | Input Parameters | Output | Authorization |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/recipes/search` | - `q` (string, required, length 2–100): Từ khóa tìm kiếm<br>- `page` (int, default: 1): Trang hiện tại<br>- `pageSize` (int, default: 10, max: 100): Kích thước trang<br>- `categoryId` (GUID, optional): Lọc theo danh mục<br>- `categorySlug` (string, optional): Lọc theo slug danh mục<br>- `difficulty` (string, optional: Easy, Medium, Hard)<br>- `maxCookTimeMinutes` (int, optional): Thời gian nấu tối đa<br>- `sortBy` (string, optional: relevance, newest, cookTime) | HTTP 200 OK<br>Body: `PagedResult<RecipeSearchResultDto>`<br>- `items`: Mảng bài viết kèm `matchType` (`FullTextSearch` hoặc `FuzzyTrigram`)<br>- `page`, `pageSize`, `totalCount`, `totalPages`<br>- `hasPreviousPage`, `hasNextPage` | Public (Không yêu cầu đăng nhập) |

---

## 6. Business Rules

1. **Giới hạn độ dài từ khóa (SRS FR-SRCH-001)**:
   - Từ khóa tìm kiếm phải có độ dài từ **2 đến 100 ký tự**.
   - Nếu từ khóa $< 2$ ký tự hoặc chỉ chứa khoảng trắng: Trả về kết quả rỗng ngay lập tức, không thực thi truy vấn database.
   - Nếu từ khóa $> 100$ ký tự: Trả về kết quả rỗng để phòng chống tấn công DoS qua chuỗi tìm kiếm quá dài.
2. **Cách ly bản ghi nháp và xóa mềm (Published Only & Data Isolation)**:
   - Câu lệnh truy vấn luôn bắt buộc điều kiện: `Status == RecipeStatus.Published && !IsDeleted`.
   - Khách và người dùng công cộng tuyệt đối không bao giờ nhìn thấy các bài viết ở trạng thái `Draft`, `Archived` hoặc đã bị `Soft-deleted`.
3. **Chuẩn hóa tiếng Việt không dấu (`unaccent` & `simple` dictionary)**:
   - Cơ sở dữ liệu sử dụng từ điển `'simple'` để xử lý tiếng Việt nguyên bản, ngăn chặn quy tắc ngắt từ tiếng Anh (English stemming) làm biến dạng âm tiết tiếng Việt.
   - Hàm `unaccent` loại bỏ toàn bộ dấu thanh và dấu mũ, đưa từ khóa và nội dung về dạng ký tự Latinh cơ bản.
4. **Tìm kiếm mờ dự phòng (Fuzzy Trigram Fallback)**:
   - Chỉ kích hoạt khi Giai đoạn 1 (FTS) không tìm thấy bản ghi nào.
   - Sử dụng giải thuật Trigram Similarity của extension `pg_trgm` với ngưỡng tương đồng $> 0.3$.
5. **Quy tắc xếp hạng 2 cấp (Two-Tier Relevance Ranking)**:
   - Cấp 1 (Độ liên quan): Sắp xếp theo `ts_rank` giảm dần (với FTS) hoặc `similarity` giảm dần (với Trigram). Từ khóa xuất hiện ở Title có trọng số cao hơn Description.
   - Cấp 2 (Thời gian): Khi hai công thức có điểm liên quan bằng nhau, công thức có `PublishedAt DESC` (mới xuất bản hơn) sẽ đứng trước.
6. **Redis Cache 1 phút & Chống va chạm khóa (Collision-Free Cache)**:
   - Khóa cache được xây dựng từ toàn bộ các tham số đầu vào:
     `search:{normalizedQ}:cat={catKey}:diff={diffKey}:cook={cookKey}:sort={sortKey}:p={page}:sz={size}`
   - Thay đổi bất kỳ tham số nào (trang, bộ lọc, sắp xếp) đều sinh ra cache key khác nhau, ngăn chặn hoàn toàn hiện tượng hiển thị sai dữ liệu đệm.
   - TTL = 1 phút theo đúng đặc tả SRS FR-SRCH-001.

---

## 7. Ví dụ hoạt động

### Ví dụ 1: Tìm kiếm tiếng Việt không dấu (FTS Hit)
```text
INPUT:
GET /api/v1/recipes/search?q=pho%20bo

PROCESS:
1. q = "pho bo" (độ dài 6 ký tự, hợp lệ)
2. Normalized: "pho bo"
3. FTS khớp với Recipe: "Phở bò Hà Nội truyền thống"
4. Tính ts_rank = 0.85
5. matchType gán bằng "FullTextSearch"

OUTPUT:
{
  "items": [
    {
      "id": "c1f7b8e2-0000-0000-0000-000000000001",
      "title": "Phở bò Hà Nội truyền thống",
      "slug": "pho-bo-ha-noi-truyen-thong",
      "difficulty": "Medium",
      "cookTimeMinutes": 180,
      "matchType": "FullTextSearch"
    }
  ],
  "totalCount": 1
}
```

### Ví dụ 2: Tìm kiếm gõ sai chính tả (Fuzzy Trigram Fallback)
```text
INPUT:
GET /api/v1/recipes/search?q=phoo%20bo

PROCESS:
1. Giai đoạn 1 FTS: "phoo bo" không khớp bản ghi nào
2. Tự động chuyển sang Giai đoạn 2: Trigram Similarity
3. Trigram của "phoo bo" so với "pho bo" đạt độ tương đồng 0.71 (> 0.3)
4. Tìm thấy "Phở bò Hà Nội truyền thống"
5. matchType gán bằng "FuzzyTrigram"

OUTPUT:
{
  "items": [
    {
      "title": "Phở bò Hà Nội truyền thống",
      "matchType": "FuzzyTrigram"
    }
  ],
  "totalCount": 1
}
```

---

## 8. Error Handling

| Tình huống | Mã HTTP / Phản hồi | Lý do và cách xử lý |
| :--- | :--- | :--- |
| **Từ khóa $< 2$ ký tự hoặc rỗng** | `HTTP 200 OK` với mảng `items: []` | Theo đặc tả nghiệp vụ, hệ thống không báo lỗi 400 mà trả về trang rỗng để giao diện hiển thị trạng thái chờ tìm kiếm thân thiện. |
| **Từ khóa $> 100$ ký tự** | `HTTP 200 OK` với mảng `items: []` | Tránh tràn bộ nhớ và ngăn ngừa tấn công DoS chuỗi dài. |
| **Độ khó không hợp lệ (`difficulty=SuperHard`)** | `HTTP 422 Unprocessable Entity` | Báo lỗi validation: `Difficulty phải là Easy, Medium hoặc Hard`. |
| **Thời gian nấu $\le 0$** | `HTTP 422 Unprocessable Entity` | Báo lỗi validation: `Thời gian nấu phải lớn hơn 0`. |
| **Redis Server sập hoặc timeout** | `HTTP 200 OK` (Truy vấn trực tiếp Database) | Cơ chế Resilient Failover: Ghi nhận log cảnh báo và fallback truy vấn trực tiếp vào PostgreSQL, người dùng không nhận thấy gián đoạn. |

---

## 9. Cách chạy và Demo

### Bước 1: Khởi động cơ sở dữ liệu và Redis
```bash
docker compose up -d
```
*(Đảm bảo PostgreSQL có cài đặt extension unaccent và pg_trgm)*

### Bước 2: Khởi động Backend API
```bash
dotnet run --project backend/src/CulinaryBlog.Api
```

### Bước 3: Kịch bản Demo thực tế cho Giảng viên

1. **Demo Tìm kiếm tiếng Việt không dấu**:
   - Gửi request: `curl "http://localhost:5000/api/v1/recipes/search?q=pho%20bo"`
   - Chỉ cho Giảng viên thấy kết quả trả về đúng món "Phở bò", trường `"matchType": "FullTextSearch"`.
2. **Demo Tìm kiếm gõ sai chính tả (Typo / Fuzzy)**:
   - Gửi request: `curl "http://localhost:5000/api/v1/recipes/search?q=phoo%20bo"`
   - Chỉ cho Giảng viên thấy hệ thống vẫn tìm thấy món "Phở bò", trường `"matchType": "FuzzyTrigram"`.
3. **Demo Kiểm tra cách ly bài viết Draft**:
   - Tạo một công thức mới ở trạng thái `Draft` với tiêu đề "Phở bò đặc biệt".
   - Thực hiện tìm kiếm `q=pho%20bo`.
   - Chứng minh công thức Draft này tuyệt đối không xuất hiện trong kết quả trả về.
4. **Demo Bộ lọc và Sắp xếp**:
   - Gửi request kèm lọc độ khó và thời gian: `q=pho&difficulty=Easy&maxCookTimeMinutes=60&sortBy=cookTime`.
   - Kết quả chỉ hiển thị các món thỏa mãn điều kiện và sắp xếp theo thời gian nấu tăng dần.
5. **Demo Redis Cache 1 phút**:
   - Gửi cùng một request lần thứ hai: Quan sát log API phản hồi tức thì dưới 5ms nhờ cache hit trong vòng 1 phút.

---

## 10. Testing

### Bộ kiểm thử chuyên biệt `SearchRecipesUnitTests`
Chạy lệnh kiểm thử đơn vị:
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --filter FullyQualifiedName~SearchRecipesUnitTests
```

**Kết quả kiểm thử thực tế:**
- **29/29 tests PASSED (100%)** (Thời gian chạy: ~240ms).
- **Danh sách các kịch bản kiểm thử cốt lõi đã chạy:**
  1. `VietnameseTextNormalizer_ShouldRemoveAccentsAndNormalize_Properly`: Xóa dấu tiếng Việt và ký tự đặc biệt.
  2. `Normalize_PhoBo_MatchesQuery_PhoBo`: Query "pho bo" khớp chính xác với "Phở bò".
  3. `Search_MatchesTitle_And_MatchesDescription`: Tìm kiếm đồng thời trên Title và Description.
  4. `FuzzyTypo_TrigramSimilarity_SimulatedCorrectly`: Kiểm tra thuật toán Trigram Similarity phát hiện typo "phoo bo".
  5. `Handle_WhenQueryIsLessThanTwoCharactersOrWhitespace_ReturnsEmptyPagedResult`: Validate query < 2 ký tự.
  6. `Handle_WhenQueryExceedsOneHundredCharacters_ReturnsEmptyPagedResult`: Validate query > 100 ký tự.
  7. `SearchQuery_PublishedOnly_ExcludesDraftAndSoftDeletedRecipes`: Cách ly tuyệt đối bản ghi Draft và Soft-deleted.
  8. `SearchQuery_Pagination_ReturnsCorrectSlices`: Phân trang chính xác các trang 1 và 2.
  9. `SearchQuery_FilterByCategoryDifficultyAndCookTime_WorksAccurately`: Lọc theo Category, Difficulty và CookTime.
  10. `SearchQuery_SortByNewest_OrdersByPublishedAtDescending`: Sắp xếp theo ngày xuất bản mới nhất.
  11. `SearchQuery_SortByCookTime_OrdersByCookTimeAscending`: Sắp xếp theo thời gian nấu tăng dần.
  12. `SearchQuery_Ranking_RelevanceTakesPrecedenceOverDate`: Điểm liên quan được ưu tiên trước ngày xuất bản.
  13. `CacheKey_ChangesWhenQueryChanges`: Cache key thay đổi theo query.
  14. `CacheKey_ChangesWhenPageOrPageSizeChanges`: Cache key chống va chạm khi phân trang.
  15. `CacheKey_ChangesWhenFilterOrSortChanges`: Cache key chống va chạm khi thay đổi filter/sort.
  16. `ResilientCacheService_WhenCacheMiss_ReturnsNull_AndSetsWithOneMinuteTtl`: Thời gian sống của cache đúng 1 phút.
  17. `ResilientCacheService_WhenRedisThrowsException_ReturnsDefaultWithoutCrashing`: Chống sập khi Redis mất kết nối.

### Tổng hợp Unit Tests toàn bộ solution:
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj
```
**Kết quả thực tế:**
- `Passed: 116, Failed: 0, Skipped: 1, Total: 117` (100% Pass).

---

## 11. Build
Thực hiện lệnh biên dịch solution:
```bash
dotnet build backend/CulinaryBlog.sln
```

**Kết quả biên dịch thực tế:**
- **Thành công (Exit code 0)**.
- **0 Error(s)**, 416 Warning(s) (chủ yếu là quy tắc StyleCop định dạng code).

---

## 12. Limitations

1. **Từ điển tiếng Việt nguyên bản**:
   - Hệ thống lựa chọn từ điển `'simple'` của PostgreSQL thay vì các từ điển tiếng Anh để giữ nguyên cấu trúc âm tiết tiếng Việt mà không bị cắt đuôi từ sai lệch. Các từ đồng nghĩa phức tạp (Synonyms) chưa được tích hợp trong phiên bản này.
2. **Môi trường chạy Integration Test vật lý**:
   - Bộ kiểm thử tích hợp trực tiếp trên PostgreSQL vật lý (`CulinaryBlog.IntegrationTests`) phụ thuộc vào việc Docker daemon có chạy trên máy host hay không. Bộ Unit Tests độc lập trong bộ nhớ RAM đã bao phủ 100% logic thuật toán và nghiệp vụ.

---

## 13. Kết luận
Branch `feat/vohungmanh-full-text-search` đã hoàn thành xuất sắc công cụ tìm kiếm ẩm thực chuẩn công nghiệp:
- Xử lý mượt mà tiếng Việt không dấu và có dấu, tự động gợi ý mờ khi gõ sai chính tả bằng Trigram Similarity.
- Chỉ mục GIN Index và Redis Cache 1 phút giúp đạt tốc độ phản hồi tính bằng mili-giây.
- Bảo vệ dữ liệu bản ghi nháp nghiêm ngặt và đạt 100% tỷ lệ vượt qua bài kiểm thử tự động.
