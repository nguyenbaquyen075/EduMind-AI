# Kiểm tra Lớp học + nhóm chat: giảng viên tạo lớp → học viên vào bằng mã → nhắn tin (lịch sử, xoá, chống spam, thời gian thực qua SignalR) → xoá lớp.
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


KHOA = 'dia-li-12'
gv = login('giangvien@sudia.vn'); hv = login('hocvien@sudia.vn'); hv2 = login('hv02@sudia.vn'); hv3 = login('hv03@sudia.vn')
lop_id = None
def tk(c, path): return token(req(c, path)[2])
def uid(email): return sql(f"SELECT \"Id\" FROM \"AspNetUsers\" WHERE \"Email\"='{email}'")
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

    # 3. Người ngoài lớp không xem được nhóm chat
    assert req(hv3, f'/Lop/{lop_id}')[0] == 404 and '/DangNhap' in req(client(), f'/Lop/{lop_id}')[1]
    assert ma not in text(req(hv3, '/Lop')[2])
    print('PASS: người ngoài lớp / chưa đăng nhập không xem được lớp')

    # 4. Nhắn tin (có ký tự HTML để thử escape)
    xss = '<script>alert(1)</script>'
    def gui(c, nd, trang=None):
        return req(c, f'/Lop/{lop_id}/Gui', {'noiDung': nd}, headers={'RequestVerificationToken': tk(c, trang or f'/Lop/{lop_id}')})
    s, _, b = gui(hv, 'Chào cả lớp ' + xss); d = json.loads(b); assert s == 200 and d['tin']['laGiangVien'] is False and d['tin']['noiDung'].endswith(xss)
    tin_hv = d['tin']['id']
    html_trang = req(hv, f'/Lop/{lop_id}')[2].decode()
    khoi = json.loads(re.search(r'<script id="ncLichSu" type="application/json">(.*?)</script>', html_trang, re.S)[1])
    assert khoi[0]['noiDung'] == 'Chào cả lớp ' + xss and xss not in html_trang and '<script>alert' not in html_trang, 'mã HTML phải được escape trong trang'
    s, _, b = gui(hv, '   '); assert s == 400 and 'Nhập nội dung' in json.loads(b)['loi']
    s, _, b = gui(hv, 'x' * 2001); assert s == 400 and 'tối đa' in json.loads(b)['loi']
    assert sql(f'SELECT count(*) FROM "TinNhanLop" WHERE "LopId"={lop_id}') == '1'
    print('PASS: học viên nhắn tin; mã HTML được escape; tin trống / quá dài bị chặn')

    # 5. Giảng viên nhắn; người ngoài lớp không đọc / gửi được; lịch sử có nhãn giảng viên
    s, _, b = gui(gv, 'Thầy chào các em'); d = json.loads(b); assert d['tin']['laGiangVien'] is True; tin_gv = d['tin']['id']
    ls = json.loads(req(hv, f'/Lop/{lop_id}/TinNhan')[2]); assert [t['noiDung'] for t in ls['tin']][-1] == 'Thầy chào các em' and ls['coCu'] is False and len(ls['tin']) == 2
    assert req(hv3, f'/Lop/{lop_id}/TinNhan')[0] == 404 and req(hv3, f'/Lop/{lop_id}/Gui', {'noiDung': 'xen vào'}, headers={'RequestVerificationToken': tk(hv3, '/Lop')})[0] == 404
    assert sql(f'SELECT count(*) FROM "TinNhanLop" WHERE "LopId"={lop_id}') == '2'
    print('PASS: giảng viên nhắn có nhãn; lịch sử đúng thứ tự; người ngoài lớp không đọc / gửi được')

    # 6. Xoá tin: người gửi xoá của mình, giảng viên xoá của mọi người, học viên không xoá tin người khác
    xoa = lambda c, tid, trang: req(c, f'/Lop/{lop_id}/TinNhan/{tid}/Xoa', {}, headers={'RequestVerificationToken': tk(c, trang)})
    assert xoa(hv, tin_gv, f'/Lop/{lop_id}')[0] == 404 and sql(f'SELECT count(*) FROM "TinNhanLop" WHERE "Id"={tin_gv}') == '1'
    assert xoa(hv, tin_hv, f'/Lop/{lop_id}')[0] == 200 and sql(f'SELECT count(*) FROM "TinNhanLop" WHERE "Id"={tin_hv}') == '0'
    _, _, b = gui(hv, 'tin để giảng viên xoá'); t2 = json.loads(b)['tin']['id']
    assert xoa(gv, t2, f'/Lop/{lop_id}')[0] == 200 and sql(f'SELECT count(*) FROM "TinNhanLop" WHERE "LopId"={lop_id}') == '1'
    print('PASS: học viên chỉ xoá được tin của mình; giảng viên xoá được tin của học viên')

    # 7. Lịch sử nhiều trang + chống spam
    sql(f'''INSERT INTO "TinNhanLop"("LopId","NguoiDungId","NoiDung","NgayTao") SELECT {lop_id}, u."Id", 'cũ ' || g, now() - interval '2 hours' - (g || ' minutes')::interval
        FROM "AspNetUsers" u, generate_series(1,60) g WHERE u."Email"='hv01@sudia.vn' ''')
    t = text(req(hv, f'/Lop/{lop_id}')[2]); assert 'Xem tin cũ hơn' in t
    trang1 = json.loads(req(hv, f'/Lop/{lop_id}/TinNhan')[2]); assert len(trang1['tin']) == 50 and trang1['coCu'] is True
    trang2 = json.loads(req(hv, f'/Lop/{lop_id}/TinNhan?truoc={trang1["tin"][0]["id"]}')[2]); assert len(trang2['tin']) == 11 and trang2['coCu'] is False
    assert not {x['id'] for x in trang1['tin']} & {x['id'] for x in trang2['tin']}
    sql(f'''INSERT INTO "TinNhanLop"("LopId","NguoiDungId","NoiDung","NgayTao") SELECT {lop_id}, u."Id", 'spam', now() FROM "AspNetUsers" u, generate_series(1,20) WHERE u."Email"='hocvien@sudia.vn' ''')
    s, _, b = gui(hv, 'thêm một tin nữa'); assert s == 400 and 'quá nhanh' in json.loads(b)['loi']
    print('PASS: lịch sử chia trang 50 tin (không trùng giữa các trang); nhắn quá 20 tin/phút bị chặn')

    # 7b. Thời gian thực qua SignalR (dùng long polling để thử bằng Python): thành viên nhận tin mới, người ngoài lớp thì không
    import threading, time
    def kn_ket_noi(c, lop):
        s, _, b = req(c, f'/hub/lop/negotiate?lopId={lop}&negotiateVersion=1', {}); assert s == 200, s
        tok = json.loads(b)['connectionToken']; url = f'/hub/lop?lopId={lop}&id={urllib.parse.quote(tok)}'
        assert req(c, url, raw=b'{"protocol":"json","version":1}\x1e', headers={'Content-Type': 'text/plain;charset=UTF-8'})[0] == 200
        return url
    def poll(c, url, giay):
        try:
            with c.open(urllib.request.Request(BASE + url), timeout=giay) as r: return r.status, r.read().decode('utf-8', 'ignore')
        except Exception as e: return 0, str(e)
    url = kn_ket_noi(hv, lop_id)
    for _ in range(3):                                  # long polling: lần poll đầu trả rỗng (đã nối), lần sau trả phản hồi bắt tay "{}"
        s, hs = poll(hv, url, 10)
        if hs.startswith('{'): break
    assert s == 200 and hs.startswith('{}'), (s, hs)
    ket_qua = {}
    def doi(): ket_qua['x'] = poll(hv, url, 15)
    th = threading.Thread(target=doi); th.start(); time.sleep(1)
    sql(f'DELETE FROM "TinNhanLop" WHERE "NguoiDungId"=\'{uid("hocvien@sudia.vn")}\' AND "NoiDung"=\'spam\'')   # bỏ tin spam để giảng viên gửi được thoải mái
    gui(gv, 'tin thời gian thực ' + ma); th.join(20)
    s, nd = ket_qua['x']; assert s == 200 and 'NhanTin' in nd and 'tin thời gian thực ' + ma in nd, (s, nd[:200])
    url2 = kn_ket_noi(hv3, lop_id); poll(hv3, url2, 5)               # người ngoài lớp: hub ngắt kết nối ngay
    ket_qua.clear(); th = threading.Thread(target=lambda: ket_qua.update(x=poll(hv3, url2, 4))); th.start(); time.sleep(0.5)
    gui(gv, 'tin thứ hai ' + ma); th.join(8); assert 'NhanTin' not in ket_qua['x'][1], 'người ngoài lớp nhận được tin!'
    print('PASS: SignalR: thành viên nhận tin mới ngay khi người khác gửi; người ngoài lớp không nhận được')

    # 8. Học viên rời lớp, đổi mã, ngừng nhận
    req(hv, f'/Lop/{lop_id}/Roi', {'__RequestVerificationToken': tk(hv, '/Lop')}); assert sql(f'SELECT count(*) FROM "LopHocVien" WHERE "LopId"={lop_id}') == '0'
    ma_cu = ma_lop
    s, u, b = req(gv, '/GiangVien/Lop/DoiMa', {'id': lop_id, '__RequestVerificationToken': tk(gv, f'/GiangVien/Lop/ChiTiet/{lop_id}')}); ma_lop = re.search(r'<code>([A-Z0-9]{6})</code>', text(b))[1]
    assert ma_lop != ma_cu and 'Mã lớp không đúng' in text(req(hv, '/Lop/Vao', {'ma': ma_cu, '__RequestVerificationToken': tk(hv, '/Lop')})[2])
    req(gv, '/GiangVien/Lop/MoDong', {'id': lop_id, '__RequestVerificationToken': tk(gv, f'/GiangVien/Lop/ChiTiet/{lop_id}')})
    assert 'ngừng nhận' in text(req(hv, '/Lop/Vao', {'ma': ma_lop, '__RequestVerificationToken': tk(hv, '/Lop')})[2])
    print('PASS: rời lớp; đổi mã làm mã cũ vô hiệu; lớp ngừng nhận thì không vào được')

    # 9. Xoá lớp: tin nhắn xoá theo
    assert sql(f'SELECT count(*) FROM "TinNhanLop" WHERE "LopId"={lop_id}') != '0'
    req(gv, '/GiangVien/Lop/Xoa', {'id': lop_id, '__RequestVerificationToken': tk(gv, f'/GiangVien/Lop/ChiTiet/{lop_id}')})
    assert sql(f'SELECT (SELECT count(*) FROM "Lop" WHERE "Id"={lop_id})+(SELECT count(*) FROM "TinNhanLop" WHERE "LopId"={lop_id})') == '0'
    lop_id = None
    print('PASS: xoá lớp xoá luôn toàn bộ tin nhắn')
    print('\nTẤT CẢ ĐẠT')
finally:
    if lop_id: sql(f'DELETE FROM "Lop" WHERE "Id"={lop_id}')
