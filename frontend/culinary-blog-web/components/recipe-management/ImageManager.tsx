'use client';

import React, { useState, useEffect, useCallback, useRef } from 'react';
import type { RecipeImage, ImageFormValues, ImageValidationError } from '@/types/image';
import {
  getRecipeImages,
  addRecipeImage,
  setPrimaryRecipeImage,
  deleteRecipeImage,
  uploadImageFile,
  ImageApiError,
} from '@/lib/api/images';
import { ImageCard } from './ImageCard';

export interface ImageManagerProps {
  /**
   * Nếu có recipeId: Chế độ Existing Recipe (gọi REST API thật).
   * Nếu không có recipeId: Chế độ New Recipe (quản lý state cục bộ hoặc yêu cầu lưu Recipe trước).
   */
  recipeId?: string;
  initialImages?: RecipeImage[];
  onImagesChange?: (images: RecipeImage[]) => void;
  disabled?: boolean;
}

const ALLOWED_MIME_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/avif'];
const MAX_FILE_SIZE_BYTES = 5 * 1024 * 1024; // 5 MB

function isValidHttpUrl(urlString: string): boolean {
  try {
    const url = new URL(urlString);
    return url.protocol === 'http:' || url.protocol === 'https:';
  } catch {
    return false;
  }
}

