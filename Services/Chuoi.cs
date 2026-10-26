using System.Text.RegularExpressions;
using SuDia.Web.Services.TroLy;

namespace SuDia.Web.Services;

public static class Chuoi
{
    /// <summary>"Ý nghĩa lịch sử" → "y-nghia-lich-su" (dùng làm neo #muc-3-…)</summary>
    public static string TaoMaNeo(string s)
    {
        var ma = Regex.Replace(KhoTaiLieu.BoDau(s), "[^a-z0-9]+", "-").Trim('-');
        return ma.Length == 0 ? "phan" : ma.Length > 40 ? ma[..40].TrimEnd('-') : ma;
    }
}
