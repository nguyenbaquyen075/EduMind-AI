namespace SuDia.Web.Data;

// Nội dung mẫu cho 2 khoá (chạy bằng: dotnet run -- --du-lieu-mau, chỉ ở Development).
// Mỗi bài: các mục (đoạn văn + ghi nhớ), ngân hàng câu hỏi trắc nghiệm, câu tự luận dùng khi giao bài tập.
internal static class DuLieuMauNoiDung
{
    internal record MucMau(string Ten, string[] DoanVan, string? GhiNho = null, (string Ten, string ThoiGian, string NoiDung)[]? Moc = null);
    internal record CauMau(string NoiDung, string A, string B, string C, string D, char Dung, string GiaiThich);
    internal record BaiMau(int So, string Ten, int SoPhut, MucMau[] Muc, CauMau[] Cau, string? TuLuan = null, string? HuongDanCham = null);
    internal record ChuDeMau(string Ten, BaiMau[] Bai);
    internal record KhoaMau(string Id, string MucTieu, string DoiTuong, ChuDeMau[] ChuDe);

    internal static readonly KhoaMau LichSu = new("lich-su-12",
        "Nắm được các sự kiện, mốc thời gian quan trọng của lịch sử thế giới và Việt Nam theo chương trình 12\nPhân tích được nguyên nhân, ý nghĩa của các sự kiện lớn\nLàm tốt câu hỏi trắc nghiệm và tự luận trong đề thi tốt nghiệp THPT",
        "Học sinh lớp 12 ôn thi tốt nghiệp THPT môn Lịch sử",
    [
        new("Chủ đề 1 · Thế giới trong và sau Chiến tranh lạnh",
        [
            new(1, "Liên hợp quốc", 35,
            [
                new("Sự ra đời của Liên hợp quốc",
                    ["Hội nghị Ianta (tháng 2/1945) thống nhất thành lập một tổ chức quốc tế để duy trì hoà bình và an ninh thế giới sau chiến tranh.",
                     "Từ ngày 25/4 đến 26/6/1945, Hội nghị San Phranxixcô (Mỹ) thông qua Hiến chương Liên hợp quốc. Ngày 24/10/1945, Hiến chương có hiệu lực, Liên hợp quốc chính thức ra đời."],
                    "Ngày 24/10/1945 được lấy làm Ngày Liên hợp quốc."),
                new("Mục đích và nguyên tắc hoạt động",
                    ["Mục đích: duy trì hoà bình và an ninh quốc tế; phát triển quan hệ hữu nghị giữa các dân tộc; thúc đẩy hợp tác quốc tế về kinh tế, văn hoá, xã hội và nhân đạo.",
                     "Nguyên tắc: bình đẳng chủ quyền giữa các quốc gia; tôn trọng toàn vẹn lãnh thổ và độc lập chính trị; không can thiệp vào công việc nội bộ; giải quyết tranh chấp bằng biện pháp hoà bình; chung sống hoà bình và sự nhất trí giữa 5 nước lớn (Liên Xô – nay là Liên bang Nga, Mỹ, Anh, Pháp, Trung Quốc)."]),
                new("Việt Nam và Liên hợp quốc",
                    ["Tháng 9/1977, Việt Nam trở thành thành viên thứ 149 của Liên hợp quốc.",
                     "Việt Nam hai lần được bầu làm Ủy viên không thường trực Hội đồng Bảo an, nhiệm kỳ 2008–2009 và 2020–2021."]),
            ],
            [
                new("Liên hợp quốc chính thức ra đời vào ngày nào?", "24/10/1945", "26/6/1945", "2/9/1945", "10/12/1948", 'A', "Ngày 24/10/1945 Hiến chương có hiệu lực, Liên hợp quốc chính thức ra đời."),
                new("Hiến chương Liên hợp quốc được thông qua tại hội nghị nào?", "Hội nghị Ianta", "Hội nghị Pốtxđam", "Hội nghị San Phranxixcô", "Hội nghị Henxinki", 'C', "Hội nghị San Phranxixcô (25/4 – 26/6/1945) thông qua Hiến chương."),
                new("Việt Nam gia nhập Liên hợp quốc năm nào?", "1975", "1977", "1986", "1995", 'B', "Tháng 9/1977 Việt Nam là thành viên thứ 149."),
                new("Nguyên tắc nhất trí giữa 5 nước lớn gồm những nước nào?", "Liên Xô, Mỹ, Anh, Pháp, Trung Quốc", "Liên Xô, Mỹ, Anh, Pháp, Đức", "Mỹ, Anh, Pháp, Nhật Bản, Trung Quốc", "Liên Xô, Mỹ, Anh, Đức, Nhật Bản", 'A', "Đây là 5 ủy viên thường trực Hội đồng Bảo an."),
                new("Cơ quan nào giữ vai trò chính trong việc duy trì hoà bình và an ninh thế giới?", "Đại hội đồng", "Hội đồng Bảo an", "Ban Thư ký", "Toà án Quốc tế", 'B', "Hội đồng Bảo an chịu trách nhiệm chính về hoà bình và an ninh quốc tế."),
                new("Việt Nam là Ủy viên không thường trực Hội đồng Bảo an trong những nhiệm kỳ nào?", "2008–2009 và 2020–2021", "2000–2001 và 2010–2011", "1997–1998 và 2008–2009", "2016–2017 và 2020–2021", 'A', "Việt Nam hai lần đảm nhiệm vai trò này."),
            ],
            "Trình bày mục đích và các nguyên tắc hoạt động của Liên hợp quốc.",
            "Nêu đủ 3 mục đích (1 điểm) và ít nhất 4 nguyên tắc (1 điểm)."),

            new(2, "Trật tự thế giới trong thời kì Chiến tranh lạnh", 40,
            [
                new("Trật tự hai cực Ianta",
                    ["Hội nghị Ianta (Liên Xô) họp từ ngày 4 đến 11/2/1945 với sự tham dự của nguyên thủ ba cường quốc Liên Xô, Mỹ, Anh.",
                     "Các quyết định của hội nghị về phân chia phạm vi ảnh hưởng đã hình thành khuôn khổ trật tự thế giới mới, gọi là trật tự hai cực Ianta, do Liên Xô và Mỹ đứng đầu mỗi cực."]),
                new("Chiến tranh lạnh",
                    ["Năm 1947, Học thuyết Truman ra đời, đánh dấu sự khởi đầu của Chiến tranh lạnh giữa Mỹ và Liên Xô.",
                     "Hai phe đối đầu gay gắt: Kế hoạch Mácsan (1947), khối NATO (1949) đối lập với Hội đồng Tương trợ kinh tế SEV (1949) và Tổ chức Hiệp ước Vácsava (1955)."],
                    "Tháng 12/1989, trong cuộc gặp ở đảo Manta, Mỹ và Liên Xô tuyên bố chấm dứt Chiến tranh lạnh."),
            ],
            [
                new("Hội nghị Ianta diễn ra vào thời gian nào?", "Tháng 2/1945", "Tháng 7/1945", "Tháng 4/1945", "Tháng 10/1945", 'A', "Hội nghị họp từ 4 đến 11/2/1945."),
                new("Sự kiện nào đánh dấu sự khởi đầu của Chiến tranh lạnh?", "Học thuyết Truman (1947)", "Kế hoạch Mácsan (1947)", "Khối NATO ra đời (1949)", "Khối Vácsava ra đời (1955)", 'A', "Học thuyết Truman năm 1947 mở đầu Chiến tranh lạnh."),
                new("Tổ chức quân sự của Mỹ và các nước Tây Âu thành lập năm 1949 là", "SEATO", "NATO", "CENTO", "ANZUS", 'B', "NATO ra đời tháng 4/1949."),
                new("Mỹ và Liên Xô tuyên bố chấm dứt Chiến tranh lạnh vào năm nào?", "1985", "1989", "1991", "1975", 'B', "Cuộc gặp ở đảo Manta tháng 12/1989."),
                new("Tổ chức Hiệp ước Vácsava được thành lập năm nào?", "1949", "1955", "1947", "1961", 'B', "Tổ chức Hiệp ước Vácsava ra đời tháng 5/1955."),
            ]),

            new(3, "Trật tự thế giới sau Chiến tranh lạnh", 35,
            [
                new("Sự sụp đổ của trật tự hai cực",
                    ["Trật tự hai cực Ianta sụp đổ năm 1991 khi Liên bang Xô viết tan rã.",
                     "Thế giới dần chuyển sang trật tự đa cực với nhiều trung tâm như Mỹ, Liên minh châu Âu, Nhật Bản, Trung Quốc, Liên bang Nga."]),
                new("Xu thế phát triển của thế giới",
                    ["Các quốc gia điều chỉnh chiến lược, lấy phát triển kinh tế làm trọng điểm.",
                     "Hoà bình, hợp tác và phát triển là xu thế chủ đạo, nhưng xung đột khu vực và chủ nghĩa khủng bố vẫn diễn ra ở nhiều nơi."],
                    "Toàn cầu hoá là xu thế khách quan, không thể đảo ngược."),
            ],
            [
                new("Trật tự hai cực Ianta sụp đổ vào năm nào?", "1989", "1991", "1945", "2001", 'B', "Liên Xô tan rã cuối năm 1991."),
                new("Sau Chiến tranh lạnh, các quốc gia lấy lĩnh vực nào làm trọng điểm?", "Quân sự", "Kinh tế", "Chính trị", "Văn hoá", 'B', "Phát triển kinh tế là trọng điểm trong chiến lược của các nước."),
                new("Xu thế chủ đạo của thế giới sau Chiến tranh lạnh là", "Hoà bình, hợp tác và phát triển", "Chạy đua vũ trang", "Đối đầu giữa hai phe", "Biệt lập về kinh tế", 'A', "Đây là xu thế chung của thế giới."),
                new("Sự kiện đánh dấu Liên bang Xô viết tan rã diễn ra vào", "Tháng 12/1991", "Tháng 11/1989", "Tháng 8/1945", "Tháng 10/1990", 'A', "Tháng 12/1991 Liên Xô chấm dứt tồn tại."),
            ]),
        ]),

        new("Chủ đề 2 · ASEAN: những chặng đường lịch sử",
        [
            new(4, "Sự ra đời và phát triển của ASEAN", 40,
            [
                new("Sự ra đời của ASEAN",
                    ["Ngày 8/8/1967, Hiệp hội các quốc gia Đông Nam Á (ASEAN) được thành lập tại Băng Cốc (Thái Lan) với 5 thành viên: Inđônêxia, Malaixia, Philíppin, Xingapo và Thái Lan.",
                     "Mục tiêu của ASEAN là phát triển kinh tế, văn hoá thông qua hợp tác giữa các nước thành viên, duy trì hoà bình và ổn định khu vực."]),
                new("Quá trình mở rộng",
                    ["Brunây gia nhập năm 1984, Việt Nam năm 1995, Lào và Mianma năm 1997, Campuchia năm 1999.",
                     "Hiệp ước Bali (1976) xác định các nguyên tắc cơ bản trong quan hệ giữa các nước ASEAN."],
                    "Việt Nam gia nhập ASEAN ngày 28/7/1995.",
                    [("Thành lập", "8/8/1967", "5 nước sáng lập"), ("Hiệp ước Bali", "2/1976", "Nguyên tắc quan hệ"), ("Việt Nam gia nhập", "28/7/1995", "Thành viên thứ 7"), ("Campuchia gia nhập", "4/1999", "ASEAN 10")]),
            ],
            [
                new("ASEAN được thành lập vào ngày nào?", "8/8/1967", "28/7/1995", "24/2/1976", "31/12/2015", 'A', "ASEAN ra đời ngày 8/8/1967."),
                new("ASEAN được thành lập tại thành phố nào?", "Giacácta", "Băng Cốc", "Manila", "Xingapo", 'B', "Tuyên bố Băng Cốc khai sinh ASEAN."),
                new("Việt Nam gia nhập ASEAN năm nào?", "1995", "1997", "1999", "1984", 'A', "Ngày 28/7/1995."),
                new("Nước gia nhập ASEAN muộn nhất trong giai đoạn 1984–1999 là", "Lào", "Mianma", "Campuchia", "Brunây", 'C', "Campuchia gia nhập năm 1999."),
                new("Hiệp ước Bali được kí kết năm nào?", "1967", "1976", "1995", "2007", 'B', "Hiệp ước Thân thiện và Hợp tác ở Đông Nam Á kí tại Bali năm 1976."),
            ],
            "Nêu hoàn cảnh ra đời và mục tiêu của ASEAN.",
            "Hoàn cảnh: nhu cầu hợp tác phát triển, hạn chế ảnh hưởng bên ngoài (1 điểm). Mục tiêu: hợp tác kinh tế, văn hoá, giữ hoà bình khu vực (1 điểm)."),

            new(5, "Cộng đồng ASEAN", 35,
            [
                new("Sự hình thành Cộng đồng ASEAN",
                    ["Hiến chương ASEAN được kí năm 2007 và có hiệu lực từ tháng 12/2008, tạo khuôn khổ pháp lí cho ASEAN.",
                     "Ngày 31/12/2015, Cộng đồng ASEAN chính thức được thành lập."]),
                new("Ba trụ cột của Cộng đồng ASEAN",
                    ["Cộng đồng ASEAN gồm ba trụ cột: Cộng đồng Chính trị – An ninh, Cộng đồng Kinh tế và Cộng đồng Văn hoá – Xã hội."],
                    "Phương châm của ASEAN: Một tầm nhìn, một bản sắc, một cộng đồng."),
            ],
            [
                new("Cộng đồng ASEAN chính thức được thành lập vào ngày nào?", "31/12/2015", "8/8/1967", "15/12/2008", "1/1/2020", 'A', "Cộng đồng ASEAN ra đời cuối năm 2015."),
                new("Nội dung nào không phải là trụ cột của Cộng đồng ASEAN?", "Chính trị – An ninh", "Kinh tế", "Văn hoá – Xã hội", "Quân sự", 'D', "Ba trụ cột: Chính trị – An ninh, Kinh tế, Văn hoá – Xã hội."),
                new("Hiến chương ASEAN được kí năm nào?", "2007", "1976", "2015", "1995", 'A', "Kí năm 2007, có hiệu lực từ 12/2008."),
                new("Phương châm của ASEAN là", "Một tầm nhìn, một bản sắc, một cộng đồng", "Đoàn kết, hợp tác, phát triển", "Hoà bình, độc lập, tự chủ", "Thống nhất trong đa dạng", 'A', "Đây là phương châm được ghi trong Hiến chương."),
            ]),
        ]),

        new("Chủ đề 3 · Cách mạng tháng Tám, chiến tranh giải phóng dân tộc",
        [
            new(6, "Cách mạng tháng Tám năm 1945", 45,
            [
                new("Thời cơ tổng khởi nghĩa",
                    ["Ngày 15/8/1945, Nhật Bản tuyên bố đầu hàng Đồng minh không điều kiện, quân Nhật ở Đông Dương hoang mang, rệu rã.",
                     "Hội nghị toàn quốc của Đảng (13–15/8) và Quốc dân Đại hội Tân Trào (16/8) quyết định phát động tổng khởi nghĩa giành chính quyền trong cả nước."]),
                new("Tổng khởi nghĩa giành chính quyền",
                    ["Từ ngày 14 đến 28/8/1945, tổng khởi nghĩa thắng lợi trong cả nước.",
                     "Ngày 2/9/1945, tại Quảng trường Ba Đình (Hà Nội), Chủ tịch Hồ Chí Minh đọc Tuyên ngôn độc lập, khai sinh nước Việt Nam Dân chủ Cộng hoà."],
                    "Cách mạng tháng Tám thành công nhờ chớp đúng thời cơ khi Nhật đầu hàng Đồng minh.",
                    [("Hà Nội", "19/8/1945", "Giành chính quyền"), ("Huế", "23/8/1945", "Giành chính quyền"), ("Sài Gòn", "25/8/1945", "Giành chính quyền"), ("Tuyên ngôn độc lập", "2/9/1945", "Quảng trường Ba Đình")]),
            ],
            [
                new("Nhân dân Hà Nội giành chính quyền vào ngày nào?", "19/8/1945", "23/8/1945", "25/8/1945", "2/9/1945", 'A', "Khởi nghĩa ở Hà Nội thắng lợi ngày 19/8/1945."),
                new("Quốc dân Đại hội họp ở đâu?", "Tân Trào (Tuyên Quang)", "Pác Bó (Cao Bằng)", "Hà Nội", "Định Hoá (Thái Nguyên)", 'A', "Quốc dân Đại hội họp ngày 16/8/1945 tại Tân Trào."),
                new("Thời cơ của Cách mạng tháng Tám xuất hiện khi", "Nhật Bản đầu hàng Đồng minh", "Nhật đảo chính Pháp", "Chiến tranh thế giới thứ hai bùng nổ", "Quân Đồng minh vào Đông Dương", 'A', "Nhật đầu hàng ngày 15/8/1945."),
                new("Trong ba thành phố Hà Nội, Huế, Sài Gòn, nơi giành chính quyền muộn nhất là", "Hà Nội", "Huế", "Sài Gòn", "Cả ba cùng ngày", 'C', "Sài Gòn giành chính quyền ngày 25/8/1945."),
                new("Chủ tịch Hồ Chí Minh đọc Tuyên ngôn độc lập vào ngày nào?", "19/8/1945", "2/9/1945", "6/1/1946", "19/12/1946", 'B', "Ngày 2/9/1945 tại Quảng trường Ba Đình."),
            ],
            "Phân tích thời cơ của Cách mạng tháng Tám năm 1945.",
            "Nêu thời cơ khách quan (Nhật đầu hàng, kẻ thù hoang mang) và chủ quan (lực lượng đã sẵn sàng) – mỗi ý 1 điểm."),

            new(7, "Kháng chiến chống thực dân Pháp (1945–1954)", 45,
            [
                new("Toàn quốc kháng chiến",
                    ["Đêm 19/12/1946, Chủ tịch Hồ Chí Minh ra Lời kêu gọi toàn quốc kháng chiến.",
                     "Đường lối kháng chiến: toàn dân, toàn diện, trường kì, dựa vào sức mình là chính."]),
                new("Các chiến dịch lớn",
                    ["Chiến dịch Việt Bắc thu – đông 1947 làm thất bại kế hoạch đánh nhanh thắng nhanh của Pháp.",
                     "Chiến dịch Biên giới thu – đông 1950 giúp ta giành thế chủ động trên chiến trường chính Bắc Bộ.",
                     "Chiến dịch Điện Biên Phủ (13/3 – 7/5/1954) toàn thắng, buộc Pháp kí Hiệp định Giơnevơ ngày 21/7/1954."],
                    "Chiến thắng Điện Biên Phủ đã đập tan kế hoạch Nava, tạo cơ sở cho thắng lợi tại Hội nghị Giơnevơ."),
            ],
            [
                new("Lời kêu gọi toàn quốc kháng chiến được ra vào thời gian nào?", "Đêm 19/12/1946", "Ngày 2/9/1945", "Ngày 6/3/1946", "Ngày 7/5/1954", 'A', "Đêm 19/12/1946."),
                new("Chiến dịch nào giúp ta giành thế chủ động trên chiến trường chính Bắc Bộ?", "Việt Bắc thu – đông 1947", "Biên giới thu – đông 1950", "Hoà Bình 1951–1952", "Điện Biên Phủ 1954", 'B', "Sau chiến dịch Biên giới 1950 ta giành thế chủ động."),
                new("Chiến dịch Điện Biên Phủ kết thúc thắng lợi vào ngày nào?", "7/5/1954", "13/3/1954", "21/7/1954", "19/12/1946", 'A', "Ngày 7/5/1954 tập đoàn cứ điểm bị tiêu diệt."),
                new("Hiệp định Giơnevơ về Đông Dương được kí ngày nào?", "21/7/1954", "6/3/1946", "27/1/1973", "7/5/1954", 'A', "Kí ngày 21/7/1954."),
                new("Nội dung nào không nằm trong đường lối kháng chiến chống Pháp?", "Toàn dân", "Toàn diện", "Trường kì", "Dựa vào viện trợ bên ngoài là chính", 'D', "Đường lối là dựa vào sức mình là chính."),
            ],
            "Nêu ý nghĩa lịch sử của chiến thắng Điện Biên Phủ năm 1954.",
            "Đập tan kế hoạch Nava, buộc Pháp kí Hiệp định Giơnevơ (1 điểm); cổ vũ phong trào giải phóng dân tộc trên thế giới (1 điểm)."),

            new(8, "Kháng chiến chống Mỹ, cứu nước (1954–1975)", 45,
            [
                new("Đồng khởi và Chiến tranh đặc biệt",
                    ["Phong trào Đồng khởi (1959–1960) đưa cách mạng miền Nam từ thế giữ gìn lực lượng sang thế tiến công, dẫn tới sự ra đời của Mặt trận Dân tộc giải phóng miền Nam Việt Nam (20/12/1960).",
                     "Chiến thắng Ấp Bắc (1/1963) chứng tỏ quân dân miền Nam có khả năng đánh bại chiến lược Chiến tranh đặc biệt của Mỹ."]),
                new("Đến Đại thắng mùa Xuân 1975",
                    ["Trận Điện Biên Phủ trên không cuối năm 1972 buộc Mỹ kí Hiệp định Pari ngày 27/1/1973 và rút quân khỏi miền Nam.",
                     "Cuộc Tổng tiến công và nổi dậy mùa Xuân 1975 gồm ba chiến dịch: Tây Nguyên, Huế – Đà Nẵng và Hồ Chí Minh. Ngày 30/4/1975, miền Nam hoàn toàn giải phóng."]),
            ],
            [
                new("Hiệp định Pari về Việt Nam được kí ngày nào?", "27/1/1973", "21/7/1954", "30/4/1975", "20/12/1960", 'A', "Kí ngày 27/1/1973."),
                new("Mặt trận Dân tộc giải phóng miền Nam Việt Nam ra đời ngày nào?", "20/12/1960", "17/1/1960", "2/9/1945", "1/1/1963", 'A', "Ra đời sau phong trào Đồng khởi."),
                new("Chiến dịch mở đầu cuộc Tổng tiến công và nổi dậy mùa Xuân 1975 là", "Chiến dịch Tây Nguyên", "Chiến dịch Huế – Đà Nẵng", "Chiến dịch Hồ Chí Minh", "Chiến dịch Đường 14 – Phước Long", 'A', "Chiến dịch Tây Nguyên mở màn tháng 3/1975."),
                new("Miền Nam hoàn toàn giải phóng vào ngày nào?", "30/4/1975", "2/5/1975", "27/1/1973", "10/3/1975", 'A', "Ngày 30/4/1975."),
                new("Chiến thắng Ấp Bắc (1/1963) chứng tỏ quân dân miền Nam có khả năng đánh bại chiến lược nào của Mỹ?", "Chiến tranh đặc biệt", "Chiến tranh cục bộ", "Việt Nam hoá chiến tranh", "Chiến tranh đơn phương", 'A', "Ấp Bắc là thắng lợi tiêu biểu chống Chiến tranh đặc biệt."),
            ],
            "Trình bày nội dung cơ bản của Hiệp định Pari năm 1973.",
            "Mỹ cam kết tôn trọng độc lập, chủ quyền, thống nhất, toàn vẹn lãnh thổ của Việt Nam (1 điểm); rút hết quân Mỹ, ngừng bắn (1 điểm)."),
        ]),

        new("Chủ đề 4 · Công cuộc Đổi mới từ năm 1986 đến nay",
        [
            new(10, "Khái quát về công cuộc Đổi mới", 35,
            [
                new("Đường lối đổi mới",
                    ["Đại hội đại biểu toàn quốc lần thứ VI của Đảng (tháng 12/1986) đề ra đường lối đổi mới toàn diện, trọng tâm là đổi mới kinh tế.",
                     "Nền kinh tế chuyển từ cơ chế tập trung, quan liêu, bao cấp sang cơ chế thị trường có sự quản lí của Nhà nước."]),
                new("Thành tựu",
                    ["Từ một nước thiếu lương thực, Việt Nam trở thành một trong những nước xuất khẩu gạo hàng đầu thế giới.",
                     "Việt Nam gia nhập Tổ chức Thương mại thế giới (WTO) năm 2007, hội nhập ngày càng sâu rộng với khu vực và thế giới."],
                    "Đại hội VI (12/1986) mở đầu công cuộc Đổi mới."),
            ],
            [
                new("Đường lối đổi mới được đề ra tại đại hội nào?", "Đại hội IV (1976)", "Đại hội V (1982)", "Đại hội VI (1986)", "Đại hội VII (1991)", 'C', "Đại hội VI tháng 12/1986."),
                new("Trọng tâm của công cuộc Đổi mới là đổi mới", "Kinh tế", "Chính trị", "Văn hoá", "Quốc phòng", 'A', "Đổi mới kinh tế là trọng tâm."),
                new("Việt Nam gia nhập WTO năm nào?", "1995", "2000", "2007", "2015", 'C', "Việt Nam là thành viên WTO từ năm 2007."),
                new("Cơ chế kinh tế mới được xác định trong công cuộc Đổi mới là", "Kinh tế thị trường có sự quản lí của Nhà nước", "Kế hoạch hoá tập trung", "Bao cấp", "Kinh tế tự cung tự cấp", 'A', "Chuyển sang cơ chế thị trường có sự quản lí của Nhà nước."),
            ]),
        ]),
    ]);

