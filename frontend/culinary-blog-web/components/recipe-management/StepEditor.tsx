'use client';

import React, { useState, useEffect, useCallback, useRef } from 'react';
import type {
  RecipeStep,
  StepFormValues,
  StepValidationError,
} from '@/types/step';
import {
  getRecipeSteps,
  createRecipeStep,
  updateRecipeStep,
  deleteRecipeStep,
  reorderRecipeSteps,
  uploadStepImageFile,
  StepApiError,
} from '@/lib/api/steps';
import { useAuthStore } from '@/lib/auth-store';
import { validateImageFile } from '@/lib/image-validator';
import { resolveImageUrl } from '@/lib/image-url';
import { StepItem } from './StepItem';

export type StepDraft = {
  id?: string;
  stepNumber?: number;
  title: string;
  description: string;
  timerMinutes?: number | string | null;
  imageUrl?: string | null;
  imageFile?: File | null;
  imagePreviewUrl?: string | null;
  uploadStatus?: 'idle' | 'uploading' | 'success' | 'error';
  uploadError?: string | null;
};

export interface FullStepEditorProps {
  /**
   * Nếu có recipeId: Chế độ Direct API mode (CRUD trực tiếp với Backend).
   * Nếu không có recipeId: Chế độ Controlled mode (Quản lý local state cho Recipe mới).
   */
  recipeId?: string;
  /**
   * ID tác giả của Recipe dùng để kiểm tra phân quyền sở hữu
   */
  authorId?: string;
  /**
   * Danh sách bước ban đầu (dùng khi render RecipeForm hoặc edit trang)
   */
  initialSteps?: RecipeStep[];
  /**
   * Callback thông báo cho form cha khi danh sách bước thay đổi
   */
  onStepsChange?: (steps: RecipeStep[]) => void;
  /**
   * Cho phép vô hiệu hóa toàn bộ editor từ form cha (ví dụ khi đang lưu Recipe)
   */
  disabled?: boolean;
}

export type StepEditorProps = FullStepEditorProps & {
  steps?: StepDraft[];
  onChange?: (steps: StepDraft[]) => void;
};

const INITIAL_FORM_VALUES: StepFormValues = {
  title: '',
  description: '',
  timerMinutes: '',
  imageUrl: '',
};

function isValidUrl(urlString: string): boolean {
  try {
    const url = new URL(urlString);
    return url.protocol === 'http:' || url.protocol === 'https:';
  } catch {
    return false;
  }
}

