using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SuDia.Web.Services;

public record TaiLieuOffice(string TenBai, string NoiDung, IReadOnlyList<string> CanhBao);

/// <summary>Đọc OOXML tại máy chủ, không chạy Office và không gửi file tới AI.</summary>
public static class DocTaiLieuOffice
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace P = "http://schemas.openxmlformats.org/presentationml/2006/main";
    private static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    public const int ToiDaByte = 20 * 1024 * 1024;

    public static TaiLieuOffice Doc(Stream stream, string extension, string tenFile)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        if (zip.Entries.Count > 10000 || zip.Entries.Sum(e => e.Length) > 50L * 1024 * 1024)
            throw new InvalidDataException("Tài liệu có quá nhiều nội dung. Anh chia thành file nhỏ hơn nhé.");
        XDocument Xml(string path)
        {
            var entry = zip.GetEntry(path) ?? throw new InvalidDataException("File thiếu thành phần bắt buộc hoặc chưa đúng định dạng Office.");
            using var input = entry.Open();
            using var reader = XmlReader.Create(input, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 30_000_000 });
            return XDocument.Load(reader);
        }
        var output = new StringBuilder();
        var warnings = new List<string>();
        var title = Path.GetFileNameWithoutExtension(tenFile);
        var images = false;
        var tables = false;
        if (extension == ".docx")
        {
            var doc = Xml("word/document.xml");
            var styles = zip.GetEntry("word/styles.xml") is null ? new Dictionary<string, XElement>() :
                Xml("word/styles.xml").Descendants(W + "style").Where(e => e.Attribute(W + "styleId") is not null)
                    .GroupBy(e => (string)e.Attribute(W + "styleId")!).ToDictionary(g => g.Key, g => g.First());
            int? StyleLevel(string? id, HashSet<string> seen)
            {
                if (id is null || !seen.Add(id) || !styles.TryGetValue(id, out var style)) return null;
                if (int.TryParse((string?)style.Element(W + "pPr")?.Element(W + "outlineLvl")?.Attribute(W + "val"), out var n)) return n < 9 ? n + 1 : null;
                if (id.StartsWith("Heading", StringComparison.OrdinalIgnoreCase) && int.TryParse(id[7..], out n) && n is >= 1 and <= 9) return n;
                return StyleLevel((string?)style.Element(W + "basedOn")?.Attribute(W + "val"), seen);
            }
            string Text(XElement e) => string.Concat(e.Descendants().Select(n => n.Name == W + "t" ? n.Value : n.Name == W + "tab" ? "\t" : n.Name == W + "br" ? "\n" : ""));
            var firstHeading = true;
            foreach (var block in doc.Root?.Element(W + "body")?.Elements() ?? [])
            {
                if (block.Name == W + "p")
                {
                    var text = Text(block).Trim();
                    if (text.Length == 0) continue;
                    var props = block.Element(W + "pPr");
                    var level = int.TryParse((string?)props?.Element(W + "outlineLvl")?.Attribute(W + "val"), out var direct)
                        ? direct < 9 ? direct + 1 : (int?)null
                        : StyleLevel((string?)props?.Element(W + "pStyle")?.Attribute(W + "val"), []);
                    if (level is not null)
                    {
                        if (firstHeading) { title = text; firstHeading = false; }
                        output.Append(new string('#', Math.Min(level.Value, 6))).Append(' ');
                    }
                    output.AppendLine(text).AppendLine();
                }
                else if (block.Name == W + "tbl")
                {
                    tables = true;
                    foreach (var row in block.Elements(W + "tr"))
                        output.AppendLine(string.Join(" | ", row.Elements(W + "tc").Select(c => string.Join(" / ", c.Elements(W + "p").Select(Text)))));
                    output.AppendLine();
                }
                else if (block.Descendants(W + "t").Any())
                {
                    warnings.Add("Một số nội dung nằm trong khối đặc biệt của Word cần kiểm tra lại trong file gốc.");
                    foreach (var p in block.Descendants(W + "p")) output.AppendLine(Text(p)).AppendLine();
                }
            }
            images = doc.Descendants(W + "drawing").Any() || doc.Descendants(W + "pict").Any();
            if (doc.Descendants(W + "numPr").Any()) warnings.Add("Tài liệu có đánh số tự động: chữ đã được lấy nhưng nhãn số tự động chưa được khôi phục. Anh kiểm tra lại thứ bậc các mục.");
        }
        else if (extension == ".pptx")
        {
            var doc = Xml("ppt/presentation.xml");
            XNamespace rel = "http://schemas.openxmlformats.org/package/2006/relationships";
            var links = Xml("ppt/_rels/presentation.xml.rels").Descendants(rel + "Relationship")
                .Where(e => (string?)e.Attribute("TargetMode") != "External")
                .ToDictionary(e => (string)e.Attribute("Id")!, e => (string)e.Attribute("Target")!);
            string? previousTitle = null;
            var index = 0;
            foreach (var slideId in doc.Descendants(P + "sldId"))
            {
                index++;
                if (!links.TryGetValue((string?)slideId.Attribute(R + "id") ?? "", out var target)) throw new InvalidDataException("Không đọc được thứ tự slide.");
                var path = new Uri(new Uri("https://office.local/ppt/presentation.xml"), target).AbsolutePath.TrimStart('/');
                var slide = Xml(path);
                var shapes = slide.Descendants(P + "sp").ToList();
                var heading = shapes.FirstOrDefault(s => s.Descendants(P + "ph").Any(ph => (string?)ph.Attribute("type") is "title" or "ctrTitle"));
                var headingText = heading is null ? "" : string.Join(" ", heading.Descendants(A + "t").Select(t => t.Value)).Trim();
                if (headingText.Length > 0 && headingText != previousTitle)
                {
                    output.Append("# ").AppendLine(headingText).AppendLine();
                    if (previousTitle is null) title = headingText;
                    previousTitle = headingText;
                }
                output.AppendLine($"[Slide {index}]");
                foreach (var shape in slide.Descendants().Where(e => e.Name == P + "sp" || e.Name == A + "tbl"))
                {
                    if (shape == heading) continue;
                    if (shape.Name == A + "tbl")
                    {
                        tables = true;
                        foreach (var row in shape.Elements(A + "tr")) output.AppendLine(string.Join(" | ", row.Elements(A + "tc").Select(c => string.Join(" ", c.Descendants(A + "t").Select(t => t.Value)))));
                        continue;
                    }
                    if (shape.Descendants(P + "ph").Any(ph => (string?)ph.Attribute("type") is "ftr" or "dt" or "sldNum")) continue;
                    foreach (var p in shape.Descendants(A + "p"))
                    {
                        var text = string.Concat(p.Descendants(A + "t").Select(t => t.Value)).Trim();
                        if (text.Length > 0) output.AppendLine(text);
                    }
                }
                images |= slide.Descendants(P + "pic").Any();
                output.AppendLine();
            }
            warnings.Add("Đã lấy chữ theo thứ tự slide; bố cục nhiều cột cần kiểm tra lại. Ghi chú thuyết trình, hiệu ứng, biểu đồ và video chưa được nhập.");
        }
        else throw new InvalidDataException("Anh chọn file .docx hoặc .pptx nhé.");
        if (images) warnings.Add("File có hình ảnh. Bản này lấy phần chữ, chưa nhận dạng chữ trong ảnh hoặc chèn hình vào bài. Anh đối chiếu file gốc để bổ sung.");
        if (tables) warnings.Add("Bảng được chuyển thành các dòng chữ, các ô cách nhau bằng dấu |. Anh kiểm tra lại bảng có ô gộp.");
        var content = output.ToString().Trim();
        if (content.Length < 30) throw new InvalidDataException("File chưa có đủ chữ để soạn bài. Nếu nội dung nằm trong ảnh, anh cần dùng bản có văn bản chọn/copy được.");
        if (content.Length > 30000) throw new InvalidDataException("Nội dung vượt 30.000 ký tự. Anh chia tài liệu theo từng bài rồi nhập lại; hệ thống không cắt bớt nội dung.");
        return new(title, content, warnings.Distinct().ToList());
    }
}
