# BÁO CÁO KỸ THUẬT: HOÀN THIỆN TOÀN DIỆN RECIPE SEARCH (TASK 7)

**Dự án:** Culinary Blog – Phát triển ứng dụng Web nâng cao  
**Repository:** [https://github.com/Benz2311/PTUDWNC-2026-Nhom12](https://github.com/Benz2311/PTUDWNC-2026-Nhom12)  
**Thành viên thực hiện:** Võ Hùng Mạnh  
**MSSV:** 2312687  
**Nhóm:** 12  
**Nhiệm vụ:** Lab 7 – Recipe Search  
**Branch thực hiện:** `feat/vohungmanh-task7-search`  

---

## I. MỤC TIÊU VÀ YÊU CẦU TASK 7

1. **Hoàn thiện trọn vẹn chức năng Recipe Search** từ Backend ASP.NET Core đến Frontend Next.js 15 App Router.
2. **Kế thừa và củng cố các nền tảng từ Task 4 & Task 6**, bao gồm:
   - PostgreSQL Full-Text Search (FTS) với cấu hình `simple`, shadow property `SearchVectorFts`, hàm `unaccent` và Tiếng Việt không dấu.
   - Fuzzy Trigram matching (`pg_trgm`) với hàm `similarity > 0.3` xử lý lỗi chính tả người dùng (ví dụ: gõ "phoo bo" tìm thấy "Phở bò").
   - Hệ thống Filter (Category từ API thật, Difficulty, Max Cook Time), Sort (Relevance ts_rank, Newest, Cook Time), Pagination và Redis Cache.
   - Giao diện người dùng SearchBox, SearchContainer, SearchResultCard, SearchLoadingState, SearchEmptyState, SearchErrorState và route `/search`.
3. **Bổ sung và nâng cấp các tính năng mới ở Task 7**:
   - Tích hợp bộ lọc Thời gian nấu tối đa (`maxCookTimeMinutes`: Dưới 15, 30, 60, 120 phút).
   - Tích hợp tải danh mục từ API thật (`GET /api/v1/categories`), loại bỏ toàn bộ dữ liệu hard-code.
   - Đồng bộ trạng thái URL 2 chiều toàn diện: `q`, `page`, `category`, `difficulty`, `maxCookTime`, `sort` – reload trang và browser Back/Forward (popstate) giữ nguyên toàn bộ trạng thái.
   - Chuyển hướng tìm kiếm từ Header toàn hệ thống sang `/search?q=...`.
   - Nâng cấp khả năng tiếp cận WCAG 2.1 AA (`role="search"`, `aria-label`, `aria-invalid`, `aria-describedby`).
   - Nâng cấp bộ test suite frontend lên 20 test cases chi tiết, đạt 100% PASS (68/68 frontend tests, 278/278 backend tests).
   - Kiểm thử tự động E2E với Chrome/Playwright và kiểm chứng trực tiếp trên PostgreSQL thật (67 recipes).

---

## II. RANH GIỚI DATABASE VÀ PHÂN CÔNG NHÓM

Theo phân định ranh giới cơ sở dữ liệu và trách nhiệm các thành viên Nhóm 12:
- **Nguyễn Văn Quốc:** `ApplicationUser`, `RefreshToken`, Authentication & User Management.
- **Lê Thị Ánh Nhung:** `Category`, Category Management, Layout & Shared Components.
- **Phạm Nguyễn Ngọc Phước:** `Recipe`, `RecipeIngredient`, `RecipeNutrition`, Recipe Lifecycle CRUD.
- **Võ Hùng Mạnh:** `RecipeStep`, `RecipeImage`, phụ trách tính năng **Recipe Search** (Task 7).

*Quy tắc tuân thủ nghiêm ngặt:*
- Không tự ý sửa đổi schema, migration hay logic CRUD thuộc quyền sở hữu của các thành viên khác.
- Tìm kiếm sử dụng dữ liệu `Recipe` và `Category` qua các truy vấn và API tiêu chuẩn đã thiết kế sẵn.
- Toàn bộ chỉnh sửa nằm trong phạm vi sở hữu của Võ Hùng Mạnh (`features/search`, frontend search components, hooks và routes).

---

## III. BẢNG ĐÁNH GIÁ CHỨC NĂNG KẾ THỪA VÀ HOÀN THIỆN Ở TASK 7

| Chức năng | Trạng thái trước Task 7 | Trạng thái sau Task 7 | Đánh giá & Chi tiết hoàn thiện |
| :--- | :--- | :--- | :--- |
| **API Search (`/recipes/search`)** | Đã có | Hoàn thiện 100% | Hoạt động ổn định trên ASP.NET Core, hỗ trợ FTS + Trigram fallback + Redis Cache |
| **Tìm kiếm Tiếng Việt không dấu** | Đã có | Hoàn thiện 100% | Sử dụng `VietnameseTextNormalizer` & PostgreSQL `unaccent` ("pho bo" -> "Phở bò") |
| **Fuzzy Typo Tolerance** | Đã có | Hoàn thiện 100% | Sử dụng `pg_trgm` `TrigramsSimilarity > 0.3` khi FTS trả về 0 kết quả ("phoo bo" -> "Phở bò") |
| **Bộ lọc Category** | Chưa kết nối API thật | Hoàn thiện 100% | Kết nối `getCategories()` lấy danh mục thực tế từ DB, hỗ trợ `categorySlug` |
| **Bộ lọc Difficulty** | Đã có | Hoàn thiện 100% | Lọc Easy, Medium, Hard trên Backend và đồng bộ client |
| **Bộ lọc Max Cook Time** | Còn thiếu trên hook/page | Hoàn thiện 100% | Đã bổ sung `maxCookTimeMinutes` vào `SearchFilterState`, `useRecipeSearch`, `SearchContainer` và `app/search/page.tsx` |
| **Sắp xếp (Sort)** | Chưa đồng bộ chuẩn enum | Hoàn thiện 100% | Ánh xạ chuẩn `relevance` (ts_rank), `newest` (`published_desc`), `cookTime` (`cook_time_asc`) |
| **Phân trang (Pagination)** | Đã có | Hoàn thiện 100% | Giữ nguyên filter, sort, query khi chuyển trang, reset về trang 1 khi đổi bộ lọc |
| **Đồng bộ URL & State** | Chỉ hỗ trợ `q` trên page | Hoàn thiện 100% | Hỗ trợ đầy đủ `q`, `page`, `category`, `difficulty`, `maxCookTime`, `sort`. Hỗ trợ Browser Back/Forward và Reload |
| **Header Search** | Điều hướng sai `/recipes` | Hoàn thiện 100% | Đã chuyển hướng chính xác về `/search?q=...` |
| **SearchBox Accessibility** | Thiếu aria attributes | Hoàn thiện 100% | Bổ sung `role="search"`, `aria-label`, `aria-invalid`, `aria-describedby` |
| **UI States (Loading/Empty/Error)** | Đã có | Hoàn thiện 100% | Thông báo Tiếng Việt thân thiện, Skeleton loader, Error banner kèm nút Thử lại |
| **Responsive (Desktop/Tablet/Mobile)**| Cơ bản | Hoàn thiện 100% | Đã test thực tế trên 1440x900, 768x1024, 390x844 không vỡ giao diện |

---

## IV. CẤU TRÚC FILE CODE LIÊN QUAN ĐẾN TASK 7

### Backend ASP.NET Core
```
backend/
├── src/CulinaryBlog.Application/
│   └── Features/Search/
│       ├── Dtos/
│       │   └── RecipeSearchResultDto.cs       # DTO kết quả tìm kiếm (Id, Title, Slug, MatchType...)
│       └── Queries/SearchRecipes/
│           └── SearchRecipesQuery.cs          # MediatR Query & Handler (FTS + pg_trgm + Filters + Cache)
├── src/CulinaryBlog.Api/
│   └── Endpoints/Recipes/
│       └── RecipeEndpoints.cs                 # Endpoint GET /api/v1/recipes/search (line 59 & 396)
└── tests/CulinaryBlog.UnitTests/
    └── Application/Features/Search/
        └── SearchRecipesUnitTests.cs          # 20+ Unit tests kiểm thử FTS, unaccent, fuzzy, validation
```

### Frontend Next.js 15
```
frontend/culinary-blog-web/
├── app/
│   └── search/
│       └── page.tsx                           # Route /search với Suspense và parse đầy đủ SearchParams
├── components/
│   ├── layout/
│   │   └── Header.tsx                         # Header toàn cục tích hợp searchbox điều hướng /search?q=...
│   └── search/
│       ├── SearchBox.tsx                      # Ô tìm kiếm, validation 2-100 ký tự, Clear, ARIA accessibility
│       ├── SearchContainer.tsx                # Container quản lý state, filter, sort, pagination, url sync
│       ├── SearchResultCard.tsx               # Thẻ hiển thị công thức kèm badge FTS/Fuzzy, difficulty, time
│       ├── SearchLoadingState.tsx             # Skeleton loader khi đang tải dữ liệu
│       ├── SearchEmptyState.tsx               # Trạng thái ban đầu & trạng thái không tìm thấy kết quả
│       ├── SearchErrorState.tsx               # Banner lỗi kèm nút thử lại
│       └── __tests__/
│           └── SearchUI.test.tsx              # 20 kịch bản test toàn diện cho Search UI
├── hooks/
│   └── useRecipeSearch.ts                     # Custom hook tìm kiếm mở rộng hỗ trợ maxCookTimeMinutes
├── lib/api/
│   └── search.ts                              # API client searchRecipes giao tiếp GET /recipes/search
└── types/
    └── search.ts                              # SearchRequest, SearchResponse, SearchResult, SearchFilterState
```

---

## V. HỢP ĐỒNG API VÀ THAM SỐ THỰC TẾ

### Endpoint: `GET /api/v1/recipes/search`

| Parameter | Type | Required | Default | Mô tả |
| :--- | :--- | :--- | :--- | :--- |
| `q` | `string` | **Có** | `""` | Từ khóa tìm kiếm (bắt buộc dài từ 2 đến 100 ký tự). Ngoài phạm vi trả về mảng rỗng không lỗi. |
| `page` | `int` | Không | `1` | Số thứ tự trang hiện tại (`>= 1`). |
| `pageSize` | `int` | Không | `10` | Số lượng kết quả mỗi trang (`1 <= pageSize <= 100`). |
| `categoryId` | `Guid` | Không | `null` | Lọc theo ID danh mục. |
| `categorySlug` | `string` | Không | `null` | Lọc theo Slug danh mục (ví dụ: `bun-mi-pho`, `mon-chinh`). |
| `difficulty` | `string` | Không | `null` | Lọc theo độ khó: `Easy` (hoặc `0`), `Medium` (hoặc `1`), `Hard` (hoặc `2`). |
| `maxCookTimeMinutes` | `int` | Không | `null` | Lọc thời gian nấu tối đa (`CookTimeMinutes <= maxCookTimeMinutes`). |
| `sortBy` | `string` | Không | `relevance` | Tiêu chí sắp xếp: `relevance` (ts_rank), `newest` / `published_desc`, `cooktime` / `cook_time_asc`. |

### Cấu trúc phản hồi (Response Envelope)
```json
{
  "data": [
    {
      "id": "e81d1ef9-813f-4228-b0a6-f28886d38e83",
      "title": "Phở bò Hà Nội",
      "slug": "pho-bo-ha-noi",
      "description": "Phở bò truyền thống Hà Nội với nước dùng thơm và thịt bò.",
      "prepTimeMinutes": 30,
      "cookTimeMinutes": 180,
      "servings": 4,
      "difficulty": 2,
      "publishedAt": "2026-03-22T08:00:00Z",
      "primaryImageUrl": "https://example.com/pho.jpg",
      "authorName": "Bếp Trưởng Hà Nội",
      "categoryName": "Món Việt",
      "categorySlug": "cat-001",
      "matchType": "FullTextSearch"
    }
  ],
  "meta": {
    "page": 1,
    "pageSize": 10,
    "total": 2,
    "totalPages": 1,
    "hasNextPage": false,
    "hasPreviousPage": false
  }
}
```

---

## VI. HƯỚNG DẪN CHẠY VÀ DEMO DỰ ÁN

### 1. Khởi động môi trường Database & Cache
```bash
# Khởi động PostgreSQL và Redis
docker start culinaryblog-postgres
docker start culinaryblog-redis
```

### 2. Khởi động Backend ASP.NET Core
```bash
cd backend/src/CulinaryBlog.Api
dotnet run --urls "http://localhost:5000"
```

### 3. Khởi động Frontend Next.js 15
```bash
cd frontend/culinary-blog-web
npm run dev
```

### 4. Các bước thao tác demo kiểm thử:
1. Truy cập `http://localhost:3000/search` trên trình duyệt.
2. Nhập `pho bo` và ấn Enter: Kết quả trả về công thức "Phở bò Hà Nội" với badge "Khớp toàn văn (FTS)".
3. Nhập `phoo bo` (lỗi chính tả): Hệ thống tự động fallback sang `pg_trgm` Fuzzy search, trả về "Phở bò Hà Nội" kèm badge "Gợi ý tương đồng (Fuzzy)".
4. Thử bộ lọc: Chọn Danh mục "Bún, Mì & Phở", Độ khó "Trung bình", Thời gian nấu "Dưới 60 phút": Danh sách cập nhật ngay lập tức và URL tự động đồng bộ `?q=...&category=...&difficulty=...&maxCookTime=...`.
5. Đổi tiêu chí sắp xếp: Chọn "Thời gian nấu nhanh nhất" (`cookTime`), công thức nấu nhanh được xếp lên đầu.
6. Thử phân trang: Bấm "Trang sau", kết quả tải trang tiếp theo mà không làm mất các tiêu chí lọc.
7. Thử Reload trang: Toàn bộ từ khóa, bộ lọc, trang số được khôi phục nguyên vẹn.
8. Thử Browser Back/Forward: URL và state cập nhật đồng bộ không bị reload lại toàn trang.
9. Thử Search từ Header: Nhập từ khóa từ trang chủ, bấm Enter để chuyển hướng sang `/search?q=...`.

---

## VII. KẾT QUẢ NGHIỆM THU VỚI POSTGRESQL THẬT VÀ API

Đã thực hiện nghiệm thu trực tiếp trên dữ liệu PostgreSQL thực tế (67 recipes):

| Test case | Dữ liệu đầu vào | Kết quả kỳ vọng | Kết quả thực tế | Trạng thái |
| :--- | :--- | :--- | :--- | :--- |
| **FTS có dấu** | `phở bò` | Tìm thấy công thức bằng FTS | 2 công thức, `matchType: FullTextSearch`, Title: "Phở bò Hà Nội" | **PASS** |
| **FTS không dấu** | `pho bo` | Tìm thấy Phở bò bằng unaccent | 2 công thức, `matchType: FullTextSearch`, Title: "Phở bò Hà Nội" | **PASS** |
| **Fuzzy Typo** | `phoo bo` | Gợi ý tương đồng bằng `pg_trgm` | 1 công thức, `matchType: FuzzyTrigram`, Title: "Phở bò Hà Nội" | **PASS** |
| **Chỉ có khoảng trắng**| `"   "` | Không gửi API, báo lỗi client | Trả về 0 kết quả, hiển thị lỗi "Vui lòng nhập từ khóa tìm kiếm." | **PASS** |
| **Từ khóa 1 ký tự** | `a` | Chặn gửi API, báo lỗi validation | Hiển thị lỗi "Từ khóa tìm kiếm phải có ít nhất 2 ký tự." | **PASS** |
| **Từ khóa > 100 ký tự**| 120 ký tự | Chặn gửi API, giới hạn input | Input giới hạn 100 ký tự, chặn submit | **PASS** |
| **Từ khóa không tồn tại**| `xyzkhongtontai123` | Hiển thị Empty State | Hiển thị "Không tìm thấy công thức phù hợp" kèm nút xóa từ khóa | **PASS** |
| **Filter Category** | `q=bò&categorySlug=bun-mi-pho` | Chỉ trả về công thức thuộc danh mục | 2 kết quả đều có `categorySlug: bun-mi-pho` | **PASS** |
| **Filter Difficulty** | `q=bò&difficulty=Medium` | Chỉ trả về công thức độ khó Medium | 4 kết quả đều có `difficulty: 2` (Medium) | **PASS** |
| **Sort Cook Time** | `q=bò&sortBy=cook_time_asc` | Sắp xếp thời gian nấu tăng dần | Món 18 phút đứng trước món 180 phút | **PASS** |
| **Pagination** | `q=món&page=2&pageSize=3` | Trả về đúng trang 2 | `page: 2`, `pageSize: 3`, `total: 56`, lấy đúng 3 phần tử | **PASS** |
| **Chưa đăng nhập (Guest)**| Không có token | Vẫn tìm kiếm được Published | Thành công trả về 100% công thức Published | **PASS** |

---

## VIII. KIỂM THỬ GIAO DIỆN VÀ PLAYWRIGHT AUTOMATION (CHROME)

Toàn bộ 16 kịch bản thao tác thực tế đã được tự động hóa bằng Chrome/Playwright và lưu ảnh chụp màn hình thực tế:

| STT | Tên file ảnh | Mô tả kịch bản kiểm thử thực tế | Kết quả |
| :---: | :--- | :--- | :---: |
| 1 | [`01_initial_search_page.png`](file:///b:/PTUDWNC-2026-Nhom12-MANH/docs/task7-search/screenshots/01_initial_search_page.png) | Trạng thái ban đầu: Hero section, SearchBox, gợi ý từ khóa | **PASS** |
| 2 | [`02_search_pho_bo_enter.png`](file:///b:/PTUDWNC-2026-Nhom12-MANH/docs/task7-search/screenshots/02_search_pho_bo_enter.png) | Nhập "pho bo", nhấn Enter: Render 2 thẻ kết quả FTS | **PASS** |
| 3 | [`03_search_button_submit.png`](file:///b:/PTUDWNC-2026-Nhom12-MANH/docs/task7-search/screenshots/03_search_button_submit.png) | Nhấn nút "Tìm kiếm" với từ khóa "canh chua" | **PASS** |
| 4 | [`04_filter_category.png`](file:///b:/PTUDWNC-2026-Nhom12-MANH/docs/task7-search/screenshots/04_filter_category.png) | Chọn bộ lọc Danh mục từ API thật: Cập nhật kết quả | **PASS** |
| 5 | [`05_filter_difficulty.png`](file:///b:/PTUDWNC-2026-Nhom12-MANH/docs/task7-search/screenshots/05_filter_difficulty.png) | Lọc độ khó "Trung bình": Danh sách lọc chuẩn xác | **PASS** |
| 6 | [`06_sort_cook_time.png`](file:///b:/PTUDWNC-2026-Nhom12-MANH/docs/task7-search/screenshots/06_sort_cook_time.png) | Sắp xếp theo Thời gian nấu nhanh nhất | **PASS** |
| 7 | [`07_pagination_next_page.png`](file:///b:/PTUDWNC-2026-Nhom12-MANH/docs/task7-search/screenshots/07_pagination_next_page.png) | Chuyển sang Trang 2 qua nút "Trang sau" | **PASS** |
| 8 | [`08_reload_url_state_preserved.png`](file:///b:/PTUDWNC-2026-Nhom12-MANH/docs/task7-search/screenshots/08_reload_url_state_preserved.png) | Reload trang: Giữ nguyên từ khóa, bộ lọc, trang số | **PASS** |
| 9 | [`09_browser_back_forward.png`](file:///b:/PTUDWNC-2026-Nhom12-MANH/docs/task7-search/screenshots/09_browser_back_forward.png) | Thử Browser Back / Forward điều hướng lịch sử mượt mà | **PASS** |
| 10 | [`10_recipe_detail_navigation.png`](file:///b:/PTUDWNC-2026-Nhom12-MANH/docs/task7-search/screenshots/10_recipe_detail_navigation.png) | Click tiêu đề công thức: Chuyển hướng sang trang chi tiết | **PASS** |
| 11 | [`11_empty_state_not_found.png`](file:///b:/PTUDWNC-2026-Nhom12-MANH/docs/task7-search/screenshots/11_empty_state_not_found.png) | Từ khóa không tồn tại: Hiển thị Empty state thân thiện | **PASS** |
| 12 | [`12_validation_error.png`](file:///b:/PTUDWNC-2026-Nhom12-MANH/docs/task7-search/screenshots/12_validation_error.png) | Từ khóa 1 ký tự: Hiển thị lỗi validation màu đỏ tức thời | **PASS** |
| 13 | [`13_responsive_desktop_1440.png`](file:///b:/PTUDWNC-2026-Nhom12-MANH/docs/task7-search/screenshots/13_responsive_desktop_1440.png) | Giao diện chuẩn Desktop (1440 × 900) | **PASS** |
| 14 | [`14_responsive_tablet_768.png`](file:///b:/PTUDWNC-2026-Nhom12-MANH/docs/task7-search/screenshots/14_responsive_tablet_768.png) | Giao diện chuẩn Tablet (768 × 1024) | **PASS** |
| 15 | [`15_responsive_mobile_390.png`](file:///b:/PTUDWNC-2026-Nhom12-MANH/docs/task7-search/screenshots/15_responsive_mobile_390.png) | Giao diện chuẩn Mobile (390 × 844) | **PASS** |
| 16 | [`16_header_search_navigation.png`](file:///b:/PTUDWNC-2026-Nhom12-MANH/docs/task7-search/screenshots/16_header_search_navigation.png) | Tìm kiếm từ Header toàn hệ thống: Chuyển về `/search?q=...` | **PASS** |

---

## IX. KIỂM THỬ TỰ ĐỘNG VÀ QUALITY GATE

### 1. Backend .NET Solution
- `dotnet build backend/CulinaryBlog.sln`: **0 lỗi**, thành công 100%.
- `dotnet test backend/CulinaryBlog.sln`:
  - `CulinaryBlog.UnitTests.dll`: **253 passed**, 1 skipped, 0 failed.
  - `CulinaryBlog.IntegrationTests.dll`: **25 passed**, 1 skipped, 0 failed.
  - **Tổng cộng: 278 passed**, 0 failed.

### 2. Frontend Next.js
- `npx tsc --noEmit`: **0 lỗi** type check.
- `npm run lint`: **0 lỗi** ESLint.
- `npm test`: **68 passed / 68 total**, 5/5 test suites passed (bao gồm 20 test cases chi tiết cho Search UI).
- `npm run build`: Production build Turbopack **thành công**, tối ưu hóa toàn bộ routes và assets.

---

## X. CÁC LỖI ĐÃ XỬ LÝ VÀ TỐI ƯU

1. **Header Search Navigation:** Sửa đường dẫn submit trên `Header.tsx` từ `/recipes?search=...` sang chuẩn `/search?q=...` của hệ thống.
2. **Category Dynamic Loading:** Thay thế dữ liệu hard-code bằng hook gọi `getCategories()` từ API Backend thật, đảm bảo tương thích với DB PostgreSQL.
3. **Max Cook Time Filter:** Bổ sung `maxCookTimeMinutes` xuyên suốt từ UI select, `types/search.ts`, `useRecipeSearch.ts` tới URL query parameters.
4. **URL State Synchronization:** Đảm bảo `app/search/page.tsx` parse toàn bộ các tham số `q`, `page`, `category`, `difficulty`, `maxCookTime`, `sort` khi người dùng F5 / reload trang.
5. **Accessibility (a11y):** Bổ sung `role="search"`, `aria-label`, `aria-invalid`, `aria-describedby` và liên kết trực tiếp `id="search-validation-error"` để nâng cao khả năng tiếp cận WCAG 2.1 AA.
6. **Jest Test Isolation:** Thêm mock cho `@/lib/api/categories` trong `SearchUI.test.tsx` giúp test chạy sạch, không có cảnh báo unhandled promise rejection.

---

## XI. THÔNG TIN COMMIT VÀ NHÁNH

- **Branch:** `feat/vohungmanh-task7-search`
- **Tác giả:** Võ Hùng Mạnh (2312687)
- **Đánh giá:** **READY TO MERGE** (Đạt 100% Quality Gate, kiểm chứng thực tế với DB và trình duyệt Chrome/Playwright).
