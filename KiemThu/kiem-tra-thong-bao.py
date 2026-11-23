# Kiểm tra Thông báo + biểu đồ + nhắc hạn: nhóm chat/giao bài sinh thông báo, chuông, đọc/đọc hết, phổ điểm, nhắc hạn không gửi trùng.
# Chạy trên web cục bộ + database Development; tự xoá đúng dữ liệu thử mang mã riêng.
# SUDIA_TEST_URL=http://localhost:5280 python3 KiemThu/kiem-tra-thong-bao.py
import html, http.cookiejar, io, json, os, re, ssl, subprocess, urllib.error, urllib.parse, urllib.request, uuid, zipfile
BASE = os.environ.get('SUDIA_TEST_URL', 'http://localhost:5280').rstrip('/')
PASSWORD = os.environ.get('SUDIA_TEST_PASSWORD', 'SuDia@2026')
DATABASE = os.environ.get('SUDIA_TEST_DATABASE', 'sudia_dev')
GOC = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
if urllib.parse.urlparse(BASE).hostname not in ('localhost', '127.0.0.1', '::1'):
    raise SystemExit('Kiểm tra này chỉ chạy trên web cục bộ.')
ma = 'tbthu' + uuid.uuid4().hex[:6]          # tên lớp thử riêng
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
gv = login('giangvien@sudia.vn'); hv = login('hocvien@sudia.vn'); hv01 = login('hv01@sudia.vn'); hv08 = login('hv08@sudia.vn')
lop_id = bt_id = None; tao_gd = False
def tk(c, path): return token(req(c, path)[2])
def post_json(c, path, obj, tok):
    return req(c, path, None, headers={'Content-Type': 'application/json', 'RequestVerificationToken': tok}, raw=json.dumps(obj).encode())
