"""Kiểm tra form chung và chấm TN/ĐS/TL trên web Development cục bộ; chỉ xoá dữ liệu thử có mã riêng."""
import html, http.cookiejar, json, os, re, ssl, subprocess, urllib.error, urllib.parse, urllib.request, uuid

BASE = os.environ.get('SUDIA_TEST_URL', 'https://localhost:7180').rstrip('/')
DATABASE = os.environ.get('SUDIA_TEST_DATABASE', 'sudia_dev')
PASSWORD = os.environ.get('SUDIA_TEST_PASSWORD', 'SuDia@2026')
if urllib.parse.urlparse(BASE).hostname not in ('localhost', '127.0.0.1', '::1'):
    raise SystemExit('Chỉ chạy trên web/database Development cục bộ.')
marker = 'sudia-exam-' + uuid.uuid4().hex
names = [marker, marker+'-expiry', marker+'-legacy', marker+'-locked']
email = marker+'@example.test'

def sql(query):
    return subprocess.check_output(['psql', '-d', DATABASE, '-At', '-v', 'ON_ERROR_STOP=1', '-c', query], text=True).strip()

def client():
    return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()),
                                      urllib.request.HTTPSHandler(context=ssl._create_unverified_context()))

def req(c, path, data=None, as_json=False, tok=None):
    body = None; headers = {}
    if data is not None:
        body = json.dumps(data).encode() if as_json else urllib.parse.urlencode(data, doseq=True).encode()
        headers['Content-Type'] = 'application/json' if as_json else 'application/x-www-form-urlencoded'
    if tok: headers['RequestVerificationToken'] = tok
    try:
        with c.open(urllib.request.Request(BASE+path, body, headers), timeout=30) as r:
            return r.status, r.url, html.unescape(r.read().decode('utf-8-sig'))
    except urllib.error.HTTPError as e:
        return e.code, e.url, html.unescape(e.read().decode())

def get(c, path):
    s, u, p = req(c, path); assert s == 200, (path, s, p[:700]); return u, p

def token(page): return re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page)[1]

def login(address):
    c = client(); _, p = get(c, '/TaiKhoan/DangNhap')
    s, u, _ = req(c, '/TaiKhoan/DangNhap', {'Email':address, 'MatKhau':PASSWORD, '__RequestVerificationToken':token(p)})
    assert s == 200 and '/DangNhap' not in u, (s, u)
    return c

teacher = login('giangvien@sudia.vn'); student = login('hocvien@sudia.vn')
bai = int(sql('''SELECT b."Id" FROM "Bai" b JOIN "ChuDe" c ON c."Id"=b."ChuDeId" JOIN "Khoa" k ON k."Id"=c."KhoaId" JOIN "AspNetUsers" t ON t."Id"=k."GiangVienId" JOIN "GhiDanh" g ON g."KhoaId"=k."Id" JOIN "AspNetUsers" s ON s."Id"=g."NguoiDungId" WHERE t."Email"='giangvien@sudia.vn' AND s."Email"='hocvien@sudia.vn' AND k."Id"='lich-su-12' ORDER BY b."Id" LIMIT 1;'''))
_, page = get(teacher, f'/GiangVien/GiaoBai/Tao?kiemTra=true&khoa=lich-su-12&bai={bai}')
tok = token(page)

def save(data, expected=200):
    s, _, p = req(teacher, '/GiangVien/GiaoBai/Luu', data, True, tok)
    assert s == expected, (s, p[:1200])
    return json.loads(p)

def publish(name, questions):
    data = {'id':0, 'baiId':bai, 'ten':name, 'huongDan':'Kiểm thử form chung', 'laKiemTra':True,
            'soPhut':5, 'daGiao':True, 'cau':questions}
    data['id'] = save(data)['id']
    de = int(sql(f'SELECT "Id" FROM "DeThi" WHERE "BaiTapGiaoId"={data["id"]};'))
    return data, de

