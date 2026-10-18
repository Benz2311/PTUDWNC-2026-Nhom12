/**
 * Client-side Image Validation aligned with backend ImageValidator.cs:
 * - Max file size: 5 MB (5 * 1024 * 1024 bytes)
 * - Allowed MIME types: image/jpeg, image/jpg, image/png, image/webp, image/avif
 * - Allowed extensions: .jpg, .jpeg, .png, .webp, .avif
 */

export const MAX_IMAGE_FILE_SIZE_BYTES = 5 * 1024 * 1024; // 5 MB

export const ALLOWED_IMAGE_MIME_TYPES = [
  'image/jpeg',
  'image/jpg',
  'image/png',
  'image/webp',
  'image/avif',
] as const;

export const ALLOWED_IMAGE_EXTENSIONS = [
  '.jpg',
  '.jpeg',
  '.png',
  '.webp',
  '.avif',
] as const;

export interface ImageValidationResult {
  isValid: boolean;
  errorMessage?: string;
}

export function validateImageFile(file: File): ImageValidationResult {
  if (!file || file.size <= 0) {
    return {
      isValid: false,
      errorMessage: 'Tệp tải lên không được để trống.',
    };
  }

  if (file.size > MAX_IMAGE_FILE_SIZE_BYTES) {
    return {
      isValid: false,
      errorMessage: `Dung lượng tệp (${(file.size / (1024 * 1024)).toFixed(2)} MB) vượt quá giới hạn tối đa cho phép là 5 MB.`,
    };
  }

  const mime = file.type?.toLowerCase().trim();
  const fileName = file.name?.toLowerCase().trim() || '';
  const hasAllowedExt = ALLOWED_IMAGE_EXTENSIONS.some((ext) => fileName.endsWith(ext));

  const hasAllowedMime = ALLOWED_IMAGE_MIME_TYPES.some((allowedMime) => allowedMime === mime);

  if (!hasAllowedMime && !hasAllowedExt) {
    return {
      isValid: false,
      errorMessage: `Định dạng tệp không được hỗ trợ. Hệ thống chỉ chấp nhận hình ảnh JPG, PNG, WebP hoặc AVIF.`,
    };
  }

  return { isValid: true };
}
