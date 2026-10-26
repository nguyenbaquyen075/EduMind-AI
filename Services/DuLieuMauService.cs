using SuDia.Web.Models;

namespace SuDia.Web.Services;

public class DuLieuMauService : IKhoaHocService
{
    public TrangChuViewModel LayTrangChu() => new()
    {
        CauHoiGoiY =
        [
            "Chiến dịch Điện Biên Phủ diễn ra khi nào?",
            "Vì sao Tây Nguyên trồng nhiều cà phê?",
        ],
        BuocHoc =
        [
            new("I.", "Đọc", "Lý thuyết theo mục ngắn, có mốc thời gian, lược đồ và phần ghi nhớ cuối bài."),
            new("II.", "Luyện", "Trắc nghiệm theo bài, mỗi đáp án có lời giải và chỉ về đúng mục lý thuyết."),
            new("III.", "Hỏi", "Trợ lý giải đáp theo tài liệu khoá, kèm chú thích nguồn để tự đối chiếu."),
            new("IV.", "Thi thử", "Đề tính giờ như thật, chấm ngay và chỉ ra chương còn yếu để ôn lại."),
        ],
        MonHoc =
        [
            new("Môn thứ nhất", "Lịch sử 12",
                "Từ thế giới sau Chiến tranh lạnh đến công cuộc Đổi mới, theo chương trình mới.",
                "#D9A48C", "/images/khoa/lich-su-12.jpg", 1,
                [
                    "Thế giới trong và sau Chiến tranh lạnh",
                    "ASEAN: những chặng đường lịch sử",
                    "Cách mạng tháng Tám, chiến tranh giải phóng và bảo vệ Tổ quốc",
                    "Công cuộc Đổi mới từ năm 1986 đến nay",
                    "Lịch sử đối ngoại của Việt Nam thời cận – hiện đại",
                    "Hồ Chí Minh trong lịch sử Việt Nam",
                ]),
            new("Môn thứ hai", "Địa lí 12",
                "Tự nhiên, dân cư, kinh tế và các vùng — cùng kỹ năng Atlat, bảng số liệu, biểu đồ.",
                "#8DB8A4", "/images/khoa/dia-li-12.jpg", 101,
                [
                    "Địa lí tự nhiên Việt Nam",
                    "Địa lí dân cư",
                    "Địa lí các ngành kinh tế",
                    "Địa lí các vùng kinh tế",
                    "Kỹ năng Atlat, bảng số liệu và biểu đồ",
                ]),
        ],
        GoiHoc =
        [
            new("Một môn", "Lịch sử 12", null, "#D9A48C", false,
                ["Toàn bộ lý thuyết lớp 12", "Câu hỏi theo bài và đề thi thử", "Trợ lý môn Lịch sử"]),
            new("Trọn bộ", "Sử & Địa 12", null, "#C8A15A", true,
                ["Toàn bộ nội dung hai môn", "Đề thi thử tổng hợp", "Trợ lý cho cả hai môn", "Phân tích chương còn yếu"]),
            new("Một môn", "Địa lí 12", null, "#8DB8A4", false,
                ["Toàn bộ lý thuyết lớp 12", "Kỹ năng Atlat, bảng số liệu, biểu đồ", "Trợ lý môn Địa lí"]),
        ],
        SoSanh =
        [
            new("Lý thuyết", "Bài đầu tiên", "Toàn bộ môn", "Cả hai môn"),
            new("Câu hỏi theo bài", "Bài đầu tiên", "Có", "Có"),
            new("Đề thi thử tính giờ", "—", "Có", "Có, kèm đề tổng hợp"),
            new("Câu hỏi trợ lý mỗi ngày", "[số]", "[số]", "[số]"),
            new("Phân tích chương còn yếu", "—", "—", "Có"),
        ],
    };

