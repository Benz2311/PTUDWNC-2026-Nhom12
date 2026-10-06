# Giao diện Tìm kiếm công thức (Recipe Search UI)

## 1. Mục tiêu

Module Giao diện Tìm kiếm công thức (`Recipe Search UI`) được phát triển nhằm đáp ứng nhu cầu tra cứu công thức nấu ăn nhanh chóng, chính xác và trực quan cho người dùng CulinaryBlog:
- **Tối ưu trải nghiệm tìm kiếm cho người dùng Việt Nam**: Cung cấp giao diện tra cứu hỗ trợ nhập tiếng Việt có dấu hoặc không dấu (vd: `pho bo` tìm ra `Phở bò Hà Nội`), hiển thị rõ ràng kết quả khớp chính xác (Full-Text Search) hoặc gợi ý tương đồng (Fuzzy Search Fallback).
- **Bộ điều khiển toàn diện (Filter, Sort & Pagination)**: Cho phép người dùng lọc kết quả theo Độ khó (Tất cả, Dễ, Trung bình, Khó), sắp xếp theo Độ liên quan / Mới nhất / Thời gian nấu nhanh nhất, và điều hướng phân trang mượt mà.
- **Độc lập và khả năng tái sử dụng**: Xây dựng thành các component chuyên trách (`SearchBox`, `SearchResultCard`, `SearchContainer`) và tuyến trang riêng biệt `/search` (với Suspense boundary), dễ dàng nhúng thanh tìm kiếm vào Navbar hoặc Header chung sau này mà không làm ảnh hưởng đến mã nguồn của các thành viên khác.

---

## 2. Kết quả đạt được

Sau khi branch `feat/vohungmanh-search-ui` được triển khai, hệ thống đạt được các năng lực UI thực tế:
- **Thanh tìm kiếm SearchBox đa năng**: Hỗ trợ kích hoạt tìm kiếm bằng phím `Enter` hoặc bấm nút kính lúp, nút xóa nhanh từ khóa (`x`), và danh sách thẻ gợi ý từ khóa phổ biến (Phở bò, Bún chả, Gà nướng...).
- **Xác thực từ khóa Client-side**: Kiểm tra độ dài từ khóa từ 2 đến 100 ký tự theo đúng hợp đồng nghiệp vụ, báo lỗi thân thiện ngay dưới input nếu nhập dưới 2 ký tự.
- **Component SearchResultCard trực quan**: Render từng bài viết với hình ảnh tỷ lệ 16:9, tiêu đề, tóm tắt nội dung (line-clamp), thời gian chuẩn bị và nấu, độ khó, tác giả, danh mục, và badge phân biệt phương thức khớp dữ liệu:
  - `Khớp toàn văn (FTS)`: Badge màu xanh lá đậm cho kết quả chính xác từ PostgreSQL FTS.
  - `Gợi ý tương đồng (Fuzzy)`: Badge màu vàng/amber khi kích hoạt thuật toán Trigram similarity (gõ sai chính tả).
- **Container quản lý trạng thái hoàn chỉnh (SearchContainer)**: Điều phối trạng thái Khởi tạo (Initial), Đang tải (Loading Skeleton), Không có kết quả (Empty/No results kèm gợi ý), và Báo lỗi mạng (Error Banner).
- **Trang `/search` độc lập**: Đồng bộ trạng thái tìm kiếm với URL query parameters (vd: `/search?q=pho+bo`), cho phép bookmark và chia sẻ liên kết trực tiếp.
- **Bộ kiểm thử tự động 100% PASS**: 15/15 unit tests chuyên biệt cho `SearchUI.test.tsx` (tổng 27/27 tests frontend) đạt kết quả thành công.

---

## 3. Luồng hoạt động

Luồng dữ liệu và điều khiển từ khi người dùng nhập từ khóa đến khi kết quả hiển thị:

