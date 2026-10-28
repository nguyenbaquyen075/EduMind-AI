(() => {
  const form = document.getElementById('kiemTraForm');
  const label = document.getElementById('ktConLai');
  if (!form || !label) return;
  const deadline = Date.parse(form.dataset.hetGio);
  if (!Number.isFinite(deadline)) return;
  let submitted = false;
  let timer;
  form.addEventListener('submit', () => { submitted = true; clearInterval(timer); });
  const update = () => {
    const seconds = Math.max(0, Math.ceil((deadline - Date.now()) / 1000));
    label.textContent = `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, '0')}`;
    if (seconds === 0 && !submitted) form.requestSubmit();
  };
  timer = setInterval(update, 1000);
  update();
})();
