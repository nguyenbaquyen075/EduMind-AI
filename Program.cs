using Microsoft.AspNetCore.DataProtection;
using System.Threading.RateLimiting;
using Anthropic;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Services;
using SuDia.Web.Services.TroLy;
using SuDia.Web.Services.Sms;
using SuDia.Web.Services.Email;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(o => o.Filters.Add<SuDia.Web.Infrastructure.LoiRangBuocCsdlFilter>());
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<KhoBanNhapNoiDung>();
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
builder.Services.AddHttpContextAccessor();

// Database (PostgreSQL) + tài khoản, vai trò Học viên / Giảng viên / Admin
// Trợ lý AI đọc nội dung đã nhập trong hệ thống: mỗi lần lưu khoá/bài/mục/gói học thì chỉ mục tài liệu được nạp lại
builder.Services.AddSingleton<SuDia.Web.Services.TroLy.DongBoChiMuc>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SuDia.Web.Services.TroLy.DongBoChiMuc>());
builder.Services.AddSingleton<TaiLieuThayDoiInterceptor>();
builder.Services.AddDbContext<AppDbContext>((sp, o) =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("SuDia")
            ?? throw new InvalidOperationException("Thiếu ConnectionStrings:SuDia"),
            // Lỗi kết nối tạm thời (mạng chập chờn, DB khởi động lại): tự thử lại tối đa 3 lần
            npg => npg.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null))
     .AddInterceptors(sp.GetRequiredService<TaiLieuThayDoiInterceptor>()));
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
// Kho tài liệu + chỉ mục tìm kiếm: luôn bật (học viên tìm được tài liệu kể cả khi chưa cấu hình AI)
builder.Services.Configure<KhoTaiLieuOptions>(builder.Configuration.GetSection("KhoTaiLieu"));
builder.Services.Configure<KhoS3Options>(builder.Configuration.GetSection("KhoTaiLieu:S3"));
builder.Services.AddSingleton<BoChonKhoTep>();
builder.Services.AddScoped<TaiLieuKhoaService>();
builder.Services.AddSingleton(sp => new KhoTaiLieu(sp.GetRequiredService<IServiceScopeFactory>(),
    Path.Combine(builder.Environment.ContentRootPath, (builder.Configuration.GetSection("TroLy").Get<TroLyOptions>() ?? new()).ThuMucTaiLieu)));
// Trợ lý AI: TroLy:NhaCungCap chọn Gemini (miễn phí) hoặc Claude; thiếu khoá bên đã chọn thì dùng bên còn lại,
// không có khoá nào thì dùng bản xem thử
var troLy = builder.Configuration.GetSection("TroLy").Get<TroLyOptions>() ?? new TroLyOptions();
var khoaClaude = builder.Configuration["ANTHROPIC_API_KEY"];
var khoaGemini = builder.Configuration["GEMINI_API_KEY"];
var dungGemini = !string.IsNullOrWhiteSpace(khoaGemini) && (troLy.NhaCungCap == "Gemini" || string.IsNullOrWhiteSpace(khoaClaude));
var dungClaude = !dungGemini && !string.IsNullOrWhiteSpace(khoaClaude);
if (dungGemini || dungClaude)
    builder.Services.AddSingleton(new PromptTroLy(Path.Combine(builder.Environment.ContentRootPath, troLy.FilePrompt)));
if (dungGemini)
{
    builder.Services.AddHttpClient<GeminiRagAiService>(h => h.Timeout = TimeSpan.FromSeconds(120));
    builder.Services.AddTransient<IAiService>(sp => sp.GetRequiredService<GeminiRagAiService>());
}
else if (dungClaude)
{
    builder.Services.AddSingleton(new AnthropicClient { ApiKey = khoaClaude });
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
builder.Services.AddScoped<HocVienService>();
builder.Services.AddScoped<LopHocService>();
// Thanh toán PayOS: khoá lưu bằng user-secrets (Development) hoặc biến môi trường PayOS__ClientId…
builder.Services.Configure<PayOSOptions>(builder.Configuration.GetSection("PayOS"));
builder.Services.AddHttpClient<PayOSService>(http => http.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddScoped<DonHangService>();
builder.Services.AddScoped<DuyetDonAgent>();
builder.Services.AddScoped<ThongBaoService>();
builder.Services.Configure<SmsOptions>(builder.Configuration.GetSection("Sms"));
if (builder.Environment.IsDevelopment()) builder.Services.AddSingleton<ISmsSender, LogSmsSender>();
else builder.Services.AddSingleton<ISmsSender, ChuaCoSmsSender>();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
if (builder.Environment.IsDevelopment()) builder.Services.AddSingleton<IEmailSender, LogEmailSender>();
else builder.Services.AddSingleton<IEmailSender, ChuaCoEmailSender>();
builder.Services.AddHostedService<ThongBaoWorker>();
builder.Services.AddHostedService<DoiSoatDonWorker>();
builder.Services.Configure<HocOnlineOptions>(builder.Configuration.GetSection("HocOnline"));

// Giới hạn: 10 câu hỏi / phút / địa chỉ IP
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("tro-ly", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

// Khoá mã hoá cookie đăng nhập / chống giả mạo form: lưu ra thư mục bền (volume khi chạy Docker),
// để cập nhật hay khởi động lại web không làm mọi người bị đăng xuất và form cũ báo lỗi
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath,
        builder.Configuration["DataProtection:ThuMuc"] ?? "App_Data/khoa-bao-ve")))
    .SetApplicationName("SuDia.Web");

