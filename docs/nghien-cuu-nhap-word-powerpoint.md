# Nhập Word/PowerPoint để soạn tài liệu học

Ngày khảo sát: 28/10/2026. Đây là đề xuất nghiên cứu; chưa triển khai nhập file vào sản phẩm.

**Cập nhật triển khai 28/10/2026:** đã thêm bản nhập chữ `.docx/.pptx` tại trang tạo chủ đề/thêm bài và soạn bài. Khung chọn file tự điền nội dung, gợi ý tên bài khi ô tên còn trống, chuyển heading Word thành cấp mục và lấy PowerPoint theo thứ tự slide; các slide liền nhau trùng tiêu đề dùng chung heading. Bảng chuyển thành văn bản và có cảnh báo; ảnh/OCR, phục hồi nhãn đánh số Word tự động, bố cục nâng cao, lưu tác vụ bền và ánh xạ nhiều bài vẫn là phần đề xuất bên dưới. Giới hạn bản hiện tại: 20 MB/file, 30.000 ký tự sau trích xuất, không cắt nội dung khi vượt giới hạn. Bộ kiểm thử HTTP: `KiemThu/kiem-tra-nhap-office.py` (tài liệu tự tạo, không gọi AI).

## Kết luận

Nên hỗ trợ `.docx` và `.pptx` bằng cách đọc cấu trúc tài liệu trước, sau đó dùng AI cho những chỗ chưa rõ. Không nên đổi toàn bộ file thành một chuỗi văn bản rồi yêu cầu AI chia theo một mẫu cố định. Mục tiêu là giữ tên mục, thứ tự và nội dung gốc; giảng viên xem, sửa rồi mới lưu vào khoá học.

