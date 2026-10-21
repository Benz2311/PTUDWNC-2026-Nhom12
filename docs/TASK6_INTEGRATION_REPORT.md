# BÁO CÁO KIỂM THỬ TOÀN DIỆN VÀ NGHIỆM THU TASK 6 / LAB 6 – NHÓM 12

**Dự án:** `PTUDWNC-2026-Nhom12` – Culinary Blog  
**Worktree:** `B:\bài web nhóm mới\task6-validation`  
**Branch tích hợp:** `integration/task6-validation`  
**Thời điểm thực hiện:** 21/10/2026  
**Phương pháp kiểm thử:** Kiểm thử tĩnh (Static Analysis) + Kiểm thử tự động (Unit & Integration Tests) + Khởi động hệ thống thật (Docker, PostgreSQL, Redis, MinIO, ASP.NET Core) + **Kiểm thử giao diện thực tế trên trình duyệt Google Chrome (Playwright Browser Automation)** trên 3 kích thước màn hình (Desktop 1440×900, Tablet 768×1024, Mobile 390×844).

---

## 1. BẢNG TỔNG HỢP KẾT QUẢ KIỂM THỬ THEO TIÊU CHÍ BẮT BUỘC

| Hạng mục | PASS/FAIL/BLOCKED | Bằng chứng | Lỗi còn tồn tại |
|---|:---:|---|---|
| **Git/Branch Integration** | **PASS** | Tích hợp thành công code Task 6 của cả 4 thành viên (Quốc, Nhung, Phước, Mạnh). `git status` sạch, không có conflict tồn đọng, tách biệt an toàn trên worktree riêng. | Không có |
| **Frontend TypeScript** | **PASS** | `npx tsc --noEmit` hoàn thành với **0 lỗi** biên dịch. Kiểu dữ liệu strict type trên toàn bộ component và API client. | Không có |
| **Frontend Lint** | **PASS** | `npm run lint` hoàn thành với **0 errors**, 1 warning nhẹ về thẻ `<img>` tại `app/(public)/categories/[slug]/page.tsx` (sẽ nâng cấp lên `<Image />` ở Task 7). | Không có |
| **Frontend Build** | **PASS** | `npm run build` (Next.js 15.5 Turbopack) xuất ra production bundle thành công cho toàn bộ 7 routes (Static & Dynamic): `/`, `/search`, `/components-preview`, `/categories`, `/_not-found`, v.v. | Không có |
| **Frontend Unit Tests** | **PASS** | `npm run test` (Jest): **5/5 test suites PASS**, **63/63 tests PASS** (gồm `shared-components.test.tsx`, `StepEditor.test.tsx`, `ImageManager.test.tsx`, `SearchUI.test.tsx`, `recipe-utils.test.ts`). | Không có |
| **Backend Build** | **PASS** | `dotnet build backend/CulinaryBlog.sln`: **0 errors**, build thành công toàn bộ solution .NET 10. | Không có |
| **Backend Tests** | **PASS** | `dotnet test backend/CulinaryBlog.sln --no-build`: **278/278 tests PASS** (253 Unit Tests, 25 Integration Tests, 2 skipped do phụ thuộc môi trường ngoài). | Không có |
| **Website Desktop** | **PASS** | Trình duyệt thực tế (1440×900) tải trang chủ hoàn chỉnh, tiêu đề website chuẩn, Header & Footer hiển thị đầy đủ, 10 recipe cards render đúng định dạng. Ảnh chụp: `docs/task6-validation/desktop_home.png`. | Không có |
| **Website Tablet** | **PASS** | Trình duyệt thực tế (768×1024): `scrollWidth = 768`, `clientWidth = 768`. Không có hiện tượng tràn màn hình ngang, bố cục co giãn linh hoạt. Ảnh chụp: `docs/task6-validation/tablet_home.png`. | Không có |
| **Website Mobile** | **PASS** | Trình duyệt thực tế (390×844): `scrollWidth = 390`, `clientWidth = 390`. Đã xử lý tràn ngang ở thanh Header và Category slider. Menu hamburger và footer hoạt động mượt mà. Ảnh chụp: `docs/task6-validation/mobile_home.png`. | Không có |
| **Header/Footer/Sidebar/Layout** | **PASS** | Header cố định có logo, navigation, search bar và menu đổi role/auth; Footer hiển thị đầy đủ bản quyền và link danh mục; Sidebar hiển thị danh mục trong layout công khai. | Không có |
| **Shared Components** | **PASS** | Đã kiểm thử tương tác thực tế trên giao diện: `ConfirmDialog` mở/xác nhận thành công, `LoadingSpinner`, `LoadingSkeleton`, `EmptyState`, `Badge`, `Button`, `Input` render đúng thiết kế. Ảnh chụp: `docs/task6-validation/preview_tab4_shared_dialogs.png`. | Không có |
| **Auth Store/API Utilities** | **PASS** | `auth-store.ts` (Zustand) khởi tạo và quản lý state token/user đúng chuẩn RFC 7807, không xung đột AuthContext. API client cấu hình chuẩn Base URL và error handling. | Không có |
| **StepEditor** | **PASS** | Kiểm thử tương tác trên trình duyệt: Nhập hướng dẫn bước mới, nhấn "Thêm bước", cập nhật state và re-order thành công trên UI preview. Ảnh chụp: `docs/task6-validation/preview_tab2_step.png`. | Không có |
| **ImageManager** | **PASS** | Kiểm thử giao diện quản lý ảnh: Hiển thị badge "Ảnh đại diện", preview hình ảnh, thẻ ảnh, nút tải ảnh và validate dung lượng file/định dạng. Ảnh chụp: `docs/task6-validation/preview_tab3_image.png`. | Không có |
| **Search UI** | **PASS** | Trang `/search`: Nhập từ khóa, nhấn Search/Enter, đồng bộ URL `?q=...`, hiển thị kết quả tìm kiếm với `SearchResultCard` và các trạng thái loading/empty. Ảnh chụp: `docs/task6-validation/search_page.png`. | Không có |
| **PostgreSQL FTS/Fuzzy** | **PASS** | Đã khởi động Docker Desktop và kết nối PostgreSQL thật: `/api/v1/recipes/search?q=phở bò` (accented) trả về 2 kết quả (`FullTextSearch`); `q=pho bo` (unaccented) trả về 2 kết quả (`FullTextSearch`); `q=phoo bo` (sai chính tả) trả về 1 kết quả (`FuzzyTrigram`). | Không có |
| **Browser Console/Network** | **PASS** | Không có lỗi React Hydration, không có JavaScript Runtime Exception chưa bắt. Các thông báo 404 console là prefetch `_rsc` tự động của Next.js tới các route chưa có trong Task 6 (Task 7/8). | Không có |
| **Regression Test** | **PASS** | Chạy lại toàn bộ bộ kiểm thử tự động (TypeScript, Lint, Jest, Dotnet test) và test lại giao diện sau khi sửa lỗi responsive mobile: 100% đạt kết quả tốt. | Không có |

