# Kiểm tra Kho tài liệu: giảng viên tải tệp → học viên tìm/mở → trợ lý AI đọc → ẩn/xoá.
# Chạy trên web cục bộ + database Development; tự xoá đúng dữ liệu thử mang mã riêng.
# SUDIA_TEST_URL=http://localhost:5280 python3 KiemThu/kiem-tra-kho-tai-lieu.py
import html, http.cookiejar, io, json, os, re, ssl, subprocess, urllib.error, urllib.parse, urllib.request, uuid, zipfile
BASE = os.environ.get('SUDIA_TEST_URL', 'http://localhost:5280').rstrip('/')
PASSWORD = os.environ.get('SUDIA_TEST_PASSWORD', 'SuDia@2026')
DATABASE = os.environ.get('SUDIA_TEST_DATABASE', 'sudia_dev')
GOC = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
if urllib.parse.urlparse(BASE).hostname not in ('localhost', '127.0.0.1', '::1'):
    raise SystemExit('Kiểm tra này chỉ chạy trên web cục bộ.')
ma = 'khothu' + uuid.uuid4().hex[:8]          # từ lạ, chỉ có trong tài liệu thử
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

def docx(dong):
    b = io.BytesIO()
    with zipfile.ZipFile(b, 'w') as z:
        z.writestr('[Content_Types].xml', '<?xml version="1.0" encoding="UTF-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/></Types>')
        z.writestr('_rels/.rels', '<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/></Relationships>')
        p = ''.join(f'<w:p><w:r><w:t xml:space="preserve">{html.escape(d)}</w:t></w:r></w:p>' for d in dong)
        z.writestr('word/document.xml', f'<?xml version="1.0" encoding="UTF-8"?><w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>{p}</w:body></w:document>')
    return b.getvalue()
def pdf(chu):
    noi = f'BT /F1 14 Tf 72 720 Td ({chu}) Tj ET'.encode()
    obj = [b'<< /Type /Catalog /Pages 2 0 R >>', b'<< /Type /Pages /Kids [3 0 R] /Count 1 >>',
           b'<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>',
           b'<< /Length %d >>\nstream\n' % len(noi) + noi + b'\nendstream', b'<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>']
    out = bytearray(b'%PDF-1.4\n'); vt = []
    for i, o in enumerate(obj, 1): vt.append(len(out)); out += b'%d 0 obj\n' % i + o + b'\nendobj\n'
    x = len(out); out += b'xref\n0 %d\n0000000000 65535 f \n' % (len(obj) + 1) + b''.join(b'%010d 00000 n \n' % v for v in vt)
    out += b'trailer\n<< /Size %d /Root 1 0 R >>\nstartxref\n%d\n%%%%EOF\n' % (len(obj) + 1, x)
    return bytes(out)

gv = login('giangvien@sudia.vn'); hv = login('hocvien@sudia.vn')
khoa = sql('''SELECT k."Id" FROM "Khoa" k JOIN "AspNetUsers" t ON t."Id"=k."GiangVienId" JOIN "GhiDanh" g ON g."KhoaId"=k."Id"
    JOIN "AspNetUsers" s ON s."Id"=g."NguoiDungId" WHERE t."Email"='giangvien@sudia.vn' AND s."Email"='hocvien@sudia.vn'
    AND (g."HanDen" IS NULL OR g."HanDen" > now()) LIMIT 1''')