Open XML SDK của Microsoft đọc các gói Office Open XML dựa trên ZIP/XML; thư viện có giấy phép MIT. Có thể dùng cho phần trích cấu trúc mà không phát sinh lượt gọi API AI, nhưng vẫn có chi phí vận hành máy chủ. Đây là lựa chọn phù hợp để thử nghiệm trong ứng dụng .NET 8 hiện tại. [Tổng quan SDK](https://learn.microsoft.com/en-us/office/open-xml/open-xml-sdk), [mã nguồn và giấy phép](https://github.com/dotnet/Open-XML-SDK).

## Cách nhận diện mục trong tài liệu

| Tài liệu | Cách xử lý đề xuất | Chỗ cần người dùng xem lại |
| --- | --- | --- |
| Word có Heading 1/2/3 | Đọc paragraph style, outline level và style kế thừa; dựng cây đề mục theo đúng cấp | Heading bị dùng sai hoặc nhảy cấp |
| Word gõ số I, 1, a thủ công | Dùng dấu mục cùng định dạng chữ làm tín hiệu; AI chỉ hỗ trợ đoạn chưa rõ | Dòng in đậm có thể là nhấn mạnh, chưa chắc là đề mục |
| Word đánh số tự động | Đọc numbering definitions và numbering properties, khôi phục số hiển thị | Không coi mọi danh sách đánh số là tiêu đề |
| PowerPoint | Đọc thứ tự slide, title/body placeholder, cấp bullet và vị trí khối chữ; nối slide tiếp theo có cùng chủ đề khi đủ bằng chứng | Một slide chưa chắc là một mục; bố cục nhiều cột có thể gây sai thứ tự đọc |
| File có ảnh/bảng | Giữ từng khối và liên kết với vị trí nguồn | Không bỏ ảnh/bảng khi chuyển sang mô hình chỉ có văn bản |

Word có paragraph styles riêng; outline level và numbering properties là cấu trúc lưu trong WordprocessingML, không nhất thiết nằm trong phần chữ nhìn thấy. Vì vậy chỉ lấy `InnerText` rồi dùng regex sẽ mất tín hiệu quan trọng. [Paragraph styles](https://learn.microsoft.com/en-us/office/open-xml/word/how-to-create-and-add-a-paragraph-style-to-a-word-processing-document), [outline level](https://learn.microsoft.com/en-us/dotnet/api/documentformat.openxml.wordprocessing.outlinelevel?view=openxml-3.0.1), [numbering properties](https://learn.microsoft.com/en-us/dotnet/api/documentformat.openxml.wordprocessing.numberingproperties?view=openxml-3.0.1).

PowerPoint lưu danh sách slide ở presentation part; mỗi slide có part riêng. Phải đi theo slide list, không dựa vào thứ tự tên file `slide1.xml`. Các hình, bảng, notes, animation và video là thành phần riêng; một hàm lấy chữ không đồng nghĩa đã nhập đầy đủ nội dung slide. [Đọc chữ theo slide](https://learn.microsoft.com/en-us/office/open-xml/presentation/how-to-get-all-the-text-in-a-slide-in-a-presentation), [cấu trúc PresentationML](https://learn.microsoft.com/en-us/office/open-xml/presentation/structure-of-a-presentationml-document).

Speaker notes có part riêng. Đề xuất mặc định không đưa notes vào nội dung học viên; cho giảng viên chọn “Kèm ghi chú người thuyết trình”, và đánh dấu rõ nguồn notes trong bản xem thử. [Notes slides](https://learn.microsoft.com/en-us/office/open-xml/presentation/working-with-notes-slides).

## Luồng sử dụng đề xuất

1. Trong trang tạo bài/chủ đề, có một vùng **Dán nội dung hoặc tải tài liệu**. Chọn `.docx` hoặc `.pptx`; hiện tên file và tình trạng đang đọc.
2. Hệ thống tự đọc cấu trúc gốc, gợi ý tên bài/chủ đề và các mục. Giữ chế độ “Theo tài liệu gốc” làm mặc định; có lựa chọn dùng AI để nhận diện những mục chưa rõ.
3. Hiện bản xem thử: mục lục nhỏ ở trái, nội dung sửa trực tiếp ở phải; mỗi mục có thêm, đổi tên, gộp, tách và xoá. Các đoạn chưa chắc chắn có nhãn “Cần xem lại”, liên kết tới slide/đoạn nguồn.
4. Với file chỉ chứa một bài, tự điền tên bài và các mục. Số bài vẫn do giảng viên nhập, đúng quy ước hiện tại. Với tài liệu gồm nhiều bài hoặc cả khoá, hiện bước ánh xạ Chủ đề → Bài → Mục để duyệt; không ép tất cả vào một bài duy nhất.
5. Bấm **Lưu bản nháp** sau khi duyệt. Xuất bản/chia sẻ dùng quy trình hiện có, không tự công bố ngay sau tải file.

Không tạo mục “Nội dung 1/2/3” chỉ vì đủ số lượng. Không lặp cùng tên ở “Tên mục”, “Tiêu đề hiển thị”, “Tiêu đề phần” nếu người dùng chưa yêu cầu. Nếu tài liệu không có cấu trúc rõ, hệ thống phải cho biết đó là gợi ý suy luận.

## Tích hợp vào mã hiện tại

Theo khảo sát repo, `NoiDungController.TaoChuDeBangAi` nhận văn bản 30–30.000 ký tự, tạo bài nháp và mở xem thử. `ChiaDeMucVanBan` đang đọc dấu mục bằng regex; `NoiDungAiMuc` gồm tên, tiêu đề và các phần văn bản. Mô hình `Muc → Phan` chưa có block ảnh/bảng. Gemini hiện có timeout 30 giây. Đây là những điểm cần mở rộng, không phải bằng chứng rằng chỉ thêm một ô tải file là đủ.

Đề xuất một lớp trung gian, ví dụ `TaiLieuNhap`, chứa cây các node `Heading`, `Paragraph`, `List`, `Table`, `Image`, `Slide`, cùng `sourceId`, thứ tự, cấp mục, định dạng và cảnh báo. Word/PowerPoint mỗi loại một bộ đọc, cùng trả về cây này. Bộ chuyển sang bài học và bộ AI dùng chung cây, thay vì làm phẳng trước rồi cố đoán lại.

AI chỉ trả cách nhóm/cấp mục và tham chiếu `sourceId`; chương trình dựng nội dung từ các block gốc. Kiểm tra node bị bỏ sót, lặp hoặc đổi thứ tự trước khi cho lưu. Coverage phải giải thích được từng source block: được giữ đúng một lần, hoặc bị loại có lý do hiển thị được. Không tự xoá mọi chữ trùng: footer/title lặp theo template khác với đoạn bài giảng lặp có chủ đích; chỉ nối các slide cùng tiêu đề khi nội dung và vị trí cho thấy là phần tiếp theo. Khi cần tóm tắt hay viết bổ sung, phải là thao tác riêng để phân biệt với nhập nguyên nội dung.

Cần quyết định mô hình ảnh/bảng trước khi nhận file có thành phần đó: thêm block nội dung và kho asset riêng, hoặc đánh dấu rõ thành phần chưa hỗ trợ ngay trong xem thử. Không được báo nhập thành công trong khi âm thầm bỏ nội dung. Số trang Word không nên cam kết nếu chưa có bộ render; dùng chỉ số đoạn/đề mục làm nguồn, còn PowerPoint dùng số slide.

## AI, ảnh scan và chi phí

Gemini có xử lý PDF bằng thị giác để hiểu chữ, hình và bảng; tài liệu chính thức lưu ý tài liệu khác PDF được nhìn như văn bản thông thường và có thể mất ngữ cảnh hình/định dạng. Điều này không bảo đảm tải trực tiếp mọi file Word/PowerPoint sẽ giữ nguyên cấu trúc. Đề xuất đọc Office bằng Open XML trước; với slide/Word chứa ảnh scan, dùng OCR/thị giác cho đúng ảnh đó, hoặc bổ sung luồng chuyển sang PDF để phân tích hình thức. [Gemini document understanding](https://ai.google.dev/gemini-api/docs/document-processing).

Chức năng đọc `.docx/.pptx` và chia theo heading có thể chạy khi AI mất kết nối, không cần huấn luyện mô hình trên máy. Chất lượng/độ nặng của OCR hay render phải đo bằng file thật, không hứa tốc độ trước. Không coi `.doc`/`.ppt` cũ là OOXML: bản đầu hướng dẫn lưu lại thành `.docx`/`.pptx`, chưa nhận file có mật khẩu, file macro hay file hỏng.

Gemini có free tier cho một số model với hạn mức; không cam kết miễn phí không giới hạn. Bảng giá hiện phân biệt dữ liệu ở free tier và paid tier, nên phải thông báo rõ khi dùng dịch vụ bên ngoài để phân tích tài liệu. Phần trích cấu trúc tại máy chủ không cần gửi file sang Gemini. [Giá và điều kiện Gemini API](https://ai.google.dev/gemini-api/docs/pricing).

## Hiệu năng và lưu nháp

Không buộc trang chờ một request AI dài. Đề xuất upload → mã tác vụ → đọc cấu trúc → phân tích mục → bản nháp; UI hiện từng trạng thái, cho huỷ và mở lại kết quả. Tác vụ và bản nháp lớn phải lưu bền trong database/kho file có quyền sở hữu; không chỉ dùng `IMemoryCache` hai giờ vì restart sẽ mất kết quả. Giới hạn đồng thời theo người dùng, cache theo hash file cùng tuỳ chọn, retry có giới hạn; không gửi lại cả file khi người dùng chỉ sửa một mục.

Ngưỡng khởi đầu để thử nghiệm, chưa phải giới hạn nhà cung cấp: một file/lần, tối đa 20 MB, thêm trần dữ liệu giải nén, số phần tử và thời gian xử lý. Kiểm tra bằng file thật trước khi công bố giới hạn số trang/slide. ASP.NET Core hỗ trợ buffered binding cho file nhỏ và streaming cho file lớn; Microsoft yêu cầu kiểm tra dung lượng, extension ở server, tên lưu do ứng dụng tạo và vị trí upload riêng. Với OOXML còn phải giới hạn gói ZIP giải nén và không tải external relationships. [ASP.NET Core file uploads](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-8.0).

## Triển khai theo từng việc

1. `feat/document-import`: bộ đọc `.docx/.pptx`, cây nguồn, chia mục không cần AI; xử lý văn bản có styles/numbering và slide có title/body; cảnh báo đủ nội dung chưa hỗ trợ.
2. `feat/document-import-preview`: xem/sửa cây, ánh xạ nhiều bài, lưu nháp bền, chống nhập lặp khi bấm lại; giữ nguyên số bài tự nhập và dữ liệu cũ.
3. `feat/document-import-ai`: AI hỗ trợ mục chưa rõ; OCR ảnh theo nhu cầu; block ảnh/bảng và thử nghiệm render khi cần giữ nội dung trực quan.

Kiểm thử chấp nhận cần file tiếng Việt thật: heading chuẩn và heading thủ công, numbering tự động, nhiều cấp, bảng/ảnh xen kẽ, slide nhiều cột, slide cùng tên, title kế thừa layout, slide chỉ có ảnh, notes, file nhiều bài, file lỗi/quá lớn. Kiểm tra giữ nguyên chữ, số mục, thứ tự, không lặp/mất block; AI lỗi vẫn nhập bằng cấu trúc; tải lại không mất nháp; không tạo dữ liệu xuất bản trước khi giảng viên duyệt. Chưa có file mẫu của giảng viên nên chưa thể kết luận tỷ lệ nhận diện hay thời gian chạy thực tế.
