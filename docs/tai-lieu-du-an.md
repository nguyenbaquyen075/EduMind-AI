# Tài liệu dự án Sử Địa — SuDia.Web

**Ngày cập nhật:** 28/10/2026  
**Mã nguồn:** [EduMind-AI](https://github.com/nguyenbaquyen075/EduMind-AI)  
**Phạm vi:** mô tả mã nguồn hiện tại sau khi gộp `dev` vào `main` tại commit `417f3e2`. Những mục ghi “hướng phát triển” chưa phải chức năng đã hoàn thành.

## 1. Giới thiệu

Sử Địa là ứng dụng web hỗ trợ dạy và học, hiện có nội dung mẫu cho Lịch sử 12 và Địa lí 12. Hệ thống kết hợp quản lý khoá học, soạn nội dung bài giảng, giao bài tập, tổ chức kiểm tra, theo dõi kết quả và trợ lý AI.

Giảng viên có thể nhập văn bản hoặc tải tài liệu Word/PowerPoint để chuẩn bị nội dung. Hệ thống hỗ trợ chia thành các đề mục; giảng viên xem và sửa trước khi lưu vào bài học. Học viên đọc bài, làm bài tập và kiểm tra, xem kết quả, đồng thời đặt câu hỏi cho trợ lý AI. Quản trị viên quản lý người dùng, khoá học, ghi danh và hoạt động học tập.

Ứng dụng hiện chạy theo mô hình web MVC với một máy chủ ứng dụng và cơ sở dữ liệu PostgreSQL. AI được gọi qua API; dự án chưa huấn luyện mô hình AI riêng trên máy cá nhân.

## 2. Vai trò và quyền sử dụng

| Vai trò | Chức năng chính | Phạm vi |
| --- | --- | --- |
| Học viên | Xem khoá học, đọc bài, làm bài tập/kiểm tra, xem điểm, hỏi AI | Nội dung được học thử hoặc đã được ghi danh theo hạn học |
| Giảng viên | Soạn bài, nhập tài liệu, giao bài, tạo đề, chấm tự luận, quản lý lịch dạy và theo dõi học viên | Các khoá được Admin phân công |
| Admin | Quản lý người dùng, vai trò, khoá học, ghi danh, báo cáo; sử dụng công cụ soạn và chấm | Toàn bộ khoá học |

Đăng ký tài khoản mới tạo vai trò học viên; việc đăng ký không tự mở toàn bộ khoá. Quyền học còn phụ thuộc trạng thái xuất bản, cờ học thử và ghi danh còn hạn. Đề kiểm tra mới yêu cầu ghi danh hợp lệ vào khoá, không mở chỉ vì bài được đánh dấu học thử.

## 3. Chức năng học viên

### 3.1. Học theo khoá

- Xem danh sách khoá và chi tiết từng khoá.
- Duyệt bài theo chủ đề, mở bài và đọc nội dung theo mục lục.
- Theo dõi tiến độ và kết quả học tập được lưu trong cơ sở dữ liệu.
- Truy cập bài học thử hoặc nội dung thuộc khoá đã ghi danh.
- Mở đường dẫn Google Meet của buổi dạy nếu đã được cấu hình.

### 3.2. Bài tập và kiểm tra

Học viên làm bài với ba loại câu: trắc nghiệm bốn phương án, đúng/sai bốn ý và tự luận. Trắc nghiệm và đúng/sai được chấm trên máy chủ; tự luận chờ giảng viên chấm. Điểm tự động được hiển thị riêng khi điểm tự luận chưa hoàn tất.

Mỗi bài tập được giao chỉ nhận một bài nộp cho mỗi học viên. Đáp án và lời giải không xuất hiện trong trang làm bài trước khi nộp. Bài quá hạn nộp bị chặn theo thời gian máy chủ.

Với đề kiểm tra mới, học viên bấm **Bắt đầu làm bài** trước khi xem câu hỏi. Thời gian bắt đầu được lưu ở máy chủ; tải lại trang không đặt lại đồng hồ. Trang tự gửi bài khi hết giờ. Máy chủ có khoảng đệm truyền bài 10 giây; đáp án đến sau khoảng này không được ghi nhận và bài nộp muộn được chấm theo dữ liệu rỗng.

Hệ thống vẫn giữ luồng luyện trắc nghiệm và dữ liệu đề cũ. Đề mới dùng form ba loại câu; không phải mọi màn luyện tập cũ đã chuyển sang mô hình mới.

### 3.3. Hỏi AI

Trang **Hỏi AI** cho phép chọn môn, gửi câu hỏi, xem nguồn trả lời và bắt đầu hội thoại mới. Máy chủ chỉ sử dụng tối đa ba lượt trao đổi gần nhất do trình duyệt gửi. Lịch sử chat chưa lưu bền trong database.

Trợ lý có chế độ xem thử khi chưa cấu hình khoá Anthropic. Chế độ xem thử không tương đương một kết nối AI thật.

## 4. Chức năng giảng viên

### 4.1. Quản lý nội dung

Cấu trúc bài học:

```text
Khoá học
└── Chủ đề
    └── Bài học
        └── Mục
            └── Phần: đoạn văn, ghi nhớ, mốc thời gian
```

Trang nội dung có nút **Tạo chủ đề** dẫn tới trang tạo riêng. Menu dấu ba chấm của chủ đề cung cấp thao tác thêm bài, chỉnh sửa và xoá. Thêm bài mở biểu mẫu để nhập thông tin và soạn nội dung.

Giảng viên tự nhập số bài hiển thị, phù hợp khi bổ sung bài vào giữa chương trình. Số bài và mã định danh dùng trong đường dẫn là hai giá trị riêng. Bài tạo từ luồng soạn nội dung ban đầu ở trạng thái nháp; thông tin bài có tuỳ chọn xuất bản và cho học thử.

Các ô nội dung ở màn xem thử và sửa mục tự tăng chiều cao theo văn bản. Tiêu đề hiển thị riêng, mốc thời gian và ghi nhớ là các thông tin tuỳ chọn để giảm số trường phải nhập.

### 4.2. Soạn từ văn bản

1. Chọn khoá/chủ đề, nhập tên và số bài.
2. Dán nội dung hoặc dàn ý.
3. Chọn **AI tạo các mục nội dung** hoặc **Chia theo đề mục văn bản**.
4. Xem và sửa bản nháp.
5. Xác nhận lưu nội dung vào bài; sau đó kiểm tra trạng thái xuất bản tại thông tin bài.

Hai cách chia nội dung hoạt động khác nhau:

| Cách chia | Xử lý | Khi nên dùng |
| --- | --- | --- |
| Gemini | Đề nghị AI nhận diện cấu trúc theo nội dung, giữ các ý và đề mục gốc | Văn bản có cấu trúc chưa rõ hoặc cần phân tích ngữ nghĩa |
| Theo đề mục văn bản | Tách bằng ký hiệu như `#`, `##`, `I.`, `1.`, `1.1.`, `a)` trên máy chủ | Tài liệu có dấu mục rõ hoặc AI không phản hồi |

Cách chia ngoại tuyến không suy luận giống AI. Nếu không có dấu mục nhận diện được, nội dung được giữ chung để giảng viên sửa. Cả hai cách đều cần rà soát kết quả trước khi dùng cho học viên.

Trang xem thử dùng URL GET riêng để tránh gửi lại yêu cầu AI khi tải lại trang. Bản xem thử tạm lưu trong bộ nhớ theo từng tài khoản, tối đa hai giờ; khởi động lại máy chủ có thể làm mất bản tạm. Nội dung đã xác nhận lưu vào bài được lưu trong PostgreSQL.

**Giới hạn chia sẻ hiện tại:** biểu mẫu có lựa chọn “Khoá học” và “Đội miễn phí”, nhưng mã xử lý hiện chỉ lưu các mục vào bài hiện có và thay thông báo theo lựa chọn. Chưa có mô hình đội miễn phí và cơ chế phân phối/quyền truy cập riêng. Thao tác này cũng không tự đặt bài thành trạng thái xuất bản.

### 4.3. Nhập Word và PowerPoint

Khung **Nhập từ Word hoặc PowerPoint** có ở trang tạo chủ đề, thêm bài và soạn bài hiện có.

| Nội dung | Hỗ trợ hiện tại |
| --- | --- |
| Định dạng | `.docx`, `.pptx`; file `.doc`/`.ppt` cần lưu lại sang định dạng mới |
| Dung lượng | Tối đa 20 MB/file; có thêm giới hạn dữ liệu giải nén |
| Độ dài văn bản | Từ 30 đến 30.000 ký tự sau trích xuất; vượt giới hạn bị báo lỗi, không cắt bớt |
| Word | Lấy chữ theo thứ tự, đọc cấp tiêu đề từ style và outline level, có xử lý style kế thừa |
| PowerPoint | Đọc theo thứ tự trình chiếu; các slide liền nhau có cùng tiêu đề dùng chung heading |
| Bảng | Chuyển thành dòng chữ, dùng dấu phân cách giữa các ô và hiển thị ghi chú |
| Ảnh | Phát hiện một số thành phần ảnh và báo đối chiếu; chưa chèn ảnh hoặc OCR vào bài |
| Tên bài | Gợi ý từ tiêu đề hoặc tên file nếu ô tên bài đang trống |
| Nội dung có sẵn | Hiện nút xác nhận thay nội dung, không tự ghi đè ngay |

Luồng sử dụng: **Chọn file → Kiểm tra nội dung được điền → Chia đề mục/AI → Xem thử → Sửa → Lưu**. Số bài vẫn do giảng viên nhập.

Phần đọc file chạy tại máy chủ bằng ZIP/XML của .NET, chưa dùng Open XML SDK. Không cài hay khởi chạy Microsoft Office; riêng bước đọc file không gửi tài liệu sang Gemini. Khi chọn nút AI, văn bản đã trích xuất mới được gửi để phân tích.

Chưa hỗ trợ đầy đủ chữ trong ảnh, ghi chú người thuyết trình, video, hiệu ứng, biểu đồ, bố cục nhiều cột và nhãn đánh số tự động của Word. Một file nhiều bài chưa có bước ánh xạ hàng loạt sang nhiều bài học; nên chia file theo bài trước khi nhập.

### 4.4. Form chung cho bài tập và kiểm tra

Đã bỏ mục **Thêm câu hỏi** riêng khỏi menu. Các đường dẫn tạo câu cũ chuyển về form giao bài tập. Công cụ sửa câu ngân hàng cũ vẫn được giữ để duy trì dữ liệu đã có.

Form gồm ba bước:

1. **Thông tin chung:** tên, bài học, hướng dẫn, hạn nộp; bài tập có thể gắn buổi dạy; kiểm tra có thời gian 5–240 phút.
2. **Soạn câu hỏi:** bật/tắt phần trắc nghiệm, đúng/sai, tự luận; thêm, xoá, đổi thứ tự và đặt điểm.
3. **Xem lại & xuất bản:** kiểm tra câu hỏi, đáp án, tổng điểm và thông tin giao bài; có thể lưu nháp.

Bài tập đã có bài nộp và đề kiểm tra mới đã có lượt bắt đầu bị khoá sửa/xoá để giữ nội dung và kết quả. Đề cũ chưa có bài làm có thể chuyển sang form chung khi lưu; việc chuyển không sửa câu gốc trong ngân hàng. Đề cũ đã có kết quả được giữ nguyên.

Giảng viên xem danh sách bài nộp, đọc tự luận và nhập điểm. Chế độ làm thử của giảng viên không tạo bài nộp học viên.

### 4.5. Lịch dạy và theo dõi học viên

Giảng viên quản lý các buổi học theo bài với thời gian và đường dẫn Meet; xem tiến độ, bài nộp và kết quả của học viên trong khoá được giao. Web mở link Meet, không có hệ thống truyền hình trực tuyến riêng.

## 5. Chức năng quản trị

| Nhóm | Công việc |
| --- | --- |
| Tổng quan | Xem chỉ số, hoạt động gần đây, lịch dạy và việc cần xử lý |
| Người dùng | Tạo/sửa tài khoản, quản lý vai trò, khoá/mở tài khoản, xem hồ sơ học tập |
| Khoá học | Tạo/sửa khoá và phân công giảng viên |
| Ghi danh | Mở quyền học, đặt hạn học, lọc ghi danh còn hạn hoặc hết hạn |
| Nội dung | Xem và truy cập công cụ soạn trên mọi khoá |
| Bài tập và bài nộp | Theo dõi bài giao, nháp và bài chờ chấm; truy cập công cụ chấm |
| Lịch dạy | Quản lý lịch toàn hệ thống |
| Báo cáo | Lọc theo ngày/khoá, xem số liệu và xuất CSV |
| Trợ lý AI | Xem trạng thái cấu hình, tài liệu nguồn và công cụ kiểm thử |

## 6. Công nghệ và tổ chức mã nguồn

| Thành phần | Công nghệ đang dùng |
| --- | --- |
| Máy chủ web | C#, ASP.NET Core MVC, .NET 8 |
| Giao diện | Razor Views, HTML, CSS, JavaScript |
| Tài khoản | ASP.NET Core Identity, cookie đăng nhập, phân quyền theo vai trò |
| Cơ sở dữ liệu | PostgreSQL, Entity Framework Core 8, Npgsql |
| Soạn nội dung AI | Gemini API qua `HttpClient` |
| Hỏi đáp AI | Anthropic SDK và luồng tìm tài liệu trước khi trả lời |
| Nhập Office | `ZipArchive`, XML parser, không thực thi macro |
| Bản xem thử | `IMemoryCache`, riêng theo tài khoản |
| Kiểm thử nghiệp vụ | Script Python, HTTP local và PostgreSQL; JavaScript kiểm tra cú pháp bằng Node.js |

Luồng web thông thường: **Trình duyệt → Controller → Service/DbContext → ViewModel → Razor HTML**. Các API tương tác trả JSON cho JavaScript.

| Thư mục/file | Vai trò |
| --- | --- |
| `Program.cs` | Khởi tạo dịch vụ, Identity, database, routing, giới hạn API và dữ liệu ban đầu |
| `Controllers/` | Trang học viên, tài khoản, làm bài và API trợ lý |
| `Areas/GiangVien/` | Công cụ soạn nội dung, bài tập, đề và theo dõi học viên |
| `Areas/Admin/` | Quản trị người dùng, khoá, ghi danh, lịch và báo cáo |
| `Data/` | Thực thể, DbContext, khởi tạo dữ liệu và migration |
| `Models/` | ViewModel, biểu mẫu và mô hình câu hỏi |
| `Services/` | Nghiệp vụ đọc dữ liệu, AI, nhập Office và chia đề mục |
| `Views/`, `wwwroot/` | Giao diện Razor và CSS/JavaScript/ảnh |
| `TaiLieu/`, `Prompts/` | Tài liệu Markdown và prompt cho trợ lý hỏi đáp |
| `KiemThu/` | Các script kiểm tra nghiệp vụ và bộ câu hỏi kiểm thử AI |
| `docs/` | Tài liệu dự án, bàn giao và nghiên cứu |

## 7. Dữ liệu chính

| Nhóm dữ liệu | Thành phần |
| --- | --- |
| Tài khoản | `NguoiDung` và các bảng ASP.NET Core Identity |
| Bài học | `Khoa → ChuDe → Bai → Muc → Phan → MocThoiGian` |
| Quyền học | `GhiDanh`, có hạn học hoặc không giới hạn |
| Tiến độ | `TienDoBai` theo học viên và bài |
| Luyện tập cũ | `CauHoi`, `DeThiCau`, `BaiLam`, `CauTraLoi` |
| Bài tập và đề mới | `BaiTapGiao`, lưu cấu trúc câu hỏi dưới dạng JSON |
| Bài nộp mới | `BaiNopGiao`, lưu câu trả lời, điểm tự động và điểm tự luận |
| Thời gian kiểm tra | `LuotLamKiemTra`, lưu thời điểm bắt đầu theo học viên và đề |
| Lịch dạy | `BuoiHoc`, gắn với bài học |

`DeThi.BaiTapGiaoId` liên kết đề mới với bộ câu hỏi chung. Đề cũ có thể chưa có liên kết này. Ràng buộc duy nhất trên cặp bài tập–học viên ngăn lưu nhiều bài nộp cho cùng một bài.

## 8. Hai chức năng AI độc lập

### 8.1. Gemini soạn bài

- Nhận tên bài và văn bản; trả về các mục/phần để mở bản xem thử.
- Cấu hình khoá bằng `GEMINI_API_KEY`; model và thời gian chờ ở mục `GeminiSoanBai`.
- Thời gian chờ nghiệp vụ mặc định 30 giây; khi lỗi hoặc hết hạn mức, người dùng có thể chuyển sang chia theo ký hiệu đề mục.
- Không tự chuyển sang nhà cung cấp trả phí khác.
- Có khoá cấu hình không có nghĩa mọi yêu cầu AI sẽ thành công; còn phụ thuộc model khả dụng, quyền dự án, mạng và hạn mức nhà cung cấp.

### 8.2. Claude hỏi đáp theo tài liệu

Luồng chính: **Câu hỏi → Xử lý câu hỏi nối tiếp → Tìm đoạn liên quan → Gọi Claude → Kiểm tra câu trả lời → Trả lời kèm nguồn**.

Nguồn hiện lấy từ file Markdown trong `TaiLieu/`, tách theo các mục. Bộ tìm kiếm dùng BM25 trên từ đã bỏ dấu; chưa sử dụng pgvector. Cấu hình ở mục `TroLy`, khoá qua `ANTHROPIC_API_KEY`.

Nội dung bài vừa soạn trong PostgreSQL chưa tự đồng bộ thành nguồn tài liệu cho trợ lý. Không nên mặc định rằng tải Word/PowerPoint lên là chatbot đã học được nội dung đó.

Phần nhập Office và chia đề mục ngoại tuyến không gọi AI. Khi sử dụng Gemini/Claude, hạn mức và chi phí phụ thuộc tài khoản API; ứng dụng không cung cấp cam kết miễn phí không giới hạn. Khoá API không được ghi trong tài liệu hoặc commit vào Git.

## 9. Cài đặt và chạy trên máy

### 9.1. Chuẩn bị

- .NET 8 SDK phù hợp target `net8.0`.
- PostgreSQL đang hoạt động và tài khoản có quyền sử dụng database dự án.
- Git để lấy mã nguồn.
- Python 3 và `psql` nếu chạy bộ kiểm thử nghiệp vụ; Node.js nếu kiểm tra cú pháp JavaScript.

### 9.2. Thiết lập lần đầu

```sh
git clone https://github.com/nguyenbaquyen075/EduMind-AI.git
cd EduMind-AI
cp appsettings.Development.example.json appsettings.Development.json
```

Sửa `ConnectionStrings:SuDia` trong file Development cho đúng máy, thay `YOUR_POSTGRES_USER` bằng tài khoản PostgreSQL. Không đưa cấu hình cá nhân lên Git. Có thể đặt mật khẩu tài khoản mẫu qua `TaiKhoanMau:MatKhau`.

```sh
createdb sudia_dev
dotnet restore
dotnet dev-certs https --trust
dotnet run --launch-profile SuDia.Web
```

Nếu database đã tồn tại thì bỏ qua lệnh `createdb`. Mở **https://localhost:7180**; launch profile cũng có HTTP ở cổng 5180 và chuyển sang HTTPS.

Khi khởi động, ứng dụng tự chạy migration, tạo vai trò và nạp nội dung mẫu nếu database trống. Tài khoản mẫu chỉ được tạo trong môi trường Development:

| Email | Vai trò |
| --- | --- |
| `admin@sudia.vn` | Admin |
| `giangvien@sudia.vn` | Giảng viên |
| `hocvien@sudia.vn` | Học viên |

Mật khẩu theo cấu hình Development; tài liệu này không ghi khoá API hoặc mật khẩu cá nhân. Đây là tài khoản thử nghiệm, không phải cơ chế cấp tài khoản vận hành chính thức.

Để tạo lại **toàn bộ** dữ liệu mẫu trong Development (xoá nội dung khoá, học viên, kết quả, bài nộp và đơn hàng đang có), chạy lệnh sau rồi để ứng dụng tự thoát:

```sh
dotnet run -- --du-lieu-mau --xac-nhan-xoa-du-lieu-mau
```

Tham số xác nhận là bắt buộc để tránh xoá nhầm dữ liệu. Không dùng lệnh này trên môi trường vận hành.

### 9.3. Kết nối AI

Để cấu hình Gemini trên macOS/zsh:

```sh
zsh scripts/cau-hinh-ai.zsh
```

Script hỏi khoá bằng ô nhập ẩn và lưu vào .NET user-secrets cho Development. Khởi động lại web sau khi cấu hình. Trợ lý Claude dùng user-secrets hoặc biến môi trường `ANTHROPIC_API_KEY` riêng; cấu hình Gemini không tự cấu hình Claude.

Ở môi trường triển khai, cấp chuỗi kết nối và khoá API bằng cơ chế cấu hình của máy chủ. User-secrets trên máy phát triển không được tự đưa lên máy chủ qua Git.

## 10. Kiểm thử và xác nhận thay đổi

Build dự án:

```sh
dotnet build
```

Chạy các luồng nghiệp vụ khi web local và PostgreSQL Development đang hoạt động:

```sh
SUDIA_TEST_URL=https://localhost:7180 python3 KiemThu/kiem-tra-giao-bai-theo-buoi.py
SUDIA_TEST_URL=https://localhost:7180 python3 KiemThu/kiem-tra-form-kiem-tra.py
SUDIA_TEST_URL=https://localhost:7180 python3 KiemThu/kiem-tra-nhap-office.py
```

Các script trên dùng tài khoản demo, có thể cấu hình `SUDIA_TEST_PASSWORD` và `SUDIA_TEST_DATABASE`. Chúng tạo dữ liệu có mã riêng và dọn dữ liệu thử sau khi chạy. Chỉ dùng trên môi trường Development. Script kiểm tra form kiểm tra và nhập Office hiện cần khoá mẫu `lich-su-12` cùng tài khoản demo/quyền học phù hợp.

Phạm vi đã kiểm tra trong các đợt phát triển gần nhất:

- Lưu nháp, xuất bản, kiểm tra dữ liệu và chống yêu cầu thiếu CSRF.
- Ba loại câu hỏi; chấm tự động, chấm tự luận và hiển thị tổng điểm.
- Bắt đầu kiểm tra, giữ thời gian khi tải lại, chặn nộp muộn và khoá sửa đề đã có người làm.
- Không hiển thị đáp án trước khi nộp; kiểm tra quyền truy cập trong các luồng được thử.
- Đọc Word có cấp heading, PowerPoint đổi thứ tự slide và mở bản xem thử từ nội dung đã nhập.
- Chặn một số file sai định dạng, file hỏng và nội dung quá dài.

Các kiểm tra này không thay thế đánh giá tải lớn, kiểm toán bảo mật hoặc kiểm thử mọi tài liệu thực tế. Chất lượng AI không được chứng minh chỉ bằng việc build thành công.

Kiểm thử trợ lý qua `/api/tro-ly/kiem-thu` chỉ bật ở Development và có thể phát sinh lượt API. Bộ kiểm thử Admin ở `KiemThu/kiem-tra-admin.py` có phạm vi dữ liệu rộng hơn; đọc script trước khi chạy.

## 11. Quy trình Git của dự án

```text
Nhánh riêng theo công việc → commit → push nhánh
                                      ↓
                                 gộp vào dev
                                      ↓
                              build và kiểm tra
                                      ↓
                        được chủ dự án duyệt mới gộp main
```

- Mỗi việc có nhánh riêng, theo nhóm `feat/...`, `style/...`, `chore/...`.
- Tách commit theo nội dung công việc; tránh gộp nhiều tính năng không liên quan vào một commit.
- Giữ merge commit để lịch sử thể hiện rõ các nhánh đã tích hợp.
- Không đẩy thay đổi trực tiếp vào `main` khi chưa được chủ dự án yêu cầu/duyệt.
- Không đưa khoá API, cấu hình Development cá nhân, thư mục build hoặc dữ liệu riêng vào commit.

Ví dụ các nhánh đã thực hiện: `feat/lesson-authoring`, `style/lesson-editor`, `chore/gemini-config`, `feat/question-authoring`, `feat/document-import`.

Đẩy mã lên GitHub chỉ cập nhật mã nguồn. Tài liệu này chưa xác nhận có pipeline tự động triển khai ứng dụng lên máy chủ công khai.

## 12. Những phần cần phát triển tiếp

| Nội dung | Hướng xử lý |
| --- | --- |
| Bản xem thử dễ mất khi restart | Lưu nháp bền trong database, tự lưu và mở lại phiên soạn |
| Nhập tài liệu còn giới hạn | OCR theo nhu cầu, giữ ảnh/bảng, phục hồi đánh số, đọc bố cục phức tạp và ánh xạ file nhiều bài |
| Chờ AI trong yêu cầu web | Tác vụ nền, trạng thái tiến độ, giới hạn đồng thời và cơ chế mở lại kết quả |
| Chia sẻ đội miễn phí | Bổ sung mô hình đội, thành viên, phạm vi chia sẻ và kiểm tra quyền truy cập |
| Nguồn chatbot tách khỏi bài học | Xây dựng luồng đồng bộ nội dung đã duyệt thành nguồn tìm kiếm |
| Lịch sử hội thoại | Lưu theo tài khoản và cho phép tiếp tục cuộc trò chuyện |
| Vận hành thực tế | Hoàn thiện cấu hình triển khai, sao lưu/khôi phục, giám sát và kiểm thử tải |
| Bán khoá | Chưa có quy trình thanh toán hoàn chỉnh trong phạm vi mã được khảo sát |

## 13. Tài liệu và mã tham chiếu

- [Hướng dẫn chạy và cấu hình](../README.md).
- [Kế hoạch giảng viên và Admin](ke-hoach-giang-vien-admin.md).
- [Phạm vi nâng cấp Admin](nang-cap-admin.md).
- [Nghiên cứu nhập Word/PowerPoint và phạm vi đã triển khai](nghien-cuu-nhap-word-powerpoint.md).
- [Controller soạn bài](../Areas/GiangVien/Controllers/NoiDungController.cs), [bộ đọc Office](../Services/DocTaiLieuOffice.cs).
- [Controller giao bài và chấm tự luận](../Areas/GiangVien/Controllers/GiaoBaiController.cs), [controller học viên làm bài](../Controllers/BaiTapController.cs).
- [Cấu hình ứng dụng](../Program.cs), [mô hình database](../Data/AppDbContext.cs), [bộ tìm tài liệu chatbot](../Services/TroLy/KhoTaiLieu.cs).

Một số tài liệu bàn giao trước đây phản ánh giao diện ở thời điểm cũ. Khi có khác biệt, đối chiếu mã nguồn và phạm vi cập nhật của tài liệu này.
