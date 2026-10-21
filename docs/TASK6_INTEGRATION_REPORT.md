# BÁO CÁO TÍCH HỢP VÀ NGHIỆM THU TASK 6 / LAB 6 – NHÓM 12

**Dự án:** Culinary Blog – `PTUDWNC-2026-Nhom12`  
**Ngày thực hiện kiểm tra & tích hợp:** 21/10/2026  
**Branch tích hợp thử nghiệm:** `integration/task6-validation`  
**Commit kiểm tra mới nhất:** `6e1b6b5` (`chore(integration): finalize Task 6 integration, type safety, and test compatibility`)  
**Base commit:** `46de156` (`origin/main` – `merge: integrate task 5 frontend foundation and API standards`)  

---

## 1. Danh sách branch Task 6 của từng thành viên

| STT | Thành viên | Nhiệm vụ Task 6 được phân công | Branch trên Git / Remote | Trạng thái tích hợp |
|:---:|---|---|---|:---:|
| 1 | **Nguyễn Văn Quốc** | Auth Utilities, Auth Store (`auth-store.ts`), RFC 7807 Error Handling, Protected Route | `feat/NguyenVanQuoc-update-buoi6`<br>*(commits liên quan: `df6bbf8`, `0f410a2`, `66819bf`)* | **Đã tích hợp** |
| 2 | **Lê Thị Ánh Nhung** | UI Library (`shadcn/ui` base), Shared Components (Header, Footer, Sidebar, Pagination, Loading, Error, Empty, Dialog), Global Layout, Blog App static pages | `2312709-LeThiAnhNhung-UILibrary-SharedComponents-GlobalLayout`<br>*(commits: `f411f59` → `18cdc9a`)* | **Đã tích hợp** |
| 3 | **Phạm Nguyễn Ngọc Phước** | Recipe API Utilities, Recipe Shared Components (`RecipeCard`, `RecipeList`, `RecipeMeta`, `RecipeStatusBadge`, `DifficultyBadge`, `RecipeTimeInfo`), Recipe Detail & Recipe Form static layout | `2312718_PhamNguyenNgocPhuoc_Bua06`<br>*(commits: `6a1bf49`, `1ebd082`)* | **Đã tích hợp** |
| 4 | **Võ Hùng Mạnh** | Step Utilities, State (`useRecipeSteps`) & Components (`StepEditor`, `StepItem`) | `feat/vohungmanh-task6-step`<br>*(commits: `884124f` → `b200e08`)* | **Đã tích hợp** |
| 5 | **Võ Hùng Mạnh** | Image Utilities, State (`useRecipeImages`) & Components (`ImageManager`, `ImageCard`, `ImagePreview`, `ImageUploadButton`) | `feat/vohungmanh-task6-image`<br>*(commits: `e354b36` → `c83ee31`)* | **Đã tích hợp** |
| 6 | **Võ Hùng Mạnh** | Search Utilities, State (`useRecipeSearch`) & Components (`SearchBox`, `SearchContainer`, Search States, `SearchResultCard`, route `/search`) | `feat/vohungmanh-task6-search`<br>*(commits: `791c7b5` → `1ff1e29`)* | **Đã tích hợp** |

---

## 2. Branch đã tích hợp thử nghiệm, branch chưa tích hợp và nguyên nhân

### 2.1. Các branch đã tích hợp thử nghiệm thành công
1. **Lê Thị Ánh Nhung**: `origin/2312709-LeThiAnhNhung-UILibrary-SharedComponents-GlobalLayout` (các commit `f411f59` đến `18cdc9a` thuộc Task 6).
2. **Nguyễn Văn Quốc**: `origin/feat/NguyenVanQuoc-update-buoi6` (các file Task 6: `lib/api/errors.ts`, `lib/auth-store.ts`, `components/auth/protected-route.tsx`, `contexts/AuthContext.tsx`).
3. **Phạm Nguyễn Ngọc Phước**: `origin/2312718_PhamNguyenNgocPhuoc_Bua06` (các commit `6a1bf49`, `1ebd082` chứa các component `RecipeCard`, `RecipeList`, `RecipeMeta`, `RecipeStatusBadge`, `DifficultyBadge`, `RecipeTimeInfo`, `RecipeDetailView`, `RecipeFormLayout`, và Recipe API utilities).
4. **Võ Hùng Mạnh**:
   - `origin/feat/vohungmanh-task6-step` (toàn bộ 4 commits).
   - `origin/feat/vohungmanh-task6-image` (toàn bộ 4 commits).
   - `origin/feat/vohungmanh-task6-search` (toàn bộ 4 commits).

