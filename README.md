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
| `Services/EfKhoaHocService.cs` | Dữ liệu trang học viên đọc từ database theo người đăng nhập (tiến độ, ghi danh, bài làm) |
| `Services/DuLieuMauService.cs` | Dữ liệu mẫu — nguồn nạp database lần đầu, nội dung trang chủ |
| `Data/` | `ThucThe.cs` (bảng), `AppDbContext.cs`, `KhoiTaoDuLieu.cs` (migration + nạp mẫu + tài khoản dùng thử), `Migrations/` |
| `Controllers/TaiKhoanController.cs` | Đăng nhập / đăng ký / đăng xuất, đưa về trang theo vai trò |
| `Areas/GiangVien`, `Areas/Admin` | Trang giảng viên (`/GiangVien`) và quản trị (`/Admin`) |
| `Controllers/HomeController.cs` | Index, TongQuan, KhoaHoc(id), BaiHoc(id), LamCauHoi(id) [GET] và LamCauHoi(id, dapAn) [POST, chấm ở server] |
| `Views/Shared/_MenuTaiKhoan.cshtml` | Menu trái dùng chung cho các trang tài khoản (Tổng quan, Khoá của tôi) |
| `Controllers/TroLyController.cs` | API `POST /api/tro-ly/hoi` cho khung "Học cùng AI" (chống CSRF, giới hạn 10 câu/phút) |
| `Views/Home/HoiAi.cshtml`, `wwwroot/js/hoi-ai.js`, `wwwroot/css/hoi-ai.css` | Trang chat riêng `/Home/HoiAi` từ menu học viên; chọn môn, gửi câu hỏi, nguồn, 3 lượt hội thoại gần nhất, thử lại và chat mới |
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

## Database & tài khoản
- PostgreSQL. Chuỗi kết nối `ConnectionStrings:SuDia` (dev: `appsettings.Development.json` → database `sudia_dev` trên máy).
  Sao chép `appsettings.Development.example.json` thành `appsettings.Development.json`, thay `YOUR_POSTGRES_USER` bằng tài khoản PostgreSQL của bạn. Cấu hình Development cá nhân không đưa lên Git.
  Tạo database lần đầu: `createdb sudia_dev`. Khi chạy, web tự cập nhật bảng (migration) và nạp nội dung mẫu nếu database trống.
- Thêm bảng / sửa mô hình: sửa `Data/ThucThe.cs` rồi `dotnet ef migrations add <Ten> -o Data/Migrations`.
- Vai trò: Học viên, Giảng viên, Admin (ASP.NET Core Identity). Đăng ký mới là học viên, chưa ghi danh (học thử bài đầu).
- Tài khoản dùng thử (chỉ tạo khi chạy Development), mật khẩu ở `TaiKhoanMau:MatKhau`:
  `admin@sudia.vn` → `/Admin` · `giangvien@sudia.vn` → `/GiangVien` · `hocvien@sudia.vn` → trang học viên (đã ghi danh Lịch sử 12).
- Kế hoạch trang giảng viên và admin: `docs/ke-hoach-giang-vien-admin.md`.

## Trang Giảng viên (`/GiangVien`) — soạn cho học viên
| Trang | Làm gì |
| --- | --- |
| Bàn làm việc | Việc cần làm (bài chưa có nội dung / câu hỏi), buổi dạy sắp tới, bài vừa nộp, câu hay sai |
| Nội dung bài học | Thêm chủ đề, bài (nháp → xuất bản, học thử); soạn mục và phần: đoạn văn, mốc thời gian, ghi nhớ |
| Ngân hàng câu hỏi | Thêm / sửa / xoá câu: 4 phương án, đáp án, giải thích, gắn bài · mục; xem tỉ lệ học viên làm đúng |
| Đề thi thử | Chọn câu từ ngân hàng, đặt phút, mở / đóng đề |
| Lịch dạy online | Buổi học theo bài: link Google Meet + giờ (học viên mở ở menu ⋮ "Học online") |
| Học viên | Tiến độ, tỉ lệ đúng, bài đã nộp và câu làm sai của từng học viên |

Giảng viên chỉ thấy và sửa khoá được gán; Admin sửa mọi khoá. "Xem như học viên" mở toàn bộ bài, không ghi tiến độ.

Giao bài tập theo buổi: **Bài tập → menu ⋮ của bài → Giao bài tập**. Khung soạn gồm
3 bước: thông tin chung (tên bài tập, bài học, buổi online nếu có, hạn nộp), soạn câu hỏi,
xem lại & xuất bản. Một bài tập có nhiều câu **trắc nghiệm**, **đúng / sai** (4 ý, điểm từng ý),
**tự luận**, có nút thêm / xoá / đổi thứ tự câu và tính tổng điểm. Lưu nháp để soạn tiếp;
giao bài xong học viên mở ở menu ⋮ của bài học → **Bài tập được giao theo buổi**.
Trắc nghiệm và đúng / sai chấm tự động; tự luận chấm tại **Bài nộp** của giảng viên.
Bài đã có học viên nộp được giữ nguyên nội dung. Học viên chỉ thấy đáp án và lời giải sau khi nộp.