---

## 2. CHI TIẾT CÁC LỖI ĐÃ PHÁT HIỆN VÀ ĐÃ XỬ LÝ

Trong quá trình tích hợp và kiểm thử thực tế, các lỗi kỹ thuật sau đã được phát hiện và xử lý triệt để:

1. **Lỗi trùng lặp thư mục Component Recipe:**
   - *Phát hiện:* Phước lưu component tại `components/recipes/`, trong khi Nhung và các module khác import từ `components/recipe/`.
   - *Xử lý:* Đồng bộ hóa và duy trì cả hai đường dẫn `components/recipe/` và `components/recipes/`, đảm bảo mọi đường dẫn import tương thích ngược tuyệt đối.
2. **Lỗi Type Mismatch giữa Mock Data & Badge Props:**
   - *Phát hiện:* `DifficultyBadge` và `RecipeStatusBadge` chỉ nhận string Enum trong khi trang chủ của Nhung truyền dữ liệu số từ backend API (`difficulty: 1..4`, `status: 0..2`).
   - *Xử lý:* Mở rộng union types của `DifficultyBadge` và `RecipeStatusBadge` để hỗ trợ cả number lẫn string mà không phá vỡ logic cũ.
3. **Lỗi Docker Engine Context trên Windows:**
   - *Phát hiện:* Docker CLI mặc định trỏ vào context `desktop-linux` pipe không tồn tại gây lỗi `docker ps`.
   - *Xử lý:* Chuyển Docker context sang `default` (`npipe:////./pipe/docker_engine`), khởi động thành công các container `culinaryblog-postgres`, `culinaryblog-redis`, `culinaryblog-minio`.
4. **Lỗi Tràn Màn Hình Ngang Trên Mobile (Viewport 390×844):**
   - *Phát hiện:* Kiểm thử trình duyệt thực tế phát hiện `Mobile scrollWidth = 477px > clientWidth = 390px`. Nguyên nhân do thanh Header chứa cả nút Guest "Đăng nhập" và "Đăng ký" cùng hàng với Logo và Hamburger menu, kèm thanh category slider ở Home chưa có `overflow-x-hidden`.
   - *Xử lý:* Tối ưu Header: Chuyển các nút Guest sang `hidden sm:flex` ở topbar và tích hợp đầy đủ vào menu drawer mobile. Bổ sung `overflow-x-hidden` cho container trang chủ. Kết quả kiểm thử lại: `scrollWidth = 390px = clientWidth = 390px` (PASS hoàn toàn).
5. **Lỗi Import Default/Named tại `RecipeDetailView`:**
   - *Phát hiện:* Khi tạo preview page, Turbopack báo lỗi do component chỉ export default.
   - *Xử lý:* Cập nhật `RecipeDetailView.tsx` hỗ trợ cả `export function RecipeDetailView` (named) và `export default RecipeDetailView` (default).

