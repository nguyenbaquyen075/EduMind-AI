"""Kiểm thử nhập Office trên localhost bằng tài liệu tự tạo, không gọi AI."""
import html, http.cookiejar, io, json, os, re, ssl, subprocess, urllib.error, urllib.parse, urllib.request, uuid, zipfile

base = os.environ.get('SUDIA_TEST_URL', 'https://localhost:7180').rstrip('/')
assert urllib.parse.urlparse(base).hostname in ('localhost', '127.0.0.1', '::1')
marker = 'sudia-office-' + uuid.uuid4().hex
c = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()), urllib.request.HTTPSHandler(context=ssl._create_unverified_context()))
def request(path, data=None, headers=None, client=c):
    try:
        with client.open(urllib.request.Request(base+path, data, headers or {}), timeout=30) as r:
            return r.status, r.url, r.read().decode('utf-8-sig')
    except urllib.error.HTTPError as e:
        return e.code, e.url, e.read().decode()
def token(p): return html.unescape(re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"',p)[1])
def form(path, fields): return request(path, urllib.parse.urlencode(fields).encode(), {'Content-Type':'application/x-www-form-urlencoded'})
_,_,p=request('/TaiKhoan/DangNhap')
s,u,p=form('/TaiKhoan/DangNhap', {'Email':'giangvien@sudia.vn','MatKhau':os.environ.get('SUDIA_TEST_PASSWORD','SuDia@2026'),'__RequestVerificationToken':token(p)})
assert s==200 and '/DangNhap' not in u
_,_,p=request('/GiangVien/NoiDung/TaoChuDe?khoa=lich-su-12')
assert 'id="taiLieu"' in p and 'nhap-tai-lieu.js' in p
tok=token(p)
def package(files):
    data=io.BytesIO()
    with zipfile.ZipFile(data,'w',zipfile.ZIP_DEFLATED) as z:
        for path,text in files.items(): z.writestr(path,text)
    return data.getvalue()
def upload(name,data,csrf=True):
    boundary='office'+uuid.uuid4().hex
    body=(f'--{boundary}\r\nContent-Disposition: form-data; name="taiLieu"; filename="{name}"\r\nContent-Type: application/octet-stream\r\n\r\n'.encode()+data+f'\r\n--{boundary}--\r\n'.encode())
    headers={'Content-Type':'multipart/form-data; boundary='+boundary}
    if csrf: headers['RequestVerificationToken']=tok
    s,_,p=request('/GiangVien/NoiDung/NhapTaiLieu',body,headers)
    return s,json.loads(p) if p else {}
w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'
word=package({'word/document.xml':f'''<w:document xmlns:w="{w}"><w:body>
<w:p><w:pPr><w:pStyle w:val="CustomHeading"/></w:pPr><w:r><w:t>I. Khí hậu</w:t></w:r></w:p>
<w:p><w:r><w:t>Đây là đoạn văn tiếng Việt đầu tiên, cần được giữ nguyên.</w:t></w:r></w:p>
<w:p><w:pPr><w:outlineLvl w:val="1"/></w:pPr><w:r><w:t>1. Nhiệt độ</w:t></w:r></w:p>
<w:p><w:r><w:t>Nhiệt độ trung bình trên 20°C, không bỏ số liệu.</w:t></w:r></w:p>
<w:tbl><w:tr><w:tc><w:p><w:r><w:t>Mùa hạ</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>30°C</w:t></w:r></w:p></w:tc></w:tr></w:tbl>
<w:p><w:r><w:drawing/></w:r></w:p>
</w:body></w:document>''', 'word/styles.xml':f'''<w:styles xmlns:w="{w}"><w:style w:styleId="CustomHeading"><w:basedOn w:val="Heading1"/></w:style><w:style w:styleId="Heading1"><w:pPr><w:outlineLvl w:val="0"/></w:pPr></w:style></w:styles>'''})
s,doc=upload('bai-hoc.docx',word)
assert s==200,(s,doc)
assert doc['tenBai']=='I. Khí hậu' and '# I. Khí hậu' in doc['noiDung'] and '## 1. Nhiệt độ' in doc['noiDung']
assert 'Mùa hạ | 30°C' in doc['noiDung'] and len(doc['canhBao'])==2
assert doc['noiDung'].index('đầu tiên') < doc['noiDung'].index('trên 20°C')
print('PASS: Word giữ chữ tiếng Việt, cấp Heading kế thừa, thứ tự; cảnh báo ảnh/bảng')
pns='http://schemas.openxmlformats.org/presentationml/2006/main'; ans='http://schemas.openxmlformats.org/drawingml/2006/main'; rns='http://schemas.openxmlformats.org/officeDocument/2006/relationships'
def slide(title,body): return f'''<p:sld xmlns:p="{pns}" xmlns:a="{ans}"><p:cSld><p:spTree><p:sp><p:nvSpPr><p:nvPr><p:ph type="title"/></p:nvPr></p:nvSpPr><p:txBody><a:p><a:r><a:t>{title}</a:t></a:r></a:p></p:txBody></p:sp><p:sp><p:txBody><a:p><a:r><a:t>{body}</a:t></a:r></a:p></p:txBody></p:sp></p:spTree></p:cSld></p:sld>'''
ppt=package({'ppt/presentation.xml':f'<p:presentation xmlns:p="{pns}" xmlns:r="{rns}"><p:sldIdLst><p:sldId r:id="r2"/><p:sldId r:id="r1"/></p:sldIdLst></p:presentation>', 'ppt/_rels/presentation.xml.rels':'<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="r1" Target="slides/slide1.xml"/><Relationship Id="r2" Target="slides/slide2.xml"/></Relationships>', 'ppt/slides/slide1.xml':slide('Địa hình','Phần tiếp theo trên slide 1, cần được nhập sau slide 2.'),'ppt/slides/slide2.xml':slide('Địa hình','Phần đầu tiên trên slide 2, cần được nhập trước slide 1.')})
s,result=upload('bai-hoc.pptx',ppt)
assert s==200 and result['noiDung'].count('# Địa hình')==1
assert result['noiDung'].index('Phần đầu tiên') < result['noiDung'].index('Phần tiếp theo')
print('PASS: PowerPoint theo thứ tự trình chiếu, slide tiếp cùng tên không tạo mục lặp')
assert upload('cu.doc',word)[0]==400
assert upload('hong.docx',b'not a zip')[0]==400
assert upload('bai.docx',word,False)[0]==400
assert upload('dai.docx',package({'word/document.xml':f'<w:document xmlns:w="{w}"><w:body><w:p><w:r><w:t>'+('a'*31000)+'</w:t></w:r></w:p></w:body></w:document>'}))[0]==400
print('PASS: chặn file cũ/hỏng, thiếu CSRF và nội dung quá dài; không âm thầm cắt chữ')
try:
    s,u,p=form('/GiangVien/NoiDung/TaoChuDeBangAi',{'khoaId':'lich-su-12','tenChuDe':marker,'tenBai':marker,'soBai':999,'noiDung':doc['noiDung'],'chiaTheoVanBan':'true','__RequestVerificationToken':tok})
    decoded=html.unescape(p)
    assert s==200 and '/XemThu?' in u and 'Nhiệt độ' in decoded and 'trên 20°C' in decoded,(s,u)
    print('PASS: nội dung nhập từ file đi qua chia đề mục và mở bản xem thử sửa được')
finally:
    query=f'''DELETE FROM "ChuDe" WHERE "Ten" LIKE '%{marker}' AND "KhoaId"='lich-su-12';'''
    subprocess.run(['psql','-d',os.environ.get('SUDIA_TEST_DATABASE','sudia_dev'),'-v','ON_ERROR_STOP=1','-c',query],check=True,capture_output=True)
    print('CLEANUP: xoá đúng chủ đề/bài nháp thử có mã riêng')