    public TongQuanViewModel LayTongQuan() => new()
    {
        TenHocSinh = "[Tên học sinh]",
        ChuCaiDaiDien = "HS",
        NgayHomNay = DateTime.Today,
        LuotHoiDaDung = LuotHoiDaDung,
        LuotHoiToiDa = LuotHoiToiDa,
        TiepTuc = new("Lịch sử 12", "Bài 7. Kháng chiến chống Pháp (1945–1954)", "Mục 3 · Chiến dịch Điện Biên Phủ", 60),
        ThongKe = new(BaiDaHoc: 5, CauDaLam: 86, TiLeDung: 78, ChuoiNgay: 4),
        KhoaCuaToi = DanhMuc.Select(k => new KhoaCuaToi(k.Id, k.Ten, k.KyHieu, k.DaMo, k.BaiDaHoc, k.TongBai, k.HanDen)).ToList(),
        KetQuaGanDay =
        [
            new("Luyện tập Bài 6", "Hôm qua", 9, 10),
            new("Đề thi thử số 2", "3 ngày trước", 31, 40),
            new("Luyện tập Bài 5", "5 ngày trước", 8, 10),
        ],
        ChuDeNenOn = ["Mốc thời gian 1945–1946", "Chiến dịch Việt Bắc 1947"],
    };

    // ======================================================= Khoá học của tôi

    private const int LuotHoiDaDung = 12, LuotHoiToiDa = 30;

    // Danh mục mẫu. Khi có database: đọc khoá học sinh đã mua + tiến độ từng bài
    private static readonly IReadOnlyList<KhoaHocChinh> DanhMuc = [TaoLichSu(), TaoDiaLi()];

    private static KhoaHocChinh TaoLichSu()
    {
        const int dangHoc = 7;
        BaiTrongKhoa B(int so, string ten, int soMuc = 4, int soPhut = 25) => new(so, so, ten,
            so < dangHoc ? TrangThaiBai.DaHoc : so == dangHoc ? TrangThaiBai.DangHoc : TrangThaiBai.ChuaHoc,
            false, soMuc, soPhut);

        return new("lich-su-12", "Lịch sử 12", "S",
            "Từ thế giới sau Chiến tranh lạnh đến công cuộc Đổi mới, theo chương trình mới.",
            "/images/khoa/lich-su-12.jpg", true, null,
            [
                new("Chủ đề 1 · Thế giới trong và sau Chiến tranh lạnh",
                    [B(1, "Liên hợp quốc"), B(2, "Trật tự thế giới trong thời kì Chiến tranh lạnh"), B(3, "Trật tự thế giới sau Chiến tranh lạnh")]),
                new("Chủ đề 2 · ASEAN: những chặng đường lịch sử",
                    [B(4, "Sự ra đời và phát triển của Hiệp hội các quốc gia Đông Nam Á (ASEAN)"), B(5, "Cộng đồng ASEAN: từ ý tưởng đến hiện thực")]),
                new("Chủ đề 3 · Cách mạng tháng Tám, chiến tranh giải phóng và bảo vệ Tổ quốc",
                    [B(6, "Cách mạng tháng Tám năm 1945", 2, 15), B(7, "Cuộc kháng chiến chống thực dân Pháp (1945–1954)"),
                     B(8, "Cuộc kháng chiến chống Mỹ, cứu nước (1954–1975)", 5, 30), B(9, "Cuộc đấu tranh bảo vệ Tổ quốc từ sau tháng 4 năm 1975 đến nay")]),
                new("Chủ đề 4 · Công cuộc Đổi mới từ năm 1986 đến nay",
                    [B(10, "Khái quát về công cuộc Đổi mới từ năm 1986 đến nay"), B(11, "Thành tựu cơ bản và bài học của công cuộc Đổi mới")]),
                new("Chủ đề 5 · Lịch sử đối ngoại của Việt Nam thời cận – hiện đại",
                    [B(12, "Hoạt động đối ngoại của Việt Nam trong đấu tranh giành độc lập dân tộc"),
                     B(13, "Hoạt động đối ngoại của Việt Nam trong kháng chiến chống Pháp và chống Mỹ"),
                     B(14, "Hoạt động đối ngoại của Việt Nam từ năm 1975 đến nay")]),
                new("Chủ đề 6 · Hồ Chí Minh trong lịch sử Việt Nam",
                    [B(15, "Khái quát về cuộc đời và sự nghiệp của Hồ Chí Minh"), B(16, "Hồ Chí Minh – Anh hùng giải phóng dân tộc"),
                     B(17, "Dấu ấn Hồ Chí Minh trong lòng nhân dân thế giới và Việt Nam")]),
            ]);
    }