const DraftStepEditor: React.FC<{
  steps: StepDraft[];
  onChange: (steps: StepDraft[]) => void;
  recipeId?: string;
}> = ({ steps, onChange, recipeId }) => {
  const [uploadingIndex, setUploadingIndex] = useState<number | null>(null);
  const [showManualUrl, setShowManualUrl] = useState<Record<number, boolean>>({});

  const update = (
    index: number,
    key: keyof StepDraft,
    value: string | number | null | undefined | File
  ) => {
    onChange(
      steps.map((step, stepIndex) =>
        stepIndex === index ? { ...step, [key]: value } : step
      )
    );
  };

  const updatePatch = (index: number, patch: Partial<StepDraft>) => {
    onChange(
      steps.map((step, stepIndex) =>
        stepIndex === index ? { ...step, ...patch } : step
      )
    );
  };

  const move = (index: number, offset: -1 | 1) => {
    const target = index + offset;
    if (target < 0 || target >= steps.length) return;
    const reordered = [...steps];
    [reordered[index], reordered[target]] = [reordered[target], reordered[index]];
    onChange(reordered);
  };

  const handleFileChange = async (index: number, e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Validate client-side (size <= 5MB, format jpg/png/webp/avif)
    const validation = validateImageFile(file);
    if (!validation.isValid) {
      updatePatch(index, {
        uploadStatus: 'error',
        uploadError: validation.errorMessage || 'Tệp hình ảnh không hợp lệ (tối đa 5 MB, định dạng JPG, PNG, WebP, AVIF).',
      });
      e.target.value = '';
      return;
    }

    // Local object URL for preview
    const previewUrl = URL.createObjectURL(file);

    if (recipeId) {
      // In Edit mode (recipeId exists), upload directly to MinIO
      setUploadingIndex(index);
      updatePatch(index, {
        imageFile: file,
        imagePreviewUrl: previewUrl,
        uploadStatus: 'uploading',
        uploadError: null,
      });

      try {
        const res = await uploadStepImageFile(recipeId, file);
        if (res?.url) {
          updatePatch(index, {
            imageUrl: res.url,
            imageFile: null,
            imagePreviewUrl: null,
            uploadStatus: 'success',
            uploadError: null,
          });
        }
      } catch (err: unknown) {
        updatePatch(index, {
          imageFile: file,
          imagePreviewUrl: previewUrl,
          uploadStatus: 'error',
          uploadError: err instanceof Error ? err.message : 'Tải ảnh lên máy chủ thất bại.',
        });
      } finally {
        setUploadingIndex(null);
        e.target.value = '';
      }
    } else {
      // In Create mode (no recipeId yet), keep file and preview in memory
      updatePatch(index, {
        imageFile: file,
        imagePreviewUrl: previewUrl,
        imageUrl: '', // Crucial: never set blob URL as imageUrl!
        uploadStatus: 'idle',
        uploadError: null,
      });
      e.target.value = '';
    }
  };

  const handleRemoveImage = (index: number) => {
    const step = steps[index];
    if (typeof window !== 'undefined' && step.imagePreviewUrl?.startsWith('blob:')) {
      URL.revokeObjectURL(step.imagePreviewUrl);
    }
    updatePatch(index, {
      imageUrl: '',
      imageFile: null,
      imagePreviewUrl: null,
      uploadStatus: 'idle',
      uploadError: null,
    });
    const fileInput = document.getElementById(`step-file-${index}`) as HTMLInputElement | null;
    if (fileInput) fileInput.value = '';
  };

  const handleRetryUpload = async (index: number) => {
    const step = steps[index];
    if (!step.imageFile) return;

    if (!recipeId) {
      updatePatch(index, {
        uploadStatus: 'idle',
        uploadError: null,
      });
      return;
    }

    setUploadingIndex(index);
    updatePatch(index, {
      uploadStatus: 'uploading',
      uploadError: null,
    });

    try {
      const res = await uploadStepImageFile(recipeId, step.imageFile);
      if (res?.url) {
        updatePatch(index, {
          imageUrl: res.url,
          imageFile: null,
          imagePreviewUrl: null,
          uploadStatus: 'success',
          uploadError: null,
        });
      }
    } catch (err: unknown) {
      updatePatch(index, {
        uploadStatus: 'error',
        uploadError: err instanceof Error ? err.message : 'Tải ảnh lên máy chủ thất bại.',
      });
    } finally {
      setUploadingIndex(null);
    }
  };

  return (
    <div className="step-editor space-y-5">
      {steps.map((step, index) => {
        const currentPreview = step.imagePreviewUrl || step.imageUrl;
        const isUrlInputVisible =
          showManualUrl[index] ||
          (Boolean(step.imageUrl) && !step.imageFile && !step.imagePreviewUrl && showManualUrl[index] !== false);

        return (
          <div
            className="step-row bg-gray-50/70 hover:bg-white rounded-2xl border border-gray-200 hover:border-emerald-200 p-5 sm:p-6 space-y-4 transition-all shadow-2xs"
            key={index}
          >
            <div className="step-row-header flex items-center justify-between pb-3 border-b border-gray-100">
              <span className="step-badge inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-emerald-100 text-emerald-800">
                <span className="w-1.5 h-1.5 rounded-full bg-emerald-600" />
                Bước {String(index + 1).padStart(2, '0')}
              </span>
              <div className="ingredient-row-actions flex items-center gap-1.5">
                <button
                  type="button"
                  aria-label="Chuyển bước lên"
                  title="Di chuyển lên"
                  disabled={index === 0}
                  onClick={() => move(index, -1)}
                  className="p-1.5 text-xs font-bold rounded-lg border border-gray-200 hover:bg-emerald-50 hover:text-emerald-700 disabled:opacity-30 transition-colors cursor-pointer"
                >
                  ↑
                </button>
                <button
                  type="button"
                  aria-label="Chuyển bước xuống"
                  title="Di chuyển xuống"
                  disabled={index === steps.length - 1}
                  onClick={() => move(index, 1)}
                  className="p-1.5 text-xs font-bold rounded-lg border border-gray-200 hover:bg-emerald-50 hover:text-emerald-700 disabled:opacity-30 transition-colors cursor-pointer"
                >
                  ↓
                </button>
                <button
                  className="small-button text-xs font-bold px-3 py-1.5 rounded-lg border border-rose-200 text-rose-600 bg-rose-50/50 hover:bg-rose-100 transition-colors cursor-pointer ml-1"
                  type="button"
                  onClick={() =>
                    onChange(
                      steps.length === 1
                        ? [{ title: '', description: '', timerMinutes: '', imageUrl: '' }]
                        : steps.filter((_, stepIndex) => stepIndex !== index)
                    )
                  }
                >
                  🗑️ Xóa bước
                </button>
              </div>
            </div>

            <div className="w-full space-y-4">
              <div className="w-full space-y-1.5">
                <label
                  htmlFor={`step-title-${index}`}
                  className="block text-xs sm:text-sm font-semibold text-gray-800"
                >
                  Tiêu đề bước <span className="text-gray-400 font-normal text-xs">(tùy chọn)</span>
                </label>
                <input
                  id={`step-title-${index}`}
                  data-testid={`input-step-title-${index}`}
                  maxLength={200}
                  value={step.title}
                  onChange={(event) => update(index, 'title', event.target.value)}
                  placeholder="Ví dụ: Sơ chế nguyên liệu hoặc Xào thơm hành tỏi..."
                  className="w-full px-4 py-2.5 rounded-xl border border-gray-300 bg-white text-gray-900 placeholder:text-gray-400 focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/20 outline-none text-sm transition-all shadow-2xs"
                />
              </div>

              <div className="w-full space-y-1.5">
                <label
                  htmlFor={`step-description-${index}`}
                  className="block text-xs sm:text-sm font-semibold text-gray-800"
                >
                  Hướng dẫn thực hiện * <span className="text-emerald-700 font-semibold">(Bắt buộc)</span>
                </label>
                <textarea
                  id={`step-description-${index}`}
                  data-testid={`input-step-description-${index}`}
                  required
                  maxLength={2000}
                  rows={3}
                  value={step.description}
                  onChange={(event) => update(index, 'description', event.target.value)}
                  placeholder="Mô tả cụ thể các thao tác thực hiện, căn lửa, thời gian hoặc dấu hiệu nhận biết..."
                  className="w-full px-4 py-2.5 rounded-xl border border-gray-300 bg-white text-gray-900 placeholder:text-gray-400 focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/20 outline-none text-sm transition-all leading-relaxed shadow-2xs"
                />
              </div>

              {/* Timer (1/3) & Image (2/3) Section on Desktop, 1 Column Full Width on Mobile */}
              <div className="grid grid-cols-1 md:grid-cols-3 gap-5 w-full pt-1 items-start">
                {/* Column 1: TimerMinutes (takes 1 of 3 columns = 1/3 width on md+) */}
                <div className="md:col-span-1 w-full space-y-1.5">
                  <label
                    htmlFor={`step-timer-${index}`}
                    className="block text-xs sm:text-sm font-semibold text-gray-800 whitespace-nowrap"
                  >
                    ⏱️ Thời gian hẹn giờ <span className="text-gray-500 font-normal text-xs">(phút, 0-1440)</span>
                  </label>
                  <div className="relative w-full">
                    <input
                      id={`step-timer-${index}`}
                      data-testid={`input-step-timer-${index}`}
                      type="number"
                      min="0"
                      max="1440"
                      step="1"
                      value={step.timerMinutes ?? ''}
                      onChange={(event) => update(index, 'timerMinutes', event.target.value)}
                      placeholder="Ví dụ: 15"
                      className="w-full pl-3.5 pr-14 py-2.5 text-sm font-medium rounded-xl border border-gray-300 bg-white text-gray-900 placeholder:text-gray-400 focus:outline-none focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/20 transition-all shadow-2xs"
                    />
                    <span className="absolute right-3.5 top-1/2 -translate-y-1/2 text-gray-500 text-xs font-semibold pointer-events-none select-none">
                      phút
                    </span>
                  </div>
                </div>

                {/* Column 2: Image Section (takes 2 of 3 columns = 2/3 width on md+) */}
                <div className="md:col-span-2 w-full space-y-2.5">
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <label
                      htmlFor={`step-image-${index}`}
                      className="block text-xs sm:text-sm font-semibold text-gray-800"
                    >
                      🖼️ Ảnh minh họa bước nấu <span className="text-gray-500 font-normal text-xs">(tùy chọn)</span>
                    </label>

                    <div className="flex flex-wrap items-center gap-2">
                      {/* Hidden file input */}
                      <input
                        type="file"
                        id={`step-file-${index}`}
                        data-testid={`input-step-file-${index}`}
                        accept="image/jpeg,image/png,image/webp,image/avif"
                        className="hidden"
                        onChange={(e) => handleFileChange(index, e)}
                      />

                      {/* Button: Chọn ảnh từ máy */}
                      <button
                        type="button"
                        data-testid={`btn-upload-step-image-${index}`}
                        disabled={uploadingIndex === index}
                        onClick={() => {
                          document.getElementById(`step-file-${index}`)?.click();
                        }}
                        className="inline-flex items-center px-3 py-1.5 rounded-lg text-xs font-semibold text-emerald-800 bg-emerald-50 hover:bg-emerald-100 border border-emerald-300 shadow-2xs transition-all cursor-pointer disabled:opacity-50"
                      >
                        <span>
                          {uploadingIndex === index
                            ? 'Đang tải lên...'
                            : currentPreview
                              ? 'Thay ảnh từ máy'
                              : 'Chọn ảnh từ máy'}
                        </span>
                      </button>

                      {/* Toggle Button: Nhập URL */}
                      <button
                        type="button"
                        data-testid={`btn-toggle-step-url-${index}`}
                        onClick={() =>
                          setShowManualUrl((prev) => ({
                            ...prev,
                            [index]: !isUrlInputVisible,
                          }))
                        }
                        className="inline-flex items-center px-2.5 py-1.5 rounded-lg text-xs text-gray-600 hover:text-gray-900 bg-gray-100 hover:bg-gray-200 border border-gray-200 transition-colors cursor-pointer"
                        title="Nhập URL ảnh trực tiếp (tùy chọn phụ)"
                      >
                        <span>{isUrlInputVisible ? 'Thu gọn URL' : 'Nhập URL'}</span>
                      </button>

                      {/* Button: Gỡ ảnh */}
                      {Boolean(currentPreview || step.imageFile) && (
                        <button
                          type="button"
                          data-testid={`btn-remove-step-image-${index}`}
                          onClick={() => handleRemoveImage(index)}
                          className="inline-flex items-center gap-1 text-xs text-rose-600 hover:text-rose-700 font-semibold hover:underline cursor-pointer ml-1"
                        >
                          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                          </svg>
                          <span>Gỡ ảnh</span>
                        </button>
                      )}
                    </div>
                  </div>

                  {/* Upload / Validation Error Banner */}
                  {step.uploadError && (
                    <div
                      data-testid={`step-image-error-${index}`}
                      className="flex items-center justify-between gap-2 p-2.5 rounded-xl bg-rose-50 border border-rose-200 text-rose-700 text-xs animate-in fade-in"
                    >
                      <div className="flex items-center gap-1.5">
                        <span>⚠️</span>
                        <span className="font-medium">{step.uploadError}</span>
                      </div>
                      {step.imageFile && (
                        <button
                          type="button"
                          data-testid={`btn-retry-step-image-${index}`}
                          disabled={uploadingIndex === index}
                          onClick={() => handleRetryUpload(index)}
                          className="px-2.5 py-1 rounded-lg bg-rose-600 text-white font-semibold hover:bg-rose-700 transition-colors text-[11px] shrink-0 cursor-pointer disabled:opacity-50"
                        >
                          {uploadingIndex === index ? 'Đang thử lại...' : 'Thử lại'}
                        </button>
                      )}
                    </div>
                  )}

                  {/* Collapsible Manual URL input */}
                  {isUrlInputVisible && (
                    <div className="relative w-full animate-in fade-in">
                      <input
                        id={`step-image-${index}`}
                        data-testid={`input-step-image-${index}`}
                        type="url"
                        maxLength={500}
                        value={step.imageUrl ?? ''}
                        onChange={(event) => {
                          const val = event.target.value;
                          if (typeof window !== 'undefined' && step.imagePreviewUrl?.startsWith('blob:')) {
                            URL.revokeObjectURL(step.imagePreviewUrl);
                          }
                          updatePatch(index, {
                            imageUrl: val,
                            imageFile: null,
                            imagePreviewUrl: null,
                            uploadStatus: 'idle',
                            uploadError: null,
                          });
                        }}
                        placeholder="https://images.unsplash.com/... hoặc đường dẫn URL ảnh"
                        className="w-full px-4 py-2.5 text-sm rounded-xl border border-gray-300 bg-white text-gray-900 placeholder:text-gray-400 focus:outline-none focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/20 transition-all shadow-2xs"
                      />
                    </div>
                  )}

                  {/* Thumbnail Preview Card */}
                  {currentPreview && (
                    <div
                      data-testid={`step-image-preview-${index}`}
                      className="flex items-center gap-3.5 p-3 bg-white rounded-xl border border-emerald-200/90 shadow-2xs w-full animate-in fade-in"
                    >
                      <div className="w-16 h-16 sm:w-20 sm:h-20 rounded-lg overflow-hidden bg-gray-100 shrink-0 border border-gray-200 relative">
                        {/* eslint-disable-next-line @next/next/no-img-element */}
                        <img
                          src={resolveImageUrl(currentPreview) || currentPreview}
                          alt={step.title || `Ảnh minh họa bước ${index + 1}`}
                          className="w-full h-full object-cover"
                          onError={(e) => {
                            (e.currentTarget as HTMLImageElement).style.display = 'none';
                          }}
                        />
                        {uploadingIndex === index && (
                          <div className="absolute inset-0 bg-black/40 flex items-center justify-center">
                            <span className="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin" />
                          </div>
                        )}
                      </div>

                      <div className="flex-1 min-w-0 text-xs">
                        <p
                          className="font-semibold text-gray-800 truncate"
                          title={step.imageFile ? step.imageFile.name : (step.imageUrl ?? '')}
                        >
                          {step.imageFile
                            ? `📁 ${step.imageFile.name} (${(step.imageFile.size / 1024).toFixed(0)} KB)`
                            : step.imageUrl}
                        </p>

                        {/* Status Badges */}
                        {uploadingIndex === index ? (
                          <p className="text-amber-600 font-medium text-[11px] mt-1 flex items-center gap-1">
                            <span className="w-1.5 h-1.5 rounded-full bg-amber-500 animate-pulse" />
                            <span>Đang tải ảnh lên MinIO...</span>
                          </p>
                        ) : step.imageFile ? (
                          <p className="text-blue-600 font-medium text-[11px] mt-1 flex items-center gap-1">
                            <span className="w-1.5 h-1.5 rounded-full bg-blue-500" />
                            <span>
                              {recipeId
                                ? 'Ảnh đã chọn từ máy tính'
                                : 'Ảnh từ máy (sẽ tải lên khi lưu nháp)'}
                            </span>
                          </p>
                        ) : (
                          <p className="text-emerald-700 font-medium text-[11px] mt-1 flex items-center gap-1">
                            <span className="w-1.5 h-1.5 rounded-full bg-emerald-600" />
                            <span>✓ Ảnh đã lưu trên máy chủ</span>
                          </p>
                        )}
                      </div>

                      <div className="flex items-center gap-1 shrink-0">
                        <button
                          type="button"
                          data-testid={`btn-change-step-image-${index}`}
                          onClick={() => {
                            document.getElementById(`step-file-${index}`)?.click();
                          }}
                          className="p-1.5 rounded-lg text-gray-500 hover:bg-gray-100 hover:text-gray-700 transition-colors cursor-pointer"
                          title="Thay ảnh khác từ máy"
                        >
                          🔄
                        </button>
                        <button
                          type="button"
                          onClick={() => handleRemoveImage(index)}
                          className="p-1.5 rounded-lg text-rose-500 hover:bg-rose-50 hover:text-rose-700 transition-colors cursor-pointer"
                          title="Gỡ ảnh này"
                        >
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                          </svg>
                        </button>
                      </div>
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        );
      })}
      <button
        className="small-button inline-flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-semibold text-emerald-800 bg-emerald-50 hover:bg-emerald-100 border border-emerald-200 transition-all cursor-pointer shadow-2xs"
        type="button"
        onClick={() => onChange([...steps, { title: '', description: '', timerMinutes: '', imageUrl: '' }])}
      >
        ＋ Thêm bước thực hiện
      </button>
    </div>
  );
};

const FullStepEditor: React.FC<FullStepEditorProps> = ({
  recipeId,
  authorId,
  initialSteps = [],
  onStepsChange,
  disabled: externalDisabled = false,
}) => {
  const isDirectApiMode = Boolean(recipeId && recipeId.trim() !== '');

  // Lấy thông tin người dùng để kiểm tra phân quyền Author/Admin
  const user = useAuthStore((state) => state.user);
  const isAuthenticated = Boolean(user);
  const checkPermissions = isDirectApiMode && Boolean(authorId);
  const isOwnerOrAdmin =
    !checkPermissions ||
    Boolean(user && (user.roles?.includes('Admin') || user.id === authorId));

  const [steps, setSteps] = useState<RecipeStep[]>(() =>
    [...initialSteps].sort((a, b) => (a.stepNumber ?? 0) - (b.stepNumber ?? 0))
  );

  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [isUploadingImage, setIsUploadingImage] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Trạng thái form Thêm / Sửa
  const [isFormOpen, setIsFormOpen] = useState<boolean>(false);
  const [editingStep, setEditingStep] = useState<RecipeStep | null>(null);
  const [formValues, setFormValues] = useState<StepFormValues>(INITIAL_FORM_VALUES);
  const [formErrors, setFormErrors] = useState<StepValidationError>({});

  // Input file ảnh minh họa cho bước
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Trạng thái modal xác nhận xóa
  const [stepToDelete, setStepToDelete] = useState<RecipeStep | null>(null);

  // Tự động load danh sách steps từ backend nếu có recipeId và chưa có initialSteps
  const fetchSteps = useCallback(() => {
    if (!isDirectApiMode) return;

    let cancelled = false;
    setIsLoading(true);
    setErrorMessage(null);

    getRecipeSteps(recipeId!)
      .then((fetchedSteps) => {
        if (!cancelled) {
          const sorted = [...fetchedSteps].sort(
            (a, b) => (a.stepNumber ?? 0) - (b.stepNumber ?? 0)
          );
          setSteps(sorted);
          onStepsChange?.(sorted);
        }
      })
      .catch((err) => {
        if (!cancelled) {
          setErrorMessage(
            err instanceof Error ? err.message : 'Không tải được danh sách bước thực hiện.'
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
  }, [isDirectApiMode, recipeId, onStepsChange]);

  useEffect(() => {
    if (isDirectApiMode && initialSteps.length === 0) {
      const cancel = fetchSteps();
      return cancel;
    }
  }, [isDirectApiMode, initialSteps.length, fetchSteps]);

  // Cập nhật khi initialSteps từ bên ngoài thay đổi
  useEffect(() => {
    if (initialSteps.length > 0) {
      const sorted = [...initialSteps].sort(
        (a, b) => (a.stepNumber ?? 0) - (b.stepNumber ?? 0)
      );
      setSteps(sorted);
    }
  }, [initialSteps]);

  // Cập nhật lên parent khi steps thay đổi trong Controlled mode
  const updateLocalSteps = useCallback(
    (newSteps: RecipeStep[]) => {
      setSteps(newSteps);
      onStepsChange?.(newSteps);
    },
    [onStepsChange]
  );

  const isActionDisabled = externalDisabled || isLoading || isUploadingImage || !isOwnerOrAdmin;

  // Validate form dữ liệu nhập
  const validateForm = (values: StepFormValues): StepValidationError => {
    const errors: StepValidationError = {};

    // Title: optional, max 200
    if (values.title && values.title.length > 200) {
      errors.title = 'Tiêu đề bước không được vượt quá 200 ký tự.';
    }

    // Description: required, max 2000
    const trimmedDesc = values.description.trim();
    if (!trimmedDesc) {
      errors.description = 'Nội dung hướng dẫn bước là bắt buộc.';
    } else if (trimmedDesc.length > 2000) {
      errors.description = 'Nội dung hướng dẫn không được vượt quá 2000 ký tự.';
    }

    // TimerMinutes: optional, 0 - 1440
    if (values.timerMinutes !== '' && values.timerMinutes !== undefined) {
      const minutesNum = Number(values.timerMinutes);
      if (Number.isNaN(minutesNum) || minutesNum < 0 || minutesNum > 1440) {
        errors.timerMinutes = 'Thời gian hẹn giờ phải từ 0 đến 1440 phút (24 giờ).';
      }
    }

    // ImageUrl: optional, max 500, valid url
    if (values.imageUrl && values.imageUrl.trim() !== '') {
      const trimmedUrl = values.imageUrl.trim();
      if (trimmedUrl.length > 500) {
        errors.imageUrl = 'Đường dẫn ảnh không được vượt quá 500 ký tự.';
      } else if (!isValidUrl(trimmedUrl)) {
        errors.imageUrl = 'Đường dẫn ảnh không hợp lệ (cần bắt đầu bằng http:// hoặc https://).';
      }
    }

    return errors;
  };

  // Xử lý chọn tệp hình ảnh cho bước
  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Validate client-side
    const validation = validateImageFile(file);
    if (!validation.isValid) {
      setFormErrors((prev) => ({
        ...prev,
        imageUrl: validation.errorMessage || 'Tệp hình ảnh không hợp lệ.',
      }));
      if (fileInputRef.current) fileInputRef.current.value = '';
      return;
    }

    // Clear error
    setFormErrors((prev) => ({ ...prev, imageUrl: undefined }));

    if (isDirectApiMode) {
      // Upload trực tiếp lên server
      setIsUploadingImage(true);
      try {
        const uploadResult = await uploadStepImageFile(recipeId!, file);
        setFormValues((prev) => ({
          ...prev,
          imageUrl: uploadResult.url,
        }));
      } catch (err) {
        setFormErrors((prev) => ({
          ...prev,
          imageUrl: err instanceof Error ? err.message : 'Tải ảnh lên máy chủ thất bại.',
        }));
      } finally {
        setIsUploadingImage(false);
        if (fileInputRef.current) fileInputRef.current.value = '';
      }
    } else {
      // Controlled mode (blob url preview)
      const blobUrl = URL.createObjectURL(file);
      setFormValues((prev) => ({
        ...prev,
        imageUrl: blobUrl,
      }));
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  // Mở form Thêm bước
  const handleOpenAddForm = () => {
    if (!isOwnerOrAdmin) return;
    setEditingStep(null);
    setFormValues(INITIAL_FORM_VALUES);
    setFormErrors({});
    setIsFormOpen(true);
  };

  // Mở form Sửa bước
  const handleOpenEditForm = (step: RecipeStep) => {
    if (!isOwnerOrAdmin) return;
    setEditingStep(step);
    setFormValues({
      title: step.title ?? '',
      description: step.description,
      timerMinutes:
        step.timerMinutes !== null && step.timerMinutes !== undefined
          ? String(step.timerMinutes)
          : '',
      imageUrl: step.imageUrl ?? '',
    });
    setFormErrors({});
    setIsFormOpen(true);
  };

  const handleCloseForm = () => {
    setIsFormOpen(false);
    setEditingStep(null);
    setFormValues(INITIAL_FORM_VALUES);
    setFormErrors({});
  };

  // Submit form (Thêm hoặc Cập nhật)
  const handleSubmitForm = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!isOwnerOrAdmin) return;
    setErrorMessage(null);

    const errors = validateForm(formValues);
    if (Object.keys(errors).length > 0) {
      setFormErrors(errors);
      return;
    }

    const payload = {
      title: formValues.title?.trim() || null,
      description: formValues.description.trim(),
      timerMinutes:
        formValues.timerMinutes !== '' && formValues.timerMinutes !== undefined
          ? Math.round(Number(formValues.timerMinutes))
          : null,
      imageUrl: formValues.imageUrl?.trim() || null,
    };

    if (isDirectApiMode) {
      // Direct API mode
      setIsLoading(true);
      try {
        if (editingStep) {
          // Update: Server giữ nguyên StepNumber
          const updated = await updateRecipeStep(recipeId!, editingStep.id, payload);
          const nextSteps = steps.map((s) => (s.id === updated.id ? updated : s));
          setSteps(nextSteps);
          onStepsChange?.(nextSteps);
        } else {
          // Create: Server tự cấp StepNumber = N + 1
          const created = await createRecipeStep(recipeId!, payload);
          const nextSteps = [...steps, created].sort(
            (a, b) => (a.stepNumber ?? 0) - (b.stepNumber ?? 0)
          );
          setSteps(nextSteps);
          onStepsChange?.(nextSteps);
        }
        handleCloseForm();
      } catch (err) {
        if (err instanceof StepApiError && err.errors) {
          const mappedErrors: StepValidationError = {};
          if (err.errors.Title?.[0]) mappedErrors.title = err.errors.Title[0];
          if (err.errors.Description?.[0]) mappedErrors.description = err.errors.Description[0];
          if (err.errors.TimerMinutes?.[0]) mappedErrors.timerMinutes = err.errors.TimerMinutes[0];
          if (err.errors.ImageUrl?.[0]) mappedErrors.imageUrl = err.errors.ImageUrl[0];
          setFormErrors(mappedErrors);
        }
        setErrorMessage(err instanceof Error ? err.message : 'Thao tác không thành công.');
      } finally {
        setIsLoading(false);
      }
    } else {
      // Controlled mode (chưa có recipeId)
      if (editingStep) {
        const nextSteps = steps.map((s) =>
          s.id === editingStep.id
            ? {
                ...s,
                title: payload.title,
                description: payload.description,
                timerMinutes: payload.timerMinutes,
                imageUrl: payload.imageUrl,
              }
            : s
        );
        updateLocalSteps(nextSteps);
      } else {
        const newStepNumber = steps.length + 1;
        const newLocalStep: RecipeStep = {
          id: `temp-${Date.now()}-${Math.random().toString(36).substring(2, 7)}`,
          recipeId: '',
          stepNumber: newStepNumber,
          title: payload.title,
          description: payload.description,
          timerMinutes: payload.timerMinutes,
          imageUrl: payload.imageUrl,
          createdAt: new Date().toISOString(),
        };
        updateLocalSteps([...steps, newLocalStep]);
      }
      handleCloseForm();
    }
  };

  // Yêu cầu xóa bước
  const handleDeleteRequest = (step: RecipeStep) => {
    if (!isOwnerOrAdmin) return;
    setStepToDelete(step);
  };

  // Xác nhận thực hiện xóa
  const handleConfirmDelete = async () => {
    if (!stepToDelete || !isOwnerOrAdmin) return;
    setErrorMessage(null);

    if (isDirectApiMode) {
      setIsLoading(true);
      try {
        await deleteRecipeStep(recipeId!, stepToDelete.id);
        // Sau khi xóa ở backend, server tự đánh số lại 1..N
        const remaining = steps
          .filter((s) => s.id !== stepToDelete.id)
          .map((s, idx) => ({ ...s, stepNumber: idx + 1 }));
        setSteps(remaining);
        onStepsChange?.(remaining);
        setStepToDelete(null);
      } catch (err) {
        setErrorMessage(err instanceof Error ? err.message : 'Không thể xóa bước thực hiện.');
      } finally {
        setIsLoading(false);
      }
    } else {
      // Controlled mode: xóa khỏi local và tự đánh số lại 1..N
      const remaining = steps
        .filter((s) => s.id !== stepToDelete.id)
        .map((s, idx) => ({ ...s, stepNumber: idx + 1 }));
      updateLocalSteps(remaining);
      setStepToDelete(null);
    }
  };

  // Reorder Up / Down
  const handleMoveStep = async (currentIndex: number, direction: 'up' | 'down') => {
    if (!isOwnerOrAdmin) return;
    const targetIndex = direction === 'up' ? currentIndex - 1 : currentIndex + 1;
    if (targetIndex < 0 || targetIndex >= steps.length) return;

    // Lưu trạng thái trước khi đổi để rollback nếu API lỗi
    const previousSteps = [...steps];

    // Hoán đổi vị trí trong mảng
    const reordered = [...steps];
    const [movedItem] = reordered.splice(currentIndex, 1);
    reordered.splice(targetIndex, 0, movedItem);

    // Cập nhật số thứ tự bước tuần tự 1..N
    const normalized = reordered.map((s, idx) => ({
      ...s,
      stepNumber: idx + 1,
    }));

    if (isDirectApiMode) {
      setIsLoading(true);
      setErrorMessage(null);
      try {
        const stepIds = normalized.map((s) => s.id);
        const serverResult = await reorderRecipeSteps(recipeId!, stepIds);
        const sortedServer = [...serverResult].sort(
          (a, b) => (a.stepNumber ?? 0) - (b.stepNumber ?? 0)
        );
        setSteps(sortedServer);
        onStepsChange?.(sortedServer);
      } catch (err) {
        // Rollback state nếu server trả lỗi
        setSteps(previousSteps);
        onStepsChange?.(previousSteps);
        setErrorMessage(
          err instanceof Error ? err.message : 'Không thể sắp xếp lại các bước.'
        );
      } finally {
        setIsLoading(false);
      }
    } else {
      // Controlled mode: chỉ cập nhật local state
      updateLocalSteps(normalized);
    }
  };

  return (
    <div className="space-y-6" data-testid="step-editor">
      {/* Read-only Warning Banner if not owner/admin */}
      {checkPermissions && (!isAuthenticated || !isOwnerOrAdmin) && (
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
              Chỉ tác giả sở hữu công thức này hoặc Quản trị viên mới có quyền thêm, chỉnh sửa hoặc sắp xếp thứ tự các bước thực hiện.
            </span>
          </div>
        </div>
      )}

      {/* Header section with title & Add button */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-5 sm:p-6 rounded-2xl bg-gradient-to-r from-emerald-50/90 via-emerald-50/50 to-white border border-emerald-100 shadow-xs">
        <div className="space-y-1">
          <div className="flex items-center gap-2.5">
            <span className="flex items-center justify-center w-8 h-8 rounded-xl bg-emerald-700 text-white shadow-2xs">
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01" />
              </svg>
            </span>
            <h3 className="text-lg sm:text-xl font-bold text-gray-900 tracking-tight">
              Các bước thực hiện
            </h3>
            <span className="text-xs font-semibold text-emerald-800 bg-emerald-100/90 border border-emerald-200/80 px-3 py-1 rounded-full shadow-2xs font-mono">
              {steps.length} bước
            </span>
          </div>
          <p className="text-xs sm:text-sm text-gray-600 pl-10.5">
            Sắp xếp trình tự các bước thực hiện món ăn mạch lạc, dễ hiểu.
          </p>
        </div>

        {isOwnerOrAdmin && (
          <button
            type="button"
            data-testid="btn-add-step"
            disabled={isActionDisabled || isFormOpen}
            onClick={handleOpenAddForm}
            className="inline-flex items-center justify-center gap-2 px-4 py-2.5 text-sm font-semibold rounded-xl text-white bg-emerald-700 hover:bg-emerald-800 active:bg-emerald-900 shadow-sm hover:shadow-md disabled:opacity-40 disabled:cursor-not-allowed transition-all duration-200 self-start sm:self-auto"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M12 4v16m8-8H4" />
            </svg>
            <span>Thêm bước mới</span>
          </button>
        )}
      </div>

      {/* Global Error Banner with Retry */}
      {errorMessage && (
        <div
          data-testid="step-error-banner"
          role="alert"
          className="flex items-start justify-between gap-3 p-4 rounded-xl bg-rose-50 border border-rose-200 text-rose-800 text-sm shadow-xs animate-in fade-in"
        >
          <div className="flex items-center gap-2.5 flex-1">
            <svg className="w-5 h-5 text-rose-600 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <span className="font-medium">{errorMessage}</span>
          </div>
          <div className="flex items-center gap-2">
            {isDirectApiMode && (
              <button
                type="button"
                data-testid="btn-retry-fetch-steps"
                onClick={fetchSteps}
                className="px-2.5 py-1 text-xs font-semibold rounded-lg bg-rose-100 text-rose-800 hover:bg-rose-200 transition-colors"
              >
                Thử lại
              </button>
            )}
            <button
              type="button"
              data-testid="btn-close-error"
              onClick={() => setErrorMessage(null)}
              className="text-rose-500 hover:text-rose-800 p-1 rounded-lg hover:bg-rose-100/50 transition-colors"
              aria-label="Đóng thông báo lỗi"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>
      )}

      {/* Loading Indicator */}
      {isLoading && (
        <div data-testid="step-loading-indicator" className="flex items-center justify-center gap-3 py-4 text-emerald-800 text-sm font-medium bg-emerald-50/50 rounded-xl border border-emerald-100">
          <svg className="animate-spin h-4 w-4 text-emerald-700" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
          </svg>
          <span>Đang lưu trữ dữ liệu bước thực hiện...</span>
        </div>
      )}

      {/* Inline Add / Edit Form */}
      {isFormOpen && (
        <form
          data-testid="step-form"
          onSubmit={handleSubmitForm}
          noValidate
          className="p-6 sm:p-7 rounded-2xl bg-white border-2 border-emerald-200/90 shadow-md space-y-5 animate-in fade-in"
        >
          <div className="flex items-center justify-between pb-3.5 border-b border-emerald-100">
            <div className="flex items-center gap-2.5">
              <span className="w-2.5 h-2.5 rounded-full bg-emerald-600 ring-4 ring-emerald-100" />
              <h4 className="text-base font-bold text-gray-900 tracking-tight">
                {editingStep ? `Chỉnh sửa bước ${String(editingStep.stepNumber).padStart(2, '0')}` : 'Soạn thảo bước nấu mới'}
              </h4>
            </div>
            <button
              type="button"
              onClick={handleCloseForm}
              disabled={isActionDisabled}
              className="text-gray-400 hover:text-gray-700 p-1.5 rounded-xl hover:bg-gray-100 transition-colors"
              aria-label="Đóng form"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 sm:gap-5">
            {/* Title Field (Optional, max 200) */}
            <div className="md:col-span-2 space-y-1.5">
              <div className="flex justify-between items-center text-xs">
                <label htmlFor="step-input-title" className="font-semibold text-gray-800">
                  Tiêu đề bước <span className="text-gray-400 font-normal">(tùy chọn)</span>
                </label>
                <span className="text-gray-400 font-mono text-[11px]">
                  {formValues.title.length}/200
                </span>
              </div>
              <input
                id="step-input-title"
                data-testid="input-step-title"
                type="text"
                maxLength={200}
                value={formValues.title}
                onChange={(e) => {
                  setFormValues({ ...formValues, title: e.target.value });
                  if (formErrors.title) setFormErrors({ ...formErrors, title: undefined });
                }}
                disabled={isActionDisabled}
                placeholder="Ví dụ: Sơ chế và ướp thịt bò"
                className="w-full px-3.5 py-2.5 text-sm rounded-xl border border-gray-300 bg-white text-gray-900 placeholder:text-gray-400 focus:outline-none focus:border-emerald-600 focus:ring-2 focus:ring-emerald-500/20 disabled:opacity-50 transition-all shadow-2xs"
              />
              {formErrors.title && (
                <p data-testid="error-step-title" className="text-xs text-rose-600 font-medium mt-1 flex items-center gap-1">
                  <span>⚠</span> {formErrors.title}
                </p>
              )}
            </div>

            {/* Description Field (Required, max 2000) */}
            <div className="md:col-span-2 space-y-1.5">
              <div className="flex justify-between items-center text-xs">
                <label htmlFor="step-input-description" className="font-semibold text-gray-800">
                  Hướng dẫn thực hiện <span className="text-rose-500">*</span>
                </label>
                <span className="text-gray-400 font-mono text-[11px]">
                  {formValues.description.length}/2000
                </span>
              </div>
              <textarea
                id="step-input-description"
                data-testid="input-step-description"
                rows={4}
                maxLength={2000}
                value={formValues.description}
                onChange={(e) => {
                  setFormValues({ ...formValues, description: e.target.value });
                  if (formErrors.description) setFormErrors({ ...formErrors, description: undefined });
                }}
                disabled={isActionDisabled}
                placeholder="Mô tả chi tiết các thao tác thực hiện trong bước này..."
                className="w-full px-3.5 py-2.5 text-sm rounded-xl border border-gray-300 bg-white text-gray-900 placeholder:text-gray-400 focus:outline-none focus:border-emerald-600 focus:ring-2 focus:ring-emerald-500/20 disabled:opacity-50 transition-all shadow-2xs"
              />
              {formErrors.description && (
                <p data-testid="error-step-description" className="text-xs text-rose-600 font-medium mt-1 flex items-center gap-1">
                  <span>⚠</span> {formErrors.description}
                </p>
              )}
            </div>

            {/* Timer Minutes (Optional, 0 - 1440) */}
            <div className="space-y-1.5">
              <label htmlFor="step-input-timer" className="block text-xs font-semibold text-gray-800">
                Thời gian hẹn giờ (phút) <span className="text-gray-400 font-normal">(0-1440)</span>
              </label>
              <div className="relative">
                <input
                  id="step-input-timer"
                  data-testid="input-step-timer"
                  type="number"
                  min="0"
                  max="1440"
                  step="1"
                  value={formValues.timerMinutes}
                  onChange={(e) => {
                    setFormValues({ ...formValues, timerMinutes: e.target.value });
                    if (formErrors.timerMinutes) setFormErrors({ ...formErrors, timerMinutes: undefined });
                  }}
                  disabled={isActionDisabled}
                  placeholder="Ví dụ: 15"
                  className="w-full pl-3.5 pr-10 py-2.5 text-sm rounded-xl border border-gray-300 bg-white text-gray-900 placeholder:text-gray-400 focus:outline-none focus:border-emerald-600 focus:ring-2 focus:ring-emerald-500/20 disabled:opacity-50 transition-all shadow-2xs"
                />
                <span className="absolute right-3.5 top-1/2 -translate-y-1/2 text-gray-400 text-xs pointer-events-none">
                  phút
                </span>
              </div>
              {formErrors.timerMinutes && (
                <p data-testid="error-step-timer" className="text-xs text-rose-600 font-medium mt-1 flex items-center gap-1">
                  <span>⚠</span> {formErrors.timerMinutes}
                </p>
              )}
            </div>

            {/* Image Upload / URL Section */}
            <div className="space-y-1.5">
              <div className="flex items-center justify-between">
                <label htmlFor="step-input-image" className="block text-xs font-semibold text-gray-800">
                  Ảnh minh họa <span className="text-gray-400 font-normal">(tùy chọn)</span>
                </label>
                <button
                  type="button"
                  data-testid="btn-upload-step-image"
                  disabled={isActionDisabled}
                  onClick={() => fileInputRef.current?.click()}
                  className="text-xs font-semibold text-emerald-700 hover:text-emerald-800 hover:underline flex items-center"
                >
                  <span>{isUploadingImage ? 'Đang tải...' : 'Tải ảnh từ máy'}</span>
                </button>
              </div>

              {/* Hidden file input */}
              <input
                ref={fileInputRef}
                type="file"
                data-testid="input-step-file"
                accept="image/jpeg,image/png,image/webp,image/avif"
                disabled={isActionDisabled}
                onChange={handleFileChange}
                className="hidden"
              />

              <input
                id="step-input-image"
                data-testid="input-step-image"
                type="url"
                maxLength={500}
                value={formValues.imageUrl}
                onChange={(e) => {
                  setFormValues({ ...formValues, imageUrl: e.target.value });
                  if (formErrors.imageUrl) setFormErrors({ ...formErrors, imageUrl: undefined });
                }}
                disabled={isActionDisabled}
                placeholder="https://example.com/step-1.jpg"
                className="w-full px-3.5 py-2.5 text-sm rounded-xl border border-gray-300 bg-white text-gray-900 placeholder:text-gray-400 focus:outline-none focus:border-emerald-600 focus:ring-2 focus:ring-emerald-500/20 disabled:opacity-50 transition-all shadow-2xs"
              />

              {formValues.imageUrl && (
                <div className="flex items-center gap-2 pt-1">
                  <div className="w-8 h-8 rounded-lg overflow-hidden border border-emerald-200 bg-slate-50 shrink-0">
                    {/* eslint-disable-next-line @next/next/no-img-element */}
                    <img
                      src={formValues.imageUrl}
                      alt="Minh họa bước"
                      className="w-full h-full object-cover"
                      onError={(e) => {
                        (e.currentTarget as HTMLImageElement).style.display = 'none';
                      }}
                    />
                  </div>
                  <button
                    type="button"
                    data-testid="btn-remove-step-image"
                    onClick={() => setFormValues((prev) => ({ ...prev, imageUrl: '' }))}
                    className="text-xs text-rose-600 hover:text-rose-800 font-medium hover:underline"
                  >
                    Gỡ ảnh
                  </button>
                </div>
              )}

              {formErrors.imageUrl && (
                <p data-testid="error-step-image" className="text-xs text-rose-600 font-medium mt-1 flex items-center gap-1">
                  <span>⚠</span> {formErrors.imageUrl}
                </p>
              )}
            </div>
          </div>

          {/* Form Actions */}
          <div className="flex items-center justify-end gap-3 pt-3 border-t border-emerald-100">
            <button
              type="button"
              data-testid="btn-cancel-step-form"
              disabled={isActionDisabled}
              onClick={handleCloseForm}
              className="px-5 py-2.5 text-xs font-semibold rounded-xl text-gray-700 bg-white hover:bg-gray-50 border border-gray-300 disabled:opacity-50 transition-colors"
            >
              Hủy bỏ
            </button>
            <button
              type="submit"
              data-testid="btn-save-step"
              disabled={isActionDisabled}
              className="inline-flex items-center gap-2 px-5 py-2.5 text-xs font-semibold rounded-xl text-white bg-emerald-700 hover:bg-emerald-800 active:bg-emerald-900 shadow-sm hover:shadow-md disabled:opacity-50 transition-all"
            >
              <span>{editingStep ? 'Lưu cập nhật' : 'Lưu bước'}</span>
            </button>
          </div>
        </form>
      )}

      {/* Steps List or Empty State */}
      {steps.length === 0 ? (
        <div
          data-testid="step-empty-state"
          className="flex flex-col items-center justify-center p-10 sm:p-12 rounded-2xl border-2 border-dashed border-emerald-200 bg-emerald-50/40 text-center space-y-3"
        >
          <div className="w-14 h-14 rounded-2xl bg-emerald-100 text-emerald-800 flex items-center justify-center shadow-xs ring-4 ring-emerald-50">
            <svg className="w-7 h-7" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.75} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
            </svg>
          </div>
          <div className="space-y-1">
            <h4 className="text-base font-bold text-gray-900">
              Chưa có bước thực hiện nào
            </h4>
            <p className="text-xs sm:text-sm text-gray-600 max-w-sm leading-relaxed">
              {isOwnerOrAdmin
                ? 'Hãy bấm nút "Thêm bước mới" ở trên để bắt đầu soạn thảo các thao tác nấu nướng cụ thể cho món ăn.'
                : 'Công thức này chưa có bước thực hiện nào được công bố.'}
            </p>
          </div>
        </div>
      ) : (
        <div data-testid="step-list" className="space-y-3.5">
          {steps.map((step, index) => (
            <StepItem
              key={step.id}
              step={step}
              index={index}
              totalSteps={steps.length}
              disabled={isActionDisabled}
              canEdit={isOwnerOrAdmin}
              canDelete={isOwnerOrAdmin}
              onEdit={handleOpenEditForm}
              onDeleteRequest={handleDeleteRequest}
              onMoveUp={(idx) => handleMoveStep(idx, 'up')}
              onMoveDown={(idx) => handleMoveStep(idx, 'down')}
            />
          ))}
        </div>
      )}

      {/* Delete Confirmation Modal */}
      {stepToDelete && (
        <div
          data-testid="delete-confirmation-dialog"
          role="dialog"
          aria-modal="true"
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs animate-in fade-in"
        >
          <div className="w-full max-w-md p-6 rounded-2xl bg-white border border-gray-100 shadow-2xl space-y-4">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-rose-50 text-rose-600 flex items-center justify-center shrink-0">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                </svg>
              </div>
              <h4 className="text-lg font-bold text-gray-900 tracking-tight">
                Xác nhận xóa bước thực hiện
              </h4>
            </div>

            <p className="text-sm text-gray-600 leading-relaxed">
              Bạn có chắc chắn muốn xóa bước{' '}
              <strong className="text-gray-900 font-semibold">{String(stepToDelete.stepNumber).padStart(2, '0')}</strong>
              {stepToDelete.title ? ` (${stepToDelete.title})` : ''}? Thao tác này sẽ tự động đánh số lại các bước còn lại từ 1 đến N.
            </p>

            <div className="flex items-center justify-end gap-3 pt-2">
              <button
                type="button"
                data-testid="btn-cancel-delete"
                disabled={isActionDisabled}
                onClick={() => setStepToDelete(null)}
                className="px-4 py-2 text-xs font-semibold rounded-xl text-gray-700 bg-white hover:bg-gray-100 border border-gray-300 disabled:opacity-50 transition-colors"
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                data-testid="btn-confirm-delete"
                disabled={isActionDisabled}
                onClick={handleConfirmDelete}
                className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold rounded-xl text-white bg-rose-600 hover:bg-rose-700 active:bg-rose-800 shadow-sm disabled:opacity-50 transition-colors"
              >
                {isLoading ? 'Đang xóa...' : 'Xóa bước'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export const StepEditor: React.FC<StepEditorProps> = ({
  steps,
  onChange,
  ...restProps
}) => {
  if (steps && onChange) {
    return <DraftStepEditor steps={steps} onChange={onChange} />;
  }
  return <FullStepEditor {...restProps} />;
};

export default StepEditor;
