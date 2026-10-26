using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SuDia.Web.Services.TroLy;

/// <param name="Khoa">Mã khoá, ví dụ "lich-su-12"</param>
/// <param name="Mon">"Lịch sử 12"</param>
/// <param name="Nguon">"Lịch sử 12 · Bài 7 · Mục 3 — Chiến dịch Điện Biên Phủ"</param>
public record DoanTaiLieu(string Khoa, string Mon, string Nguon, string NoiDung);

/// <summary>
/// Nạp tài liệu khoá (.md) và tìm đoạn liên quan đến câu hỏi.
/// Cắt đoạn theo Heading 3 (Mục) như quy chuẩn ở Phần 5 của prompt-he-thong-tro-ly.md.
/// Tìm kiếm BM25 trên chữ đã bỏ dấu (gõ không dấu vẫn tìm được), dùng cả cặp từ ghép
/// vì tiếng Việt là ngôn ngữ đơn âm tiết ("dien bien", "bien phu").
/// Khi có database: thay bằng pgvector + full-text, giữ nguyên giao diện TimKiem.
/// </summary>
public class KhoTaiLieu
{
    private const int DoDaiToiDa = 1500;   // ký tự mỗi đoạn
    private const double K1 = 1.2, B = 0.75;

    private static readonly HashSet<string> TuDung =
    [
        "la", "cua", "va", "cac", "nhung", "mot", "nao", "gi", "khi", "the", "thi", "sao", "bao", "nhieu",
        "co", "khong", "duoc", "trong", "voi", "cho", "ve", "tu", "den", "o", "ai", "hay", "hoac", "nay",
        "do", "da", "dang", "se", "bi", "boi", "ra", "vao", "len", "nhu", "ma", "con", "cung", "rat",
        "nhe", "a", "oi", "vi", "de", "theo", "minh", "em", "ban", "biet", "hoi",
    ];

    private readonly List<DoanTaiLieu> _doan = [];
    private readonly List<Dictionary<string, int>> _tanSo = [];
    private readonly List<int> _doDai = [];
    private readonly Dictionary<string, int> _soDoanChua = [];
    private double _doDaiTrungBinh;

    public IReadOnlyList<DoanTaiLieu> TatCa => _doan;

    public KhoTaiLieu(string thuMuc)
    {
        if (Directory.Exists(thuMuc))
            foreach (var file in Directory.GetFiles(thuMuc, "*.md").Order())
                NapFile(file);

        foreach (var d in _doan)
        {
            var tu = TachTu(d.Nguon + "\n" + d.NoiDung);
            var tf = new Dictionary<string, int>();
            foreach (var t in tu) tf[t] = tf.GetValueOrDefault(t) + 1;
            foreach (var t in tf.Keys) _soDoanChua[t] = _soDoanChua.GetValueOrDefault(t) + 1;
            _tanSo.Add(tf);
            _doDai.Add(tu.Count);
        }
        _doDaiTrungBinh = _doDai.Count == 0 ? 1 : Math.Max(1, _doDai.Average());
    }

    /// <summary>Trả về tối đa <paramref name="soDoan"/> đoạn liên quan nhất, điểm giảm dần.</summary>
    public IReadOnlyList<DoanTaiLieu> TimKiem(string cauHoi, string? khoa, int soDoan)
    {
        var tuHoi = TachTu(cauHoi).Distinct().ToList();
        if (tuHoi.Count == 0) return [];

        var n = _doan.Count;
        var ketQua = new List<(int I, double Diem)>();
        for (var i = 0; i < n; i++)
        {
            if (khoa is not null && _doan[i].Khoa != khoa) continue;
            double diem = 0;
            foreach (var t in tuHoi)
            {
                if (!_tanSo[i].TryGetValue(t, out var tf)) continue;
                var df = _soDoanChua[t];
                var idf = Math.Log(1 + (n - df + 0.5) / (df + 0.5));
                diem += idf * tf * (K1 + 1) / (tf + K1 * (1 - B + B * _doDai[i] / _doDaiTrungBinh));
            }
            if (diem > 0) ketQua.Add((i, diem));
        }

        return ketQua.OrderByDescending(x => x.Diem).Take(soDoan).Select(x => _doan[x.I]).ToList();
    }

