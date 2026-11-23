# Kiểm tra ngân hàng đề + chứng chỉ: độ khó câu hỏi, ra đề theo ma trận, cấp / xem / xác thực chứng chỉ.
# Chạy trên web cục bộ + database Development; tự xoá đúng dữ liệu thử mang mã riêng.
# SUDIA_TEST_URL=http://localhost:5280 python3 KiemThu/kiem-tra-ngan-hang-chung-chi.py
import html, http.cookiejar, io, json, os, re, ssl, subprocess, urllib.error, urllib.parse, urllib.request, uuid, zipfile
BASE = os.environ.get('SUDIA_TEST_URL', 'http://localhost:5280').rstrip('/')
PASSWORD = os.environ.get('SUDIA_TEST_PASSWORD', 'SuDia@2026')
DATABASE = os.environ.get('SUDIA_TEST_DATABASE', 'sudia_dev')
GOC = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
if urllib.parse.urlparse(BASE).hostname not in ('localhost', '127.0.0.1', '::1'):
    raise SystemExit('Kiểm tra này chỉ chạy trên web cục bộ.')
ma = 'nhthu' + uuid.uuid4().hex[:6]          # tên lớp thử riêng
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


KHOA = 'dia-li-12'; BAI = 101; EM = 'hv14@sudia.vn'
gv = login('giangvien@sudia.vn'); hv = login(EM); ad = login('admin@sudia.vn'); hv_khac = login('hv13@sudia.vn')
cau_id = de_id = None; tao_gd = False; goc = {}
def tk(c, path): return token(req(c, path)[2])
def uid(email): return sql(f"SELECT \"Id\" FROM \"AspNetUsers\" WHERE \"Email\"='{email}'")
def form_cau(dokho, **kw):
    d = {'Id': '0', 'ViTri': str(BAI), 'NoiDung': 'Câu ngân hàng ' + ma, 'A': 'a1', 'B': 'b1', 'C': 'c1', 'D': 'd1', 'DapAn': 'B', 'GiaiThich': 'Giải thích', 'DoKho': str(dokho)}
    d.update(kw); return d
