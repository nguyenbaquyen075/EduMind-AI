namespace SuDia.Web.Data;

// Một bài học có nhiều bài tập được giao riêng cho từng buổi.
public class BaiTapGiao
{
    public int Id { get; set; }
    public int BaiId { get; set; }
    public Bai? Bai { get; set; }
    public int? BuoiHocId { get; set; }
    public BuoiHoc? BuoiHoc { get; set; }
    public string Ten { get; set; } = "";
    public string HuongDan { get; set; } = "";
    public DateTime? HanNop { get; set; }
    public bool DaGiao { get; set; }
    public string NoiDungJson { get; set; } = "[]";
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}

public class BaiNopGiao
{
    public int Id { get; set; }
    public int BaiTapGiaoId { get; set; }
    public BaiTapGiao? BaiTapGiao { get; set; }
    public string NguoiDungId { get; set; } = "";
    public NguoiDung? NguoiDung { get; set; }
    public string TraLoiJson { get; set; } = "{}";
    public decimal DiemTuDong { get; set; }
    public decimal? DiemTuLuan { get; set; }
    public DateTime NopLuc { get; set; } = DateTime.UtcNow;
}
