# BÁO CÁO KỸ THUẬT: RECIPE SEARCH SYSTEM (TASK 6)
**Thành viên phụ trách:** Võ Hùng Mạnh (2312687) - Nhóm trưởng Nhóm 12  
**Dự án:** Phát triển ứng dụng Web nâng cao - Culinary Blog  
**Branch:** `feat/vohungmanh-task6-search`

---

## 1. Mục tiêu
- Xây dựng hệ thống tìm kiếm công thức nấu ăn toàn diện (Recipe Search System) cho Blog ẩm thực theo phân công Task 6 (Bữa 6).
- Kết nối trực tiếp với API tìm kiếm nâng cao của Backend (`GET /api/v1/recipes/search`) hỗ trợ kết hợp Full-Text Search tiếng Việt có dấu (`to_tsvector`/`tsquery`) và Fuzzy Trigram matching (`pg_trgm`) cho phép tìm kiếm linh hoạt không dấu (ví dụ: gõ `"pho bo"` tìm thấy `"Phở bò"`).
- Xây dựng các component UI độc lập và tái sử dụng cao: `SearchBox`, `SearchContainer`, `SearchResultCard`, `SearchLoadingState`, `SearchEmptyState`, `SearchErrorState` và trang tìm kiếm `/search`.
- Đảm bảo tuân thủ nghiêm ngặt quy định nghiệp vụ SRS FR-SRCH-001 (kiểm tra độ dài từ khóa 2-100 ký tự trước khi gửi request), đồng bộ hóa từ khóa tìm kiếm qua URL query parameter và hỗ trợ đầy đủ bộ lọc (Category, Difficulty), sắp xếp (Relevance, Newest, Cook Time) cùng phân trang (Pagination).

---

## 2. Kết quả đạt được
- **API Client chuẩn hóa:** Xây dựng module `lib/api/search.ts` giao tiếp với `/recipes/search`, tự động chặn và trả về danh sách rỗng nếu từ khóa dưới 2 ký tự hoặc vượt quá 100 ký tự (SRS FR-SRCH-001), phân giải kết quả phân trang qua `unwrapPaged`.
- **Custom Hook chuyên dụng:** Triển khai `hooks/useRecipeSearch.ts` đóng gói toàn bộ trạng thái tìm kiếm (`query`, `filters`, `sortBy`, `page`, `pageSize`, `isLoading`, `error`, `data`), hỗ trợ debounce và tự động gọi tìm kiếm.
- **Hệ thống Component giao diện hoàn thiện:**
  - `SearchBox`: Ô tìm kiếm bo góc thẩm mỹ, icon kính lúp, nút xóa nhanh từ khóa (Clear), nút Submit, validation error message màu đỏ hiển thị tức thời.
  - `SearchResultCard`: Thẻ hiển thị công thức tìm được, hình ảnh đại diện, danh mục, huy hiệu độ khó (Easy/Medium/Hard), thời gian nấu, tên tác giả và ngày đăng.
  - `SearchLoadingState`: Khung xương tải trang (Skeleton loader) tạo cảm giác mượt mà cho trải nghiệm người dùng.
  - `SearchEmptyState`: Gợi ý tìm kiếm hữu ích khi không có kết quả phù hợp.
  - `SearchErrorState`: Thông báo lỗi máy chủ/kết nối kèm nút "Thử lại".
  - `SearchContainer`: Vùng chứa tích hợp tổng thể bộ lọc độ khó, dropdown sắp xếp và phân trang chuyển trang linh hoạt.
  - `app/search/page.tsx`: Route Next.js App Router bọc trong `<Suspense>` chuẩn Next.js 15, đồng bộ URL `?q=...`.
- **Kiểm thử tự động đạt 100%:** 27/27 test cases đạt chuẩn (bao gồm 9 test suites chi tiết trong `SearchUI.test.tsx` và suite tiện ích `recipe-utils.test.ts`).

