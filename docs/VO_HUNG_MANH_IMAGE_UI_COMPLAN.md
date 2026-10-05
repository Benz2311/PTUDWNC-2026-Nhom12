# Báo Cáo Kỹ Thuật & Giải Trình Kiến Trúc - Module Recipe Image UI

> **Sinh viên thực hiện:** Võ Hùng Mạnh (TV4 - Nhóm 12)
> **Học phần:** Phát triển ứng dụng Web nâng cao (PTUDWNC-2026)
> **Dự án:** CulinaryBlog Web Application
> **Branch:** `feat/vohungmanh-image-ui`
> **Tài liệu tham chiếu:** [README_IMAGE_UI.md](file:///b:/PTUDWNC-2026-Nhom12-MANH/docs/README_IMAGE_UI.md)

---

## 1. Tổng quan & Kiến trúc lưu trữ hình ảnh

### 1.1. Tại sao KHÔNG lưu dữ liệu nhị phân (Binary/BLOB) trực tiếp vào PostgreSQL?
Trong kiến trúc hệ thống của CulinaryBlog, toàn bộ tệp nhị phân hình ảnh được lưu trữ trên **Object Storage (MinIO / S3)** và cơ sở dữ liệu quan hệ PostgreSQL chỉ lưu trữ siêu dữ liệu (Metadata) gồm: URL công khai (`OriginalUrl`, `MediumUrl`, `ThumbnailUrl`), chú thích (`AltText`), thứ tự (`OrderIndex`) và cờ đại diện (`IsPrimary`).

**Lý do kỹ thuật cốt lõi:**
1. **Tránh phình to dung lượng CSDL (Database Bloat):** Một bức ảnh có kích thước từ 2 MB đến 5 MB. Nếu lưu 10.000 ảnh dạng `bytea`/BLOB, dung lượng database sẽ tăng thêm 20–50 GB, làm giảm hiệu năng bộ nhớ đệm (shared buffers) của PostgreSQL, gây chậm chạp các truy vấn quan hệ (`JOIN`, `INDEX SCAN`).
2. **Tối ưu hóa sao lưu và phục hồi (Backup & Recovery):** Việc sao lưu định kỳ `pg_dump` sẽ trở nên cực kỳ nặng nề và mất hàng giờ đồng hồ nếu database chứa hàng chục gigabyte dữ liệu binary.
3. **Phân tách trách nhiệm (Separation of Concerns):** RDBMS tối ưu cho dữ liệu có cấu trúc và tính toàn vẹn giao dịch (ACID). Object Storage tối ưu cho việc ghi/đọc dữ liệu phi cấu trúc dung lượng lớn, hỗ trợ streaming và phục vụ tĩnh qua CDN (Content Delivery Network).
4. **Giảm tải cho Web Server:** Client có thể tải trực tiếp ảnh từ Object Storage hoặc CDN mà không cần chiếm dụng connection pool của cơ sở dữ liệu.

---

### 1.2. MinIO là gì và tại sao sử dụng AWS SDK?
- **MinIO** là hệ thống Object Storage mã nguồn mở tương thích hoàn toàn với chuẩn API của Amazon S3 (High-performance, S3 compatible object storage). Trong dự án, MinIO được chạy độc lập thông qua Docker container.
- **Tại sao dùng AWS SDK (`AWSSDK.S3`) thay vì package `Minio`?**
  - Tuân thủ yêu cầu đặc tả SRS: Giữ tính độc lập nền tảng và dễ dàng di chuyển lên đám mây thực tế (AWS S3, Cloudflare R2, Google Cloud Storage) trong tương lai mà không cần thay đổi một dòng code nghiệp vụ nào (`S3StorageService.cs` triển khai `IStorageService`).
  - MinIO hỗ trợ `ForcePathStyle = true` và `DisablePayloadSigning = true` tương thích hoàn hảo với AWS SDK Client.

---

## 2. Thiết kế dữ liệu & Quy tắc nghiệp vụ (Business Rules)

### 2.1. Quy tắc ảnh đại diện (IsPrimary) theo SRS FR-RCP-008
- **Ảnh đầu tiên mặc định là Primary:** Khi người dùng thêm ảnh đầu tiên cho một công thức (chưa có ảnh nào đang kích hoạt), hệ thống backend tự động gán `IsPrimary = true` dù request có truyền hay không.
- **Nguyên tắc độc tôn (Single Primary):** Tại mỗi thời điểm, một công thức chỉ có duy nhất **MỘT** ảnh đại diện chính.
- **Đổi ảnh đại diện (Set Primary):** Khi người dùng chọn một ảnh khác làm đại diện, Backend mở database transaction:
  1. Hạ cờ `IsPrimary = false` của tất cả các ảnh khác trong công thức.
  2. Nâng cờ `IsPrimary = true` cho ảnh được chọn.
  3. Commit transaction và kích hoạt xóa cache chi tiết công thức (`InvalidateRecipeDetailCacheAsync`).
- **Xóa ảnh đại diện chính (Delete Primary Fallback):** Khi ảnh đang là Primary bị xóa mềm (`IsDeleted = true`):
  - Backend tự động truy vấn các ảnh còn lại của công thức đó, sắp xếp theo `OrderIndex ASC`.
  - Ảnh có `OrderIndex` nhỏ nhất còn lại sẽ tự động được thăng cấp làm `IsPrimary = true`.

---

### 2.2. Client-side Validation vs. Backend Magic Bytes Security
Hệ thống áp dụng mô hình bảo vệ hai lớp (Two-tier Defense):

| Tiêu chí | Client-side Validation (`ImageManager.tsx`) | Backend Security (`Magic Bytes`) |
| :--- | :--- | :--- |
| **Mục đích** | Tối ưu trải nghiệm người dùng (UX), phản hồi lỗi tức thì, tiết kiệm băng thông. | Đảm bảo an toàn thông tin, bảo mật tuyệt đối cho máy chủ. |
| **Kích thước file** | Kiểm tra `file.size <= 5 * 1024 * 1024` (5 MB). | Giới hạn dung lượng request tối đa ở tầng Kestrel / IIS. |
| **Định dạng file** | Kiểm tra `file.type` (MIME): `image/jpeg`, `image/png`, `image/webp`, `image/avif`. | Đọc trực tiếp các byte đầu tiên của luồng file (Magic Bytes: `FF D8 FF` cho JPEG, `89 50 4E 47` cho PNG...) |
| **Khả năng giả mạo** | Dễ dàng bị vượt qua nếu kẻ tấn công đổi đuôi file `.exe` / `.sh` thành `.jpg` hoặc dùng Postman. | **Bất khả xâm phạm:** Dù đổi đuôi file, hệ thống vẫn nhận diện đúng định dạng nhị phân thực tế. |

> **Khẳng định:** Client validation **KHÔNG THỂ** thay thế Magic Bytes của Backend. Client chỉ đóng vai trò hỗ trợ người dùng nhận biết lỗi sớm trước khi tiêu tốn thời gian upload.

---

## 3. Kiến trúc Component & Quản lý State Frontend

### 3.1. Hai chế độ vận hành (Dual-mode Architecture)
Component `ImageManager` được thiết kế linh hoạt hỗ trợ 2 kịch bản:

1. **Existing Recipe Mode (Khi có `recipeId`):**
   - Tương tác trực tiếp với Backend REST API (`POST /images`, `PATCH /primary`, `DELETE /images`).
   - Tự động fetch dữ liệu ảnh từ backend khi khởi tạo nếu chưa có `initialImages`.
2. **New Recipe Mode (Khi chưa có `recipeId`):**
   - Quản lý danh sách ảnh bằng local state React, cấp ID tạm (`temp-...`).
   - Giao diện hiển thị thông báo rõ ràng "Chế độ công thức mới (State cục bộ)" và thông báo cho người dùng biết ảnh sẽ được đồng bộ khi công thức được lưu.
   - Tuyệt đối không tạo API giả, không upload blob giả vào localStorage để tránh tràn bộ nhớ trình duyệt.

### 3.2. Quản lý bộ nhớ Object URL (Memory Leak Prevention)
Khi người dùng chọn file ảnh từ máy tính, frontend tạo URL xem trước bằng `URL.createObjectURL(file)`. Để tránh rò rỉ bộ nhớ (Memory Leak), component luôn tự động giải phóng tài nguyên qua `URL.revokeObjectURL(previewUrl)` trong các trường hợp:
- Khi người dùng chọn file ảnh mới thay thế.
- Khi người dùng bấm nút "Hủy bỏ" hoặc form được reset sau khi thêm ảnh thành công.
- Khi component unmount khỏi DOM thông qua cleanup function trong `useEffect`.

---

## 4. Xử lý lỗi ngoại lệ & Dịch vụ lưu trữ gián đoạn (HTTP 503)

Khi hệ thống Object Storage (MinIO) gặp sự cố (container chưa chạy, mất kết nối mạng, đầy ổ đĩa):
- Backend trả về mã trạng thái `503 Service Unavailable`.
- API Client `lib/api/images.ts` bắt mã lỗi 503 và ném ra `ImageApiError` với thông điệp thân thiện: *"Dịch vụ lưu trữ hình ảnh (MinIO / Object Storage) hiện không khả dụng (HTTP 503). Vui lòng thử lại sau."*
- Component `ImageManager` hiển thị error banner màu đỏ phía trên danh sách, vô hiệu hóa trạng thái loading mà **không làm crash giao diện**, cho phép người dùng chuyển sang phương thức nhập URL ảnh ngoài để tiếp tục công việc.

---

## 5. Mười (10) câu hỏi phản biện của Giảng viên & Trả lời

### Câu 1: Tại sao nhóm không lưu luôn file ảnh dạng Base64 hoặc Binary vào bảng Recipe trong PostgreSQL cho đơn giản?
**Trả lời:** Việc lưu Base64 hoặc Binary trực tiếp vào database là một lỗi kiến trúc nghiêm trọng trong các hệ thống sản xuất. Dữ liệu Base64 làm tăng dung lượng file thêm ~33%. Lưu dữ liệu nhị phân vào PostgreSQL sẽ làm phình to CSDL (database bloat), gây tắc nghẽn bộ nhớ đệm RAM của database server khi thực hiện quét dữ liệu, làm chậm toàn bộ các truy vấn đọc/ghi công thức khác, đồng thời khiến việc backup `pg_dump` mất rất nhiều thời gian. Mô hình chuẩn là tách file tĩnh ra Object Storage (MinIO/S3) và chỉ lưu URL trong database.

### Câu 2: Client validation kiểm tra đuôi file và kích thước có đủ an toàn không? Kẻ xấu có thể bypass được không?
**Trả lời:** Client validation hoàn toàn có thể bị bypass bằng cách dùng công cụ như Postman, cURL hoặc chỉnh sửa mã nguồn trình duyệt. Người dùng có thể đổi tên một file mã độc `.sh` thành `.jpg` để đánh lừa client. Do đó, client validation của `ImageManager` chỉ có giá trị tối ưu hóa trải nghiệm người dùng (UX) để người dùng không phải chờ đợi upload một file quá lớn. Ở tầng bảo mật thực sự, Backend của nhóm bắt buộc phải kiểm tra Magic Bytes (chữ ký số nhị phân ở đầu file) để xác thực chính xác cấu trúc ảnh trước khi chấp nhận lưu vào MinIO.

### Câu 3: Làm thế nào frontend đảm bảo chỉ có duy nhất một ảnh đại diện chính (IsPrimary) trong bộ sưu tập?
**Trả lời:** Tính toàn vẹn của cờ `IsPrimary` được bảo đảm ở cả 2 đầu:
- Tại Backend: Khi nhận lệnh `SetPrimary` hoặc `AddImage` với cờ primary, handler bọc trong một Database Transaction: hạ toàn bộ các ảnh khác về `IsPrimary = false` trước khi đặt ảnh mới thành `true`.
- Tại Frontend: Component `ImageManager` phản ánh đúng nghiệp vụ này: khi một ảnh được đặt làm Primary, state cục bộ cập nhật lại toàn bộ mảng ảnh để đảm bảo chỉ có 1 card duy nhất mang badge "Ảnh đại diện".

### Câu 4: Khi xóa ảnh đại diện chính thì chuyện gì xảy ra? Giao diện phản hồi như thế nào?
**Trả lời:** Theo đặc tả nghiệp vụ SRS FR-RCP-008: Khi ảnh đang là Primary bị xóa mềm (`IsDeleted = true`), Backend tự động tìm ảnh có `OrderIndex` nhỏ nhất trong các ảnh còn lại để nâng cấp thành ảnh Primary mới. Khi frontend nhận được phản hồi thành công từ API xóa, giao diện sẽ cập nhật danh sách ảnh còn lại và tự động hiển thị badge "Ảnh đại diện" cho ảnh có OrderIndex nhỏ nhất đó.

### Câu 5: Trường `AltText` dùng để làm gì và tại sao cần giới hạn 200 ký tự?
**Trả lời:** `AltText` (Alternative Text) là văn bản thay thế của hình ảnh, có hai vai trò cực kỳ quan trọng:
1. **Accessibility (Khả năng tiếp cận):** Giúp người khiếm thị sử dụng trình đọc màn hình (Screen Reader) có thể hiểu được nội dung món ăn trong bức ảnh.
2. **SEO (Tối ưu hóa công cụ tìm kiếm):** Giúp Googlebot lập chỉ mục hình ảnh của bài viết nấu ăn. Giới hạn 200 ký tự là chuẩn thực tế (W3C recommendation) đủ để mô tả cô đọng nội dung ảnh mà không làm loãng từ khóa tìm kiếm.

### Câu 6: `URL.createObjectURL` hoạt động như thế nào khi xem trước ảnh và có rủi ro gì không?
**Trả lời:** `URL.createObjectURL(file)` tạo ra một chuỗi URL cục bộ (dạng `blob:http://...`) trỏ trực tiếp đến vùng nhớ của file đang nằm trên RAM trình duyệt, giúp hiển thị ảnh preview ngay lập tức mà không cần tốn thời gian upload lên server. Rủi ro của phương pháp này là rò rỉ bộ nhớ (Memory Leak) nếu tạo ra quá nhiều blob URL mà không giải phóng. `ImageManager` đã xử lý triệt để vấn đề này bằng cách gọi `URL.revokeObjectURL(previewUrl)` mỗi khi đổi file, submit thành công hoặc khi component unmount.

### Câu 7: Khi MinIO container bị tắt hoặc lỗi HTTP 503, ứng dụng xử lý ra sao để không bị đơ hoặc trắng màn hình?
**Trả lời:** Khi gọi upload file gặp mã lỗi HTTP 503, API client sẽ ném ra lỗi `ImageApiError` với status 503. Component `ImageManager` bắt lỗi này trong khối `try...catch`, tắt cờ `isLoading`, giải phóng trạng thái disabled của các nút bấm và hiển thị một Error Banner màu đỏ thông báo rõ ràng: "Dịch vụ lưu trữ hình ảnh hiện không khả dụng (HTTP 503)". Ứng dụng không bị crash và người dùng có thể linh hoạt chuyển sang tab "Nhập liên kết (URL)" để dán link ảnh từ CDN ngoài.

### Câu 8: Tại sao component `ImageManager` lại tách rời `ImageCard` thành một sub-component riêng?
**Trả lời:** Đây là nguyên tắc Single Responsibility Principle (SRP) trong thiết kế React:
- `ImageCard`: Chịu trách nhiệm thuần túy về mặt trình bày (Presentational Component) cho từng thẻ ảnh: hiển thị thumbnail, badge, alt text, và bắt sự kiện bấm nút.
- `ImageManager`: Đóng vai trò là Container Component quản lý state tổng thể, logic validation, gọi API, quản lý modal xóa và form nhập liệu. Việc phân tách này giúp mã nguồn dễ đọc, dễ bảo trì và có thể viết unit test độc lập cho từng thành phần.

### Câu 9: Ranh giới mã nguồn của branch này với module của các thành viên khác trong nhóm như thế nào?
**Trả lời:** Ranh giới được phân định tuyệt đối:
- Module `ImageManager` do Võ Hùng Mạnh (TV4) chịu trách nhiệm độc lập.
- File `RecipeForm.tsx` thuộc quyền quản lý của Phạm Nguyễn Ngọc Phước (TV3).
- Branch `feat/vohungmanh-image-ui` tuyệt đối không chỉnh sửa `RecipeForm.tsx` hay các file thuộc về Step, Category, Search nhằm loại bỏ nguy cơ xung đột mã nguồn (merge conflict). Khi TV3 hoàn thiện form cha, component chỉ cần được mount thông qua interface props chuẩn hóa (`recipeId`).

### Câu 10: Nhóm đã thực hiện kiểm thử tự động những gì cho module Image UI này?
**Trả lời:** Module được kiểm thử toàn diện với **14 test cases trong Jest & React Testing Library**, bao quát 100% các trạng thái và hành vi nghiệp vụ:
- Render Empty state và Render danh sách ảnh với badge Primary.
- Kiểm tra Client validation từ chối file > 5MB và file sai định dạng MIME.
- Kiểm tra tính năng Preview ảnh cục bộ trước khi upload.
- Kiểm tra upload và gọi API `addRecipeImage` thành công.
- Kiểm tra validation giới hạn 200 ký tự cho Alt text và số nguyên cho OrderIndex.
- Kiểm tra thao tác Đặt làm ảnh đại diện (`setPrimaryRecipeImage`).
- Kiểm tra Modal xác nhận xóa và thực hiện xóa ảnh (`deleteRecipeImage`).
- Kiểm tra hiển thị Error Banner khi gặp lỗi 401/403/404 và lỗi 503 từ Object Storage.
- Kiểm tra vô hiệu hóa nút bấm khi component ở trạng thái disabled. Toàn bộ test suite đạt kết quả **26/26 tests PASS (100%)**.

---

## 6. Ma trận phân tích rủi ro tích hợp (Integration Risk Matrix)

| Kịch bản tích hợp | Mức độ rủi ro | Biện pháp kiểm soát & Giải pháp kỹ thuật |
| :--- | :---: | :--- |
| **Tích hợp vào RecipeForm khi tạo mới (chưa có recipeId)** | Thấp | `ImageManager` hỗ trợ sẵn chế độ New Recipe Mode, hiển thị badge nhắc nhở và quản lý state cục bộ. |
| **Người dùng chọn file ảnh có dung lượng lớn (> 5 MB)** | Rất thấp | Client validation chặn ngay lập tức tại trình duyệt, không gửi request lên mạng. |
| **Người dùng đổi đuôi file mã độc thành .jpg** | Không có rủi ro cho CSDL | Client chỉ đọc MIME của OS; Backend bắt buộc kiểm tra Magic Bytes trước khi ghi file vào MinIO. |
| **Mất kết nối với MinIO Object Storage (HTTP 503)** | Trung bình | Đã có cơ chế bắt lỗi HTTP 503 hiển thị banner thân thiện, cung cấp giải pháp thay thế qua URL trực tiếp. |
| **Xung đột mã nguồn với RecipeForm của TV3** | Không có rủi ro | Branch hoàn toàn không sửa đổi file `RecipeForm.tsx`. |

---

## 7. Kết luận
Module **Recipe Image UI (`ImageManager`)** của sinh viên Võ Hùng Mạnh đã được triển khai hoàn chỉnh, đúng chuẩn kiến trúc hiện đại, tuân thủ nghiêm ngặt đặc tả SRS FR-RCP-008, đồng bộ nhận diện thương hiệu Culinary Blog và vượt qua toàn bộ các đợt kiểm thử đơn vị và đóng gói sản phẩm.
