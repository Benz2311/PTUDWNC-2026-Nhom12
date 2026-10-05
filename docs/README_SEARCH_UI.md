# Võ Hùng Mạnh - Giao diện Tìm kiếm công thức (Recipe Search UI)

## 1. Thông tin branch
- **Thành viên:** Võ Hùng Mạnh
- **Task:** Task 4 - Frontend Recipe Search UI
- **Tên branch:** `feat/vohungmanh-search-ui`
- **Base branch / Base commit:** `origin/main` (`a91589618f6460fc37e9b4861f2c5a0e946311b5`)
- **Mục tiêu branch:** Xây dựng module giao diện tìm kiếm công thức nấu ăn toàn diện (`SearchBox`, `SearchResultCard`, `SearchContainer`, trang `/search`), hỗ trợ tìm kiếm toàn văn Full-Text Search (FTS) và gợi ý Trigram Fuzzy Fallback, xử lý từ khóa tiếng Việt không dấu và có dấu ("pho bo" -> "Phở bò"), phân trang, bộ lọc và sắp xếp, đồng bộ visual design với dự án Culinary Blog.

---

## 2. Branch này làm gì?
Branch triển khai đầy đủ các tính năng tìm kiếm công thức thực tế trong mã nguồn:
- **Search Box:** Thanh tìm kiếm hiện đại với biểu tượng kính lúp, nút xóa nhanh từ khóa (`x`), gợi ý từ khóa phổ biến (Phở bò, Bún chả, Gà nướng...).
- **Xác thực từ khóa (Query Validation):** Kiểm tra độ dài từ 2 đến 100 ký tự theo chuẩn hợp đồng nghiệp vụ backend. Báo lỗi thân thiện nếu từ khóa < 2 ký tự.
- **Tìm kiếm đa phương thức:** Kích hoạt tìm kiếm linh hoạt bằng nút bấm "Tìm kiếm" hoặc phím `Enter` trên bàn phím.
- **Search Results Display:** Hiển thị danh sách kết quả dưới dạng thẻ bài viết trực quan, hình ảnh tỉ lệ 16:9, tiêu đề, tóm tắt, thời gian chuẩn bị và nấu, độ khó, tác giả và danh mục.
- **Badge phân biệt phương thức khớp dữ liệu:**
  - `Khớp toàn văn (FTS):` Badge xanh lá đậm cho kết quả tìm thấy qua PostgreSQL Full-Text Search.
  - `Gợi ý tương đồng (Fuzzy):` Badge vàng/amber cho kết quả tìm thấy qua thuật toán pg_trgm similarity (khi người dùng gõ sai chính tả nhẹ).
- **Bộ lọc & Sắp xếp (Filter & Sort):**
  - Lọc nhanh theo Độ khó: Tất cả, Dễ, Trung bình, Khó.
  - Sắp xếp theo: Độ liên quan (FTS Rank), Mới nhất, Thời gian nấu nhanh nhất.
- **Phân trang (Pagination):** Hiển thị số trang hiện tại `Trang {page} / {totalPages}`, tổng số lượng kết quả, nút chuyển trang trước và sau với kiểm soát trạng thái biên.
- **Xử lý trạng thái (UI States):**
  - *Initial state:* Màn hình hướng dẫn khi người dùng chưa nhập từ khóa.
  - *Loading state:* Skeleton cards tạo hiệu ứng mượt mà trong khi chờ kết quả API.
  - *No results state:* Thông báo "Không tìm thấy công thức phù hợp" kèm gợi ý từ khóa liên quan.
  - *Error state:* Banner thông báo lỗi màu đỏ khi mất kết nối máy chủ.
- **Trang độc lập:** Khởi tạo trang `/search` hỗ trợ đọc query param trực tiếp từ URL (vd: `/search?q=pho+bo`).

---

