# Mở rộng khu quản trị — 27/10/2026

Khu admin có 9 mục hoạt động, dùng dữ liệu PostgreSQL hiện có; không thêm bảng mới.

| Mục | Chức năng |
| --- | --- |
| Tổng quan | 8 chỉ số, biểu đồ hoạt động 7 ngày, việc cần xử lý, tình hình khoá, bài nộp gần đây, buổi sắp tới và lối tắt thao tác |
| Người dùng | Tìm / lọc vai trò và chưa ghi danh; tạo, khoá / mở, đổi vai trò; chi tiết hồ sơ, khoá, luyện tập, bài nộp; sửa họ tên và email |
| Khoá học | Tạo khoá, kiểm tra mã trùng, sửa thông tin, gán giảng viên hợp lệ, mở công cụ nội dung / ghi danh |
| Ghi danh & hạn học | Danh sách toàn hệ thống, tìm học viên, lọc khoá và còn / sắp / hết hạn, phân trang, ghi danh bằng email, mở quản lý gia hạn |
| Nội dung bài học | Lọc bài theo khoá, tên / chủ đề, nháp / xuất bản / thiếu nội dung; thống kê mục, câu hỏi, bài tập; mở trình soạn và quản lý bài tập |
| Bài tập & bài nộp | Lọc bài tập theo khoá, tên, nháp / đã giao / chờ chấm; xem số bài nộp; mở xem / chấm tự luận, nội dung bài tập và đề thi thử |
| Lịch dạy | Xem lịch mọi khoá, lọc sắp tới / đã qua / chưa đặt giờ; tạo / sửa buổi, vào lớp, giao bài |
| Báo cáo học tập | Lọc khoảng tối đa 90 ngày và khoá; học viên hoạt động, luyện tập, bài tập nộp, tỉ lệ đúng, biểu đồ / bảng theo ngày; xuất CSV |
| Trợ lý AI & tài liệu | Xem chế độ demo / đã cấu hình, giới hạn câu hỏi, cấu hình hiện tại, tài liệu nguồn và bộ kiểm thử; mở trang chat |

Dashboard và báo cáo tính ngày theo giờ Việt Nam. Các số liệu nội dung / ghi danh hiện tại được ghi rõ để phân biệt với số bài nộp trong khoảng báo cáo. CSV có UTF-8 BOM và bảo vệ ô văn bản bắt đầu bằng ký tự công thức.

Admin dùng lại trình soạn, giao bài, lịch dạy và chấm bài hiện có ở khu giảng viên (Admin được quyền quản lý mọi khoá). Không tạo thêm quy trình soạn trùng lặp.

Đổi email người dùng sẽ vô hiệu phiên cũ. Không thay đổi cơ chế tự bảo vệ: admin không tự khoá hoặc đổi vai trò chính mình. Các trang và thao tác mới đều yêu cầu vai trò Admin; người học / giảng viên không truy cập được.

## Giới hạn phạm vi

Trợ lý AI hiện hiển thị cấu hình và tài liệu; chưa có chỉnh cấu hình trên giao diện hoặc nhật ký hội thoại lưu database. Chưa triển khai gói học / giá, thanh toán / đơn hàng, chỉnh nội dung trang chủ. Các phần này vẫn nằm trong kế hoạch chung.

## Kiểm tra

- Build .NET thành công, không cảnh báo / lỗi.
- HTTP: 9 trang, bộ lọc và phân trang; tạo / sửa tài khoản; tạo khoá với trường tuỳ chọn để trống; chặn mã trùng và giảng viên sai; ghi danh / gia hạn và email không phân biệt hoa thường; nội dung thiếu; lịch dạy; bài nộp chờ chấm trên dashboard / hồ sơ / danh sách; số liệu CSV; ngày sai và phân quyền.
- Chrome thử riêng: điều hướng 9 mục, mục đang chọn, bộ lọc báo cáo thay đổi biểu đồ, desktop 1440px / mobile 390px. Không có lỗi JavaScript hoặc tràn ngang trang. Đã chụp và kiểm tra ảnh.
- Dữ liệu thử dùng mã riêng, được xoá sau kiểm tra.

Bộ kiểm tra lặp lại: `python3 KiemThu/kiem-tra-admin.py`, cần web Development ở cổng 5280, PostgreSQL có dữ liệu mẫu và `psql`. Tuỳ chỉnh bằng `SUDIA_TEST_URL`, `SUDIA_TEST_PASSWORD`, `SUDIA_TEST_DATABASE`. Chỉ chạy trên web cục bộ.