bai = int(sql(f'''SELECT b."Id" FROM "Bai" b JOIN "ChuDe" c ON c."Id"=b."ChuDeId" WHERE c."KhoaId"='{khoa}' ORDER BY b."Id" LIMIT 1'''))
tep_docx = docx(['Đề cương ôn tập ' + ma, f'Hiệp định {ma} được ký năm 1954 tại Giơ-ne-vơ, chấm dứt chiến tranh ở Đông Dương.'])
tep_pdf = pdf(f'Bang so lieu {ma}pdf dan so nam 2021')
tep_txt = f'Ghi chú {ma}txt: Tây Nguyên trồng nhiều cà phê.'.encode()
try:
    _, _, p = req(gv, f'/GiangVien/TaiLieu?khoa={khoa}')
    s, u, p = req(gv, '/GiangVien/TaiLieu/Tai', {'khoaId': khoa, 'baiId': bai, '__RequestVerificationToken': token(p)},
                  files=[('tep', (f'{ma}.docx', tep_docx)), ('tep', (f'{ma}.pdf', tep_pdf)), ('tep', (f'{ma}.txt', tep_txt)),
                         ('tep', (f'{ma}-gia.pdf', b'khong phai pdf'))])
    assert s == 200 and 'Đã thêm 3 tài liệu' in text(p) and 'không đúng định dạng' in text(p), text(p)[:400]
    rows = sql(f'''SELECT "Id","LoaiTep","TrangThai","DuongDan","BaiId" FROM "TaiLieuKhoa" WHERE "TenTep" LIKE '{ma}%' ORDER BY "LoaiTep"''').splitlines()
    assert len(rows) == 3 and all(r.split('|')[2] == 'SanSang' and r.split('|')[4] == str(bai) for r in rows), rows
    NOI_LUU = sql(f'SELECT DISTINCT "NoiLuu" FROM "TaiLieuKhoa" WHERE "Id" IN ({",".join(r.split("|")[0] for r in rows)})')
    if NOI_LUU == 'CucBo':
        for r in rows: assert os.path.isfile(os.path.join(GOC, 'App_Data/kho-tai-lieu', r.split('|')[3])), r
    ids = {r.split('|')[1]: int(r.split('|')[0]) for r in rows}
    print(f'PASS: giảng viên tải 3 tệp (docx/pdf/txt) lên kho ({NOI_LUU}), đọc được chữ, tệp giả PDF bị từ chối')

    _, _, p = req(gv, f'/GiangVien/TaiLieu?khoa={khoa}')
    s, _, p = req(gv, '/GiangVien/TaiLieu/Tai', {'khoaId': khoa, '__RequestVerificationToken': token(p)}, files=[('tep', ('ban-sao.docx', tep_docx))])
    assert 'đã có sẵn' in text(p) and sql(f'''SELECT count(*) FROM "TaiLieuKhoa" WHERE "MaBam"=(SELECT "MaBam" FROM "TaiLieuKhoa" WHERE "Id"={ids['docx']})''') == '1'
    print('PASS: tải lại cùng tệp không tạo bản trùng')

    for duoi, tu in (('docx', ma), ('pdf', ma + 'pdf'), ('txt', ma + 'txt')):
        assert doi(lambda: f'/Home/TaiLieu/Mo/{ids[duoi]}' in text(req(hv, '/Home/TaiLieu?q=' + urllib.parse.quote(tu))[2])), duoi
    s, u, p = req(hv, f'/Home/TaiLieu/Mo/{ids["docx"]}'); assert s == 200 and p == tep_docx, (s, u)
    s, _, p = req(hv, '/Home/TaiLieu?q=' + urllib.parse.quote('hiep dinh ' + ma)); assert f'/Home/TaiLieu/Mo/{ids["docx"]}' in text(p)
    print('PASS: học viên tìm thấy cả 3 tệp (kể cả gõ không dấu) và tải đúng tệp gốc')

    s, u, _ = req(client(), f'/Home/TaiLieu/Mo/{ids["docx"]}'); assert '/DangNhap' in u
    la = sql(f'''SELECT u."Email" FROM "AspNetUsers" u JOIN "AspNetUserRoles" ur ON ur."UserId"=u."Id" JOIN "AspNetRoles" r ON r."Id"=ur."RoleId"
        WHERE r."Name"='HocVien' AND NOT EXISTS (SELECT 1 FROM "GhiDanh" g WHERE g."NguoiDungId"=u."Id" AND g."KhoaId"='{khoa}') LIMIT 1''')
    if la:
        try: hv_khac = login(la)
        except AssertionError: hv_khac = None
        if hv_khac: assert req(hv_khac, f'/Home/TaiLieu/Mo/{ids["docx"]}')[0] == 404
    print('PASS: chưa đăng nhập / chưa ghi danh khoá thì không mở được tài liệu')

    s, _, p = req(hv, '/api/tro-ly/chi-muc?q=' + urllib.parse.quote(ma))
    if s == 200:
        assert doi(lambda: any(d['nguon'].find('Tài liệu: ') >= 0 and ma in d['noiDung']
                               for d in json.loads(req(hv, '/api/tro-ly/chi-muc?q=' + urllib.parse.quote(ma))[2])['doan']))
        print('PASS: chỉ mục trợ lý AI đọc được tài liệu vừa tải')

    _, _, p = req(gv, f'/GiangVien/TaiLieu?khoa={khoa}')
    req(gv, '/GiangVien/TaiLieu/Sua', {'id': ids['docx'], 'ten': 'Đề cương ' + ma, 'baiId': bai, '__RequestVerificationToken': token(p)})
    assert doi(lambda: f'/Home/TaiLieu/Mo/{ids["docx"]}' not in text(req(hv, '/Home/TaiLieu?q=' + urllib.parse.quote(ma))[2]))
    assert req(hv, f'/Home/TaiLieu/Mo/{ids["docx"]}')[0] == 404
    s, _, p = req(hv, '/api/tro-ly/chi-muc?q=' + urllib.parse.quote(ma))
    if s == 200: assert doi(lambda: not any(f'Đề cương {ma}' in d['nguon'] for d in json.loads(req(hv, '/api/tro-ly/chi-muc?q=' + urllib.parse.quote(ma))[2])['doan']))
    print('PASS: tắt "Học viên thấy" → học viên không tìm/mở được, AI không đọc nữa')

    duong = os.path.join(GOC, 'App_Data/kho-tai-lieu', sql(f'SELECT "DuongDan" FROM "TaiLieuKhoa" WHERE "Id"={ids["txt"]}'))
    _, _, p = req(gv, f'/GiangVien/TaiLieu?khoa={khoa}')
    req(gv, '/GiangVien/TaiLieu/Xoa', {'id': ids['txt'], '__RequestVerificationToken': token(p)})
    assert sql(f'SELECT count(*) FROM "TaiLieuKhoa" WHERE "Id"={ids["txt"]}') == '0' and (NOI_LUU != 'CucBo' or not os.path.exists(duong))
    print('PASS: xoá tài liệu xoá cả dòng CSDL lẫn tệp trên đĩa')

    _, _, p = req(gv, f'/GiangVien/NoiDung/SoanBai/{bai}')
    tep_nhap = docx([f'Bài nhập {ma}nhap', 'Nội dung nhập từ Word lúc soạn bài.'])
    s, _, p = req(gv, '/GiangVien/NoiDung/NhapTaiLieu', {'baiId': bai}, files=[('taiLieu', (f'{ma}-nhap.docx', tep_nhap))],
                  headers={'RequestVerificationToken': token(p)})
    kq = json.loads(p); assert s == 200 and kq['daLuuKho'] and ma + 'nhap' in kq['noiDung'], kq
    assert sql(f'''SELECT count(*) FROM "TaiLieuKhoa" WHERE "TenTep"='{ma}-nhap.docx' AND "BaiId"={bai}''') == '1'
    print('PASS: nhập Word lúc soạn bài tự lưu tệp gốc vào kho, gắn đúng bài')
