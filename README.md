# Võ Hùng Mạnh - Nội dung công việc cá nhân

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

**Kết quả:** Hoàn thành cấu trúc dữ liệu nền cho Step và Image, đảm bảo
một Recipe có thể có nhiều bước chế biến và nhiều hình ảnh.

---

## Bữa 2 - Hoàn thiện cấu hình dữ liệu và dữ liệu kiểm thử

**Phụ trách: RecipeStep & RecipeImage**

- Hoàn thiện entity `RecipeStep`.
- Hoàn thiện entity `RecipeImage`.
- Hoàn thiện `RecipeStepConfiguration`.
- Hoàn thiện `RecipeImageConfiguration`.
- Kiểm tra và hoàn thiện quan hệ giữa `RecipeStep`, `RecipeImage`
  và `Recipe`.
- Xử lý các điểm chưa thống nhất giữa Entity và Configuration của
  Step/Image.
- Hoàn thiện thứ tự các bước chế biến.
- Hoàn thiện thông tin thứ tự hiển thị hình ảnh.
- Xây dựng dữ liệu kiểm thử cho `RecipeStep`.
- Xây dựng dữ liệu kiểm thử cho `RecipeImage`.
- Đảm bảo mỗi Recipe sau khi tích hợp có ít nhất 5 bước chế biến.
- Kiểm tra dữ liệu Step/Image sau khi seed để đảm bảo quan hệ dữ liệu
  được tạo đúng.

**Kết quả:** Hoàn thiện Entity, Configuration, relationship và dữ liệu
kiểm thử cho RecipeStep/RecipeImage, tạo nền tảng để sử dụng Step/Image
trong Recipe Detail.

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

- Tối ưu các truy vấn chỉ đọc Step/Image bằng `AsNoTracking()` khi
  phù hợp.
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
- Hỗ trợ trường hợp gõ sai nhẹ như `phoo bo` vẫn tìm được
  `Phở bò Hà Nội`.
- Cache cho Search và Recipe Detail.
- Cơ chế Resilient Cache/fail-safe khi Redis không khả dụng.
- Bổ sung dữ liệu demo `Phở bò Hà Nội` để kiểm thử Full-Text Search
  trực tiếp.
- Seeder dữ liệu demo theo hướng idempotent, tránh tạo dữ liệu trùng.
- Bổ sung Domain Validation/Domain Exception cho Step và Image.
- Kiểm thử các chức năng Step, Image và Full-Text Search sau khi tích hợp.

**Kết quả:** Hoàn thành RecipeStep, RecipeImage, tối ưu truy vấn
Step/Image và PostgreSQL Full-Text Search; hỗ trợ tìm kiếm có dấu,
không dấu và fuzzy search cho Recipe.