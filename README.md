# Phát triển ứng dụng web nâng cao 
# ##  Nhóm 12 thành viên gồm :
1. Võ Hùng Mạnh - 2312687 - 2312687@dlu.edu.vn ( Nhóm trưởng )
2. Lê Thị Ánh Nhung - 2312709 - 2312709@dlu.edu.vn
3. Phạm Nguyễn Ngọc Phước - 2312718 - 2312718@dlu.edu.vn
4. Nguyễn Văn Quốc - 2312729 - 2312729@dlu.edu.vn
# ## Quy tắc làm việc
1. Không push trực tiếp code chức năng lên `main`.
2. Luôn pull code mới nhất trước khi bắt đầu làm việc.
3. Mỗi task thực hiện trên branch riêng.
4. Không tự ý thay đổi cấu trúc project chung.
5. Không commit file `.env` chứa thông tin bí mật.
6. Commit message phải mô tả rõ thay đổi.
7. Kiểm tra code trước khi tạo Pull Request.
8. Khi push lên nhớ tạo nhánh con từ nhánh cha ( nhớ ghi chức năng mình làm )

### Bữa 1 - Xây dựng cấu trúc dữ liệu ban đầu

**Mục tiêu:** Xác định các thành phần dữ liệu chính, thiết lập quan hệ và chuẩn bị dữ liệu nền cho hệ thống.

#### Nguyễn Văn Quốc - User/Auth
- Rà soát yêu cầu về người dùng, tài khoản và xác thực.
- Hoàn thiện `ApplicationUser` và thông tin cơ bản của người dùng.
- Thiết lập `RefreshToken` phục vụ quá trình xác thực tài khoản.
- Hoàn chỉnh liên kết giữa người dùng và công thức.
- Chuẩn bị dữ liệu mẫu phục vụ kiểm thử người dùng và tài khoản.


#### Lê Thị Ánh Nhung - Category & Statistics
- Xác định dữ liệu liên quan đến danh mục và khai thác công thức.
- Hoàn thiện `Category` và các thông tin cần thiết.
- Thiết lập liên kết giữa `Category` và `Recipe`.
- Chuẩn bị dữ liệu phục vụ lọc, sắp xếp, phân trang và thống kê.
- Chuẩn bị dữ liệu mẫu danh mục phục vụ truy vấn và thống kê.

#### Phạm Nguyễn Ngọc Phước - Recipe & Ingredient
- Xác định cấu trúc và thông tin chính của công thức.
- Hoàn thiện `Recipe` và các thuộc tính chính.
- Thiết lập `RecipeIngredient` và cấu hình `RecipeNutrition`.
- Hoàn chỉnh liên kết giữa `Recipe`, User và `Category`.
- Chuẩn bị dữ liệu mẫu cho công thức, dinh dưỡng và nguyên liệu.

####  Võ Hùng Mạnh - Step & Image
- Xác định dữ liệu về các bước chế biến và hình ảnh.
- Hoàn thiện `RecipeStep` và thứ tự các bước thực hiện.
- Thiết lập `RecipeImage` và thông tin hiển thị hình ảnh.
- Hoàn chỉnh liên kết `RecipeStep`, `RecipeImage` với `Recipe`.
- Chuẩn bị dữ liệu mẫu cho bước nấu và hình ảnh.