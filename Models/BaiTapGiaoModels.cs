using System.Text.Json;

namespace SuDia.Web.Models;

public class YDungSai
{
    public string NoiDung { get; set; } = "";
    public bool? Dung { get; set; }
    public decimal Diem { get; set; } = .25m;
}

public class CauBaiTap
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Kieu { get; set; } = "TN";
    public string NoiDung { get; set; } = "";
    public decimal Diem { get; set; } = 1;
    public List<string> PhuongAn { get; set; } = ["", "", "", ""];
    public int? DapAn { get; set; }
    public List<YDungSai> Y { get; set; } = [];
    public string LoiGiai { get; set; } = "";
    public decimal TongDiem => Kieu == "DS" ? Y.Sum(y => y.Diem) : Diem;
}

public class GiaoBaiForm
{
    public int Id { get; set; }
    public int BaiId { get; set; }
    public int? BuoiHocId { get; set; }
    public string Ten { get; set; } = "";
    public string HuongDan { get; set; } = "";
    public DateTimeOffset? HanNop { get; set; }
    public bool DaGiao { get; set; }
    public bool LaKiemTra { get; set; }
    public int? DeThiId { get; set; }
    [System.ComponentModel.DataAnnotations.Range(5, 240)]
    public int SoPhut { get; set; } = 45;
    public List<CauBaiTap> Cau { get; set; } = [];
}

public class LamBaiGiaoViewModel
{
    public required SuDia.Web.Data.BaiTapGiao BaiTap { get; init; }
    public required List<CauBaiTap> Cau { get; init; }
    public SuDia.Web.Data.BaiNopGiao? BaiNop { get; init; }
    public Dictionary<string, string> TraLoi { get; init; } = [];
    public bool XemTruoc { get; init; }
    public DateTime? HetGio { get; init; }
    public bool CanBatDau => BaiTap.DeThi is not null && !XemTruoc && BaiNop is null && HetGio is null;
    public decimal TongDiem => Cau.Sum(c => c.TongDiem);
}

public static class NoiDungBaiTap
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static List<CauBaiTap> Doc(string json) => JsonSerializer.Deserialize<List<CauBaiTap>>(json, Json) ?? [];
    public static string Viet<T>(T value) => JsonSerializer.Serialize(value, Json);

    public static string? KiemTra(List<CauBaiTap>? cau, bool xuatBan)
    {
        if (cau is null || cau.Count > 200) return "Mỗi bài tập tối đa 200 câu.";
        if (xuatBan && cau.Count == 0) return "Thêm ít nhất một câu hỏi trước khi giao bài.";
        if (cau.Any(c => c is null || string.IsNullOrWhiteSpace(c.Id) || c.Id.Length > 64) || cau.Select(c => c.Id).Distinct().Count() != cau.Count)
            return "Mã câu hỏi bị trùng hoặc không hợp lệ.";
        foreach (var c in cau)
        {
            if (c.Kieu is not ("TN" or "DS" or "TL")) return "Loại câu hỏi không hợp lệ.";
            if (c.NoiDung is null || c.NoiDung.Length > 20000 || c.LoiGiai is null || c.LoiGiai.Length > 20000)
                return "Nội dung mỗi câu tối đa 20.000 ký tự.";
            if (xuatBan && string.IsNullOrWhiteSpace(c.NoiDung)) return "Nhập nội dung cho tất cả câu hỏi.";
            if (c.Kieu == "DS")
            {
                if (c.Y is null || c.Y.Count != 4 || c.Y.Any(y => y is null || y.NoiDung is null || y.NoiDung.Length > 10000 || y.Diem <= 0 || y.Diem > 100))
                    return "Câu Đúng / Sai cần 4 ý, điểm mỗi ý lớn hơn 0 và không quá 100.";
                if (xuatBan && c.Y.Any(y => string.IsNullOrWhiteSpace(y.NoiDung) || y.Dung is null)) return "Nhập đủ 4 ý và chọn Đúng / Sai cho từng ý.";
            }
            else if (c.Diem <= 0 || c.Diem > 100) return "Điểm mỗi câu phải lớn hơn 0 và không quá 100.";
            if (c.Kieu == "TN")
            {
                if (c.PhuongAn is null || c.PhuongAn.Count != 4 || c.PhuongAn.Any(p => p is null || p.Length > 10000)) return "Câu trắc nghiệm cần 4 phương án.";
                if (xuatBan && (c.PhuongAn.Any(string.IsNullOrWhiteSpace) || c.DapAn is null or < 0 or > 3)) return "Nhập đủ 4 phương án và chọn đáp án đúng.";
            }
        }
        return cau.Sum(c => c.TongDiem) > 1000 ? "Tổng điểm bài tập không quá 1.000." : null;
    }

    public static decimal Cham(IEnumerable<CauBaiTap> cau, IReadOnlyDictionary<string, string> traLoi) => cau.Sum(c =>
        c.Kieu == "TN" ? (traLoi.GetValueOrDefault(c.Id) == c.DapAn?.ToString() ? c.Diem : 0) :
        c.Kieu == "DS" ? c.Y.Select((y, i) => traLoi.GetValueOrDefault($"{c.Id}-{i}") == (y.Dung == true ? "true" : "false") ? y.Diem : 0).Sum() : 0);
}
