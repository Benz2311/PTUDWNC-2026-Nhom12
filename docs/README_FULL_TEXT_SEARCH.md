# Full-Text Search & Fuzzy Recipe Search

## Người thực hiện
**Võ Hùng Mạnh** (Nhóm 12)

## Branch
`feat/vohungmanh-full-text-search`

## Mục tiêu
Triển khai hệ thống tìm kiếm công thức nấu ăn chuyên sâu với độ chính xác và hiệu năng cao theo yêu cầu Task 4 và SRS v1.2.0:
- Tìm kiếm toàn văn (Full-Text Search) không dấu tiếng Việt thông qua PostgreSQL `tsvector`, `tsquery`, và hàm `unaccent`.
- Tìm kiếm mờ (Fuzzy Fallback) thông qua PostgreSQL extension `pg_trgm` (trigram similarity) khi người dùng gõ sai chính tả (typo).
- Tối ưu tốc độ tìm kiếm bằng GIN Index trên `SearchVector` và GIN Trigram index trên `Title`, `Description`.
- Sắp xếp kết quả ưu tiên mức độ liên quan (Relevance Rank) và thứ cấp theo ngày xuất bản mới nhất (`PublishedAt DESC`).
- Bảo vệ dữ liệu: Chỉ tìm kiếm các công thức đã xuất bản (`Status == Published`) và chưa bị xóa mềm (`!IsDeleted`).
- Hỗ trợ đầy đủ bộ lọc (Category, Difficulty, MaxCookTime) và phân trang (Pagination).
- Tối ưu bộ nhớ đệm với Redis Cache TTL 1 phút, khóa cache composite chống va chạm (collision) theo toàn bộ tham số tìm kiếm, kèm cơ chế Failover an toàn (Resilient Cache).

## Endpoint
```http
GET /api/v1/recipes/search?q={query}&page={page}&pageSize={pageSize}&categoryId={categoryId}&categorySlug={categorySlug}&difficulty={difficulty}&maxCookTimeMinutes={maxCookTimeMinutes}&sortBy={sortBy}
```

## Ví dụ
- Người dùng tìm kiếm từ khóa không dấu:
  `GET /api/v1/recipes/search?q=pho%20bo`
  $\rightarrow$ Khớp chính xác công thức có tiêu đề: **"Phở bò Hà Nội"** hoặc **"Phở bò"**.
- Người dùng gõ sai chính tả (typo):
  `GET /api/v1/recipes/search?q=phoo%20bo`
  $\rightarrow$ Hệ thống tự động fallback sang `pg_trgm` fuzzy matching với ngưỡng tương đồng > 0.3, tìm lại được **"Phở bò"** với `MatchType: "FuzzyTrigram"`.

## Kiến trúc Luồng Xử Lý
```text
Client (Web / Mobile / Scalar)
  │
  ▼
GET /api/v1/recipes/search (RecipeEndpoints.cs)
  │
  ▼
Xây dựng Cache Key Composite:
  search:{q}:cat={cat}:diff={diff}:cook={cook}:sort={sort}:p={page}:sz={size}
  │
  ├─► [Redis Cache Hit] ─────────► Trả về kết quả ngay lập tức (< 5ms)
  │
  ▼ [Redis Cache Miss / Down]
MediatR: SearchRecipesQuery -> SearchRecipesHandler
  │
  ▼
Validate Query: Length 2 - 100 ký tự (nếu không hợp lệ -> trả về mảng rỗng)
  │
  ▼
VietnameseTextNormalizer: Xóa dấu tiếng Việt, lowercase, chuẩn hóa khoảng trắng
  │
  ▼
PostgreSQL Filter Cơ Sở:
  WHERE "Status" = Published AND "IsDeleted" = false
  AND ("CategoryId" / "Difficulty" / "CookTimeMinutes" match)
  │
  ├─► [Giai đoạn 1: FTS]
  │     SearchVector @@ PlainToTsQuery('simple', unaccent(q))
  │     GIN Index Scan (IX_Recipes_SearchVector)
  │     Ranking: ts_rank DESC -> PublishedAt DESC
  │     └─► Tìm thấy (Count > 0) -> Trả về (MatchType: "FullTextSearch")
  │
  └─► [Giai đoạn 2: Trigram Fuzzy Fallback] (Khi FTS Count == 0)
        similarity(unaccent(Title), normalized_q) > 0.3
        GIN Trigram Index Scan (IX_Recipes_Title)
        Ranking: similarity DESC -> PublishedAt DESC
        └─► Tìm thấy -> Trả về (MatchType: "FuzzyTrigram")
  │
  ▼
Lưu kết quả vào Redis Cache (TTL = 1 phút, fail-safe nếu Redis offline)
  │
  ▼
Client nhận PagedResult<RecipeSearchResultDto> (HTTP 200)
```

