'use client';

import React, { useState, useEffect, useCallback, useRef } from 'react';
import type { RecipeImage, ImageFormValues, ImageValidationError } from '@/types/image';
import {
  getRecipeImages,
  addRecipeImage,
  uploadRecipeImageFile,
  updateRecipeImage,
  setPrimaryRecipeImage,
  deleteRecipeImage,
  ImageApiError,
} from '@/lib/api/images';
import { useAuthStore } from '@/lib/auth-store';
import { ImageCard } from './ImageCard';
import { ImagePreview } from './ImagePreview';

export interface ImageManagerProps {
  /**
   * Nếu có recipeId: Chế độ Existing Recipe (gọi REST API thật).
   * Nếu không có recipeId: Chế độ New Recipe (quản lý state cục bộ hoặc yêu cầu lưu Recipe trước).
   */
  recipeId?: string;
  authorId?: string;
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
  authorId,
  initialImages = [],
  onImagesChange,
  disabled: externalDisabled = false,
}) => {
  const isDirectApiMode = Boolean(recipeId && recipeId.trim() !== '');

  // Lấy thông tin người dùng từ auth store để kiểm tra phân quyền (Author-Owner hoặc Admin)
  const user = useAuthStore((state) => state.user);
  const isAuthenticated = Boolean(user);
  const checkPermissions = isDirectApiMode && Boolean(authorId);
  const isOwnerOrAdmin =
    !checkPermissions ||
    Boolean(user && (user.roles?.includes('Admin') || user.id === authorId));

  const [images, setImages] = useState<RecipeImage[]>(() =>
    [...initialImages].sort((a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0))
  );

  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [uploadProgress, setUploadProgress] = useState<number>(0);
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
  // Modal xem trước ảnh lớn
  const [previewImage, setPreviewImage] = useState<RecipeImage | null>(null);

  // Tự động load ảnh từ backend khi có recipeId
  const fetchImages = useCallback(() => {
    if (!isDirectApiMode) return;

    let cancelled = false;
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
          setErrorMessage(
            err instanceof Error ? err.message : 'Không tải được danh sách hình ảnh.'
          );
        }
      })
      .finally(() => {
        if (!cancelled) {
          setIsLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [isDirectApiMode, recipeId, onImagesChange]);

  useEffect(() => {
    if (isDirectApiMode && initialImages.length === 0) {
      const cancel = fetchImages();
      return cancel;
    }
  }, [isDirectApiMode, initialImages.length, fetchImages]);

  // Cập nhật khi initialImages từ bên ngoài thay đổi
  useEffect(() => {
    if (initialImages.length > 0) {
      const sorted = [...initialImages].sort(
        (a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0)
      );
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
      const sorted = [...newImages].sort(
        (a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0)
      );
      setImages(sorted);
      onImagesChange?.(sorted);
    },
    [onImagesChange]
  );

  const isActionDisabled = externalDisabled || isLoading || !isOwnerOrAdmin;

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
    setUploadProgress(0);
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
        errors.imageUrl =
          'Đường dẫn ảnh không hợp lệ. Phải bắt đầu bằng http:// hoặc https://';
      } else if (trimmedUrl.length > 500) {
        errors.imageUrl = 'Đường dẫn ảnh không được vượt quá 500 ký tự.';
      }
    }

    if (formValues.altText && formValues.altText.length > 200) {
      errors.altText = 'Chú thích ảnh (Alt text) không được vượt quá 200 ký tự.';
    }

    if (formValues.orderIndex !== '' && formValues.orderIndex !== undefined) {
      const parsed = Number(formValues.orderIndex);
      if (Number.isNaN(parsed) || parsed < 0) {
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
      const targetUrl =
        uploadMode === 'file' ? previewUrl || '' : formValues.imageUrl.trim();
      const nextOrder =
        formValues.orderIndex !== ''
          ? Number(formValues.orderIndex)
          : images.length > 0
          ? Math.max(...images.map((img) => img.orderIndex ?? 0)) + 1
          : 0;

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
    setUploadProgress(10);
    try {
      let added: RecipeImage;

      if (uploadMode === 'file' && selectedFile) {
        // Tải file trực tiếp lên endpoint multipart của Recipe
        added = await uploadRecipeImageFile(
          recipeId!,
          selectedFile,
          formValues.altText.trim() || undefined,
          images.length === 0 ? true : formValues.isPrimary,
          (progressEvent) => {
            if (progressEvent.total) {
              const percent = Math.round(
                (progressEvent.loaded * 100) / progressEvent.total
              );
              setUploadProgress(percent);
            }
          }
        );
      } else {
        // Nhập URL có sẵn
        const nextOrder =
          formValues.orderIndex !== ''
            ? Number(formValues.orderIndex)
            : images.length > 0
            ? Math.max(...images.map((img) => img.orderIndex ?? 0)) + 1
            : 0;

        added = await addRecipeImage(recipeId!, {
          originalUrl: formValues.imageUrl.trim(),
          altText: formValues.altText.trim() || null,
          isPrimary: images.length === 0 ? true : formValues.isPrimary,
          orderIndex: nextOrder,
        });
      }

      // Nếu backend trả về ảnh mới là Primary, cập nhật cờ các ảnh khác
      let nextImages: RecipeImage[];
      if (added.isPrimary) {
        nextImages = images.map((img) => ({ ...img, isPrimary: false }));
        nextImages.push(added);
      } else {
        nextImages = [...images, added];
      }

      const sorted = nextImages.sort(
        (a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0)
      );
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
      setErrorMessage(
        err instanceof Error ? err.message : 'Thêm hình ảnh không thành công.'
      );
    } finally {
      setIsLoading(false);
      setUploadProgress(0);
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
        setErrorMessage(
          err instanceof Error ? err.message : 'Không thể đặt ảnh làm ảnh đại diện.'
        );
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

  // Sắp xếp thứ tự ảnh (Reorder: Move Up / Move Down)
  const handleMoveImage = async (index: number, direction: 'up' | 'down') => {
    const targetIndex = direction === 'up' ? index - 1 : index + 1;
    if (targetIndex < 0 || targetIndex >= images.length) return;

    setErrorMessage(null);
    const currentImg = images[index];
    const targetImg = images[targetIndex];

    const currentOrder = currentImg.orderIndex ?? index;
    const targetOrder = targetImg.orderIndex ?? targetIndex;

    // Hoán đổi thứ tự trong mảng
    const newImages = [...images];
    newImages[index] = { ...targetImg, orderIndex: currentOrder };
    newImages[targetIndex] = { ...currentImg, orderIndex: targetOrder };

    const sorted = newImages.sort(
      (a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0)
    );
    setImages(sorted);
    onImagesChange?.(sorted);

    // Nếu ở chế độ Direct API: Cập nhật thứ tự lên backend qua PATCH
    if (isDirectApiMode) {
      try {
        await updateRecipeImage(recipeId!, currentImg.id, {
          orderIndex: targetOrder,
        });
        await updateRecipeImage(recipeId!, targetImg.id, {
          orderIndex: currentOrder,
        });
      } catch (err) {
        // Rollback lại thứ tự nếu API thất bại
        setImages(images);
        onImagesChange?.(images);
        setErrorMessage(
          err instanceof Error
            ? err.message
            : 'Không thể cập nhật thứ tự hình ảnh.'
        );
      }
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
          const sortedRemaining = [...remaining].sort(
            (a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0)
          );
          sortedRemaining[0].isPrimary = true;
          remaining = sortedRemaining;
        }

        setImages(remaining);
        onImagesChange?.(remaining);
        setImageToDelete(null);
      } catch (err) {
        setErrorMessage(
          err instanceof Error ? err.message : 'Không thể xóa hình ảnh.'
        );
      } finally {
        setIsLoading(false);
      }
    } else {
      // Controlled mode
      const wasPrimary = imageToDelete.isPrimary;
      let remaining = images.filter((img) => img.id !== imageToDelete.id);
      if (wasPrimary && remaining.length > 0) {
        const sortedRemaining = [...remaining].sort(
          (a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0)
        );
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
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-4 border-b border-emerald-100">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-800">
              <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
              Culinary Gallery
            </span>
          </div>
          <h3 className="text-xl font-bold text-slate-900 tracking-tight flex items-center gap-2">
            Bộ sưu tập hình ảnh
            <span className="inline-flex items-center justify-center px-2 py-0.5 rounded-full text-xs font-semibold bg-emerald-700 text-white shadow-2xs">
              {images.length}
            </span>
          </h3>
          <p className="text-xs text-slate-500 mt-0.5">
            Quản lý ảnh minh họa của món ăn. Ảnh đầu tiên được thêm sẽ tự động đặt làm ảnh đại diện.
          </p>
        </div>

        {!isDirectApiMode && (
          <div className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-medium bg-amber-50 text-amber-800 border border-amber-200 shadow-2xs">
            <svg className="w-3.5 h-3.5 text-amber-600 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            Chế độ công thức mới (State cục bộ)
          </div>
        )}
      </div>

      {/* Authorization Banners */}
      {checkPermissions && !isAuthenticated && (
        <div
          data-testid="auth-required-banner"
          className="p-4 rounded-2xl bg-amber-50 border border-amber-200 text-sm text-amber-800 flex items-start gap-3 shadow-xs"
        >
          <div className="w-8 h-8 rounded-full bg-amber-100 flex items-center justify-center text-amber-700 shrink-0 mt-0.5">
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
          </div>
          <div className="flex-1">
            <strong className="font-semibold block text-amber-900">Yêu cầu đăng nhập:</strong>
            <span className="text-xs sm:text-sm">
              Bạn cần đăng nhập với tư cách tác giả sở hữu hoặc Quản trị viên để tải lên và quản lý hình ảnh công thức.
            </span>
          </div>
        </div>
      )}

      {checkPermissions && isAuthenticated && !isOwnerOrAdmin && (
        <div
          data-testid="forbidden-banner"
          className="p-4 rounded-2xl bg-slate-50 border border-slate-200 text-sm text-slate-700 flex items-start gap-3 shadow-xs"
        >
          <div className="w-8 h-8 rounded-full bg-slate-200 flex items-center justify-center text-slate-600 shrink-0 mt-0.5">
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
            </svg>
          </div>
          <div className="flex-1">
            <strong className="font-semibold block text-slate-900">Chế độ chỉ xem:</strong>
            <span className="text-xs sm:text-sm">
              Chỉ tác giả sở hữu công thức này hoặc Quản trị viên mới có quyền tải lên, thay đổi ảnh chính hoặc xóa hình ảnh.
            </span>
          </div>
        </div>
      )}

      {/* Error banner */}
      {errorMessage && (
        <div
          data-testid="image-error-banner"
          className="p-4 rounded-2xl bg-rose-50 border border-rose-200 text-sm text-rose-800 flex items-start gap-3 shadow-xs animate-shake"
        >
          <div className="w-8 h-8 rounded-full bg-rose-100 flex items-center justify-center text-rose-600 shrink-0 mt-0.5">
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <circle cx="12" cy="12" r="10" strokeWidth={2} />
              <line x1="12" y1="8" x2="12" y2="12" strokeWidth={2} strokeLinecap="round" />
              <line x1="12" y1="16" x2="12.01" y2="16" strokeWidth={2} strokeLinecap="round" />
            </svg>
          </div>
          <div className="flex-1">
            <strong className="font-semibold block text-rose-900">Đã xảy ra lỗi:</strong>
            <span className="text-xs sm:text-sm">{errorMessage}</span>
          </div>
          <div className="flex items-center gap-1.5">
            {isDirectApiMode && (
              <button
                type="button"
                data-testid="btn-retry-fetch"
                onClick={fetchImages}
                className="px-2.5 py-1 text-xs font-semibold rounded-lg bg-rose-100 text-rose-800 hover:bg-rose-200 transition-colors"
              >
                Thử lại
              </button>
            )}
            <button
              type="button"
              onClick={() => setErrorMessage(null)}
              className="text-rose-500 hover:text-rose-800 p-1 rounded-lg hover:bg-rose-100/60 transition-colors"
              aria-label="Đóng thông báo"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>
      )}

      {/* Upload Progress Bar */}
      {uploadProgress > 0 && uploadProgress < 100 && (
        <div data-testid="upload-progress-bar" className="space-y-1.5 p-3 rounded-xl bg-emerald-50 border border-emerald-200">
          <div className="flex justify-between text-xs font-semibold text-emerald-800">
            <span>Đang tải ảnh lên máy chủ...</span>
            <span>{uploadProgress}%</span>
          </div>
          <div className="w-full h-2 rounded-full bg-emerald-200 overflow-hidden">
            <div
              className="h-full bg-emerald-600 transition-all duration-200 ease-out"
              style={{ width: `${uploadProgress}%` }}
            />
          </div>
        </div>
      )}

      {/* Upload & Add Form Section */}
      <div className="p-5 sm:p-6 rounded-2xl bg-gradient-to-br from-emerald-50/50 via-white to-emerald-50/30 border border-emerald-100/90 shadow-xs space-y-5">
        <div className="flex items-center justify-between flex-wrap gap-3 pb-3 border-b border-emerald-100/60">
          <div className="flex items-center gap-2">
            <div className="w-7 h-7 rounded-lg bg-emerald-100 flex items-center justify-center text-emerald-800 font-bold text-xs">
              +
            </div>
            <span className="text-sm font-bold text-slate-900">Thêm hình ảnh mới</span>
          </div>

          {/* Mode Switch Tabs */}
          <div className="inline-flex rounded-xl p-1 bg-slate-100 border border-slate-200 text-xs">
            <button
              type="button"
              data-testid="tab-upload-file"
              onClick={() => {
                setUploadMode('file');
                setFormErrors({});
              }}
              className={`px-3 py-1.5 rounded-lg font-semibold transition-all ${
                uploadMode === 'file'
                  ? 'bg-emerald-700 text-white shadow-xs'
                  : 'text-slate-600 hover:text-slate-900'
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
              className={`px-3 py-1.5 rounded-lg font-semibold transition-all ${
                uploadMode === 'url'
                  ? 'bg-emerald-700 text-white shadow-xs'
                  : 'text-slate-600 hover:text-slate-900'
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
                onClick={() => {
                  if (!isActionDisabled) {
                    fileInputRef.current?.click();
                  }
                }}
                className={`relative flex flex-col items-center justify-center p-8 border-2 border-dashed rounded-2xl transition-all duration-200 ${
                  isActionDisabled
                    ? 'border-slate-200 bg-slate-50 cursor-not-allowed opacity-60'
                    : formErrors.file
                    ? 'border-rose-300 bg-rose-50/40 cursor-pointer'
                    : 'border-emerald-300 bg-emerald-50/20 hover:border-emerald-600 hover:bg-emerald-50/50 cursor-pointer'
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
                  <div className="flex flex-col items-center gap-2.5">
                    {/* eslint-disable-next-line @next/next/no-img-element */}
                    <img
                      src={previewUrl}
                      alt="Preview ảnh trước khi upload"
                      data-testid="image-preview"
                      className="h-40 w-auto max-w-full rounded-xl object-contain shadow-md border border-emerald-200 bg-white p-1"
                    />
                    <div className="text-center">
                      <p className="text-xs text-emerald-900 font-semibold">
                        Đã chọn: {selectedFile?.name} ({(selectedFile?.size ? selectedFile.size / (1024 * 1024) : 0).toFixed(2)} MB)
                      </p>
                      <span className="text-[11px] text-emerald-700 underline font-medium hover:text-emerald-900">
                        Nhấp để thay đổi ảnh khác
                      </span>
                    </div>
                  </div>
                ) : (
                  <div className="flex flex-col items-center text-center space-y-2">
                    <div className="w-12 h-12 rounded-2xl bg-emerald-100/80 flex items-center justify-center text-emerald-800 shadow-2xs">
                      <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.75} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                      </svg>
                    </div>
                    <div>
                      <p className="text-xs sm:text-sm font-semibold text-slate-800">
                        Nhấp để chọn ảnh từ máy hoặc kéo thả vào đây
                      </p>
                      <p className="text-[11px] text-slate-500 mt-0.5">
                        Hỗ trợ JPEG, PNG, WebP, AVIF (Tối đa 5 MB)
                      </p>
                    </div>
                  </div>
                )}
              </div>
              {formErrors.file && (
                <p data-testid="error-file" className="text-xs text-rose-600 font-medium flex items-center gap-1">
                  <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <circle cx="12" cy="12" r="10" strokeWidth={2} />
                    <line x1="12" y1="8" x2="12" y2="12" strokeWidth={2} />
                    <line x1="12" y1="16" x2="12.01" y2="16" strokeWidth={2} />
                  </svg>
                  {formErrors.file}
                </p>
              )}
            </div>
          ) : (
            /* Upload Mode: Direct URL */
            <div className="space-y-1.5">
              <label htmlFor="image-url-input" className="block text-xs font-semibold text-slate-700">
                Đường dẫn ảnh (Image URL) <span className="text-rose-500">*</span>
              </label>
              <input
                id="image-url-input"
                type="url"
                data-testid="input-image-url"
                maxLength={500}
                disabled={isActionDisabled}
                value={formValues.imageUrl}
                onChange={(e) => {
                  setFormValues({ ...formValues, imageUrl: e.target.value });
                  if (formErrors.imageUrl) setFormErrors({ ...formErrors, imageUrl: undefined });
                }}
                placeholder="https://example.com/photos/recipe-main.jpg"
                className={`w-full px-3.5 py-2.5 text-xs sm:text-sm rounded-xl border bg-white focus:outline-none focus:ring-2 transition-all ${
                  formErrors.imageUrl
                    ? 'border-rose-300 focus:ring-rose-200'
                    : 'border-emerald-200 focus:ring-emerald-100 focus:border-emerald-600'
                }`}
              />
              {formErrors.imageUrl && (
                <p data-testid="error-image-url" className="text-xs text-rose-600 font-medium flex items-center gap-1">
                  <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <circle cx="12" cy="12" r="10" strokeWidth={2} />
                    <line x1="12" y1="8" x2="12" y2="12" strokeWidth={2} />
                    <line x1="12" y1="16" x2="12.01" y2="16" strokeWidth={2} />
                  </svg>
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
                <label htmlFor="image-alt-input" className="block text-xs font-semibold text-slate-700">
                  Chú thích ảnh (Alt text)
                </label>
                <span className="text-[10px] text-slate-400 font-mono">{formValues.altText.length}/200</span>
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
                className="w-full px-3.5 py-2 text-xs rounded-xl border border-emerald-200 bg-white focus:outline-none focus:ring-2 focus:ring-emerald-100 focus:border-emerald-600"
              />
              {formErrors.altText && (
                <p className="text-xs text-rose-600">{formErrors.altText}</p>
              )}
            </div>

            {/* OrderIndex */}
            <div className="space-y-1">
              <label htmlFor="image-order-input" className="block text-xs font-semibold text-slate-700">
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
                className="w-full px-3.5 py-2 text-xs rounded-xl border border-emerald-200 bg-white focus:outline-none focus:ring-2 focus:ring-emerald-100 focus:border-emerald-600"
              />
              {formErrors.orderIndex && (
                <p className="text-xs text-rose-600">{formErrors.orderIndex}</p>
              )}
            </div>

            {/* IsPrimary Checkbox */}
            <div className="flex items-center gap-2.5 pt-5">
              <input
                id="image-primary-checkbox"
                type="checkbox"
                data-testid="checkbox-is-primary"
                disabled={isActionDisabled}
                checked={formValues.isPrimary || images.length === 0}
                onChange={(e) => setFormValues({ ...formValues, isPrimary: e.target.checked })}
                className="w-4 h-4 text-emerald-700 border-emerald-300 rounded-md focus:ring-emerald-600 cursor-pointer"
              />
              <label htmlFor="image-primary-checkbox" className="text-xs font-semibold text-slate-800 cursor-pointer select-none">
                Đặt làm ảnh đại diện chính
              </label>
            </div>
          </div>

          {/* Submit button */}
          <div className="flex items-center justify-end gap-2.5 pt-3 border-t border-emerald-100/60">
            {(selectedFile || formValues.imageUrl || formValues.altText) && (
              <button
                type="button"
                onClick={resetForm}
                disabled={isActionDisabled}
                className="px-3.5 py-2 text-xs font-semibold text-slate-600 hover:text-slate-900 bg-white border border-slate-200 rounded-xl hover:bg-slate-50 transition-colors shadow-2xs"
              >
                Hủy bỏ
              </button>
            )}

            <button
              type="submit"
              data-testid="btn-submit-image"
              disabled={isActionDisabled}
              className="inline-flex items-center gap-2 px-5 py-2.5 text-xs sm:text-sm font-semibold rounded-xl text-white bg-emerald-700 hover:bg-emerald-800 shadow-sm hover:shadow-md disabled:opacity-50 disabled:cursor-not-allowed transition-all"
            >
              {isLoading ? (
                <>
                  <svg className="w-4 h-4 animate-spin text-emerald-200" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                  </svg>
                  Đang xử lý...
                </>
              ) : (
                <>
                  <svg className="w-4 h-4 text-emerald-100" fill="none" stroke="currentColor" viewBox="0 0 24 24">
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
          className="flex flex-col items-center justify-center p-12 rounded-2xl bg-emerald-50/20 border-2 border-dashed border-emerald-200 text-center space-y-3"
        >
          <div className="w-14 h-14 rounded-2xl bg-emerald-100 flex items-center justify-center text-emerald-800 shadow-2xs">
            <svg className="w-7 h-7" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
          </div>
          <h4 className="text-base font-bold text-slate-800">Chưa có hình ảnh nào cho công thức</h4>
          <p className="text-xs sm:text-sm text-slate-500 max-w-md leading-relaxed">
            Tải lên ít nhất một ảnh minh họa cho món ăn của bạn. Ảnh đầu tiên được thêm sẽ tự động trở thành ảnh đại diện chính của công thức.
          </p>
        </div>
      ) : (
        /* Image Grid */
        <div
          data-testid="image-grid"
          className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5"
        >
          {images.map((image, idx) => (
            <ImageCard
              key={image.id}
              image={image}
              index={idx}
              disabled={isActionDisabled}
              onSetPrimary={handleSetPrimary}
              onDeleteRequest={handleDeleteRequest}
              onPreview={(img) => setPreviewImage(img)}
              onMoveUp={() => handleMoveImage(idx, 'up')}
              onMoveDown={() => handleMoveImage(idx, 'down')}
              canMoveUp={idx > 0 && !isActionDisabled}
              canMoveDown={idx < images.length - 1 && !isActionDisabled}
            />
          ))}
        </div>
      )}

      {/* Preview Modal */}
      <ImagePreview
        image={previewImage}
        isOpen={Boolean(previewImage)}
        onClose={() => setPreviewImage(null)}
        onSetPrimary={handleSetPrimary}
        onDeleteRequest={handleDeleteRequest}
        disabled={isActionDisabled}
      />

      {/* Delete Confirmation Dialog */}
      {imageToDelete && (
        <div
          data-testid="modal-delete-image"
          className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/70 p-4 backdrop-blur-xs animate-fade-in"
        >
          <div className="bg-white rounded-3xl shadow-2xl max-w-md w-full p-6 space-y-4 border border-emerald-100">
            <div className="flex items-start gap-3.5">
              <div className="p-3 rounded-2xl bg-rose-100 text-rose-600 shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
              </div>
              <div className="space-y-1.5">
                <h4 className="text-base font-bold text-slate-900">Xác nhận xóa hình ảnh</h4>
                <p className="text-xs sm:text-sm text-slate-600 leading-relaxed">
                  Bạn có chắc chắn muốn xóa hình ảnh này không?
                  {imageToDelete.isPrimary && (
                    <span className="block mt-2 p-2.5 rounded-xl bg-amber-50 text-amber-800 border border-amber-200 text-xs font-semibold">
                      Lưu ý: Đây là ảnh đại diện chính. Khi xóa, hệ thống sẽ tự động chuyển cờ đại diện cho ảnh kế tiếp.
                    </span>
                  )}
                </p>
              </div>
            </div>

            <div className="flex items-center justify-end gap-2.5 pt-3 border-t border-slate-100">
              <button
                type="button"
                data-testid="btn-cancel-delete-image"
                disabled={isLoading}
                onClick={() => setImageToDelete(null)}
                className="px-4 py-2 text-xs font-semibold text-slate-700 bg-white border border-slate-200 rounded-xl hover:bg-slate-50 transition-colors"
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                data-testid="btn-confirm-delete-image"
                disabled={isLoading}
                onClick={handleConfirmDelete}
                className="px-4 py-2 text-xs font-semibold text-white bg-rose-600 rounded-xl hover:bg-rose-700 disabled:opacity-50 transition-colors shadow-xs"
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