```
User nhập từ khóa (SearchBox)
   │ (Nhấn Enter hoặc click nút "Tìm kiếm")
   ▼
Validation Client-side (Độ dài từ 2 đến 100 ký tự sau khi trim)
   │
   ├── [Dưới 2 ký tự] ──> Báo lỗi đỏ & Chặn gửi request
   │
   └── [Hợp lệ]
         │
         ▼
      SearchContainer kích hoạt Loading State (hiển thị 3 Card Skeletons)
         │
         ▼
      Search API Client (lib/api/search.ts via apiFetch / apiJson)
         │ [GET /api/v1/recipes/search?q={query}&page={page}&pageSize={pageSize}]
         ▼
      Backend Search Endpoint (FTS Vector -> Trigram Fuzzy Fallback -> Redis Cache)
         │
         ├── [Thành công có dữ liệu] ──> Render danh sách SearchResultCard (Badge FTS / Fuzzy)
         │                                 + Bảng điều khiển Filter (Difficulty) & Sort (Relevant/Newest/CookTime)
         │                                 + Điều hướng phân trang (Pagination: Prev/Next)
         │
         ├── [Thành công không có kết quả] ──> Render No Results State kèm từ khóa gợi ý
         │
         └── [Lỗi kết nối / Server 500]   ──> Render Error Banner màu đỏ
```

### Giải thích chi tiết các bước:
1. **Bước 1 - Người dùng nhập từ khóa**: Người dùng gõ từ khóa vào `SearchBox` (hoặc bấm chọn một thẻ gợi ý từ khóa) và nhấn phím `Enter` hoặc click nút "Tìm kiếm".
2. **Bước 2 - Kiểm tra hợp lệ (Validation)**: Chuỗi tìm kiếm được `.trim()`. Nếu độ dài $< 2$ ký tự, giao diện hiển thị cảnh báo đỏ và không gọi API. Nếu độ dài từ 2 đến 100 ký tự, cho phép thực hiện.
3. **Bước 3 - Hiển thị Loading State**: `SearchContainer` đặt `isLoading = true`, tạm ẩn kết quả cũ và hiển thị các khối Card Skeleton với hiệu ứng nhấp nháy (shimmer).
4. **Bước 4 - Gửi HTTP Request**: API Client gọi `searchRecipes(query, page, pageSize)` tương ứng với `GET /api/v1/recipes/search?q=...`.
5. **Bước 5 - Nhận kết quả và Render UI**:
   - Khi nhận được dữ liệu, tắt loading. Mỗi phần tử trong mảng `items` được render thành một `SearchResultCard`.
   - Dựa vào trường `matchType`, card hiển thị badge tương ứng: `Khớp toàn văn (FTS)` nếu là `FullTextSearch`, hoặc `Gợi ý tương đồng (Fuzzy)` nếu là `FuzzyTrigram`.
   - Nếu mảng `items` rỗng, hiển thị màn hình thông báo không tìm thấy kết quả và đề xuất các từ khóa thay thế.
   - Nếu có lỗi kết nối, hiển thị Banner màu đỏ ở đầu trang có nút `x` để đóng.
6. **Bước 6 - Tương tác lọc, sắp xếp và phân trang**: Người dùng chọn độ khó (Easy, Medium, Hard), sắp xếp theo thời gian, hoặc chuyển trang -> `SearchContainer` cập nhật state và kích hoạt lấy lại dữ liệu trang mới tương ứng.

---

## 4. Các file chính