    // ---------------------------------------------------------------- Nạp file

    private void NapFile(string duongDan)
    {
        var dong = File.ReadAllLines(duongDan);
        var batDau = 0;
        var thongTin = new Dictionary<string, string>();
        if (dong.Length > 0 && dong[0].Trim() == "---")
        {
            for (batDau = 1; batDau < dong.Length && dong[batDau].Trim() != "---"; batDau++)
            {
                var p = dong[batDau].Split(':', 2);
                if (p.Length == 2) thongTin[p[0].Trim()] = p[1].Trim();
            }
            batDau++;
        }

        var tenFile = Path.GetFileNameWithoutExtension(duongDan);
        var khoa = thongTin.GetValueOrDefault("khoa", tenFile);
        var mon = thongTin.GetValueOrDefault("mon", tenFile);

        string? bai = null, muc = null;
        var noiDung = new List<string>();

        void Chot()
        {
            var text = string.Join('\n', noiDung).Trim();
            noiDung.Clear();
            if (text.Length == 0) return;
            var nguon = TaoNguon(mon, bai, muc);
            foreach (var phan in ChiaNho(text))
                _doan.Add(new DoanTaiLieu(khoa, mon, nguon, phan));
        }

        for (var i = batDau; i < dong.Length; i++)
        {
            var d = dong[i];
            if (d.StartsWith("### ")) { Chot(); muc = d[4..].Trim(); }
            else if (d.StartsWith("## ")) { Chot(); bai = d[3..].Trim(); muc = null; }
            else if (d.StartsWith("# ")) { Chot(); bai = null; muc = null; }
            else noiDung.Add(d);
        }
        Chot();
    }

    // "Bài 7. Kháng chiến…" + "3. Chiến dịch Điện Biên Phủ" -> "Lịch sử 12 · Bài 7 · Mục 3 — Chiến dịch Điện Biên Phủ"
    private static string TaoNguon(string mon, string? bai, string? muc)
    {
        var sb = new StringBuilder(mon);
        if (bai is not null)
        {
            var m = Regex.Match(bai, @"^(Bài\s+\d+)[.:]?\s*(.*)$");
            sb.Append(" · ").Append(m.Success ? m.Groups[1].Value : bai);
            if (muc is null && m.Success && m.Groups[2].Length > 0) sb.Append(" — ").Append(m.Groups[2].Value);
        }
        if (muc is not null)
        {
            var m = Regex.Match(muc, @"^(\d+)[.)]\s*(.+)$");
            sb.Append(" · ").Append(m.Success ? $"Mục {m.Groups[1].Value} — {m.Groups[2].Value}" : muc);
        }
        return sb.ToString();
    }

    // Mục dài thì chia theo đoạn văn, mỗi phần ≤ DoDaiToiDa ký tự
    private static IEnumerable<string> ChiaNho(string text)
    {
        if (text.Length <= DoDaiToiDa) { yield return text; yield break; }
        var sb = new StringBuilder();
        foreach (var para in text.Split("\n\n"))
        {
            if (sb.Length > 0 && sb.Length + para.Length > DoDaiToiDa)
            {
                yield return sb.ToString().Trim();
                sb.Clear();
            }
            sb.Append(para).Append("\n\n");
        }
        if (sb.Length > 0) yield return sb.ToString().Trim();
    }

    // ---------------------------------------------------------------- Tách từ

    public static string BoDau(string s)
    {
        var tach = s.ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(tach.Length);
        foreach (var c in tach)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString();
    }

    /// <summary>Âm tiết (bỏ từ dừng) + cặp âm tiết liền nhau, đều đã bỏ dấu.</summary>
    private static List<string> TachTu(string s)
    {
        var amTiet = Regex.Split(BoDau(s), @"[^a-z0-9]+").Where(t => t.Length > 0).ToList();
        var kq = new List<string>();
        for (var i = 0; i < amTiet.Count; i++)
        {
            if (!TuDung.Contains(amTiet[i])) kq.Add(amTiet[i]);
            if (i + 1 < amTiet.Count) kq.Add(amTiet[i] + "_" + amTiet[i + 1]);
        }
        return kq;
    }
}
