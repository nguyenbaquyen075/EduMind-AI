#!/bin/zsh
# Nhập khoá Cloudflare R2 cho kho tài liệu trực tiếp trên máy: không ghi vào mã nguồn, không lưu vào lịch sử lệnh.
# Trước khi chạy: dash.cloudflare.com → R2 → tạo bucket (riêng tư) → Manage R2 API Tokens → Create API token
#   quyền "Object Read & Write", chỉ áp cho bucket vừa tạo → ghi lại Access Key ID, Secret Access Key.
#   Account ID xem ở trang R2 (cột bên phải) hoặc trong địa chỉ S3 API: https://<account-id>.r2.cloudflarestorage.com
set -eu
set -o pipefail
du_an="${0:A:h:h}"
printf 'Cấu hình Cloudflare R2 cho kho tài liệu SuDia\n\n'
read -r 'tai_khoan?Account ID (32 ký tự, hiện chữ): '
read -r 'bucket?Tên bucket [sudia-tai-lieu]: '
bucket="${bucket:-sudia-tai-lieu}"
read -r -s 'khoa_truy_cap?Access Key ID (ẩn ký tự): '; printf '\n'
read -r -s 'khoa_bi_mat?Secret Access Key (ẩn ký tự): '; printf '\n'
trap 'unset khoa_truy_cap khoa_bi_mat' EXIT
if [[ ! "$tai_khoan" =~ '^[0-9a-f]{32}$' ]]; then printf 'Account ID phải gồm 32 ký tự 0-9, a-f. Chưa thay đổi gì.\n'; exit 1; fi
if [[ -z "$khoa_truy_cap" || -z "$khoa_bi_mat" ]]; then printf 'Thiếu khoá. Chưa thay đổi gì.\n'; exit 1; fi
printf '%s\n%s\n%s\n%s' "$tai_khoan" "$bucket" "$khoa_truy_cap" "$khoa_bi_mat" |
  node -e 'let s=""; process.stdin.setEncoding("utf8"); process.stdin.on("data", c => s += c); process.stdin.on("end", () => {
    const [tk, b, a, k] = s.split("\n").map(x => x.trim());
    process.stdout.write(JSON.stringify({ "KhoTaiLieu:S3:DiaChi": `https://${tk}.r2.cloudflarestorage.com`, "KhoTaiLieu:S3:Bucket": b,
      "KhoTaiLieu:S3:Vung": "auto", "KhoTaiLieu:S3:AccessKey": a, "KhoTaiLieu:S3:SecretKey": k })); });' |
  dotnet user-secrets set --project "$du_an/SuDia.Web.csproj" >/dev/null
printf 'Đã lưu khoá R2 (user-secrets, môi trường Development). Đang kiểm tra kết nối…\n\n'
cd "$du_an"
if ASPNETCORE_ENVIRONMENT=Development dotnet run --no-launch-profile -- --kiem-tra-r2 2>/dev/null | grep -v '^\(info\|warn\|      \)'; then
  dotnet user-secrets set --project "$du_an/SuDia.Web.csproj" "KhoTaiLieu:NoiLuu" "S3" >/dev/null
  printf '\nĐã bật lưu tệp mới lên R2 (KhoTaiLieu:NoiLuu = S3). Khởi động lại web để áp dụng.\n'
else
  printf '\nKết nối R2 chưa được, CHƯA bật lưu lên R2. Sửa lại theo gợi ý ở trên rồi chạy lại script.\n'
fi