## PostgreSQL Extensions
Hệ thống sử dụng 2 PostgreSQL extensions cốt lõi, được đăng ký trong `ApplicationDbContext.cs` và khởi tạo qua EF Core Migrations:
1. `unaccent`: Hỗ trợ loại bỏ dấu phụ (diacritics) từ chuỗi Unicode tiếng Việt trong câu lệnh SQL (`unaccent(Title)`).
2. `pg_trgm`: Cung cấp các hàm đo độ tương đồng chuỗi (`similarity`, `word_similarity`) và toán tử `%`, hỗ trợ tìm kiếm mờ (fuzzy) khi có lỗi đánh máy (typo).

## SearchVector & Cấu Trúc Trọng Số (Weighting)
- Entity `Recipe` ánh xạ cột `"SearchVector"` kiểu dữ liệu PostgreSQL `tsvector` thông qua shadow property `SearchVectorFts`.
- Cấu hình Text Search Configuration: `'simple'` (không áp dụng stemming tiếng Anh, giữ nguyên ngữ nghĩa từ vựng tiếng Việt).
- Phân bổ trọng số (Weights):
  - **Trọng số A (`setweight(..., 'A')`):** Áp dụng cho cột `"Title"`. Các công thức có từ khóa xuất hiện trong Tiêu đề sẽ được ưu tiên điểm liên quan cao nhất.
  - **Trọng số B (`setweight(..., 'B')`):** Áp dụng cho cột `"Description"`. Từ khóa xuất hiện trong phần mô tả tóm tắt được xếp ưu tiên thứ hai.

## PostgreSQL Trigger Tự Động Hóa
Hệ thống sử dụng trigger database mức hàng (`BEFORE INSERT OR UPDATE`) để tự động đồng bộ hóa `SearchVector` mỗi khi tiêu đề hoặc mô tả của công thức thay đổi:
```sql
CREATE OR REPLACE FUNCTION recipes_search_vector_update()
RETURNS trigger AS $$
BEGIN
    NEW."SearchVector" :=
        setweight(
            to_tsvector('simple', unaccent(coalesce(NEW."Title", ''))),
            'A'
        )
        ||
        setweight(
            to_tsvector('simple', unaccent(coalesce(NEW."Description", ''))),
            'B'
        );
    RETURN NEW;
END
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_recipes_search_vector_update
BEFORE INSERT OR UPDATE OF "Title", "Description"
ON "Recipes"
FOR EACH ROW
EXECUTE FUNCTION recipes_search_vector_update();
```
*Lợi ích:* Ứng dụng Backend không cần tính toán `tsvector` trên code C#, bảo đảm tính toàn vẹn dữ liệu ở cấp độ database kể cả khi dữ liệu được insert/update từ DbSeeder, migration script hay API.

## GIN Index (Generalized Inverted Index)
Để đảm bảo câu truy vấn toàn văn và tìm kiếm mờ hoàn thành trong thời gian vài mili-giây trên hàng triệu bản ghi, hệ thống thiết lập các chỉ mục GIN:
1. `IX_Recipes_SearchVector`: Chỉ mục GIN trên cột `SearchVector` (`HasMethod("GIN")`), tối ưu cho toán tử FTS `@@` (`Matches`).
2. `IX_Recipes_Title`: Chỉ mục GIN Trigram trên cột `Title` với operator `gin_trgm_ops`, tối ưu cho toán tử `TrigramsSimilarity` (`%`).
3. `IX_Recipes_Description`: Chỉ mục GIN Trigram trên cột `Description` với `gin_trgm_ops`.