## 3. Branch này KHÔNG làm gì?
Nhằm đảm bảo tính độc lập và phân chia nhiệm vụ rành mạch trong Nhóm 12:
- KHÔNG chỉnh sửa form tạo/sửa công thức (`RecipeForm.tsx` thuộc quyền quản lý của TV3 - Phạm Nguyễn Ngọc Phước).
- KHÔNG chỉnh sửa `StepEditor` (thuộc branch riêng `feat/vohungmanh-step-ui`).
- KHÔNG chỉnh sửa `ImageManager` (thuộc branch riêng `feat/vohungmanh-image-ui`).
- KHÔNG can thiệp vào trang danh mục (`Category` của TV2 - Lê Thị Ánh Nhung) hay quản lý người dùng (Auth của TV1).
- KHÔNG can thiệp chỉnh sửa cấu hình Full-Text Search trong PostgreSQL ở branch này (tầng DB đã được tích hợp trước đó).

---

## 4. Cấu trúc file
Dưới đây là danh sách CHÍNH XÁC các file được tạo hoặc sửa đổi trên branch:

- `frontend/culinary-blog-web/components/search/SearchBox.tsx`: Component thanh tìm kiếm (input, icon kính lúp, nút xóa, nút submit, validation 2-100 ký tự, hỗ trợ phím Enter).
- `frontend/culinary-blog-web/components/search/SearchResultCard.tsx`: Thẻ hiển thị từng công thức trong kết quả tìm kiếm kèm badge FTS / Fuzzy và thông tin chi tiết.
- `frontend/culinary-blog-web/components/search/SearchContainer.tsx`: Container component chính quản lý toàn bộ luồng tìm kiếm, bộ lọc, sắp xếp, phân trang và các trạng thái giao diện.
- `frontend/culinary-blog-web/app/search/page.tsx`: Tuyến trang Next.js độc lập (`/search`) nhúng `SearchContainer` với Suspense wrapper.
- `frontend/culinary-blog-web/components/search/__tests__/SearchUI.test.tsx`: Test suite gồm 15 kịch bản kiểm thử toàn diện cho module Search UI.
- `frontend/culinary-blog-web/lib/api.ts`: Module abstraction dùng chung cho HTTP request (`apiFetch`, `apiJson`, `ApiError`).
- `frontend/culinary-blog-web/lib/api/search.ts`: API Client tương tác với endpoint tìm kiếm của Backend.
- `frontend/culinary-blog-web/types/search.ts`: Định nghĩa TypeScript interfaces (`RecipeSearchResult`, `SearchResponse`, `SearchFilterState`).
- `frontend/culinary-blog-web/jest.config.mjs`: Cấu hình Jest ES Module native.
- `frontend/culinary-blog-web/package.json`: Cập nhật script test trỏ tới `jest.config.mjs`.
- `docs/README_SEARCH_UI.md`: Tài liệu kỹ thuật chi tiết của branch Search UI.
- `docs/VO_HUNG_MANH_SEARCH_UI_COMPLAN.md`: Báo cáo giải trình kỹ thuật bảo vệ đồ án, FTS/Trigram, 10 câu hỏi phản biện.

---

## 5. Component Architecture
Kiến trúc luồng dữ liệu của module Search UI:

```
[ app/search/page.tsx ] (Next.js Page)
          │
          ▼
┌────────────────────────────────────────────────────────┐
│                  SearchContainer.tsx                   │
│  - Quản lý state: activeQuery, page, searchResponse    │
│  - Quản lý bộ lọc (difficulty) & sắp xếp (sortBy)      │
│  - Điều khiển phân trang (Pagination Controls)         │
│  - Quản lý Loading (Skeleton), Empty, No Results, Error│
└───────────────┬────────────────────────┬───────────────┘
                │                        │
       renders  │               renders  │ (Lặp qua từng item)
                ▼                        ▼
     ┌────────────────────┐    ┌────────────────────┐
     │   SearchBox.tsx    │    │ SearchResultCard   │
     │  - Input form      │    │  - Thumbnail cover │
     │  - Validation 2-100│    │  - FTS/Fuzzy badge │
     │  - Nút Enter/Click │    │  - Category/Time   │
     └────────────────────┘    │  - Link chi tiết   │
                               └────────────────────┘
```

---

## 6. API được sử dụng
Module giao tiếp với RESTful endpoint tìm kiếm sau trên Backend:

- **Endpoint:** `GET /api/v1/recipes/search`
- **Query Parameters:**
  - `q` (string, bắt buộc từ 2-100 ký tự): Từ khóa tìm kiếm (hỗ trợ cả tiếng Việt không dấu và có dấu, ví dụ `pho bo` hoặc `phở bò`).
  - `page` (int, mặc định `1`): Số thứ tự trang hiện tại.
  - `pageSize` (int, mặc định `10`, tối đa `100`): Số lượng kết quả trên một trang.
- **Response DTO (`PagedResult<RecipeSearchResultDto>`):**
  ```json
  {
    "items": [
      {
        "id": "c1f7b8e2-...",
        "title": "Phở bò Hà Nội truyền thống",
        "slug": "pho-bo-ha-noi-truyen-thong",
        "description": "Nước dùng trong vắt, đậm đà từ xương bò hầm 8 tiếng...",
        "prepTimeMinutes": 30,
        "cookTimeMinutes": 180,
        "servings": 4,
        "difficulty": "Medium",
        "publishedAt": "2026-03-01T08:00:00Z",
        "primaryImageUrl": "https://example.com/pho-bo.jpg",
        "authorName": "Võ Hùng Mạnh",
        "categoryName": "Món Nước",
        "categorySlug": "mon-nuoc",
        "matchType": "FullTextSearch"
      }
    ],
    "page": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  }
  ```

---

## 7. Luồng hoạt động

### Luồng tìm kiếm từ khóa ("pho bo" -> "Phở bò"):
1. Người dùng nhập từ khóa "pho bo" vào `SearchBox` và nhấn phím `Enter` (hoặc bấm nút "Tìm kiếm").
2. `SearchBox` kiểm tra validation: từ khóa có độ dài 6 ký tự (hợp lệ trong khoảng 2–100 ký tự).
3. `SearchContainer` kích hoạt trạng thái Loading (hiển thị 3 skeleton card nhấp nháy).
4. API Client gọi `GET /api/v1/recipes/search?q=pho+bo&page=1&pageSize=10`.
5. Tại Backend:
   - Backend chuẩn hóa từ khóa `VietnameseTextNormalizer.Normalize("pho bo")` -> `pho bo`.
   - Đối chiếu với shadow property `SearchVectorFts` (được đánh chỉ mục GIN với hàm `unaccent` và từ điển `simple`).
   - Tìm thấy công thức "Phở bò Hà Nội truyền thống", chấm điểm xếp hạng `ts_rank` và trả về với `matchType = "FullTextSearch"`.
   - Kết quả được lưu tạm 1 phút trong Redis Cache.
6. Client nhận response JSON, tắt loading và hiển thị `SearchResultCard` với badge "Khớp toàn văn (FTS)".

### Luồng Fuzzy Search Fallback (Gõ sai chính tả):
1. Người dùng gõ nhầm từ khóa "phoo bo" -> Gửi request lên Backend.
2. Giai đoạn 1 (FTS) không tìm thấy bản ghi nào khớp chính xác.
3. Backend tự động chuyển sang Giai đoạn 2: Thuật toán Trigram Similarity (`pg_trgm`) so sánh độ tương đồng với ngưỡng `> 0.3`.
4. Tìm thấy công thức "Phở bò Hà Nội truyền thống", trả về kết quả với `matchType = "FuzzyTrigram"`.
5. Client hiển thị kết quả với badge màu vàng/amber "Gợi ý tương đồng (Fuzzy)".

---

## 8. Validation
- **Độ dài tối thiểu:** Từ khóa phải có ít nhất 2 ký tự (theo quy định SRS FR-SRCH-001 để tránh full table scan với từ khóa quá ngắn). Nếu < 2 ký tự, UI hiển thị thông báo lỗi màu đỏ và không gửi request lên server.
- **Độ dài tối đa:** Ô nhập liệu giới hạn thuộc tính `maxLength={100}`, ngăn người dùng nhập quá 100 ký tự.
- **Loại bỏ khoảng trắng thừa:** Chuỗi tìm kiếm tự động được `.trim()` trước khi kiểm tra và gửi đi.

---

