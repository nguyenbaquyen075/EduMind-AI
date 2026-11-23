// Trang chủ GeoEdu
// - Hero: Parallax 3D theo chuột, bong bóng thoại nổi, hạt sao pastel Canvas 60fps, đếm số thống kê, tìm kiếm trực tiếp, thông báo & modal giới thiệu.
// - Khung chat AI: tự chạy 2 câu hỏi mẫu, gõ câu riêng gọi API thật POST /api/tro-ly/hoi, nghiêng kính 3D theo chuột.
// - Lộ trình cá nhân: chọn nút hoặc nhắn tin tự nhiên để tạo lộ trình theo tuần.

(() => {
  const giamChuyenDong = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const $ = (id) => document.getElementById(id);

  // ===================================================== Thanh điều hướng cố định + active theo mục cuộn
  const head = $('tcHead');
  const capNhatChieuCao = () => {
    if (head) document.documentElement.style.setProperty('--tc-head-h', head.offsetHeight + 'px');
  };
  const capNhatCuon = () => {
    if (head) head.classList.toggle('is-scrolled', scrollY > 24);
    const navLinks = document.querySelectorAll('.tc-nav-link[data-nav]');
    const sections = ['top', 'chatbot', 'khoahoc', 'giangvien', 'lotrinh'];
    let current = 'top';
    for (const id of sections) {
      const el = $(id);
      if (el && el.getBoundingClientRect().top <= 160) current = id;
    }
    navLinks.forEach((a) => a.classList.toggle('is-active', a.dataset.nav === current));
  };
  capNhatChieuCao();
  capNhatCuon();
  addEventListener('resize', capNhatChieuCao);
  addEventListener('scroll', capNhatCuon, { passive: true });

  // Menu di động
  const burger = $('tcBurger');
  const drawer = $('tcMobileDrawer');
  if (burger && drawer) {
    burger.addEventListener('click', () => {
      const open = drawer.hidden;
      drawer.hidden = !open;
      drawer.classList.toggle('is-open', open);
      burger.setAttribute('aria-expanded', String(open));
    });
    drawer.querySelectorAll('a').forEach((a) => a.addEventListener('click', () => {
      drawer.hidden = true;
      drawer.classList.remove('is-open');
      burger.setAttribute('aria-expanded', 'false');
    }));
  }

  // ===================================================== Chuông thông báo
  const bellBtn = $('tcBellBtn');
  const bellPop = $('tcBellPop');
  const bellBadge = $('tcBellBadge');
  if (bellBtn && bellPop) {
    bellBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      const willOpen = bellPop.hidden;
      bellPop.hidden = !willOpen;
      bellBtn.setAttribute('aria-expanded', String(willOpen));
      if (willOpen && bellBadge) bellBadge.hidden = true;
    });
    document.addEventListener('click', (e) => {
      if (!bellPop.hidden && !bellPop.contains(e.target) && !bellBtn.contains(e.target)) {
        bellPop.hidden = true;
        bellBtn.setAttribute('aria-expanded', 'false');
      }
    });
  }

  // ===================================================== Tìm kiếm nhanh khóa học & bài học
  const searchWrap = $('tcSearchWrap');
  const searchInput = $('tcSearchInput');
  const searchResults = $('tcSearchResults');
  if (searchInput && searchResults) {
    let data = { goi: [], mon: [] };
    try { data = JSON.parse($('timKiemData')?.textContent || '{}'); } catch { /* bỏ qua */ }
    const norm = (s) => (s || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();

    const mucTim = [
      ...(data.goi || []).map((g) => ({ ten: g.ten, tag: 'Khóa học', href: g.url })),
      ...(data.mon || []).flatMap((m) => (m.chuong || []).map((c) => ({ ten: c, tag: m.ten, href: m.baiDau }))),
      { ten: 'Hỏi đáp Trợ lý AI 24/7', tag: 'AI', href: '#chatbot' },
      { ten: 'Tạo lộ trình ôn thi cá nhân', tag: 'Lộ trình', href: '#lotrinh' },
      { ten: 'Đội ngũ giảng viên biên soạn', tag: 'Giới thiệu', href: '#giangvien' },
    ];

    const hienKetQua = () => {
      const q = norm(searchInput.value);
      if (!q) { searchResults.hidden = true; return; }
      const hit = mucTim.filter((x) => norm(x.ten + ' ' + x.tag).includes(q)).slice(0, 6);
      searchResults.innerHTML = '';
      if (hit.length) {
        hit.forEach((x) => {
          const a = document.createElement('a');
          a.className = 'tc-sr-item';
          a.href = x.href;
          a.innerHTML = `<span></span><span class="tc-sr-tag"></span>`;
          a.firstChild.textContent = x.ten;
          a.lastChild.textContent = x.tag;
          a.addEventListener('click', () => { searchResults.hidden = true; });
          searchResults.appendChild(a);
        });
      } else {
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'tc-sr-item';
        btn.innerHTML = `<span>Hỏi AI: "${searchInput.value.trim()}"</span><span class="tc-sr-tag">Hỏi AI</span>`;
        btn.addEventListener('click', () => {
          const cau = searchInput.value.trim();
          searchResults.hidden = true;
          $('chatbot')?.scrollIntoView({ behavior: 'smooth' });
          setTimeout(() => gui(cau), 320);
        });
        searchResults.appendChild(btn);
      }
      searchResults.hidden = false;
    };

    searchInput.addEventListener('input', hienKetQua);
    searchInput.addEventListener('focus', hienKetQua);
    searchInput.addEventListener('keydown', (e) => {
      if (e.key === 'Escape') searchResults.hidden = true;
      if (e.key === 'Enter' && searchInput.value.trim()) {
        e.preventDefault();
        const first = searchResults.querySelector('.tc-sr-item');
        if (first) first.click();
      }
    });
    document.addEventListener('click', (e) => {
      if (searchWrap && !searchWrap.contains(e.target)) searchResults.hidden = true;
    });
  }

  // ===================================================== Hiệu ứng nổi nhẹ cho phần tử phụ (KHÔNG zoom/scale ảnh nền để giữ ảnh nét tối đa)
  const heroStage = $('heroStage');
  const heroCanvas = $('heroCanvas');
  const floatItems = document.querySelectorAll('[data-depth]');

  if (heroStage && floatItems.length && !giamChuyenDong) {
    let targetX = 0, targetY = 0, curX = 0, curY = 0;
    heroStage.addEventListener('mousemove', (e) => {
      const r = heroStage.getBoundingClientRect();
      targetX = ((e.clientX - r.left) / r.width - 0.5) * 2;
      targetY = ((e.clientY - r.top) / r.height - 0.5) * 2;
    });
    heroStage.addEventListener('mouseleave', () => { targetX = 0; targetY = 0; });

    const animateFloats = () => {
      curX += (targetX - curX) * 0.06;
      curY += (targetY - curY) * 0.06;
      floatItems.forEach((item) => {
        const d = Number(item.dataset.depth || 12);
        item.style.marginRight = `${(-curX * d).toFixed(2)}px`;
        item.style.marginTop = `${(curY * (d * 0.65)).toFixed(2)}px`;
      });
      requestAnimationFrame(animateFloats);
    };
    requestAnimationFrame(animateFloats);
  }

  // ===================================================== Hiệu ứng 3D nghiêng theo chuột & bấm hỏi AI cho 4 khung ảnh con
  const polaroids = document.querySelectorAll('.tc-polaroid[data-tilt]');
  polaroids.forEach((card) => {
    const baseRot = parseFloat(getComputedStyle(card).getPropertyValue('--rot')) || 0;
    if (!giamChuyenDong) {
      card.addEventListener('mousemove', (e) => {
        const r = card.getBoundingClientRect();
        const px = ((e.clientX - r.left) / r.width - 0.5) * 2;
        const py = ((e.clientY - r.top) / r.height - 0.5) * 2;
        card.style.transform = `perspective(700px) translateY(-10px) scale(1.13) rotate(${(baseRot * 0.35).toFixed(1)}deg) rotateX(${(-py * 12).toFixed(1)}deg) rotateY(${(px * 14).toFixed(1)}deg)`;
      });
      card.addEventListener('mouseleave', () => {
        card.style.transform = '';
      });
    }
    card.addEventListener('click', () => {
      const q = card.dataset.ask;
      if (!q) return;
      $('chatbot')?.scrollIntoView({ behavior: 'smooth' });
      setTimeout(() => gui(q), 380);
    });
  });

  // Canvas hạt ánh sáng nhẹ pastel hợp nền trắng-tím GeoEdu
  if (heroStage && heroCanvas && !giamChuyenDong) {
    const ctx = heroCanvas.getContext('2d');
    let W = 0, H = 0, dpr = Math.min(devicePixelRatio || 1, 2);
    const resize = () => {
      const r = heroStage.getBoundingClientRect();
      W = r.width; H = r.height;
      heroCanvas.width = Math.round(W * dpr);
      heroCanvas.height = Math.round(H * dpr);
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    };
    resize();
    addEventListener('resize', resize);

    const COLORS = ['#8B5CF6', '#60A5FA', '#FACC15', '#C084FC', '#FFFFFF'];
    const ambient = Array.from({ length: 34 }, () => ({
      x: 0.42 + Math.random() * 0.56,
      y: Math.random(),
      r: 1.2 + Math.random() * 2.4,
      vy: 0.00016 + Math.random() * 0.00034,
      vx: (Math.random() - 0.5) * 0.00014,
      phase: Math.random() * Math.PI * 2,
      star: Math.random() > 0.45,
      color: COLORS[Math.floor(Math.random() * COLORS.length)],
    }));

    const trail = [];
    let lastEmit = 0;
    const spawn = (x, y, count, speed = 1) => {
      for (let i = 0; i < count; i++) {
        const ang = Math.random() * Math.PI * 2;
        const spd = (0.35 + Math.random() * 1.3) * speed;
        trail.push({
          x, y,
          vx: Math.cos(ang) * spd,
          vy: Math.sin(ang) * spd - 0.3,
          r: 1.4 + Math.random() * 2.4,
          life: 1,
          decay: 0.02 + Math.random() * 0.02,
          star: Math.random() > 0.35,
          color: COLORS[Math.floor(Math.random() * COLORS.length)],
        });
      }
      if (trail.length > 80) trail.splice(0, trail.length - 80);
    };

    heroStage.addEventListener('mousemove', (e) => {
      const now = performance.now();
      if (now - lastEmit < 32) return;
      lastEmit = now;
      const r = heroStage.getBoundingClientRect();
      spawn(e.clientX - r.left, e.clientY - r.top, 2, 0.85);
    });
    heroStage.addEventListener('click', (e) => {
      if (e.target.closest('a, button, input')) return;
      const r = heroStage.getBoundingClientRect();
      spawn(e.clientX - r.left, e.clientY - r.top, 12, 1.6);
    });

    const drawStar = (x, y, r, alpha, color) => {
      ctx.save();
      ctx.globalAlpha = alpha;
      ctx.fillStyle = color;
      ctx.shadowColor = color;
      ctx.shadowBlur = r * 3.5;
      ctx.beginPath();
      for (let i = 0; i < 4; i++) {
        const a = (i * Math.PI) / 2;
        ctx.lineTo(x + Math.cos(a) * r * 2.2, y + Math.sin(a) * r * 2.2);
        ctx.lineTo(x + Math.cos(a + Math.PI / 4) * r * 0.55, y + Math.sin(a + Math.PI / 4) * r * 0.55);
      }
      ctx.closePath();
      ctx.fill();
      ctx.restore();
    };

    const drawOrb = (x, y, r, alpha, color) => {
      ctx.save();
      ctx.globalAlpha = alpha;
      ctx.fillStyle = color;
      ctx.shadowColor = color;
      ctx.shadowBlur = r * 4;
      ctx.beginPath();
      ctx.arc(x, y, r, 0, Math.PI * 2);
      ctx.fill();
      ctx.restore();
    };

    const tick = (now) => {
      ctx.clearRect(0, 0, W, H);
      const t = now * 0.002;
      for (const p of ambient) {
        p.y -= p.vy;
        p.x += p.vx + Math.sin(t + p.phase) * 0.00007;
        if (p.y < -0.04) { p.y = 1.03; p.x = 0.42 + Math.random() * 0.56; }
        const alpha = 0.22 + 0.58 * (0.5 + 0.5 * Math.sin(t * 1.4 + p.phase));
        if (p.star) drawStar(p.x * W, p.y * H, p.r, alpha, p.color);
        else drawOrb(p.x * W, p.y * H, p.r, alpha * 0.75, p.color);
      }
      for (let i = trail.length - 1; i >= 0; i--) {
        const p = trail[i];
        p.x += p.vx; p.y += p.vy;
        p.vx *= 0.98; p.vy *= 0.98;
        p.life -= p.decay;
        if (p.life <= 0) { trail.splice(i, 1); continue; }
        if (p.star) drawStar(p.x, p.y, p.r * p.life, p.life * 0.85, p.color);
        else drawOrb(p.x, p.y, p.r * p.life, p.life * 0.75, p.color);
      }
      requestAnimationFrame(tick);
    };
    requestAnimationFrame(tick);
  }

  // ===================================================== Đếm số tăng dần cho 3 thẻ thống kê
  const counters = document.querySelectorAll('.tc-stat-card strong[data-count]');
  if (counters.length && !giamChuyenDong) {
    counters.forEach((el) => {
      const target = Number(el.dataset.count || 0);
      const suffix = el.dataset.suffix || '';
      const useDot = el.dataset.dot === 'true';
      const dur = 1200;
      const start = performance.now();
      const fmt = (n) => (useDot ? n.toLocaleString('vi-VN') : String(n)) + suffix;
      const step = (now) => {
        const p = Math.min(1, (now - start) / dur);
        const eased = 1 - Math.pow(1 - p, 3);
        el.textContent = fmt(Math.round(target * eased));
        if (p < 1) requestAnimationFrame(step);
      };
      requestAnimationFrame(step);
    });
  }

  // ===================================================== Modal Xem giới thiệu
  const introModal = $('introModal');
  const openIntro = $('btnXemGioiThieu');
  const closeIntro = $('introModalClose');
  const dongModal = () => {
    if (!introModal) return;
    introModal.hidden = true;
    introModal.classList.remove('is-open');
  };
  const moModal = () => {
    if (!introModal) return;
    introModal.hidden = false;
    introModal.classList.add('is-open');
  };
  if (openIntro && introModal) {
    dongModal();
    openIntro.addEventListener('click', moModal);
    closeIntro?.addEventListener('click', dongModal);
    introModal.addEventListener('click', (e) => { if (e.target === introModal) dongModal(); });
    $('introTryAi')?.addEventListener('click', dongModal);
    $('introSeeCourses')?.addEventListener('click', dongModal);
    document.addEventListener('keydown', (e) => { if (e.key === 'Escape' && !introModal.hidden) dongModal(); });
  }

  // ===================================================== Khung chat AI
  const list = $('chatList');
  const inner = $('chatInner');
  const empty = $('chatEmpty');
  const typing = $('chatTyping');
  const input = $('chatHoi');
  const form = $('chatForm');
  const sendBtn = $('chatGui');
  const mon = $('chatMon');

  const lichSu = [];
  let timers = [];
  let demoOn = true;
  let busy = false;
  let dangViet = null;

  const later = (fn, ms) => { const id = setTimeout(() => { timers = timers.filter((x) => x !== id); fn(); }, ms); timers.push(id); return id; };
  const clearAll = () => { timers.forEach(clearTimeout); timers = []; };
  const scrollDown = () => { if (list) list.scrollTop = list.scrollHeight; };
  const capNhatNut = () => { if (sendBtn && input) sendBtn.disabled = !input.value.trim(); };

  const MAU = {
    'Vì sao Tây Nguyên trồng nhiều cà phê?': {
      label: 'Địa lí 12',
      text: 'Tây Nguyên có đất badan màu mỡ, tầng phong hoá sâu, phân bố tập trung trên các cao nguyên; khí hậu cận xích đạo với một mùa mưa và một mùa khô rõ rệt, thuận lợi cho phơi sấy và bảo quản. Vì vậy cà phê là cây công nghiệp lâu năm quan trọng nhất của vùng.',
      mark: '', source: 'Nguồn: Địa lí 12 · Vùng Tây Nguyên',
    },
    'Chiến dịch Điện Biên Phủ diễn ra khi nào?': {
      label: 'Lịch sử 12',
      text: 'Chiến dịch Điện Biên Phủ diễn ra từ ngày 13/3 đến ngày 7/5/1954, gồm ba đợt tấn công. Đợt 1 (13–17/3) tiêu diệt cụm cứ điểm Him Lam và phân khu Bắc; đợt 2 (30/3–26/4) đánh chiếm các điểm cao phía đông phân khu Trung tâm; đợt 3 (1/5–7/5) tổng công kích. Chiều 7/5/1954, toàn bộ tập đoàn cứ điểm bị tiêu diệt, tướng Đờ Cát-xtơ-ri bị bắt sống.',
      mark: '', source: 'Nguồn: Lịch sử 12 · Chiến dịch Điện Biên Phủ năm 1954',
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
        body: JSON.stringify({ cauHoi: q, khoaId: mon.value || null, lichSu: lichSu.slice(-6) }),
      });
      if (res.status === 429) return { label: 'Tạm dừng', text: 'Bạn hỏi hơi nhanh, vui lòng đợi một chút rồi hỏi tiếp.', mark: '', source: '' };
      if (res.status === 400) {
        const d = await res.json().catch(() => ({}));
        return { label: 'Chưa gửi được', text: d.loi || 'Câu hỏi chưa hợp lệ.', mark: '', source: '' };
      }
      if (!res.ok) throw new Error(res.status);
      const d = await res.json();
      lichSu.push({ cauHoi: q, traLoi: d.noiDung });
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
  input.addEventListener('input', () => { if (demoOn) dungDemo(); capNhatNut(); });
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
  if (giamChuyenDong) { demoOn = false; empty.hidden = false; } else later(tiepDemo, 900);

  // Lắc nhẹ mỗi 8 giây + nghiêng 3D khung kính chat theo chuột (giống bản gốc)
  const glass = $('chatGlass');
  const chatSection = $('chatbot');
  if (glass && chatSection && !giamChuyenDong) {
    let mx = 0, my = 0, tx = 0, ty = 0;
    chatSection.addEventListener('mousemove', (e) => {
      const r = chatSection.getBoundingClientRect();
      mx = ((e.clientX - r.left) / r.width - 0.5) * 2;
      my = ((e.clientY - r.top) / r.height - 0.5) * 2;
    });
    chatSection.addEventListener('mouseleave', () => { mx = 0; my = 0; });
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
    const ds = s.mon === 'su' ? su : s.mon === 'dia' ? dia : dia.concat(su);
    const W = soTuan(s);
    const w1 = Math.max(1, Math.round(W * 0.4)), w2 = Math.max(1, Math.round(W * 0.3)), w3 = Math.max(1, Math.round(W * 0.2));
    const nua = Math.ceil(ds.length / 2);
    const tuan = (a, b) => (a >= b ? 'Tuần ' + a : 'Tuần ' + a + '–' + b);
    const muc = s.diem === '7' ? 'câu nhận biết, thông hiểu' : s.diem === '8' ? 'thêm câu vận dụng' : 'thêm câu vận dụng cao, đề nâng cao';
    const p = Number(s.phut);
    const ngay = p <= 40 ? '1 mục lý thuyết + 10 câu hỏi' : p <= 75 ? '2 mục lý thuyết + 20 câu hỏi + hỏi AI phần chưa hiểu' : '3 mục lý thuyết + 30 câu hỏi + 1 đề mini 15 phút';
    return {
      tieuDe: (s.mon === 'su' ? 'Lịch sử 12' : s.mon === 'dia' ? 'Địa lí 12' : 'Địa & Sử 12') + ' · mục tiêu ' + (s.diem === '9' ? '9+' : s.diem) + ' điểm',
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
    }, giamChuyenDong ? 0 : 900);
  }

  $('planTao').addEventListener('click', () => taoLoTrinh(planInput.value.trim() || viet(chon)));
  $('planForm').addEventListener('submit', (e) => {
    e.preventDefault();
    const t = planInput.value.trim();
    if (t) taoLoTrinh(t);
  });
})();