// Chạy sau proxy HTTPS (Caddy trong docker-compose): lấy IP thật của người dùng và giao thức https từ proxy.
// Chỉ bật khi web không mở cổng trực tiếp ra Internet (SauProxy=true), nếu không người ngoài giả được IP.
var sauProxy = builder.Configuration.GetValue<bool>("SauProxy");
if (sauProxy)
    builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
        o.KnownNetworks.Clear();
        o.KnownProxies.Clear();
        o.ForwardLimit = 1;
    });

var app = builder.Build();
if (sauProxy) app.UseForwardedHeaders();
app.Logger.LogInformation("AI soạn bài Gemini: {TrangThai}", string.IsNullOrWhiteSpace(builder.Configuration["GEMINI_API_KEY"]) ? "Chưa cấu hình kết nối" : "Đã cấu hình kết nối");

// dotnet ef chỉ cần tạo service provider để đọc model; không chạy máy chủ hay khởi tạo dữ liệu.
if (Environment.GetEnvironmentVariable("EF_DESIGN_TIME") == "1") return;

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

// Kiểm tra sức khoẻ cho máy chủ / công cụ giám sát: web chạy và kết nối được CSDL
app.MapGet("/healthz", async (AppDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct) ? Results.Text("ok") : Results.StatusCode(503));

app.MapControllerRoute(name: "areas", pattern: "{area:exists}/{controller=TongQuan}/{action=Index}/{id?}");
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

// Cập nhật database, vai trò, nội dung mẫu; tài khoản dùng thử chỉ tạo ở Development.
// EF Tools nạp Program để lấy metadata migration, không được thử kết nối database lúc đó.
var dangTaoMigration = AppDomain.CurrentDomain.GetAssemblies()
    .Any(a => a.GetName().Name == "Microsoft.EntityFrameworkCore.Design");
if (!dangTaoMigration)
{
    await KhoiTaoDuLieu.ChayAsync(app.Services, app.Environment.IsDevelopment(),
        builder.Configuration["TaiKhoanMau:MatKhau"] ?? "SuDia@2026");
}

