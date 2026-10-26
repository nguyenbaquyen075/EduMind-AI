using System.Threading.RateLimiting;
using Anthropic;
using SuDia.Web.Services;
using SuDia.Web.Services.TroLy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

// Trợ lý AI: có ANTHROPIC_API_KEY (biến môi trường hoặc user-secrets) thì trả lời theo tài liệu khoá,
// chưa có thì dùng bản xem thử
builder.Services.Configure<TroLyOptions>(builder.Configuration.GetSection("TroLy"));
var khoaApi = builder.Configuration["ANTHROPIC_API_KEY"];
if (!string.IsNullOrWhiteSpace(khoaApi))
{
    var troLy = builder.Configuration.GetSection("TroLy").Get<TroLyOptions>() ?? new TroLyOptions();
    var goc = builder.Environment.ContentRootPath;
    builder.Services.AddSingleton(new AnthropicClient { ApiKey = khoaApi });
    builder.Services.AddSingleton(new KhoTaiLieu(Path.Combine(goc, troLy.ThuMucTaiLieu)));
    builder.Services.AddSingleton(new PromptTroLy(Path.Combine(goc, troLy.FilePrompt)));
    builder.Services.AddSingleton<IAiService, RagAiService>();
}
else
{
    builder.Services.AddSingleton<IAiService, DemoAiService>();
}

// Dữ liệu các trang. Khi có database: đổi sang service đọc từ EF Core
builder.Services.AddScoped<IKhoaHocService, DuLieuMauService>();

// Giới hạn: 10 câu hỏi / phút / địa chỉ IP
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("tro-ly", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