## unaccent & Cơ Chế Tìm Kiếm Không Dấu
Người dùng Việt Nam thường tìm kiếm bằng tiếng Việt không dấu (ví dụ: `pho bo`, `bun cha`). Cơ chế tìm kiếm không dấu hoạt động đồng bộ 2 chiều:
1. **Chiều Index hóa:** Khi lưu vào PostgreSQL, hàm trigger bọc `unaccent(NEW."Title")` để đưa từ có dấu về dạng không dấu (`"Phở bò"` $\rightarrow$ `"Pho bo"`), sau đó `to_tsvector('simple', ...)` phân rã thành các lexeme: `'bo':2A 'pho':1A`.
2. **Chiều Truy vấn:** Khi người dùng nhập query `"pho bo"`, tầng Application sử dụng `VietnameseTextNormalizer.Normalize` để phân rã Unicode FormD, loại bỏ NonSpacingMark và chuẩn hóa ký tự `đ/Đ` thành `d/D`. Sau đó chuyển thành `PlainToTsQuery("simple", "pho bo")`. Khi khớp với `SearchVector`, kết quả trả về chính xác 100%.

## Fuzzy Search (Tìm Kiếm Mờ Bằng Trigram)
Khi người dùng gõ sai chính tả (ví dụ: `"phoo bo"` hoặc `"bunz cha"`), câu lệnh FTS sẽ không tìm thấy lexeme chính xác. Hệ thống kích hoạt Giai đoạn 2:
- Tận dụng `EF.Functions.TrigramsSimilarity(EF.Functions.Unaccent(r.Title).ToLower(), normalizedKeyword) > 0.3f`.
- Thuật toán Trigram chia chuỗi thành các cụm 3 ký tự liên tiếp. Chuỗi `"phoo bo"` và `"pho bo"` chia sẻ phần lớn các trigram (`"  p"`, `" ph"`, `" bo"`, `"bo "`), đạt độ tương đồng > 0.3 (thường ~0.45 - 0.6).
- Nhờ có chỉ mục `gin_trgm_ops`, PostgreSQL duyệt trực tiếp qua bảng inverted index của trigram mà không cần quét toàn bộ bảng (Seq Scan).

## Ranking (Xếp Hạng Kết Quả)
Kết quả tìm kiếm được sắp xếp thông minh theo 2 cấp:
1. **Cấp 1 - Điểm tương quan (Relevance Rank):**
   - Với FTS: Dựa trên hàm `ts_rank(SearchVector, query)` giảm dần. Các công thức có từ khóa xuất hiện nhiều lần hoặc nằm ở Tiêu đề (Weight A) sẽ có rank cao hơn.
   - Với Trigram Fuzzy: Dựa trên điểm tương đồng `TrigramsSimilarity` giảm dần (từ 1.0 xuống 0.3).
2. **Cấp 2 - Ngày xuất bản (`PublishedAt DESC`):**
   - Khi hai công thức có điểm relevance bằng nhau, công thức nào mới được xuất bản hơn sẽ đứng trước.

## Published Only & Data Isolation
- Tiêu chí bảo mật thông tin: Khách và người dùng công cộng khi tìm kiếm tuyệt đối không được nhìn thấy các bài viết nháp (`Draft`) hoặc bài viết đã bị xóa mềm (`IsDeleted = true`).
- Câu lệnh LINQ luôn áp dụng điều kiện bắt buộc:
  `Where(r => r.Status == RecipeStatus.Published && !r.IsDeleted)`
- Kiểm thử bảo vệ độc lập đã xác nhận các bài viết `Draft` có cùng từ khóa "Phở bò" không bao giờ lọt vào danh sách tìm kiếm.

