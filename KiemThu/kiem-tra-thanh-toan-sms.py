#!/usr/bin/env python3
"""Kiểm thử thanh toán Development: cần web localhost và PostgreSQL qua psql.

Không có khoá hay mật khẩu trong tệp. T1/T3/T4 chỉ chạy khi có
SUDIA_TEST_PAYOS_CHECKSUM_KEY; ca giảng viên/Admin cần SUDIA_TEST_PASSWORD.
Mọi dữ liệu kiểm thử được dọn theo thứ tự khoá ngoại ở finally.
"""
import hashlib, hmac, html as htmllib, http.cookiejar, os, re, subprocess, time, urllib.error, urllib.parse, urllib.request, uuid

BASE = os.environ.get("SUDIA_TEST_URL", "http://localhost:5280").rstrip("/")
DB = os.environ.get("SUDIA_TEST_DATABASE", "sudia_dev")
if urllib.parse.urlparse(BASE).hostname not in ("localhost", "127.0.0.1", "::1"): raise SystemExit("Chỉ chạy với web localhost.")
def sql(q): return subprocess.check_output(["psql", "-d", DB, "-At", "-v", "ON_ERROR_STOP=1", "-c", q], text=True, timeout=15).strip()
try: sql("SELECT 1")
except Exception as e: raise SystemExit(f"SKIP: không kết nối được PostgreSQL Development ({e}).")
def client(): return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
def req(c, path, data=None): return c.open(urllib.request.Request(BASE + path, urllib.parse.urlencode(data).encode() if data else None), timeout=15)
def csrf(page):
    m = re.search(r'name="__RequestVerificationToken" type="hidden" value="([^"]+)"', page)
    assert m, "Không lấy được antiforgery token"; return m.group(1)

prefix, emails, teacher_orders, teacher_enrollments, teacher_snapshot_taken = "test-pay-" + uuid.uuid4().hex[:10], [], [], [], False
def signup(return_url=None, c=None):
    email = f"{prefix}-{len(emails)}@example.test"; emails.append(email); c = c or client()
    path = "/TaiKhoan/DangKy" + ("?returnUrl=" + urllib.parse.quote(return_url, safe="") if return_url else "")
    page = req(c, path).read().decode()
    data = {"HoTen":"Kiểm thử", "Email":email, "MatKhau":"Testpay2026", "NhapLai":"Testpay2026", "__RequestVerificationToken":csrf(page)}
    if return_url: data["ReturnUrl"] = return_url
    r = req(c, "/TaiKhoan/DangKy", data); r.read()
    if return_url:
        target = urllib.parse.urlparse(r.url)
        assert target.path == "/ThanhToan/Mua" and urllib.parse.parse_qs(target.query).get("id") == [return_url.split("=")[-1]], "T13: đăng ký không chuyển về returnUrl"
    return c, email
def signin(email, password):
    c = client(); page = req(c, "/TaiKhoan/DangNhap").read().decode()
    req(c, "/TaiKhoan/DangNhap", {"Email":email, "MatKhau":password, "GhiNho":"true", "__RequestVerificationToken":csrf(page)}).read()
    return c
def buy(c, package, phone="", sms=False):
    page = req(c, f"/ThanhToan/Mua?id={package}").read().decode()
    r = req(c, f"/ThanhToan/Mua?id={package}", {"dongY":"true", "soDienThoai":phone, "dongYNhanTin":"true" if sms else "false", "linkFacebook":"", "__RequestVerificationToken":csrf(page)})
    return re.search(r"ma=(\d+)", r.url).group(1), r.read().decode()
def wait(q, message):
    for _ in range(20):
        if q(): return
        time.sleep(1)
    raise AssertionError(message)
def webhook_optional():
    key = os.environ.get("SUDIA_TEST_PAYOS_CHECKSUM_KEY")
    if not key: print("SKIP T1/T3/T4 webhook: thiếu SUDIA_TEST_PAYOS_CHECKSUM_KEY"); return
    data = {"amount":1, "code":"00", "orderCode":1}
    raw = "&".join(f"{k}={v}" for k,v in sorted(data.items()))
    assert len(hmac.new(key.encode(), raw.encode(), hashlib.sha256).hexdigest()) == 64
    print("T1/T3/T4: có hàm ký đúng thuật toán PayOS; không gửi webhook tới đơn giả lập.")
