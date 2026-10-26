# SuDia.Web — giao diện ASP.NET Core MVC

## Chạy
1. Mở thư mục `SuDia.Web` bằng JetBrains Rider (hoặc `dotnet run` trong thư mục này).
2. Mở https://localhost:7180

Cần .NET 8 SDK trở lên. Muốn dùng .NET 10 thì đổi `TargetFramework` trong `SuDia.Web.csproj` thành `net10.0`.

## Cấu trúc (chuẩn MVC)
| Đường dẫn | Nội dung |
| --- | --- |
| `Models/ViewModels.cs` | ViewModel cho 4 trang + record con (BuocHoc, MonHoc, GoiHoc, KhoaCuaToi, PhuongAn…) và lớp `DinhDang` (giá vi-VN, ngày, số La Mã) |
| `Services/IKhoaHocService.cs` | Giao diện lấy dữ liệu các trang |
| `Services/DuLieuMauService.cs` | Dữ liệu mẫu — sau này thay bằng service đọc từ EF Core + PostgreSQL |
| `Controllers/HomeController.cs` | Index, TongQuan, KhoaHoc(id), BaiHoc(id), LamCauHoi(id) [GET] và LamCauHoi(id, dapAn) [POST, chấm ở server] |
| `Views/Shared/_MenuTaiKhoan.cshtml` | Menu trái dùng chung cho các trang tài khoản (Tổng quan, Khoá của tôi) |
| `Controllers/TroLyController.cs` | API `POST /api/tro-ly/hoi` cho khung "Học cùng AI" (chống CSRF, giới hạn 10 câu/phút) |
| `Services/IAiService.cs`, `DemoAiService.cs` | Giao diện trợ lý + bản xem thử (dùng khi chưa có khoá API) |
| `Services/RagAiService.cs` | Trợ lý thật: viết lại câu hỏi nối tiếp → tìm đoạn → gọi Claude → kiểm tra số liệu → AI chấm |
| `Services/TroLy/` | `KhoTaiLieu` (cắt đoạn theo Mục, tìm kiếm không dấu), `PromptTroLy` (đọc prompt từ file .md), `KiemTraCauTraLoi`, `BoKiemThu`, `TroLyOptions` |
| `TaiLieu/*.md` | Tài liệu khoá (hiện là tài liệu mẫu) — soạn theo Phần 5 của file prompt |
| `KiemThu/bo-cau-hoi.json` | Bộ câu hỏi kiểm thử (Phần 6) |
| `Views/Home/*.cshtml` | View Razor: `@model`, `@foreach`, `@if` sinh giao diện từ Model |
| `Views/Shared/_Layout.cshtml` | Khung chung (section Head/Scripts) |
| `wwwroot/css/*.css` | CSS riêng từng trang (hiệu ứng mở trang, nền sao, đung đưa…) |
| `wwwroot/js/trang-chu.js` | Khung chat trang chủ, gọi API trợ lý |
| `wwwroot/images/hero.jpg` | Ảnh nền đầu trang |
| `Prompts/prompt-he-thong-tro-ly.md` | Prompt hệ thống cho AI |

## Luồng học của học sinh
`/Home/KhoaHoc` (chỉ các ô khoá, có ảnh bìa) → bấm khoá → `/Home/KhoaHoc/lich-su-12` (trang chi tiết khoá: ảnh, thông tin, tiến độ, bài theo chủ đề) → **Chi tiết** → `/Home/BaiHoc/{mã bài}`.
Làm đề: `/Home/LamCauHoi/{mã bài | mã đề}` hiện toàn bộ câu, chưa có đáp án; bấm **Nộp bài** → server chấm, mới hiện điểm, đáp án, lời giải và link về mục lý thuyết. Đề thi thử có đồng hồ, hết giờ tự nộp. Câu hỏi mẫu: `NganHangMau` trong `DuLieuMauService` (Bài 6, Bài 7).
Luyện đề: `/Home/LuyenDe` (ô khoá) → `/Home/LuyenDe/lich-su-12` (đề thi thử + bài tập theo chủ đề, mỗi bài có nút Ôn lý thuyết → `/Home/BaiHoc/{mã bài}`) → `/Home/LamCauHoi/{mã bài | mã đề}`. Trang khoá có nút Luyện đề sang đúng khoá; trang làm câu hỏi quay về luyện đề của khoá.
Ảnh bìa khoá: `wwwroot/images/khoa/{mã khoá}.jpg` (16:9, khuyên dùng 960×540) — thay ảnh thật cùng tên là xong.
Mã bài không trùng giữa các khoá (mẫu: Lịch sử = số bài, Địa lí = 100 + số bài). Khoá chưa mua chỉ học thử bài đầu, các bài khác hiện khoá.

## Luồng dữ liệu
Trình duyệt → `HomeController` → `IKhoaHocService` trả ViewModel → View Razor vẽ HTML.
Đổi nguồn dữ liệu (database) chỉ cần viết service mới và đổi một dòng đăng ký trong `Program.cs`; View giữ nguyên.

## Trợ lý AI
1. Đặt khoá API (không ghi vào appsettings.json):
   `dotnet user-secrets init && dotnet user-secrets set ANTHROPIC_API_KEY sk-ant-...`
   (hoặc biến môi trường `ANTHROPIC_API_KEY`). Chưa có khoá thì web tự dùng bản xem thử.
2. Tài liệu khoá: thả file `.md` vào `TaiLieu/` theo cấu trúc `#` Chương / `##` Bài / `###` Mục, đầu file có `khoa:` và `mon:`. Mỗi `###` là một đoạn để tìm kiếm và ghi nguồn.
3. Prompt: sửa trong `Prompts/prompt-he-thong-tro-ly.md` (Phần 1 và Phần 4 được đọc trực tiếp), khởi động lại web.
4. Cấu hình ở mục `TroLy` trong `appsettings.json`: model trả lời (`claude-opus-5-5`), model phụ cho viết lại câu hỏi + AI chấm (`claude-haiku-5-5`), số đoạn, mức suy nghĩ, bật/tắt AI chấm.
5. Kiểm thử: chạy ở môi trường Development rồi mở `GET /api/tro-ly/kiem-thu` (mỗi lần chạy tốn lượt gọi API). Mục tiêu: trả lời đúng ≥ 90%, từ chối đúng ≥ 95%.
6. Câu trả lời không qua kiểm tra bị thay bằng "Nội dung này chưa có trong tài liệu khoá học." và ghi log cảnh báo `Trợ lý KHONG_DAT` để Admin xem.

## Ghi chú
- Trong file `.cshtml`, ký tự `@` thường phải viết `@@` (Razor). CSS/JS đã tách ra `wwwroot` nên không bị ảnh hưởng.
- Trang Tổng quan, Bài học, Làm câu hỏi vẫn theo phong cách cũ (xanh ngọc); trang chủ theo phong cách mới (đen ấm, đồng).
- Bước tiếp theo: Identity + phân quyền theo gói, database; chuyển `KhoTaiLieu` sang pgvector + full-text khi tài liệu nhiều; nối khung trợ lý ở trang Bài học vào API.
