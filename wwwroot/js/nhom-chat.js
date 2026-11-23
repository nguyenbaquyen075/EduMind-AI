// Nhóm chat của lớp: lịch sử nạp sẵn trong trang, tin mới nhận qua SignalR (/hub/lop), gửi / xoá bằng POST có chống giả mạo form
(() => {
  const goc = document.querySelector('[data-nc]');
  if (!goc) return;
  const lopId = goc.dataset.lop, toi = goc.dataset.toi, quanLy = goc.dataset.quanLy === '1';
  const ds = goc.querySelector('[data-nc-ds]'), form = goc.querySelector('[data-nc-form]'), nhap = goc.querySelector('[data-nc-nhap]');
  const nut = goc.querySelector('[data-nc-nut]'), trong = goc.querySelector('[data-nc-trong]'), loi = goc.querySelector('[data-nc-loi]');
  const trangThai = document.querySelector('[data-nc-trang-thai]');
  const token = document.querySelector('input[name=__RequestVerificationToken]')?.value;
  const co = new Set(); let cuNhat = null, daCoTinCu = !!goc.querySelector('[data-nc-cu]');

  const gio = (iso) => { const d = new Date(iso), h = new Date(); const g = d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
    return d.toDateString() === h.toDateString() ? g : `${g} · ${d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' })}`; };
  const baoLoi = (s) => { loi.textContent = s || ''; loi.hidden = !s; };

  function the(t) {
    const la = t.nguoiDungId === toi;
    const li = document.createElement('div'); li.className = 'nc-tin' + (la ? ' cua-toi' : ''); li.dataset.id = t.id;
    const dau = document.createElement('div'); dau.className = 'nc-tin-dau';
    const ten = document.createElement('b'); ten.textContent = la ? 'Bạn' : t.tacGia; dau.append(ten);
    if (t.laGiangVien) { const c = document.createElement('em'); c.className = 'dd-chip ghim'; c.textContent = 'Giảng viên'; dau.append(c); }
    const g = document.createElement('time'); g.dateTime = t.ngayTao; g.textContent = gio(t.ngayTao); dau.append(g);
    if (la || quanLy) {
      const x = document.createElement('button'); x.type = 'button'; x.className = 'nc-xoa'; x.textContent = 'Xoá'; x.setAttribute('aria-label', 'Xoá tin nhắn này');
      x.addEventListener('click', () => xoa(t.id)); dau.append(x);
    }
    const nd = document.createElement('div'); nd.className = 'nc-tin-nd'; nd.textContent = t.noiDung;
    li.append(dau, nd); return li;
  }
  const gan = (t, dauDs) => {
    if (co.has(t.id)) return; co.add(t.id);
    const n = the(t);
    if (dauDs) ds.querySelector('[data-nc-cu]') ? ds.querySelector('[data-nc-cu]').after(n) : ds.prepend(n); else ds.append(n);
    if (cuNhat === null || t.id < cuNhat) cuNhat = t.id;
  };
  const cuonXuong = () => { ds.scrollTop = ds.scrollHeight; };
  const gan1 = () => (trong.hidden = co.size > 0);

  // Lịch sử có sẵn
  const dau = JSON.parse(document.getElementById('ncLichSu').textContent || '[]');
  dau.forEach((t) => gan(t)); gan1(); cuonXuong();

  // Tin cũ hơn
  goc.addEventListener('click', async (e) => {
    const b = e.target.closest('[data-nc-cu]'); if (!b) return;
    b.disabled = true;
    try {
      const r = await fetch(`/Lop/${lopId}/TinNhan?truoc=${cuNhat}`, { credentials: 'same-origin' });
      if (!r.ok) throw new Error();
      const d = await r.json(); const truocCao = ds.scrollHeight;
      [...d.tin].reverse().forEach((t) => gan(t, true));
      ds.scrollTop = ds.scrollHeight - truocCao;
      if (!d.coCu) b.remove(); else b.disabled = false;
    } catch { b.disabled = false; baoLoi('Không tải được tin cũ. Thử lại sau.'); }
  });

  // Gửi
  async function gui() {
    const nd = nhap.value.trim(); if (!nd) return;
    nut.disabled = true; baoLoi('');
    try {
      const r = await fetch(`/Lop/${lopId}/Gui`, { method: 'POST', credentials: 'same-origin', headers: { 'Content-Type': 'application/x-www-form-urlencoded', RequestVerificationToken: token }, body: 'noiDung=' + encodeURIComponent(nd) });
      if (r.status === 404) throw new Error('Bạn không còn trong lớp này.');
      const d = await r.json().catch(() => ({}));
      if (!r.ok) throw new Error(d.loi || 'Không gửi được. Thử lại sau.');
      nhap.value = ''; nhap.style.height = '';
      if (d.tin) { gan(d.tin); gan1(); cuonXuong(); }   // hiện ngay; tin phát qua SignalR có cùng id nên không bị lặp
    } catch (er) { baoLoi(er.message); }
    finally { nut.disabled = false; nhap.focus(); }
  }
  form.addEventListener('submit', (e) => { e.preventDefault(); gui(); });
  nhap.addEventListener('keydown', (e) => { if (e.key === 'Enter' && !e.shiftKey && !e.isComposing) { e.preventDefault(); gui(); } });
  nhap.addEventListener('input', () => { nhap.style.height = 'auto'; nhap.style.height = Math.min(nhap.scrollHeight, 140) + 'px'; });

  async function xoa(id) {
    if (!confirm('Xoá tin nhắn này?')) return;
    const r = await fetch(`/Lop/${lopId}/TinNhan/${id}/Xoa`, { method: 'POST', credentials: 'same-origin', headers: { RequestVerificationToken: token } });
    if (!r.ok) baoLoi('Không xoá được tin nhắn.'); else go(id);
  }
  function go(id) { ds.querySelector(`[data-id="${id}"]`)?.remove(); co.delete(id); gan1(); }

  // Thời gian thực
  if (window.signalR) {
    const kn = new signalR.HubConnectionBuilder().withUrl(`/hub/lop?lopId=${lopId}`).withAutomaticReconnect([0, 2000, 5000, 10000, 30000]).build();
    kn.on('NhanTin', (t) => { const cuoi = ds.scrollHeight - ds.scrollTop - ds.clientHeight < 80 || t.nguoiDungId === toi; gan(t); gan1(); if (cuoi) cuonXuong(); });
    kn.on('XoaTin', go);
    const dat = (s, ok) => { if (trangThai) { trangThai.textContent = s; trangThai.dataset.ok = ok ? '1' : '0'; } };
    kn.onreconnecting(() => dat('Mất kết nối, đang nối lại…', false));
    kn.onreconnected(async () => { dat('Đã kết nối', true);
      try { const r = await fetch(`/Lop/${lopId}/TinNhan`, { credentials: 'same-origin' }); if (r.ok) { (await r.json()).tin.forEach((t) => gan(t)); gan1(); } } catch { /* giữ nguyên */ } });
    kn.onclose(() => dat('Mất kết nối. Tải lại trang để nối lại.', false));
    kn.start().then(() => dat('Đã kết nối', true)).catch(() => dat('Không kết nối được máy chủ chat. Tin vẫn gửi được, tải lại trang để thấy tin mới.', false));
  } else if (trangThai) trangThai.textContent = 'Không tải được thư viện chat. Tải lại trang.';
})();