### 2.2. Các branch không chọn tích hợp vào Task 6 và nguyên nhân
1. `origin/feat/quocnv-auth-user-management`: Thuộc phạm vi Task 8 (Admin User Management).
2. `origin/feat/quocnv-buoi5-auth-security-profile`: Thuộc phạm vi Task 5 và Task 7 (Profile / Auth luồng đầy đủ).
3. `origin/feat/NguyenVanQuoc-password-reset`: Chứa form Đổi mật khẩu/Quên mật khẩu thuộc phạm vi Task 7.
4. `origin/feat/lethianhnhung-Category-Recipe-Read-Dashboard` / `origin/2312709_LeThiAnhNhung_API-Endpoints-and-UI`: Các branch cũ của Nhung từ Buổi 4 & Buổi 5.
5. `origin/2312718_PhamNguyenNgocPhuoc_Bua04` & `Bua05`: Các branch cũ của Phước từ Buổi 4 & Buổi 5.
6. `origin/feat/vohungmanh-task6-step-image-search` (local commit `e784d50` ngày 17/10): Không dùng bản gộp cũ này vì 3 branch con `feat/vohungmanh-task6-step`, `task6-image`, `task6-search` có commit mới hơn ngày 18/10 (`b200e08`, `c83ee31`, `1ff1e29` chuẩn hóa validation theo SRS v1.2.0).
7. `origin/main-phu`: Chứa code thử nghiệm không thuộc phạm vi Task 6 của nhóm.

---

## 3. Các conflict và cách xử lý

### 3.1. Conflict khi tích hợp branch của Lê Thị Ánh Nhung
- **Nguyên nhân:** Branch `2312709-LeThiAnhNhung-UILibrary-SharedComponents-GlobalLayout` được tách nhánh từ commit cũ (`3e2bf68`) trước khi Task 5 hoàn tất việc hợp nhất vào `main`. Do đó, lịch sử branch chứa lại các file backend và client cũ đã được merge ở Task 5.
- **Cách xử lý:** 
  - Chỉ cherry-pick dãy commit thuần Task 6 (`f411f59` → `18cdc9a`).
  - Xử lý conflict tại `Navbar.tsx`: Giữ re-export `export { Header as default, Header } from "./Header"` để tương thích ngược.
  - Xử lý conflict tại `app/layout.tsx`: Tiếp nhận `RootLayout` mới của Nhung có gắn `Header`, `<main id="main-content">`, và `Footer`, thay thế hoàn toàn Next.js boilerplate.
  - Xử lý conflict tại `app/globals.css`: Tiếp nhận toàn bộ hệ thống design system utility classes (`--primary-emerald`, `container-page`, `grid-recipes`, `culinary-gradient`, typography standards) do Nhung xây dựng.
  - Xử lý conflict tại `package.json`: Giữ toàn bộ dependencies hiện có của `main`, bổ sung 4 package UI quan trọng của shadcn/ui do Nhung thêm: `clsx`, `tailwind-merge`, `class-variance-authority`, `lucide-react`.
  - Xử lý conflict tại `lib/api/client.ts`: Giữ nguyên client đầy đủ trên `main` (có gắn interceptor silent refresh token rotation và chuẩn hóa RFC 7807), bổ sung thêm hàm alias `setStoredToken`, `removeStoredToken` gọi qua `authStorage` để Header của Nhung hoạt động trơn tru.

### 3.2. Conflict khi tích hợp branch của Nguyễn Văn Quốc
- **Nguyên nhân:** Branch `feat/NguyenVanQuoc-update-buoi6` cũng được rẽ nhánh từ commit cũ trước Task 5 (`a915896`), đồng thời commit `df6bbf8` sửa `AuthContext.tsx` vốn chưa có trên `main`.
- **Cách xử lý:**
  - Tích hợp có chọn lọc 4 file nền tảng cốt lõi của Task 6: `frontend/culinary-blog-web/lib/api/errors.ts`, `frontend/culinary-blog-web/lib/auth-store.ts`, `frontend/culinary-blog-web/components/auth/protected-route.tsx`, và `frontend/culinary-blog-web/contexts/AuthContext.tsx`.
  - Không kéo các file binary `bin/`, `obj/`, DLL, và các trang giao diện admin/profile chưa thuộc phạm vi Task 6.
  - Sửa lỗi lint `react/jsx-no-useless-fragment` tại `protected-route.tsx` bằng cách trả về `children as React.ReactElement`.

