# Kế hoạch mở rộng hệ thống (4 mảng)

Mỗi mảng làm trên một nhánh riêng, có migration, kiểm thử trong `KiemThu/`, rồi gộp vào `dev`.

| # | Mảng | Nhánh | Nội dung chính |
| --- | --- | --- | --- |
| 1 | Lớp học + diễn đàn hỏi đáp | `feat/lop-hoc-dien-dan` | Bảng Lop, LopHocVien, BaiDang, TraLoiBaiDang. Giảng viên tạo lớp, học viên vào lớp bằng mã; diễn đàn đăng câu hỏi, trả lời, ghim, đánh dấu đáp án, đóng bài. |
| 2 | Thống kê, biểu đồ và thông báo | `feat/thong-ke-thong-bao` | Thông báo trong web (chuông) cho trả lời mới, bài giao, sắp hết hạn; biểu đồ tiến độ, điểm theo thời gian, phổ điểm; email nhắc hạn nộp. |
| 3 | AI phân tích điểm yếu | `feat/ai-diem-yeu` | Thống kê câu sai theo chủ đề; AI sinh đề ôn theo điểm yếu; AI nhận xét bài tự luận theo tiêu chí; báo cáo mạnh/yếu từng học viên cho giảng viên. |
| 4 | Nhiều môn, ngân hàng đề, chứng chỉ | `feat/ngan-hang-de-chung-chi` | Ngân hàng đề dùng chung có độ khó, sinh đề ngẫu nhiên theo ma trận; chứng nhận hoàn thành khoá (PDF); hướng dẫn thêm môn mới. |

Thứ tự 1 → 2 → 3 → 4 vì thông báo (2) cần diễn đàn (1), và báo cáo điểm yếu (3) dùng biểu đồ của (2).

## Trạng thái

| # | Mảng | Trạng thái |
| --- | --- | --- |
| 1 | Lớp học + diễn đàn | Xong, kiểm thử `kiem-tra-lop-hoc.py` |
| 2 | Thống kê, biểu đồ, thông báo | Xong, kiểm thử `kiem-tra-thong-bao.py` |
| 3 | AI phân tích điểm yếu | Xong phần thống kê, đề ôn riêng, báo cáo giảng viên (`kiem-tra-diem-yeu.py`). Hai tính năng gọi Gemini (AI tạo câu hỏi, AI gợi ý chấm tự luận) đã kiểm tra phần kiểm định bằng code và phần phân quyền; chưa thấy kết quả Gemini thật vì dịch vụ quá tải (503) lúc thử |
| 4 | Ngân hàng đề, chứng chỉ, nhiều môn | Xong: độ khó câu hỏi, ra đề theo ma trận, chứng chỉ (bản in A4 + xác thực công khai), hướng dẫn thêm môn ([them-mon-hoc-moi.md](them-mon-hoc-moi.md)); kiểm thử `kiem-tra-ngan-hang-chung-chi.py`, `kiem-tra-mon-moi.py` |

## Mảng 3: các phần chính

- `PhanTichDiemYeuService`: tỉ lệ đúng theo bài từ `CauTraLoi` (120 ngày gần nhất, cần từ 5 câu mới kết luận: dưới 60% là yếu, từ 80% là mạnh), đề ôn riêng 10 câu từ 3 bài yếu nhất (câu sai trước, chưa làm sau), bài cả lớp hay sai (cần từ 10 câu).
- `AiHocTapService` dùng chung đường gọi Gemini của trợ lý: tạo 5 câu trắc nghiệm từ nội dung bài (số liệu trong câu hỏi, đáp án đúng, lời giải phải có trong bài, phương án được xáo, không lưu), và gợi ý nhận xét + điểm cho câu tự luận (điểm bị chặn trong khoảng 0 đến điểm tối đa, bài làm của học sinh bị coi là dữ liệu, giảng viên quyết định điểm).
- Học viên: `/DiemYeu`, `/DiemYeu/DeOn`. Giảng viên: bảng mạnh/yếu trong chi tiết học viên, "Bài cả lớp hay sai" ở trang Học viên & điểm, nút "AI gợi ý chấm" ở trang bài nộp.
