(() => {
  const input = document.getElementById('aiCauHoi');
  const form = document.getElementById('aiForm');
  const list = document.getElementById('aiTinNhan');
  const scroll = document.getElementById('aiScroll');
  const welcome = document.getElementById('aiWelcome');
  const typing = document.getElementById('aiDangTraLoi');
  const sendButton = document.getElementById('aiGui');
  const subject = document.getElementById('aiKhoa');
  const maxLength = input.maxLength;
  const suggestions = [...document.querySelectorAll('#aiGoiY button')];
  let history = [], controller = null, version = 0, busy = false;
  function update() {
    sendButton.disabled = busy || !input.value.trim() || input.value.length > maxLength;
    document.getElementById('aiDem').textContent = `${input.value.length}/${maxLength} ký tự`;
    suggestions.forEach(b => { b.disabled = busy; b.hidden = !!subject.value && b.dataset.mon !== subject.value; });
    list.setAttribute('aria-busy', String(busy));
  }
  function bottom() { scroll.scrollTop = scroll.scrollHeight; }
  function message(role, text, label, source, error = false) {
    const row = document.createElement('article');
    row.className = `ai-message ${role}${error ? ' error' : ''}`;
    row.setAttribute('aria-label', role === 'user' ? 'Bạn' : 'Trợ lý AI');
    if (role !== 'user') {
      const avatar = document.createElement('span'); avatar.className = 'ai-avatar'; avatar.textContent = '✦'; avatar.setAttribute('aria-hidden','true'); row.append(avatar);
    }
    const body = document.createElement('div'); body.className = 'ai-bubble';
    if (label) { const el = document.createElement('p'); el.className = 'ai-label'; el.textContent = label; body.append(el); }
    const p = document.createElement('p'); p.className = 'ai-text'; p.textContent = text; body.append(p);
    if (source) { const el = document.createElement('p'); el.className = 'ai-source'; el.textContent = source; body.append(el); }
    row.append(body); list.append(row); return {row, body};
  }
  async function send(raw, retryRow) {
    const q = String(raw || '').trim();
    if (!q || q.length > maxLength || busy) return;
    const current = version;
    welcome.hidden = true;
    if (retryRow) retryRow.remove(); else message('user', q);
    input.value = ''; busy = true; typing.hidden = false; update(); bottom();
    const requestController = new AbortController();
    controller = requestController;
    const timeout = setTimeout(() => requestController.abort(), 60000);
    try {
      const res = await fetch('/api/tro-ly/hoi', { method:'POST', signal:requestController.signal, headers: {
        'Content-Type':'application/json', 'RequestVerificationToken':form.querySelector('[name=__RequestVerificationToken]').value
      }, body:JSON.stringify({cauHoi:q, khoaId:subject.value || null, lichSu:history.slice(-3)}) });
      if (current !== version) return;
      if (res.status === 429) throw new Error('Bạn hỏi hơi nhanh. Vui lòng đợi khoảng một phút rồi thử lại.');
      if (res.status === 400) throw new Error('Không gửi được câu hỏi. Vui lòng tải lại trang rồi thử lại.');
      if (!res.ok) throw new Error('Trợ lý đang gặp sự cố. Vui lòng thử lại sau.');
      const data = await res.json();
      if (current !== version) return;
      if (!data.noiDung) throw new Error('Chưa nhận được câu trả lời. Bạn vui lòng thử lại.');
      message('assistant', data.noiDung, data.nhan || 'Trợ lý AI', data.nguon || '');
      if (!data.tuChoi) history = [...history, {cauHoi:q, traLoi:data.noiDung}].slice(-3);
    } catch(e) {
      if (current !== version) return;
      const m = message('assistant', e.name === 'AbortError' ? 'Trợ lý trả lời lâu hơn dự kiến. Bạn vui lòng thử lại.' : e.message, 'Chưa gửi được', '', true);
      const retry = document.createElement('button'); retry.type = 'button'; retry.className = 'ai-retry'; retry.textContent = 'Thử lại';
      retry.addEventListener('click', () => send(q, m.row)); m.body.append(retry);
    } finally {
      clearTimeout(timeout);
      if (current === version) { controller = null; busy = false; typing.hidden = true; update(); bottom(); input.focus({preventScroll:true}); }
    }
  }
  function reset() {
    version++; controller?.abort(); controller = null; history = []; busy = false;
    list.replaceChildren(); welcome.hidden = false; typing.hidden = true; input.value = ''; update(); input.focus({preventScroll:true});
  }
  input.addEventListener('input', update);
  input.addEventListener('keydown', e => { if (e.key === 'Enter' && !e.shiftKey && !e.isComposing) { e.preventDefault(); send(input.value); } });
  form.addEventListener('submit', e => { e.preventDefault(); send(input.value); });
  suggestions.forEach(b => b.addEventListener('click', () => send(b.textContent)));
  document.getElementById('aiMoi').addEventListener('click', reset);
  subject.addEventListener('change', reset);
  update();
  if (input.value.trim()) send(input.value); else input.focus({preventScroll:true});
})();