questions = [
    {'id':'tn-test', 'kieu':'TN', 'noiDung':'CAU-TN-'+marker, 'diem':1, 'phuongAn':['Một','Hai','Ba','Bốn'], 'dapAn':1, 'y':[], 'loiGiai':'LOI-GIAI-'+marker},
    {'id':'ds-test', 'kieu':'DS', 'noiDung':'CAU-DS-'+marker, 'diem':1, 'phuongAn':[],
     'y':[{'noiDung':'Ý '+str(i), 'dung':i%2==0, 'diem':.25} for i in range(4)], 'loiGiai':'LOI-GIAI-'+marker},
    {'id':'tl-test', 'kieu':'TL', 'noiDung':'CAU-TL-'+marker, 'diem':1, 'phuongAn':[], 'y':[], 'loiGiai':'LOI-GIAI-'+marker}
]
try:
    assert 'href="/GiangVien/CauHoi/Tao' not in page
    assert all(f'data-type="{kind}"' in page for kind in ['TN','DS','TL']) and 'id="gbPhut"' in page
    assert '/GiangVien/GiaoBai/Tao' in get(teacher, f'/GiangVien/CauHoi/Tao?bai={bai}')[0]
    assert 'kiemTra=True' in get(teacher, '/GiangVien/DeThi/Tao?khoa=lich-su-12')[0] or 'gbPhut' in get(teacher, '/GiangVien/DeThi/Tao?khoa=lich-su-12')[1]
    print('PASS: bỏ menu riêng; bài tập và kiểm tra dùng cùng form 3 bước đủ TN/ĐS/TL')

    data = {'id':0, 'baiId':bai, 'ten':marker, 'huongDan':'Hướng dẫn thử', 'laKiemTra':True, 'soPhut':5, 'daGiao':False, 'cau':questions}
    data['id'] = save(data)['id']
    de = int(sql(f'SELECT "Id" FROM "DeThi" WHERE "BaiTapGiaoId"={data["id"]};'))
    assert req(student, f'/BaiTap/Lam/{data["id"]}')[0] == 404
    bad={**data,'daGiao':True,'soPhut':4}; save(bad,400)
    bad={**data,'daGiao':True,'cau':[{**questions[0],'dapAn':None}]}; save(bad,400)
    assert req(teacher,'/GiangVien/GiaoBai/Luu',data,True)[0] == 400
    data['daGiao']=True; save(data)
    _, p = get(teacher, '/GiangVien/DeThi'); assert marker in p and f'/GiangVien/GiaoBai/BaiNop/{data["id"]}' in p
    _, p = get(student, '/Home/LuyenDe/lich-su-12'); assert marker in p
    _, p = get(teacher, f'/BaiTap?bai={bai}'); assert marker not in p
    print('PASS: lưu nháp/xuất bản kiểm tra, kiểm tra dữ liệu và CSRF, không lẫn vào danh sách bài tập')

    u, p = get(student, f'/Home/LamCauHoi/{de}')
    assert f'/BaiTap/Lam/{data["id"]}' in u and 'Bắt đầu làm bài' in p and 'CAU-TN-'+marker not in p
    req(student, f'/BaiTap/Lam/{data["id"]}', {'__RequestVerificationToken':token(p), 'tn-test':'1'})
    assert sql(f'SELECT count(*) FROM "BaiNopGiao" WHERE "BaiTapGiaoId"={data["id"]};') == '0'
    s,_,p=req(student,f'/BaiTap/BatDau/{data["id"]}',{'__RequestVerificationToken':token(p)})
    assert s==200 and all('CAU-'+kind+'-'+marker in p for kind in ['TN','DS','TL']) and 'LOI-GIAI-'+marker not in p
    deadline=re.search(r'data-het-gio="([^"]+)"',p)[1]
    assert re.search(r'data-het-gio="([^"]+)"', get(student,f'/BaiTap/Lam/{data["id"]}')[1])[1] == deadline
    save(data,400)
    _, locked = get(teacher,f'/GiangVien/GiaoBai/Sua/{data["id"]}'); assert 'data-locked="true"' in locked
    print('PASS: bắt đầu trước khi làm, không lộ đáp án, tải lại không reset giờ, khoá nội dung sau khi bắt đầu')

    answers={'__RequestVerificationToken':token(p),'tn-test':'1','tl-test':'BÀI-TỰ-LUẬN-'+marker}
    answers.update({f'ds-test-{i}':'true' if i%2==0 else 'false' for i in range(4)})
    s,_,p=req(student,f'/BaiTap/Lam/{data["id"]}',answers)
    assert s==200 and 'LOI-GIAI-'+marker in p and 'chờ giảng viên chấm' in p
    assert sql(f'SELECT "DiemTuDong"::text FROM "BaiNopGiao" WHERE "BaiTapGiaoId"={data["id"]};').startswith('2')
    req(student,f'/BaiTap/Lam/{data["id"]}',{**answers,'tn-test':'0'})
    assert sql(f'SELECT count(*) FROM "BaiNopGiao" WHERE "BaiTapGiaoId"={data["id"]};') == '1'
    _, p=get(teacher,f'/GiangVien/GiaoBai/BaiNop/{data["id"]}'); assert 'BÀI-TỰ-LUẬN-'+marker in p
    nop=int(sql(f'SELECT "Id" FROM "BaiNopGiao" WHERE "BaiTapGiaoId"={data["id"]};'))
    s,_,p=req(teacher,'/GiangVien/GiaoBai/ChamTuLuan',{'id':nop,'diem':'1','__RequestVerificationToken':token(p)})
    assert s==200 and 'Đã lưu điểm tự luận' in p
    _,p=get(student,f'/BaiTap/Lam/{data["id"]}'); assert re.search(r'Tổng điểm: <b>3(?:[.,]0+)? / 3',p)
    assert 'điểm' in get(student,'/Home/LuyenDe/lich-su-12')[1]
    print('PASS: tự chấm TN/ĐS, tự luận chấm thủ công, nộp lại không ghi đè, học viên thấy kết quả')

    expired,_=publish(marker+'-expiry',[questions[0]])
    _,p=get(student,f'/BaiTap/Lam/{expired["id"]}')
    req(student,f'/BaiTap/BatDau/{expired["id"]}',{'__RequestVerificationToken':token(p)})
    sql(f'''UPDATE "LuotLamKiemTra" SET "BatDauLuc"=now()-interval '6 minutes' WHERE "BaiTapGiaoId"={expired['id']};''')
    _,p=get(student,f'/BaiTap/Lam/{expired["id"]}')
    req(student,f'/BaiTap/Lam/{expired["id"]}',{'__RequestVerificationToken':token(p),'tn-test':'1'})
    assert sql(f'SELECT "DiemTuDong"::text FROM "BaiNopGiao" WHERE "BaiTapGiaoId"={expired["id"]};') == '0'
    print('PASS: server không nhận đáp án gửi sau khi hết giờ')

    guest=client(); _,p=get(guest,'/TaiKhoan/DangKy')
    s,u,_=req(guest,'/TaiKhoan/DangKy',{'HoTen':marker,'Email':email,'MatKhau':PASSWORD,'NhapLai':PASSWORD,'__RequestVerificationToken':token(p)})
    assert s==200 and '/DangKy' not in u
    assert req(guest,f'/BaiTap/Lam/{data["id"]}')[0] == 404
    assert '/KhongCoQuyen' in req(student,'/GiangVien/GiaoBai/Tao?kiemTra=true')[1]
    assert '/DangNhap' in req(client(),f'/BaiTap/Lam/{data["id"]}')[1]
    print('PASS: chặn học viên chưa ghi danh, chặn học viên vào soạn, yêu cầu đăng nhập')

    bank=int(sql(f'''INSERT INTO "CauHoi" ("BaiId","ThuTu","NoiDung","PhuongAn","DapAnDung","GiaiThich","NguonHienThi") VALUES ({bai},999,'{marker}',ARRAY['A','B','C','D'],'A','','') RETURNING "Id";''').splitlines()[0])
    bank_before=sql(f'SELECT "NoiDung" FROM "CauHoi" WHERE "Id"={bank};')
    for name in names[2:]:
        _,p=get(teacher,'/GiangVien/DeThi')
        s,_,p=req(teacher,'/GiangVien/DeThi/Luu',{'KhoaId':'lich-su-12','Ten':name,'SoPhut':5,'DangMo':'true','CauHoiIds':[bank],'__RequestVerificationToken':token(p)})
        assert s==200
    legacy=int(sql(f'''SELECT "Id" FROM "DeThi" WHERE "Ten"='{marker}-legacy';'''))
    _,p=get(teacher,f'/GiangVien/DeThi/Sua/{legacy}'); assert 'id="giaoBai"' in p and 'data-type="TL"' in p
    converted={**data,'id':0,'deThiId':legacy,'ten':marker+'-legacy','cau':questions}
    converted_id=save(converted)['id']
    assert sql(f'SELECT "BaiTapGiaoId" FROM "DeThi" WHERE "Id"={legacy};') == str(converted_id)
    assert sql(f'SELECT "NoiDung" FROM "CauHoi" WHERE "Id"={bank};') == bank_before
    locked=int(sql(f'''SELECT "Id" FROM "DeThi" WHERE "Ten"='{marker}-locked';'''))
    sql(f'''INSERT INTO "BaiLam" ("NguoiDungId","MaDe","LaDeThi","TenDe","SoCau","SoDung","NopLuc") SELECT "Id",{locked},true,'{marker}-locked',1,1,now() FROM "AspNetUsers" WHERE "Email"='hocvien@sudia.vn';''')
    _,p=get(teacher,f'/GiangVien/DeThi/Sua/{locked}'); assert 'data-locked="true"' in p
    save({**converted,'deThiId':locked,'ten':marker+'-locked'},400)
    assert req(teacher,'/GiangVien/DeThi/Luu',{'Id':locked,'KhoaId':'lich-su-12','Ten':marker+'-locked','SoPhut':5,'DangMo':'true','CauHoiIds':[bank],'__RequestVerificationToken':token(p)})[0] == 400
    assert sql(f'SELECT count(*) FROM "DeThi" WHERE "Id"={locked} AND "BaiTapGiaoId" IS NULL;')=='1'
    print('PASS: đề cũ chưa làm chuyển sang form chung không sửa ngân hàng; đề đã có kết quả được giữ nguyên')
finally:
    targets=','.join("'"+name+"'" for name in names)
    sql(f'''BEGIN; DELETE FROM "BaiLam" WHERE "TenDe" IN ({targets}); DELETE FROM "DeThi" WHERE "Ten" IN ({targets}); DELETE FROM "BaiTapGiao" WHERE "Ten" IN ({targets}); DELETE FROM "CauHoi" WHERE "NoiDung"='{marker}'; DELETE FROM "AspNetUsers" WHERE "Email"='{email}' AND "HoTen"='{marker}'; COMMIT;''')
    print('CLEANUP: chỉ xoá đề, bài nộp, lượt làm và tài khoản thử mang mã riêng')
