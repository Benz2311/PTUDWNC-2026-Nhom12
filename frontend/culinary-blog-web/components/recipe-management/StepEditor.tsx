'use client';

import React, { useState, useEffect, useCallback } from 'react';
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
  StepApiError,
} from '@/lib/api/steps';
import { StepItem } from './StepItem';

export interface StepEditorProps {
  /**
   * Nếu có recipeId: Chế độ Direct API mode (CRUD trực tiếp với Backend).
   * Nếu không có recipeId: Chế độ Controlled mode (Quản lý local state cho Recipe mới).
   */
  recipeId?: string;
  /**
   * Danh sách bước ban đầu (dùng khi render RecipeForm hoặc edit trang)
   */
  initialSteps?: RecipeStep[];
  /**
   * Callback thông báo cho form cha khi danh sách bước thay đổi (đặc biệt trong Controlled mode)
   */
  onStepsChange?: (steps: RecipeStep[]) => void;
  /**
   * Cho phép vô hiệu hóa toàn bộ editor từ form cha (ví dụ khi đang lưu Recipe)
   */
  disabled?: boolean;
}

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

export const StepEditor: React.FC<StepEditorProps> = ({
  recipeId,
  initialSteps = [],
  onStepsChange,
  disabled: externalDisabled = false,
}) => {
  const isDirectApiMode = Boolean(recipeId && recipeId.trim() !== '');

  const [steps, setSteps] = useState<RecipeStep[]>(() => 
    // Sắp xếp initial steps theo StepNumber tăng dần
     [...initialSteps].sort((a, b) => (a.stepNumber ?? 0) - (b.stepNumber ?? 0))
  );

  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Trạng thái form Thêm / Sửa
  const [isFormOpen, setIsFormOpen] = useState<boolean>(false);
  const [editingStep, setEditingStep] = useState<RecipeStep | null>(null);
  const [formValues, setFormValues] = useState<StepFormValues>(INITIAL_FORM_VALUES);
  const [formErrors, setFormErrors] = useState<StepValidationError>({});

  // Trạng thái modal xác nhận xóa
  const [stepToDelete, setStepToDelete] = useState<RecipeStep | null>(null);

  // Tự động load danh sách steps từ backend nếu có recipeId và chưa có initialSteps
  useEffect(() => {
    let cancelled = false;

    if (isDirectApiMode && initialSteps.length === 0) {
      setIsLoading(true);
      setErrorMessage(null);

      getRecipeSteps(recipeId!)
        .then((fetchedSteps) => {
          if (!cancelled) {
            const sorted = [...fetchedSteps].sort((a, b) => (a.stepNumber ?? 0) - (b.stepNumber ?? 0));
            setSteps(sorted);
            onStepsChange?.(sorted);
          }
        })
        .catch((err) => {
          if (!cancelled) {
            setErrorMessage(err instanceof Error ? err.message : 'Không tải được danh sách bước thực hiện.');
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

  // Cập nhật khi initialSteps từ bên ngoài thay đổi (nếu có)
  useEffect(() => {
    if (initialSteps.length > 0) {
      const sorted = [...initialSteps].sort((a, b) => (a.stepNumber ?? 0) - (b.stepNumber ?? 0));
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

  const isActionDisabled = externalDisabled || isLoading;

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

    // ImageUrl: optional, max 2048, valid url
    if (values.imageUrl && values.imageUrl.trim() !== '') {
      const trimmedUrl = values.imageUrl.trim();
      if (trimmedUrl.length > 2048) {
        errors.imageUrl = 'Đường dẫn ảnh không được vượt quá 2048 ký tự.';
      } else if (!isValidUrl(trimmedUrl)) {
        errors.imageUrl = 'Đường dẫn ảnh không hợp lệ (cần bắt đầu bằng http:// hoặc https://).';
      }
    }

    return errors;
  };

  // Mở form Thêm bước
  const handleOpenAddForm = () => {
    setEditingStep(null);
    setFormValues(INITIAL_FORM_VALUES);
    setFormErrors({});
    setIsFormOpen(true);
  };

  // Mở form Sửa bước
  const handleOpenEditForm = (step: RecipeStep) => {
    setEditingStep(step);
    setFormValues({
      title: step.title ?? '',
      description: step.description,
      timerMinutes: step.timerMinutes !== null && step.timerMinutes !== undefined ? String(step.timerMinutes) : '',
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
          const nextSteps = [...steps, created].sort((a, b) => (a.stepNumber ?? 0) - (b.stepNumber ?? 0));
          setSteps(nextSteps);
          onStepsChange?.(nextSteps);
        }
        handleCloseForm();
      } catch (err) {
        if (err instanceof StepApiError && err.errors) {
          // Hiển thị validation errors từ server nếu có
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
    setStepToDelete(step);
  };

  // Xác nhận thực hiện xóa
  const handleConfirmDelete = async () => {
    if (!stepToDelete) return;
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
    const targetIndex = direction === 'up' ? currentIndex - 1 : currentIndex + 1;
    if (targetIndex < 0 || targetIndex >= steps.length) return;

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
        const sortedServer = [...serverResult].sort((a, b) => (a.stepNumber ?? 0) - (b.stepNumber ?? 0));
        setSteps(sortedServer);
        onStepsChange?.(sortedServer);
      } catch (err) {
        setErrorMessage(err instanceof Error ? err.message : 'Không thể sắp xếp lại các bước.');
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
      </div>

      {/* Global Error Banner */}
      {errorMessage && (
        <div
          data-testid="step-error-banner"
          role="alert"
          className="flex items-start justify-between gap-3 p-4 rounded-xl bg-rose-50 border border-rose-200 text-rose-800 text-sm shadow-xs animate-in fade-in"
        >
          <div className="flex items-center gap-2.5">
            <svg className="w-5 h-5 text-rose-600 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <span className="font-medium">{errorMessage}</span>
          </div>
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

            {/* Image URL (Optional, max 2048) */}
            <div className="space-y-1.5">
              <label htmlFor="step-input-image" className="block text-xs font-semibold text-gray-800">
                URL ảnh minh họa <span className="text-gray-400 font-normal">(tùy chọn)</span>
              </label>
              <input
                id="step-input-image"
                data-testid="input-step-image"
                type="url"
                maxLength={2048}
                value={formValues.imageUrl}
                onChange={(e) => {
                  setFormValues({ ...formValues, imageUrl: e.target.value });
                  if (formErrors.imageUrl) setFormErrors({ ...formErrors, imageUrl: undefined });
                }}
                disabled={isActionDisabled}
                placeholder="https://example.com/step-1.jpg"
                className="w-full px-3.5 py-2.5 text-sm rounded-xl border border-gray-300 bg-white text-gray-900 placeholder:text-gray-400 focus:outline-none focus:border-emerald-600 focus:ring-2 focus:ring-emerald-500/20 disabled:opacity-50 transition-all shadow-2xs"
              />
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
              Hãy bấm nút &ldquo;Thêm bước&rdquo; ở trên để bắt đầu soạn thảo các thao tác nấu nướng cụ thể cho món ăn.
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

export default StepEditor;
