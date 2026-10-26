# Kế hoạch trang Giảng viên và Admin

> **Cập nhật 27/10/2026:** Admin đã mở rộng thành 9 mục: Tổng quan, Người dùng, Khoá học, Ghi danh & hạn học, Nội dung bài học, Bài tập & bài nộp, Lịch dạy, Báo cáo học tập, Trợ lý AI & tài liệu. Có tạo khoá, hồ sơ / sửa người dùng, báo cáo theo ngày / khoá và xuất CSV. Trợ lý mới hiển thị trạng thái / cấu hình hiện tại; chỉnh cấu hình, nhật ký AI, gói học / giá, đơn hàng và nội dung trang chủ vẫn chưa làm. Xem `nang-cap-admin.md`.

> **Tiến độ (26/10/2026):** Giai đoạn 0 xong (Identity, PostgreSQL, lưu bài làm). Giảng viên: Bàn làm việc, Nội dung bài học, Ngân hàng câu hỏi, Đề thi thử, Lịch dạy online, Học viên — đã làm. Admin: Tổng quan, Người dùng, Khoá học & ghi danh — đã làm. Còn lại: nhập file .md, Trợ lý AI của khoá (log, báo sai, kiểm thử), Gói học & giá, Đơn hàng, cấu hình AI, nội dung trang chủ.

Nguyên tắc: mỗi thứ học viên **thấy** ở khu học viên thì phải có người **tạo / sửa / theo dõi** được. Giảng viên lo **nội dung và học viên của lớp mình**; Admin lo **tài khoản, khoá, gói, cấu hình và toàn hệ thống**.

Đã bổ sung **Giao bài tập theo buổi**: menu ⋮ ở mỗi bài → trình soạn 3 bước với nhiều câu trắc nghiệm, đúng / sai, tự luận; lưu nháp / xuất bản; học viên nộp bài; chấm tự động phần khách quan và giảng viên chấm tự luận. Chi tiết tại `ban-giao-giao-bai-tap.md`.

## 1. Đối chiếu tính năng học viên → giảng viên / admin

| Học viên đang có | Giảng viên cần | Admin cần |
| --- | --- | --- |
| Khoá của tôi: ô khoá có ảnh, tiến độ, học thử / đã mở | Xem khoá mình phụ trách, tiến độ chung của lớp | Tạo khoá, ảnh bìa, mô tả, gán giảng viên; ghi danh học viên, hạn học, học thử |
| Chi tiết khoá: chủ đề → bài, trạng thái đã học / đang học | Sắp xếp chủ đề, bài; xuất bản / ẩn bài | — |
| Bài học: tài liệu dài, mục → phần, mốc thời gian, ghi nhớ, mục lục | Soạn bài theo mục / phần, xem trước như học viên, nháp → xuất bản; nhập từ file .md | — |
| Menu ⋮ "Học online" (Google Meet) | Lịch dạy: link Meet + giờ cho từng buổi, danh sách buổi sắp tới | Xem lịch toàn hệ thống |
| Luyện đề: bài tập theo bài, đề thi thử có giờ | Ngân hàng câu hỏi (câu, 4 phương án, đáp án, lời giải, mục liên kết); tạo đề thi thử từ ngân hàng | — |
| Làm bài: nộp mới hiện đáp án, điểm, lọc đúng / sai | Xem bài làm của từng học viên, câu hay sai, tỉ lệ đúng từng câu | Thống kê lượt làm đề toàn hệ thống |
| Tổng quan: tiếp tục học, thống kê tuần, kết quả gần đây, "nên ôn lại" | Danh sách học viên cần chú ý (điểm thấp, lâu không học) | — |
| Trợ lý AI: trả lời theo tài liệu, có nguồn, từ chối khi ngoài tài liệu, nút "Báo sai" | Xem câu học viên hỏi trong khoá, câu bị từ chối / không đạt kiểm tra → bổ sung tài liệu; xử lý "Báo sai"; chạy bộ kiểm thử | Cấu hình model, bật AI chấm, xem nhật ký AI toàn hệ thống, kết quả kiểm thử theo khoá |
| Lượt hỏi AI hôm nay (12/30) | — | Giới hạn lượt hỏi theo gói |
| Trang chủ: môn học, gói học, giá, so sánh gói | — | Sửa gói, giá, quyền lợi, câu hỏi gợi ý trang chủ |

## 2. Trang Giảng viên (`/GiangVien`)

