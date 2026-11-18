#!/bin/zsh
# Nhập khoá Anthropic (trợ lý AI) trực tiếp trên máy: ẩn ký tự, không ghi vào mã nguồn hay lịch sử lệnh.
# Khoá được thử với Anthropic trước; chỉ lưu khi khoá dùng được.
set -eu
set -o pipefail
du_an="${0:A:h:h}"
printf 'Lấy khoá tại https://console.anthropic.com/settings/keys → Create Key (khoá bắt đầu bằng sk-ant-)\n'
read -r -s 'khoa?Dán khoá Anthropic rồi nhấn Enter (ẩn ký tự): '
printf '\n'
trap 'unset khoa' EXIT
khoa="${khoa//[[:space:]]/}"
if [[ "$khoa" != sk-ant-* || ${#khoa} -lt 40 ]]; then
  printf 'Đây không phải khoá Anthropic (khoá đúng bắt đầu bằng "sk-ant-" và dài khoảng 100 ký tự). Chưa thay đổi gì.\n'
  exit 1
fi
printf 'Đang thử khoá với Anthropic…\n'
ma=$(printf 'header = "x-api-key: %s"\n' "$khoa" | curl -s -o /dev/null -w '%{http_code}' -K - \
  -H 'anthropic-version: 2023-06-01' https://api.anthropic.com/v1/models)
case "$ma" in
  200) ;;
  401) printf 'Anthropic báo khoá không hợp lệ hoặc đã bị thu hồi (401). Chưa thay đổi gì.\n'; exit 1 ;;
  403) printf 'Khoá đúng nhưng tài khoản chưa được phép gọi API (403): kiểm tra Billing / Plans trên console.anthropic.com. Chưa thay đổi gì.\n'; exit 1 ;;
  000) printf 'Không kết nối được tới api.anthropic.com (mạng?). Chưa thay đổi gì.\n'; exit 1 ;;
  *)   printf 'Anthropic trả mã %s. Chưa thay đổi gì.\n' "$ma"; exit 1 ;;
esac
printf '%s' "$khoa" |
  node -e 'let k=""; process.stdin.setEncoding("utf8"); process.stdin.on("data", c => k += c); process.stdin.on("end", () => process.stdout.write(JSON.stringify({ANTHROPIC_API_KEY: k.trim()})));' |
  dotnet user-secrets set --project "$du_an/SuDia.Web.csproj" >/dev/null
printf 'Khoá dùng được và đã lưu. Khởi động lại web để trợ lý AI trả lời thật.\n'