| File | Vai trò | Xử lý gì |
| :--- | :--- | :--- |
| `frontend/culinary-blog-web/components/search/SearchBox.tsx` | Thanh tìm kiếm | Ô nhập liệu, icon kính lúp, nút xóa nhanh (`x`), nút tìm kiếm, validation 2-100 ký tự, hỗ trợ phím Enter và danh sách thẻ gợi ý từ khóa |
| `frontend/culinary-blog-web/components/search/SearchResultCard.tsx` | Thẻ kết quả bài viết | Hiển thị thumbnail 16:9, badge phương thức khớp (FTS màu xanh lá / Fuzzy màu vàng), thời gian nấu, độ khó, tác giả, danh mục và link chi tiết |
| `frontend/culinary-blog-web/components/search/SearchContainer.tsx` | Container điều phối | Quản lý active query, kết quả tìm kiếm, bộ lọc độ khó, sắp xếp, phân trang, các trạng thái Initial, Loading Skeleton, Empty, và Error banner |
| `frontend/culinary-blog-web/app/search/page.tsx` | Tuyến trang Next.js | Trang độc lập `/search`, đọc query param `?q=` từ URL và bọc `SearchContainer` bên trong `Suspense` boundary |
| `frontend/culinary-blog-web/lib/api/search.ts` | API Client Module | Chứa hàm `searchRecipes(query, page, pageSize)` gọi endpoint `GET /api/v1/recipes/search` |
| `frontend/culinary-blog-web/lib/api.ts` | Core HTTP Client | Hàm tiện ích `apiFetch`, `apiJson`, xử lý query parameters và parse lỗi HTTP |
| `frontend/culinary-blog-web/types/search.ts` | TypeScript Interfaces | Định nghĩa kiểu `RecipeSearchResult`, `SearchResponse`, `SearchFilterState` |
| `frontend/culinary-blog-web/components/search/__tests__/SearchUI.test.tsx` | Unit Test Suite | 15 test cases kiểm tra SearchBox, validation, phím Enter, Loading Skeleton, FTS/Fuzzy badge, filter, sort, pagination, empty và error states |
| `frontend/culinary-blog-web/jest.config.mjs` | Test Configuration | Cấu hình Jest ES Module cho Next.js 15 |
| `docs/README_SEARCH_UI.md` | Tài liệu kỹ thuật | Hướng dẫn kiến trúc, luồng hoạt động, validation và kết quả kiểm thử của branch Search UI |
| `docs/VO_HUNG_MANH_SEARCH_UI_COMPLAN.md` | Tài liệu giải trình | Báo cáo chi tiết, câu hỏi bảo vệ và ma trận đánh giá rủi ro |

---

## 5. API / Interface

### Component Props & Interface

| Component | Input / Props | API sử dụng | Output UI |
| :--- | :--- | :--- | :--- |
| **SearchBox** | `initialQuery?: string`<br>`onSearch: (query: string) => void`<br>`isLoading?: boolean`<br>`placeholder?: string` | Không gọi trực tiếp (gửi callback lên cha) | Thanh tìm kiếm, nút submit, nút xóa nhanh, danh sách gợi ý từ khóa phổ biến, thông báo lỗi validation |
| **SearchResultCard** | `recipe: RecipeSearchResult` | Không gọi trực tiếp | Thẻ bài viết tỷ lệ 16:9, badge `Khớp toàn văn (FTS)` hoặc `Gợi ý tương đồng (Fuzzy)`, metadata thời gian, tác giả |
| **SearchContainer** | `initialQuery?: string` | `searchRecipes` | Giao diện tìm kiếm hoàn chỉnh: SearchBox, thanh công cụ Lọc & Sắp xếp, danh sách `SearchResultCard`, phân trang, Loading Skeletons |

### REST Endpoints mà `lib/api/search.ts` giao tiếp:

| Method | Endpoint | Query Parameters | Output | Authorization |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/recipes/search` | `q`: string (2-100 ký tự)<br>`page`: number (mặc định 1)<br>`pageSize`: number (mặc định 10) | `SearchResponse` (`items: RecipeSearchResult[]`, `totalCount`, `totalPages`, `hasPreviousPage`, `hasNextPage`) | Không bắt buộc (Public API) |

---

## 6. Business Rules

1. **Quy tắc kiểm tra độ dài từ khóa (Query Length Validation)**:
   - Từ khóa sau khi cắt khoảng trắng thừa (`trim`) phải có độ dài tối thiểu là **2 ký tự** (theo quy định SRS FR-SRCH-001 để tránh full scan cơ sở dữ liệu với từ khóa 1 ký tự).
   - Độ dài tối đa được khống chế ở mức **100 ký tự** (`maxLength={100}`) để ngăn chặn request quá tải.
2. **Kích hoạt tìm kiếm kép (Dual Trigger)**:
   - Người dùng có thể kích hoạt tìm kiếm bằng cách bấm chuột vào nút "Tìm kiếm" hoặc nhấn phím `Enter` trên bàn phím khi đang focus trong ô nhập liệu.
3. **Phân biệt trực quan phương thức khớp dữ liệu (Match Type Badges)**:
   - Nếu `matchType === "FullTextSearch"`: Giao diện hiển thị badge màu xanh lá cây đậm `"Khớp toàn văn (FTS)"` khẳng định kết quả khớp chính xác theo chỉ mục văn bản.
   - Nếu `matchType === "FuzzyTrigram"`: Giao diện hiển thị badge màu vàng/amber `"Gợi ý tương đồng (Fuzzy)"` giải thích cho người dùng biết kết quả được gợi ý do gõ sai chính tả nhẹ.
4. **Quy tắc phân trang an toàn (Safe Pagination)**:
   - Nút "Trang trước" tự động vô hiệu hóa (`disabled`) khi `hasPreviousPage === false` hoặc `page <= 1`.
   - Nút "Trang sau" tự động vô hiệu hóa (`disabled`) khi `hasNextPage === false` hoặc `page >= totalPages`.
5. **Bộ lọc cục bộ & Sắp xếp linh hoạt**:
   - Hỗ trợ lọc tức thì danh sách kết quả theo độ khó (`All`, `Easy`, `Medium`, `Hard`).
   - Hỗ trợ sắp xếp danh sách kết quả theo Độ liên quan (thứ tự FTS rank mặc định), Thời gian nấu nhanh nhất (`cookTimeMinutes` tăng dần), hoặc Mới nhất (`publishedAt` giảm dần).

---

## 7. Ví dụ hoạt động

### Ví dụ: Tìm kiếm từ khóa không dấu "pho bo"

```
INPUT:
- Người dùng truy cập trang /search
- Nhập từ khóa: "pho bo"
- Nhấn phím Enter trên bàn phím

↓ PROCESS:
1. SearchBox kiểm tra: "pho bo".length = 6 -> Nằm trong [2, 100] -> HỢP LỆ.
2. Gọi callback onSearch("pho bo").
3. SearchContainer chuyển sang trạng thái isLoading = true, hiển thị 3 Card Skeletons nhấp nháy.
4. API Client gọi: GET /api/v1/recipes/search?q=pho+bo&page=1&pageSize=10.
5. Backend xử lý:
   - Chuẩn hóa text tiếng Việt không dấu: "pho bo"
   - Truy vấn PostgreSQL FTS Vector, tìm thấy công thức "Phở bò Hà Nội truyền thống".
   - Trả về JSON:
     {
       "items": [{
         "id": "c1f7b8e2-...",
         "title": "Phở bò Hà Nội truyền thống",
         "slug": "pho-bo-ha-noi-truyen-thong",
         "description": "Nước dùng trong vắt, đậm đà từ xương bò hầm 8 tiếng...",
         "prepTimeMinutes": 30,
         "cookTimeMinutes": 180,
         "difficulty": "Medium",
         "matchType": "FullTextSearch",
         "primaryImageUrl": "https://example.com/pho-bo.jpg"
       }],
       "totalCount": 1,
       "page": 1,
       "totalPages": 1
     }
6. SearchContainer cập nhật searchResponse, tắt loading.

↓ OUTPUT:
- Giao diện hiển thị:
  * Thanh thông báo: "Tìm thấy 1 kết quả cho từ khóa 'pho bo'"
  * Thẻ SearchResultCard cho bài viết "Phở bò Hà Nội truyền thống"
  * Badge màu xanh lá đậm: "Khớp toàn văn (FTS)"
  * Nhãn thời gian: "Chuẩn bị: 30p | Nấu: 180p"
  * Nhãn độ khó: "Trung bình"
