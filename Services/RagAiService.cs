using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using SuDia.Web.Models;
using SuDia.Web.Services.TroLy;
using Beta = Anthropic.Models.Beta.Messages;

namespace SuDia.Web.Services;

/// <summary>
/// Trợ lý trả lời theo tài liệu khoá (RAG), làm đúng theo Prompts/prompt-he-thong-tro-ly.md:
/// 1. Câu hỏi nối tiếp -> viết lại thành câu đầy đủ (model phụ)
/// 2. Tìm 5–6 đoạn tài liệu liên quan (KhoTaiLieu)
/// 3. Gọi model với prompt hệ thống (Phần 1) + tin nhắn theo mẫu (Phần 2)
/// 4. Kiểm tra bằng code: số liệu và số nguồn [n] phải có trong đoạn được trích
/// 5. AI chấm (Phần 4, model phụ). Không đạt -> trả câu từ chối, ghi log cho Admin
/// </summary>
public class RagAiService : IAiService
{
    private const string LoiTamThoi = "Trợ lý đang bận, bạn vui lòng thử lại sau ít phút.";

    private readonly AnthropicClient _client;
    private readonly KhoTaiLieu _kho;
    private readonly PromptTroLy _prompt;
    private readonly TroLyOptions _opt;
    private readonly ILogger<RagAiService> _log;

    public RagAiService(AnthropicClient client, KhoTaiLieu kho, PromptTroLy prompt,
        IOptions<TroLyOptions> opt, ILogger<RagAiService> log)
    {
        _client = client;
        _kho = kho;
        _prompt = prompt;
        _opt = opt.Value;
        _log = log;
    }

    public async Task<HoiAiResponse> HoiAsync(HoiAiRequest request, CancellationToken ct = default)
    {
        try
        {
            return await HoiCoreAsync(request, ct);
        }
        catch (AnthropicRateLimitException ex)
        {
            _log.LogWarning(ex, "Trợ lý: vượt giới hạn gọi API");
            return Loi(LoiTamThoi);
        }
        catch (Anthropic5xxException ex)
        {
            _log.LogWarning(ex, "Trợ lý: API lỗi máy chủ");
            return Loi(LoiTamThoi);
        }
        catch (AnthropicApiException ex)
        {
            _log.LogError(ex, "Trợ lý: API từ chối yêu cầu (kiểm tra khoá API, tên model)");
            return Loi("Trợ lý đang gặp sự cố. Vui lòng thử lại sau.");
        }
        catch (AnthropicIOException ex)
        {
            _log.LogWarning(ex, "Trợ lý: không kết nối được API");
            return Loi(LoiTamThoi);
        }
    }

    private async Task<HoiAiResponse> HoiCoreAsync(HoiAiRequest request, CancellationToken ct)
    {
        // 1. Viết lại câu hỏi nối tiếp
        var lichSu = (request.LichSu ?? []).TakeLast(_opt.SoLuotLichSu).ToList();
        var cauHoi = lichSu.Count == 0 ? request.CauHoi.Trim() : await VietLaiCauHoiAsync(request.CauHoi, lichSu, ct);

        // 2. Tìm đoạn tài liệu. Không có đoạn nào liên quan thì từ chối luôn, khỏi gọi model
        var doan = _kho.TimKiem(cauHoi, request.KhoaId, _opt.SoDoan);
        if (doan.Count == 0) return TuChoi();

        // 3. Gọi model trả lời
        var traLoi = await TraLoiAsync(doan, cauHoi, ct);
        if (traLoi is null) return TuChoi();

        // 4. Kiểm tra bằng code
        var kq = KiemTraCauTraLoi.KiemTra(traLoi, doan);
        if (!kq.Dat)
        {
            GhiLogKhongDat(cauHoi, traLoi, string.Join(" ", kq.Loi));
            return TuChoi();
        }

        // Từ chối (ngoài tài liệu / ngoài phạm vi) không trích [n] nào: hiện nguyên văn kèm gợi ý
        if (kq.NguonDaDung.Count == 0)
            return new HoiAiResponse("Trợ lý", kq.ThanBai, "", "", true);

        // 5. AI chấm
        if (_opt.BatAiCham)
        {
            var cham = await AiChamAsync(doan, kq.ThanBai, ct);
            if (cham is not null && cham.Value.KetLuan != "DAT")
            {
                GhiLogKhongDat(cauHoi, traLoi, cham.Value.ChiTiet);
                return TuChoi();
            }
        }

        var dung = kq.NguonDaDung.Select(n => doan[n - 1]).ToList();
        var nguon = string.Join("\n", kq.NguonDaDung.Select(n => $"[{n}] {doan[n - 1].Nguon}"));
        return new HoiAiResponse("Trợ lý · " + dung[0].Mon, kq.ThanBai, "", nguon, false);
    }

    // ---------------------------------------------------------------- Gọi model

