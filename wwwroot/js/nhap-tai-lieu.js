(() => {
  document.querySelectorAll('[data-nhap-tai-lieu]').forEach(root => {
    const form = root.closest('form');
    const file = root.querySelector('input[type=file]');
    const status = root.querySelector('[data-nhap-status]');
    const warnings = root.querySelector('[data-nhap-canh-bao]');
    const apply = root.querySelector('[data-ap-dung-file]');
    const content = form.querySelector('[name=noiDung]');
    const title = form.querySelector('[name=tenBai]');
    let result;
    let controller;
    const show = text => { status.hidden = false; status.textContent = text; };
    const fill = () => {
      if (!result) return;
      content.value = result.noiDung;
      if (title && !title.value.trim()) title.value = result.tenBai;
      content.dispatchEvent(new Event('input', { bubbles: true }));
      apply.hidden = true;
      show('Đã điền nội dung và đề mục từ file. Anh kiểm tra lại, nhập số bài rồi chọn “Chia theo đề mục văn bản” để xem thử ngay, hoặc dùng AI.');
    };
    apply.addEventListener('click', fill);
    file.addEventListener('change', async () => {
      controller?.abort();
      result = null; apply.hidden = true; warnings.hidden = true; warnings.replaceChildren();
      const selected = file.files[0];
      if (!selected) { status.hidden = true; return; }
      if (!/\.(docx|pptx)$/i.test(selected.name) || selected.size > 20 * 1024 * 1024) {
        show('Anh chọn file .docx hoặc .pptx, tối đa 20 MB nhé.'); return;
      }
      controller = new AbortController();
      const current = controller;
      const data = new FormData(); data.append('taiLieu', selected);
      show('Đang đọc tài liệu và nhận diện đề mục…');
      const buttons = [...form.querySelectorAll('button[type=submit]')];
      buttons.forEach(b => b.disabled = true);
      try {
        const response = await fetch('/GiangVien/NoiDung/NhapTaiLieu', {
          method: 'POST', body: data, signal: current.signal,
          headers: { RequestVerificationToken: form.querySelector('[name=__RequestVerificationToken]').value }
        });
        const json = await response.json();
        if (!response.ok) throw new Error(json.loi || 'Không đọc được tài liệu. Anh thử lại nhé.');
        result = json;
        for (const message of json.canhBao || []) {
          const li = document.createElement('li'); li.textContent = message; warnings.append(li);
        }
        warnings.hidden = !warnings.children.length;
        if (content.value.trim()) {
          apply.hidden = false;
          show('Đã đọc xong file. Ô nội dung đang có chữ; bấm “Điền nội dung từ file” nếu anh muốn thay bằng tài liệu vừa chọn.');
        } else fill();
      } catch (error) {
        if (error.name !== 'AbortError') show(error instanceof SyntaxError ? 'Phiên đăng nhập đã hết hoặc file không đọc được. Anh tải lại trang rồi thử lại nhé.' : error.message);
      } finally {
        if (current === controller) buttons.forEach(b => b.disabled = false);
      }
    });
  });
})();