```

---

## 8. Error Handling

| Tình huống lỗi | Hành vi UI | Thông điệp hiển thị |
| :--- | :--- | :--- |
| Nhập từ khóa dưới 2 ký tự (vd: "a") | Chặn gửi request | Chữ đỏ dưới ô input: `"Từ khóa tìm kiếm phải có ít nhất 2 ký tự."` |
| Nhập chuỗi chỉ gồm khoảng trắng (vd: "   ") | Chặn gửi request | Chữ đỏ dưới ô input: `"Từ khóa tìm kiếm phải có ít nhất 2 ký tự."` |
| Không tìm thấy công thức nào phù hợp | Tắt loading, chuyển sang Empty State | Tiêu đề: `"Không tìm thấy công thức phù hợp"`, gợi ý thử từ khóa khác hoặc bấm vào các thẻ đề xuất |
| Mất kết nối máy chủ hoặc lỗi HTTP 500 từ Backend | Bắt lỗi trong `catch` block | Banner cảnh báo đỏ ở đầu trang: `"Không thể kết nối đến máy chủ tìm kiếm. Vui lòng thử lại sau."` |
| Lỗi tải ảnh đại diện bài viết | Fallback ảnh mặc định | Ẩn ảnh hỏng hoặc hiển thị placeholder để không làm vỡ layout |

---

## 9. Cách chạy và Demo

### Điều kiện tiên quyết
- Node.js 18+ hoặc 20+
- Dependencies frontend đã cài đặt đầy đủ

### Lệnh chạy môi trường phát triển
```bash
cd frontend/culinary-blog-web
npm run dev
```
Truy cập trình duyệt: `http://localhost:3000/search`.

### Kịch bản Demo cho Giảng viên (8 bước):
1. **Demo Trang Tìm kiếm ban đầu**: Mở `http://localhost:3000/search` -> Trình diễn giao diện khởi tạo với thanh tìm kiếm nổi bật và danh sách các thẻ từ khóa gợi ý (Phở bò, Bún chả...).
2. **Demo Validation dưới 2 ký tự**: Nhập chữ "a" và nhấn Enter -> Hệ thống lập tức hiển thị cảnh báo đỏ và không gọi request.
3. **Demo Tìm kiếm tiếng Việt không dấu ("pho bo")**: Nhập "pho bo" và bấm "Tìm kiếm" -> Xem hiệu ứng Skeleton loading nhấp nháy, sau đó render kết quả bài viết "Phở bò Hà Nội truyền thống".
4. **Demo Badge Khớp toàn văn (FTS)**: Chỉ rõ badge màu xanh lá `"Khớp toàn văn (FTS)"` trên thẻ kết quả phở bò.
5. **Demo Gợi ý tương đồng khi gõ sai (Fuzzy Fallback)**: Nhập từ khóa "phoo bo" -> Hệ thống hiển thị kết quả với badge màu vàng `"Gợi ý tương đồng (Fuzzy)"`.
6. **Demo Bộ lọc Độ khó (Difficulty Filter)**: Tại thanh công cụ lọc, chọn độ khó "Dễ" -> Danh sách chỉ hiển thị các công thức có độ khó tương ứng.
7. **Demo Sắp xếp theo Thời gian nấu (Sort by Cook Time)**: Chọn sắp xếp theo thời gian nấu nhanh nhất -> Thứ tự các thẻ kết quả được cập nhật tương ứng.
8. **Demo Trạng thái không có kết quả**: Nhập từ khóa ngẫu nhiên "xyz123abc" -> Giao diện hiển thị màn hình thông báo không tìm thấy kết quả và đề xuất các từ khóa thay thế.

---

## 10. Testing

### Bộ kiểm thử tự động Frontend
Chạy kiểm thử bằng Jest:
```powershell
cd frontend/culinary-blog-web
npm test
```

### Kết quả kiểm thử thực tế
- **Test Suites**: `2 passed, 2 total`
- **Tests**: `27 passed, 27 total`
- **Snapshots**: `0 total`
- **Time**: ~7.057 s