**Bài tập và kiểm tra dùng chung form 3 bước**: Thông tin chung → Soạn câu hỏi → Xem lại & xuất bản.
Cả hai đều có Trắc nghiệm, Đúng/Sai và Tự luận; có thể thêm, xoá và sắp xếp câu trong form.
Đã bỏ menu riêng **Thêm câu hỏi**; các đường dẫn thêm câu cũ chuyển về form soạn bài tập.
Kiểm tra có thời gian 5–240 phút, chỉ bắt đầu khi học viên bấm **Bắt đầu làm bài**; thời gian bắt đầu lưu ở server,
tải lại không đặt lại đồng hồ. Hết giờ trang tự nộp; server không nhận đáp án gửi quá thời gian kèm 10 giây truyền bài.
Trắc nghiệm/Đúng-Sai chấm tự động, Tự luận chấm tại **Bài nộp & chấm điểm**.
Đề cũ đã có bài làm và đề mới đã có lượt bắt đầu được giữ nguyên nội dung. Đề cũ chưa có bài làm mở bằng form chung;
khi lưu sẽ tạo bộ câu riêng, không sửa câu ngân hàng đang dùng ở nơi khác.

Kiểm tra luồng giao bài (web Development đang chạy, PostgreSQL có dữ liệu mẫu, Python 3 và `psql`):
`python3 KiemThu/kiem-tra-giao-bai-theo-buoi.py`. Mặc định dùng `http://localhost:5280`,
database `sudia_dev`; có thể đặt `SUDIA_TEST_URL`, `SUDIA_TEST_DATABASE`, `SUDIA_TEST_PASSWORD`.
Script tạo bài tập có mã riêng, thử nộp / chấm rồi xoá đúng dữ liệu thử đó.

## Trang Admin (`/Admin`) — vận hành
| Trang | Làm gì |
| --- | --- |
| Tổng quan | 8 chỉ số, biểu đồ 7 ngày, việc cần xử lý, tình hình khoá, bài nộp gần đây và lịch sắp tới |
| Người dùng | Tạo / sửa thông tin, chi tiết hồ sơ và kết quả học, lọc vai trò / chưa ghi danh, khoá / mở và đổi vai trò |
| Khoá học | Tạo / sửa khoá, kiểm tra mã trùng và gán giảng viên hợp lệ |
| Ghi danh & hạn học | Tất cả ghi danh, lọc học viên / khoá / còn hạn / sắp hết / đã hết, phân trang, mở khoá và quản lý hạn học |
| Nội dung bài học | Xem mọi khoá, lọc nháp / xuất bản / thiếu nội dung, mở công cụ soạn bài và câu hỏi |
| Bài tập & bài nộp | Lọc bài đã giao / nháp / chờ chấm, xem và chấm bài nộp, quản lý đề thi thử |
| Lịch dạy toàn hệ thống | Các buổi của mọi khoá, lọc thời gian, tạo / sửa lịch, vào lớp và giao bài |
| Báo cáo học tập | Lọc ngày / khoá, số liệu và biểu đồ bài nộp, tỉ lệ đúng, bảng theo ngày và xuất CSV |
| Trợ lý AI & tài liệu | Xem trạng thái, cấu hình hiện tại, tài liệu nguồn, bộ kiểm thử và mở chat |

Chi tiết phạm vi và kiểm tra: `docs/nang-cap-admin.md`. Các công cụ soạn / chấm / lịch dùng lại
khu giảng viên với quyền Admin trên mọi khoá. Báo cáo tính theo giờ Việt Nam.
Kiểm tra với web Development cục bộ: `python3 KiemThu/kiem-tra-admin.py` (cần Python 3 và `psql`).


## AI soạn bài cho giảng viên (Gemini)

Nút **Chia theo đề mục văn bản** là lựa chọn ngoại tuyến, không gọi API: nhận diện các tiêu đề Markdown,
số La Mã, số thứ tự, số phân cấp và chữ cái; giữ nguyên đoạn văn và mở bản xem thử có thể sửa.
Không có ký hiệu tiêu đề thì giữ chung nội dung, không tự đoán cấu trúc theo ngữ nghĩa như AI.
Nút có ở trang tạo chủ đề và trang soạn bài, dùng được khi Gemini hết hạn mức hoặc không phản hồi.

