# Thêm một môn học mới

Hệ thống không gắn cứng vào Lịch sử và Địa lí: mỗi môn là một hoặc nhiều **khoá học**, trường `Mon` chỉ là nhãn chữ. Kiểm thử `KiemThu/kiem-tra-mon-moi.py` tạo thử một khoá "Giáo dục công dân" và dùng đủ lớp học, ma trận đề, nội dung.

## Các bước

1. **Tạo khoá** (Admin › Khoá học › Tạo khoá): nhập mã khoá (chữ thường, số, gạch nối), tên, môn (gõ tên môn mới được, danh sách chỉ là gợi ý), ký hiệu 1–3 chữ, gán giảng viên, giá nếu bán lẻ.
2. **Soạn nội dung** (Giảng viên › Khoá học của tôi): tạo chủ đề và bài; nhập từ văn bản, Word hoặc PowerPoint rồi chia mục bằng AI hoặc theo đề mục.
3. **Soạn câu hỏi** (Giảng viên › Bài tập): mỗi câu gắn với bài, chọn độ khó dễ / trung bình / khó.
4. **Ra đề** (Giảng viên › Đề thi thử): chọn tay, hoặc "Đề theo ma trận" để hệ thống bốc ngẫu nhiên theo số câu dễ / trung bình / khó của từng bài.
5. **Mở lớp** (Giảng viên › Lớp học & diễn đàn): tạo lớp, gửi mã cho học viên.
6. **Kho tài liệu** (Giảng viên › Kho tài liệu): tải tệp để học viên tìm và trợ lý AI đọc.

Nội dung giảng viên nhập tự vào chỉ mục của trợ lý AI (có thể mất vài giây), không cần cấu hình thêm.

## Điều cần chỉnh tay (không thuộc dữ liệu)

- **Trang chủ** (`Views/Home/Index.cshtml`) đang nêu hai môn đầu bằng cách tìm khoá tên bắt đầu bằng "Lịch sử" / "Địa lí"; môn mới vẫn hiện trong danh sách khoá và trang học viên, chỉ không có khối giới thiệu riêng ở trang chủ.
- **Lời nhắc của trợ lý** (`Prompts/prompt-he-thong-tro-ly.md`) nói về Lịch sử và Địa lí: sửa câu mở đầu và phần "Phạm vi" để trợ lý nhận môn mới là trong phạm vi, nếu không nó sẽ từ chối câu hỏi của môn khác.
- **Dữ liệu mẫu** (`Data/KhoiTaoDuLieu.cs`) chỉ tạo hai khoá mẫu; khoá thật tạo qua Admin.