try:
    # 1. Độ khó của câu hỏi
    t = tk(gv, f'/GiangVien/CauHoi/Tao?bai={BAI}')
    s, u, b = req(gv, '/GiangVien/CauHoi/Luu', {**form_cau(3), '__RequestVerificationToken': t}); assert s == 200, s
    cau_id = int(sql(f"SELECT \"Id\" FROM \"CauHoi\" WHERE \"NoiDung\"='Câu ngân hàng {ma}'"))
    assert sql(f'SELECT "DoKho" FROM "CauHoi" WHERE "Id"={cau_id}') == '3'
    s, u, b = req(gv, '/GiangVien/CauHoi/Luu', {**form_cau(7, NoiDung='Câu sai độ khó ' + ma), '__RequestVerificationToken': t})
    assert sql(f"SELECT count(*) FROM \"CauHoi\" WHERE \"NoiDung\"='Câu sai độ khó {ma}'") == '0', 'độ khó 7 vẫn lưu được'
    assert 'Khó' in text(req(gv, f'/GiangVien/CauHoi?bai={BAI}')[2])
    print('PASS: đặt độ khó cho câu hỏi; độ khó ngoài 1–3 bị từ chối; danh sách hiện nhãn độ khó')
    assert sql("SELECT count(*) FROM \"CauHoi\" WHERE \"DoKho\" NOT BETWEEN 1 AND 3") == '0'

    # 2. Ra đề theo ma trận
    page = text(req(gv, f'/GiangVien/DeThi/MaTran?khoa={KHOA}')[2]); assert 'Ra đề theo ma trận' in page and 'name="Dong[0].BaiId"' in page
    t = token(req(gv, f'/GiangVien/DeThi/MaTran?khoa={KHOA}')[2])
    n_tb = int(sql(f'SELECT count(*) FROM "CauHoi" WHERE "BaiId"={BAI} AND "DoKho"=2'))
    ds_bai = [int(x) for x in re.findall(r'name="Dong\[\d+\]\.BaiId" value="(\d+)"', page)]; i101 = ds_bai.index(BAI)
    def mt(de, tb, kho, **kw):
        d = {'KhoaId': KHOA, 'Ten': 'Đề ' + ma, 'SoPhut': '30', '__RequestVerificationToken': t}
        for i, b in enumerate(ds_bai): d[f'Dong[{i}].BaiId'] = str(b); d[f'Dong[{i}].De'] = '0'; d[f'Dong[{i}].TB'] = str(tb if b == BAI else 0); d[f'Dong[{i}].Kho'] = str(kho if b == BAI else 0)
        d.update(kw); return d
    s, u, b = req(gv, '/GiangVien/DeThi/TaoMaTran', mt(0, n_tb + 5, 1)); assert 'ngân hàng chỉ có' in text(b), 'lấy quá số câu có'
    s, u, b = req(gv, '/GiangVien/DeThi/TaoMaTran', mt(0, 0, 0)); assert 'ít nhất một bài' in text(b)
    assert sql(f"SELECT count(*) FROM \"DeThi\" WHERE \"Ten\"='Đề {ma}'") == '0'
    s, u, b = req(gv, '/GiangVien/DeThi/TaoMaTran', mt(0, 2, 1)); assert s == 200 and 'Đã tạo đề nháp 3 câu' in text(b), text(b)[:300]
    de_id = int(sql(f"SELECT \"Id\" FROM \"DeThi\" WHERE \"Ten\"='Đề {ma}'"))
    assert sql(f'SELECT "DangMo"::text||"SoPhut" FROM "DeThi" WHERE "Id"={de_id}') == 'false30'
    assert sql(f'SELECT count(*) FROM "DeThiCau" WHERE "DeThiId"={de_id}') == '3' and sql(f'SELECT count(*) FROM "DeThiCau" WHERE "DeThiId"={de_id} AND "CauHoiId"={cau_id}') == '1'
    assert sql(f'SELECT count(DISTINCT "CauHoiId") FROM "DeThiCau" WHERE "DeThiId"={de_id}') == '3'
    assert sql(f'SELECT count(*) FROM "DeThiCau" d JOIN "CauHoi" c ON c."Id"=d."CauHoiId" WHERE d."DeThiId"={de_id} AND c."DoKho"=2') == '2'
    assert req(hv, f'/GiangVien/DeThi/MaTran?khoa={KHOA}')[0] != 200 or '/DangNhap' in req(hv, f'/GiangVien/DeThi/MaTran?khoa={KHOA}')[1] or 'KhongCoQuyen' in req(hv, f'/GiangVien/DeThi/MaTran?khoa={KHOA}')[1]
    print('PASS: ma trận chặn lấy quá số câu có / đề rỗng; đề tạo đúng 3 câu (2 trung bình + 1 khó), không trùng, ở dạng nháp; học viên không vào được')

    # 3. Chứng chỉ
    if sql(f"SELECT count(*) FROM \"GhiDanh\" g JOIN \"AspNetUsers\" u ON u.\"Id\"=g.\"NguoiDungId\" WHERE u.\"Email\"='{EM}' AND g.\"KhoaId\"='{KHOA}'") == '0':
        sql(f"INSERT INTO \"GhiDanh\"(\"NguoiDungId\",\"KhoaId\",\"HanDen\",\"NgayTao\") VALUES ('{uid(EM)}','{KHOA}',NULL,now())"); tao_gd = True
    ids = [int(x) for x in sql(f"SELECT b.\"Id\" FROM \"Bai\" b JOIN \"ChuDe\" c ON c.\"Id\"=b.\"ChuDeId\" WHERE c.\"KhoaId\"='{KHOA}' AND b.\"DaXuatBan\"").split()]
    for r in sql(f"SELECT \"BaiId\"||':'||\"TrangThai\" FROM \"TienDoBai\" WHERE \"NguoiDungId\"='{uid(EM)}' AND \"BaiId\" IN ({','.join(map(str, ids))})").split(): goc[int(r.split(':')[0])] = int(r.split(':')[1])
    def dat(bai, tt): sql(f"INSERT INTO \"TienDoBai\"(\"NguoiDungId\",\"BaiId\",\"TrangThai\",\"CapNhat\") VALUES ('{uid(EM)}',{bai},{tt},now()) ON CONFLICT (\"NguoiDungId\",\"BaiId\") DO UPDATE SET \"TrangThai\"={tt}")
    for i in ids: dat(i, 2)
    dat(ids[0], 1)                                               # còn một bài chưa học xong
    th = tk(hv, '/Lop')
    s, u, b = req(hv, '/ChungChi/Nhan', {'khoaId': KHOA, '__RequestVerificationToken': th}); assert 'Học hết các bài' in text(b) and sql(f'SELECT count(*) FROM "ChungChi" WHERE "NguoiDungId"=\'{uid(EM)}\'') == '0'
    print('PASS: chưa học hết bài thì không nhận được chứng chỉ')
    dat(ids[0], 2)
    s, u, b = req(hv, '/ChungChi/Nhan', {'khoaId': KHOA, '__RequestVerificationToken': th})
    m = re.search(r'/ChungChi/In/(\d+)', u); assert m, (s, u); cc = int(m[1]); t = text(b)
    ma_cc = sql(f'SELECT "MaXacThuc" FROM "ChungChi" WHERE "Id"={cc}'); ho_ten = sql(f"SELECT \"HoTen\" FROM \"AspNetUsers\" WHERE \"Email\"='{EM}'")
    assert ho_ten in t and ma_cc[:5] + '-' + ma_cc[5:] in t and 'Chứng nhận hoàn thành' in t and 'window.print' in t and f'/XacThuc/{ma_cc}' in t
    assert int(sql(f"SELECT count(*) FROM \"TinThongBao\" WHERE \"NguoiDungId\"='{uid(EM)}' AND \"Loai\"='ChungChi'")) == 1
    s, u, b = req(hv, '/ChungChi/Nhan', {'khoaId': KHOA, '__RequestVerificationToken': th}); assert f'/ChungChi/In/{cc}' in u
    assert sql(f"SELECT count(*) FROM \"ChungChi\" WHERE \"NguoiDungId\"='{uid(EM)}' AND \"KhoaId\"='{KHOA}'") == '1', 'nhận lần hai không được tạo trùng'
    print(f'PASS: học hết bài → nhận chứng chỉ (mã {ma_cc}); trang in có tên, khoá, mã, link xác thực; thông báo chúc mừng; nhận lại không tạo trùng')
    pub = text(req(client(), f'/XacThuc/{ma_cc.lower()}')[2]); assert 'Chứng chỉ hợp lệ' in pub and ho_ten in pub
    assert 'Không tìm thấy' in text(req(client(), '/XacThuc/AAAAAAAAAA')[2]) and 'Không tìm thấy' in text(req(client(), '/XacThuc/abc')[2])
    assert req(hv_khac, f'/ChungChi/In/{cc}')[0] == 404 and req(ad, f'/ChungChi/In/{cc}')[0] == 200 and '/DangNhap' in req(client(), f'/ChungChi/In/{cc}')[1]
    assert 'Đã nhận' in text(req(hv, '/ChungChi')[2])
    sql(f"UPDATE \"AspNetUsers\" SET \"HoTen\"='Tên Mới Thử' WHERE \"Email\"='{EM}'")
    try: assert ho_ten in text(req(hv, f'/ChungChi/In/{cc}')[2]), 'đổi tên không được làm đổi chứng chỉ đã cấp'
    finally: sql(f"UPDATE \"AspNetUsers\" SET \"HoTen\"='{ho_ten}' WHERE \"Email\"='{EM}'")
    print('PASS: xác thực công khai (không phân biệt hoa thường, mã sai báo không thấy); người khác không xem được bản in, admin xem được; đổi tên sau này không làm đổi chứng chỉ')
    print('\nTẤT CẢ ĐẠT')