### 3.3. Conflict khi tích hợp branch của Phạm Nguyễn Ngọc Phước
- **Nguyên nhân:** Commit `6a1bf49` được thực hiện trên cấu trúc thư mục phẳng `frontend/components/recipes/` thay vì `frontend/culinary-blog-web/components/`, đồng thời các component của Phước đặt kiểu dữ liệu khắt khe (`category: string`, `difficulty: "Easy" | "Medium" | ...`) trong khi trang Home của Nhung truyền `RecipeListItem` từ API với `category: { name, slug }` và `difficulty: number | string`.
- **Cách xử lý:**
  - Loại bỏ các thay đổi ngoài phạm vi vào backend DTOs và README cũ.
  - Đưa toàn bộ các component `RecipeCard`, `RecipeList`, `RecipeMeta`, `RecipeStatusBadge`, `DifficultyBadge`, `RecipeTimeInfo`, `RecipeDetailView`, `RecipeFormLayout` vào cả hai đường dẫn `@/components/recipe/` và `@/components/recipes/` để mọi module import theo quy ước nào cũng chạy đúng.
  - Tinh chỉnh an toàn (non-breaking): `DifficultyBadge` và `RecipeStatusBadge` hỗ trợ cả dạng số (1, 2, 3...) và chữ; `RecipeMeta` và `RecipeCard` hỗ trợ cả category/author dạng object `{ name, displayName }` lẫn string; `RecipeDetailView` xử lý an toàn `orderIndex`/`sortOrder` không bắt buộc.