    private static KhoaHocChinh TaoDiaLi()
    {
        // Mã bài Địa lí = 100 + số bài để không trùng với Lịch sử. Chưa mua: chỉ học thử bài 1
        BaiTrongKhoa B(int so, string ten) => new(100 + so, so, ten, TrangThaiBai.ChuaHoc, so != 1, 3, 20);

        return new("dia-li-12", "Địa lí 12", "Đ",
            "Tự nhiên, dân cư, kinh tế và các vùng — cùng kỹ năng Atlat, bảng số liệu, biểu đồ.",
            "/images/khoa/dia-li-12.jpg", false, null,
            [
                new("Chủ đề 1 · Địa lí tự nhiên Việt Nam",
                    [B(1, "Vị trí địa lí và phạm vi lãnh thổ"), B(2, "Thiên nhiên nhiệt đới ẩm gió mùa"), B(3, "Sự phân hoá đa dạng của thiên nhiên")]),
                new("Chủ đề 2 · Địa lí dân cư",
                    [B(6, "Dân số Việt Nam"), B(7, "Lao động và việc làm"), B(8, "Đô thị hoá")]),
                new("Chủ đề 3 · Địa lí các ngành kinh tế", [B(10, "[Tên bài]")]),
                new("Chủ đề 4 · Địa lí các vùng kinh tế", [B(30, "Khai thác thế mạnh ở Tây Nguyên")]),
                new("Chủ đề 5 · Kỹ năng Atlat, bảng số liệu và biểu đồ", [B(35, "[Tên bài]")]),
            ]);
    }

    public KhoaHocViewModel LayKhoaHoc(string? khoaId) => new()
    {
        Menu = new MenuTaiKhoan("KhoaHoc", LuotHoiDaDung, LuotHoiToiDa),
        Khoa = DanhMuc,
        DangChon = DanhMuc.FirstOrDefault(k => k.Id == khoaId),
    };

    // ======================================================= Luyện đề

    // Mẫu: mỗi bài có 10 câu luyện tập; bài đã học coi như đã luyện, kết quả lấy xoay vòng
    private static readonly int[] DiemMau = [9, 8, 10, 7, 9, 8];

    private static KhoaLuyen TaoKhoaLuyen(KhoaHocChinh k) => new(k,
        k.Chuong.Select(c => new ChuongLuyen(c.Ten, c.Bai.Select(b => new BaiLuyen(
            b.Id, b.So, b.Ten, 10,
            b.TrangThai == TrangThaiBai.DaHoc ? DiemMau[b.So % DiemMau.Length] : null,
            b.BiKhoa)).ToList())).ToList(),
        k.DaMo
            ? [new(1001, "Đề thi thử số 1", 40, 50, 33, false), new(1002, "Đề thi thử số 2", 40, 50, 31, false), new(1003, "Đề thi thử số 3", 40, 50, null, false)]
            : [new(2001, "Đề thi thử số 1", 40, 50, null, true)]);

    public LuyenDeViewModel LayLuyenDe(string? khoaId)
    {
        var tatCa = DanhMuc.Select(TaoKhoaLuyen).ToList();
        return new()
        {
            Menu = new MenuTaiKhoan("LuyenDe", LuotHoiDaDung, LuotHoiToiDa),
            Khoa = tatCa,
            DangChon = tatCa.FirstOrDefault(k => k.Khoa.Id == khoaId),
        };
    }

    // ======================================================= Bài học