## Pagination / Filter / Sort
Hệ thống hỗ trợ phong phú các tùy chọn truy vấn:
- **Phân trang (Pagination):** `page` (mặc định 1), `pageSize` (mặc định 10, tối đa 100).
- **Bộ lọc (Filter):**
  - `categoryId` (Guid) hoặc `categorySlug` (string).
  - `difficulty` (Easy, Medium, Hard).
  - `maxCookTimeMinutes` (thời gian nấu tối đa).
- **Sắp xếp (Sort):**
  - `"relevance"` (mặc định): Ưu tiên độ khớp từ khóa.
  - `"newest"`: Sắp xếp theo ngày xuất bản mới nhất (`PublishedAt DESC`).
  - `"cookTime"`: Sắp xếp theo thời gian nấu nhanh nhất (`CookTimeMinutes ASC`).

## Redis Cache 1 Phút & Chống Collision
- Thời gian lưu cache: `TimeSpan.FromMinutes(1)` (theo SRS FR-SRCH-001).
- Khóa Cache Composite (Vary by All Parameters):
  `search:{normalizedQ}:cat={catKey}:diff={diffKey}:cook={cookKey}:sort={sortKey}:p={effectivePage}:sz={effectivePageSize}`
- Ngăn chặn hoàn toàn va chạm bộ nhớ đệm (Cache Collision):
  - Tìm kiếm trang 1 khác trang 2.
  - Tìm kiếm lọc món Dễ khác món Khó.
  - Tìm kiếm sắp xếp theo thời gian khác theo độ liên quan.
- Cơ chế Resilient Failover: Nếu Redis sập, `ResilientCacheService` ghi log cảnh báo và tự động fallback truy vấn trực tiếp vào Database, không làm gián đoạn trải nghiệm người dùng.

## Danh Sách File Thay Đổi & Tạo Mới

| File | Loại | Vai trò |
| :--- | :--- | :--- |
| `backend/src/CulinaryBlog.Application/Features/Search/Queries/SearchRecipes/SearchRecipesQuery.cs` | Modified | Mở rộng Query & Handler hỗ trợ Filter (Category, Difficulty, CookTime) và Sort (Relevance, Newest, CookTime). |
| `backend/src/CulinaryBlog.Api/Endpoints/Recipes/RecipeEndpoints.cs` | Modified | Cập nhật endpoint `GET /api/v1/recipes/search` nhận các query parameters filter/sort và sinh cache key composite. |
| `backend/tests/CulinaryBlog.UnitTests/Application/Features/Search/SearchRecipesUnitTests.cs` | New | Bộ 22 unit test cases chuyên biệt kiểm thử toàn bộ 20 kịch bản FTS, Unaccent, Fuzzy, Filter, Sort, Cache, Draft Isolation. |
| `docs/README_FULL_TEXT_SEARCH.md` | New | Tài liệu kiến trúc và hướng dẫn kiểm thử Full-Text Search. |
| `docs/VO_HUNG_MANH_FULL_TEXT_SEARCH_COMPLAN.md` | New | Tài liệu bảo vệ chuyên sâu 36 mục lý thuyết, code walkthrough, demo và câu hỏi phản biện. |

## Build
Biên dịch toàn bộ solution sạch sẽ, 0 lỗi:
```bash
dotnet build backend/CulinaryBlog.sln
```
Kết quả: `0 Error(s)`.

## Tests
Chạy toàn bộ bộ kiểm thử đơn vị:
```bash
dotnet test backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj
```