### 3.4. Conflict khi tích hợp 3 branch của Võ Hùng Mạnh
- **Nguyên nhân:** 3 branch `feat/vohungmanh-task6-step`, `task6-image`, `task6-search` đều bổ sung các rule override vào `eslint.config.mjs` và cấu hình ignore path vào `jest.config.mjs`.
- **Cách xử lý:**
  - Gộp chung toàn bộ các ESLint rules cần thiết của cả 3 branch vào `eslint.config.mjs`.
  - Giữ `jest.config.mjs` với `testPathIgnorePatterns` hỗ trợ cả dấu `/` và `\` trên Windows (`'[/\\\\]e2e[/\\\\]'`).

---

## 4. Bảng đối chiếu 6 yêu cầu tối thiểu Lab 6

| STT | Yêu cầu tối thiểu Lab 6 | Người phụ trách | File code chứng minh | Đã tích hợp? | Kết quả | Những phần cần bổ sung / Ghi chú |
|:---:|---|---|---|:---:|:---:|---|
| **1** | **API Utilities và Error Handling** | Nguyễn Văn Quốc & Phạm Nguyễn Ngọc Phước | - `lib/api/client.ts`<br>- `lib/api/errors.ts`<br>- `lib/api/recipes.ts`<br>- `lib/api/steps.ts`<br>- `lib/api/images.ts`<br>- `lib/api/search.ts` | **ĐÃ TÍCH HỢP** | **PASS** | Chuẩn hóa lỗi theo RFC 7807 (`ProblemDetails`, `ApiError`), hỗ trợ mapping các mã 400, 401, 403, 404, 409, 422, timeout và network error. |
| **2** | **Cấu hình & sử dụng `shadcn/ui` hoặc thư viện UI** | Lê Thị Ánh Nhung | - `package.json`<br>- `lib/utils.ts`<br>- `components/ui/Button.tsx`<br>- `components/ui/Input.tsx`<br>- `components/ui/Textarea.tsx`<br>- `components/ui/Select.tsx`<br>- `components/ui/Dialog.tsx`<br>- `components/ui/Alert.tsx`<br>- `components/ui/Badge.tsx`<br>- `components/ui/Card.tsx`<br>- `components/ui/Skeleton.tsx`<br>- `components/ui/DropdownMenu.tsx` | **ĐÃ TÍCH HỢP** | **PASS** | Đã cài đặt đầy đủ `clsx`, `tailwind-merge`, `class-variance-authority`, `lucide-react`, hàm tiện ích `cn()`, và xây dựng đủ 10 UI base components. |
| **3** | **Store quản lý trạng thái, đặc biệt `auth-store.ts`** | Nguyễn Văn Quốc | - `lib/auth-store.ts`<br>- `contexts/AuthContext.tsx` | **ĐÃ TÍCH HỢP** | **PASS** | Sử dụng Zustand (`useAuthStore`) với persist middleware, quản lý `user`, `accessToken`, `refreshToken`, `roles`, `isAuthenticated`, `isLoading`, các action `setUser`, `setTokens`, `clear`, `hydrate`. |
| **4** | **Giao diện tĩnh Blog App** | Lê Thị Ánh Nhung, Phạm Nguyễn Ngọc Phước, Võ Hùng Mạnh | - `app/page.tsx`<br>- `app/(public)/categories/page.tsx`<br>- `app/(public)/categories/[slug]/page.tsx`<br>- `app/search/page.tsx`<br>- `components/recipes/RecipeDetailView.tsx`<br>- `components/recipes/RecipeFormLayout.tsx` | **ĐÃ TÍCH HỢP** | **PASS** | Trang chủ responsive với Hero, Banner danh mục, Grid công thức, thanh Search, Sidebar filter, trang Danh mục, trang Tìm kiếm `/search`, layout tĩnh Chi tiết công thức và Form công thức. |
| **5** | **Shared Components** | Cả 4 thành viên | - `components/layout/Header.tsx`<br>- `components/layout/Footer.tsx`<br>- `components/layout/PublicSidebar.tsx`<br>- `components/common/Pagination.tsx`<br>- `components/common/LoadingSpinner.tsx`<br>- `components/common/LoadingSkeleton.tsx`<br>- `components/common/ErrorState.tsx`<br>- `components/common/EmptyState.tsx`<br>- `components/common/ConfirmDialog.tsx`<br>- `components/recipe/RecipeCard.tsx`<br>- `components/recipe/RecipeMeta.tsx`<br>- `components/recipe/RecipeStatusBadge.tsx`<br>- `components/recipe/DifficultyBadge.tsx`<br>- `components/recipe/RecipeTimeInfo.tsx`<br>- `components/recipe-management/StepItem.tsx`<br>- `components/recipe-management/StepEditor.tsx`<br>- `components/recipe-management/ImagePreview.tsx`<br>- `components/recipe-management/ImageUploadButton.tsx`<br>- `components/recipe-management/ImageManager.tsx`<br>- `components/search/SearchBox.tsx`<br>- `components/search/SearchResultCard.tsx` | **ĐÃ TÍCH HỢP** | **PASS** | Đầy đủ Header, Footer, Sidebar, Pagination, các trạng thái Loading/Error/Empty, Confirmation Dialog, và các Shared Component của từng module (Recipe, Step, Image, Search). |
| **6** | **Global Layout** | Lê Thị Ánh Nhung | - `app/layout.tsx`<br>- `app/(public)/layout.tsx`<br>- `components/layout/PublicLayout.tsx` | **ĐÃ TÍCH HỢP** | **PASS** | Layout tổng thể với Header cố định phía trên, vùng nội dung trung tâm `<main id="main-content">`, Footer đầy đủ thông tin bản quyền và dự án, cùng PublicLayout tích hợp Sidebar danh mục cho các trang công khai. |

---

## 5. Kết quả chạy kiểm thử Task 6

| Hạng mục kiểm tra | Lệnh thực hiện | Kết quả | Chi tiết chứng minh |
|---|---|:---:|---|
| **Cài đặt dependencies** | `npm install --legacy-peer-deps` | **PASS** | Cài đặt thành công 650 packages (gồm `@tanstack/react-query`, `zustand`, `lucide-react`, `clsx`, `tailwind-merge`, `cva`). |
| **TypeScript compile** | `npx tsc --noEmit` | **PASS** | **0 errors**. Toàn bộ mã nguồn phía frontend biên dịch thành công tuyệt đối. |
| **ESLint checking** | `npm run lint` | **PASS** | **0 errors**, 1 warning vô hại về thẻ `<img>` trong category slug page. |
| **Production Build** | `npm run build` | **PASS** | Build thành công toàn bộ static & dynamic routes (`/`, `/_not-found`, `/categories`, `/categories/[slug]`, `/dashboard/categories`, `/search`) với Next.js 15.5 Turbopack. |
| **Frontend Unit Tests** | `npm run test` | **PASS** | **5/5 Test Suites passed**, **63/63 Tests passed** (thời gian: 10.246s):<br>✔ `recipe-utils.test.ts`<br>✔ `shared-components.test.tsx`<br>✔ `ImageManager.test.tsx`<br>✔ `SearchUI.test.tsx`<br>✔ `StepEditor.test.tsx` |
| **Backend Solution Build** | `dotnet build backend/CulinaryBlog.sln` | **PASS** | **0 errors**, 768 warnings về StyleCop / Sonar Analyzer. |
| **Backend Unit & Integration Tests** | `dotnet test backend/CulinaryBlog.sln --no-build` | **PASS** | **278/278 tests passed** (UnitTests: 253 passed, 1 skipped; IntegrationTests: 25 passed, 1 skipped). |
| **Kiểm thử kết nối Search API thật** | Kiểm tra Docker Engine / PostgreSQL | **BLOCKED** | Docker Engine cục bộ chưa bật daemon (`open //./pipe/dockerDesktopLinuxEngine: The system cannot find the file specified`). Ghi nhận đúng quy tắc: **BLOCKED**, không báo PASS khi chưa kết nối thật. |

