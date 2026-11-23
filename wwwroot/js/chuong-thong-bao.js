// Chuông thông báo: hiện số tin chưa đọc, cập nhật mỗi phút khi tab đang mở
(() => {
  const chuong = document.querySelectorAll('[data-tb-dem]');
  if (!chuong.length) return;
  const hien = (n) => chuong.forEach((el) => {
    el.hidden = !(n > 0); el.textContent = n > 99 ? '99+' : String(n);
    el.closest('a')?.setAttribute('aria-label', n > 0 ? `Thông báo, ${n} chưa đọc` : 'Thông báo');
  });
  async function tai() {
    if (document.hidden) return;
    try { const r = await fetch('/ThongBao/Dem', { headers: { Accept: 'application/json' }, credentials: 'same-origin' }); if (r.ok) hien((await r.json()).dem || 0); } catch { /* mạng lỗi: giữ số cũ */ }
  }
  tai(); setInterval(tai, 60000); document.addEventListener('visibilitychange', tai);
})();
