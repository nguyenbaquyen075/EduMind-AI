using System.Text.RegularExpressions;

namespace SuDia.Web.Services;

public static partial class SoDienThoai
{
    [GeneratedRegex("^0(3|5|7|8|9)\\d{8}$")]
    private static partial Regex SoVietNam();
    public static bool ThuChuanHoa(string? input, out string so)
    {
        var s = Regex.Replace(input ?? "", "[ .-]", "");
        if (s.StartsWith("+84")) s = "0" + s[3..]; else if (s.StartsWith("84")) s = "0" + s[2..];
        if (!SoVietNam().IsMatch(s)) { so = ""; return false; }
        so = "+84" + s[1..]; return true;
    }
    public static string Che(string? so) => string.IsNullOrEmpty(so) ? "" : so.Length <= 8 ? "****" : so[..3] + "****" + so[^4..];
}
