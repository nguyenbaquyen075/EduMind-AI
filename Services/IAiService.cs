using SuDia.Web.Models;

namespace SuDia.Web.Services;

/// <summary>
/// Trợ lý AI trả lời theo tài liệu khoá học.
/// Bản này là bản mẫu (DemoAiService). Khi làm thật, viết RagAiService:
/// tìm đoạn tài liệu (pgvector + full-text) -> rerank -> gọi LLM với prompt hệ thống -> kiểm tra số liệu.
/// Xem tài liệu dự án mục 5-8 và file prompt-he-thong-tro-ly.md.
/// </summary>
public interface IAiService
{
    Task<HoiAiResponse> HoiAsync(HoiAiRequest request, CancellationToken ct = default);
}