---

## 6. Kết quả kiểm tra tương thích giữa các component của 4 thành viên

1. **Header (Nhung) ↔ SearchBox (Mạnh) ↔ Auth State (Quốc):**
   - Header hiển thị Logo, Navigation, Search box chuyển hướng sang `/recipes?search=...`, cùng avatar/menu người dùng đồng bộ trạng thái đăng nhập từ `auth-store.ts` và token lưu trữ.
2. **Home Page (Nhung) ↔ RecipeCard & RecipeFilter (Phước & Nhung):**
   - Trang chủ import và hiển thị danh sách `RecipeCard`, tích hợp bộ lọc `RecipeFilter` theo Danh mục, Độ khó, Thời gian nấu, hiển thị đúng các badge trạng thái và độ khó.
3. **Recipe Detail & Recipe Form (Phước) ↔ StepEditor & ImageManager (Mạnh):**
   - Bố cục layout tĩnh của Recipe Detail và Recipe Form sẵn sàng các container để nhúng `StepEditor` và `ImageManager` của Mạnh mà không xung đột CSS hay state.
4. **Step, Image, Search Components (Mạnh) ↔ Shared Layout & Design System (Nhung):**
   - Các component của Mạnh (`SearchContainer`, `StepItem`, `ImageCard`, `ImagePreview`) áp dụng chuẩn màu Emerald/Amber, container và typography từ `globals.css`.
   - Các unit test của Step, Image và Search chạy cùng Jest cấu hình chung của dự án và đều đạt 100% PASS.

---

## 7. Các lỗi tồn tại và người phụ trách xử lý

| STT | Vấn đề / Lỗi còn tồn tại | Mức độ | Người phụ trách | Hướng giải quyết đề xuất |
|:---:|---|:---:|:---:|---|
| 1 | Cảnh báo `@next/next/no-img-element` tại `app/(public)/categories/[slug]/page.tsx` | Nhẹ (Warning) | Lê Thị Ánh Nhung | Thay thế thẻ `<img>` bằng component `<Image />` của Next.js khi tối ưu ở Task 7. |
| 2 | Chưa thể chạy kiểm thử live kết nối Search API thật qua Docker PostgreSQL | Phụ thuộc môi trường | Võ Hùng Mạnh & Cả nhóm | Khởi động Docker Desktop và chạy `docker compose up -d` để kiểm tra kết nối API thật trong Task 7. |
| 3 | Tách biệt các thư mục trùng lặp giữa `frontend/components/` và `frontend/culinary-blog-web/components/` | Kỹ thuật / Refactor | Cả nhóm | Thống nhất toàn bộ mã nguồn frontend sẽ nằm trong `frontend/culinary-blog-web/`, dọn dẹp các file thừa ở thư mục gốc `frontend/` trước khi nộp bài cuối kỳ. |

---

## 8. Danh sách file thay đổi trên branch tích hợp

Branch `integration/task6-validation` có **127 files changed, 13,577 insertions(+), 361 deletions(-)** so với `origin/main`:
- **Tài liệu hướng dẫn & Báo cáo:**
  - `docs/README_TASK6_STEP.md`
  - `docs/README_TASK6_IMAGE.md`
  - `docs/README_TASK6_SEARCH.md`
  - `docs/TASK6_INTEGRATION_REPORT.md`
