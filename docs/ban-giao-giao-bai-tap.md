# Giao bài tập theo buổi — 26/10/2026

Trên nhánh `feat/nen-tang`, đã tách hai luồng theo yêu cầu:

- **Thêm câu hỏi**: thêm từng câu luyện tập vào bài học, giữ nguyên chức năng cũ và mục menu riêng.
- **Giao bài tập**: menu ⋮ của mỗi bài trong Bài tập mở trình soạn một bài tập riêng cho buổi học.

## Luồng giao bài

1. Thông tin chung: tên bài tập, bài học, buổi dạy online (không bắt buộc), hạn nộp, hướng dẫn.
2. Soạn câu hỏi: 3 phần theo ảnh tham chiếu — trắc nghiệm A/B/C/D, đúng / sai 4 ý, tự luận. Thêm / xoá / đổi thứ tự nhiều câu, đặt điểm từng câu hoặc ý, tổng điểm cập nhật trực tiếp. Có thể bỏ phần không dùng.
3. Xem lại & xuất bản: hiển thị câu hỏi, đáp án, tổng điểm và thông tin giao bài trước khi lưu. Lưu nháp được cả khi nội dung chưa hoàn chỉnh, xuất bản yêu cầu đủ nội dung và đáp án.

Mỗi bài học có nhiều bài tập được giao, hiện riêng với câu luyện tập của bài. Các bài tập được lưu trong bảng `BaiTapGiao`; bài nộp ở `BaiNopGiao`. Migration `GiaoBaiTapTheoBuoi` chỉ thêm hai bảng và các khoá liên quan.

Học viên mở bài tập từ menu ⋮ trong chi tiết khoá → Bài tập được giao theo buổi. Chỉ học viên có quyền học bài, bài đã xuất bản và bài tập đã giao mới xem / nộp được. Đáp án và lời giải không có trong HTML trước khi nộp. Mỗi học viên nộp một lần, không nhận bài nộp sau hạn.

Trắc nghiệm và từng ý đúng / sai được chấm phía server. Tự luận chờ giảng viên chấm tại Bài nộp; điểm cuối cùng chỉ hiện khi phần này đã chấm. Giảng viên xem trước / nộp thử không tạo bài nộp. Bài tập đã có bài nộp bị chặn sửa / xoá để giữ nguyên kết quả.

## Kiểm tra

- Build .NET thành công, không lỗi hoặc cảnh báo; JavaScript qua kiểm tra cú pháp.
- Chrome với hồ sơ thử riêng: kiểm tra menu ⋮ và Thêm câu hỏi, 3 phần soạn, thêm / xoá / đổi thứ tự câu, xem lại, lưu nháp trên giao diện và mở lại dữ liệu.
- Đã chụp và xem ảnh desktop 1280px, mobile 390px; mobile không tràn ngang và không có lỗi JavaScript.
- Kiểm tra HTTP với PostgreSQL: lưu nháp / học viên không thấy nháp; xuất bản; kiểm tra đủ 3 loại câu; không lộ lời giải trước nộp; nộp bài chấm 2 điểm tự động, chờ tự luận; giảng viên chấm 1 điểm tự luận, học viên thấy 3/3.
- Chặn thiếu đáp án, điểm âm, buổi học sai, bài không tồn tại, thiếu CSRF, sửa bài đã có bài nộp và truy cập sai vai trò.
- Dữ liệu kiểm tra có mã riêng, đã xoá sau khi thử. Bộ kiểm tra lặp lại: `KiemThu/kiem-tra-giao-bai-theo-buoi.py`.
