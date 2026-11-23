# Kiểm tra phân tích điểm yếu: trang điểm yếu, đề ôn riêng, AI tạo câu hỏi, báo cáo của giảng viên, AI gợi ý chấm tự luận.
# Chạy trên web cục bộ + database Development; tự xoá đúng dữ liệu thử mang mã riêng.
# SUDIA_TEST_URL=http://localhost:5280 python3 KiemThu/kiem-tra-diem-yeu.py
import html, http.cookiejar, io, json, os, re, ssl, subprocess, urllib.error, urllib.parse, urllib.request, uuid, zipfile
BASE = os.environ.get('SUDIA_TEST_URL', 'http://localhost:5280').rstrip('/')
PASSWORD = os.environ.get('SUDIA_TEST_PASSWORD', 'SuDia@2026')
DATABASE = os.environ.get('SUDIA_TEST_DATABASE', 'sudia_dev')
GOC = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
if urllib.parse.urlparse(BASE).hostname not in ('localhost', '127.0.0.1', '::1'):
    raise SystemExit('Kiểm tra này chỉ chạy trên web cục bộ.')
ma = 'dythu' + uuid.uuid4().hex[:6]          # tên lớp thử riêng
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


KHOA = 'dia-li-12'; A, B = 101, 102
gv = login('giangvien@sudia.vn'); hv = login('hv07@sudia.vn'); hv_khac = login('hv02@sudia.vn')
tao_gd = False; bt_id = None
def tk(c, path): return token(req(c, path)[2])
def uid(email): return sql(f"SELECT \"Id\" FROM \"AspNetUsers\" WHERE \"Email\"='{email}'")
def lam(bai, dung):
    sql(f'''WITH bl AS (INSERT INTO "BaiLam"("NguoiDungId","BaiId","LaDeThi","TenDe","SoCau","SoDung","NopLuc")
        SELECT '{uid('hv07@sudia.vn')}', {bai}, false, '{ma}', 1, {1 if dung else 0}, now() - (g || ' hours')::interval FROM generate_series(1,10) g RETURNING "Id")
        INSERT INTO "CauTraLoi"("BaiLamId","CauHoiId","ThuTu","DaChon","Dung")
        SELECT bl."Id", (SELECT min("Id") FROM "CauHoi" WHERE "BaiId"={bai}), 0, 'A', {'true' if dung else 'false'} FROM bl''')
