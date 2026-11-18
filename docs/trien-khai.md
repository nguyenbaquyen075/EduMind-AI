# Đưa SửĐịa.AI lên máy chủ chạy 24/7

Sau khi làm xong, web, cơ sở dữ liệu và trợ lý AI chạy trên một máy chủ luôn bật. Máy tính cá nhân tắt cũng không ảnh hưởng.

Bộ triển khai nằm trong thư mục `deploy/` và chạy được ba thành phần:

| Thành phần | Vai trò |
|---|---|
| `web` | Ứng dụng SửĐịa (.NET 8). Chạy bằng tài khoản thường (không phải root) và chỉ nhận kết nối từ Caddy |
| `db` | PostgreSQL 16. Dữ liệu nằm trong volume `csdl`. Không mở cổng ra Internet |
| `caddy` | Nhận kết nối ở cổng 80/443. Tự xin và gia hạn chứng chỉ HTTPS miễn phí (Let's Encrypt), chuyển `http` sang `https` |

Đã chạy thử đủ các bước trên máy: dựng 3 container, cấp HTTPS, `/healthz`, tạo admin, đăng nhập và giữ phiên sau khi khởi động lại, chuyển dữ liệu từ Mac (PostgreSQL 14 → 16), tìm kiếm không dấu, sao lưu, cập nhật.

---

## Bước 0 — Chuẩn bị

| Cần có | Gợi ý |
|---|---|
| **Máy chủ ảo (VPS)** | Ubuntu 24.04 LTS, tối thiểu **2 vCPU, 2 GB RAM, 40 GB SSD** (4 GB RAM sẽ thoải mái hơn). Nên đặt máy chủ ở Việt Nam hoặc Singapore cho nhanh. Nhà cung cấp phải cho đăng nhập SSH bằng quyền root |
| **Tên miền** | Ví dụ `sudia.vn` hoặc tên miền phụ `hoc.sudia.vn` |
| **Khoá dịch vụ** | `ANTHROPIC_API_KEY` (trợ lý AI), `GEMINI_API_KEY` (AI soạn bài), 3 khoá PayOS. Khoá R2/B2 nếu muốn lưu tài liệu ngoài máy chủ |
| **Code trên GitHub** | Repo `nguyenbaquyen075/EduMind-AI`, nhánh `main` chứa bản muốn chạy |

## Bước 1 — Trỏ tên miền về máy chủ

1. Vào trang quản lý DNS của nơi anh mua tên miền.
2. Tạo bản ghi **A**: tên `@` (hoặc `hoc` nếu dùng tên miền phụ), giá trị là **IP của VPS**, TTL để mặc định.
3. Đợi 5–30 phút rồi kiểm tra trên máy anh. Lệnh in ra đúng IP của VPS là được.

   ```bash
   dig +short sudia.vn
   ```

## Bước 2 — Vào máy chủ và cài Docker

Trên máy anh, đăng nhập vào VPS:

```bash
ssh root@<IP-VPS>
```

Sau đó chạy trên VPS:

```bash
# Cập nhật hệ thống, bật tường lửa (chỉ mở SSH, HTTP, HTTPS)
apt update && apt -y upgrade
ufw allow OpenSSH && ufw allow 80/tcp && ufw allow 443/tcp && ufw --force enable

# Cài Docker (script chính thức của Docker)
curl -fsSL https://get.docker.com | sh
docker --version && docker compose version
```

## Bước 3 — Lấy code về máy chủ

Repo đang để riêng tư, nên cần cho máy chủ quyền đọc. Cách gọn là dùng **deploy key**: khoá chỉ đọc, chỉ dùng được cho một repo.

1. Tạo khoá trên VPS:

   ```bash
   ssh-keygen -t ed25519 -f ~/.ssh/sudia_github -N "" -C "may-chu-sudia"
   cat ~/.ssh/sudia_github.pub
   ```

2. Trên GitHub, vào repo → **Settings → Deploy keys → Add deploy key**. Dán nội dung vừa in ra, **không** tick "Allow write access".
3. Lấy code về:

   ```bash
   printf 'Host github.com\n  IdentityFile ~/.ssh/sudia_github\n' >> ~/.ssh/config
   git clone git@github.com:nguyenbaquyen075/EduMind-AI.git /opt/sudia
   cd /opt/sudia && git checkout main
   ```

## Bước 4 — Điền cấu hình

```bash
cd /opt/sudia/deploy
cp .env.example .env
openssl rand -hex 24      # chép chuỗi này làm POSTGRES_PASSWORD
nano .env
```

Điền vào `.env`:

- `TEN_MIEN`: tên miền ở Bước 1, không có `https://`.
- `POSTGRES_PASSWORD`: chuỗi vừa tạo.
- `ANTHROPIC_API_KEY`, `GEMINI_API_KEY`, `PayOS__ClientId`, `PayOS__ApiKey`, `PayOS__ChecksumKey`.
- Kho tệp tài liệu:
  - Để `KhoTaiLieu__NoiLuu=CucBo` thì tệp lưu trên ổ máy chủ, trong volume `du-lieu-web`.
  - Dùng R2 hoặc B2 thì đặt `S3` và điền 4 dòng `KhoTaiLieu__S3__*`.

Lưu bằng `Ctrl+O`, Enter, rồi thoát bằng `Ctrl+X`. Sau đó khoá quyền đọc tệp:

```bash
chmod 600 .env
```

Tệp `.env` chứa khoá bí mật và đã nằm trong `.gitignore`, **không** đưa lên GitHub.

## Bước 5 — Chạy

```bash
cd /opt/sudia/deploy
docker compose up -d --build     # lần đầu mất 3–5 phút để build
docker compose ps                # cả 3 dịch vụ phải "Up", db "(healthy)"
curl https://sudia.vn/healthz    # in ra: ok
```

Mở `https://<tên-miền>` trên trình duyệt. Có ổ khoá HTTPS là được.

Chưa lên được thì xem mục **Xử lý sự cố** ở cuối.

## Bước 6 — Tạo tài khoản Admin

Ở chế độ chạy thật, web không tạo tài khoản mẫu. Anh tự tạo admin:

```bash
docker compose exec web dotnet SuDia.Web.dll --tao-admin email-cua-anh@gmail.com
```

Lệnh hỏi mật khẩu, tối thiểu 8 ký tự. Lúc gõ không hiện chữ, như vậy là bình thường. Chạy lại lệnh với cùng email thì sẽ đổi mật khẩu.

Đăng nhập xong, anh tạo giảng viên, khoá học và gói học trong trang Admin như lúc chạy trên máy.

## Bước 7 (tuỳ chọn) — Chuyển dữ liệu đang có trên Mac lên máy chủ

> ⚠️ Dữ liệu trên Mac có **tài khoản mẫu** (`admin@sudia.vn`, `giangvien@sudia.vn`, `hocvien@sudia.vn`, `hv…@`) với mật khẩu ai cũng biết. Chuyển lên rồi thì phải đổi mật khẩu hoặc xoá các tài khoản đó ngay, xem lệnh ở cuối bước này. Nếu đây chỉ là dữ liệu thử, nên bỏ qua bước này và bắt đầu sạch trên máy chủ.

**Trên Mac:** xuất cơ sở dữ liệu rồi gửi lên máy chủ.

```bash
pg_dump -h localhost -U admin -d sudia_dev -Fc --no-owner --no-privileges -f sudia.dump
scp sudia.dump root@<IP-VPS>:/opt/sudia/deploy/
```

Nếu kho tài liệu đang lưu trên ổ Mac, gửi lên luôn thư mục tệp:

```bash
scp -r ~/Downloads/SuDia.Web/App_Data/kho-tai-lieu root@<IP-VPS>:/opt/sudia/deploy/
```

**Trên máy chủ:** nạp dữ liệu vào CSDL, rồi chép tệp tài liệu (nếu có) vào volume.

```bash
cd /opt/sudia/deploy
docker compose stop web
docker compose exec -T db psql -U sudia -d postgres -c "DROP DATABASE sudia WITH (FORCE);" -c "CREATE DATABASE sudia OWNER sudia;"
docker compose exec -T db pg_restore -U sudia -d sudia --no-owner --no-privileges < sudia.dump
docker compose start web

# Tệp tài liệu (nếu có): chép vào volume của web
docker compose cp kho-tai-lieu web:/app/App_Data/
docker compose exec -u root web chown -R app:app /app/App_Data
rm -rf sudia.dump kho-tai-lieu
```

**Khoá các tài khoản mẫu:** đổi mật khẩu bằng lệnh dưới, hoặc xoá chúng trong Admin › Tài khoản & phân quyền.

```bash
docker compose exec web dotnet SuDia.Web.dll --tao-admin admin@sudia.vn
```

## Bước 8 — Khai báo webhook PayOS

Vào my.payos.vn → **Kênh thanh toán → Thông tin tích hợp** → Webhook URL, nhập:

```
https://<tên-miền>/api/payos/webhook
```

Sau đó thử mua một gói giá nhỏ để chắc đơn tự chuyển sang "Đã thanh toán".

## Bước 9 — Sao lưu tự động

```bash
chmod +x /opt/sudia/deploy/*.sh
/opt/sudia/deploy/sao-luu.sh          # thử một lần, tạo deploy/sao-luu/csdl_*.dump và du-lieu-web_*.tar.gz
crontab -e                           # thêm dòng dưới (chạy 2 giờ sáng mỗi ngày):
0 2 * * * /opt/sudia/deploy/sao-luu.sh >> /var/log/sudia-sao-luu.log 2>&1
```

Script giữ 14 bản gần nhất ngay trên máy chủ. **Nên có thêm một bản ở nơi khác**, phòng khi máy chủ hỏng. Ví dụ mỗi tuần tải về Mac:

```bash
scp root@<IP-VPS>:/opt/sudia/deploy/sao-luu/csdl_*.dump ~/SuDia-backups/
```

Cách khác là đồng bộ thư mục `sao-luu/` lên R2 hoặc B2 bằng `rclone`.

**Khôi phục từ một bản sao lưu:** làm như Bước 7, thay `sudia.dump` bằng `sao-luu/csdl_<ngày>.dump`.

## Bước 10 — Cập nhật phiên bản mới

Sau khi gộp code mới vào nhánh `main` trên GitHub, chạy trên máy chủ:

```bash
/opt/sudia/deploy/cap-nhat.sh
```

Script làm lần lượt:

1. Sao lưu.
2. Lấy code mới (`git pull`).
3. Build lại web.
4. Chờ tới khi `/healthz` trả `ok`.

Cấu trúc CSDL tự cập nhật (migration) khi web khởi động. Người đang dùng không bị đăng xuất, vì khoá mã hoá đăng nhập nằm trong volume.

---

## Xử lý sự cố

| Hiện tượng | Kiểm tra |
|---|---|
| Không vào được trang, trình duyệt báo lỗi chứng chỉ | Chạy `dig +short <tên-miền>`, kết quả phải đúng IP VPS. Cổng 80/443 phải mở (`ufw status`). Xem `docker compose logs caddy`: Caddy chỉ xin được chứng chỉ khi tên miền đã trỏ đúng |
| Trang báo lỗi 502 | Web chưa chạy hoặc vừa lỗi: `docker compose logs --tail 100 web` |
| `/healthz` báo 503 | Web không kết nối được CSDL: `docker compose ps db`, `docker compose logs db` |
| Trợ lý AI trả "Trợ lý đang gặp sự cố" | Khoá `ANTHROPIC_API_KEY` sai hoặc hết hạn. Sửa `.env` rồi chạy `docker compose up -d web` |
| Hết dung lượng ổ | `df -h`. Dọn ảnh Docker cũ bằng `docker image prune -f`. Kiểm tra `deploy/sao-luu/` |
| Muốn khởi động lại | `docker compose restart web`. Cả bộ: `docker compose restart` |

Các lệnh hay dùng (chạy trong `/opt/sudia/deploy`):

```bash
docker compose ps                       # trạng thái
docker compose logs -f --tail 100 web   # xem log web trực tiếp
docker compose exec db psql -U sudia    # vào CSDL
docker stats --no-stream                # CPU / RAM từng container
```

## Danh sách kiểm tra sau khi lên

- [ ] Mở được `https://<tên-miền>`, có ổ khoá HTTPS
- [ ] `https://<tên-miền>/healthz` trả `ok`
- [ ] Đăng nhập Admin được; không còn tài khoản mẫu với mật khẩu ai cũng biết
- [ ] Trang Admin › Trợ lý AI hiện "Đã kết nối" và có số đoạn tài liệu
- [ ] Tải một tài liệu vào Kho tài liệu. Học viên tìm thấy và mở được
- [ ] Thử thanh toán PayOS một gói giá nhỏ, đơn tự chuyển "Đã thanh toán"
- [ ] `crontab -l` có dòng sao lưu; thư mục `deploy/sao-luu/` có bản mới mỗi ngày
- [ ] Có ít nhất một bản sao lưu nằm ngoài máy chủ
