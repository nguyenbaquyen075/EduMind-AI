#!/bin/zsh
# Nhập khoá trực tiếp trên máy, không ghi vào mã nguồn hay lịch sử lệnh.
set -eu
set -o pipefail
task_project_dir="${0:A:h:h}"
printf 'Lấy khóa tại https://aistudio.google.com/apikey — chọn dự án Free tier, chưa bật Billing để dùng hạn mức miễn phí.\n'
read -r -s 'sudia_api_key?Dán Gemini API key rồi nhấn Enter (ẩn ký tự): '
printf '\n'
if [[ -z "$sudia_api_key" ]]; then
  printf 'Chưa nhập khoá; cấu hình chưa thay đổi.\n'
  exit 1
fi
trap 'unset sudia_api_key' EXIT
printf '%s' "$sudia_api_key" |
  node -e 'let key=""; process.stdin.setEncoding("utf8"); process.stdin.on("data", chunk => key += chunk); process.stdin.on("end", () => process.stdout.write(JSON.stringify({GEMINI_API_KEY:key.trim()})));' |
  dotnet user-secrets set --project "$task_project_dir/SuDia.Web.csproj"
printf 'Đã lưu khoá Gemini cho môi trường Development. Khởi động lại web để kết nối AI.\n'
