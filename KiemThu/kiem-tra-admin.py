import csv, html, http.cookiejar, io, json, os, re, subprocess, urllib.error, urllib.parse, urllib.request, uuid
# Chỉ dùng với dữ liệu Development; tự xoá đúng dữ liệu thử có mã riêng.
BASE=os.environ.get('SUDIA_TEST_URL','http://localhost:5280').rstrip('/')
PASSWORD=os.environ.get('SUDIA_TEST_PASSWORD','SuDia@2026')
DATABASE=os.environ.get('SUDIA_TEST_DATABASE','sudia_dev')
if urllib.parse.urlparse(BASE).hostname not in ('localhost','127.0.0.1','::1'):
    raise SystemExit('Kiểm tra này chỉ chạy trên web cục bộ.')
marker='sudia-admin-'+uuid.uuid4().hex[:12]
email=marker+'@example.test'
edited=marker+'-edited@example.test'
uid=None
def client(): return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
def req(c,path,data=None,isjson=False,tok=None):
    headers={};body=None
    if data is not None:
        body=json.dumps(data).encode() if isjson else urllib.parse.urlencode(data).encode()
        headers['Content-Type']='application/json' if isjson else 'application/x-www-form-urlencoded'
    if tok:headers['RequestVerificationToken']=tok
    try:
        with c.open(urllib.request.Request(BASE+path,body,headers),timeout=30) as r:return r.status,r.url,html.unescape(r.read().decode('utf-8-sig'))
    except urllib.error.HTTPError as e:return e.code,e.url,html.unescape(e.read().decode())
def token(page):return re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"',page)[1]
def get(c,path):
    s,u,p=req(c,path);assert s==200,(path,s,p[:1500]);return u,p
def post(c,path,data,source):
    _,page=get(c,source);data={**data,'__RequestVerificationToken':token(page)}
    return req(c,path,data)
def login(email):
    c=client();s,u,p=post(c,'/TaiKhoan/DangNhap',{'Email':email,'MatKhau':PASSWORD},'/TaiKhoan/DangNhap')
    assert s==200 and '/DangNhap' not in u,(s,u);return c