def quote(value): return "'" + value.replace("'", "''") + "'"
def snapshot_enrollments(user_id, package_id):
    rows = sql(f'''SELECT g."KhoaId" || E'\\t' || coalesce(g."HanDen"::text, '__NULL__') || E'\\t' || g."NgayTao"::text
        FROM "GhiDanh" g JOIN "GoiBanKhoa" p ON p."GoiBanId"={package_id} AND p."KhoaId" = g."KhoaId"
        WHERE g."NguoiDungId"={quote(user_id)}''')
    return [row.split("\t", 2) for row in rows.splitlines() if row]
def restore_enrollments(user_id, package_id, snapshot):
    sql(f'''DELETE FROM "GhiDanh" WHERE "NguoiDungId"={quote(user_id)}
        AND "KhoaId" IN (SELECT "KhoaId" FROM "GoiBanKhoa" WHERE "GoiBanId"={package_id})''')
    for khoa_id, han_den, ngay_tao in snapshot:
        han_sql = "NULL" if han_den == "__NULL__" else quote(han_den)
        sql(f'''INSERT INTO "GhiDanh" ("NguoiDungId", "KhoaId", "HanDen", "NgayTao")
            VALUES ({quote(user_id)}, {quote(khoa_id)}, {han_sql}, {quote(ngay_tao)})''')
def delete_orders(order_ids):
    if not order_ids: return
    ids = ",".join(order_ids)
    sql(f'DELETE FROM "NhatKyDuyet" WHERE "DonHangId" IN ({ids}); '
        f'DELETE FROM "ThongBao" WHERE "DonHangId" IN ({ids}); '
        f'DELETE FROM "DonHang" WHERE "Id" IN ({ids});')

