# BÁO CÁO KỸ THUẬT: FRONTEND FOUNDATION & API CONTRACTS (TASK 5)

## 1. Thông tin sinh viên & Nhánh Git
- **Sinh viên thực hiện:** Võ Hùng Mạnh
- **MSSV:** 2312687
- **Lớp / Học phần:** Phát triển ứng dụng Web nâng cao (PTUDWNC) — Nhóm 12
- **Nhánh Git:** `feat/vohungmanh-task5-frontend-foundation`
- **Mục tiêu nhánh:** Thiết lập kiến trúc nền tảng giao diện người dùng (Front-end Foundation), cấu hình biến môi trường và chuẩn hóa toàn bộ các hợp đồng dữ liệu TypeScript (API Contracts) phục vụ giao tiếp đồng bộ giữa Backend ASP.NET Core và Frontend Next.js theo phân công Bữa 5.

---

## 2. Mục tiêu và phạm vi công việc Task 5
1. **Kiến trúc hợp đồng dữ liệu chuẩn hóa (API Envelope Contract):** Định nghĩa cấu trúc phong bì dữ liệu thống nhất `ApiResponse<T>`, `ApiError`, `PaginationParams`, `PaginatedResponse<T>` đảm bảo Client luôn unwrap dữ liệu an toàn.
2. **Hệ thống kiểu dữ liệu phân hệ Step, Image và Search:**
   - `types/step.ts`: Định nghĩa các kiểu `RecipeStep`, `CreateRecipeStepRequest`, `UpdateRecipeStepRequest`, `ReorderStepsRequest`.
   - `types/image.ts`: Định nghĩa các kiểu `RecipeImage`, `CreateRecipeImageRequest`, `UpdateRecipeImageRequest`, `SetPrimaryImageRequest`.
   - `types/search.ts`: Định nghĩa các kiểu truy vấn tìm kiếm `RecipeSearchQuery`, kết quả `RecipeSearchResult`, facet và bộ lọc.
3. **Cấu hình môi trường:** Xây dựng `.env.example` chuẩn cho Next.js với biến `NEXT_PUBLIC_API_URL` kết nối Backend API.

---

## 3. Chức năng triển khai thực tế & Bằng chứng mã nguồn

| STT | Chức năng thực tế | File + Type / Interface liên quan | Commit HEAD | Kiểm thử / Xác minh | Trạng thái |
|:---:|---|---|:---:|---|:---:|
| 1 | Envelope ApiResponse & Phân trang | `types/api.ts` (`ApiResponse<T>`, `PaginatedResponse<T>`) | `88128ec` | TypeScript compile check: khớp 100% envelope Backend | **PASS** |
| 2 | Contract RecipeStep (CRUD & Reorder) | `types/step.ts` (`RecipeStep`, `CreateRecipeStepRequest`) | `88128ec` | TypeScript compile check: khớp entity RecipeStep | **PASS** |
| 3 | Contract RecipeImage (Upload & Primary) | `types/image.ts` (`RecipeImage`, `CreateRecipeImageRequest`) | `88128ec` | TypeScript compile check: khớp entity RecipeImage | **PASS** |
| 4 | Contract RecipeSearch & Filters | `types/search.ts` (`RecipeSearchQuery`, `RecipeSearchResult`) | `88128ec` | TypeScript compile check: khớp DTO Search | **PASS** |
| 5 | Biến môi trường Next.js | `frontend/culinary-blog-web/.env.example` | `88128ec` | Kiểm tra file cấu hình API URL mẫu | **PASS** |

---

## 4. Danh sách file quan trọng

| STT | Đường dẫn file | Vai trò chính |
|:---:|---|---|
| 1 | `frontend/culinary-blog-web/types/api.ts` | Giao diện chuẩn cho phong bì dữ liệu API response, phân trang và lỗi |
| 2 | `frontend/culinary-blog-web/types/step.ts` | Giao diện kiểu dữ liệu cho bước thực hiện công thức |
| 3 | `frontend/culinary-blog-web/types/image.ts` | Giao diện kiểu dữ liệu cho hình ảnh công thức |
| 4 | `frontend/culinary-blog-web/types/search.ts` | Giao diện kiểu dữ liệu cho tìm kiếm và bộ lọc món ăn |
| 5 | `frontend/culinary-blog-web/.env.example` | Mẫu cấu hình biến môi trường kết nối API backend |

---

## 5. Hướng dẫn sử dụng & Kiểm thử
- Kiểm tra cú pháp và độ chính xác của TypeScript types:
  ```bash
  cd frontend/culinary-blog-web
  npx tsc --noEmit
  ```