| Trang | Nội dung |
| --- | --- |
| Tổng quan | Buổi online sắp tới; học viên cần chú ý; 5 câu hỏi sai nhiều nhất; câu AI bị báo sai chờ xử lý |
| Nội dung khoá | Cây Khoá → Chủ đề → Bài → Mục → Phần, kéo thả sắp xếp; trình soạn phần (tiêu đề, đoạn văn, mốc thời gian, ghi nhớ); Xem trước; Nháp / Xuất bản; Nhập file .md theo quy chuẩn tài liệu (Phần 5 file prompt). Xuất bản xong tự nạp lại tài liệu cho trợ lý AI |
| Ngân hàng câu hỏi | Lọc theo bài / chủ đề; thêm / sửa câu; nhập Excel / CSV; mỗi câu hiện tỉ lệ đúng |
| Đề thi thử | Tạo đề: chọn câu thủ công hoặc ngẫu nhiên theo chủ đề; số phút; mở / đóng đề |
| Lịch dạy online | Buổi học theo bài: link Meet, giờ; thay cho việc sửa `appsettings.json` |
| Học viên | Danh sách lớp; tiến độ từng người; mở bài làm của học viên (giống màn kết quả học viên thấy) |
| Trợ lý AI của khoá | Câu học viên hỏi; câu bị từ chối / không đạt; báo sai; chạy bộ câu hỏi kiểm thử của khoá |

## 3. Trang Admin (`/Admin`)

| Trang | Nội dung |
| --- | --- |
| Tổng quan | Số học viên, giảng viên; lượt hỏi AI, lượt làm đề theo ngày; tỉ lệ AI từ chối / không đạt |
| Người dùng | Tạo / khoá tài khoản; vai trò Học viên / Giảng viên / Admin |
| Khoá học | Tạo khoá (môn, mô tả, ảnh bìa); gán giảng viên; ẩn / hiện |
| Ghi danh | Học viên ↔ khoá, hạn học, học thử bài 1, mở khoá thủ công |
| Gói học | Một môn / Trọn bộ: giá, quyền lợi, lượt hỏi AI mỗi ngày, bảng so sánh trên trang chủ |
| Đơn hàng | (Nếu bán online) danh sách đơn, trạng thái thanh toán |
| Trợ lý AI | Model, MaxTokens, bật AI chấm; nhật ký toàn hệ thống; kết quả bộ kiểm thử theo khoá (mục tiêu đúng ≥ 90 %, từ chối đúng ≥ 95 %) |
| Trang chủ | Câu hỏi gợi ý, môn học, nội dung giới thiệu |

## 4. Nền tảng phải làm trước

1. **Đăng nhập & phân quyền** — ASP.NET Core Identity, 3 vai trò `HocVien`, `GiangVien`, `Admin`; `[Authorize(Roles = …)]`; Area `GiangVien`, `Admin`. Khu học viên đòi đăng nhập.
2. **Cơ sở dữ liệu** — EF Core + PostgreSQL. Bảng chính: NguoiDung, Khoa, ChuDe, Bai, Muc, Phan, Moc, CauHoi, PhuongAn, DeThi, DeThiCau, BaiLam, CauTraLoi, GhiDanh, Goi, DonHang, BuoiOnline, NhatKyAi, BaoSai. Viết `EfKhoaHocService` thay `DuLieuMauService` (giữ nguyên `IKhoaHocService`, view học viên không phải sửa); nạp dữ liệu mẫu hiện tại làm seed.
3. **Lưu bài làm** — hiện đang chấm nhưng không lưu; cần lưu để có "kết quả gần đây", điểm thật ở Luyện đề và thống kê cho giảng viên.
4. **Khung giao diện chung** — dùng lại menu trái và phong cách khu học viên (Inter, thẻ, bảng), mỗi vai trò một menu riêng.

## 5. Lộ trình

| Giai đoạn | Việc | Xong khi |
| --- | --- | --- |
| 0. Nền tảng | Identity + vai trò; DB + migration + seed; lưu bài làm | Đăng nhập 3 vai trò; dữ liệu học viên đọc từ DB; nộp bài được lưu |
| 1. Giảng viên – nội dung | Nội dung khoá (soạn / xem trước / xuất bản / nhập .md); Ngân hàng câu hỏi; Lịch dạy online | Giảng viên tạo 1 bài + 10 câu hỏi + 1 buổi Meet, học viên thấy ngay |
| 2. Giảng viên – theo dõi | Học viên & bài làm; Đề thi thử; Trợ lý AI của khoá (log, báo sai, bộ kiểm thử) | Giảng viên xem được bài làm và câu hay sai; chạy kiểm thử AI từ trang |
| 3. Admin – vận hành | Người dùng & vai trò; Khoá học & gán giảng viên; Ghi danh; Gói học | Admin tạo giảng viên, khoá, ghi danh học viên, đặt hạn học |
| 4. Admin – hệ thống | Tổng quan số liệu; Cấu hình & nhật ký AI; Trang chủ; (Đơn hàng) | Admin đổi giới hạn lượt hỏi theo gói và xem nhật ký AI |

Mỗi việc làm trên một nhánh riêng (`feat/…`) → gộp `dev` để kiểm tra → gộp `main` khi được duyệt.

## 6. Cần chốt trước khi làm

1. Giảng viên chỉ sửa **khoá được gán** hay sửa mọi khoá?
2. Có **bán khoá và thanh toán online** không (ảnh hưởng trang Đơn hàng, Gói học)?
3. Đăng nhập bằng **email + mật khẩu**, **Google**, hay cả hai?
4. Nơi chạy web và PostgreSQL (máy chủ / dịch vụ nào)?
