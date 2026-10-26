import html, http.cookiejar, json, os, re, subprocess, urllib.error, urllib.parse, urllib.request, uuid
# Chỉ chạy trên database Development. Script xoá đúng bài tập và bài nộp thử do nó tạo.
BASE = os.environ.get('SUDIA_TEST_URL', 'http://localhost:5280').rstrip('/')
PASSWORD = os.environ.get('SUDIA_TEST_PASSWORD', 'SuDia@2026')
DATABASE = os.environ.get('SUDIA_TEST_DATABASE', 'sudia_dev')
if urllib.parse.urlparse(BASE).hostname not in ('localhost', '127.0.0.1', '::1'):
    raise SystemExit('Kiểm tra này chỉ được chạy trên web cục bộ.')
marker = 'sudia-assignment-' + uuid.uuid4().hex
created = []
def client(): return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
def req(c, path, data=None, isjson=False, tok=None):
    headers = {}
    if data is not None:
        body = json.dumps(data).encode() if isjson else urllib.parse.urlencode(data).encode()
        headers['Content-Type'] = 'application/json' if isjson else 'application/x-www-form-urlencoded'
    else: body = None
    if tok: headers['RequestVerificationToken'] = tok
    try:
        with c.open(urllib.request.Request(BASE+path, body, headers), timeout=30) as r:
            return r.status, r.url, html.unescape(r.read().decode())
    except urllib.error.HTTPError as e: return e.code, e.url, html.unescape(e.read().decode())
def token(page): return re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page)[1]
def login(email):
    c = client(); _,_,p = req(c,'/TaiKhoan/DangNhap')
    status,url,_=req(c,'/TaiKhoan/DangNhap',{'Email':email,'MatKhau':PASSWORD,'__RequestVerificationToken':token(p)})
    assert status==200 and '/DangNhap' not in url
    return c
teacher=login('giangvien@sudia.vn'); student=login('hocvien@sudia.vn')
_,_,page=req(teacher,'/GiangVien/GiaoBai/Tao?bai=6'); tok=token(page)
def save(data, expected=200):
    status,_,body=req(teacher,'/GiangVien/GiaoBai/Luu',data,True,tok)
    assert status==expected,(status,body[:1000])
    return json.loads(body)
questions=[{'id':'tn-test','kieu':'TN','noiDung':'Câu TN '+marker,'diem':1,'phuongAn':['Một','Hai','Ba','Bốn'],'dapAn':1,'y':[],'loiGiai':'LOI-GIAI-'+marker},
 {'id':'ds-test','kieu':'DS','noiDung':'Câu DS '+marker,'diem':1,'phuongAn':[], 'y':[{'noiDung':'Ý '+str(i),'dung':i%2==0,'diem':.25} for i in range(4)],'loiGiai':'LOI-GIAI-'+marker},
 {'id':'tl-test','kieu':'TL','noiDung':'Câu TL '+marker,'diem':1,'phuongAn':[],'y':[],'loiGiai':'LOI-GIAI-'+marker}]
data={'id':0,'baiId':6,'buoiHocId':None,'ten':marker,'huongDan':'Hướng dẫn thử','hanNop':None,'daGiao':False,'cau':questions}
try:
    status,_,body=req(teacher,'/GiangVien/CauHoi')
    assert status==200 and '/GiangVien/GiaoBai/Tao?bai=6' in body and 'Thêm câu hỏi' in body
    draft=save(data); created.append(draft['id']); data['id']=draft['id']
    assert req(student,f'/BaiTap/Lam/{data["id"]}')[0]==404
    assert req(teacher,f'/GiangVien/GiaoBai/Sua/{data["id"]}')[0]==200
    print('PASS: menu, lưu nháp, mở lại; học viên không thấy nháp')
    bad=json.loads(json.dumps(data));bad['daGiao']=True;bad['cau'][0]['dapAn']=None
    assert 'phương án' in save(bad,400)['loi']
    bad=json.loads(json.dumps(data));bad['buoiHocId']=999999
    assert 'Buổi học' in save(bad,400)['loi']
    bad=json.loads(json.dumps(data));bad['baiId']=999999
    assert req(teacher,'/GiangVien/GiaoBai/Luu',bad,True,tok)[0]==404
    bad=json.loads(json.dumps(data));bad['cau'][1]['y'][0]['diem']=-1
    assert 'điểm' in save(bad,400)['loi']
    assert req(teacher,'/GiangVien/GiaoBai/Luu',data,True)[0]==400
    print('PASS: chặn thiếu đáp án, sai buổi/bài, điểm âm, thiếu CSRF')
    data['daGiao']=True;save(data)
    status,_,body=req(student,'/BaiTap?bai=6'); assert status==200 and marker in body,(status,body[:500])
    status,_,body=req(student,f'/BaiTap/Lam/{data["id"]}')
    assert status==200 and 'Câu TN' in body and 'Câu DS' in body and 'Câu TL' in body,(status,body[:500])
    assert 'LOI-GIAI-'+marker not in body
    print('PASS: xuất bản, học viên thấy đủ 3 loại câu, không lộ đáp án/lời giải')
    answers={'__RequestVerificationToken':token(body),'tn-test':'1','tl-test':'Bài tự luận thử'}
    answers.update({f'ds-test-{i}':'true' if i%2==0 else 'false' for i in range(4)})
    status,_,body=req(student,f'/BaiTap/Lam/{data["id"]}',answers)
    assert status==200 and 'LOI-GIAI-'+marker in body and 'chờ giảng viên chấm' in body,(status,body[:500])
    assert re.search(r'Điểm trắc nghiệm và đúng / sai: <b>2(?:[.,]0+)?</b>',body),body[:1000]
    print('PASS: nộp bài, chấm tự động 2 điểm, tự luận chờ chấm')
    status,_,body=req(teacher,f'/GiangVien/GiaoBai/BaiNop/{data["id"]}')
    assert status==200 and 'Bài tự luận thử' in body
    submission=int(re.search(r'name="id" value="(\d+)"',body)[1])
    status,_,body=req(teacher,'/GiangVien/GiaoBai/ChamTuLuan',{'id':submission,'diem':'1','__RequestVerificationToken':token(body)})
    assert status==200 and 'Đã lưu điểm tự luận' in body
    _,_,body=req(student,f'/BaiTap/Lam/{data["id"]}')
    assert 'chờ giảng viên chấm' not in body and re.search(r'Tổng điểm: <b>3(?:[.,]0+)? / 3',body),body[:1000]
    blocked=save(data,400);assert 'đã có học viên nộp' in blocked['loi']
    print('PASS: giảng viên chấm tự luận, học viên thấy tổng 3/3; khoá sửa bài đã nộp')
    assert '/KhongCoQuyen' in req(student,'/GiangVien/GiaoBai/Tao?bai=6')[1]
    assert req(client(),f'/BaiTap/Lam/{data["id"]}')[1].find('/DangNhap')>=0
    print('PASS: phân quyền giảng viên/học viên và yêu cầu đăng nhập')
finally:
    for i in created:
        sql=f'DELETE FROM "BaiTapGiao" WHERE "Id" = {int(i)} AND "Ten" = \'{marker}\';'
        subprocess.run(['psql','-d',DATABASE,'-v','ON_ERROR_STOP=1','-c',sql],check=True,capture_output=True)
    print('CLEANUP: đã xoá bài tập và bài nộp thử theo mã riêng')
