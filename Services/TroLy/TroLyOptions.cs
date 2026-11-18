namespace SuDia.Web.Services.TroLy;

/// <summary>Mục "TroLy" trong appsettings.json.</summary>
public class TroLyOptions
{
    public string ThuMucTaiLieu { get; set; } = "TaiLieu";
    public string FilePrompt { get; set; } = "Prompts/prompt-he-thong-tro-ly.md";

    /// <summary>Nhà cung cấp AI cho trợ lý: "Gemini" (gói miễn phí Google AI Studio, khoá GEMINI_API_KEY)
    /// hoặc "Claude" (Anthropic, khoá ANTHROPIC_API_KEY). Thiếu khoá của nhà cung cấp đã chọn thì dùng bên còn lại.</summary>
    public string NhaCungCap { get; set; } = "Gemini";

    /// <summary>Model Gemini dùng cho trợ lý (trả lời, viết lại câu hỏi, AI chấm).</summary>
    public string ModelGemini { get; set; } = "gemini-3-flash-preview";

    /// <summary>Model Gemini thử tiếp khi model chính quá tải / hết lượt miễn phí (mỗi model có hạn mức riêng).</summary>
    public string[] ModelGeminiDuPhong { get; set; } = ["gemini-flash-lite-latest", "gemini-3.8-flash"];

    /// <summary>Mức suy nghĩ của Gemini: "low" (nhanh, đủ cho đọc tài liệu rồi diễn đạt) | "medium" | "high"; rỗng = mặc định của model.</summary>
    public string MucSuyNghiGemini { get; set; } = "low";

    /// <summary>Model trả lời học sinh.</summary>
    public string ModelTraLoi { get; set; } = "claude-opus-5-5";

    /// <summary>Model rẻ hơn: viết lại câu hỏi nối tiếp và AI chấm (Phần 4).</summary>
    public string ModelPhu { get; set; } = "claude-haiku-5-5";

    /// <summary>Model phân tích và biên tập tài liệu giảng viên.</summary>
    public string ModelSoanBai { get; set; } = "claude-sonnet-5-5";

    /// <summary>low | medium | high. Độ kỹ khi đọc tài liệu; cao hơn thì chậm và tốn hơn.</summary>
    public string MucSuyNghi { get; set; } = "medium";

    /// <summary>Gồm cả phần suy nghĩ của model, nên để rộng hơn 800 ở Phần 2.</summary>
    public int MaxTokens { get; set; } = 4000;

    public int SoDoan { get; set; } = 6;
    public int SoLuotLichSu { get; set; } = 6;
    public bool BatAiCham { get; set; } = true;

    /// <summary>Tài liệu chưa có thì trợ lý vẫn trả lời bằng hiểu biết chung của AI, ghi rõ "ngoài tài liệu khoá học".
    /// Tắt (false) để chỉ trả lời theo tài liệu.</summary>
    public bool ChoPhepNgoaiTaiLieu { get; set; } = true;
}