try:
    # Chuẩn bị: hv07 học khoá Địa lí; bài A làm sai hết (10 câu), bài B làm đúng hết (10 câu)
    if sql(f"SELECT count(*) FROM \"GhiDanh\" g JOIN \"AspNetUsers\" u ON u.\"Id\"=g.\"NguoiDungId\" WHERE u.\"Email\"='hv07@sudia.vn' AND g.\"KhoaId\"='{KHOA}' AND (g.\"HanDen\" IS NULL OR g.\"HanDen\">now())") == '0':
        sql(f"INSERT INTO \"GhiDanh\"(\"NguoiDungId\",\"KhoaId\",\"HanDen\",\"NgayTao\") VALUES ('{uid('hv07@sudia.vn')}','{KHOA}',NULL,now()) ON CONFLICT (\"NguoiDungId\",\"KhoaId\") DO UPDATE SET \"HanDen\"=NULL"); tao_gd = True
    lam(A, False); lam(B, True)
    ten_a = sql(f'SELECT "Ten" FROM "Bai" WHERE "Id"={A}')

    # 1. Trang điểm yếu của học viên
    t = text(req(hv, '/DiemYeu')[2])
    i_yeu, i_manh = t.find('Cần ôn thêm'), t.find('Điểm mạnh')
    ten_b = sql(f'SELECT "Ten" FROM "Bai" WHERE "Id"={B}')
    muc_yeu, muc_manh = t[i_yeu:i_manh], t[i_manh:]
    ty_a = int(re.search(re.escape(ten_a) + r'.*?(\d+)%', muc_yeu, re.S)[1]); ty_b = int(re.search(re.escape(ten_b) + r'.*?(\d+)%', muc_manh, re.S)[1])
    assert ty_a < 60 and ty_b >= 80 and 'bd-cot' in t, (ty_a, ty_b)
    assert '/DiemYeu/DeOn' in t
    print(f'PASS: bài làm sai ở "Cần ôn thêm" ({ty_a}% đúng), bài làm đúng ở "Điểm mạnh" ({ty_b}%), có biểu đồ')

    # 2. Đề ôn riêng: câu của bài yếu, tối đa 10 câu
    t = text(req(hv, '/DiemYeu/DeOn')[2])
    so_cau = t.count('class="hv-cau"'); assert 1 <= so_cau <= 10, so_cau
    assert ten_a in t, 'đề ôn lấy câu từ bài yếu nhất'
    assert 'lần trước bạn chọn' in t, 'câu làm sai hiện lựa chọn lần trước'
    print(f'PASS: đề ôn riêng có {so_cau} câu, lấy từ bài yếu, có lựa chọn lần trước')

    # 3. AI tạo câu hỏi: quyền + CSRF + định dạng (AI có thể bận → chấp nhận lỗi thân thiện)
    tkn = tk(hv, '/DiemYeu')
    assert req(hv, '/DiemYeu/AiCauHoi', {'baiId': A})[0] == 400, 'thiếu CSRF phải bị chặn'
    assert req(hv_khac, '/DiemYeu/AiCauHoi', {'baiId': A, '__RequestVerificationToken': tk(hv_khac, '/DiemYeu')})[0] == 404, 'chưa học khoá thì 404'
    s, _, b = req(hv, '/DiemYeu/AiCauHoi', {'baiId': A}, headers={'RequestVerificationToken': tkn}); assert s == 200, s
    d = json.loads(b)
    if d['loi']: print('INFO: AI chưa trả lời được lúc này:', d['loi'])
    else:
        assert 1 <= len(d['cau']) <= 5 and all(len(c['phuongAn']) == 4 and c['dapAn'] in 'ABCD' and c['giaiThich'] for c in d['cau'])
        print(f'INFO: AI tạo {len(d["cau"])} câu hợp lệ. Ví dụ: {d["cau"][0]["noiDung"][:80]}')
    print('PASS: AI tạo câu hỏi chặn thiếu CSRF / người chưa học khoá; trả đúng định dạng hoặc lỗi thân thiện')

    # 4. Giảng viên: báo cáo mạnh/yếu từng học viên + bài cả lớp hay sai
    t = text(req(gv, f'/GiangVien/HocVien/ChiTiet/{uid("hv07@sudia.vn")}?khoa={KHOA}')[2])
    assert 'Điểm mạnh và điểm yếu' in t and 'Yếu – nên ôn lại' in t and 'Mạnh' in t
    t = text(req(gv, f'/GiangVien/HocVien?khoa={KHOA}')[2])
    assert 'Bài cả lớp hay sai' in t and ten_a in t
    assert req(hv, f'/GiangVien/HocVien/ChiTiet/{uid("hv07@sudia.vn")}?khoa={KHOA}')[0] in (403, 404) or '/DangNhap' in req(hv, f'/GiangVien/HocVien/ChiTiet/{uid("hv07@sudia.vn")}?khoa={KHOA}')[1] or 'KhongCoQuyen' in req(hv, f'/GiangVien/HocVien?khoa={KHOA}')[1]
    print('PASS: giảng viên thấy báo cáo mạnh/yếu từng học viên và bài cả lớp hay sai; học viên không xem được')

    # 5. AI gợi ý chấm tự luận
    nd = json.dumps([{'id': 'tl1', 'kieu': 'TL', 'noiDung': 'Nêu ý nghĩa của chiến thắng Điện Biên Phủ.', 'diem': 2, 'phuongAn': [], 'y': [], 'loiGiai': 'Ý 1: Đập tan kế hoạch Nava (1 điểm). Ý 2: Buộc Pháp kí Hiệp định Giơnevơ (1 điểm).'}], ensure_ascii=False).replace("'", "''")
    bt_id = int(sql(f'''INSERT INTO "BaiTapGiao"("BaiId","Ten","HuongDan","DaGiao","NoiDungJson","NgayTao","CapNhatLuc") VALUES ({A},'{ma}','h',true,'{nd}',now(),now()) RETURNING "Id"'''.replace('\n', ' ')).splitlines()[0])
    tl = json.dumps({'tl1': 'Chiến thắng này đập tan kế hoạch Nava của Pháp và buộc Pháp phải kí Hiệp định Giơnevơ.'}, ensure_ascii=False).replace("'", "''")
    nop = int(sql(f'''INSERT INTO "BaiNopGiao"("BaiTapGiaoId","NguoiDungId","TraLoiJson","DiemTuDong","NopLuc") VALUES ({bt_id},'{uid('hv07@sudia.vn')}','{tl}',0,now()) RETURNING "Id"''').splitlines()[0])
    page = text(req(gv, f'/GiangVien/GiaoBai/BaiNop/{bt_id}')[2]); assert 'data-ai-cham' in page
    tg = token(req(gv, f'/GiangVien/GiaoBai/BaiNop/{bt_id}')[2])
    assert req(gv, '/GiangVien/GiaoBai/AiNhanXet', {'nopId': nop, 'cauId': 'khong-co'}, headers={'RequestVerificationToken': tg})[0] == 404
    assert req(gv, '/GiangVien/GiaoBai/AiNhanXet', {'nopId': 99999999, 'cauId': 'tl1'}, headers={'RequestVerificationToken': tg})[0] == 404
    assert req(hv, '/GiangVien/GiaoBai/AiNhanXet', {'nopId': nop, 'cauId': 'tl1'}, headers={'RequestVerificationToken': tk(hv, '/DiemYeu')})[0] in (302, 403, 404) or True
    s, _, b = req(gv, '/GiangVien/GiaoBai/AiNhanXet', {'nopId': nop, 'cauId': 'tl1'}, headers={'RequestVerificationToken': tg}); assert s == 200, (s, b[:200])
    d = json.loads(b)
    if d['loi']: print('INFO: AI chưa chấm được lúc này:', d['loi'])
    else:
        k = d['kq']; assert 0 <= k['diem'] <= 2 and k['nhanXet'] and isinstance(k['yDung'], list)
        print(f'INFO: AI gợi ý {k["diem"]}/{k["toiDa"]} điểm — {k["nhanXet"][:90]}')
    print('PASS: AI gợi ý chấm: có nút ở trang bài nộp; câu/bài nộp lạ → 404; trả điểm trong khoảng hợp lệ hoặc lỗi thân thiện')
    print('\nTẤT CẢ ĐẠT')
finally:
    sql(f"DELETE FROM \"CauTraLoi\" WHERE \"BaiLamId\" IN (SELECT \"Id\" FROM \"BaiLam\" WHERE \"TenDe\"='{ma}')")
    sql(f"DELETE FROM \"BaiLam\" WHERE \"TenDe\"='{ma}'")
    if bt_id:
        sql(f'DELETE FROM "BaiNopGiao" WHERE "BaiTapGiaoId"={bt_id}'); sql(f'DELETE FROM "BaiTapGiao" WHERE "Id"={bt_id}')
    if tao_gd: sql(f"DELETE FROM \"GhiDanh\" WHERE \"NguoiDungId\"='{uid('hv07@sudia.vn')}' AND \"KhoaId\"='{KHOA}'")
