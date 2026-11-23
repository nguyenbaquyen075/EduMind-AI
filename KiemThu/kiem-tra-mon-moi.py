# Kiểm tra thêm môn học mới: admin tạo khoá môn mới → học viên học, giảng viên dùng được lớp / ma trận / nội dung.
# Chạy trên web cục bộ + database Development; tự xoá đúng dữ liệu thử mang mã riêng.
# SUDIA_TEST_URL=http://localhost:5280 python3 KiemThu/kiem-tra-mon-moi.py
import html, http.cookiejar, io, json, os, re, ssl, subprocess, urllib.error, urllib.parse, urllib.request, uuid, zipfile
BASE = os.environ.get('SUDIA_TEST_URL', 'http://localhost:5280').rstrip('/')
PASSWORD = os.environ.get('SUDIA_TEST_PASSWORD', 'SuDia@2026')
DATABASE = os.environ.get('SUDIA_TEST_DATABASE', 'sudia_dev')
GOC = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
if urllib.parse.urlparse(BASE).hostname not in ('localhost', '127.0.0.1', '::1'):
    raise SystemExit('Kiểm tra này chỉ chạy trên web cục bộ.')
ma = 'monthu' + uuid.uuid4().hex[:6]          # tên lớp thử riêng
def sql(q): return subprocess.check_output(['psql', '-d', DATABASE, '-At', '-v', 'ON_ERROR_STOP=1', '-c', q], text=True).strip()
def client(): return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()), urllib.request.HTTPSHandler(context=ssl._create_unverified_context()))
def req(c, path, data=None, files=None, headers=None, raw=None):
    headers = dict(headers or {})
    body = None
    if files is not None:
        ranh = '----sudia' + uuid.uuid4().hex
        parts = []
        for k, v in (data or {}).items():
            parts.append(f'--{ranh}\r\nContent-Disposition: form-data; name="{k}"\r\n\r\n{v}\r\n'.encode())
        for k, (ten, noi_dung) in files:
            parts.append(f'--{ranh}\r\nContent-Disposition: form-data; name="{k}"; filename="{ten}"\r\nContent-Type: application/octet-stream\r\n\r\n'.encode() + noi_dung + b'\r\n')
        body = b''.join(parts) + f'--{ranh}--\r\n'.encode()
        headers['Content-Type'] = 'multipart/form-data; boundary=' + ranh
    elif raw is not None:
        body = raw
    elif data is not None:
        body = urllib.parse.urlencode(data).encode()
    try:
        with c.open(urllib.request.Request(BASE + path, body, headers), timeout=60) as r: return r.status, r.url, r.read()
    except urllib.error.HTTPError as e: return e.code, e.url, e.read()
def text(b): return html.unescape(b.decode('utf-8', 'ignore'))
def doi(dieu_kien, giay=10):
    # Chỉ mục cập nhật ở tiến trình nền (~0,5 giây sau khi lưu): chờ tới khi đúng hoặc hết giờ
    import time
    het = time.time() + giay
    while True:
        kq = dieu_kien()
        if kq or time.time() > het: return kq
        time.sleep(0.4)
def token(p): return re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', text(p))[1]
def login(email):
    c = client(); _, _, p = req(c, '/TaiKhoan/DangNhap')
    s, u, _ = req(c, '/TaiKhoan/DangNhap', {'Email': email, 'MatKhau': PASSWORD, '__RequestVerificationToken': token(p)})
    assert s == 200 and '/DangNhap' not in u, (email, s, u); return c


