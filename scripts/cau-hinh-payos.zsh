#!/bin/zsh
# Nhập khoá PayOS trực tiếp trên máy, không ghi vào mã nguồn hay lịch sử lệnh.
set -eu
set -o pipefail
task_project_dir="${0:A:h:h}"
printf 'Lấy 3 khoá tại https://my.payos.vn → Kênh thanh toán → Thông tin tích hợp.\n'
read -r -s 'payos_client_id?Client ID (ẩn ký tự): '; printf '\n'
read -r -s 'payos_api_key?API Key (ẩn ký tự): '; printf '\n'
read -r -s 'payos_checksum?Checksum Key (ẩn ký tự): '; printf '\n'
trap 'unset payos_client_id payos_api_key payos_checksum' EXIT
if [[ -z "$payos_client_id" || -z "$payos_api_key" || -z "$payos_checksum" ]]; then
  printf 'Thiếu khoá; cấu hình chưa thay đổi.\n'
  exit 1
fi
printf '%s\n%s\n%s' "$payos_client_id" "$payos_api_key" "$payos_checksum" |
  node -e 'let s=""; process.stdin.setEncoding("utf8"); process.stdin.on("data", c => s += c); process.stdin.on("end", () => { const [a,b,c] = s.split("\n").map(x => x.trim()); process.stdout.write(JSON.stringify({"PayOS:ClientId":a,"PayOS:ApiKey":b,"PayOS:ChecksumKey":c})); });' |
  dotnet user-secrets set --project "$task_project_dir/SuDia.Web.csproj"
printf 'Đã lưu khoá PayOS cho môi trường Development. Khởi động lại web để bật thanh toán.\n'
printf 'Webhook khai báo trên PayOS: https://<tên-miền>/api/payos/webhook\n'
