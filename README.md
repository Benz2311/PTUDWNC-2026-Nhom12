# TASK 4 (BỮA 4) – GIAO DIỆN TÌM KIẾM CÔNG THỨC (RECIPE SEARCH UI PROTOTYPE)

**Sinh viên:** Võ Hùng Mạnh
**MSSV:** 2312687
**Nhóm:** 12
**Nhánh:** `feat/vohungmanh-search-ui`

---

### 1. Mục tiêu Task 4 liên quan đến nhánh
Trong khuôn khổ Bữa 4 (Task 4), song song với việc xây dựng công cụ tìm kiếm toàn văn Backend PostgreSQL, nhánh này chịu trách nhiệm nghiên cứu và phát triển nguyên mẫu giao diện người dùng (UI Prototype) cho trang Tìm kiếm công thức (`/search`). Giao diện phải cung cấp ô nhập từ khóa tiện lợi, cơ chế xử lý trạng thái tìm kiếm (Loading, Empty, Error), hiển thị danh sách thẻ kết quả trực quan và các bộ lọc sơ bộ theo độ khó và sắp xếp.

### 2. Công việc cá nhân thực tế
- Xây dựng hệ thống kiểu dữ liệu TypeScript cho module tìm kiếm tại `frontend/.../types/search.ts` (`RecipeSearchResult`, `SearchResponse`, `SearchFilterState`).
- Hiện thực API client helper trong `frontend/.../lib/api/search.ts` kết nối tới endpoint `GET /api/v1/recipes/search`.
- Xây dựng component `SearchBox.tsx` (129 dòng mã) hỗ trợ nhập liệu, nút xóa nhanh từ khóa (Clear), và bắt sự kiện gõ phím `Enter`.
- Phát triển component chính `SearchContainer.tsx` (324 dòng mã) quản lý toàn diện vòng đời tìm kiếm:
  - Kiểm tra độ dài từ khóa (yêu cầu tối thiểu 2 ký tự theo chuẩn SRS).
  - Quản lý trạng thái đang tải (Loading Skeleton / Spinner).
  - Quản lý trạng thái không có kết quả (Empty State) và trạng thái lỗi máy chủ (Error Alert).
  - Bộ lọc sơ bộ theo độ khó (`Easy`, `Medium`, `Hard`) và các tùy chọn sắp xếp (`relevance`, `newest`, `cookTime`).
- Xây dựng thẻ hiển thị kết quả `SearchResultCard.tsx` (144 dòng mã) trình bày hình ảnh món ăn, tiêu đề, mô tả tóm tắt, thời gian nấu, tác giả, danh mục và nhãn `MatchType` (FTS / Fuzzy).
- Khởi tạo trang `/search` trong Next.js App Router (`frontend/.../app/search/page.tsx`).
- Viết 278 dòng mã kiểm thử giao diện tự động với Jest trong `SearchUI.test.tsx`.

### 3. Chức năng đã hoàn thành (UI Prototype)
- **Thanh tìm kiếm phản hồi nhanh:** Ô nhập từ khóa có placeholder gợi ý, nút kính lúp tìm kiếm và nút "X" để xóa nhanh từ khóa.
- **Hiển thị thẻ kết quả phong phú:** Thẻ món ăn hiển thị ảnh đại diện, danh mục, tác giả, thời gian chuẩn bị & nấu, số khẩu phần, độ khó và nhãn nhận diện thuật toán tìm kiếm (`FullTextSearch` hoặc `FuzzyTrigram`).
- **Xử lý toàn diện các trạng thái giao diện:**
  - *Initial State:* Lời nhắc hướng dẫn người dùng nhập từ khóa tìm kiếm.
  - *Loading State:* Hiệu ứng quay spinner khi đang chờ phản hồi từ server.
  - *Empty State:* Thông báo thân thiện khi không tìm thấy công thức phù hợp, gợi ý kiểm tra lại chính tả hoặc giảm bớt bộ lọc.
  - *Error State:* Hiển thị thông báo lỗi rõ ràng khi mất kết nối mạng hoặc server gặp sự cố.
- **Lọc và sắp xếp sơ bộ:** Lọc nhanh kết quả theo độ khó và sắp xếp theo độ phù hợp hoặc ngày mới nhất.

### 4. Cách xử lý và luồng hoạt động
```text
Người dùng nhập từ khóa (ví dụ "phở bò") vào SearchBox và nhấn Enter / Click Search
        │
        ├── Kiểm tra từ khóa:
        │     ├── Nếu length < 2 ký tự --> Thông báo yêu cầu nhập tối thiểu 2 ký tự
        │     └── Nếu length >= 2 ký tự --> Bật trạng thái isLoading = true
        │
        ▼
SearchContainer gọi searchRecipes() trong lib/api/search.ts
        │
        ▼
Gửi HTTP GET tới /api/v1/recipes/search?q=phở+bò&difficulty=all&sortBy=relevance
        │
        ├── Thành công (200 OK):
        │     ├── Nếu items.length > 0 --> Render danh sách SearchResultCard
        │     └── Nếu items.length == 0 --> Render Empty State
        │
        └── Thất bại (500 / Network Error):
              └── Render Error Alert kèm nút "Thử lại"
```

### 5. Danh sách file mã nguồn quan trọng

