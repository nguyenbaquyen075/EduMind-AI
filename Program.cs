using System.Threading.RateLimiting;
using Anthropic;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Services;
using SuDia.Web.Services.TroLy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<KhoBanNhapNoiDung>();
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
builder.Services.AddHttpContextAccessor();

// Database (PostgreSQL) + tài khoản, vai trò Học viên / Giảng viên / Admin
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("SuDia")
        ?? throw new InvalidOperationException("Thiếu ConnectionStrings:SuDia")));
builder.Services.AddIdentity<NguoiDung, IdentityRole>(o =>
    {
        o.User.RequireUniqueEmail = true;
        o.Password.RequiredLength = 8;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequireUppercase = false;
        o.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
// Kiểm tra phiên đăng nhập ở mỗi lần mở trang: tài khoản bị khoá / đổi vai trò có hiệu lực ngay
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero);
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/TaiKhoan/DangNhap";
    o.AccessDeniedPath = "/TaiKhoan/KhongCoQuyen";
    o.ExpireTimeSpan = TimeSpan.FromDays(14);
    o.SlidingExpiration = true;
});

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
builder.Services.Configure<GeminiSoanBaiOptions>(builder.Configuration.GetSection("GeminiSoanBai"));
builder.Services.AddHttpClient<ISoanNoiDungAiService, GeminiSoanNoiDungAiService>(http => http.Timeout = TimeSpan.FromSeconds(120));

// Dữ liệu các trang học viên: đọc từ database theo người đăng nhập
builder.Services.AddScoped<IKhoaHocService, EfKhoaHocService>();
builder.Services.AddScoped<AdminThongKeService>();
builder.Services.Configure<HocOnlineOptions>(builder.Configuration.GetSection("HocOnline"));

// Giới hạn: 10 câu hỏi / phút / địa chỉ IP
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("tro-ly", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();
app.Logger.LogInformation("AI soạn bài Gemini: {TrangThai}", string.IsNullOrWhiteSpace(builder.Configuration["GEMINI_API_KEY"]) ? "Chưa cấu hình kết nối" : "Đã cấu hình kết nối");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "areas", pattern: "{area:exists}/{controller=TongQuan}/{action=Index}/{id?}");
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

// Cập nhật database, vai trò, nội dung mẫu; tài khoản dùng thử chỉ tạo ở Development
await KhoiTaoDuLieu.ChayAsync(app.Services, app.Environment.IsDevelopment(),
    builder.Configuration["TaiKhoanMau:MatKhau"] ?? "SuDia@2026");

await app.RunAsync();
