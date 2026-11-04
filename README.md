# Võ Hùng Mạnh - Báo cáo tổng hợp đóng góp cá nhân (Lab 1 - Lab 8)

> **Họ và tên:** Võ Hùng Mạnh
> **MSSV:** 2312687
> **Lớp / Học phần:** Phát triển ứng dụng Web nâng cao (PTUDWNC) — Nhóm 12
> **Vai trò trong nhóm:** Nhóm trưởng / Phụ trách Module RecipeStep, RecipeImage, Full-Text Search & Hạ tầng kỹ thuật
> **Nhánh cá nhân tổng hợp:** `2312687_VoHungManh`

---

## Bảng tổng hợp tiến độ và các nhánh tính năng chi tiết

Do quy trình làm việc của nhóm áp dụng Feature Branch Workflow, mỗi bài Lab/Task được phát triển và kiểm thử độc lập trên từng nhánh chuyên biệt. Dưới đây là bảng định tuyến mã nguồn và tài liệu tương ứng:

| Bữa / Task | Nội dung phụ trách | Nhánh Git triển khai | Tài liệu chi tiết | Trạng thái |
|:---:|---|---|---|:---:|
| **Bữa 1** | Cấu trúc dữ liệu `RecipeStep` & `RecipeImage` | `2312687_VoHungManh`, `main` | Mục [Bữa 1](#bữa-1---xây-dựng-cấu-trúc-dữ-liệu-ban-đầu) | **Hoàn thành** |
| **Bữa 2** | Entity, Configuration & Seed data Step/Image | `2312687_VoHungManh`, `main` | Mục [Bữa 2](#bữa-2---hoàn-thiện-cấu-hình-dữ-liệu-và-dữ-liệu-kiểm-thử) | **Hoàn thành** |
| **Bữa 3** | Tối ưu truy vấn, PostgreSQL Full-Text Search, Unaccent & Fuzzy | `2312687_VoHungManh`, `main` | Mục [Bữa 3](#bữa-3---thiết-kế-dữ-liệu-và-tối-ưu-truy-vấn) | **Hoàn thành** |
| **Bữa 4** | API Endpoints Step/Image, MinIO, Health Check, Observability, Thumbnail Job | `feat/vohungmanh-*` (7 nhánh), `main` | Mục [Bữa 4](#bữa-4---hoàn-thiện-api-endpoints-và-hạ-tầng) | **Hoàn thành** |
| **Bữa 5** | Frontend Foundation, API Contracts TypeScript, Rate Limiting | `feat/vohungmanh-task5-frontend-foundation`, `feat/task5-integration` | Mục [Bữa 5](#bữa-5---chuẩn-hóa-api-contracts-và-frontend-foundation) | **Hoàn thành** |
| **Bữa 6** | Chuẩn hóa Step, Image, Search bám sát SRS v1.2.0 & E2E Validation | `feat/vohungmanh-task6-step`, `...task6-image`, `...task6-search`, `integration/task6-validation` | [README_TASK6_SEARCH](docs/README_TASK6_SEARCH.md) | **Hoàn thành** |
| **Bữa 7** | Recipe Search UI toàn diện (filter, sort, debounce, URL sync, 16 ảnh E2E) | `feat/vohungmanh-task7-search`, `integration/task7-validation` | [README_TASK7_SEARCH](docs/README_TASK7_SEARCH.md) | **Hoàn thành** |
| **Bữa 8** | Hoàn thiện StepEditor, ImageManager, upload MinIO, Vitest & Playwright E2E | `feat/vohungmanh-task8-step`, `feat/vohungmanh-task8-image` | [README_TASK8_STEP](docs/README_TASK8_STEP.md), [README_TASK8_IMAGE](docs/README_TASK8_IMAGE.md) | **Hoàn thành** |

---

## Bữa 1 - Xây dựng cấu trúc dữ liệu ban đầu

**Phụ trách: RecipeStep & RecipeImage**

- Xác định cấu trúc dữ liệu cho các bước chế biến (`RecipeStep`).
- Hoàn thiện các thuộc tính cần thiết của `RecipeStep`.
- Sử dụng `StepNumber` để quản lý thứ tự các bước chế biến.
- Xác định cấu trúc dữ liệu hình ảnh của công thức (`RecipeImage`).
- Hoàn thiện các thông tin cần thiết để lưu và hiển thị hình ảnh.
- Thiết lập quan hệ giữa `RecipeStep` và `Recipe`.
- Thiết lập quan hệ giữa `RecipeImage` và `Recipe`.
- Chuẩn bị dữ liệu mẫu cho các bước chế biến và hình ảnh.

**Kết quả:** Hoàn thành cấu trúc dữ liệu nền cho Step và Image, đảm bảo một Recipe có thể có nhiều bước chế biến và nhiều hình ảnh.

---

## Bữa 2 - Hoàn thiện cấu hình dữ liệu và dữ liệu kiểm thử

**Phụ trách: RecipeStep & RecipeImage**

- Hoàn thiện entity `RecipeStep`.
- Hoàn thiện entity `RecipeImage`.
- Hoàn thiện `RecipeStepConfiguration`.
- Hoàn thiện `RecipeImageConfiguration`.
- Kiểm tra và hoàn thiện quan hệ giữa `RecipeStep`, `RecipeImage` và `Recipe`.
- Xử lý các điểm chưa thống nhất giữa Entity và Configuration của Step/Image.
- Hoàn thiện thứ tự các bước chế biến.
- Hoàn thiện thông tin thứ tự hiển thị hình ảnh.
- Xây dựng dữ liệu kiểm thử cho `RecipeStep`.
- Xây dựng dữ liệu kiểm thử cho `RecipeImage`.
- Đảm bảo mỗi Recipe sau khi tích hợp có ít nhất 5 bước chế biến.
- Kiểm tra dữ liệu Step/Image sau khi seed để đảm bảo quan hệ dữ liệu được tạo đúng.

**Kết quả:** Hoàn thiện Entity, Configuration, relationship và dữ liệu kiểm thử cho RecipeStep/RecipeImage, tạo nền tảng để sử dụng Step/Image trong Recipe Detail.

---

## Bữa 3 - Thiết kế dữ liệu và tối ưu truy vấn

**Phụ trách: Step, Image & Full-Text Search**

### RecipeStep & RecipeImage
- Rà soát `RecipeStep`, `RecipeImage` và các Configuration tương ứng.
- Kiểm tra lại quan hệ giữa `RecipeStep`, `RecipeImage` và `Recipe`.
- Kiểm tra và đảm bảo thứ tự các bước chế biến bằng `StepNumber`.
- Kiểm tra thứ tự hiển thị hình ảnh bằng `SortOrder`/`OrderIndex`.
- Kiểm tra và hoàn thiện quy tắc ảnh chính (`IsPrimary`) của Recipe.

### Tối ưu truy vấn
- Tối ưu các truy vấn chỉ đọc Step/Image bằng `AsNoTracking()` khi phù hợp.
- Kiểm tra cách load Step và Image trong Recipe Detail.
- Hạn chế N+1 Query khi lấy dữ liệu liên quan đến Recipe.
- Kiểm thử lại Recipe Detail sau khi tối ưu truy vấn.

### PostgreSQL Full-Text Search
- Triển khai Full-Text Search cho Recipe bằng PostgreSQL.
- Cấu hình `SearchVector` phục vụ tìm kiếm toàn văn.
- Cấu hình GIN Index cho `SearchVector`.
- Sử dụng `PlainToTsQuery()` để tạo truy vấn Full-Text Search.
- Sử dụng `unaccent` để hỗ trợ tìm kiếm tiếng Việt không dấu.
- Tạo và kiểm tra Migration liên quan đến SearchVector/GIN Index.
- Kiểm thử từ khóa `pho bo` và đảm bảo tìm được Recipe `Phở bò`.

### Phần mở rộng
Ngoài yêu cầu chính của Bữa 3, đã bổ sung:
- Fuzzy Search bằng PostgreSQL `pg_trgm`.
- Hỗ trợ trường hợp gõ sai nhẹ như `phoo bo` vẫn tìm được `Phở bò Hà Nội`.
- Cache cho Search và Recipe Detail.
- Cơ chế Resilient Cache/fail-safe khi Redis không khả dụng.
- Bổ sung dữ liệu demo `Phở bò Hà Nội` để kiểm thử Full-Text Search trực tiếp.
- Seeder dữ liệu demo theo hướng idempotent, tránh tạo dữ liệu trùng.
- Bổ sung Domain Validation/Domain Exception cho Step và Image.
- Kiểm thử các chức năng Step, Image và Full-Text Search sau khi tích hợp.

**Kết quả:** Hoàn thành RecipeStep, RecipeImage, tối ưu truy vấn Step/Image và PostgreSQL Full-Text Search; hỗ trợ tìm kiếm có dấu, không dấu và fuzzy search cho Recipe.

---

## Bữa 4 - Hoàn thiện API Endpoints và Hạ tầng

**Phụ trách: Step/Image API, MinIO Storage, Health Check, Observability & Thumbnail Job**

- **Recipe Step API:** Triển khai đầy đủ các endpoint CRUD cho bước nấu ăn: `GET`, `POST`, `PUT`, `DELETE /api/v1/recipes/{id}/steps` và endpoint sắp xếp `PUT .../steps/reorder`.
- **Recipe Image API:** Triển khai endpoint quản lý ảnh: `GET`, `POST`, `DELETE /api/v1/recipes/{id}/images`, endpoint đặt ảnh chính `PATCH .../images/{imageId}/set-primary` và xác thực định dạng ảnh qua `ImageValidator` (hỗ trợ JPG, PNG, WebP, AVIF; tối đa 5MB).
- **MinIO Object Storage:** Tích hợp `IStorageService`, cấu hình MinIO S3-compatible lưu trữ tệp tin độc lập thay vì lưu trên local disk.
- **Application Health Checks:** Thiết lập giám sát sức khỏe dịch vụ tại `/health`, `/health/ready`, `/health/live` kiểm tra tự động trạng thái kết nối PostgreSQL và MinIO.
- **Observability & Logging:** Cấu hình Serilog Structured Logging, middleware `CorrelationIdMiddleware` gán mã vết cho mỗi request HTTP, `LoggingBehavior` trong MediatR pipeline.
- **Thumbnail Background Job:** Xây dựng worker ngầm sử dụng `SixLabors.ImageSharp` tự động sinh ảnh thu nhỏ kích thước chuẩn phục vụ tối ưu tải trang.

*Mã nguồn chi tiết được lưu trữ và nghiệm thu trên các nhánh tính năng: `feat/vohungmanh-file-storage`, `feat/vohungmanh-full-text-search`, `feat/vohungmanh-health-check`, `feat/vohungmanh-observability`, `feat/vohungmanh-thumbnail-job`, `feat/vohungmanh-recipe-image`, `feat/vohungmanh-recipe-step`.*

---

## Bữa 5 - Chuẩn hóa API Contracts và Frontend Foundation

**Phụ trách: API Versioning, Frontend Architecture, Types & Contracts**

- **TypeScript Type Definitions:** Thiết kế toàn bộ giao diện kiểu dữ liệu cốt lõi cho Frontend: `types/api.ts` (ApiResponse wrapper envelope), `types/step.ts`, `types/image.ts`, `types/search.ts`.
- **Rate Limiting Middleware:** Tích hợp cơ chế giới hạn tần suất gọi API trên ASP.NET Core (`RateLimitingExtensions.cs`) bảo vệ hệ thống trước tấn công DoS/Brute Force.
- **Tài liệu API chuẩn hóa:** Đóng góp xây dựng tài liệu đặc tả hợp đồng API đồng bộ tại `docs/API_DOCUMENTATION.md`.

*Mã nguồn chi tiết được phát triển trên nhánh: `feat/vohungmanh-task5-frontend-foundation` và nhánh tích hợp `feat/task5-integration`.*

---

## Bữa 6 - Chuẩn hóa giao diện & nghiệp vụ theo SRS v1.2.0

**Phụ trách: Traceability Matrix, Step/Image/Search Frontend & Validation**

- **Chuẩn hóa Recipe Image:** Tuân thủ Bảng 7.5 SRS v1.2.0, giới hạn độ dài `ImageUrl` tối đa 500 ký tự, `AltText` tối đa 200 ký tự, bổ sung PATCH endpoint. (Chi tiết tại nhánh `feat/vohungmanh-task6-image`).
- **Chuẩn hóa Recipe Step:** Tuân thủ Bảng 7.3 SRS v1.2.0, validation `imageUrl` max 500 ký tự, `timerMinutes` 0-1440 phút, `description` max 2000 ký tự. (Chi tiết tại nhánh `feat/vohungmanh-task6-step`).
- **Chuẩn hóa Recipe Search:** Tìm kiếm toàn văn unaccent kết hợp lọc danh mục server-side, sort CookTime/Title, debounce 400ms và đồng bộ URL query params. (Chi tiết tại nhánh `feat/vohungmanh-task6-search` - tài liệu mẫu mực 672 dòng).
- **Kiểm thử nghiệm thu E2E Task 6:** Thực hiện kiểm thử toàn diện trên nhánh `integration/task6-validation`, chụp ảnh kiểm thử đa kích thước màn hình lưu tại `docs/task6-validation/`.

---

## Bữa 7 - Hoàn thiện toàn diện Recipe Search UI

**Phụ trách: Nâng cấp trải nghiệm tìm kiếm, Lọc đa tiêu chí & Khôi phục trạng thái**

- Bổ sung bộ lọc danh mục (`category`), độ khó (`difficulty`: Easy/Medium/Hard), sắp xếp thời gian nấu (`cookTimeAsc`/`cookTimeDesc`).
- Đồng bộ hóa hai chiều giữa form tìm kiếm và URL browser; hỗ trợ tải lại trang giữ nguyên trạng thái (URL state preservation) và điều hướng Back/Forward trên trình duyệt.
- Xử lý các trạng thái giao diện: Loading Skeleton, Error State, Empty State thân thiện.
- Thực hiện kiểm thử nghiệm thu 16 kịch bản thực tế, chụp đầy đủ 16 ảnh minh chứng lưu tại `docs/task7-search/screenshots/`.
- Tham gia nghiệm thu tích hợp toàn bộ hệ thống nhóm và kiểm toán cơ sở dữ liệu Docker PostgreSQL trên nhánh `integration/task7-validation`.

*Chi tiết tài liệu và hình ảnh minh chứng: [README_TASK7_SEARCH.md](docs/README_TASK7_SEARCH.md).*

---

## Bữa 8 - Hoàn thiện StepEditor, ImageManager và kiểm thử Playwright E2E

**Phụ trách: Hoàn thiện UI Step & Image, Tải ảnh MinIO, Kiểm thử tự động**

- **Quản lý bước nấu ăn (`StepEditor.tsx`):**
  - Hỗ trợ xem, thêm, sửa, xóa (kèm Modal xác nhận).
  - Sắp xếp thứ tự (Move Up/Down) với Optimistic UI và cơ chế tự động Rollback khi API lỗi.
  - Tải tệp ảnh trực tiếp từ máy lên MinIO (chặn file >5MB).
  - Phân quyền RBAC: Guest và Author không chính chủ bị chặn chỉnh sửa (`forbidden-banner`).
  - Đạt **21/21 Unit Tests PASSED** và kịch bản Playwright E2E (`step-editor.spec.ts`).
  - *Mã nguồn & tài liệu chi tiết:* Nhánh `feat/vohungmanh-task8-step` / [README_TASK8_STEP.md](docs/README_TASK8_STEP.md).

- **Quản lý hình ảnh (`ImageManager.tsx`):**
  - Tải lên ảnh qua Multipart Form Data lên MinIO, kiểm tra định dạng (JPG, PNG, WebP, AVIF) và dung lượng tối đa 5MB.
  - Đặt ảnh đại diện (`Primary Image`).
  - Cơ chế tự động chuyển giao quyền Primary cho ảnh nhỏ nhất còn lại khi xóa ảnh đại diện (FR-RCP-008).
  - Đạt **16/16 Unit Tests PASSED** và kịch bản Playwright E2E (`image-manager.spec.ts`).
  - *Mã nguồn & tài liệu chi tiết:* Nhánh `feat/vohungmanh-task8-image` / [README_TASK8_IMAGE.md](docs/README_TASK8_IMAGE.md).
