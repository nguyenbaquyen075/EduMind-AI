# Kiểm tra Lớp học + diễn đàn: giảng viên tạo lớp → học viên vào bằng mã → hỏi đáp, ghim, đóng, đáp án → xoá lớp.
# Chạy trên web cục bộ + database Development; tự xoá đúng dữ liệu thử mang mã riêng.
# SUDIA_TEST_URL=http://localhost:5280 python3 KiemThu/kiem-tra-lop-hoc.py
import html, http.cookiejar, io, json, os, re, ssl, subprocess, urllib.error, urllib.parse, urllib.request, uuid, zipfile
BASE = os.environ.get('SUDIA_TEST_URL', 'http://localhost:5280').rstrip('/')
PASSWORD = os.environ.get('SUDIA_TEST_PASSWORD', 'SuDia@2026')
DATABASE = os.environ.get('SUDIA_TEST_DATABASE', 'sudia_dev')
GOC = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
if urllib.parse.urlparse(BASE).hostname not in ('localhost', '127.0.0.1', '::1'):
    raise SystemExit('Kiểm tra này chỉ chạy trên web cục bộ.')
ma = 'lopthu' + uuid.uuid4().hex[:6]          # tên lớp thử riêng
def sql(q): return subprocess.check_output(['psql', '-d', DATABASE, '-At', '-v', 'ON_ERROR_STOP=1', '-c', q], text=True).strip()
def client(): return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()), urllib.request.HTTPSHandler(context=ssl._create_unverified_context()))
def req(c, path, data=None, files=None, headers=None):
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