**Nội dung bài học → Tạo chủ đề** dùng Gemini, độc lập với trợ lý hỏi đáp Claude.
Lấy khóa từ https://aistudio.google.com/apikey và chọn dự án **Free tier chưa bật Billing** nếu muốn chỉ dùng hạn mức miễn phí.
Chạy `zsh scripts/cau-hinh-ai.zsh` rồi khởi động lại web. Script lưu `GEMINI_API_KEY` bằng .NET user-secrets, không đưa khóa vào Git.
Model cấu hình tại `GeminiSoanBai:Model` trong `appsettings.json`.
`GeminiSoanBai:TimeoutSeconds` mặc định 30 giây: hết thời gian chờ thì giữ lại văn bản và báo để dùng nút chia theo đề mục.
Trong khi chờ, form hiển thị số giây đã chờ, chặn bấm AI lặp lại và vẫn cho chuyển sang chia đề mục văn bản.
Trang xem thử có URL GET riêng sau khi gửi form, nên tải lại/quay lại không cần gửi lại yêu cầu AI.
Bản nháp tạm riêng theo tài khoản được giữ trong bộ nhớ máy chủ tối đa 2 giờ; hết hạn hoặc khởi động lại web sẽ đưa về trang nội dung kèm thông báo.
Khi nhập sai hoặc AI lỗi, các ô đã nhập được giữ tạm và mở lại trên trang GET để tránh lưu trang POST trong lịch sử.
Hết hạn mức trả thông báo chờ/thử lại, không tự chuyển sang Claude. Chi phí thực tế phụ thuộc dự án Google gắn với khóa;
ứng dụng không kiểm tra hay thay đổi Billing của tài khoản Google. Hạn mức miễn phí có thể thay đổi.
Ở gói miễn phí, Google có thể sử dụng nội dung đầu vào/đầu ra để cải thiện sản phẩm:
https://ai.google.dev/gemini-api/docs/pricing.

## Trợ lý AI
Menu học viên **Hỏi AI** mở trực tiếp `/Home/HoiAi`, không mở trang bài học.
Ô **Hỏi nhanh AI** ở Tổng quan cũng chuyển sang chat và gửi câu hỏi đã nhập.
Trang dùng API trợ lý hiện có, hiển thị rõ khi đang ở bản xem thử (chỉ trả lời các câu gợi ý).
Đoạn chat mới hoặc đổi môn sẽ xoá cuộc trò chuyện hiện tại; lịch sử chưa lưu vào database.

1. Đặt khoá API (không ghi vào appsettings.json):
   `dotnet user-secrets init && dotnet user-secrets set ANTHROPIC_API_KEY sk-ant-...`
   (hoặc biến môi trường `ANTHROPIC_API_KEY`). Chưa có khoá thì web tự dùng bản xem thử.
2. Tài liệu khoá: thả file `.md` vào `TaiLieu/` theo cấu trúc `#` Chương / `##` Bài / `###` Mục, đầu file có `khoa:` và `mon:`. Mỗi `###` là một đoạn để tìm kiếm và ghi nguồn.
3. Prompt: sửa trong `Prompts/prompt-he-thong-tro-ly.md` (Phần 1 và Phần 4 được đọc trực tiếp), khởi động lại web.
4. Cấu hình ở mục `TroLy` trong `appsettings.json`: model trả lời (`claude-opus-5-5`), model phụ cho viết lại câu hỏi + AI chấm (`claude-haiku-5-5`), số đoạn, mức suy nghĩ, bật/tắt AI chấm.
5. Kiểm thử: chạy ở môi trường Development rồi mở `GET /api/tro-ly/kiem-thu` (mỗi lần chạy tốn lượt gọi API). Mục tiêu: trả lời đúng ≥ 90%, từ chối đúng ≥ 95%.
6. Câu trả lời không qua kiểm tra bị thay bằng "Nội dung này chưa có trong tài liệu khoá học." và ghi log cảnh báo `Trợ lý KHONG_DAT` để Admin xem.

## Học online (Google Meet)
Menu ⋮ ở mỗi bài (trang chi tiết khoá) có mục **Học online**, mở link Meet ở tab mới. Dán link vào `appsettings.json`:
```json
"HocOnline": {
  "LinkChung": "https://meet.google.com/abc-defg-hij",
  "TheoBai": {
    "7": { "Link": "https://meet.google.com/xyz-abcd-efg", "ThoiGian": "2026-10-28T19:30:00" }
  }
}
```
`LinkChung` dùng cho mọi buổi; `TheoBai` (khoá = mã bài) ghi đè link và thêm giờ học (hiện khi rê chuột). Chỉ nhận link `https`. Bài chưa có link thì mục hiện mờ.

## Ghi chú
- Trong file `.cshtml`, ký tự `@` thường phải viết `@@` (Razor). CSS/JS đã tách ra `wwwroot` nên không bị ảnh hưởng.
- Trang Tổng quan, Bài học, Làm câu hỏi vẫn theo phong cách cũ (xanh ngọc); trang chủ theo phong cách mới (đen ấm, đồng).
- Bước tiếp theo: Identity + phân quyền theo gói, database; chuyển `KhoTaiLieu` sang pgvector + full-text khi tài liệu nhiều; nối khung trợ lý ở trang Bài học vào API.