    // Nội dung mẫu theo bài (mã bài -> các mục). Lấy từ TaiLieu/lich-su-12.md.
    // Khi có database: đọc mục/phần của bài từ tài liệu khoá đã nạp
    private static readonly Dictionary<int, IReadOnlyList<MucNoiDung>> NoiDungMau = new()
    {
        [7] =
        [
            new(1, "Bối cảnh", "Bối cảnh cuộc kháng chiến", 3,
            [
                new("bung-no", "Kháng chiến toàn quốc bùng nổ", ["Đêm 19/12/1946, cuộc kháng chiến toàn quốc chống thực dân Pháp bùng nổ."]),
                new("ghi-nho", "Ghi nhớ nhanh", ["19/12/1946: kháng chiến toàn quốc chống thực dân Pháp bùng nổ."], LaGhiNho: true),
            ]),
            new(2, "Các chiến dịch lớn", "Các chiến dịch lớn", 4,
            [
                new("viet-bac", "Chiến dịch Việt Bắc thu – đông năm 1947", ["Chiến dịch Việt Bắc thu – đông năm 1947 diễn ra từ ngày 7/10 đến ngày 19/12/1947."]),
                new("bien-gioi", "Chiến dịch Biên giới thu – đông năm 1950", ["Chiến dịch Biên giới thu – đông năm 1950 diễn ra từ ngày 16/9 đến ngày 22/10/1950."]),
                new("ghi-nho", "Ghi nhớ nhanh", ["7/10 – 19/12/1947: Chiến dịch Việt Bắc · 16/9 – 22/10/1950: Chiến dịch Biên giới"], LaGhiNho: true),
            ]),
            new(3, "Chiến dịch Điện Biên Phủ", "Chiến dịch Điện Biên Phủ năm 1954", 6,
            [
                new("dien-bien", "Diễn biến",
                    ["Diễn ra từ 13/3 đến 7/5/1954, là trận quyết chiến chiến lược của cuộc kháng chiến chống thực dân Pháp."],
                    [
                        new("ĐỢT 1", "13–17/3", "Tiêu diệt Him Lam và phân khu Bắc."),
                        new("ĐỢT 2", "30/3–26/4", "Đánh phía đông phân khu Trung tâm."),
                        new("ĐỢT 3", "1/5–7/5", "Tổng công kích, toàn thắng."),
                    ]),
                new("y-nghia", "Ý nghĩa", ["Thắng lợi tạo điều kiện cho đấu tranh ngoại giao, dẫn tới việc ký Hiệp định Giơnevơ ngày 21/7/1954."]),
                new("ghi-nho", "Ghi nhớ nhanh", ["13/3/1954 mở màn · 7/5/1954 toàn thắng · 21/7/1954 ký Hiệp định Giơnevơ"], LaGhiNho: true),
            ]),
            new(4, "Hiệp định Giơnevơ", "Hiệp định Giơnevơ năm 1954", 4,
            [
                new("ky-ket", "Hội nghị và ký kết", ["Hội nghị Giơnevơ về Đông Dương khai mạc ngày 8/5/1954. Hiệp định Giơnevơ được ký ngày 21/7/1954."]),
                new("noi-dung", "Nội dung chính", ["Theo Hiệp định Giơnevơ, Việt Nam tạm thời chia làm hai miền, lấy vĩ tuyến 17 làm giới tuyến quân sự tạm thời."]),
                new("ghi-nho", "Ghi nhớ nhanh", ["8/5/1954 khai mạc hội nghị · 21/7/1954 ký hiệp định · vĩ tuyến 17"], LaGhiNho: true),
            ]),
        ],
    };