    internal static readonly KhoaMau DiaLi = new("dia-li-12",
        "Hiểu đặc điểm tự nhiên, dân cư, các ngành và các vùng kinh tế Việt Nam\nĐọc Atlat, nhận xét bảng số liệu và biểu đồ\nLàm tốt đề thi tốt nghiệp THPT môn Địa lí",
        "Học sinh lớp 12 ôn thi tốt nghiệp THPT môn Địa lí",
    [
        new("Chủ đề 1 · Địa lí tự nhiên Việt Nam",
        [
            new(1, "Vị trí địa lí và phạm vi lãnh thổ", 35,
            [
                new("Vị trí địa lí",
                    ["Việt Nam nằm ở rìa phía đông bán đảo Đông Dương, gần trung tâm khu vực Đông Nam Á.",
                     "Lãnh thổ nằm hoàn toàn trong vùng nội chí tuyến bán cầu Bắc và chủ yếu trong múi giờ số 7."]),
                new("Phạm vi lãnh thổ",
                    ["Vùng đất có diện tích khoảng 331 nghìn km², đường biên giới trên đất liền giáp Trung Quốc, Lào và Campuchia.",
                     "Đường bờ biển dài khoảng 3 260 km, từ Móng Cái (Quảng Ninh) đến Hà Tiên (Kiên Giang). Vùng biển gồm nội thuỷ, lãnh hải, vùng tiếp giáp lãnh hải, vùng đặc quyền kinh tế và thềm lục địa."],
                    "Lãnh hải rộng 12 hải lí tính từ đường cơ sở."),
            ],
            [
                new("Phần lớn lãnh thổ Việt Nam nằm trong múi giờ số mấy?", "Múi giờ số 6", "Múi giờ số 7", "Múi giờ số 8", "Múi giờ số 9", 'B', "Việt Nam dùng giờ GMT+7."),
                new("Đường bờ biển nước ta dài khoảng", "2 360 km", "3 260 km", "4 600 km", "1 650 km", 'B', "Khoảng 3 260 km từ Móng Cái đến Hà Tiên."),
                new("Lãnh hải nước ta rộng bao nhiêu hải lí?", "12 hải lí", "24 hải lí", "200 hải lí", "6 hải lí", 'A', "Lãnh hải rộng 12 hải lí."),
                new("Trên đất liền, Việt Nam không có chung biên giới với nước nào?", "Trung Quốc", "Lào", "Campuchia", "Thái Lan", 'D', "Việt Nam giáp Trung Quốc, Lào, Campuchia."),
                new("Lãnh thổ Việt Nam nằm hoàn toàn trong vùng", "Nội chí tuyến bán cầu Bắc", "Ôn đới bán cầu Bắc", "Xích đạo", "Nội chí tuyến bán cầu Nam", 'A', "Nằm giữa xích đạo và chí tuyến Bắc."),
            ],
            "Phân tích ý nghĩa của vị trí địa lí Việt Nam đối với phát triển kinh tế – xã hội.",
            "Thuận lợi giao lưu, hội nhập, phát triển kinh tế biển (1 điểm); khó khăn về thiên tai, bảo vệ chủ quyền (1 điểm)."),

            new(2, "Thiên nhiên nhiệt đới ẩm gió mùa", 40,
            [
                new("Khí hậu nhiệt đới ẩm",
                    ["Nhiệt độ trung bình năm trên 20°C (trừ vùng núi cao), tổng số giờ nắng từ 1 400 đến 3 000 giờ/năm.",
                     "Lượng mưa lớn, trung bình 1 500 – 2 000 mm/năm; độ ẩm không khí trên 80%."]),
                new("Hoạt động của gió mùa",
                    ["Gió mùa mùa đông (tháng 11 – 4) hoạt động mạnh ở miền Bắc, tạo nên một mùa đông lạnh.",
                     "Gió mùa mùa hạ (tháng 5 – 10) mang mưa lớn cho cả nước. Gió phơn Tây Nam gây thời tiết khô nóng ở Bắc Trung Bộ vào đầu mùa hạ."],
                    "Gió mùa mùa đông hoạt động từ tháng 11 đến tháng 4 năm sau."),
            ],
            [
                new("Nhiệt độ trung bình năm của nước ta (trừ vùng núi cao) là", "Trên 20°C", "Dưới 15°C", "Trên 30°C", "Từ 15 – 18°C", 'A', "Trên 20°C."),
                new("Lượng mưa trung bình năm của nước ta khoảng", "500 – 1 000 mm", "1 500 – 2 000 mm", "3 000 – 4 000 mm", "Dưới 500 mm", 'B', "Trung bình 1 500 – 2 000 mm/năm."),
                new("Độ ẩm không khí trung bình của nước ta", "Trên 80%", "Dưới 50%", "Khoảng 60%", "Khoảng 40%", 'A', "Độ ẩm trên 80%."),
                new("Vùng chịu ảnh hưởng mạnh nhất của gió phơn Tây Nam là", "Tây Bắc", "Bắc Trung Bộ", "Đồng bằng sông Cửu Long", "Đông Nam Bộ", 'B', "Gió phơn gây khô nóng mạnh ở Bắc Trung Bộ."),
                new("Gió mùa mùa đông hoạt động ở nước ta trong khoảng thời gian", "Tháng 11 – 4", "Tháng 5 – 10", "Tháng 1 – 6", "Quanh năm", 'A', "Từ tháng 11 đến tháng 4 năm sau."),
            ],
            "Trình bày hoạt động của gió mùa mùa đông ở nước ta.",
            "Nêu thời gian, nguồn gốc, hướng gió (1 điểm) và tác động tới thời tiết miền Bắc, miền Nam (1 điểm)."),

            new(3, "Sự phân hoá đa dạng của thiên nhiên", 35,
            [
                new("Phân hoá theo chiều Bắc – Nam",
                    ["Ranh giới là dãy Bạch Mã (khoảng vĩ tuyến 16°B).",
                     "Phần lãnh thổ phía Bắc có mùa đông lạnh; phần phía Nam mang sắc thái cận xích đạo gió mùa, nóng quanh năm với mùa mưa và mùa khô rõ rệt."]),
                new("Phân hoá theo độ cao",
                    ["Đai nhiệt đới gió mùa ở dưới 600 – 700 m (miền Bắc) và dưới 900 – 1 000 m (miền Nam).",
                     "Đai cận nhiệt đới gió mùa trên núi và đai ôn đới gió mùa trên núi (từ 2 600 m trở lên, chỉ có ở dãy Hoàng Liên Sơn)."]),
            ],
            [
                new("Ranh giới phân hoá thiên nhiên giữa phần lãnh thổ phía Bắc và phía Nam là", "Dãy Hoành Sơn", "Dãy Bạch Mã", "Dãy Trường Sơn", "Sông Cả", 'B', "Dãy Bạch Mã, khoảng vĩ tuyến 16°B."),
                new("Đai ôn đới gió mùa trên núi có độ cao từ", "1 600 m trở lên", "2 600 m trở lên", "900 m trở lên", "3 600 m trở lên", 'B', "Từ 2 600 m trở lên."),
                new("Phần lãnh thổ phía Nam mang sắc thái khí hậu", "Cận xích đạo gió mùa", "Ôn đới", "Cận nhiệt đới", "Nhiệt đới có mùa đông lạnh", 'A', "Nóng quanh năm, có mùa mưa và mùa khô."),
                new("Đai ôn đới gió mùa trên núi chỉ có ở", "Dãy Hoàng Liên Sơn", "Dãy Trường Sơn Nam", "Tây Nguyên", "Dãy Bạch Mã", 'A', "Chỉ có ở Hoàng Liên Sơn."),
            ]),
        ]),

        new("Chủ đề 2 · Địa lí dân cư",
        [
            new(6, "Dân số Việt Nam", 35,
            [
                new("Quy mô và thành phần dân tộc",
                    ["Năm 2023, dân số Việt Nam khoảng 100,3 triệu người, đứng thứ ba ở Đông Nam Á.",
                     "Việt Nam có 54 dân tộc, trong đó dân tộc Kinh chiếm khoảng 85% dân số."]),
                new("Phân bố dân cư",
                    ["Dân cư phân bố không đều: tập trung đông ở đồng bằng, ven biển; thưa thớt ở trung du và miền núi.",
                     "Đồng bằng sông Hồng có mật độ dân số cao nhất cả nước."],
                    "Phân bố dân cư chưa hợp lí ảnh hưởng tới sử dụng lao động và khai thác tài nguyên."),
            ],
            [
                new("Nước ta có bao nhiêu dân tộc?", "54", "45", "64", "50", 'A', "54 dân tộc."),
                new("Dân số Việt Nam năm 2023 khoảng", "80 triệu người", "90 triệu người", "100 triệu người", "120 triệu người", 'C', "Khoảng 100,3 triệu người."),
                new("Vùng có mật độ dân số cao nhất nước ta là", "Đồng bằng sông Hồng", "Đồng bằng sông Cửu Long", "Đông Nam Bộ", "Tây Nguyên", 'A', "Đồng bằng sông Hồng."),
                new("Về dân số, Việt Nam đứng thứ mấy ở Đông Nam Á?", "Thứ nhất", "Thứ hai", "Thứ ba", "Thứ tư", 'C', "Sau Inđônêxia và Philíppin."),
            ],
            "Nhận xét sự phân bố dân cư ở nước ta và giải thích nguyên nhân.",
            "Nhận xét: không đều giữa đồng bằng – miền núi, thành thị – nông thôn (1 điểm). Nguyên nhân: tự nhiên, lịch sử khai thác lãnh thổ, kinh tế (1 điểm)."),

            new(7, "Lao động và việc làm", 30,
            [
                new("Nguồn lao động",
                    ["Nguồn lao động nước ta dồi dào, cần cù, có kinh nghiệm sản xuất nông nghiệp.",
                     "Chất lượng lao động ngày càng được nâng lên nhưng tỉ lệ lao động đã qua đào tạo còn thấp."]),
                new("Cơ cấu lao động",
                    ["Cơ cấu lao động chuyển dịch theo hướng giảm tỉ trọng khu vực nông – lâm – thuỷ sản, tăng tỉ trọng khu vực công nghiệp – xây dựng và dịch vụ."]),
            ],
            [
                new("Cơ cấu lao động nước ta đang chuyển dịch theo hướng", "Giảm tỉ trọng nông – lâm – thuỷ sản", "Tăng tỉ trọng nông – lâm – thuỷ sản", "Giảm tỉ trọng dịch vụ", "Không thay đổi", 'A', "Giảm nông nghiệp, tăng công nghiệp và dịch vụ."),
                new("Hạn chế lớn của nguồn lao động nước ta là", "Tỉ lệ lao động qua đào tạo còn thấp", "Thiếu lao động", "Lao động không cần cù", "Không có kinh nghiệm", 'A', "Chất lượng lao động còn hạn chế."),
                new("Thế mạnh của nguồn lao động nước ta là", "Dồi dào, cần cù", "Trình độ rất cao", "Phân bố đều", "Tập trung ở miền núi", 'A', "Dồi dào, cần cù, ham học hỏi."),
            ]),

            new(8, "Đô thị hoá", 30,
            [
                new("Đặc điểm đô thị hoá",
                    ["Quá trình đô thị hoá diễn ra ngày càng nhanh, tỉ lệ dân thành thị tăng, khoảng 38% năm 2023.",
                     "Các đô thị lớn như Hà Nội, Thành phố Hồ Chí Minh là trung tâm kinh tế, văn hoá của cả nước."]),
                new("Ảnh hưởng của đô thị hoá",
                    ["Đô thị hoá thúc đẩy chuyển dịch cơ cấu kinh tế, tạo nhiều việc làm.",
                     "Đô thị hoá cũng gây sức ép về nhà ở, giao thông và môi trường."]),
            ],
            [
                new("Tỉ lệ dân thành thị nước ta năm 2023 khoảng", "18%", "28%", "38%", "58%", 'C', "Khoảng 38%."),
                new("Tác động tích cực của đô thị hoá là", "Thúc đẩy chuyển dịch cơ cấu kinh tế", "Gây ô nhiễm môi trường", "Ùn tắc giao thông", "Thiếu nhà ở", 'A', "Đô thị hoá thúc đẩy chuyển dịch cơ cấu kinh tế."),
                new("Vấn đề nào là hậu quả tiêu cực của đô thị hoá?", "Ô nhiễm môi trường", "Tạo việc làm", "Tăng thu nhập", "Phát triển dịch vụ", 'A', "Đô thị hoá gây sức ép môi trường."),
            ]),
        ]),

        new("Chủ đề 3 · Địa lí các ngành kinh tế",
        [
            new(12, "Vấn đề phát triển nông nghiệp", 40,
            [
                new("Nền nông nghiệp nhiệt đới",
                    ["Nông nghiệp nước ta là nền nông nghiệp nhiệt đới, cho phép trồng nhiều vụ trong năm và đa dạng cây trồng, vật nuôi.",
                     "Lúa là cây lương thực chính; Đồng bằng sông Cửu Long là vùng sản xuất lúa lớn nhất cả nước."]),
                new("Cây công nghiệp",
                    ["Cây công nghiệp lâu năm tập trung theo vùng: cà phê ở Tây Nguyên, cao su ở Đông Nam Bộ, chè ở Trung du và miền núi Bắc Bộ."]),
            ],
            [
                new("Vùng sản xuất lúa lớn nhất nước ta là", "Đồng bằng sông Hồng", "Đồng bằng sông Cửu Long", "Duyên hải Nam Trung Bộ", "Bắc Trung Bộ", 'B', "Đồng bằng sông Cửu Long."),
                new("Vùng trồng cà phê lớn nhất nước ta là", "Tây Nguyên", "Đông Nam Bộ", "Bắc Trung Bộ", "Tây Bắc", 'A', "Tây Nguyên."),
                new("Chè được trồng nhiều nhất ở vùng", "Trung du và miền núi Bắc Bộ", "Đồng bằng sông Cửu Long", "Đông Nam Bộ", "Duyên hải Nam Trung Bộ", 'A', "Trung du và miền núi Bắc Bộ."),
                new("Cao su được trồng nhiều nhất ở vùng", "Đông Nam Bộ", "Tây Bắc", "Đồng bằng sông Hồng", "Bắc Trung Bộ", 'A', "Đông Nam Bộ."),
            ]),
        ]),

        new("Chủ đề 4 · Địa lí các vùng kinh tế",
        [
            new(30, "Khai thác thế mạnh ở Tây Nguyên", 40,
            [
                new("Phát triển cây công nghiệp lâu năm",
                    ["Tây Nguyên có đất badan màu mỡ và khí hậu cận xích đạo, thuận lợi để trồng cây công nghiệp lâu năm.",
                     "Cà phê là cây công nghiệp quan trọng nhất của vùng. Khó khăn lớn là mùa khô kéo dài gây thiếu nước tưới."]),
                new("Thuỷ điện và khoáng sản",
                    ["Tây Nguyên có tiềm năng thuỷ điện lớn trên các sông Xê Xan, Xrê Pốk và thượng nguồn sông Đồng Nai.",
                     "Vùng có trữ lượng bôxit lớn."],
                    "Đất badan và khí hậu cận xích đạo là thế mạnh nổi bật của Tây Nguyên."),
            ],
            [
                new("Loại đất chiếm diện tích lớn và có giá trị nhất ở Tây Nguyên là", "Đất phù sa", "Đất badan", "Đất phèn", "Đất cát", 'B', "Đất badan màu mỡ."),
                new("Khó khăn lớn nhất về tự nhiên đối với trồng cây công nghiệp ở Tây Nguyên là", "Mùa khô kéo dài, thiếu nước", "Đất xấu", "Rét đậm", "Bão", 'A', "Mùa khô sâu sắc."),
                new("Hệ thống sông có tiềm năng thuỷ điện lớn ở Tây Nguyên là", "Sông Xê Xan", "Sông Hồng", "Sông Cả", "Sông Tiền", 'A', "Xê Xan, Xrê Pốk, Đồng Nai."),
                new("Khoáng sản có trữ lượng lớn ở Tây Nguyên là", "Bôxit", "Than đá", "Dầu khí", "Apatit", 'A', "Tây Nguyên giàu bôxit."),
            ],
            "Trình bày thế mạnh và hạn chế trong phát triển cây cà phê ở Tây Nguyên.",
            "Thế mạnh: đất badan, khí hậu, thị trường (1 điểm). Hạn chế: mùa khô thiếu nước, biến động giá (1 điểm)."),
        ]),
    ]);

    internal static readonly string[] HocVien =
    [
        "Nguyễn Minh Anh", "Trần Gia Bảo", "Lê Thu Hà", "Phạm Đức Huy", "Hoàng Ngọc Lan", "Vũ Quang Minh", "Đặng Khánh Linh",
        "Bùi Tuấn Kiệt", "Đỗ Phương Thảo", "Ngô Hải Nam", "Dương Mai Chi", "Lý Thành Đạt", "Phan Bảo Ngọc", "Trịnh Hoàng Long",
    ];
}
