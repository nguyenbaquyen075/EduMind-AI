#!/usr/bin/env bash
# Sao lưu CSDL + dữ liệu web (khoá đăng nhập, tệp tài liệu nếu đang lưu trên máy chủ) vào deploy/sao-luu/.
# Giữ 14 bản gần nhất. Cài chạy tự động lúc 2 giờ sáng: crontab -e  →  0 2 * * * /đường-dẫn/SuDia.Web/deploy/sao-luu.sh
set -euo pipefail
cd "$(dirname "$0")"
mkdir -p sao-luu
luc=$(date +%Y%m%d_%H%M)
docker compose exec -T db pg_dump -U sudia -d sudia -Fc > "sao-luu/csdl_$luc.dump"
docker run --rm -v sudia_du-lieu-web:/du-lieu:ro -v "$PWD/sao-luu":/ra alpine tar czf "/ra/du-lieu-web_$luc.tar.gz" -C /du-lieu .
# Bỏ bản cũ, giữ 14 bản mỗi loại
ls -1t sao-luu/csdl_*.dump 2>/dev/null | tail -n +15 | xargs -r rm --
ls -1t sao-luu/du-lieu-web_*.tar.gz 2>/dev/null | tail -n +15 | xargs -r rm --
echo "Đã sao lưu: sao-luu/csdl_$luc.dump, sao-luu/du-lieu-web_$luc.tar.gz"