    private async Task<string?> TraLoiAsync(IReadOnlyList<DoanTaiLieu> doan, string cauHoi, CancellationToken ct)
    {
        var res = await _client.Beta.Messages.Create(new Beta.MessageCreateParams
        {
            Model = _opt.ModelTraLoi,
            MaxTokens = _opt.MaxTokens,
            // Nếu bộ lọc an toàn của model từ chối nhầm (hiếm), server tự chạy lại bằng model dự phòng
            Betas = ["server-side-fallback-2026-07-01"],
            Fallbacks = new Beta.Default(),
            System = _prompt.HeThong,
            OutputConfig = new Beta.BetaOutputConfig { Effort = MucSuyNghiBeta() },
            Messages = [new Beta.BetaUserMessageParam { Content = PromptTroLy.GhepTinNhan(doan, cauHoi) }],
        }, ct);

        if (res.StopReason == "refusal")
        {
            _log.LogWarning("Trợ lý: model từ chối câu hỏi \"{CauHoi}\"", cauHoi);
            return null;
        }
        if (res.StopReason == "max_tokens")
            _log.LogWarning("Trợ lý: câu trả lời bị cắt do MaxTokens={Max}", _opt.MaxTokens);

        var text = string.Concat(res.Content.Select(b => b.Value).OfType<Beta.BetaTextBlock>().Select(t => t.Text)).Trim();
        return text.Length == 0 ? null : text;
    }

    private async Task<string> VietLaiCauHoiAsync(string cauHoi, IReadOnlyList<LuotHoi> lichSu, CancellationToken ct)
    {
        var hoiThoai = string.Join("\n", lichSu.Select(l => $"Học sinh: {l.CauHoi}\nTrợ lý: {l.TraLoi}"));
        var res = await _client.Messages.Create(new MessageCreateParams
        {
            Model = _opt.ModelPhu,
            MaxTokens = 1000,
            System = "Bạn viết lại câu hỏi cuối của học sinh thành một câu hỏi đầy đủ, tự hiểu được mà không cần đọc hội thoại "
                   + "(nêu rõ sự kiện, đợt, vùng, giai đoạn được nhắc tới trước đó). Không trả lời câu hỏi. "
                   + "Nếu câu hỏi đã đầy đủ thì giữ nguyên. Chỉ trả về đúng một câu hỏi, không giải thích.",
            OutputConfig = new OutputConfig { Effort = Effort.Low },
            Messages = [new UserMessageParam { Content = $"<hoi_thoai>\n{hoiThoai}\n</hoi_thoai>\n\n<cau_hoi_cuoi>\n{cauHoi}\n</cau_hoi_cuoi>" }],
        }, ct);

        var text = string.Concat(res.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text)).Trim();
        return text.Length is 0 or > 500 ? cauHoi : text;
    }

    private async Task<(string KetLuan, string ChiTiet)?> AiChamAsync(IReadOnlyList<DoanTaiLieu> doan, string thanBai, CancellationToken ct)
    {
        var res = await _client.Messages.Create(new MessageCreateParams
        {
            Model = _opt.ModelPhu,
            MaxTokens = 4000,
            System = _prompt.AiCham,
            OutputConfig = new OutputConfig { Effort = Effort.Low, Format = new JsonOutputFormat { Schema = SchemaCham } },
            Messages = [new UserMessageParam { Content = PromptTroLy.GhepTinNhanCham(doan, thanBai) }],
        }, ct);

        var json = string.Concat(res.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text));
        try
        {
            using var doc = JsonDocument.Parse(json);
            return (doc.RootElement.GetProperty("ket_luan").GetString() ?? "KHONG_DAT", json);
        }
        catch (JsonException)
        {
            // AI chấm hỏng thì vẫn còn lớp kiểm tra bằng code đã qua; ghi log để xem lại
            _log.LogWarning("Trợ lý: AI chấm trả JSON không hợp lệ: {Json}", json);
            return null;
        }
    }

    private static readonly Dictionary<string, JsonElement> SchemaCham = new()
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
        ["required"] = JsonSerializer.SerializeToElement(new[] { "cac_cau", "so_lieu_khong_co_trong_tai_lieu", "ket_luan" }),
        ["properties"] = JsonSerializer.SerializeToElement(new Dictionary<string, object>
        {
            ["cac_cau"] = new
            {
                type = "array",
                items = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["additionalProperties"] = false,
                    ["required"] = new[] { "cau", "duoc_ho_tro", "doan" },
                    ["properties"] = new
                    {
                        cau = new { type = "string" },
                        duoc_ho_tro = new { type = "boolean" },
                        doan = new { type = "array", items = new { type = "integer" } },
                    },
                },
            },
            ["so_lieu_khong_co_trong_tai_lieu"] = new { type = "array", items = new { type = "string" } },
            ["ket_luan"] = new { type = "string", @enum = new[] { "DAT", "KHONG_DAT" } },
        }),
    };

    // ---------------------------------------------------------------- Tiện ích

    private Beta.Effort MucSuyNghiBeta() => _opt.MucSuyNghi switch
    {
        "low" => Beta.Effort.Low,
        "high" => Beta.Effort.High,
        _ => Beta.Effort.Medium,
    };

    private void GhiLogKhongDat(string cauHoi, string traLoi, string lyDo) =>
        _log.LogWarning("Trợ lý KHONG_DAT. Câu hỏi: {CauHoi}\nTrả lời: {TraLoi}\nLý do: {LyDo}", cauHoi, traLoi, lyDo);

    private static HoiAiResponse TuChoi() => new("Trợ lý", PromptTroLy.CauTuChoi, "", "", true);

    private static HoiAiResponse Loi(string noiDung) => new("Tạm dừng", noiDung, "", "", true);
}
