/**
 * Helper chuẩn hóa URL hình ảnh:
 * Khi chạy sau Nginx reverse-proxy (cổng 8088), các URL ảnh lưu cứng trong DB dạng
 * 'http://localhost:9000/culinaryblog/...' sẽ được tự động chuyển thành relative path
 * '/culinaryblog/...' để Nginx proxy trực tiếp vào MinIO mà không gặp lỗi kết nối port 9000.
 */
export function resolveImageUrl(url?: string | null): string | undefined {
  if (!url) return undefined;
  if (url.includes("/culinaryblog/")) {
    return url.replace(/^https?:\/\/[^/]+\/culinaryblog\//, "/culinaryblog/");
  }
  return url;
}
