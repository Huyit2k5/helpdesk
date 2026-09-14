/**
 * In ra với khổ giấy @page cụ thể, áp dụng ĐỘNG bằng cách tiêm 1 thẻ <style> vào
 * <head> ngay trước khi gọi window.print(), rồi gỡ bỏ ngay sau đó.
 *
 * Lý do cần làm động thay vì khai báo @page tĩnh trong file .scss của từng component:
 * Chromium không hỗ trợ CSS Named Pages (`@page <tên>` + `page: <tên>`), nên khi 2
 * tính năng in khác nhau (VD: Tem QR 70mm x 40mm và Biên Bản bàn giao A4) cùng có mặt
 * trên một trang, 2 khối "@page {}" không tên sẽ đè lẫn nhau tùy thứ tự Angular gộp
 * CSS - không cách nào tách biệt bằng CSS thuần. Tiêm/gỡ động đảm bảo tại đúng thời
 * điểm gọi window.print() chỉ có duy nhất 1 quy tắc @page tồn tại trên trang.
 */
export function printWithPageSize(pageCss: string): void {
  const styleEl = document.createElement('style');
  styleEl.setAttribute('data-dynamic-page-size', 'true');
  styleEl.textContent = `@media print { @page { ${pageCss} } }`;
  document.head.appendChild(styleEl);

  const cleanup = () => {
    styleEl.remove();
    window.removeEventListener('afterprint', cleanup);
  };
  // 'afterprint' bắn ra ngay sau khi hộp thoại in đóng lại (kể cả khi người dùng bấm Hủy),
  // là thời điểm an toàn để dọn dẹp. Thêm timeout dự phòng cho các trình duyệt cũ không
  // hỗ trợ sự kiện này một cách nhất quán.
  window.addEventListener('afterprint', cleanup);
  setTimeout(cleanup, 5000);

  window.print();
}