KHOA = 'dia-li-12'
gv = login('giangvien@sudia.vn'); hv = login('hocvien@sudia.vn'); hv2 = login('hv02@sudia.vn'); hv3 = login('hv03@sudia.vn')
lop_id = None
def tk(c, path): return token(req(c, path)[2])
try:
    # 1. Giảng viên tạo lớp
    t = tk(gv, '/GiangVien/Lop/Tao')
    s, u, b = req(gv, '/GiangVien/Lop/Luu', {'Id': '0', 'KhoaId': KHOA, 'Ten': ma, 'MoTa': 'Lớp thử', '__RequestVerificationToken': t})
    m = re.search(r'/GiangVien/Lop/ChiTiet/(\d+)', u); assert s == 200 and m, (s, u); lop_id = int(m[1])
    ma_lop = re.search(r'<code>([A-Z0-9]{6})</code>', text(b))[1]
    print(f'PASS: giảng viên tạo lớp {ma} (mã {ma_lop})')
    assert req(gv, '/GiangVien/Lop/Luu', {'Id': '0', 'KhoaId': 'khong-co', 'Ten': 'x', '__RequestVerificationToken': t})[0] == 200
    assert sql('SELECT count(*) FROM "Lop" WHERE "KhoaId"=\'khong-co\'') == '0'
    s, _, b = req(gv, '/GiangVien/Lop/Luu', {'Id': '0', 'KhoaId': KHOA, 'Ten': '   ', '__RequestVerificationToken': t}); assert 'Nhập tên lớp' in text(b)
    print('PASS: không tạo được lớp với khoá không phải của mình hoặc tên trống')

    # 2. Học viên vào lớp
    t = tk(hv, '/Lop')
    s, u, b = req(hv, '/Lop/Vao', {'ma': 'ZZZZZZ', '__RequestVerificationToken': t}); assert 'Mã lớp không đúng' in text(b)
    s, u, b = req(hv2, '/Lop/Vao', {'ma': ma_lop.lower(), '__RequestVerificationToken': tk(hv2, '/Lop')})
    assert 'chưa ghi danh' in text(b) and sql(f'SELECT count(*) FROM "LopHocVien" WHERE "LopId"={lop_id}') == '0', 'hv chưa ghi danh khoá vẫn vào được'
    s, u, b = req(hv, '/Lop/Vao', {'ma': ' ' + ma_lop.lower() + ' ', '__RequestVerificationToken': t})
    assert f'/Lop/{lop_id}' in u and ma in text(b), (s, u)
    assert sql(f'SELECT count(*) FROM "LopHocVien" WHERE "LopId"={lop_id}') == '1'
    print('PASS: mã sai bị từ chối; học viên chưa ghi danh khoá không vào được; học viên đúng vào được (không phân biệt hoa thường)')

    # 3. Người ngoài lớp không xem được diễn đàn
    assert req(hv3, f'/Lop/{lop_id}')[0] == 404 and '/DangNhap' in req(client(), f'/Lop/{lop_id}')[1]
    assert ma not in text(req(hv3, '/Lop')[2])
    print('PASS: người ngoài lớp / chưa đăng nhập không xem được lớp')

    # 4. Học viên đăng bài (có ký tự HTML để thử escape)
    xss = '<script>alert(1)</script>'
    t = tk(hv, f'/Lop/{lop_id}')
    s, u, b = req(hv, f'/Lop/{lop_id}/DangBai', {'tieuDe': 'Câu hỏi ' + ma, 'noiDung': 'Nội dung ' + xss, '__RequestVerificationToken': t})
    mb = re.search(r'/Lop/BaiDang/(\d+)', u); assert mb, (s, u); bai = int(mb[1])
    assert xss not in b.decode() and '&lt;script&gt;' in b.decode()
    s, u, b = req(hv, f'/Lop/{lop_id}/DangBai', {'tieuDe': '', 'noiDung': 'x', '__RequestVerificationToken': t}); assert 'Tiêu đề cần' in text(b)
    s, u, b = req(hv, f'/Lop/{lop_id}/DangBai', {'tieuDe': 'a', 'noiDung': 'x' * 4001, '__RequestVerificationToken': t}); assert 'Nội dung cần' in text(b)
    assert sql(f'SELECT count(*) FROM "BaiDang" WHERE "LopId"={lop_id}') == '1'
    print('PASS: đăng bài đúng; mã HTML được escape; tiêu đề trống / nội dung quá dài bị chặn')

    # 5. Giảng viên trả lời; học viên đánh dấu đáp án; người ngoài không động được
    t = tk(gv, f'/Lop/BaiDang/{bai}')
    s, u, b = req(gv, f'/Lop/BaiDang/{bai}/TraLoi', {'noiDung': 'Trả lời của giảng viên', '__RequestVerificationToken': t}); assert 'Trả lời của giảng viên' in text(b) and '(giảng viên)' in text(b)
    tl = int(sql(f'SELECT "Id" FROM "TraLoiBaiDang" WHERE "BaiDangId"={bai}'))
    assert req(hv3, f'/Lop/BaiDang/{bai}/TraLoi', {'noiDung': 'xen vào', '__RequestVerificationToken': tk(hv3, '/Lop')})[0] == 404
    assert sql(f'SELECT count(*) FROM "TraLoiBaiDang" WHERE "BaiDangId"={bai}') == '1'
    s, u, b = req(hv, f'/Lop/BaiDang/{bai}/DapAn/{tl}', {'__RequestVerificationToken': tk(hv, f'/Lop/BaiDang/{bai}')}); assert 'Đáp án' in text(b)
    assert sql(f'SELECT "LaDapAn" FROM "TraLoiBaiDang" WHERE "Id"={tl}') == 't'
    print('PASS: giảng viên trả lời (có nhãn); học viên đánh dấu đáp án; người ngoài lớp không trả lời được')

    # 6. Quyền: học viên không ghim / đóng; giảng viên ghim và đóng
    th = tk(hv, f'/Lop/BaiDang/{bai}')
    assert req(hv, f'/Lop/BaiDang/{bai}/Ghim', {'__RequestVerificationToken': th})[0] == 404
    assert req(hv, f'/Lop/BaiDang/{bai}/Dong', {'__RequestVerificationToken': th})[0] == 404
    assert sql(f'SELECT "Ghim"::text||"DaDong"::text FROM "BaiDang" WHERE "Id"={bai}') == 'falsefalse'
    req(gv, f'/Lop/BaiDang/{bai}/Ghim', {'__RequestVerificationToken': t}); req(gv, f'/Lop/BaiDang/{bai}/Dong', {'__RequestVerificationToken': t})
    assert sql(f'SELECT "Ghim"::text||"DaDong"::text FROM "BaiDang" WHERE "Id"={bai}') == 'truetrue'
    s, u, b = req(hv, f'/Lop/BaiDang/{bai}/TraLoi', {'noiDung': 'sau khi đóng', '__RequestVerificationToken': th}); assert 'đã đóng' in text(b)
    assert sql(f'SELECT count(*) FROM "TraLoiBaiDang" WHERE "BaiDangId"={bai}') == '1'
    print('PASS: học viên không ghim/đóng được; giảng viên ghim và đóng; bài đóng không nhận trả lời')

    # 7. Chống spam
    sql(f'''INSERT INTO "BaiDang"("LopId","NguoiDungId","TieuDe","NoiDung","Ghim","DaDong","NgayTao","CapNhatLuc")
        SELECT {lop_id}, u."Id", 'spam', 'spam', false, false, now(), now() FROM "AspNetUsers" u, generate_series(1,10) WHERE u."Email"='hocvien@sudia.vn' ''')
    s, u, b = req(hv, f'/Lop/{lop_id}/DangBai', {'tieuDe': 'thêm', 'noiDung': 'thêm', '__RequestVerificationToken': tk(hv, f'/Lop/{lop_id}')}); assert 'hơi nhiều' in text(b)
    print('PASS: đăng quá 10 bài trong một giờ bị chặn')

    # 8. Học viên rời lớp, đổi mã, ngừng nhận
    req(hv, f'/Lop/{lop_id}/Roi', {'__RequestVerificationToken': tk(hv, '/Lop')}); assert sql(f'SELECT count(*) FROM "LopHocVien" WHERE "LopId"={lop_id}') == '0'
    ma_cu = ma_lop
    s, u, b = req(gv, '/GiangVien/Lop/DoiMa', {'id': lop_id, '__RequestVerificationToken': tk(gv, f'/GiangVien/Lop/ChiTiet/{lop_id}')}); ma_lop = re.search(r'<code>([A-Z0-9]{6})</code>', text(b))[1]
    assert ma_lop != ma_cu and 'Mã lớp không đúng' in text(req(hv, '/Lop/Vao', {'ma': ma_cu, '__RequestVerificationToken': tk(hv, '/Lop')})[2])
    req(gv, '/GiangVien/Lop/MoDong', {'id': lop_id, '__RequestVerificationToken': tk(gv, f'/GiangVien/Lop/ChiTiet/{lop_id}')})
    assert 'ngừng nhận' in text(req(hv, '/Lop/Vao', {'ma': ma_lop, '__RequestVerificationToken': tk(hv, '/Lop')})[2])
    print('PASS: rời lớp; đổi mã làm mã cũ vô hiệu; lớp ngừng nhận thì không vào được')

    # 9. Xoá lớp: bài đăng, trả lời xoá theo
    assert sql(f'SELECT count(*) FROM "TraLoiBaiDang" WHERE "BaiDangId"={bai}') != '0'
    req(gv, '/GiangVien/Lop/Xoa', {'id': lop_id, '__RequestVerificationToken': tk(gv, f'/GiangVien/Lop/ChiTiet/{lop_id}')})
    assert sql(f'SELECT (SELECT count(*) FROM "Lop" WHERE "Id"={lop_id})+(SELECT count(*) FROM "BaiDang" WHERE "LopId"={lop_id})+(SELECT count(*) FROM "TraLoiBaiDang" WHERE "BaiDangId"={bai})') == '0'
    lop_id = None
    print('PASS: xoá lớp xoá luôn bài đăng, trả lời')
    print('\nTẤT CẢ ĐẠT')
finally:
    if lop_id: sql(f'DELETE FROM "Lop" WHERE "Id"={lop_id}')