    public BaiHocViewModel LayBaiHoc(int? id)
    {
        var khoa = DanhMuc.FirstOrDefault(k => k.TatCaBai.Any(b => b.Id == id)) ?? DanhMuc[0];
        var bai = khoa.TatCaBai.FirstOrDefault(b => b.Id == id) ?? khoa.BaiHocTiep ?? khoa.TatCaBai.First();
        var chuong = khoa.Chuong.First(c => c.Bai.Contains(bai));
        var noiDung = bai.BiKhoa ? [] : NoiDungMau.GetValueOrDefault(bai.Id) ?? [];

        return new BaiHocViewModel
        {
            KhoaId = khoa.Id,
            TenKhoa = khoa.Ten,
            BaiDaHoc = khoa.BaiDaHoc,
            TongBai = khoa.TongBai,
            Chuong = [new(chuong.Ten, chuong.Bai.Select(b => new BaiHocItem(b.Id, b.So, b.Ten, b.TrangThai, b.BiKhoa)).ToList())],
            BaiId = bai.Id,
            SoBaiHienTai = bai.So,
            BiKhoa = bai.BiKhoa,
            NoiDung = noiDung,
            NhanBai = $"BÀI {bai.So} · {(noiDung.Count > 0 ? noiDung.Count : bai.SoMuc)} MỤC",
            TieuDe = bai.Ten,
            MoDau = "Nội dung bài này sẽ hiện khi nạp tài liệu khoá học.",
            TinNhanMau = bai.Id == 7
                ?
                [
                    new(true, "Chiến dịch kết thúc ngày nào?"),
                    new(false, "Ngày 7/5/1954, khi toàn bộ tập đoàn cứ điểm bị tiêu diệt, tướng Đờ Cát-xtơ-ri bị bắt sống.", "Bài 7 · Mục 3"),
                    new(true, "Chiến dịch Hồ Chí Minh 1975 thế nào?"),
                    new(false, "Nội dung này chưa nằm trong các bài đã mở. Bạn có thể hỏi về Bài 6 và Bài 7.", LaTuChoi: true),
                ]
                : [],
            GoiYHoi = noiDung.Count > 0 ? ["Tóm tắt bài này", "Giải thích dễ hiểu hơn"] : ["Tóm tắt bài này"],
        };
    }

    public LamCauHoiViewModel LayCauHoi(int? id, string? dapAnDaChon)
    {
        const string dapAnDung = "B";
        var cauSo = 3;
        var bang = new List<OCauHoi>();
        for (var i = 1; i <= 10; i++)
        {
            var tt = i switch
            {
                1 => TrangThaiCau.Dung,
                2 => TrangThaiCau.Sai,
                _ when i == cauSo && dapAnDaChon is not null =>
                    dapAnDaChon == dapAnDung ? TrangThaiCau.Dung : TrangThaiCau.Sai,
                _ => TrangThaiCau.ChuaLam,
            };
            bang.Add(new OCauHoi(i, tt, i == cauSo));
        }

        // Mã 1001+ / 2001+ là đề thi thử (Lịch sử / Địa lí); còn lại là bài tập theo bài
        var khoa = DanhMuc.FirstOrDefault(k => k.TatCaBai.Any(b => b.Id == id))
                   ?? (id >= 2000 ? DanhMuc[1] : DanhMuc[0]);
        var bai = khoa.TatCaBai.FirstOrDefault(b => b.Id == id);
        var tenBai = bai is not null ? $"Luyện tập · Bài {bai.So}"
                   : id >= 1000 ? $"Đề thi thử số {id % 1000}"
                   : "Luyện tập · Bài 7";

        return new LamCauHoiViewModel
        {
            Id = id ?? 7,
            KhoaId = khoa.Id,
            TenBai = tenBai,
            CauSo = cauSo,
            TongCau = 10,
            MucDo = "NHẬN BIẾT",
            NoiDung = "Chiến dịch Điện Biên Phủ kết thúc thắng lợi vào ngày nào?",
            PhuongAn =
            [
                new("A", "13/3/1954"),
                new("B", "7/5/1954"),
                new("C", "21/7/1954"),
                new("D", "19/12/1946"),
            ],
            DapAnDung = dapAnDung,
            DapAnDaChon = dapAnDaChon,
            GiaiThich = "Chiều 7/5/1954, toàn bộ tập đoàn cứ điểm Điện Biên Phủ bị tiêu diệt. Ngày 21/7/1954 là ngày ký Hiệp định Giơnevơ.",
            NguonMuc = "Bài 7 · Mục 3",
            BangCau = bang,
            ThoiGianConLai = "08:42",
        };
    }
}