## 9. Error Handling
- **Lỗi mạng hoặc 500 Internal Server Error:** Hiển thị Error Banner màu đỏ thân thiện ở đầu trang: "Không thể kết nối đến máy chủ tìm kiếm. Vui lòng thử lại sau."
- **Không có kết quả tìm kiếm (Empty Response):** Hiển thị màn hình rỗng thông báo "Không tìm thấy công thức phù hợp cho từ khóa..." kèm theo các từ khóa gợi ý thay thế.
- **Đóng thông báo:** Nút `x` trên banner cho phép người dùng đóng cảnh báo lỗi bất cứ lúc nào.

---

## 10. Visual Design
Giao diện bám sát phong cách thiết kế đặc trưng của Culinary Blog:
- **Tone màu chủ đạo:** Màu xanh lá đậm (`#166534` / `bg-green-800` / `hover:bg-green-900`) dùng cho nút "Tìm kiếm", badge danh mục và badge FTS.
- **Thẻ Card kết quả:** Nền trắng (`bg-white`), bo góc mềm mại (`rounded-xl`), viền xám mảnh nhẹ (`border-gray-200 hover:border-gray-300`), không sử dụng shadow đậm.
- **Typography:** Phông chữ rõ ràng, phân cấp thị giác hợp lý giữa Tiêu đề bài viết (font-bold) và Mô tả tóm tắt (line-clamp-2, text-gray-600).
- **Responsive:** Co giãn linh hoạt từ điện thoại di động (layout 1 cột, ảnh trên chữ dưới) đến máy tính bảng và desktop (layout hàng ngang, ảnh bên trái, thông tin bên phải).

---

## 11. Testing
- **File kiểm thử:** `frontend/culinary-blog-web/components/search/__tests__/SearchUI.test.tsx`
- **Lệnh chạy kiểm thử:**
  ```powershell
  cd frontend/culinary-blog-web
  npm test
  ```
- **Danh sách 15 kịch bản kiểm thử đã thực hiện:**
  1. Render Search Box với đầy đủ ô nhập liệu, icon và nút tìm kiếm.
  2. Báo lỗi khi từ khóa tìm kiếm ít hơn 2 ký tự.
  3. Giới hạn độ dài ô nhập liệu tối đa 100 ký tự.
  4. Kích hoạt tìm kiếm khi người dùng nhấn nút Tìm kiếm.
  5. Kích hoạt tìm kiếm khi người dùng nhấn phím Enter trên bàn phím.
  6. Hiển thị trạng thái Loading Skeletons trong khi đang chờ kết quả từ API.
  7. Render đầy đủ các thẻ kết quả công thức với thông tin chuẩn DTO.
  8. Hiển thị badge "Khớp toàn văn (FTS)" màu xanh lá cho kết quả FTS.
  9. Hiển thị badge "Gợi ý tương đồng (Fuzzy)" màu vàng cho kết quả Trigram.
  10. Tìm kiếm từ khóa không dấu "pho bo" gọi đúng hàm searchRecipes.
  11. Hiển thị thông điệp "Không tìm thấy công thức phù hợp" khi API trả về danh sách rỗng.
  12. Hiển thị Error Banner màu đỏ thân thiện khi API trả về lỗi.
  13. Điều khiển phân trang gọi đúng trang tiếp theo.
  14. Lọc kết quả danh sách theo độ khó Easy.
  15. Sắp xếp các công thức theo thời gian nấu tăng dần.
- **Kết quả kiểm thử thực tế mới nhất:**
  - `Test Suites:` **2 passed, 2 total**
  - `Tests:` **27 passed, 27 total** (15/15 test cases của `SearchUI.test.tsx` + 12 test cases của `recipe-utils.test.ts` PASS 100%)
  - `Time:` 4.703 s

---

## 12. Build
- **Lệnh đóng gói build:**
  ```powershell
  cd frontend/culinary-blog-web
  $env:NODE_OPTIONS="--max-old-space-size=4096"; npm run build
  ```
- **Kết quả build thực tế:**
  - Next.js 15.5.25 (Turbopack)
  - `Compiled successfully in 2.1s`
  - Đã xuất bản thành công trang tĩnh `/search` (`○ /search 6.22 kB`)
  - Zero TypeScript error, exit code 0.