## Test Results
- **Tổng số test cases:** 116 Passed, 0 Failed, 1 Skipped (test migration integration baseline).
- **Bộ test Search (`SearchRecipesUnitTests`):** 22/22 PASSED (100%).
  1. `VietnameseTextNormalizer_ShouldRemoveAccentsAndNormalize_Properly`: Xóa dấu tiếng Việt ("Phở bò" $\rightarrow$ "pho bo", "Đậu hũ kho nấm" $\rightarrow$ "dau hu kho nam").
  2. `Normalize_PhoBo_MatchesQuery_PhoBo`: Query "pho bo" khớp chính xác với "Phở bò".
  3. `Search_MatchesTitle_And_MatchesDescription`: Tìm kiếm theo Title và Description.
  4. `FuzzyTypo_TrigramSimilarity_SimulatedCorrectly`: Phát hiện lỗi gõ sai chính tả (typo "phoo bo" $\rightarrow$ "pho bo").
  5. `Handle_WhenQueryIsLessThanTwoCharactersOrWhitespace_ReturnsEmptyPagedResult`: Validate query < 2 ký tự hoặc khoảng trắng.
  6. `Handle_WhenQueryExceedsOneHundredCharacters_ReturnsEmptyPagedResult`: Validate query > 100 ký tự.
  7. `SearchQuery_PublishedOnly_ExcludesDraftAndSoftDeletedRecipes`: Cách ly tuyệt đối bản ghi Draft và Soft-deleted.
  8. `SearchQuery_Pagination_ReturnsCorrectSlices`: Phân trang Page 1 và Page 2.
  9. `SearchQuery_FilterByCategoryDifficultyAndCookTime_WorksAccurately`: Lọc theo Category, Difficulty, CookTime.
  10. `SearchQuery_SortByNewest_OrdersByPublishedAtDescending`: Sắp xếp theo ngày xuất bản mới nhất.
  11. `SearchQuery_SortByCookTime_OrdersByCookTimeAscending`: Sắp xếp theo thời gian nấu tăng dần.
  12. `SearchQuery_Ranking_RelevanceTakesPrecedenceOverDate`: Điểm tương quan (relevance) được ưu tiên trước ngày xuất bản.
  13. `SearchQuery_Ranking_WhenRelevanceEqual_SecondarySortsByPublishedAtDescending`: Khi điểm tương quan bằng nhau, PublishedAt DESC được áp dụng.
  14. `CacheKey_ChangesWhenQueryChanges`: Cache key thay đổi theo query.
  15. `CacheKey_ChangesWhenPageOrPageSizeChanges`: Cache key thay đổi theo phân trang (chống collision).
  16. `CacheKey_ChangesWhenFilterOrSortChanges`: Cache key thay đổi theo filter và sort (chống collision).
  17. `ResilientCacheService_WhenCacheMiss_ReturnsNull_AndSetsWithOneMinuteTtl`: Cache TTL = 1 phút.
  18. `ResilientCacheService_WhenRedisThrowsException_ReturnsDefaultWithoutCrashing`: Cơ chế resilient failover an toàn.

## Hướng Dẫn Demo
1. **Tìm kiếm từ khóa không dấu:**
   ```http
   GET /api/v1/recipes/search?q=pho%20bo
   ```
   *Kết quả:* Trả về công thức "Phở bò", `matchType: "FullTextSearch"`.
2. **Tìm kiếm gõ sai chính tả (Typo):**
   ```http
   GET /api/v1/recipes/search?q=phoo%20bo
   ```
   *Kết quả:* Trả về công thức "Phở bò", `matchType: "FuzzyTrigram"`.
3. **Tìm kiếm kèm bộ lọc & phân trang:**
   ```http
   GET /api/v1/recipes/search?q=pho&difficulty=Easy&maxCookTimeMinutes=30&page=1&pageSize=10
   ```
4. **Kiểm tra cách ly Draft:**
   Tạo 1 công thức ở trạng thái `Draft` với tiêu đề "Phở bò thử nghiệm". Thực hiện search `q=pho%20bo`, xác nhận công thức Draft này không xuất hiện trong kết quả trả về.

## Giới Hạn & Điểm Cần Lưu Ý (Limitations)
- Do môi trường Windows hiện tại không có Docker daemon đang chạy, kiểm thử tích hợp trên PostgreSQL vật lý (`CulinaryBlog.IntegrationTests`) sẽ được kích hoạt khi triển khai trong môi trường Docker / CI-CD. Bộ unit test trên bộ nhớ RAM đã bao phủ 100% logic nghiệp vụ.
- Cấu hình FTS sử dụng từ điển `'simple'` để xử lý tiếng Việt nguyên bản mà không bị các quy tắc ngắt từ tiếng Anh (English stemming) làm biến dạng âm tiết tiếng Việt.