// dotnet SuDia.Web.dll --tao-admin <email>: tạo (hoặc cấp quyền) tài khoản Admin trên máy chủ thật rồi thoát.
// Mật khẩu nhập ẩn trên bàn phím, hoặc lấy từ biến môi trường MAT_KHAU_ADMIN.
if (args.Contains("--tao-admin"))
{
    var i = Array.IndexOf(args, "--tao-admin");
    var email = i + 1 < args.Length ? args[i + 1].Trim() : "";
    if (!email.Contains('@')) { Console.WriteLine("Cách dùng: --tao-admin email@ten-mien.vn"); Environment.ExitCode = 1; return; }
    var matKhau = Environment.GetEnvironmentVariable("MAT_KHAU_ADMIN");
    if (string.IsNullOrEmpty(matKhau))
    {
        Console.Write("Mật khẩu (ít nhất 8 ký tự, không hiện khi gõ): ");
        var sb = new System.Text.StringBuilder();
        for (ConsoleKeyInfo k; (k = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter;)
            if (k.Key == ConsoleKey.Backspace) { if (sb.Length > 0) sb.Length--; } else if (!char.IsControl(k.KeyChar)) sb.Append(k.KeyChar);
        Console.WriteLine();
        matKhau = sb.ToString();
    }
    using var scope = app.Services.CreateScope();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<NguoiDung>>();
    var nd = await users.FindByEmailAsync(email);
    var kq = nd is null
        ? await users.CreateAsync(nd = new NguoiDung { UserName = email, Email = email, EmailConfirmed = true, HoTen = "Quản trị" }, matKhau)
        : await users.ResetPasswordAsync(nd, await users.GeneratePasswordResetTokenAsync(nd), matKhau);
    if (kq.Succeeded && !await users.IsInRoleAsync(nd, VaiTro.Admin)) kq = await users.AddToRoleAsync(nd, VaiTro.Admin);
    Console.WriteLine(kq.Succeeded ? $"Đã sẵn sàng tài khoản Admin {email}." : "Không tạo được: " + string.Join(" ", kq.Errors.Select(e => e.Description)));
    if (!kq.Succeeded) Environment.ExitCode = 1;
    return;
}

// dotnet run -- --kiem-tra-r2: thử ghi / đọc / link tải tạm / xoá một tệp nhỏ trên kho S3 (Cloudflare R2) đã cấu hình rồi thoát
if (args.Contains("--kiem-tra-r2"))
{
    var chon = app.Services.GetRequiredService<BoChonKhoTep>();
    if (!chon.CoS3) { Console.WriteLine("Chưa cấu hình KhoTaiLieu:S3 (Bucket, AccessKey, SecretKey). Chạy scripts/cau-hinh-r2.zsh trước."); Environment.ExitCode = 1; return; }
    var s3 = chon.Theo(NoiLuuTep.S3);
    var khoaThu = $"kiem-tra/{Guid.NewGuid():N}.txt";
    var noiDung = System.Text.Encoding.UTF8.GetBytes("SuDia kiểm tra kết nối R2 " + DateTime.UtcNow.ToString("O"));
    try
    {
        await s3.LuuAsync(khoaThu, noiDung, "text/plain; charset=utf-8");
        Console.WriteLine("1. Ghi tệp thử: được");
        await using (var doc = await s3.MoAsync(khoaThu))
        {
            using var ms = new MemoryStream();
            await doc!.CopyToAsync(ms);
            Console.WriteLine(ms.ToArray().SequenceEqual(noiDung) ? "2. Đọc lại đúng nội dung: được" : "2. Đọc lại: SAI nội dung");
        }
        var link = s3.LinkTaiTam(khoaThu, "kiem-tra.txt", "text/plain; charset=utf-8", false, TimeSpan.FromMinutes(1))!;
        using var http = new HttpClient();
        var tai = await http.GetByteArrayAsync(link);
        Console.WriteLine(tai.SequenceEqual(noiDung) ? "3. Link tải tạm (học viên tải tài liệu): được" : "3. Link tải tạm: SAI nội dung");
        await s3.XoaAsync(khoaThu);
        Console.WriteLine("4. Xoá tệp thử: được");
        Console.WriteLine("R2 sẵn sàng. Đặt KhoTaiLieu:NoiLuu = S3 để tệp mới lưu lên R2, rồi chạy --chuyen-kho-tep để chuyển tệp cũ.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("LỖI: " + ex.Message);
        Console.WriteLine("Kiểm tra lại: địa chỉ S3 API (https://<account-id>.r2.cloudflarestorage.com), tên bucket, quyền token (Object Read & Write cho đúng bucket).");
        Environment.ExitCode = 1;
    }
    return;
}

// dotnet run -- --chuyen-kho-tep: chuyển tệp kho tài liệu đang nằm trên đĩa lên kho S3 (Cloudflare R2) đã cấu hình rồi thoát.
// Mỗi tệp được kiểm mã băm trước khi chuyển; chuyển xong mới xoá bản trên đĩa. Chạy lại an toàn (chỉ chuyển tệp còn trên đĩa).
if (args.Contains("--chuyen-kho-tep"))
{
    using var scope = app.Services.CreateScope();
    var so = await scope.ServiceProvider.GetRequiredService<TaiLieuKhoaService>().ChuyenLenS3Async(Console.WriteLine);
    Console.WriteLine($"Đã chuyển {so} tệp lên kho S3.");
    return;
}

// dotnet run -- --du-lieu-mau --xac-nhan-xoa-du-lieu-mau:
// làm mới toàn bộ dữ liệu mẫu (chỉ Development) rồi thoát
if (args.Contains("--du-lieu-mau"))
{
    if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("Chỉ tạo dữ liệu mẫu ở Development.");
    if (!args.Contains("--xac-nhan-xoa-du-lieu-mau"))
        throw new InvalidOperationException("Lệnh này xoá dữ liệu Development. Chạy lại với --xac-nhan-xoa-du-lieu-mau nếu anh chắc chắn.");
    await DuLieuMauDayDu.TaoAsync(app.Services, builder.Configuration["TaiKhoanMau:MatKhau"] ?? "SuDia@2026");
    Console.WriteLine("Đã tạo dữ liệu mẫu.");
    return;
}

await app.RunAsync();