def uid(email): return sql(f"SELECT \"Id\" FROM \"AspNetUsers\" WHERE \"Email\"='{email}'")
def tin(email, mau): return int(sql(f"SELECT count(*) FROM \"TinThongBao\" WHERE \"NguoiDungId\"='{uid(email)}' AND (\"TieuDe\" LIKE '%{mau}%' OR \"NoiDung\" LIKE '%{mau}%')"))
def dem(c): return json.loads(req(c, '/ThongBao/Dem')[2])['dem']
try:
    # 0. Chuông: chưa đăng nhập bị chuyển đi, đã đăng nhập đọc được số
    assert '/DangNhap' in req(client(), '/ThongBao/Dem')[1] and isinstance(dem(hv), int)
    print('PASS: /ThongBao/Dem cần đăng nhập và trả số tin chưa đọc')

    # 1. Nhóm chat sinh thông báo (mỗi lớp tối đa một thông báo mỗi giờ theo từng chiều)
    t = tk(gv, '/GiangVien/Lop/Tao')
    s, u, b = req(gv, '/GiangVien/Lop/Luu', {'Id': '0', 'KhoaId': KHOA, 'Ten': ma, 'MoTa': '', '__RequestVerificationToken': t})
    lop_id = int(re.search(r'/GiangVien/Lop/ChiTiet/(\d+)', u)[1]); ma_lop = re.search(r'<code>([A-Z0-9]{6})</code>', text(b))[1]
    req(hv, '/Lop/Vao', {'ma': ma_lop, '__RequestVerificationToken': tk(hv, '/Lop')})
    req(hv01, '/Lop/Vao', {'ma': ma_lop, '__RequestVerificationToken': tk(hv01, '/Lop')})
    gui = lambda c, nd: req(c, f'/Lop/{lop_id}/Gui', {'noiDung': nd}, headers={'RequestVerificationToken': tk(c, f'/Lop/{lop_id}')})
    assert gui(hv, 'câu hỏi đầu')[0] == 200 and gui(hv, 'câu hỏi thứ hai')[0] == 200
    assert tin('giangvien@sudia.vn', ma) == 1 and tin('hocvien@sudia.vn', ma) == 0 and tin('hv01@sudia.vn', ma) == 0, 'học viên nhắn → chỉ giảng viên được báo, và chỉ một thông báo'
    assert gui(gv, 'thầy trả lời')[0] == 200 and gui(gv, 'thầy nhắn thêm')[0] == 200
    assert tin('hocvien@sudia.vn', ma) == 1 and tin('hv01@sudia.vn', ma) == 1 and int(sql(f"SELECT count(*) FROM \"TinThongBao\" WHERE \"NguoiDungId\"='{uid('giangvien@sudia.vn')}' AND \"Loai\"='ChatGiangVien'")) == 0, 'giảng viên nhắn → cả lớp (trừ mình) được báo, một thông báo'
    bai = lop_id
    print('PASS: học viên nhắn → báo giảng viên; giảng viên nhắn → báo cả lớp; nhắn nhiều tin trong một giờ chỉ có một thông báo')

    # 2. Đọc, mở, đọc hết, người khác không mở được
    truoc = dem(hv); assert truoc >= 1
    trang = text(req(hv, '/ThongBao')[2]); assert 'Tin nhắn mới trong lớp' in trang
    tid = int(sql(f"SELECT \"Id\" FROM \"TinThongBao\" WHERE \"NguoiDungId\"='{uid('hocvien@sudia.vn')}' AND \"TieuDe\" LIKE '%{ma}%' LIMIT 1"))
    assert req(hv01, f'/ThongBao/Mo/{tid}', {'__RequestVerificationToken': tk(hv01, '/ThongBao')})[0] == 404
    s, u, _ = req(hv, f'/ThongBao/Mo/{tid}', {'__RequestVerificationToken': tk(hv, '/ThongBao')}); assert f'/Lop/{lop_id}' in u, u
    assert sql(f'SELECT "DaDoc"::text FROM "TinThongBao" WHERE "Id"={tid}') == 'true' and dem(hv) == truoc - 1
    req(hv, '/ThongBao/DocHet', {'__RequestVerificationToken': tk(hv, '/ThongBao')}); assert dem(hv) == 0
    sql(f"UPDATE \"TinThongBao\" SET \"Link\"='https://example.com/x' WHERE \"Id\"={tid}")
    assert 'example.com' not in req(hv, f'/ThongBao/Mo/{tid}', {'__RequestVerificationToken': tk(hv, '/ThongBao')})[1]
    print('PASS: mở tin → đã đọc + chuyển trang; người khác không mở được; đọc hết; link ngoài bị chặn')

    # 3. Giao bài → học viên đang học khoá được báo
    page = req(gv, '/GiangVien/CauHoi')[2]; tkn = token(page)
    baih = int(sql(f"SELECT b.\"Id\" FROM \"Bai\" b JOIN \"ChuDe\" c ON c.\"Id\"=b.\"ChuDeId\" WHERE c.\"KhoaId\"='{KHOA}' ORDER BY b.\"Id\" LIMIT 1"))
    cau = [{'id': 'tn1', 'kieu': 'TN', 'noiDung': 'Câu ' + ma, 'diem': 10, 'phuongAn': ['A', 'B', 'C', 'D'], 'dapAn': 1, 'y': [], 'loiGiai': 'x'}]
    s, _, b = post_json(gv, '/GiangVien/GiaoBai/Luu', {'id': 0, 'baiId': baih, 'buoiHocId': None, 'ten': 'BT ' + ma, 'huongDan': 'h', 'hanNop': None, 'daGiao': True, 'cau': cau}, tkn)
    assert s == 200, (s, b[:300]); bt_id = json.loads(b)['id']
    assert tin('hocvien@sudia.vn', 'BT ' + ma) == 1 and tin('hv01@sudia.vn', 'BT ' + ma) == 1 and tin('hv02@sudia.vn', 'BT ' + ma) == 0, 'giao bài → học viên khoá này (hv02 chỉ học Sử) '
    print('PASS: giao bài → báo đúng học viên đang học khoá (không báo người ngoài khoá)')

    # 4. Biểu đồ: phổ điểm (giảng viên) + điểm theo thời gian, tiến độ (học viên)
    sql(f'''INSERT INTO "BaiNopGiao"("BaiTapGiaoId","NguoiDungId","TraLoiJson","DiemTuDong","DiemTuLuan","NopLuc")
        SELECT {bt_id}, u."Id", '{{}}', CASE u."Email" WHEN 'hocvien@sudia.vn' THEN 10 ELSE 4 END, NULL, now() FROM "AspNetUsers" u WHERE u."Email" IN ('hocvien@sudia.vn','hv01@sudia.vn')''')
    trang = text(req(gv, f'/GiangVien/GiaoBai/BaiNop/{bt_id}')[2])
    assert 'Phổ điểm của lớp' in trang and 'bd-cot' in trang and 'trung bình 7.0' in trang.replace(',', '.'), 'phổ điểm'
    trang = text(req(hv, '/Home/Diem')[2])
    assert 'Điểm theo thời gian' in trang and 'bd-diem' in trang and 'Tiến độ học theo khoá' in trang, 'biểu đồ học viên'
    print('PASS: giảng viên thấy phổ điểm (có trung bình); học viên thấy biểu đồ điểm và tiến độ')

    # 5. Nhắc hạn: khoá sắp hết hạn, bài sắp đến hạn chưa nộp; chạy lại không gửi trùng
    sql(f'''INSERT INTO "GhiDanh"("NguoiDungId","KhoaId","HanDen","NgayTao") SELECT u."Id", '{KHOA}', now() + interval '3 days', now() FROM "AspNetUsers" u WHERE u."Email"='hv08@sudia.vn'
        ON CONFLICT ("NguoiDungId","KhoaId") DO UPDATE SET "HanDen"=now() + interval '3 days' ''')
    tao_gd = True
    sql(f"UPDATE \"BaiTapGiao\" SET \"HanNop\"=now() + interval '20 hours' WHERE \"Id\"={bt_id}")
    r1 = json.loads(req(hv08, '/ThongBao/nhac-han-thu')[2])['soTin']
    het = int(sql(f"SELECT count(*) FROM \"TinThongBao\" WHERE \"NguoiDungId\"='{uid('hv08@sudia.vn')}' AND \"Loai\"='SapHetHan' AND \"KhoaDuyNhat\" LIKE 'hanhoc:{KHOA}:%'"))
    den = int(sql(f"SELECT count(*) FROM \"TinThongBao\" WHERE \"Loai\"='SapDenHan' AND \"KhoaDuyNhat\"='hannop:{bt_id}'"))
    assert r1 >= 2 and het == 1, (r1, het)
    nguoi_nhan = sql(f"SELECT string_agg(u.\"Email\",',' ORDER BY u.\"Email\") FROM \"TinThongBao\" t JOIN \"AspNetUsers\" u ON u.\"Id\"=t.\"NguoiDungId\" WHERE t.\"KhoaDuyNhat\"='hannop:{bt_id}'")
    assert 'hocvien@sudia.vn' not in nguoi_nhan and 'hv01@sudia.vn' not in nguoi_nhan and 'hv08@sudia.vn' in nguoi_nhan, nguoi_nhan   # hai người đã nộp thì không bị nhắc
    json.loads(req(hv08, '/ThongBao/nhac-han-thu')[2])
    assert int(sql(f"SELECT count(*) FROM \"TinThongBao\" WHERE \"NguoiDungId\"='{uid('hv08@sudia.vn')}' AND \"KhoaDuyNhat\" LIKE 'hanhoc:{KHOA}:%'")) == 1
    assert int(sql(f"SELECT count(*) FROM \"TinThongBao\" WHERE \"KhoaDuyNhat\"='hannop:{bt_id}'")) == int(den)
    print('PASS: nhắc khoá sắp hết hạn + bài sắp đến hạn (chỉ người chưa nộp); chạy lại không gửi trùng')
    print('\nTẤT CẢ ĐẠT')
finally:
    if bt_id:
        sql(f'DELETE FROM "TinThongBao" WHERE "KhoaDuyNhat"=\'hannop:{bt_id}\'')
        sql(f'DELETE FROM "BaiNopGiao" WHERE "BaiTapGiaoId"={bt_id}')
        sql(f'DELETE FROM "DeThi" WHERE "BaiTapGiaoId"={bt_id}')
        sql(f'DELETE FROM "BaiTapGiao" WHERE "Id"={bt_id}')
    sql(f"DELETE FROM \"TinThongBao\" WHERE \"TieuDe\" LIKE '%{ma}%' OR \"NoiDung\" LIKE '%{ma}%'")
    if tao_gd: sql(f"DELETE FROM \"TinThongBao\" WHERE \"KhoaDuyNhat\" LIKE 'hanhoc:{KHOA}:%' AND \"NguoiDungId\"='{uid('hv08@sudia.vn')}'")
    if lop_id: sql(f'DELETE FROM "Lop" WHERE "Id"={lop_id}')