def sql(query):return subprocess.check_output(['psql','-d',DATABASE,'-At','-v','ON_ERROR_STOP=1','-c',query],text=True).strip()
admin=login('admin@sudia.vn')
routes=['/Admin','/Admin/NguoiDung','/Admin/KhoaHoc','/Admin/GhiDanh','/Admin/NoiDung','/Admin/BaiTap','/Admin/LichDay','/Admin/BaoCao','/Admin/TroLy']
try:
    for path in routes:
        _,p=get(admin,path);assert 'ql--admin' in p and '/css/admin.css?v=' in p,path
    print('PASS: 9 trang admin hoạt động, có menu và giao diện mới')
    for path in ['/Admin/NoiDung?trangThai=thieu-noi-dung','/Admin/NoiDung?q=not-found-'+marker,'/Admin/BaiTap?trangThai=cho-cham','/Admin/LichDay?trangThai=tat-ca','/Admin/GhiDanh?trangThai=het-han','/Admin/GhiDanh?trang=999999999']:
        get(admin,path)
    _,p=get(admin,'/Admin/BaoCao?tu=2026-10-27&den=2026-10-01');assert 'Chọn khoảng thời gian' in p
    assert req(admin,'/Admin/BaoCao/Xuat?tu=2026-10-27&den=2026-10-01')[0]==400
    _,p=get(admin,'/Admin/BaoCao?tu=0001-01-01&den=9999-12-31');assert 'Chọn khoảng thời gian' in p
    print('PASS: bộ lọc, phân trang, ngày không hợp lệ không gây lỗi')
    s,u,p=post(admin,'/Admin/NguoiDung/Tao',{'HoTen':marker,'Email':email,'MatKhau':PASSWORD,'VaiTro':'HocVien'},'/Admin/NguoiDung/Tao')
    assert s==200 and 'Đã tạo tài khoản' in p,(s,p[:1200])
    _,p=get(admin,'/Admin/NguoiDung?q='+marker)
    uid=re.search(r'/Admin/NguoiDung/ChiTiet/([^"?]+)',p)[1]
    get(admin,'/Admin/NguoiDung/ChiTiet/'+uid)
    _,p=get(admin,'/Admin/NguoiDung?q='+marker+'&vaiTro=HocVien&chuaGhiDanh=true');assert '/ChiTiet/'+uid in p
    s,_,p=post(admin,'/Admin/NguoiDung/Sua',{'Id':uid,'HoTen':marker+' đã sửa','Email':edited},'/Admin/NguoiDung/Sua/'+uid)
    assert s==200 and edited in p and 'Đã cập nhật thông tin' in p
    print('PASS: tạo tài khoản, chi tiết và sửa thông tin người dùng')
    _,p=get(admin,'/Admin/KhoaHoc/Tao')
    teacher=re.search(r'<option value="([^"]+)"[^>]*>[^<]*giangvien@sudia.vn',p)[1]
    course={'Id':marker,'Ten':marker,'KyHieu':'KT','MoTa':'','Anh':'','GiangVienId':teacher}
    s,_,p=post(admin,'/Admin/KhoaHoc/Tao',course,'/Admin/KhoaHoc/Tao');assert s==200 and 'Đã tạo khoá' in p,(s,p[:2000])
    s,_,p=post(admin,'/Admin/KhoaHoc/Tao',course,'/Admin/KhoaHoc/Tao');assert 'Mã khoá đã tồn tại' in p
    invalid={**course,'Id':marker+'-invalid','GiangVienId':'no-such-user'}
    s,_,p=post(admin,'/Admin/KhoaHoc/Tao',invalid,'/Admin/KhoaHoc/Tao');assert 'Chọn giảng viên hợp lệ' in p
    print('PASS: tạo khoá với trường tuỳ chọn trống; chặn trùng mã và giảng viên sai')
    s,_,p=post(admin,'/Admin/KhoaHoc/ThemGhiDanh',{'id':marker,'email':edited.upper(),'hanDen':'2030-01-01'},'/Admin/GhiDanh');assert s==200 and 'Đã ghi danh' in p
    _,p=get(admin,'/Admin/GhiDanh?q='+marker+'&khoa='+marker+'&trangThai=con-han');assert edited in p
    _,p=get(admin,'/Admin/NguoiDung?q='+marker+'&vaiTro=HocVien&chuaGhiDanh=true');assert '/ChiTiet/'+uid not in p
    s,_,p=post(admin,'/Admin/KhoaHoc/DoiHan',{'id':marker,'nguoiDungId':uid,'hanDen':'2020-01-01'},'/Admin/KhoaHoc/GhiDanh/'+marker);assert s==200
    _,p=get(admin,'/Admin/GhiDanh?khoa='+marker+'&trangThai=het-han');assert edited in p
    post(admin,'/Admin/KhoaHoc/DoiHan',{'id':marker,'nguoiDungId':uid,'hanDen':'2030-01-01'},'/Admin/KhoaHoc/GhiDanh/'+marker)
    print('PASS: ghi danh, gia hạn và bộ lọc hết hạn/còn hạn')
    s,_,p=post(admin,'/GiangVien/NoiDung/ThemChuDe',{'khoaId':marker,'ten':marker},'/GiangVien/NoiDung?khoa='+marker);assert s==200
    topic=int(sql(f'SELECT "Id" FROM "ChuDe" WHERE "KhoaId"=\'{marker}\';'))
    s,url,p=post(admin,'/GiangVien/NoiDung/ThemBai',{'chuDeId':topic,'ten':marker,'soBai':1},'/GiangVien/NoiDung?khoa='+marker);assert s==200
    lesson=int(url.rstrip('/').split('/')[-1])
    post(admin,'/GiangVien/NoiDung/LuuBai',{'id':lesson,'ten':marker,'soBai':1,'hocThu':'false','daXuatBan':'true'},'/GiangVien/NoiDung/SoanBai/'+str(lesson))
    _,p=get(admin,'/Admin/NoiDung?khoa='+marker+'&trangThai=thieu-noi-dung');assert f'/SoanBai/{lesson}' in p
    _,p=get(admin,'/Admin/NoiDung?khoa='+marker+'&trangThai=thieu-noi-dung-xuat-ban');assert f'/SoanBai/{lesson}' in p
    s,_,p=post(admin,'/GiangVien/LichDay/Luu',{'BaiId':lesson,'Link':'https://meet.google.com/test-admin-demo','ThoiGian':'2030-01-01T19:30'},'/GiangVien/LichDay/Tao?bai='+str(lesson));assert s==200 and 'Đã lưu buổi học' in p
    _,p=get(admin,'/Admin/LichDay?khoa='+marker+'&trangThai=sap-toi');assert marker in p and '19:30' in p
    print('PASS: quản lý nội dung thiếu, lịch dạy và phân công khoá')
    _,p=get(admin,'/GiangVien/GiaoBai/Tao?bai='+str(lesson))
    assignment={'id':0,'baiId':lesson,'ten':marker,'huongDan':'Kiểm tra admin','daGiao':True,'cau':[{'id':'tl-admin-test','kieu':'TL','noiDung':'Tự luận thử','diem':1,'loiGiai':'Đáp án thử'}]}
    s,_,p=req(admin,'/GiangVien/GiaoBai/Luu',assignment,True,token(p));assert s==200,(s,p[:1000]);assignment_id=json.loads(p)['id']
    learner=login(edited);_,p=get(learner,'/BaiTap/Lam/'+str(assignment_id))
    s,_,p=req(learner,'/BaiTap/Lam/'+str(assignment_id),{'tl-admin-test':'Bài nộp thử admin','__RequestVerificationToken':token(p)});assert s==200
    _,p=get(admin,'/Admin/BaiTap?khoa='+marker+'&trangThai=cho-cham');assert f'/BaiNop/{assignment_id}' in p
    _,p=get(admin,'/Admin/NguoiDung/ChiTiet/'+uid);assert marker in p and 'Chờ chấm tự luận' in p
    _,p=get(admin,'/Admin');assert 'Bài tự luận chờ chấm' in p
    _,p=get(admin,'/Admin/BaoCao/Xuat?khoa='+marker)
    rows=list(csv.reader(io.StringIO(p)))
    assert ['Bài tập đã nộp','1'] in rows and ['Học viên hoạt động','1'] in rows,rows
    print('PASS: bài nộp chờ chấm xuất hiện ở dashboard, quản lý, hồ sơ và CSV đúng số liệu')
    for email_role in ['giangvien@sudia.vn','hocvien@sudia.vn']:
        user=login(email_role)
        for path in routes:
            s,url,_=req(user,path);assert '/KhongCoQuyen' in url,(email_role,path,s,url)
    s,url,_=req(client(),'/Admin');assert '/DangNhap' in url
    print('PASS: toàn bộ khu admin chặn học viên/giảng viên; yêu cầu đăng nhập')
finally:
    sql(f'DELETE FROM "Khoa" WHERE "Id"=\'{marker}\' AND "Ten"=\'{marker}\';')
    if not uid:uid=sql(f'SELECT "Id" FROM "AspNetUsers" WHERE "Email" IN (\'{email}\',\'{edited}\');')
    if uid:sql(f'DELETE FROM "AspNetUsers" WHERE "Id"=\'{uid}\' AND "Email" IN (\'{email}\',\'{edited}\');')
    print('CLEANUP: đã xoá đúng khoá, tài khoản và dữ liệu thử có mã riêng')