### 15 kịch bản kiểm thử trong `SearchUI.test.tsx` (15/15 PASS):
1. `renders search box with input and buttons`: PASS
2. `shows validation error when query length is less than 2`: PASS
3. `enforces max length of 100 characters on input`: PASS
4. `triggers search when clicking search button`: PASS
5. `triggers search when pressing Enter key`: PASS
6. `shows loading skeleton during search`: PASS
7. `renders search results correctly with metadata`: PASS
8. `displays FullTextSearch badge correctly`: PASS
9. `displays FuzzyTrigram badge correctly`: PASS
10. `searches unaccented query "pho bo" successfully`: PASS
11. `renders empty state when no results found`: PASS
12. `displays error banner when API returns error`: PASS
13. `handles pagination page change correctly`: PASS
14. `filters search results by difficulty`: PASS
15. `sorts search results by cook time`: PASS

---

## 11. Build

### Lệnh đóng gói build
```powershell
cd frontend/culinary-blog-web
$env:NODE_OPTIONS="--max-old-space-size=4096"; npm run build
```

### Kết quả build thực tế
- **Framework**: Next.js 15.5.25 (Turbopack)
- **Status**: Compiled successfully (exit code 0)
- **Static Pages Generated**: `6/6` routes (bao gồm tuyến tĩnh `/search` dung lượng `6.22 kB`)
- **Type Checking**: Zero TypeScript error
- **Bundle Output**:
  - Route `/search`: 6.22 kB (First Load JS: 122 kB)
  - Route `/`: 5.47 kB (First Load JS: 121 kB)
  - Shared JS chunks: 124 kB

---

## 12. Limitations

1. **Phân biệt giữa Frontend Unit Tests và PostgreSQL Full-Text Search thật**:
   - Bộ kiểm thử `SearchUI.test.tsx` là **Frontend Unit Tests** chạy trên môi trường Jest/jsdom, sử dụng mock client để kiểm tra các hành vi tương tác UI, validation, render badges và pagination.
   - Các hành vi phân tích từ khóa thực tế (`unaccent`, GIN index, `SearchVector`, tính điểm ranking `ts_rank`, và ngưỡng tương đồng Trigram `0.3`) thuộc quyền sở hữu của backend và cơ sở dữ liệu PostgreSQL (đã được kiểm chứng trong branch backend `feat/vohungmanh-full-text-search`).
   - Tìm kiếm End-to-End từ trình duyệt thật đến CSDL thật đòi hỏi cả hệ thống Web API và PostgreSQL container đang vận hành đồng thời.
2. **Ranh giới tích hợp vào Navbar toàn cục**:
   - `SearchBox` hiện tại được gắn mặc định tại trang độc lập `/search` để bảo vệ mã nguồn layout chung của nhóm.
   - Thành viên phụ trách Layout/Navbar (TV1/TV2) có thể tái sử dụng trực tiếp component `SearchBox` để nhúng lên thanh điều hướng chung khi thực hiện tích hợp toàn hệ thống.

---

## 13. Kết luận

Branch `feat/vohungmanh-search-ui` đã hoàn thiện toàn diện module giao diện tìm kiếm công thức (`SearchBox`, `SearchResultCard`, `SearchContainer`, và trang `/search`):
- Trải nghiệm tìm kiếm tối ưu cho người dùng Việt Nam (hỗ trợ tiếng Việt không dấu, kích hoạt linh hoạt qua phím `Enter` hoặc click).
- Trực quan hóa kết quả tìm kiếm với phân loại rõ ràng qua các badge `Khớp toàn văn (FTS)` và `Gợi ý tương đồng (Fuzzy)`.
- Đầy đủ tính năng bổ trợ: Bộ lọc độ khó, sắp xếp linh hoạt, phân trang an toàn, xử lý đầy đủ các trạng thái Loading Skeleton, Empty và Error.
- Đạt chất lượng kiểm thử cao với **15/15 unit tests passed** và biên dịch production build thành công 0 lỗi.