finally:
    if de_id: sql(f'DELETE FROM "DeThiCau" WHERE "DeThiId"={de_id}'); sql(f'DELETE FROM "DeThi" WHERE "Id"={de_id}')
    if cau_id: sql(f'DELETE FROM "CauHoi" WHERE "Id"={cau_id}')
    sql(f"DELETE FROM \"ChungChi\" WHERE \"NguoiDungId\"='{uid(EM)}'"); sql(f"DELETE FROM \"TinThongBao\" WHERE \"NguoiDungId\"='{uid(EM)}' AND \"Loai\"='ChungChi'")
    for i, tt in goc.items(): sql(f"UPDATE \"TienDoBai\" SET \"TrangThai\"={tt} WHERE \"NguoiDungId\"='{uid(EM)}' AND \"BaiId\"={i}")
    if goc is not None:
        ids2 = [int(x) for x in sql(f"SELECT b.\"Id\" FROM \"Bai\" b JOIN \"ChuDe\" c ON c.\"Id\"=b.\"ChuDeId\" WHERE c.\"KhoaId\"='{KHOA}' AND b.\"DaXuatBan\"").split()]
        for i in ids2:
            if i not in goc: sql(f"DELETE FROM \"TienDoBai\" WHERE \"NguoiDungId\"='{uid(EM)}' AND \"BaiId\"={i}")
    if tao_gd: sql(f"DELETE FROM \"GhiDanh\" WHERE \"NguoiDungId\"='{uid(EM)}' AND \"KhoaId\"='{KHOA}'")