| Đường dẫn file | Vai trò và chức năng chính |
| :--- | :--- |
| `frontend/culinary-blog-web/app/search/page.tsx` | Route page chính của trang tìm kiếm `/search` trong Next.js App Router. |
| `frontend/culinary-blog-web/components/search/SearchContainer.tsx` | 324 dòng mã component điều khiển luồng tìm kiếm, bộ lọc độ khó, sắp xếp và các trạng thái. |
| `frontend/culinary-blog-web/components/search/SearchBox.tsx` | 129 dòng mã thanh tìm kiếm, bắt sự kiện phím Enter, nút Clear và validation độ dài. |
| `frontend/culinary-blog-web/components/search/SearchResultCard.tsx` | 144 dòng mã hiển thị thẻ kết quả tìm kiếm với đầy đủ metadata và badge MatchType. |
| `frontend/culinary-blog-web/types/search.ts` | Định nghĩa interface TypeScript: `RecipeSearchResult`, `SearchResponse`, `SearchFilterState`. |
| `frontend/culinary-blog-web/lib/api/search.ts` | Thư viện gọi API tìm kiếm với Axios. |
| `frontend/culinary-blog-web/components/search/__tests__/SearchUI.test.tsx` | 278 dòng kiểm thử giao diện tự động với Jest và React Testing Library. |
| `docs/README_SEARCH_UI.md` | Tài liệu kỹ thuật chi tiết về thiết kế giao diện tìm kiếm. |
| `docs/VO_HUNG_MANH_SEARCH_UI_COMPLAN.md` | Kế hoạch triển khai và ma trận kiểm thử giao diện tìm kiếm Bữa 4. |

### 6. API, Database và Infrastructure liên quan
- **Framework Frontend:** Next.js 14+ (App Router), React 18, TypeScript, Tailwind CSS.
- **Thư viện kiểm thử:** Jest, `@testing-library/react`.
- **API Utilities:** Axios Client (`lib/api.ts`).
- **Endpoint kết nối:** `GET /api/v1/recipes/search`.

### 7. Hướng dẫn chạy

#### Bước 1: Cài đặt dependencies (nếu chưa có)
```bash
cd frontend/culinary-blog-web
npm install
```

#### Bước 2: Chạy ứng dụng Next.js trên môi trường dev
```bash
npm run dev
```
Mở trang tìm kiếm tại: `http://localhost:3000/search`.

### 8. Hướng dẫn kiểm thử

#### Kiểm thử giao diện tự động với Jest
```bash
cd frontend/culinary-blog-web
npm test -- SearchUI.test.tsx
```

#### Kiểm thử tương tác thủ công
1. Mở trang `/search`, thử nhấn nút Tìm kiếm khi chưa nhập chữ -> hệ thống báo lỗi yêu cầu nhập tối thiểu 2 ký tự.
2. Nhập từ khóa `"phở bò"` và nhấn Enter -> quan sát trạng thái Loading và danh sách thẻ kết quả hiển thị.
3. Thử chuyển đổi bộ lọc độ khó sang `"Easy"` -> quan sát danh sách kết quả được lọc tương ứng.
4. Nhập từ khóa không có thật (ví dụ `"xyz123"`) -> quan sát giao diện Empty State.

### 9. Kết quả kiểm thử có bằng chứng
- **Kết quả Unit Test (Jest):** Toàn bộ test suite trong `SearchUI.test.tsx` đạt kết quả `PASS` (bao phủ: kiểm tra render thanh tìm kiếm, chặn từ khóa < 2 ký tự, hiển thị thẻ kết quả, hiển thị empty state khi không có dữ liệu, và hiển thị error state khi API lỗi).
- **Trải nghiệm tìm kiếm:** Thẻ kết quả hiển thị hình ảnh món ăn sắc nét, thông tin tác giả và danh mục bố cục rõ ràng.

### 10. Hạn chế và tồn đọng
- **Trạng thái thực tế:** Đây là **Nguyên mẫu giao diện (UI Prototype)** hoàn chỉnh về mặt component và luồng tìm kiếm cơ bản.
- **Tồn đọng:** Ở mốc Task 4, giao diện chưa đồng bộ query parameters lên thanh địa chỉ trình duyệt (URL state persistence khi reload hoặc bấm nút Back/Forward) và chưa có bộ lọc sidebar đa tiêu chí nâng cao; các tính năng này được phân bổ phát triển ở Task 6 và hoàn thiện nghiệm thu toàn diện với 16 ảnh chụp tại Task 7 (`feat/vohungmanh-task7-search`).

### 11. Liên kết tài liệu kỹ thuật chi tiết
- Chi tiết thiết kế giao diện tìm kiếm: [docs/README_SEARCH_UI.md](docs/README_SEARCH_UI.md)
- Kế hoạch triển khai kỹ thuật Bữa 4: [docs/VO_HUNG_MANH_SEARCH_UI_COMPLAN.md](docs/VO_HUNG_MANH_SEARCH_UI_COMPLAN.md)

---

# Phát triển ứng dụng Web nâng cao

## Nhóm 12

### Thành viên
1. Võ Hùng Mạnh - 2312687 - 2312687@dlu.edu.vn - Nhóm trưởng
2. Lê Thị Ánh Nhung - 2312709 - 2312709@dlu.edu.vn
3. Phạm Nguyễn Ngọc Phước - 2312718 - 2312718@dlu.edu.vn
4. Nguyễn Văn Quốc - 2312729 - 2312729@dlu.edu.vn
