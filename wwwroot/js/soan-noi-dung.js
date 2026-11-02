(() => {
  document.querySelectorAll('form[data-soan-noi-dung]').forEach((form) => {
    const status = form.querySelector('[data-soan-status]');
    const aiButton = form.querySelector('button[type="submit"]:not([name="chiaTheoVanBan"])');
    let pending = null;
    let timer;
    const reset = () => {
      clearInterval(timer);
      pending = null;
      aiButton.disabled = false;
      form.removeAttribute('aria-busy');
      status.hidden = true;
    };
    window.addEventListener('pageshow', reset);
    form.addEventListener('submit', (event) => {
      if (event.defaultPrevented) return;
      const isText = event.submitter?.name === 'chiaTheoVanBan' && event.submitter.value === 'true';
      // A new text submission replaces the pending AI navigation in the browser.
      // Keep its submit button enabled so its name/value reaches the server.
      if (pending === 'text' || (pending === 'ai' && !isText)) {
        event.preventDefault();
        return;
      }
      clearInterval(timer);
      pending = isText ? 'text' : 'ai';
      aiButton.disabled = true;
      form.setAttribute('aria-busy', 'true');
      status.hidden = false;
      if (isText) {
        status.textContent = 'Đang chia theo đề mục…';
        return;
      }
      const started = Date.now();
      const update = () => {
        const seconds = Math.floor((Date.now() - started) / 1000);
        status.textContent = `AI đang chia nội dung… ${seconds} giây. Có thể bấm “Chia theo đề mục” để làm ngay.`;
      };
      update();
      timer = setInterval(update, 1000);
    });
  });
})();
