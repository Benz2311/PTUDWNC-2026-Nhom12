'use client';

import React, { useState } from 'react';
import type { RecipeStep } from '@/types/step';

export interface StepItemProps {
  step: RecipeStep;
  index: number;
  totalSteps: number;
  disabled?: boolean;
  canEdit?: boolean;
  canDelete?: boolean;
  onEdit: (step: RecipeStep) => void;
  onDeleteRequest: (step: RecipeStep) => void;
  onMoveUp: (index: number) => void;
  onMoveDown: (index: number) => void;
}

export function StepItem({
  step,
  index,
  totalSteps,
  disabled = false,
  canEdit = true,
  canDelete = true,
  onEdit,
  onDeleteRequest,
  onMoveUp,
  onMoveDown,
}: StepItemProps) {
  const [imageError, setImageError] = useState(false);
  const isFirst = index === 0;
  const isLast = index === totalSteps - 1;

  // Format step number with leading zero (e.g. 01, 02)
  const formattedNumber = String(step.stepNumber ?? index + 1).padStart(2, '0');

  const isEditDisabled = disabled || !canEdit;
  const isDeleteDisabled = disabled || !canDelete;
  const isMoveUpDisabled = disabled || isFirst || !canEdit;
  const isMoveDownDisabled = disabled || isLast || !canEdit;

  return (
    <div
      data-testid={`step-item-${index}`}
      className="group relative flex flex-col md:flex-row items-start gap-4 p-5 sm:p-6 rounded-2xl bg-white border border-emerald-100/80 shadow-xs hover:shadow-md hover:border-emerald-300 transition-all duration-200"
    >
      {/* Step Number Badge & Reorder Arrows */}
      <div className="flex md:flex-col items-center justify-between w-full md:w-auto gap-2.5 shrink-0">
        <div className="flex items-center justify-center w-11 h-11 rounded-xl bg-gradient-to-br from-emerald-700 via-emerald-800 to-emerald-900 text-white font-bold text-base font-mono shadow-xs ring-2 ring-emerald-100">
          <span>{formattedNumber}</span>
        </div>

        {/* Up / Down Reorder buttons */}
        <div className="flex md:flex-col gap-1 bg-emerald-50/60 p-1 rounded-xl border border-emerald-100/70">
          <button
            type="button"
            data-testid={`btn-move-up-${index}`}
            disabled={isMoveUpDisabled}
            onClick={() => onMoveUp(index)}
            aria-label={`Di chuyển bước ${formattedNumber} lên trên`}
            title="Di chuyển lên trên"
            className="p-1.5 rounded-lg text-emerald-800 hover:text-emerald-950 hover:bg-emerald-100/80 disabled:opacity-30 disabled:hover:bg-transparent disabled:cursor-not-allowed transition-colors"
          >
            <svg
              className="w-4 h-4"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              xmlns="http://www.w3.org/2000/svg"
            >
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.2} d="M5 15l7-7 7 7" />
            </svg>
          </button>

          <button
            type="button"
            data-testid={`btn-move-down-${index}`}
            disabled={isMoveDownDisabled}
            onClick={() => onMoveDown(index)}
            aria-label={`Di chuyển bước ${formattedNumber} xuống dưới`}
            title="Di chuyển xuống dưới"
            className="p-1.5 rounded-lg text-emerald-800 hover:text-emerald-950 hover:bg-emerald-100/80 disabled:opacity-30 disabled:hover:bg-transparent disabled:cursor-not-allowed transition-colors"
          >
            <svg
              className="w-4 h-4"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              xmlns="http://www.w3.org/2000/svg"
            >
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.2} d="M19 9l-7 7-7-7" />
            </svg>
          </button>
        </div>
      </div>

      {/* Main Content Area */}
      <div className="flex-1 min-w-0 space-y-2.5 w-full">
        <div className="flex flex-wrap items-center gap-2.5">
          {step.title ? (
            <h4
              data-testid={`step-title-${index}`}
              className="text-base sm:text-lg font-bold text-gray-900 tracking-tight break-words"
            >
              {step.title}
            </h4>
          ) : (
            <h4 className="text-sm font-semibold text-emerald-800/60 italic">
              Bước {formattedNumber}
            </h4>
          )}

          {step.timerMinutes !== null && step.timerMinutes !== undefined && step.timerMinutes > 0 && (
            <span
              data-testid={`step-timer-${index}`}
              className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-amber-50 text-amber-900 border border-amber-200/80 shadow-2xs"
            >
              <svg className="w-3.5 h-3.5 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <circle cx="12" cy="12" r="10" strokeWidth="2" />
                <polyline points="12 6 12 12 16 14" strokeWidth="2" />
              </svg>
              <span>{step.timerMinutes} phút</span>
            </span>
          )}
        </div>

        <p
          data-testid={`step-description-${index}`}
          className="text-sm text-gray-700 whitespace-pre-line leading-relaxed break-words font-normal"
        >
          {step.description}
        </p>

        {step.imageUrl && (
          <div className="mt-3 overflow-hidden rounded-xl border border-emerald-100 max-w-sm bg-gray-50 shadow-2xs group/img">
            {imageError ? (
              <div className="flex items-center gap-2 p-3 text-xs text-slate-500 bg-slate-50">
                <svg className="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                </svg>
                <span>Không thể tải hình ảnh minh họa</span>
              </div>
            ) : (
              /* eslint-disable-next-line @next/next/no-img-element */
              <img
                src={step.imageUrl}
                alt={step.title || `Ảnh minh họa bước ${formattedNumber}`}
                className="w-full h-36 sm:h-40 object-cover transition-transform duration-300 group-hover/img:scale-105"
                onError={() => setImageError(true)}
              />
            )}
          </div>
        )}
      </div>

      {/* Action Buttons: Edit & Delete */}
      <div className="flex items-center justify-end gap-2 shrink-0 w-full md:w-auto pt-3 md:pt-0 border-t md:border-t-0 border-emerald-50">
        <button
          type="button"
          data-testid={`btn-edit-step-${index}`}
          disabled={isEditDisabled}
          onClick={() => onEdit(step)}
          className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-xl text-emerald-800 bg-emerald-50 hover:bg-emerald-100 hover:text-emerald-950 border border-emerald-200/80 shadow-2xs disabled:opacity-40 disabled:cursor-not-allowed transition-all"
        >
          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
          </svg>
          Sửa
        </button>

        <button
          type="button"
          data-testid={`btn-delete-step-${index}`}
          disabled={isDeleteDisabled}
          onClick={() => onDeleteRequest(step)}
          className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-xl text-rose-700 bg-rose-50 hover:bg-rose-100 hover:text-rose-900 border border-rose-200/80 shadow-2xs disabled:opacity-40 disabled:cursor-not-allowed transition-all"
        >
          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
          </svg>
          Xóa
        </button>
      </div>
    </div>
  );
}

export default StepItem;
