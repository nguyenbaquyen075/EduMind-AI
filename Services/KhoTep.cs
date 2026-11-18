using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace SuDia.Web.Services;

/// <summary>Nơi cất tệp gốc của kho tài liệu. Chọn bằng KhoTaiLieu:NoiLuu ("CucBo" hoặc "S3").</summary>
public interface IKhoTep
{
    string Ten { get; }
    Task LuuAsync(string khoa, byte[] duLieu, string kieuNoiDung, CancellationToken ct = default);
    Task<Stream?> MoAsync(string khoa, CancellationToken ct = default);
    Task XoaAsync(string khoa, CancellationToken ct = default);
    /// <summary>Link tải tạm thời ký sẵn (S3); null nếu kho không hỗ trợ (đĩa cục bộ → web tự trả tệp).</summary>
    string? LinkTaiTam(string khoa, string tenTep, string kieuNoiDung, bool moTrucTiep, TimeSpan han);
}

public static class NoiLuuTep
{
    public const string CucBo = "CucBo";
    public const string S3 = "S3";
}

public class KhoS3Options
{
    /// <summary>Cloudflare R2: https://&lt;account-id&gt;.r2.cloudflarestorage.com · AWS S3: để trống, đặt Vung.</summary>
    public string DiaChi { get; set; } = "";
    public string Bucket { get; set; } = "";
    /// <summary>R2: "auto". AWS: ví dụ "ap-southeast-1".</summary>
    public string Vung { get; set; } = "auto";
    // Khoá truy cập đặt bằng dotnet user-secrets / biến môi trường, không ghi vào appsettings.json
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";

    public bool DaCauHinh => Bucket.Length > 0 && AccessKey.Length > 0 && SecretKey.Length > 0;
}

/// <summary>Lưu trên đĩa máy chủ web (mặc định khi chạy thử, không cần tài khoản).</summary>
public class KhoTepCucBo(string thuMucGoc) : IKhoTep
{
    private readonly string _goc = Path.GetFullPath(thuMucGoc);
    public string Ten => NoiLuuTep.CucBo;

    private string DuongDan(string khoa)
    {
        var day = Path.GetFullPath(Path.Combine(_goc, khoa));
        if (!day.StartsWith(_goc + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Đường dẫn tệp nằm ngoài kho.");
        return day;
    }

    public async Task LuuAsync(string khoa, byte[] duLieu, string kieuNoiDung, CancellationToken ct = default)
    {
        var p = DuongDan(khoa);
        Directory.CreateDirectory(Path.GetDirectoryName(p)!);
        await File.WriteAllBytesAsync(p, duLieu, ct);
    }

    public Task<Stream?> MoAsync(string khoa, CancellationToken ct = default)
    {
        var p = DuongDan(khoa);
        return Task.FromResult<Stream?>(File.Exists(p) ? File.OpenRead(p) : null);
    }

    public Task XoaAsync(string khoa, CancellationToken ct = default)
    {
        var p = DuongDan(khoa);
        if (File.Exists(p)) File.Delete(p);
        return Task.CompletedTask;
    }

    public string? LinkTaiTam(string khoa, string tenTep, string kieuNoiDung, bool moTrucTiep, TimeSpan han) => null;
}

/// <summary>Kho chuẩn S3: Cloudflare R2, AWS S3, MinIO, SeaweedFS…</summary>
public sealed class KhoTepS3 : IKhoTep, IDisposable
{
    private readonly AmazonS3Client _s3;
    private readonly string _bucket;
    private readonly Protocol _giaoThuc;
    public string Ten => NoiLuuTep.S3;

    public KhoTepS3(KhoS3Options o)
    {
        var cfg = new AmazonS3Config
        {
            // R2 và các kho tương thích S3 chưa hỗ trợ đủ checksum mặc định mới của AWS SDK: chỉ gửi khi bắt buộc
            RequestChecksumCalculation = Amazon.Runtime.RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = Amazon.Runtime.ResponseChecksumValidation.WHEN_REQUIRED,
        };
        if (o.DiaChi.Length > 0) { cfg.ServiceURL = o.DiaChi; cfg.ForcePathStyle = true; cfg.AuthenticationRegion = o.Vung; }
        else cfg.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(o.Vung);
        _s3 = new AmazonS3Client(o.AccessKey, o.SecretKey, cfg);
        _bucket = o.Bucket;
        // Link ký tạm dùng đúng giao thức của địa chỉ kho (R2/AWS: https; kho thử trên máy: có thể http)
        _giaoThuc = o.DiaChi.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? Protocol.HTTP : Protocol.HTTPS;
    }

    public async Task LuuAsync(string khoa, byte[] duLieu, string kieuNoiDung, CancellationToken ct = default)
    {
        using var ms = new MemoryStream(duLieu);
        await _s3.PutObjectAsync(new PutObjectRequest { BucketName = _bucket, Key = khoa, InputStream = ms, ContentType = kieuNoiDung, AutoCloseStream = false }, ct);
    }

    public async Task<Stream?> MoAsync(string khoa, CancellationToken ct = default)
    {
        try
        {
            var r = await _s3.GetObjectAsync(_bucket, khoa, ct);
            return r.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { return null; }
    }

    public Task XoaAsync(string khoa, CancellationToken ct = default) => _s3.DeleteObjectAsync(_bucket, khoa, ct);

    public string? LinkTaiTam(string khoa, string tenTep, string kieuNoiDung, bool moTrucTiep, TimeSpan han)
    {
        var ten = Uri.EscapeDataString(tenTep);
        return _s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucket, Key = khoa, Verb = HttpVerb.GET, Expires = DateTime.UtcNow.Add(han), Protocol = _giaoThuc,
            ResponseHeaderOverrides = new ResponseHeaderOverrides
            {
                ContentType = kieuNoiDung,
                ContentDisposition = (moTrucTiep ? "inline" : "attachment") + $"; filename*=UTF-8''{ten}",
            },
        });
    }

    public void Dispose() => _s3.Dispose();
}

/// <summary>Chọn kho theo tên lưu trong từng tài liệu (để chuyển dần từ đĩa lên S3 không gián đoạn).</summary>
public class BoChonKhoTep(IOptions<KhoTaiLieuOptions> opt, IOptions<KhoS3Options> s3, IWebHostEnvironment env) : IDisposable
{
    private readonly Lazy<KhoTepCucBo> _cucBo = new(() => new KhoTepCucBo(Path.Combine(env.ContentRootPath, opt.Value.ThuMuc)));
    private readonly Lazy<KhoTepS3?> _s3 = new(() => s3.Value.DaCauHinh ? new KhoTepS3(s3.Value) : null);

    public bool CoS3 => _s3.Value is not null;

    /// <summary>Kho cho tệp mới tải lên.</summary>
    public IKhoTep MacDinh => opt.Value.NoiLuu == NoiLuuTep.S3
        ? _s3.Value ?? throw new InvalidOperationException("KhoTaiLieu:NoiLuu = S3 nhưng chưa cấu hình KhoTaiLieu:S3 (Bucket, AccessKey, SecretKey).")
        : _cucBo.Value;

    public IKhoTep Theo(string noiLuu) => noiLuu == NoiLuuTep.S3
        ? _s3.Value ?? throw new InvalidOperationException("Tài liệu nằm trên S3 nhưng chưa cấu hình KhoTaiLieu:S3.")
        : _cucBo.Value;

    public void Dispose() { if (_s3.IsValueCreated) _s3.Value?.Dispose(); }
}