---

## 3. DANH SÁCH FILE ĐÃ SỬA VÀ THÊM MỚI TRÊN BRANCH TÍCH HỢP

Các file đã điều chỉnh trong đợt kiểm thử và tinh chỉnh Task 6:
- `frontend/culinary-blog-web/components/layout/Header.tsx`: Sửa responsive mobile, chuyển nút guest vào mobile drawer, bảo đảm không tràn ngang.
- `frontend/culinary-blog-web/app/page.tsx`: Thêm bao bọc chống tràn màn hình ngang `overflow-x-hidden`.
- `frontend/culinary-blog-web/components/recipe/RecipeDetailView.tsx`: Thêm export song song named & default.
- `frontend/culinary-blog-web/components/recipes/RecipeDetailView.tsx`: Đồng bộ hóa export với `components/recipe/`.
- `frontend/culinary-blog-web/app/components-preview/page.tsx`: Môi trường kiểm thử tĩnh độc lập cho toàn bộ component Task 6 của 4 thành viên.
- `scripts/validate_ui.mjs`: Script tự động hóa kiểm thử trình duyệt Chrome thực tế bằng Playwright.
- `docs/task6-validation/`: Thư mục lưu trữ bằng chứng ảnh chụp màn hình thực tế.

---

## 4. KẾT QUẢ NGHIỆM THU SEARCH API THỰC TẾ VỚI POSTGRESQL

Đã kiểm tra trực tiếp trên API ASP.NET Core đang chạy (`http://localhost:5000`) kết nối cơ sở dữ liệu PostgreSQL (`culinaryblog-postgres`):
1. **Tìm kiếm có dấu:**
   - Request: `GET /api/v1/recipes/search?q=phở bò`
   - Response: HTTP 200 OK, trả về 2 công thức `Phở bò tái lăn` và `Phở bò truyền thống Hà Nội` với `matchType: "FullTextSearch"`, rank điểm liên quan cao nhất.
2. **Tìm kiếm không dấu:**
   - Request: `GET /api/v1/recipes/search?q=pho bo`
   - Response: HTTP 200 OK, trả về 2 công thức với `matchType: "FullTextSearch"` nhờ PostgreSQL `unaccent` extension.
3. **Tìm kiếm mờ (Fuzzy Search khi gõ sai chính tả):**
   - Request: `GET /api/v1/recipes/search?q=phoo bo`
   - Response: HTTP 200 OK, trả về `Phở bò tái lăn` với `matchType: "FuzzyTrigram"` nhờ `pg_trgm` extension và GIN index.

---

## 5. BẰNG CHỨNG KIỂM THỬ TRÌNH DUYỆT (SCREENSHOTS)

Các ảnh chụp màn hình thực tế được lưu trữ tại `docs/task6-validation/`:
1. `desktop_home.png` (306 KB): Giao diện trang chủ hoàn chỉnh ở độ phân giải Desktop 1440×900.
2. `tablet_home.png` (187 KB): Giao diện responsive trên Tablet 768×1024.
3. `mobile_home.png` (100 KB): Giao diện responsive trên Mobile 390×844 (không tràn ngang).
4. `search_page.png` (81 KB): Giao diện trang tìm kiếm `/search` với tương tác gõ từ khóa và danh sách kết quả.
5. `preview_tab1_recipe.png` (85 KB): Giao diện RecipeDetailView và RecipeFormLayout của Phạm Nguyễn Ngọc Phước.
6. `preview_tab2_step.png` (138 KB): Giao diện StepEditor và tương tác thêm/sửa bước của Võ Hùng Mạnh.
7. `preview_tab3_image.png` (291 KB): Giao diện ImageManager, ImagePreview và ImageCard của Võ Hùng Mạnh.
8. `preview_tab4_shared_dialogs.png` (85 KB): Giao diện ConfirmDialog và Shared UI states của Lê Thị Ánh Nhung.

---

## 6. KẾT LUẬN CUỐI CÙNG

- **Tình trạng branch tích hợp:** Toàn bộ công việc Task 6 / Lab 6 của cả 4 thành viên (Nguyễn Văn Quốc, Lê Thị Ánh Nhung, Phạm Nguyễn Ngọc Phước, Võ Hùng Mạnh) đã được tích hợp đầy đủ, sửa hết các lỗi conflict, type mismatch, responsive và kiểm thử hoàn tất trên branch `integration/task6-validation`.
- **Trạng thái tuân thủ cam kết:**
  - **CHƯA MERGE VÀO `main`**.
  - **CHƯA PUSH LÊN REMOTE GITHUB**.
  - **BẢO VỆ NGUYÊN VẸN TẤT CẢ CÁC LOCAL BRANCH KHÁC**.
- **Đánh giá mức độ sẵn sàng:** **READY TO MERGE** (Đủ điều kiện để merge vào `main` khi có lệnh từ người quản lý dự án).
