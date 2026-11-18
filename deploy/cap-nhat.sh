#!/usr/bin/env bash
# Cập nhật web lên phiên bản mới nhất của nhánh main: sao lưu trước, build lại, kiểm tra web sống lại.
# CSDL tự cập nhật cấu trúc (migration) khi web khởi động.
set -euo pipefail
cd "$(dirname "$0")"
./sao-luu.sh
git -C .. pull --ff-only
docker compose up -d --build web
for i in $(seq 1 30); do
  if docker compose exec -T caddy wget -qO- http://web:8080/healthz 2>/dev/null | grep -q "ok"; then
    echo "Web đã chạy bản mới."; exit 0
  fi
  sleep 3
done
echo "Web chưa trả lời /healthz sau 90 giây. Xem lỗi: docker compose logs --tail 100 web"; exit 1
