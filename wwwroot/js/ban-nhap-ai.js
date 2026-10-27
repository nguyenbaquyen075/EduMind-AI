(() => {
  document.querySelectorAll('#banNhapAi, #formMuc').forEach((form) => {

  const initialized = new WeakSet();
  const fit = (field) => {
    if (!field.getClientRects().length) return;
    const styles = getComputedStyle(field);
    const borders = parseFloat(styles.borderTopWidth) + parseFloat(styles.borderBottomWidth);
    field.style.height = 'auto';
    field.style.height = `${field.scrollHeight + borders}px`;
  };

  const resizeAll = () => form.querySelectorAll('textarea').forEach((field) => {
    if (!initialized.has(field)) {
      initialized.add(field);
      field.rows = field.name === 'Ten' ? 2 : 3;
      field.style.overflowY = 'hidden';
      field.style.resize = 'none';
      field.style.maxHeight = 'none';
      field.addEventListener('input', () => fit(field));
    }
    fit(field);
  });
  let frame;
  const schedule = () => {
    cancelAnimationFrame(frame);
    frame = requestAnimationFrame(resizeAll);
  };
  resizeAll();
  new MutationObserver(schedule).observe(form, { childList: true, subtree: true });
  form.addEventListener('toggle', schedule, true);

  // Recalculate wrapping when the page width or loaded fonts change.
  let width = form.clientWidth;
  new ResizeObserver(() => {
    if (form.clientWidth === width) return;
    width = form.clientWidth;
    schedule();
  }).observe(form);
  document.fonts.ready.then(schedule);
  });
})();
