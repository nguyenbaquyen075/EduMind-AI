// Nút lưu khoá trên ô khoá: nhớ trên trình duyệt này. Khi có tài khoản thật: gọi API lưu theo học sinh
(() => {
  const KEY = 'sd-khoa-da-luu';
  let luu = [];
  try { luu = JSON.parse(localStorage.getItem(KEY) || '[]'); } catch { }
  document.querySelectorAll('[data-luu]').forEach((btn) => {
    const id = btn.dataset.luu;
    const ve = () => btn.setAttribute('aria-pressed', luu.includes(id) ? 'true' : 'false');
    ve();
    btn.addEventListener('click', () => {
      luu = luu.includes(id) ? luu.filter((x) => x !== id) : [...luu, id];
      try { localStorage.setItem(KEY, JSON.stringify(luu)); } catch { }
      ve();
    });
  });
})();
