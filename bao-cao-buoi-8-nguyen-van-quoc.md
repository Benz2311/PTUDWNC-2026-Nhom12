# BÁO CÁO LAB - MÔN PHÁT TRIỂN ỨNG DỤNG WEB NÂNG CAO

**Lab:** 08  
**MSSV:** 2131719  
**Họ và tên:** Nguyễn Văn Quốc  
**Nhóm:** 01  

## Công việc

| STT | Công việc được giao | Liên kết đến GitHub branch | Tiến độ |
|---|---|---|---:|
| 1 | **Hoàn thiện Authentication và phân quyền User/Author/Admin**<br><br>**Đã hoàn thành:**<br>- Hoàn thiện đăng ký, đăng nhập, đăng xuất, refresh token và profile.<br>- Phân biệt quyền Guest, User, Author và Admin.<br>- Đăng ký mới mặc định role `User`.<br>- Giới hạn tạo, sửa, xóa công thức cho Author/Admin; User nhận lỗi 403.<br>- Bổ sung bảo vệ các route theo role.<br>- Thêm chức năng Admin cấp và thu hồi quyền Author; khi thu hồi vẫn giữ role User.<br>- Bổ sung menu và giao diện phù hợp theo từng vai trò.<br><br>**Chưa hoàn thành:**<br>- Chưa cấu hình SMTP thật nên email xác thực và email đặt lại mật khẩu chưa gửi được đến hộp thư thật. | [feat/NguyenVanQuoc_2131719_buoi8](https://github.com/Benz2311/PTUDWNC-2026-Nhom12/tree/feat/NguyenVanQuoc_2131719_buoi8) | 95% |
| 2 | **Hoàn thiện quản lý người dùng dành cho Admin**<br><br>**Đã hoàn thành:**<br>- Hiển thị danh sách User tại `/admin/users`.<br>- Tìm kiếm, lọc trạng thái và phân trang.<br>- Xem chi tiết User.<br>- Kích hoạt/vô hiệu hóa tài khoản.<br>- Không cho Admin tự vô hiệu hóa tài khoản của mình.<br>- Cấp/thu hồi role Author từ giao diện Admin.<br>- Xử lý các trường hợp lỗi 403, 404 và lỗi cập nhật.<br><br>**Chưa hoàn thành:**<br>- Chưa bổ sung chức năng quản lý role Admin trực tiếp trên giao diện. | [feat/NguyenVanQuoc_2131719_buoi8](https://github.com/Benz2311/PTUDWNC-2026-Nhom12/tree/feat/NguyenVanQuoc_2131719_buoi8) | 95% |
| 3 | **Hoàn thiện bảo mật mật khẩu và email**<br><br>**Đã hoàn thành:**<br>- Bổ sung hiển thị/ẩn mật khẩu bằng nút con mắt ở đăng nhập, đặt lại mật khẩu và đổi mật khẩu.<br>- Hoàn thiện kiểm tra mật khẩu hiện tại khi đổi mật khẩu.<br>- Không trả token reset mật khẩu hoặc token xác thực email trực tiếp qua API/UI.<br>- Phản hồi quên mật khẩu không làm lộ email có tồn tại hay không.<br><br>**Chưa hoàn thành:**<br>- Cần cấu hình SMTP hoặc mail catcher để kiểm thử đầy đủ email trong môi trường phát triển. | [feat/NguyenVanQuoc_2131719_buoi8](https://github.com/Benz2311/PTUDWNC-2026-Nhom12/tree/feat/NguyenVanQuoc_2131719_buoi8) | 90% |
| 4 | **Bổ sung tính năng logging để ghi lại nhật ký hệ thống**<br><br>**Đã hoàn thành:**<br>- Cấu hình Serilog để ghi nhật ký có cấu trúc cho backend.<br>- Ghi log request/response, thời gian xử lý và các request chậm.<br>- Bổ sung Correlation ID cho từng request để theo dõi xuyên suốt quá trình xử lý.<br>- Ghi nhận các sự kiện đăng ký, đăng nhập, đăng xuất, xác thực email và cập nhật profile.<br>- Ghi log cảnh báo lỗi phân quyền, giới hạn request và lỗi hệ thống.<br><br>**Chưa hoàn thành:**<br>- Chưa cấu hình hệ thống thu thập log tập trung cho môi trường production. | [feat/NguyenVanQuoc_2131719_buoi8](https://github.com/Benz2311/PTUDWNC-2026-Nhom12/tree/feat/NguyenVanQuoc_2131719_buoi8) | 95% |

## Tự đánh giá

Đã hoàn thiện phần Authentication, Profile, phân quyền và quản lý User theo phạm vi được giao. Các chức năng chính đã được kiểm tra bằng frontend lint, frontend build và backend build. Phần cần bổ sung là cấu hình SMTP an toàn để kiểm thử gửi email xác thực và đặt lại mật khẩu trong môi trường thật.
