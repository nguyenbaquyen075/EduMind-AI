#!/usr/bin/env python3
"""Kiểm thử thanh toán Development: cần web localhost và PostgreSQL qua psql.

Không có khoá trong tệp. T1/T3/T4 chỉ chạy khi có SUDIA_TEST_PAYOS_CHECKSUM_KEY;
các ca giả lập chạy bằng tài khoản tạm và luôn dọn dữ liệu ở finally.
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

prefix, emails = "test-pay-" + uuid.uuid4().hex[:10], []
def signup():
    email = f"{prefix}-{len(emails)}@example.test"; emails.append(email); c = client()
    page = req(c, "/TaiKhoan/DangKy").read().decode()
    req(c, "/TaiKhoan/DangKy", {"HoTen":"Kiểm thử", "Email":email, "MatKhau":"Testpay2026", "NhapLai":"Testpay2026", "__RequestVerificationToken":csrf(page)}).read()
    return c, email
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

try:
    package = sql('SELECT "Id" FROM "GoiBan" WHERE "DangBan" AND "Gia">0 AND cardinality("KhoaIds")>0 ORDER BY "Id" LIMIT 1')
    assert package, "Development chưa có gói bán được"
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
    webhook_optional(); print("PASS: T2/T7/T8/T11/T12, L1/L2/L4 và email (giả lập).")
finally:
    if emails:
        es = ",".join("'" + e + "'" for e in emails)
        ids = "(SELECT \"Id\" FROM \"AspNetUsers\" WHERE \"Email\" IN (" + es + "))"
        sql(f'DELETE FROM "NhatKyDuyet" WHERE "DonHangId" IN (SELECT "Id" FROM "DonHang" WHERE "NguoiDungId" IN {ids}); '
            f'DELETE FROM "ThongBao" WHERE "NguoiDungId" IN {ids}; DELETE FROM "DonHang" WHERE "NguoiDungId" IN {ids}; '
            f'DELETE FROM "GhiDanh" WHERE "NguoiDungId" IN {ids}; DELETE FROM "AspNetUserRoles" WHERE "UserId" IN {ids}; '
            f'DELETE FROM "AspNetUsers" WHERE "Email" IN ({es});')
