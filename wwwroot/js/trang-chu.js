// Gọi API trợ lý: POST /api/tro-ly/hoi (TroLyController)
// Gửi kèm 3 lượt hỏi đáp gần nhất để server hiểu câu hỏi nối tiếp ("còn đợt 2 thì sao?")
const history = [];
async function answerFor(q) {
  const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
  try {
    const res = await fetch('/api/tro-ly/hoi', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
      body: JSON.stringify({ cauHoi: q, khoaId: null, lichSu: history.slice(-3) })
    });
    if (res.status === 429) return { label: 'Tạm dừng', text: 'Bạn hỏi hơi nhanh, vui lòng đợi một chút rồi hỏi tiếp.', mark: '', source: '' };
    if (!res.ok) throw new Error(res.status);
    const d = await res.json();
    if (!d.tuChoi) history.push({ cauHoi: q, traLoi: d.noiDung });
    return { label: d.nhan, text: d.noiDung, mark: d.danhDau, source: d.nguon };
  } catch (e) {
    return { label: 'Lỗi kết nối', text: 'Không gửi được câu hỏi. Vui lòng thử lại.', mark: '', source: '' };
  }
}

const intro = document.getElementById('heroIntro');
const panel = document.getElementById('chatPanel');
const dim = document.getElementById('chatDim');
const list = document.getElementById('msgList');
const typing = document.getElementById('typing');
let busy = false;

function addUser(text) {
  const el = document.getElementById('tplUser').content.firstElementChild.cloneNode(true);
  el.textContent = text;
  list.insertBefore(el, typing);
}
function addAi(a) {
  const el = document.getElementById('tplAi').content.firstElementChild.cloneNode(true);
  for (const k of ['label', 'text', 'mark', 'source']) el.querySelector('[data-f="' + k + '"]').textContent = a[k];
  el.querySelector('[data-f="source"]').hidden = !a.source;
  list.insertBefore(el, typing);
}
function scrollDown() { list.scrollTop = list.scrollHeight; }

function send(raw) {
  const q = (raw || '').trim();
  if (!q || busy) return;
  intro.hidden = true; intro.classList.add('seen');
  panel.hidden = false; dim.hidden = false;
  document.querySelectorAll('.js-ask input').forEach((i) => { i.value = ''; });
  addUser(q);
  busy = true; typing.hidden = false; scrollDown();
  answerFor(q).then((a) => {
    typing.hidden = true; addAi(a); busy = false; scrollDown();
    document.getElementById('chat-ask').focus();
  });
}

document.querySelectorAll('.js-ask').forEach((f) => f.addEventListener('submit', (e) => {
  e.preventDefault(); send(f.querySelector('input').value);
}));
document.querySelectorAll('[data-q]').forEach((b) => b.addEventListener('click', () => send(b.dataset.q)));
document.getElementById('chatClose').addEventListener('click', () => {
  panel.hidden = true; dim.hidden = true; intro.hidden = false;
});

// Header hiện nền khi bắt đầu lướt trang
const head = document.getElementById('siteHead');
const onScroll = () => head.classList.toggle('scrolled', window.scrollY > 8);
window.addEventListener('scroll', onScroll, { passive: true });
onScroll();

// Đo chiều cao header (thay đổi khi menu xuống dòng trên màn hình hẹp) để bìa luồn đúng bằng chừng đó
const setHeadH = () => document.documentElement.style.setProperty('--head-h', head.offsetHeight + 'px');
new ResizeObserver(setHeadH).observe(head);
setHeadH();
