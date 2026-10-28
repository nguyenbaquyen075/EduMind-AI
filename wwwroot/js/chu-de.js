(() => {
  const dialog = document.getElementById('chuDeDialog');
  if (!dialog) return;
  const id = document.getElementById('chuDeDialogId');
  const ten = document.getElementById('chuDeDialogTen');
  document.addEventListener('click', (event) => {
    const button = event.target.closest('[data-topic-action="edit"]');
    if (!button) return;
    id.value = button.dataset.id;
    ten.value = button.dataset.ten;
    button.closest('details').open = false;
    dialog.showModal();
    ten.focus();
  });
  document.getElementById('chuDeDialogCancel').addEventListener('click', () => dialog.close());
})();