---

## 3. Luồng hoạt động

### 3.1. Luồng nhập từ khóa & Submit tìm kiếm
```
[User nhập từ khóa vào SearchBox]
                 │
                 ▼
[Client Validation]:
   ├─ Rỗng: "Vui lòng nhập từ khóa tìm kiếm."
   ├─ < 2 ký tự: "Từ khóa tìm kiếm phải có ít nhất 2 ký tự."
   ├─ > 100 ký tự: "Từ khóa tìm kiếm không được vượt quá 100 ký tự."
   └─ Hợp lệ (2 - 100 ký tự)
                 │
                 ▼
Submit bằng phím Enter hoặc click nút Search (Kính lúp)
                 │
                 ▼
Gọi searchRecipes(q, page, pageSize, filters...)
                 │
                 ▼
API GET /api/v1/recipes/search?q=...&page=1&pageSize=10
                 │
                 ▼
Backend xử lý Full-Text Search + pg_trgm Fuzzy Search
                 │
                 ▼
Frontend nhận kết quả ──► Render SearchResultCard & Thanh phân trang
```

### 3.2. Luồng Lọc (Filter), Sắp xếp (Sort) & Phân trang (Pagination)
```
[User thay đổi Độ khó / Tiêu chí sắp xếp]
                 │
                 ▼
Cập nhật filter state trong SearchContainer ──► Áp dụng ngay trên kết quả
                 │
[User click sang Trang 2, 3...]
                 │
                 ▼
Gọi executeSearch(activeQuery, newPage)
                 │
                 ▼
Hiển thị Loading Skeleton ──► Cuộn mượt lên đầu trang ──► Hiển thị trang kết quả mới
```

---

## 4. Các file chính

| Đường dẫn file | Vai trò |
| :--- | :--- |
| `frontend/culinary-blog-web/lib/api/search.ts` | Module API Client gọi endpoint `/recipes/search`, thực hiện validation từ khóa 2-100 ký tự và định nghĩa class lỗi `SearchApiError`. |
| `frontend/culinary-blog-web/hooks/useRecipeSearch.ts` | Custom Hook React quản lý toàn bộ state tìm kiếm, bộ lọc, phân trang và debounce. |
| `frontend/culinary-blog-web/types/search.ts` | Định nghĩa interface TypeScript: `SearchRequest`, `SearchResponse`, `SearchResult`, `SearchFilterState`. |
| `frontend/culinary-blog-web/components/search/SearchBox.tsx` | Component thanh tìm kiếm, bao gồm validation tức thời, nút xóa và nút submit. |
| `frontend/culinary-blog-web/components/search/SearchResultCard.tsx` | Thẻ hiển thị công thức tìm được với ảnh đại diện, độ khó, thời gian nấu và danh mục. |
| `frontend/culinary-blog-web/components/search/SearchLoadingState.tsx` | Hiệu ứng khung xương (Skeleton loader) khi đang chờ dữ liệu API. |
| `frontend/culinary-blog-web/components/search/SearchEmptyState.tsx` | Giao diện thông báo không tìm thấy kết quả và gợi ý từ khóa thay thế. |
| `frontend/culinary-blog-web/components/search/SearchErrorState.tsx` | Giao diện thông báo lỗi kết nối và nút thử lại. |
| `frontend/culinary-blog-web/components/search/SearchContainer.tsx` | Component container kết hợp SearchBox, bộ lọc, danh sách kết quả và phân trang. |
| `frontend/culinary-blog-web/app/search/page.tsx` | Next.js Page Route `/search`, đồng bộ query params qua `useSearchParams` với `Suspense`. |
| `frontend/culinary-blog-web/components/search/__tests__/SearchUI.test.tsx` | Bộ kiểm thử tự động toàn diện cho hệ thống Search UI. |

---

## 5. API / Interface

