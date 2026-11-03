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

// Tải lại trang thì luôn hiện từ đầu trang chủ: không giữ vị trí cuộn cũ, bỏ #mục trên địa chỉ.
// Mở liên kết /#monhoc từ trang khác hoặc bấm menu vẫn nhảy tới mục như bình thường.
// Trình duyệt chốt việc khôi phục vị trí trước khi trang chạy mã, nên phải tắt sẵn từ lần mở trước.
// Dùng window.history: biến `history` ở đầu file là lịch sử hỏi đáp của khung chat.
window.history.scrollRestoration = 'manual';
if (performance.getEntriesByType('navigation')[0]?.type === 'reload') {
  if (location.hash) window.history.replaceState(null, '', location.pathname + location.search);
  const veDau = () => window.scrollTo({ top: 0, behavior: 'instant' });
  veDau();
  // Chrome vẫn cuộn lại chỗ cũ ở khung hình đầu sau khi tải xong, nên đưa về đầu thêm lần nữa sau đó
  window.addEventListener('load', () => requestAnimationFrame(() => requestAnimationFrame(veDau)), { once: true });
}

// Lướt / kéo trang thì nền sao trôi nhanh hơn, dừng tay thì chậm dần về tốc độ thường.
// Chỉ tăng tốc phần quay và trôi; nhấp nháy giữ nguyên để không bị chớp loạn.
const stars = document.querySelector('.stars');
if (stars && !matchMedia('(prefers-reduced-motion: reduce)').matches) {
  const CHUYEN_DONG = ['galaxySpin', 'drift1', 'drift2', 'drift3'];
  const TOI_DA = 30;
  let rate = 1, target = 1, frame = 0;
  let lastY = window.scrollY, lastT = performance.now();
  const apply = () => stars.getAnimations({ subtree: true })
    .filter((a) => CHUYEN_DONG.includes(a.animationName))
    .forEach((a) => { a.playbackRate = rate; });
  const step = () => {
    target += (1 - target) * 0.06; // không lướt nữa thì tốc độ mục tiêu tự về 1
    rate += (target - rate) * 0.15;
    if (Math.abs(rate - 1) < 0.01 && target < 1.01) { rate = 1; apply(); frame = 0; return; }
    apply();
    frame = requestAnimationFrame(step);
  };
  // px trong ms → tốc độ: lướt nhanh 2–3 px/ms là gần tối đa
  const boost = (px, ms) => {
    target = Math.min(TOI_DA, Math.max(target, 1 + (px / Math.max(ms, 16)) * 12));
    if (!frame) frame = requestAnimationFrame(step);
  };
  window.addEventListener('scroll', () => {
    const now = performance.now();
    boost(Math.abs(window.scrollY - lastY), now - lastT);
    lastY = window.scrollY; lastT = now;
  }, { passive: true });
  // Kéo chuột / ngón tay mà trang không cuộn (ví dụ đã ở cuối trang) cũng tăng tốc
  let lastP = null;
  window.addEventListener('pointerdown', (e) => { lastP = { x: e.clientX, y: e.clientY, t: performance.now() }; }, { passive: true });
  window.addEventListener('pointermove', (e) => {
    if (!lastP || !e.buttons) return;
    const now = performance.now();
    boost(Math.hypot(e.clientX - lastP.x, e.clientY - lastP.y), now - lastP.t);
    lastP = { x: e.clientX, y: e.clientY, t: now };
  }, { passive: true });
  window.addEventListener('pointerup', () => { lastP = null; }, { passive: true });
}