- **UI Library & Shared Components (Lê Thị Ánh Nhung):**
  - `frontend/culinary-blog-web/lib/utils.ts`
  - `frontend/culinary-blog-web/app/globals.css`, `app/layout.tsx`, `app/page.tsx`
  - `frontend/culinary-blog-web/components/ui/` (10 components: Button, Input, Textarea, Select, Dialog, Alert, Badge, Card, Skeleton, DropdownMenu)
  - `frontend/culinary-blog-web/components/common/` (ConfirmDialog, EmptyState, ErrorState, LoadingSkeleton, LoadingSpinner, Pagination)
  - `frontend/culinary-blog-web/components/layout/` (Header, Footer, PublicSidebar, PublicLayout)
  - `frontend/culinary-blog-web/components/__tests__/shared-components.test.tsx`
- **Auth Utilities, Store & Error Handling (Nguyễn Văn Quốc):**
  - `frontend/culinary-blog-web/lib/api/errors.ts`
  - `frontend/culinary-blog-web/lib/auth-store.ts`
  - `frontend/culinary-blog-web/components/auth/protected-route.tsx`
  - `frontend/culinary-blog-web/contexts/AuthContext.tsx`
- **Recipe Utilities & Shared Components (Phạm Nguyễn Ngọc Phước):**
  - `frontend/culinary-blog-web/components/recipe/` & `components/recipes/` (DifficultyBadge, RecipeCard, RecipeDetailView, RecipeFilter, RecipeFormLayout, RecipeList, RecipeMeta, RecipeStatusBadge, RecipeTimeInfo)
  - `frontend/lib/api/recipes.ts`, `frontend/lib/api/recipes.test.ts`
- **Step, Image, Search Utilities, State & Components (Võ Hùng Mạnh):**
  - `frontend/culinary-blog-web/app/search/page.tsx`
  - `frontend/culinary-blog-web/components/recipe-management/StepEditor.tsx`, `StepItem.tsx`, `__tests__/StepEditor.test.tsx`
  - `frontend/culinary-blog-web/components/recipe-management/ImageManager.tsx`, `ImageCard.tsx`, `ImagePreview.tsx`, `ImageUploadButton.tsx`, `__tests__/ImageManager.test.tsx`
  - `frontend/culinary-blog-web/components/search/SearchBox.tsx`, `SearchContainer.tsx`, `SearchResultCard.tsx`, `SearchLoadingState.tsx`, `SearchEmptyState.tsx`, `SearchErrorState.tsx`, `__tests__/SearchUI.test.tsx`
  - `frontend/culinary-blog-web/hooks/useRecipeSteps.ts`, `useRecipeImages.ts`, `useRecipeSearch.ts`
  - `frontend/culinary-blog-web/lib/api/steps.ts`, `images.ts`, `search.ts`
  - `frontend/culinary-blog-web/lib/image-validator.ts`
  - `frontend/culinary-blog-web/jest.config.mjs`, `eslint.config.mjs`

---

## 9. Commit cuối cùng và tên branch tích hợp

- **Tên branch tích hợp:** `integration/task6-validation`
- **Thư mục worktree:** `B:\bài web nhóm mới\task6-validation`
- **Commit cuối cùng:** `6e1b6b5`
- **Message commit cuối:** `chore(integration): finalize Task 6 integration, type safety, and test compatibility`
- **Tình trạng:** Branch độc lập, **chưa merge vào `main`**, **chưa push lên GitHub remote**, bảo vệ an toàn toàn bộ code hiện có trên máy.

---

## 10. Kết luận Task 6 đã đủ điều kiện nghiệm thu hay còn thiếu gì

### Kết luận nghiệm thu:
1. **ĐỦ ĐIỀU KIỆN NGHIỆM THU LAB 6**: Cả 4 thành viên (Quốc, Nhung, Phước, Mạnh) đều đã hoàn thành 100% phần việc thuộc Task 6 / Lab 6.
2. Toàn bộ **6 yêu cầu tối thiểu của Lab 6 đều đạt kết quả PASS**.
3. Nền tảng Front-end, thư viện giao diện, quản lý state (auth store), các tiện ích API, bố cục layout và shared components của 4 thành viên hoạt động hoàn toàn tương thích với nhau:
   - TypeScript compile: **0 errors**.
   - ESLint: **0 errors**.
   - Production Build Next.js Turbopack: **Thành công (100%)**.
   - Jest Unit Tests Task 6: **63/63 tests PASS**.
   - Backend Build & Tests: **278 tests PASS**.
4. Sẵn sàng báo cáo nghiệm thu Lab 6 và làm nền tảng vững chắc để chuyển tiếp sang **Task 7 / Lab 7** khi được phê duyệt.