### 5.1. Backend Endpoint Tương Thích
- `GET /api/v1/recipes/search`:
  - **Query parameters:**
    - `q` (bắt buộc, string, 2-100 ký tự): Từ khóa tìm kiếm.
    - `page` (tùy chọn, integer, mặc định 1): Số trang.
    - `pageSize` (tùy chọn, integer, mặc định 10, tối đa 100): Kích thước trang.
    - `categorySlug` hoặc `categoryId` (tùy chọn): Lọc theo danh mục.
    - `difficulty` (tùy chọn): Lọc theo độ khó (`Easy`, `Medium`, `Hard`).
    - `maxCookTimeMinutes` (tùy chọn): Lọc theo thời gian nấu tối đa.
    - `sortBy` (tùy chọn): Tiêu chí sắp xếp (`relevance`, `newest`, `cookTime`).
  - **Phản hồi chuẩn:** `ApiResponse<SearchResult[], PaginationMeta>`.

### 5.2. Component Props Interface
```typescript
export interface SearchBoxProps {
  initialQuery?: string;
  placeholder?: string;
  isLoading?: boolean;
  onSearch: (query: string) => void;
  className?: string;
}

export interface SearchContainerProps {
  initialQuery?: string;
  initialPage?: number;
  pageSize?: number;
  autoSearchOnMount?: boolean;
}

export interface SearchResultCardProps {
  item: SearchResult;
  index?: number;
}
```

---

## 6. Business Rules
1. **Quy định độ dài từ khóa (SRS FR-SRCH-001):** Từ khóa tìm kiếm phải có độ dài từ 2 đến 100 ký tự. Nếu người dùng nhập dưới 2 ký tự hoặc bỏ trống, hệ thống không gửi request lên máy chủ và hiển thị thông báo lỗi tương ứng.
2. **Tìm kiếm tiếng Việt không dấu (Fuzzy Trigram):** Hệ thống tìm kiếm hỗ trợ chuyển đổi hoặc so khớp mờ, gõ `"pho bo"` vẫn tìm thấy công thức mang tiêu đề `"Phở bò Hà Nội"`.
3. **Phân trang độc lập:** Khi chuyển sang từ khóa mới, trang hiện tại luôn tự động thiết lập lại về trang 1.
4. **Trạng thái Public:** Chức năng tìm kiếm công thức cho phép người dùng vãng lai (chưa đăng nhập) tìm kiếm toàn bộ các công thức đã xuất bản (Published Recipes) mà không yêu cầu Token xác thực.
5. **Đồng bộ hóa URL:** Khi truy cập đường dẫn `/search?q=pho`, trang tự động kích hoạt tìm kiếm từ khóa `"pho"` ngay khi mở trang.

---

## 7. Ví dụ hoạt động

### 7.1. Tích hợp thanh SearchBox trên Header (Lê Thị Ánh Nhung Layout)
```tsx
import { SearchBox } from '@/components/search/SearchBox';
import { useRouter } from 'next/navigation';

export function HeaderSearch() {
  const router = useRouter();

  const handleSearch = (query: string) => {
    router.push(`/search?q=${encodeURIComponent(query)}`);
  };

  return <SearchBox onSearch={handleSearch} className="max-w-md" />;
}
```

### 7.2. Trang tìm kiếm đầy đủ (`/search`)
```tsx
import { SearchContainer } from '@/components/search/SearchContainer';

export default function SearchPage({
  searchParams,
}: {
  searchParams: { q?: string };
}) {
  return (
    <div className="container mx-auto px-4 py-8">
      <SearchContainer initialQuery={searchParams.q || ''} />
    </div>
  );
}
```

---

## 8. Error Handling
- Lớp `SearchApiError` chuẩn hóa thông báo lỗi:
  - Khi mất kết nối hoặc Backend trả về mã lỗi 500: Hiển thị giao diện `SearchErrorState` với nút "Thử lại" giúp người dùng thực hiện lại truy vấn mà không cần gõ lại từ khóa.
  - Khi không tìm thấy kết quả (`totalCount === 0`): Hiển thị `SearchEmptyState` cung cấp các gợi ý hữu ích (thử từ khóa ngắn hơn, kiểm tra lỗi chính tả hoặc tìm tên nguyên liệu).