finally:
    # Dọn qua web trước (xoá đúng tệp trên đĩa hoặc S3 và đoạn chỉ mục), phần còn sót dọn bằng SQL
    for i in sql(f'''SELECT "Id" FROM "TaiLieuKhoa" WHERE "TenTep" LIKE '{ma}%' ''').splitlines():
        try:
            _, _, p = req(gv, f'/GiangVien/TaiLieu?khoa={khoa}')
            req(gv, '/GiangVien/TaiLieu/Xoa', {'id': i, '__RequestVerificationToken': token(p)})
        except Exception: pass
    for d in sql(f'''SELECT "DuongDan" FROM "TaiLieuKhoa" WHERE "TenTep" LIKE '{ma}%' AND "NoiLuu"='CucBo' ''').splitlines():
        f = os.path.join(GOC, 'App_Data/kho-tai-lieu', d)
        if os.path.exists(f): os.remove(f)
    # Xoá thẳng bằng SQL không qua web nên phải tự dọn luôn các đoạn chỉ mục của tài liệu thử
    sql(f'''DELETE FROM "DoanChiMuc" WHERE "NguonKhoa" IN (SELECT 'TaiLieu:' || "Id" FROM "TaiLieuKhoa" WHERE "TenTep" LIKE '{ma}%') ''')
    sql(f'''DELETE FROM "TaiLieuKhoa" WHERE "TenTep" LIKE '{ma}%' ''')
    print('CLEANUP: đã xoá tài liệu thử (CSDL + tệp)')
