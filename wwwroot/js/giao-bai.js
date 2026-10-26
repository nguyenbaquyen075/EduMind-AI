(() => {
  const root = document.getElementById('giaoBai');
  if (!root) return;
  const data = JSON.parse(document.getElementById('gbData').textContent);
  const buoi = JSON.parse(document.getElementById('gbBuoiData').textContent);
  const $ = (id) => document.getElementById(id);
  const esc = (v) => String(v ?? '').replace(/[&<>"']/g, (c) => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const names = { TN: 'Trắc nghiệm (A/B/C/D)', DS: 'Đúng / Sai', TL: 'Tự luận' };
  let step = 1, dirty = false, saving = false;
  const locked = root.dataset.locked === 'true';
  const fresh = (kieu) => ({ id: crypto.randomUUID(), kieu, noiDung: '', diem: 1,
    phuongAn: ['', '', '', ''], dapAn: null,
    y: kieu === 'DS' ? Array.from({length: 4}, () => ({noiDung: '', dung: null, diem: .25})) : [], loiGiai: '' });
  if (!data.id && !data.cau.length) data.cau = ['TN','DS','TL'].map(fresh);
  const active = new Set(data.cau.map(c => c.kieu));
  $('gbTen').value = data.ten;
  $('gbBai').value = data.baiId || '';
  $('gbHuongDan').value = data.huongDan;
  if (data.hanNop) {
    const d = new Date(data.hanNop);
    $('gbHan').value = new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
  }
  function renderBuoi(selected) {
    $('gbBuoi').innerHTML = '<option value="">Bài tập của bài học (không gắn buổi)</option>' + buoi.filter(b => b.baiId === Number($('gbBai').value))
      .map(b => `<option value="${b.id}">${b.thoiGian ? esc(new Date(b.thoiGian).toLocaleString('vi-VN')) : `Buổi #${b.id} · chưa đặt giờ`}</option>`).join('');
    $('gbBuoi').value = selected || '';
  }
  renderBuoi(data.buoiHocId);
  const totalCau = c => c.kieu === 'DS' ? c.y.reduce((s,y) => s + Number(y.diem || 0), 0) : Number(c.diem || 0);
  const total = () => data.cau.reduce((s,c) => s + totalCau(c), 0);
  const score = (c, path, value) => `<input type="number" min="0.01" max="100" step="0.01" value="${esc(value)}" data-path="${path}" aria-label="Điểm câu hỏi hoặc ý" class="gb-score">`;
  const text = (path, value, placeholder, area = false) => area
    ? `<textarea data-path="${path}" placeholder="${placeholder}" aria-label="${placeholder}" rows="2" maxlength="20000">${esc(value)}</textarea>`
    : `<input type="text" data-path="${path}" value="${esc(value)}" placeholder="${placeholder}" aria-label="${placeholder}" maxlength="10000">`;
  function render() {
    root.querySelectorAll('[data-type]').forEach(el => { el.checked = active.has(el.dataset.type); });
    $('gbCau').innerHTML = ['TN','DS','TL'].filter(k => active.has(k)).map((k, ki) => {
      const list = data.cau.filter(c => c.kieu === k);
      return `<section class="gb-section ${k === 'TL' ? 'warm' : ''}" aria-label="${names[k]}">
      <h3><span aria-hidden="true">${k === 'TN' ? '⊙' : k === 'DS' ? '⊜' : '≡'}</span> Phần ${['I','II','III'][['TN','DS','TL'].indexOf(k)]}: ${names[k]}</h3>
      <div class="gb-section-body">${list.map((c, i) => `<article class="gb-question" data-id="${esc(c.id)}">
        <header><span class="gb-grip" aria-hidden="true">⠿</span><b>${k} Câu ${i+1}</b>
        <span class="gb-muted">${k === 'DS' ? `Tổng điểm: <strong data-subtotal>${totalCau(c).toFixed(2)}đ</strong>` : `Điểm: ${score(c, 'diem', c.diem)}`}</span>
        <div class="gb-tools"><button type="button" data-move="-1" aria-label="Chuyển câu lên" ${i === 0 ? 'disabled' : ''}>↑</button><button type="button" data-move="1" aria-label="Chuyển câu xuống" ${i === list.length-1 ? 'disabled' : ''}>↓</button><button type="button" data-delete aria-label="Xoá câu ${i+1}" class="gb-delete"><svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M3 6h18M9 6V3h6v3M5 6l1 15h12l1-15M10 10v7M14 10v7"/></svg> <span>Xoá</span></button></div></header>
        ${text('noiDung', c.noiDung, k === 'DS' ? 'Câu dẫn…' : 'Nội dung câu hỏi…', true)}
        ${k === 'TN' ? `<div class="gb-options">${c.phuongAn.map((p,j) => `<div><label><input type="radio" name="answer-${esc(c.id)}" data-path="dapAn" value="${j}" ${c.dapAn === j ? 'checked' : ''} aria-label="Chọn đáp án ${'ABCD'[j]}"> ${'ABCD'[j]}.</label>${text(`phuongAn.${j}`, p, `Đáp án ${'ABCD'[j]}`)}</div>`).join('')}</div>` : ''}
        ${k === 'DS' ? `<div class="gb-statements">${c.y.map((y,j) => `<div><b>${'abcd'[j]}</b>${text(`y.${j}.noiDung`, y.noiDung, `Ý ${'abcd'[j]}…`)}<div class="gb-truefalse">${[true,false].map(v => `<label><input type="radio" name="y-${esc(c.id)}-${j}" data-path="y.${j}.dung" value="${v}" ${y.dung === v ? 'checked' : ''}><span>${v ? '✓ Đúng' : '✕ Sai'}</span></label>`).join('')}</div><span class="gb-muted">Điểm</span>${score(c, `y.${j}.diem`, y.diem)}</div>`).join('')}</div>` : ''}
        <details class="gb-explanation"><summary>${k === 'TL' ? 'Đáp án tham khảo & hướng dẫn chấm' : 'Lời giải (hiện sau khi nộp bài)'}</summary>${text('loiGiai', c.loiGiai, k === 'TL' ? 'Đáp án tham khảo, tiêu chí chấm…' : 'Nhập lời giải…', true)}</details>
        ${k === 'TL' ? '<p class="gb-muted">Học viên nhập câu trả lời tự luận. Giảng viên chấm điểm sau khi nộp.</p>' : ''}
      </article>`).join('')}
      <button type="button" class="gb-add" data-add="${k}">+ Thêm câu ${k === 'TN' ? 'trắc nghiệm' : k === 'DS' ? 'Đúng/Sai' : 'tự luận'}</button></div></section>`;
    }).join('');
    updateTotal();
    if (locked) root.querySelectorAll('input,textarea,select,[data-add],[data-delete],[data-move]').forEach(e => e.disabled = true);
  }
  function updateTotal() {
    $('gbTong').textContent = total().toFixed(2) + 'đ';
    root.querySelectorAll('[data-subtotal]').forEach(el => {
      const c = data.cau.find(c => c.id === el.closest('[data-id]').dataset.id);
      el.textContent = totalCau(c).toFixed(2) + 'đ';
    });
  }
  function error(message) { $('gbLoi').textContent = message || ''; $('gbLoi').hidden = !message; }
  function info() {
    data.ten = $('gbTen').value.trim(); data.baiId = Number($('gbBai').value);
    data.buoiHocId = Number($('gbBuoi').value) || null; data.huongDan = $('gbHuongDan').value.trim();
    data.hanNop = $('gbHan').value ? new Date($('gbHan').value).toISOString() : null;
    return data.ten && data.baiId;
  }
  function validate() {
    if (!info()) return 'Nhập tên bài tập và chọn bài học ở bước Thông tin chung.';
    if (!data.cau.length) return 'Thêm ít nhất một câu hỏi.';
    for (const [i,c] of data.cau.entries()) {
      if (!c.noiDung.trim()) return `Câu ${i+1}: chưa nhập nội dung.`;
      if (c.kieu === 'TN' && (c.phuongAn.some(p => !p.trim()) || c.dapAn === null)) return `Câu ${i+1}: nhập đủ 4 phương án và chọn đáp án đúng.`;
      if (c.kieu === 'DS' && c.y.some(y => !y.noiDung.trim() || y.dung === null)) return `Câu ${i+1}: nhập đủ 4 ý và chọn Đúng / Sai cho từng ý.`;
      if ((c.kieu === 'DS' ? c.y.map(y => Number(y.diem)) : [Number(c.diem)]).some(d => !Number.isFinite(d) || d <= 0 || d > 100)) return `Câu ${i+1}: điểm phải lớn hơn 0 và không quá 100.`;
    }
    return total() > 1000 ? 'Tổng điểm không quá 1.000.' : null;
  }
  function review() {
    $('gbReview').innerHTML = `<h3>${esc(data.ten)}</h3><p class="gb-muted">${esc($('gbBai').selectedOptions[0]?.textContent)} · ${esc($('gbBuoi').selectedOptions[0]?.textContent)}</p>
    <p>${data.cau.length} câu · <b>${total().toFixed(2)} điểm</b> · ${data.hanNop ? 'Hạn nộp: '+esc(new Date(data.hanNop).toLocaleString('vi-VN')) : 'Không giới hạn hạn nộp'}</p><p class="gb-pre">${esc(data.huongDan)}</p>` +
    data.cau.map((c,i) => `<article class="gb-question"><b>Câu ${i+1} · ${names[c.kieu]} · ${totalCau(c).toFixed(2)}đ</b><p class="gb-pre">${esc(c.noiDung)}</p>
    ${c.kieu === 'TN' ? c.phuongAn.map((p,j) => `<p class="${c.dapAn === j ? 'gb-correct' : ''}">${'ABCD'[j]}. ${esc(p)} ${c.dapAn === j ? '✓' : ''}</p>`).join('') : ''}
    ${c.kieu === 'DS' ? c.y.map((y,j) => `<p>${'abcd'[j]}) ${esc(y.noiDung)} — <b>${y.dung ? 'Đúng' : 'Sai'}</b> · ${y.diem}đ</p>`).join('') : ''}
    ${c.loiGiai ? `<p class="gb-muted gb-pre">${esc(c.loiGiai)}</p>` : ''}</article>`).join('');
  }
  function go(n) {
    error('');
    if (n > 1 && !info()) { error('Nhập tên bài tập và chọn bài học trước.'); return; }
    if (n === 3) { const e = validate(); if (e) { error(e); return; } review(); }
    step = n;
    root.querySelectorAll('[data-panel]').forEach(e => e.hidden = Number(e.dataset.panel) !== n);
    root.querySelectorAll('[data-step]').forEach(e => { e.classList.toggle('current', Number(e.dataset.step) === n); e.classList.toggle('done', Number(e.dataset.step) < n); e.setAttribute('aria-current', Number(e.dataset.step) === n ? 'step' : 'false'); e.querySelector('span').textContent = Number(e.dataset.step) < n ? '✓' : e.dataset.step; });
    $('gbTruoc').hidden = n === 1; $('gbTiep').hidden = n === 3;
    $('gbGiao').hidden = n !== 3 || locked; $('gbNhap').hidden = locked;
  }
  root.addEventListener('input', (e) => {
    dirty = true;
    const path = e.target.dataset.path;
    if (!path || e.target.type === 'radio') return;
    const c = data.cau.find(c => c.id === e.target.closest('[data-id]').dataset.id);
    const keys = path.split('.'); let obj = c;
    for (const k of keys.slice(0,-1)) obj = obj[k];
    obj[keys.at(-1)] = e.target.type === 'number' ? Number(e.target.value) : e.target.value;
    updateTotal();
  });
  root.addEventListener('change', e => {
    dirty = true;
    if (e.target.id === 'gbBai') renderBuoi(null);
    if (e.target.dataset.path && e.target.type === 'radio') {
      const c = data.cau.find(c => c.id === e.target.closest('[data-id]').dataset.id);
      const path = e.target.dataset.path.split('.'); let obj = c;
      for (const k of path.slice(0,-1)) obj = obj[k];
      obj[path.at(-1)] = e.target.value === 'true' ? true : e.target.value === 'false' ? false : Number(e.target.value);
    }
    const k = e.target.dataset.type;
    if (k) {
      if (!e.target.checked && data.cau.some(c => c.kieu === k) && !confirm('Bỏ phần này sẽ xoá các câu đang soạn trong phần. Tiếp tục?')) { e.target.checked = true; return; }
      if (e.target.checked) { active.add(k); data.cau.push(fresh(k)); }
      else { active.delete(k); data.cau = data.cau.filter(c => c.kieu !== k); }
      render();
    }
  });
  root.addEventListener('click', e => {
    const btn = e.target.closest('button'); if (!btn) return;
    if (btn.dataset.step) go(Number(btn.dataset.step));
    if (btn.dataset.add && !locked) { data.cau.push(fresh(btn.dataset.add)); dirty = true; render(); }
    const article = btn.closest('[data-id]');
    if (article && !locked) {
      const i = data.cau.findIndex(c => c.id === article.dataset.id);
      if (btn.hasAttribute('data-delete')) { data.cau.splice(i,1); dirty = true; render(); }
      if (btn.dataset.move) {
        const same = data.cau.map((c,j) => c.kieu === data.cau[i].kieu ? j : -1).filter(j => j >= 0);
        const j = same[same.indexOf(i) + Number(btn.dataset.move)];
        if (j !== undefined) { [data.cau[i],data.cau[j]] = [data.cau[j],data.cau[i]]; dirty = true; render(); }
      }
    }
  });
  async function save(publish) {
    if (saving || locked) return;
    if (!info()) { go(1); error('Nhập tên bài tập và chọn bài học trước khi lưu.'); return; }
    if (publish) { const e = validate(); if (e) { error(e); return; } }
    saving = true; error(''); $('gbStatus').textContent = 'Đang lưu…';
    root.querySelectorAll('#gbNhap,#gbGiao').forEach(b => b.disabled = true);
    try {
      const res = await fetch('/GiangVien/GiaoBai/Luu', { method: 'POST', headers: {
        'Content-Type': 'application/json', 'RequestVerificationToken': root.querySelector('[name=__RequestVerificationToken]').value
      }, body: JSON.stringify({...data, daGiao: publish}) });
      const result = await res.json().catch(() => ({}));
      if (!res.ok || !result.url) throw new Error(result.loi || 'Không lưu được bài tập. Vui lòng thử lại hoặc đăng nhập lại.');
      dirty = false; location.assign(result.url);
    } catch(e) { error(e.message); }
    finally { saving = false; $('gbStatus').textContent = ''; root.querySelectorAll('#gbNhap,#gbGiao').forEach(b => b.disabled = false); }
  }
  $('gbTiep').addEventListener('click', () => go(Math.min(3, step+1)));
  $('gbTruoc').addEventListener('click', () => go(Math.max(1, step-1)));
  $('gbNhap').addEventListener('click', () => save(false));
  $('gbForm').addEventListener('submit', e => { e.preventDefault(); if (step === 3) save(true); else go(step+1); });
  window.addEventListener('beforeunload', e => { if (dirty && !locked) { e.preventDefault(); e.returnValue = ''; } });
  render(); go(1);
})();