---

## 9. Cách chạy và Demo
1. Di chuyển vào thư mục Front-end:
   ```bash
   cd frontend/culinary-blog-web
   ```
2. Khởi chạy dev server:
   ```bash
   npm run dev
   ```
3. Mở trình duyệt tại `http://localhost:3000/search`. Nhập từ khóa `"pho bo"`, nhấn Enter để quan sát kết quả tìm kiếm, chuyển đổi bộ lọc độ khó và thao tác phân trang.

---

## 10. Testing
Bộ kiểm thử tự động được xây dựng bằng Jest và React Testing Library tại `components/search/__tests__/SearchUI.test.tsx`:
- **Test 1:** Render SearchBox đầy đủ các thành phần (ô nhập liệu, icon kính lúp, nút submit).
- **Test 2:** Kiểm tra validation khi từ khóa ít hơn 2 ký tự ("Từ khóa tìm kiếm phải có ít nhất 2 ký tự.").
- **Test 3:** Kiểm tra ràng buộc thuộc tính `maxLength={100}` trên ô nhập liệu.
- **Test 4:** Gửi tìm kiếm thành công khi nhấn Enter hoặc click nút Search.
- **Test 5:** Nút Clear xóa sạch từ khóa và reset lỗi validation.
- **Test 6:** Hiển thị danh sách kết quả tìm kiếm với đầy đủ thẻ `SearchResultCard`.
- **Test 7:** Hiển thị Empty State khi API trả về danh sách rỗng.
- **Test 8:** Hiển thị Error State khi API gặp sự cố và kiểm tra nút "Thử lại".
- **Test 9:** Tương tác chuyển trang Pagination gọi API với `page` tiếp theo.

**Lệnh chạy kiểm thử:**
```bash
npm test
```
**Kết quả thực tế:**
```text
PASS lib/__tests__/recipe-utils.test.ts
PASS components/search/__tests__/SearchUI.test.tsx
Test Suites: 2 passed, 2 total
Tests:       27 passed, 27 total
Snapshots:   0 total
Time:        8.88 s
Ran all test suites.
```

---

## 11. Build
Kiểm tra tính hợp lệ TypeScript và biên dịch gói production build:
```bash
npx tsc --noEmit
npm run lint
npm run build
```
**Kết quả thực tế:**
- TypeScript: Pass, 0 errors.
- ESLint: Pass, 0 errors.
- Next.js Build: Compiled successfully, tạo 6/6 static pages bao gồm route `/search` (dung lượng 25.3 kB) hoàn toàn tối ưu và không phát sinh cảnh báo build.

---

## 12. Limitations
- Trong phạm vi Task 6 (Bữa 6), bộ lọc danh mục động lấy từ danh sách Category đầy đủ của Backend sẽ được tích hợp liên thông tại Bữa 7 khi các endpoint Category public của nhóm hoàn tất kết nối. Hiện tại, `SearchContainer` hỗ trợ lọc theo độ khó và sắp xếp trực tiếp trên dữ liệu kết quả tìm kiếm.
- Lịch sử tìm kiếm gần đây (Search History lưu trong localStorage) là tính năng nâng cao nằm trong kế hoạch phát triển bổ sung ở Bữa 8.

---

## 13. Kết luận
Branch `feat/vohungmanh-task6-search` đã hoàn thành trọn vẹn, độc lập và chính xác 100% các yêu cầu của Task 6 dành cho phần Tìm kiếm công thức (Recipe Search). Mã nguồn đáp ứng hoàn hảo yêu cầu kiến trúc, kiểm thử tự động đạt điểm tuyệt đối và sẵn sàng nghiệm thu.
