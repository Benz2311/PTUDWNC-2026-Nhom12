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

  const [steps, setSteps] = useState<RecipeStep[]>(() => {
    // Sắp xếp initial steps theo StepNumber tăng dần
    return [...initialSteps].sort((a, b) => (a.stepNumber ?? 0) - (b.stepNumber ?? 0));
  });

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
      if (isNaN(minutesNum) || minutesNum < 0 || minutesNum > 1440) {
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
    <div className="space-y-5" data-testid="step-editor">
      {/* Header section with title & Add button */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b border-gray-200">
        <div>
          <h3 className="text-lg font-bold text-gray-900 flex items-center gap-2">
            <span>Các bước thực hiện</span>
            <span className="text-xs font-normal text-gray-600 bg-gray-100 px-2.5 py-0.5 rounded-full">
              {steps.length} bước
            </span>
          </h3>
          <p className="text-xs text-gray-500 mt-0.5">
            Thêm và sắp xếp các bước theo trình tự chuẩn bị món ăn.
          </p>
        </div>

        <button
          type="button"
          data-testid="btn-add-step"
          disabled={isActionDisabled || isFormOpen}
          onClick={handleOpenAddForm}
          className="inline-flex items-center justify-center gap-1.5 px-4 py-2 text-sm font-medium rounded-lg text-white bg-green-800 hover:bg-green-900 active:bg-green-950 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
        >
          <span className="text-base leading-none">+</span>
          <span>Thêm bước</span>
        </button>
      </div>

      {/* Global Error Banner */}
      {errorMessage && (
        <div
          data-testid="step-error-banner"
          role="alert"
          className="flex items-start justify-between gap-3 p-3.5 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm"
        >
          <div className="flex items-center gap-2">
            <svg className="w-4 h-4 text-red-600 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <span>{errorMessage}</span>
          </div>
          <button
            type="button"
            data-testid="btn-close-error"
            onClick={() => setErrorMessage(null)}
            className="text-red-500 hover:text-red-800 p-0.5 text-base leading-none"
            aria-label="Đóng thông báo lỗi"
          >
            ×
          </button>
        </div>
      )}

      {/* Loading Indicator */}
      {isLoading && (
        <div data-testid="step-loading-indicator" className="flex items-center justify-center gap-2 py-3 text-gray-600 text-sm">
          <svg className="animate-spin h-4 w-4 text-green-800" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
          </svg>
          <span>Đang xử lý bước thực hiện...</span>
        </div>
      )}

      {/* Inline Add / Edit Form */}
      {isFormOpen && (
        <form
          data-testid="step-form"
          onSubmit={handleSubmitForm}
          noValidate
          className="p-5 rounded-xl bg-white border border-gray-200 shadow-xs space-y-4"
        >
          <div className="flex items-center justify-between pb-2.5 border-b border-gray-100">
            <h4 className="text-sm font-semibold text-gray-900">
              {editingStep ? `Chỉnh sửa bước ${String(editingStep.stepNumber).padStart(2, '0')}` : 'Thêm bước mới'}
            </h4>
            <button
              type="button"
              onClick={handleCloseForm}
              disabled={isActionDisabled}
              className="text-gray-400 hover:text-gray-600 text-lg leading-none"
              aria-label="Đóng form"
            >
              ×
            </button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* Title Field (Optional, max 200) */}
            <div className="md:col-span-2 space-y-1">
              <div className="flex justify-between items-center text-xs">
                <label htmlFor="step-input-title" className="font-medium text-gray-700">
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
                className="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 bg-white text-gray-900 placeholder:text-gray-400 focus:outline-none focus:border-green-800 focus:ring-1 focus:ring-green-800 disabled:opacity-50"
              />
              {formErrors.title && (
                <p data-testid="error-step-title" className="text-xs text-red-600 mt-1">
                  {formErrors.title}
                </p>
              )}
            </div>

            {/* Description Field (Required, max 2000) */}
            <div className="md:col-span-2 space-y-1">
              <div className="flex justify-between items-center text-xs">
                <label htmlFor="step-input-description" className="font-medium text-gray-700">
                  Hướng dẫn thực hiện <span className="text-red-500">*</span>
                </label>
                <span className="text-gray-400 font-mono text-[11px]">
                  {formValues.description.length}/2000
                </span>
              </div>
              <textarea
                id="step-input-description"
                data-testid="input-step-description"
                rows={3}
                maxLength={2000}
                value={formValues.description}
                onChange={(e) => {
                  setFormValues({ ...formValues, description: e.target.value });
                  if (formErrors.description) setFormErrors({ ...formErrors, description: undefined });
                }}
                disabled={isActionDisabled}
                placeholder="Mô tả chi tiết các thao tác thực hiện trong bước này..."
                className="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 bg-white text-gray-900 placeholder:text-gray-400 focus:outline-none focus:border-green-800 focus:ring-1 focus:ring-green-800 disabled:opacity-50"
              />
              {formErrors.description && (
                <p data-testid="error-step-description" className="text-xs text-red-600 mt-1">
                  {formErrors.description}
                </p>
              )}
            </div>

            {/* Timer Minutes (Optional, 0 - 1440) */}
            <div className="space-y-1">
              <label htmlFor="step-input-timer" className="block text-xs font-medium text-gray-700">
                Thời gian (phút) <span className="text-gray-400 font-normal">(0-1440)</span>
              </label>
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
                className="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 bg-white text-gray-900 placeholder:text-gray-400 focus:outline-none focus:border-green-800 focus:ring-1 focus:ring-green-800 disabled:opacity-50"
              />
              {formErrors.timerMinutes && (
                <p data-testid="error-step-timer" className="text-xs text-red-600 mt-1">
                  {formErrors.timerMinutes}
                </p>
              )}
            </div>

            {/* Image URL (Optional, max 2048) */}
            <div className="space-y-1">
              <label htmlFor="step-input-image" className="block text-xs font-medium text-gray-700">
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
                className="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 bg-white text-gray-900 placeholder:text-gray-400 focus:outline-none focus:border-green-800 focus:ring-1 focus:ring-green-800 disabled:opacity-50"
              />
              {formErrors.imageUrl && (
                <p data-testid="error-step-image" className="text-xs text-red-600 mt-1">
                  {formErrors.imageUrl}
                </p>
              )}
            </div>
          </div>

          {/* Form Actions */}
          <div className="flex items-center justify-end gap-2 pt-2 border-t border-gray-100">
            <button
              type="button"
              data-testid="btn-cancel-step-form"
              disabled={isActionDisabled}
              onClick={handleCloseForm}
              className="px-4 py-2 text-xs font-medium rounded-lg text-gray-700 bg-white hover:bg-gray-50 border border-gray-300 disabled:opacity-50 transition-colors"
            >
              Hủy
            </button>
            <button
              type="submit"
              data-testid="btn-save-step"
              disabled={isActionDisabled}
              className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-medium rounded-lg text-white bg-green-800 hover:bg-green-900 disabled:opacity-50 transition-colors"
            >
              {editingStep ? 'Lưu cập nhật' : 'Lưu bước'}
            </button>
          </div>
        </form>
      )}

      {/* Steps List or Empty State */}
      {steps.length === 0 ? (
        <div
          data-testid="step-empty-state"
          className="flex flex-col items-center justify-center p-8 rounded-xl border border-dashed border-gray-300 bg-gray-50/50 text-center"
        >
          <div className="w-10 h-10 rounded-full bg-green-50 flex items-center justify-center text-green-800 mb-2.5">
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.75} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
            </svg>
          </div>
          <h4 className="text-sm font-semibold text-gray-800">
            Chưa có bước thực hiện nào
          </h4>
          <p className="text-xs text-gray-500 mt-1 max-w-sm">
            Công thức nấu ăn cần có các bước hướng dẫn cụ thể. Hãy bấm nút &ldquo;Thêm bước&rdquo; ở trên để bắt đầu soạn thảo.
          </p>
        </div>
      ) : (
        <div data-testid="step-list" className="space-y-3">
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
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40"
        >
          <div className="w-full max-w-md p-5 sm:p-6 rounded-xl bg-white border border-gray-200 shadow-lg space-y-3.5">
            <div className="flex items-center gap-2.5 text-red-600">
              <svg className="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
              <h4 className="text-base font-semibold text-gray-900">
                Xác nhận xóa bước thực hiện
              </h4>
            </div>

            <p className="text-sm text-gray-600 leading-relaxed">
              Bạn có chắc chắn muốn xóa bước{' '}
              <strong>{String(stepToDelete.stepNumber).padStart(2, '0')}</strong>: &ldquo;
              {stepToDelete.title || stepToDelete.description.slice(0, 30)}...&rdquo; không?
              Thao tác này sẽ tự động đánh số lại các bước còn lại.
            </p>

            <div className="flex items-center justify-end gap-2.5 pt-2">
              <button
                type="button"
                data-testid="btn-cancel-delete"
                disabled={isActionDisabled}
                onClick={() => setStepToDelete(null)}
                className="px-4 py-2 text-xs font-medium rounded-lg text-gray-700 bg-white hover:bg-gray-50 border border-gray-300 disabled:opacity-50 transition-colors"
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                data-testid="btn-confirm-delete"
                disabled={isActionDisabled}
                onClick={handleConfirmDelete}
                className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-medium rounded-lg text-white bg-red-600 hover:bg-red-700 disabled:opacity-50 transition-colors"
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