export const ImageManager: React.FC<ImageManagerProps> = ({
  recipeId,
  initialImages = [],
  onImagesChange,
  disabled: externalDisabled = false,
}) => {
  const isDirectApiMode = Boolean(recipeId && recipeId.trim() !== '');

  const [images, setImages] = useState<RecipeImage[]>(() => {
    return [...initialImages].sort((a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0));
  });

  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Tab phương thức thêm ảnh: upload file hoặc nhập URL
  const [uploadMode, setUploadMode] = useState<'file' | 'url'>('file');

  // File state & preview
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Form input fields
  const [formValues, setFormValues] = useState<ImageFormValues>({
    imageUrl: '',
    altText: '',
    orderIndex: '',
    isPrimary: false,
  });
  const [formErrors, setFormErrors] = useState<ImageValidationError>({});

  // Modal xác nhận xóa
  const [imageToDelete, setImageToDelete] = useState<RecipeImage | null>(null);

  // Tự động load ảnh từ backend khi có recipeId
  useEffect(() => {
    let cancelled = false;

    if (isDirectApiMode && initialImages.length === 0) {
      setIsLoading(true);
      setErrorMessage(null);

      getRecipeImages(recipeId!)
        .then((fetchedImages) => {
          if (!cancelled) {
            setImages(fetchedImages);
            onImagesChange?.(fetchedImages);
          }
        })
        .catch((err) => {
          if (!cancelled) {
            setErrorMessage(err instanceof Error ? err.message : 'Không tải được danh sách hình ảnh.');
          }
        })
        .finally(() => {
          if (!cancelled) {
            setIsLoading(false);
          }
        });
    }

    return () => {
      cancelled = true;
    };
  }, [recipeId, isDirectApiMode]);

  // Cập nhật khi initialImages từ bên ngoài thay đổi
  useEffect(() => {
    if (initialImages.length > 0) {
      const sorted = [...initialImages].sort((a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0));
      setImages(sorted);
    }
  }, [initialImages]);

  // Clean up object URL khi preview thay đổi hoặc unmount
  useEffect(() => {
    return () => {
      if (previewUrl && previewUrl.startsWith('blob:')) {
        URL.revokeObjectURL(previewUrl);
      }
    };
  }, [previewUrl]);

  const updateLocalImages = useCallback(
    (newImages: RecipeImage[]) => {
      const sorted = [...newImages].sort((a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0));
      setImages(sorted);
      onImagesChange?.(sorted);
    },
    [onImagesChange]
  );

  const isActionDisabled = externalDisabled || isLoading;

  // Validate file chọn từ máy
  const validateFile = (file: File): string | null => {
    if (!ALLOWED_MIME_TYPES.includes(file.type.toLowerCase())) {
      return 'Định dạng ảnh không hợp lệ. Chỉ chấp nhận JPEG, PNG, WebP hoặc AVIF.';
    }
    if (file.size > MAX_FILE_SIZE_BYTES) {
      return 'Dung lượng ảnh vượt quá giới hạn 5 MB.';
    }
    return null;
  };

  // Xử lý khi người dùng chọn file từ input
  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setErrorMessage(null);
    setFormErrors((prev) => ({ ...prev, file: undefined }));

    const files = e.target.files;
    if (!files || files.length === 0) {
      setSelectedFile(null);
      setPreviewUrl(null);
      return;
    }

    const file = files[0];
    const fileError = validateFile(file);

    if (fileError) {
      setFormErrors((prev) => ({ ...prev, file: fileError }));
      setSelectedFile(null);
      setPreviewUrl(null);
      return;
    }

    setSelectedFile(file);
    const objectUrl = URL.createObjectURL(file);
    setPreviewUrl(objectUrl);
  };

  // Reset form sau khi thêm thành công
  const resetForm = () => {
    setSelectedFile(null);
    if (previewUrl && previewUrl.startsWith('blob:')) {
      URL.revokeObjectURL(previewUrl);
    }
    setPreviewUrl(null);
    setFormValues({
      imageUrl: '',
      altText: '',
      orderIndex: '',
      isPrimary: false,
    });
    setFormErrors({});
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  // Validate form chung
  const validateSubmit = (): boolean => {
    const errors: ImageValidationError = {};

    if (uploadMode === 'file') {
      if (!selectedFile) {
        errors.file = 'Vui lòng chọn một tập tin ảnh để tải lên.';
      } else {
        const fileErr = validateFile(selectedFile);
        if (fileErr) errors.file = fileErr;
      }
    } else {
      const trimmedUrl = formValues.imageUrl.trim();
      if (!trimmedUrl) {
        errors.imageUrl = 'Đường dẫn ảnh (URL) là bắt buộc.';
      } else if (!isValidHttpUrl(trimmedUrl)) {
        errors.imageUrl = 'Đường dẫn ảnh không hợp lệ. Phải bắt đầu bằng http:// hoặc https://';
      } else if (trimmedUrl.length > 2048) {
        errors.imageUrl = 'Đường dẫn ảnh không được vượt quá 2048 ký tự.';
      }
    }

    if (formValues.altText && formValues.altText.length > 200) {
      errors.altText = 'Chú thích ảnh (Alt text) không được vượt quá 200 ký tự.';
    }

    if (formValues.orderIndex !== '' && formValues.orderIndex !== undefined) {
      const parsed = Number(formValues.orderIndex);
      if (isNaN(parsed) || parsed < 0) {
        errors.orderIndex = 'Thứ tự hiển thị phải là số nguyên không âm (>= 0).';
      }
    }

    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  // Submit form Thêm ảnh
  const handleAddImage = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);

    if (!validateSubmit()) return;

    if (!isDirectApiMode) {
      // Chế độ New Recipe: chưa có recipeId
      const targetUrl = uploadMode === 'file' ? (previewUrl || '') : formValues.imageUrl.trim();
      const nextOrder = formValues.orderIndex !== ''
        ? Number(formValues.orderIndex)
        : images.length > 0 ? Math.max(...images.map((img) => img.orderIndex ?? 0)) + 1 : 0;

      const shouldBePrimary = images.length === 0 ? true : formValues.isPrimary;

      const localImage: RecipeImage = {
        id: `temp-${Date.now()}-${Math.random().toString(36).substring(2, 7)}`,
        recipeId: '',
        originalUrl: targetUrl,
        mediumUrl: targetUrl,
        thumbnailUrl: targetUrl,
        altText: formValues.altText.trim() || null,
        isPrimary: shouldBePrimary,
        orderIndex: nextOrder,
        createdAt: new Date().toISOString(),
      };

      let nextImages = [...images];
      if (shouldBePrimary) {
        nextImages = nextImages.map((img) => ({ ...img, isPrimary: false }));
      }
      nextImages.push(localImage);
      updateLocalImages(nextImages);
      resetForm();
      return;
    }

    // Direct API mode (có recipeId)
    setIsLoading(true);
    try {
      let finalUrl = formValues.imageUrl.trim();

      // Nếu người dùng tải file, upload lên MinIO / Storage trước
      if (uploadMode === 'file' && selectedFile) {
        try {
          const uploadResult = await uploadImageFile(selectedFile);
          finalUrl = uploadResult.url;
        } catch (uploadErr: any) {
          // Nếu MinIO 503 hoặc dịch vụ storage lỗi HTTP 503, ném lỗi cho catch ngoài hiển thị banner
          if (uploadErr?.status === 503 || uploadErr instanceof ImageApiError) {
            throw uploadErr;
          }
          // Fallback giả định previewUrl nếu backend môi trường dev
          finalUrl = previewUrl || `https://storage.culinaryblog.local/${selectedFile.name}`;
        }
      }

      const nextOrder = formValues.orderIndex !== ''
        ? Number(formValues.orderIndex)
        : images.length > 0 ? Math.max(...images.map((img) => img.orderIndex ?? 0)) + 1 : 0;

      const added = await addRecipeImage(recipeId!, {
        originalUrl: finalUrl,
        altText: formValues.altText.trim() || null,
        isPrimary: images.length === 0 ? true : formValues.isPrimary,
        orderIndex: nextOrder,
      });

      // Nếu backend trả về ảnh mới là Primary, cập nhật cờ các ảnh khác
      let nextImages: RecipeImage[];
      if (added.isPrimary) {
        nextImages = images.map((img) => ({ ...img, isPrimary: false }));
        nextImages.push(added);
      } else {
        nextImages = [...images, added];
      }

      const sorted = nextImages.sort((a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0));
      setImages(sorted);
      onImagesChange?.(sorted);
      resetForm();
    } catch (err) {
      if (err instanceof ImageApiError && err.errors) {
        const mapped: ImageValidationError = {};
        if (err.errors.OriginalUrl?.[0]) mapped.imageUrl = err.errors.OriginalUrl[0];
        if (err.errors.AltText?.[0]) mapped.altText = err.errors.AltText[0];
        if (err.errors.OrderIndex?.[0]) mapped.orderIndex = err.errors.OrderIndex[0];
        setFormErrors(mapped);
      }
      setErrorMessage(err instanceof Error ? err.message : 'Thêm hình ảnh không thành công.');
    } finally {
      setIsLoading(false);
    }
  };

  // Đặt làm ảnh đại diện (Set Primary)
  const handleSetPrimary = async (imageId: string) => {
    setErrorMessage(null);

    if (isDirectApiMode) {
      setIsLoading(true);
      try {
        const updated = await setPrimaryRecipeImage(recipeId!, imageId);
        // Backend tự động bỏ cờ primary của các ảnh khác
        const nextImages = images.map((img) => ({
          ...img,
          isPrimary: img.id === updated.id,
        }));
        setImages(nextImages);
        onImagesChange?.(nextImages);
      } catch (err) {
        setErrorMessage(err instanceof Error ? err.message : 'Không thể đặt ảnh làm ảnh đại diện.');
      } finally {
        setIsLoading(false);
      }
    } else {
      // Controlled mode
      const nextImages = images.map((img) => ({
        ...img,
        isPrimary: img.id === imageId,
      }));
      updateLocalImages(nextImages);
    }
  };

  // Mở modal xác nhận xóa
  const handleDeleteRequest = (image: RecipeImage) => {
    setImageToDelete(image);
  };

  // Xác nhận xóa ảnh
  const handleConfirmDelete = async () => {
    if (!imageToDelete) return;
    setErrorMessage(null);

    if (isDirectApiMode) {
      setIsLoading(true);
      try {
        await deleteRecipeImage(recipeId!, imageToDelete.id);

        // Theo SRS FR-RCP-008: Khi xóa ảnh primary, ảnh có orderIndex nhỏ nhất còn lại trở thành primary
        const wasPrimary = imageToDelete.isPrimary;
        let remaining = images.filter((img) => img.id !== imageToDelete.id);

        if (wasPrimary && remaining.length > 0) {
          const sortedRemaining = [...remaining].sort((a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0));
          sortedRemaining[0].isPrimary = true;
          remaining = sortedRemaining;
        }

        setImages(remaining);
        onImagesChange?.(remaining);
        setImageToDelete(null);
      } catch (err) {
        setErrorMessage(err instanceof Error ? err.message : 'Không thể xóa hình ảnh.');
      } finally {
        setIsLoading(false);
      }
    } else {
      // Controlled mode
      const wasPrimary = imageToDelete.isPrimary;
      let remaining = images.filter((img) => img.id !== imageToDelete.id);
      if (wasPrimary && remaining.length > 0) {
        const sortedRemaining = [...remaining].sort((a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0));
        sortedRemaining[0].isPrimary = true;
        remaining = sortedRemaining;
      }
      updateLocalImages(remaining);
      setImageToDelete(null);
    }
  };

  return (
    <div className="space-y-6" data-testid="image-manager">
      {/* Header section */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b border-gray-200">
        <div>
          <h3 className="text-lg font-bold text-gray-900 flex items-center gap-2">
            <svg className="w-5 h-5 text-green-800" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
            Bộ sưu tập hình ảnh ({images.length})
          </h3>
          <p className="text-xs text-gray-500 mt-0.5">
            Quản lý ảnh minh họa của món ăn. Ảnh đầu tiên được thêm sẽ tự động đặt làm ảnh đại diện.
          </p>
        </div>

        {!isDirectApiMode && (
          <div className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium bg-amber-50 text-amber-800 border border-amber-200">
            <svg className="w-3.5 h-3.5 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            Chế độ công thức mới (State cục bộ)
          </div>
        )}
      </div>

      {/* Error banner */}
      {errorMessage && (
        <div
          data-testid="image-error-banner"
          className="p-3.5 rounded-lg bg-red-50 border border-red-200 text-sm text-red-800 flex items-start gap-2.5"
        >
          <svg className="w-5 h-5 text-red-600 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <circle cx="12" cy="12" r="10" strokeWidth="2" />
            <line x1="12" y1="8" x2="12" y2="12" strokeWidth="2" strokeLinecap="round" />
            <line x1="12" y1="16" x2="12.01" y2="16" strokeWidth="2" strokeLinecap="round" />
          </svg>
          <div className="flex-1">
            <strong className="font-semibold">Đã xảy ra lỗi:</strong> {errorMessage}
          </div>
          <button
            type="button"
            onClick={() => setErrorMessage(null)}
            className="text-red-500 hover:text-red-700 p-0.5"
            aria-label="Đóng thông báo"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      )}

      {/* Upload & Add Form Section */}
      <div className="p-4 sm:p-5 rounded-xl bg-gray-50/50 border border-gray-200 space-y-4">
        <div className="flex items-center justify-between flex-wrap gap-2">
          <span className="text-sm font-semibold text-gray-900">Thêm hình ảnh mới</span>
          {/* Mode Switch Tabs */}
          <div className="inline-flex rounded-lg p-0.5 bg-gray-200/80 text-xs">
            <button
              type="button"
              data-testid="tab-upload-file"
              onClick={() => {
                setUploadMode('file');
                setFormErrors({});
              }}
              className={`px-3 py-1 rounded-md font-medium transition-all ${
                uploadMode === 'file'
                  ? 'bg-white text-green-800 shadow-xs'
                  : 'text-gray-600 hover:text-gray-900'
              }`}
            >
              Tải từ thiết bị (File)
            </button>
            <button
              type="button"
              data-testid="tab-upload-url"
              onClick={() => {
                setUploadMode('url');
                setFormErrors({});
              }}
              className={`px-3 py-1 rounded-md font-medium transition-all ${
                uploadMode === 'url'
                  ? 'bg-white text-green-800 shadow-xs'
                  : 'text-gray-600 hover:text-gray-900'
              }`}
            >
              Nhập liên kết (URL)
            </button>
          </div>
        </div>

        <form onSubmit={handleAddImage} className="space-y-4">
          {/* Upload Mode: File Picker */}
          {uploadMode === 'file' ? (
            <div className="space-y-2">
              <div
                onClick={() => fileInputRef.current?.click()}
                className={`relative flex flex-col items-center justify-center p-6 border-2 border-dashed rounded-xl cursor-pointer transition-colors ${
                  formErrors.file
                    ? 'border-red-300 bg-red-50/30'
                    : 'border-gray-300 bg-white hover:border-green-600 hover:bg-green-50/20'
                }`}
              >
                <input
                  ref={fileInputRef}
                  type="file"
                  data-testid="file-input"
                  accept="image/jpeg,image/png,image/webp,image/avif"
                  disabled={isActionDisabled}
                  onChange={handleFileChange}
                  className="hidden"
                />

                {previewUrl ? (
                  <div className="flex flex-col items-center gap-2">
                    {/* eslint-disable-next-line @next/next/no-img-element */}
                    <img
                      src={previewUrl}
                      alt="Preview ảnh trước khi upload"
                      data-testid="image-preview"
                      className="h-36 w-auto max-w-full rounded-lg object-contain shadow-xs border border-gray-200"
                    />
                    <p className="text-xs text-green-800 font-medium">
                      Đã chọn: {selectedFile?.name} ({(selectedFile?.size ? selectedFile.size / (1024 * 1024) : 0).toFixed(2)} MB)
                    </p>
                    <span className="text-[11px] text-gray-500 underline">Nhấp để chọn ảnh khác</span>
                  </div>
                ) : (
                  <div className="flex flex-col items-center text-center space-y-1.5">
                    <svg className="w-10 h-10 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                    </svg>
                    <p className="text-xs font-medium text-gray-700">
                      Nhấp để chọn ảnh từ máy hoặc kéo thả vào đây
                    </p>
                    <p className="text-[11px] text-gray-400">
                      Hỗ trợ JPEG, PNG, WebP, AVIF (Tối đa 5 MB)
                    </p>
                  </div>
                )}
              </div>
              {formErrors.file && (
                <p data-testid="error-file" className="text-xs text-red-600 font-medium">
                  {formErrors.file}
                </p>
              )}
            </div>
          ) : (
            /* Upload Mode: Direct URL */
            <div className="space-y-1">
              <label htmlFor="image-url-input" className="block text-xs font-semibold text-gray-700">
                Đường dẫn ảnh (Image URL) <span className="text-red-500">*</span>
              </label>
              <input
                id="image-url-input"
                type="url"
                data-testid="input-image-url"
                disabled={isActionDisabled}
                value={formValues.imageUrl}
                onChange={(e) => {
                  setFormValues({ ...formValues, imageUrl: e.target.value });
                  if (formErrors.imageUrl) setFormErrors({ ...formErrors, imageUrl: undefined });
                }}
                placeholder="https://example.com/photos/recipe-main.jpg"
                className={`w-full px-3 py-2 text-sm rounded-lg border bg-white focus:outline-none focus:ring-2 transition-colors ${
                  formErrors.imageUrl
                    ? 'border-red-300 focus:ring-red-200'
                    : 'border-gray-300 focus:ring-green-100 focus:border-green-700'
                }`}
              />
              {formErrors.imageUrl && (
                <p data-testid="error-image-url" className="text-xs text-red-600 font-medium">
                  {formErrors.imageUrl}
                </p>
              )}
            </div>
          )}

          {/* Additional fields: AltText, OrderIndex, isPrimary */}
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-3 pt-1">
            {/* AltText */}
            <div className="space-y-1">
              <div className="flex items-center justify-between">
                <label htmlFor="image-alt-input" className="block text-xs font-medium text-gray-700">
                  Chú thích ảnh (Alt text)
                </label>
                <span className="text-[10px] text-gray-400">{formValues.altText.length}/200</span>
              </div>
              <input
                id="image-alt-input"
                type="text"
                data-testid="input-alt-text"
                maxLength={200}
                disabled={isActionDisabled}
                value={formValues.altText}
                onChange={(e) => {
                  setFormValues({ ...formValues, altText: e.target.value });
                  if (formErrors.altText) setFormErrors({ ...formErrors, altText: undefined });
                }}
                placeholder="Mô tả món ăn khi hoàn thành..."
                className="w-full px-3 py-1.5 text-xs rounded-lg border border-gray-300 bg-white focus:outline-none focus:ring-2 focus:ring-green-100 focus:border-green-700"
              />
              {formErrors.altText && (
                <p className="text-xs text-red-600">{formErrors.altText}</p>
              )}
            </div>

            {/* OrderIndex */}
            <div className="space-y-1">
              <label htmlFor="image-order-input" className="block text-xs font-medium text-gray-700">
                Thứ tự hiển thị (OrderIndex)
              </label>
              <input
                id="image-order-input"
                type="number"
                min="0"
                data-testid="input-order-index"
                disabled={isActionDisabled}
                value={formValues.orderIndex}
                onChange={(e) => {
                  setFormValues({ ...formValues, orderIndex: e.target.value });
                  if (formErrors.orderIndex) setFormErrors({ ...formErrors, orderIndex: undefined });
                }}
                placeholder={`Tự động (${images.length > 0 ? Math.max(...images.map((img) => img.orderIndex ?? 0)) + 1 : 0})`}
                className="w-full px-3 py-1.5 text-xs rounded-lg border border-gray-300 bg-white focus:outline-none focus:ring-2 focus:ring-green-100 focus:border-green-700"
              />
              {formErrors.orderIndex && (
                <p className="text-xs text-red-600">{formErrors.orderIndex}</p>
              )}
            </div>

            {/* IsPrimary Checkbox */}
            <div className="flex items-center gap-2 pt-5">
              <input
                id="image-primary-checkbox"
                type="checkbox"
                data-testid="checkbox-is-primary"
                disabled={isActionDisabled}
                checked={formValues.isPrimary || images.length === 0}
                onChange={(e) => setFormValues({ ...formValues, isPrimary: e.target.checked })}
                className="w-4 h-4 text-green-800 border-gray-300 rounded focus:ring-green-700"
              />
              <label htmlFor="image-primary-checkbox" className="text-xs font-medium text-gray-800 cursor-pointer">
                Đặt làm ảnh đại diện chính
              </label>
            </div>
          </div>

          {/* Submit button */}
          <div className="flex items-center justify-end gap-2 pt-2">
            {(selectedFile || formValues.imageUrl || formValues.altText) && (
              <button
                type="button"
                onClick={resetForm}
                disabled={isActionDisabled}
                className="px-3 py-1.5 text-xs font-medium text-gray-600 hover:text-gray-900 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
              >
                Hủy bỏ
              </button>
            )}

            <button
              type="submit"
              data-testid="btn-submit-image"
              disabled={isActionDisabled}
              className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold rounded-lg text-white bg-green-800 hover:bg-green-900 shadow-xs disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {isLoading ? (
                <>
                  <svg className="w-3.5 h-3.5 animate-spin" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                  </svg>
                  Đang xử lý...
                </>
              ) : (
                <>
                  <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                  </svg>
                  Tải lên & Lưu hình ảnh
                </>
              )}
            </button>
          </div>
        </form>
      </div>

      {/* Gallery Section */}
      {images.length === 0 ? (
        /* Empty State */
        <div
          data-testid="image-empty-state"
          className="flex flex-col items-center justify-center p-10 rounded-xl bg-white border border-dashed border-gray-200 text-center space-y-2"
        >
          <div className="w-12 h-12 rounded-full bg-green-50 flex items-center justify-center text-green-800">
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
          </div>
          <h4 className="text-sm font-semibold text-gray-800">Chưa có hình ảnh nào cho công thức</h4>
          <p className="text-xs text-gray-500 max-w-sm">
            Tải lên ít nhất một ảnh minh họa cho món ăn của bạn. Ảnh đầu tiên được thêm sẽ tự động trở thành ảnh đại diện chính của công thức.
          </p>
        </div>
      ) : (
        /* Image Grid */
        <div
          data-testid="image-grid"
          className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4"
        >
          {images.map((image, idx) => (
            <ImageCard
              key={image.id}
              image={image}
              index={idx}
              disabled={isActionDisabled}
              onSetPrimary={handleSetPrimary}
              onDeleteRequest={handleDeleteRequest}
            />
          ))}
        </div>
      )}

      {/* Delete Confirmation Dialog */}
      {imageToDelete && (
        <div
          data-testid="modal-delete-image"
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 animate-fade-in"
        >
          <div className="bg-white rounded-xl shadow-xl max-w-md w-full p-5 space-y-4 border border-gray-200">
            <div className="flex items-start gap-3">
              <div className="p-2 rounded-full bg-red-100 text-red-600 shrink-0">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
              </div>
              <div className="space-y-1">
                <h4 className="text-sm font-bold text-gray-900">Xác nhận xóa hình ảnh</h4>
                <p className="text-xs text-gray-600 leading-relaxed">
                  Bạn có chắc chắn muốn xóa hình ảnh này không?
                  {imageToDelete.isPrimary && (
                    <span className="block mt-1 font-semibold text-amber-700">
                      Lưu ý: Đây là ảnh đại diện chính. Khi xóa, hệ thống sẽ tự động chuyển cờ đại diện cho ảnh kế tiếp.
                    </span>
                  )}
                </p>
              </div>
            </div>

            <div className="flex items-center justify-end gap-2 pt-2 border-t border-gray-100">
              <button
                type="button"
                data-testid="btn-cancel-delete-image"
                disabled={isLoading}
                onClick={() => setImageToDelete(null)}
                className="px-3 py-1.5 text-xs font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                data-testid="btn-confirm-delete-image"
                disabled={isLoading}
                onClick={handleConfirmDelete}
                className="px-3.5 py-1.5 text-xs font-semibold text-white bg-red-600 rounded-lg hover:bg-red-700 disabled:opacity-50 transition-colors"
              >
                {isLoading ? 'Đang xóa...' : 'Xóa hình ảnh'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
