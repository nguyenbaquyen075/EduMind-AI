(() => {
  document.querySelectorAll('[data-nhap-tai-lieu]').forEach(root => {
    const form = root.closest('form');
    const file = root.querySelector('input[type=file]');
    const status = root.querySelector('[data-nhap-status]');
    const warnings = root.querySelector('[data-nhap-canh-bao]');
    const apply = root.querySelector('[data-ap-dung-file]');
    const content = form.querySelector('[name=noiDung]');
    const title = form.querySelector('[name=tenBai]');
    const topic = form.querySelector('[name=tenChuDe]');
    let result;
    let controller;
    const show = text => { status.hidden = false; status.textContent = text; };
    const fill = () => {
      if (!result) return;
      content.value = result.noiDung;
      // Tài liệu nhiều bài (Bài 1, Bài 2…): tiêu đề file là tên chủ đề, không phải tên bài
      const soBai = (result.noiDung.match(/^\s*(?:#{1,6}\s*)?bài\s+\d+/gimu) || []).length;
      if (soBai >= 2) {
        if (topic && !topic.value.trim()) topic.value = result.tenBai.replace(/^\s*(?:chủ\s*đề|chương|chuyên\s*đề)\s*[0-9IVXLC]*\s*[.:\-–—]?\s*/iu, '');
      } else if (title && !title.value.trim()) title.value = result.tenBai;
      content.dispatchEvent(new Event('input', { bubbles: true }));
      apply.hidden = true;
      show('Đã điền nội dung từ file. Kiểm tra lại rồi chọn cách chia.');
    };
    apply.addEventListener('click', fill);
    file.addEventListener('change', async () => {
      controller?.abort();
      result = null; apply.hidden = true; warnings.hidden = true; warnings.replaceChildren();
      const selected = file.files[0];
      if (!selected) { status.hidden = true; return; }
      if (!/\.(docx|pptx)$/i.test(selected.name) || selected.size > 20 * 1024 * 1024) {
        show('Chọn file .docx hoặc .pptx, tối đa 20 MB.'); return;
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
        if (!response.ok) throw new Error(json.loi || 'Không đọc được tài liệu. Thử lại sau.');
        result = json;
        for (const message of json.canhBao || []) {
          const li = document.createElement('li'); li.textContent = message; warnings.append(li);
        }
        warnings.hidden = !warnings.children.length;
        if (content.value.trim()) {
          apply.hidden = false;
          show('Đã đọc xong file. Bấm “Điền nội dung từ file” để thay nội dung đang có.');
        } else fill();
      } catch (error) {
        if (error.name !== 'AbortError') show(error instanceof SyntaxError ? 'Phiên đăng nhập đã hết hoặc file không đọc được. Tải lại trang rồi thử lại.' : error.message);
      } finally {
        if (current === controller) buttons.forEach(b => b.disabled = false);
      }
    });
  });
})();