MID = 'gdcd-thu-' + uuid.uuid4().hex[:5]; TEN = 'Giáo dục công dân 12 ' + ma
ad = login('admin@sudia.vn'); gv = login('giangvien@sudia.vn'); hv = login('hocvien@sudia.vn')
lop_id = None
def tk(c, path): return token(req(c, path)[2])
def uid(email): return sql(f"SELECT \"Id\" FROM \"AspNetUsers\" WHERE \"Email\"='{email}'")
try:
    # 1. Admin tạo khoá thuộc môn mới (không nằm trong danh sách gợi ý)
    t = tk(ad, '/Admin/KhoaHoc/Tao')
    s, u, b = req(ad, '/Admin/KhoaHoc/Tao', {'Id': MID, 'Ten': TEN, 'Mon': 'Giáo dục công dân', 'KyHieu': 'GD', 'MoTa': 'Khoá thử', 'GiangVienId': uid('giangvien@sudia.vn'), 'HienThi': 'true', 'SoThang': '12', '__RequestVerificationToken': t})
    assert sql(f"SELECT \"Mon\" FROM \"Khoa\" WHERE \"Id\"='{MID}'") == 'Giáo dục công dân', text(b)[:300]
    assert TEN in text(req(ad, '/Admin/KhoaHoc')[2])
    print('PASS: admin tạo khoá thuộc môn mới "Giáo dục công dân" và gán giảng viên')

    # 2. Nội dung mẫu + ghi danh học viên (thay cho các bước soạn bài)
    sql(f"INSERT INTO \"ChuDe\"(\"KhoaId\",\"Ten\",\"ThuTu\",\"CapNhatLuc\") VALUES ('{MID}','Chủ đề 1 · Thử',0,now())")
    cd = int(sql(f"SELECT \"Id\" FROM \"ChuDe\" WHERE \"KhoaId\"='{MID}'"))
    bai = int(sql(f'''INSERT INTO "Bai"("ChuDeId","So","Ten","SoMuc","SoPhut","ThuTu","HocThu","DaXuatBan","CapNhatLuc") VALUES ({cd},1,'Bài thử {ma}',0,10,0,false,true,now()) RETURNING "Id"''').splitlines()[0])
    sql(f'''INSERT INTO "CauHoi"("BaiId","ThuTu","NoiDung","PhuongAn","DapAnDung","GiaiThich","NguonHienThi","DoKho","CapNhatLuc") VALUES ({bai},0,'Câu {ma} một cộng một bằng mấy?',ARRAY['1','2','3','4'],'B','Một cộng một bằng hai','x',2,now())''')
    sql(f"INSERT INTO \"GhiDanh\"(\"NguoiDungId\",\"KhoaId\",\"HanDen\",\"NgayTao\") VALUES ('{uid('hocvien@sudia.vn')}','{MID}',NULL,now())")
    t = text(req(hv, f'/Home/KhoaHoc/{MID}')[2]); assert TEN in t, 'học viên thấy khoá mới'
    s, _, b = req(hv, f'/Home/LuyenDe/{MID}'); assert s == 200 and TEN.split(' 12 ')[0] in text(b)
    t = text(req(hv, f'/Home/LamCauHoi/{bai}')[2]); assert TEN in t and 'Lịch sử 12' not in t.split('<main')[1][:600], 'đầu trang làm bài phải hiện đúng tên khoá, không phải "Lịch sử 12"'
    print('PASS: học viên thấy khoá mới, luyện đề được, đầu trang làm bài hiện đúng tên khoá')

    # 3. Giảng viên dùng được các công cụ cho khoá mới
    t = tk(gv, '/GiangVien/Lop/Tao')
    s, u, b = req(gv, '/GiangVien/Lop/Luu', {'Id': '0', 'KhoaId': MID, 'Ten': 'Lớp ' + ma, 'MoTa': '', '__RequestVerificationToken': t})
    m = re.search(r'/GiangVien/Lop/ChiTiet/(\d+)', u); assert m, text(b)[:200]; lop_id = int(m[1])
    assert TEN in text(req(gv, f'/GiangVien/DeThi/MaTran?khoa={MID}')[2]) and 'Bài 1. Bài thử' in text(req(gv, f'/GiangVien/DeThi/MaTran?khoa={MID}')[2])
    assert TEN in text(req(gv, f'/GiangVien/NoiDung')[2]) and TEN in text(req(gv, f'/GiangVien/HocVien?khoa={MID}')[2])
    print('PASS: giảng viên tạo lớp, ra đề theo ma trận, quản lý nội dung và học viên cho khoá mới')
    print('\nTẤT CẢ ĐẠT')
finally:
    if lop_id: sql(f'DELETE FROM "Lop" WHERE "Id"={lop_id}')
    sql(f"DELETE FROM \"GhiDanh\" WHERE \"KhoaId\"='{MID}'")
    sql(f"DELETE FROM \"CauHoi\" WHERE \"BaiId\" IN (SELECT b.\"Id\" FROM \"Bai\" b JOIN \"ChuDe\" c ON c.\"Id\"=b.\"ChuDeId\" WHERE c.\"KhoaId\"='{MID}')")
    sql(f"DELETE FROM \"DoanChiMuc\" WHERE \"KhoaId\"='{MID}'")
    sql(f"DELETE FROM \"Bai\" WHERE \"ChuDeId\" IN (SELECT \"Id\" FROM \"ChuDe\" WHERE \"KhoaId\"='{MID}')")
    sql(f"DELETE FROM \"ChuDe\" WHERE \"KhoaId\"='{MID}'")
    sql(f"DELETE FROM \"GoiBanKhoa\" WHERE \"KhoaId\"='{MID}'")
    sql(f"DELETE FROM \"Khoa\" WHERE \"Id\"='{MID}'")