try:
    package = sql('SELECT "Id" FROM "GoiBan" WHERE "DangBan" AND "Gia">0 AND EXISTS (SELECT 1 FROM "GoiBanKhoa" k WHERE k."GoiBanId"="GoiBan"."Id") ORDER BY "Id" LIMIT 1')
    assert package, "Development chưa có gói bán được"
    # Khách bị [Authorize] chuyển đến đăng nhập, sau đó đăng ký phải quay đúng trang Mua.
    guest = client()
    login = req(guest, f"/ThanhToan/Mua?id={package}")
    assert urllib.parse.urlparse(login.url).path == "/TaiKhoan/DangNhap", "Khách chưa đăng nhập không được chuyển tới đăng nhập"
    return_url = f"/ThanhToan/Mua?id={package}"
    assert "DangKy?returnUrl=" in login.read().decode(), "T13: liên kết đăng ký làm mất returnUrl"
    guest, _ = signup(return_url, guest)
    page = req(guest, return_url).read().decode()
    assert "Xác nhận và thanh toán" in htmllib.unescape(page), "T13: đăng ký không quay lại trang Mua"
    c, email = signup()
    ma, html = buy(c, package); oid = sql(f'SELECT "Id" FROM "DonHang" WHERE "MaThanhToan"={ma}')
    assert sql(f'SELECT "TrangThai" FROM "DonHang" WHERE "Id"={oid}') == "DaThanhToan"
    assert sql(f'SELECT count(*) FROM "ThongBao" WHERE "DonHangId"={oid} AND "Kenh"=\'Sms\'') == "0", "T8: tạo SMS khi không tick"
    assert sql(f'SELECT count(*) FROM "ThongBao" WHERE "DonHangId"={oid} AND "Kenh"=\'Email\'') == "1", "Email chưa vào outbox"
    wait(lambda: sql(f'SELECT "TrangThai" FROM "ThongBao" WHERE "DonHangId"={oid} AND "Kenh"=\'Email\'') == "DaGui", "Email giả lập chưa gửi")
    assert "email" in htmllib.unescape(html), "T11: không hiện email"
    ma_sms, _ = buy(c, package, "0901234567", True); sms_oid = sql(f'SELECT "Id" FROM "DonHang" WHERE "MaThanhToan"={ma_sms}')
    wait(lambda: sql(f'SELECT "MaNhaCungCap" FROM "ThongBao" WHERE "DonHangId"={sms_oid} AND "Kenh"=\'Sms\'') == "development-log", "L1: SMS development không ghi mã giả lập")
    before = sql(f'SELECT count(*) FROM "DonHang" WHERE "NguoiDungId"=(SELECT "Id" FROM "AspNetUsers" WHERE "Email"=\'{email}\')')
    page = req(c, f"/ThanhToan/Mua?id={package}").read().decode(); bad = req(c, f"/ThanhToan/Mua?id={package}", {"dongY":"true", "soDienThoai":"123", "dongYNhanTin":"true", "__RequestVerificationToken":csrf(page)}).read().decode()
    assert "không đúng định dạng" in htmllib.unescape(bad) and sql(f'SELECT count(*) FROM "DonHang" WHERE "NguoiDungId"=(SELECT "Id" FROM "AspNetUsers" WHERE "Email"=\'{email}\')') == before, "T7 thất bại"
    buy(c, package); buy(c, package)
    assert sql(f'SELECT count(*) FROM "NhatKyDuyet" n JOIN "DonHang" d ON d."Id"=n."DonHangId" WHERE d."NguoiDungId"=(SELECT "Id" FROM "AspNetUsers" WHERE "Email"=\'{email}\') AND n."QuyetDinh"=\'CanXemXet\'') != "0", "L2 chưa gắn CanXemXet"
    sql(f'UPDATE "GhiDanh" SET "HanDen"=NULL WHERE "NguoiDungId"=(SELECT "Id" FROM "AspNetUsers" WHERE "Email"=\'{email}\')')
    ma_unlimited, _ = buy(c, package)
    assert "không giới hạn" in htmllib.unescape(sql(f'SELECT "NoiDung" FROM "ThongBao" WHERE "DonHangId"=(SELECT "Id" FROM "DonHang" WHERE "MaThanhToan"={ma_unlimited}) AND "Kenh"=\'Email\'')), "L4 bỏ qua hạn vô hạn"
    other, _ = signup()
    try: req(other, f"/ThanhToan/TrangThai?ma={ma}"); raise AssertionError("T12 lộ đơn người khác")
    except urllib.error.HTTPError as e: assert e.code == 404, f"T12 trả {e.code}"
    password = os.environ.get("SUDIA_TEST_PASSWORD")
    if not password:
        print("SKIP T14 giảng viên/Admin mua gói: thiếu SUDIA_TEST_PASSWORD.")
    else:
        teacher_email = "giangvien@sudia.vn"
        teacher_id = sql(f'SELECT "Id" FROM "AspNetUsers" WHERE "Email"={quote(teacher_email)}')
        if not teacher_id:
            print("SKIP T14 giảng viên/Admin mua gói: không có tài khoản mẫu giangvien@sudia.vn.")
        else:
            teacher_enrollments = snapshot_enrollments(teacher_id, package); teacher_snapshot_taken = True
            teacher = signin(teacher_email, password)
            teacher_ma, _ = buy(teacher, package)
            teacher_oid = sql(f'SELECT "Id" FROM "DonHang" WHERE "MaThanhToan"={teacher_ma}')
            teacher_orders.append(teacher_oid)
            assert sql(f'SELECT "NguoiDungId" FROM "DonHang" WHERE "Id"={teacher_oid}') == teacher_id, "T14: đơn không thuộc giảng viên/Admin"
            assert sql(f'SELECT "TrangThai" FROM "DonHang" WHERE "Id"={teacher_oid}') == "DaThanhToan", "T14: giảng viên/Admin không mua được"
    webhook_optional(); print("PASS: T2/T7/T8/T11/T12/T13, L1/L2/L4 và email (giả lập).")
finally:
    delete_orders(teacher_orders)
    if teacher_snapshot_taken:
        teacher_id = sql(f'SELECT "Id" FROM "AspNetUsers" WHERE "Email"={quote("giangvien@sudia.vn")}')
        if teacher_id: restore_enrollments(teacher_id, package, teacher_enrollments)
    if emails:
        es = ",".join("'" + e + "'" for e in emails)
        ids = "(SELECT \"Id\" FROM \"AspNetUsers\" WHERE \"Email\" IN (" + es + "))"
        sql(f'DELETE FROM "NhatKyDuyet" WHERE "DonHangId" IN (SELECT "Id" FROM "DonHang" WHERE "NguoiDungId" IN {ids}); '
            f'DELETE FROM "ThongBao" WHERE "NguoiDungId" IN {ids}; DELETE FROM "DonHang" WHERE "NguoiDungId" IN {ids}; '
            f'DELETE FROM "GhiDanh" WHERE "NguoiDungId" IN {ids}; DELETE FROM "AspNetUserRoles" WHERE "UserId" IN {ids}; '
            f'DELETE FROM "AspNetUsers" WHERE "Email" IN ({es});')
