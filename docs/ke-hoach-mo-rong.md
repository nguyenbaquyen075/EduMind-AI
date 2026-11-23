# Kế hoạch mở rộng hệ thống (4 mảng)

Mỗi mảng làm trên một nhánh riêng, có migration, kiểm thử trong `KiemThu/`, rồi gộp vào `dev`.

| # | Mảng | Nhánh | Nội dung chính |
| --- | --- | --- | --- |
| 1 | Lớp học + diễn đàn hỏi đáp | `feat/lop-hoc-dien-dan` | Bảng Lop, LopHocVien, BaiDang, TraLoiBaiDang. Giảng viên tạo lớp, học viên vào lớp bằng mã; diễn đàn đăng câu hỏi, trả lời, ghim, đánh dấu đáp án, đóng bài. |
| 2 | Thống kê, biểu đồ và thông báo | `feat/thong-ke-thong-bao` | Thông báo trong web (chuông) cho trả lời mới, bài giao, sắp hết hạn; biểu đồ tiến độ, điểm theo thời gian, phổ điểm; email nhắc hạn nộp. |
| 3 | AI phân tích điểm yếu | `feat/ai-diem-yeu` | Thống kê câu sai theo chủ đề; AI sinh đề ôn theo điểm yếu; AI nhận xét bài tự luận theo tiêu chí; báo cáo mạnh/yếu từng học viên cho giảng viên. |
| 4 | Nhiều môn, ngân hàng đề, chứng chỉ | `feat/ngan-hang-de-chung-chi` | Ngân hàng đề dùng chung có độ khó, sinh đề ngẫu nhiên theo ma trận; chứng nhận hoàn thành khoá (PDF); hướng dẫn thêm môn mới. |

Thứ tự 1 → 2 → 3 → 4 vì thông báo (2) cần diễn đàn (1), và báo cáo điểm yếu (3) dùng biểu đồ của (2).
