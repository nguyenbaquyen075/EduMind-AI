// Trang chủ SửĐịa.AI
// - Khung chat: tự chạy 2 câu hỏi mẫu (trả lời soạn sẵn, không tốn lượt AI),
//   người xem gõ câu riêng thì gọi API thật POST /api/tro-ly/hoi (TroLyController).
// - Khung chat lắc nhẹ mỗi 8 giây và nghiêng 3D theo chuột.
// - Lộ trình cá nhân: chọn nút hoặc nhắn tin, tạo lộ trình theo tuần ngay trên trình duyệt.

(() => {
  const giamChuyenDong = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const $ = (id) => document.getElementById(id);

  // Thanh menu cố định trên cùng; đo chiều cao thực của nó (menu có thể xuống dòng trên màn hẹp)
  const menu = document.querySelector('.tc-head');
  const chinhMenu = () => { if (menu) document.documentElement.style.setProperty('--tc-head-h', menu.offsetHeight + 'px'); };
  chinhMenu();
  addEventListener('resize', chinhMenu);
  addEventListener('load', chinhMenu);
  // Ẩn menu khi chưa lướt; lướt xuống quá 60px thì hiện
  if (menu) {
    menu.classList.add('tc-head-auto');
    const hienMenu = () => menu.classList.toggle('is-on', scrollY > 60);
    hienMenu();
    addEventListener('scroll', hienMenu, { passive: true });
    // Bật hiệu ứng trượt sau khung hình đầu để lúc tải trang menu không bị trượt ra rồi vào
    requestAnimationFrame(() => requestAnimationFrame(() => menu.classList.add('tc-head-anim')));
  }

  // ===================================================== Khung chat
  const list = $('chatList');
  const inner = $('chatInner');
  const empty = $('chatEmpty');
  const typing = $('chatTyping');
  const input = $('chatHoi');
  const form = $('chatForm');
  const sendBtn = $('chatGui');
  const mon = $('chatMon');

  const lichSu = []; // 3 lượt gần nhất gửi kèm để server hiểu câu hỏi nối tiếp
  let timers = [];
  let demoOn = true;
  let busy = false;
  let dangViet = null; // { el, full, timer } câu trả lời đang hiện từng chữ

  const later = (fn, ms) => { const id = setTimeout(() => { timers = timers.filter((x) => x !== id); fn(); }, ms); timers.push(id); return id; };
  const clearAll = () => { timers.forEach(clearTimeout); timers = []; };
  const scrollDown = () => { list.scrollTop = list.scrollHeight; };
  const capNhatNut = () => { sendBtn.disabled = !input.value.trim(); };

  const MAU = {
    'Chiến dịch Điện Biên Phủ diễn ra khi nào?': {
      label: 'Lịch sử 12',
      text: 'Chiến dịch Điện Biên Phủ diễn ra từ ngày 13/3 đến ngày 7/5/1954, gồm ba đợt tấn công. Đợt 1 (13–17/3) tiêu diệt cụm cứ điểm Him Lam và phân khu Bắc; đợt 2 (30/3–26/4) đánh chiếm các điểm cao phía đông phân khu Trung tâm; đợt 3 (1/5–7/5) tổng công kích. Chiều 7/5/1954, toàn bộ tập đoàn cứ điểm bị tiêu diệt, tướng Đờ Cát-xtơ-ri bị bắt sống.',
      mark: '', source: 'Nguồn: Lịch sử 12 · Chiến dịch Điện Biên Phủ năm 1954',
    },
    'Vì sao Tây Nguyên trồng nhiều cà phê?': {
      label: 'Địa lí 12',
      text: 'Tây Nguyên có đất badan màu mỡ, tầng phong hoá sâu, phân bố tập trung trên các cao nguyên; khí hậu cận xích đạo với một mùa mưa và một mùa khô rõ rệt, thuận lợi cho phơi sấy và bảo quản. Vì vậy cà phê là cây công nghiệp lâu năm quan trọng nhất của vùng.',
      mark: '', source: 'Nguồn: Địa lí 12 · Vùng Tây Nguyên',
    },
  };

  function themUser(text) {
    const el = $('tplUser').content.firstElementChild.cloneNode(true);
    el.textContent = text;
    inner.insertBefore(el, typing);
  }

  function themAi(a) {
    const el = $('tplAi').content.firstElementChild.cloneNode(true);
    const f = (k) => el.querySelector('[data-f="' + k + '"]');
    f('label').textContent = a.label || '';
    f('mark').textContent = a.mark || '';
    f('source').textContent = a.source || '';
    inner.insertBefore(el, typing);
    return el;
  }

  // Hiện câu trả lời từng chữ, xong mới hiện nguồn
  function vietTungChu(a, xong) {
    const el = themAi(a);
    const text = el.querySelector('[data-f="text"]');
    const caret = el.querySelector('[data-f="caret"]');
    const src = el.querySelector('[data-f="source"]');
    const full = a.text || '';
    let k = 0;
    const ketThuc = () => { text.textContent = full; caret.remove(); src.hidden = !a.source; dangViet = null; scrollDown(); xong && xong(); };
    dangViet = { ketThuc };
    if (giamChuyenDong) { ketThuc(); return; }
    const buoc = () => {
      k = Math.min(full.length, k + 4 + Math.floor(Math.random() * 4));
      text.textContent = full.slice(0, k);
      scrollDown();
      if (k < full.length) later(buoc, 16); else ketThuc();
    };
    later(buoc, 60);
  }

  async function hoiServer(q) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    try {
      const res = await fetch('/api/tro-ly/hoi', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', RequestVerificationToken: token },
        body: JSON.stringify({ cauHoi: q, khoaId: mon.value || null, lichSu: lichSu.slice(-3) }),
      });
      if (res.status === 429) return { label: 'Tạm dừng', text: 'Bạn hỏi hơi nhanh, vui lòng đợi một chút rồi hỏi tiếp.', mark: '', source: '' };
      if (res.status === 400) {
        const d = await res.json().catch(() => ({}));
        return { label: 'Chưa gửi được', text: d.loi || 'Câu hỏi chưa hợp lệ.', mark: '', source: '' };
      }
      if (!res.ok) throw new Error(res.status);
      const d = await res.json();
      if (!d.tuChoi) lichSu.push({ cauHoi: q, traLoi: d.noiDung });
      return { label: d.nhan, text: d.noiDung, mark: d.danhDau, source: d.nguon };
    } catch {
      return { label: 'Lỗi kết nối', text: 'Không gửi được câu hỏi. Vui lòng thử lại.', mark: '', source: '' };
    }
  }

  function dungDemo() {
    if (!demoOn && !dangViet) return;
    demoOn = false;
    clearAll();
    if (dangViet) dangViet.ketThuc();
    typing.hidden = true;
    busy = false;
  }

  function gui(raw, tuDemo) {
    const q = (raw || '').trim();
    if (!q) return;
    if (!tuDemo) dungDemo(); else if (busy) return;
    empty.hidden = true;
    input.value = ''; capNhatNut();
    themUser(q);
    busy = true; typing.hidden = false; scrollDown();

    const xong = () => { busy = false; if (tuDemo) later(tiepDemo, 1500); };
    if (tuDemo && MAU[q]) {
      later(() => { typing.hidden = true; vietTungChu(MAU[q], xong); }, 550);
    } else {
      hoiServer(q).then((a) => { typing.hidden = true; vietTungChu(a, xong); });
    }
  }

  // Màn tự chạy: gõ từng ký tự vào ô nhập rồi gửi
  const DEMO = Object.keys(MAU);
  let demoIdx = 0;
  function goDemo(text, i) {
    if (!demoOn) return;
    if (i <= text.length) { input.value = text.slice(0, i); capNhatNut(); later(() => goDemo(text, i + 1), 18 + Math.random() * 22); }
    else later(() => gui(text, true), 220);
  }
  function tiepDemo() {
    if (!demoOn) return;
    if (demoIdx >= DEMO.length) { demoOn = false; return; }
    goDemo(DEMO[demoIdx++], 1);
  }

  form.addEventListener('submit', (e) => { e.preventDefault(); gui(input.value); });
  input.addEventListener('input', () => { if (demoOn) { dungDemo(); } capNhatNut(); });
  input.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' && !e.shiftKey && !e.isComposing) { e.preventDefault(); gui(input.value); }
  });
  document.querySelectorAll('.tc-chip[data-q]').forEach((b) => b.addEventListener('click', () => gui(b.dataset.q)));
  $('chatMoi').addEventListener('click', () => {
    dungDemo();
    inner.querySelectorAll('.tc-msg').forEach((m) => m.remove());
    lichSu.length = 0;
    input.value = ''; capNhatNut();
    empty.hidden = false;
  });
  capNhatNut();
  if (giamChuyenDong) { demoOn = false; empty.hidden = false; } else later(tiepDemo, 600);

  // ===================================================== Lắc nhẹ + nghiêng theo chuột
  const glass = $('chatGlass');
  const hero = $('chatbot');
  if (glass && hero && !giamChuyenDong) {
    let mx = 0, my = 0, tx = 0, ty = 0;
    hero.addEventListener('mousemove', (e) => {
      const r = hero.getBoundingClientRect();
      mx = ((e.clientX - r.left) / r.width - 0.5) * 2;
      my = ((e.clientY - r.top) / r.height - 0.5) * 2;
    });
    hero.addEventListener('mouseleave', () => { mx = 0; my = 0; });
    const t0 = performance.now();
    const loop = (now) => {
      const t = (now - t0) / 1000;
      tx += (mx - tx) * 0.08; if (Math.abs(tx) < 0.005 && mx === 0) tx = 0;
      ty += (my - ty) * 0.08; if (Math.abs(ty) < 0.005 && my === 0) ty = 0;
      let rot = 0;
      const ph = t % 8;
      if (ph < 0.8) rot = 1.2 * Math.sin(ph * 26) * (1 - ph / 0.8);
      glass.style.transform = (Math.abs(tx) + Math.abs(ty) < 0.01 && Math.abs(rot) < 0.01)
        ? 'none'
        : `perspective(1400px) rotateX(${(-ty * 5).toFixed(2)}deg) rotateY(${(tx * 7).toFixed(2)}deg) rotate(${rot.toFixed(2)}deg)`;
      requestAnimationFrame(loop);
    };
    requestAnimationFrame(loop);
  }

  // ===================================================== Lộ trình cá nhân
  let chuong = { su: [], dia: [] };
  try { chuong = JSON.parse($('loTrinhData').textContent); } catch { /* giữ rỗng */ }
  const chon = { mon: 'both', diem: '8', thang: '3', phut: '60' };
  const planInput = $('planHoi');

  const tenMon = (m) => (m === 'su' ? 'Lịch sử' : m === 'dia' ? 'Địa lí' : 'cả Sử và Địa');
  const soTuan = (s) => Math.max(4, Math.round(Number(s.thang) * 4));
  const viet = (s) => {
    const thang = Number(s.thang);
    const tg = thang < 1 ? Math.round(thang * 4) + ' tuần' : thang + ' tháng';
    return `Em muốn ôn ${tenMon(s.mon)}, mục tiêu ${s.diem === '9' ? '9+' : s.diem} điểm, còn ${tg}, mỗi ngày học ${s.phut} phút.`;
  };

  function capNhatNutChon() {
    document.querySelectorAll('.tc-opt-group').forEach((g) => {
      g.querySelectorAll('button').forEach((b) => b.setAttribute('aria-pressed', String(b.dataset.v === chon[g.dataset.key])));
    });
  }

  document.querySelectorAll('.tc-opt-group').forEach((g) => {
    g.querySelectorAll('button').forEach((b) => b.addEventListener('click', () => {
      chon[g.dataset.key] = b.dataset.v;
      capNhatNutChon();
      planInput.value = viet(chon);
    }));
  });

  // Hiểu tin nhắn tự nhiên: môn, điểm, thời gian, số phút mỗi ngày
  function docTinNhan(text) {
    const t = ' ' + text.toLowerCase() + ' ';
    const up = {};
    const coSu = /sử/.test(t), coDia = /địa/.test(t);
    if (/cả hai|hai môn|2 môn/.test(t) || (coSu && coDia)) up.mon = 'both'; else if (coSu) up.mon = 'su'; else if (coDia) up.mon = 'dia';
    let m = t.match(/(\d+(?:[.,]\d+)?)\s*(?:\+\s*)?(?:điểm|đ\b)/);
    if (m) { const v = parseFloat(m[1].replace(',', '.')); up.diem = v < 7.5 ? '7' : v < 8.5 ? '8' : '9'; }
    m = t.match(/(\d+(?:[.,]\d+)?)\s*tháng/); if (m) up.thang = String(parseFloat(m[1].replace(',', '.')));
    m = t.match(/(\d+)\s*tuần/); if (m) up.thang = String(Number(m[1]) / 4);
    m = t.match(/(\d+)\s*(?:phút|p\b)/); if (m) up.phut = String(Number(m[1]));
    m = t.match(/(\d+(?:[.,]\d+)?)\s*(?:giờ|tiếng|h\b)/); if (m) up.phut = String(Math.round(parseFloat(m[1].replace(',', '.')) * 60));
    if (/nửa tiếng|nửa giờ/.test(t)) up.phut = '30';
    if (/một tiếng|một giờ/.test(t) && !up.phut) up.phut = '60';
    return up;
  }

  function lapLoTrinh(s) {
    const su = (chuong.su || []).map((x) => 'Sử: ' + x);
    const dia = (chuong.dia || []).map((x) => 'Địa: ' + x);
    const ds = s.mon === 'su' ? su : s.mon === 'dia' ? dia : su.concat(dia);
    const W = soTuan(s);
    const w1 = Math.max(1, Math.round(W * 0.4)), w2 = Math.max(1, Math.round(W * 0.3)), w3 = Math.max(1, Math.round(W * 0.2));
    const nua = Math.ceil(ds.length / 2);
    const tuan = (a, b) => (a >= b ? 'Tuần ' + a : 'Tuần ' + a + '–' + b);
    const muc = s.diem === '7' ? 'câu nhận biết, thông hiểu' : s.diem === '8' ? 'thêm câu vận dụng' : 'thêm câu vận dụng cao, đề nâng cao';
    const p = Number(s.phut);
    const ngay = p <= 40 ? '1 mục lý thuyết + 10 câu hỏi' : p <= 75 ? '2 mục lý thuyết + 20 câu hỏi + hỏi AI phần chưa hiểu' : '3 mục lý thuyết + 30 câu hỏi + 1 đề mini 15 phút';
    return {
      tieuDe: (s.mon === 'su' ? 'Lịch sử 12' : s.mon === 'dia' ? 'Địa lí 12' : 'Sử & Địa 12') + ' · mục tiêu ' + (s.diem === '9' ? '9+' : s.diem) + ' điểm',
      soTuan: W + ' tuần',
      giaiDoan: [
        { ten: 'Nền tảng', tuan: tuan(1, w1), chiTiet: ds.slice(0, nua).join(' · ') || 'Lý thuyết các chương đầu' },
        { ten: 'Củng cố', tuan: tuan(w1 + 1, w1 + w2), chiTiet: ds.slice(nua).join(' · ') || 'Lý thuyết các chương còn lại' },
        { ten: 'Luyện đề theo chương', tuan: tuan(w1 + w2 + 1, w1 + w2 + w3), chiTiet: 'Làm câu hỏi theo bài, ' + muc + '. Hỏi AI ngay câu chưa hiểu.' },
        { ten: 'Thi thử & ôn chương yếu', tuan: tuan(w1 + w2 + w3 + 1, W), chiTiet: 'Đề tính giờ mỗi tuần, ôn lại các chương AI đánh dấu còn yếu.' },
      ],
      moiNgay: ngay + ' (' + s.phut + ' phút)',
    };
  }

  let dangTao = false;
  function taoLoTrinh(text) {
    if (dangTao) return;
    const up = docTinNhan(text);
    Object.assign(chon, up);
    capNhatNutChon();
    const W = soTuan(chon);
    $('planUser').textContent = text;
    $('planReply').textContent = Object.keys(up).length
      ? `Mình hiểu: ôn ${tenMon(chon.mon)} 12, mục tiêu ${chon.diem === '9' ? '9+' : chon.diem} điểm, còn khoảng ${W} tuần, mỗi ngày ${chon.phut} phút. Đây là lộ trình gợi ý:`
      : 'Mình chưa rõ lắm, bạn cho mình biết môn, mục tiêu điểm, thời gian còn lại và số phút học mỗi ngày nhé. Tạm thời mình dùng thông tin đang chọn:';
    $('planMsg').hidden = false;
    $('planIdle').hidden = true;
    $('planResult').hidden = true;
    $('planLoading').hidden = false;
    planInput.value = '';
    dangTao = true;

    setTimeout(() => {
      const kq = lapLoTrinh(chon);
      $('planTitle').textContent = kq.tieuDe;
      $('planWeeks').textContent = kq.soTuan;
      $('planDaily').textContent = kq.moiNgay;
      const ol = $('planPhases');
      ol.innerHTML = '';
      $('planLoading').hidden = true;
      $('planResult').hidden = false;
      kq.giaiDoan.forEach((g, i) => setTimeout(() => {
        const li = document.createElement('li');
        const n = document.createElement('span'); n.className = 'tc-phase-n'; n.textContent = String(i + 1);
        const card = document.createElement('div'); card.className = 'tc-phase-card';
        const top = document.createElement('div'); top.className = 'tc-phase-top';
        const ten = document.createElement('p'); ten.textContent = g.ten;
        const tuan = document.createElement('span'); tuan.textContent = g.tuan;
        const ct = document.createElement('p'); ct.textContent = g.chiTiet;
        top.append(ten, tuan); card.append(top, ct); li.append(n, card); ol.append(li);
        if (i === kq.giaiDoan.length - 1) dangTao = false;
      }, giamChuyenDong ? 0 : i * 260));
    }, giamChuyenDong ? 0 : 1200);
  }

  $('planTao').addEventListener('click', () => taoLoTrinh(planInput.value.trim() || viet(chon)));
  $('planForm').addEventListener('submit', (e) => {
    e.preventDefault();
    const t = planInput.value.trim();
    if (t) taoLoTrinh(t);
  });
})();
