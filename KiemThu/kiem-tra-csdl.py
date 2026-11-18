# Kiểm tra thiết kế CSDL (migration ThietKeLaiCsdl) trên web cục bộ + database Development.
# Tự xoá đúng dữ liệu thử mang mã riêng. Chạy: SUDIA_TEST_URL=http://localhost:5280 python3 KiemThu/kiem-tra-csdl.py
import html, http.cookiejar, os, re, ssl, subprocess, urllib.error, urllib.parse, urllib.request, uuid
BASE = os.environ.get('SUDIA_TEST_URL', 'http://localhost:5280').rstrip('/')
PASSWORD = os.environ.get('SUDIA_TEST_PASSWORD', 'SuDia@2026')
DATABASE = os.environ.get('SUDIA_TEST_DATABASE', 'sudia_dev')
if urllib.parse.urlparse(BASE).hostname not in ('localhost', '127.0.0.1', '::1'):
    raise SystemExit('Kiểm tra này chỉ chạy trên web cục bộ.')
marker = 'sudia-csdl-' + uuid.uuid4().hex[:10]
def sql(q): return subprocess.check_output(['psql', '-d', DATABASE, '-At', '-v', 'ON_ERROR_STOP=1', '-c', q], text=True).strip()
def client(): return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()), urllib.request.HTTPSHandler(context=ssl._create_unverified_context()))
def req(c, path, data=None, referer=None):
    body = urllib.parse.urlencode(data, doseq=True).encode() if data is not None else None
    headers = {'Referer': BASE + referer} if referer else {}
    try:
        with c.open(urllib.request.Request(BASE + path, body, headers), timeout=30) as r: return r.status, r.url, html.unescape(r.read().decode())
    except urllib.error.HTTPError as e: return e.code, e.url, html.unescape(e.read().decode())
def token(p): return re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', p)[1]
def login(email):
    c = client(); _, _, p = req(c, '/TaiKhoan/DangNhap')
    s, u, _ = req(c, '/TaiKhoan/DangNhap', {'Email': email, 'MatKhau': PASSWORD, '__RequestVerificationToken': token(p)})
    assert s == 200 and '/DangNhap' not in u, (email, s, u); return c

gv = login('giangvien@sudia.vn'); hv = login('hocvien@sudia.vn'); ad = login('admin@sudia.vn')
chu_de = int(sql('''SELECT c."Id" FROM "ChuDe" c JOIN "Khoa" k ON k."Id"=c."KhoaId" JOIN "AspNetUsers" u ON u."Id"=k."GiangVienId"
    WHERE u."Email"='giangvien@sudia.vn' ORDER BY c."Id" LIMIT 1'''))
try:
    # 1. Mã bài do CSDL cấp từ dãy chung MaBaiDe, không trùng mã đề
    _, _, p = req(gv, '/GiangVien/NoiDung')
    s, u, p = req(gv, '/GiangVien/NoiDung/ThemBai', {'chuDeId': chu_de, 'ten': marker, 'soBai': 901, '__RequestVerificationToken': token(p)})
    bai = int(sql(f'''SELECT "Id" FROM "Bai" WHERE "Ten"='{marker}' '''))
    assert sql(f'SELECT count(*) FROM "DeThi" WHERE "Id"={bai}') == '0'
    assert bai > int(sql('SELECT max("Id") FROM "DeThi"')) or bai > 2002, bai
    assert f'/GiangVien/NoiDung/SoanBai/{bai}' in u, u
    print(f'PASS: tạo bài mới, mã {bai} lấy từ dãy chung, không trùng mã đề')

    # 2. Trùng số bài trong cùng chủ đề: chặn, báo lỗi dễ hiểu (không trang 500)
    s, u, p = req(gv, '/GiangVien/NoiDung/ThemBai', {'chuDeId': chu_de, 'ten': marker + '-trung', 'soBai': 901, '__RequestVerificationToken': token(p)},
                  referer='/GiangVien/NoiDung')
    assert s == 200 and 'trùng' in p, (s, u, p[:300])
    assert sql(f'''SELECT count(*) FROM "Bai" WHERE "Ten" LIKE '{marker}%' ''') == '1'
    print('PASS: trùng số bài bị chặn, có thông báo, không lỗi 500')

    # 3. Đường dẫn cũ: mã đề mở đề, mã bài mở bài
    de = int(sql('SELECT min("Id") FROM "DeThi" WHERE "BaiTapGiaoId" IS NULL'))
    _, _, p = req(hv, f'/Home/LamCauHoi/{de}'); assert 'Đề thi thử' in p
    _, _, p = req(hv, '/Home/LamCauHoi/7'); assert 'Luyện tập theo bài' in p
    print('PASS: /Home/LamCauHoi/{mã} nhận đúng đề và bài')

    # 4. Câu hỏi đang nằm trong đề: chặn xoá, câu vẫn còn
    cau = int(sql('''SELECT d."CauHoiId" FROM "DeThiCau" d JOIN "CauHoi" q ON q."Id"=d."CauHoiId" JOIN "Bai" b ON b."Id"=q."BaiId"
        JOIN "ChuDe" c ON c."Id"=b."ChuDeId" JOIN "Khoa" k ON k."Id"=c."KhoaId" JOIN "AspNetUsers" u ON u."Id"=k."GiangVienId"
        WHERE u."Email"='giangvien@sudia.vn' LIMIT 1'''))
    bai_cau = sql(f'SELECT "BaiId" FROM "CauHoi" WHERE "Id"={cau}')
    _, _, p = req(gv, f'/GiangVien/CauHoi?bai={bai_cau}')
    s, u, p = req(gv, '/GiangVien/CauHoi/Xoa', {'id': cau, '__RequestVerificationToken': token(p)})
    assert 'không xoá được' in p and sql(f'SELECT count(*) FROM "CauHoi" WHERE "Id"={cau}') == '1', p[:300]
    print('PASS: câu hỏi trong đề không xoá được, có lý do')

    # 5. Gói học dùng bảng nối GoiBanKhoa: trang sửa gói tick đúng khoá, trang mua gói hiện đủ khoá
    goi = sql('SELECT "GoiBanId" FROM "GoiBanKhoa" GROUP BY 1 HAVING count(*)=2 LIMIT 1')
    _, _, p = req(ad, f'/Admin/GoiHoc/Sua/{goi}')
    assert len(re.findall(r'name="KhoaIds" value="[^"]+" checked', p)) == 2, 'gói trọn bộ phải tick 2 khoá'
    s, _, p = req(hv, '/ThanhToan/Goi'); assert s == 200 and 'Lịch sử 12' in p and 'Địa lí 12' in p
    print('PASS: gói học đọc khoá từ bảng nối GoiBanKhoa')
finally:
    sql(f'''DELETE FROM "Bai" WHERE "Ten" LIKE '{marker}%' ''')
    print('CLEANUP: đã xoá bài thử')