---

## 13. Cách chạy branch
Khởi chạy branch từ đầu trên môi trường phát triển:

```bash
git switch feat/vohungmanh-search-ui
cd frontend/culinary-blog-web
npm install
npm run dev
```

- **URL truy cập local:** `http://localhost:3000/search` hoặc `http://localhost:3000/search?q=pho+bo`

---

## 14. Cách demo với giảng viên
Trình tự demo 8 bước chi tiết:

1. **Khởi động trang Tìm kiếm:** Truy cập `http://localhost:3000/search` -> Trình diễn giao diện khởi tạo với SearchBox nổi bật và các thẻ gợi ý từ khóa.
2. **Kiểm tra Validation < 2 ký tự:** Nhập chữ "a" và nhấn Enter -> Hệ thống lập tức báo lỗi đỏ: "Từ khóa tìm kiếm phải có ít nhất 2 ký tự."
3. **Tìm kiếm tiếng Việt không dấu ("pho bo"):** Nhập "pho bo" và bấm nút "Tìm kiếm" -> Hệ thống hiển thị 3 skeleton loading nhấp nháy, sau đó render kết quả bài viết "Phở bò Hà Nội truyền thống".
4. **Trình bày Badge Khớp toàn văn (FTS):** Chỉ cho giảng viên thấy badge màu xanh lá "Khớp toàn văn (FTS)" trên thẻ bài viết phở bò.
5. **Thử nghiệm Gõ sai chính tả (Fuzzy Fallback):** Nhập từ khóa "phoo bo" -> Hệ thống vẫn thông minh tìm thấy món "Phở bò" và gắn badge màu vàng "Gợi ý tương đồng (Fuzzy)".
6. **Lọc theo Độ khó (Difficulty Filter):** Tại thanh công cụ lọc, chọn độ khó "Dễ" (Easy) -> Danh sách tự động lọc chỉ hiển thị các món ăn dễ làm.
7. **Sắp xếp theo Thời gian nấu (Sort by Cook Time):** Chọn sắp xếp theo thời gian nấu nhanh nhất -> Các món ăn có thời gian ngắn hơn tự động đảo lên đầu.
8. **Tìm kiếm không có kết quả:** Nhập từ khóa "xyzabc123" -> Giao diện hiển thị trạng thái "Không tìm thấy công thức phù hợp" cùng các nút bấm từ khóa gợi ý.

---

## 15. Ranh giới ownership
- **Mã nguồn do Võ Hùng Mạnh sở hữu:** `SearchBox.tsx`, `SearchResultCard.tsx`, `SearchContainer.tsx`, `app/search/page.tsx`, `lib/api/search.ts`, `types/search.ts`, `SearchUI.test.tsx`.
- **Điểm tích hợp với thành viên khác:** `SearchBox` có thể được tái sử dụng để nhúng trực tiếp vào Header / Navbar của trang web (do TV2 hoặc TV1 phụ trách) mà không cần viết lại logic tìm kiếm.
- **Cam kết:** Branch này **tuyệt đối không chỉnh sửa `RecipeForm.tsx`**, không sửa các module Step, Image, Category hay Auth của các thành viên khác.

---

## 16. Hạn chế hiện tại
- `SearchBox` hiện tại được gắn mặc định tại trang `/search`, chưa được mount trực tiếp vào Navbar chung của toàn bộ website (để bảo vệ mã nguồn layout chung của nhóm).
- Cần có backend (.NET Web API) và PostgreSQL đang chạy để thực hiện tìm kiếm trực tiếp trên dữ liệu thật.

---

## 17. Tài liệu COMPLAN
Báo cáo giải trình chuyên sâu về kỹ thuật PostgreSQL Full-Text Search, GIN Index, hàm `unaccent`, giải thuật Trigram Similarity và 10 câu hỏi bảo vệ đồ án được lưu trữ tại:
- [VO_HUNG_MANH_SEARCH_UI_COMPLAN.md](file:///b:/PTUDWNC-2026-Nhom12-MANH/docs/VO_HUNG_MANH_SEARCH_UI_COMPLAN.md)
