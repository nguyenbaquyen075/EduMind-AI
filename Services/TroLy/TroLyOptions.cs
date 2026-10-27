namespace SuDia.Web.Services.TroLy;

/// <summary>Mục "TroLy" trong appsettings.json.</summary>
public class TroLyOptions
{
    public string ThuMucTaiLieu { get; set; } = "TaiLieu";
    public string FilePrompt { get; set; } = "Prompts/prompt-he-thong-tro-ly.md";

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
    public int SoLuotLichSu { get; set; } = 3;
    public bool BatAiCham { get; set; } = true;
}
