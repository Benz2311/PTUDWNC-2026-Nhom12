'use client';

import React from 'react';
import type { RecipeStep } from '@/types/step';

interface StepItemProps {
  step: RecipeStep;
  index: number;
  totalSteps: number;
  disabled?: boolean;
  onEdit: (step: RecipeStep) => void;
  onDeleteRequest: (step: RecipeStep) => void;
  onMoveUp: (index: number) => void;
  onMoveDown: (index: number) => void;
}

export const StepItem: React.FC<StepItemProps> = ({
  step,
  index,
  totalSteps,
  disabled = false,
  onEdit,
  onDeleteRequest,
  onMoveUp,
  onMoveDown,
}) => {
  const isFirst = index === 0;
  const isLast = index === totalSteps - 1;

  // Format step number with leading zero (e.g. 01, 02)
  const formattedNumber = String(step.stepNumber ?? index + 1).padStart(2, '0');

  return (
    <div
      data-testid={`step-item-${index}`}
      className="group flex flex-col md:flex-row items-start gap-4 p-4 sm:p-5 rounded-xl bg-white border border-gray-200 transition-colors hover:border-gray-300"
    >
      {/* Step Number Badge & Reorder Arrows */}
      <div className="flex md:flex-col items-center justify-between w-full md:w-auto gap-2 shrink-0">
        <div className="flex items-center justify-center px-3 py-1.5 rounded-lg bg-green-50 text-green-800 border border-green-200 font-bold text-sm font-mono">
          <span>{formattedNumber}</span>
        </div>

        {/* Up / Down Reorder buttons */}
        <div className="flex md:flex-col gap-1">
          <button
            type="button"
            data-testid={`btn-move-up-${index}`}
            disabled={disabled || isFirst}
            onClick={() => onMoveUp(index)}
            aria-label={`Di chuyển bước ${formattedNumber} lên trên`}
            title="Di chuyển lên trên"
            className="p-1.5 rounded-md text-gray-500 hover:text-gray-900 hover:bg-gray-100 border border-transparent hover:border-gray-200 disabled:opacity-25 disabled:cursor-not-allowed transition-colors"
          >
            <svg
              className="w-4 h-4"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              xmlns="http://www.w3.org/2000/svg"
            >
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 15l7-7 7 7" />
            </svg>
          </button>

          <button
            type="button"
            data-testid={`btn-move-down-${index}`}
            disabled={disabled || isLast}
            onClick={() => onMoveDown(index)}
            aria-label={`Di chuyển bước ${formattedNumber} xuống dưới`}
            title="Di chuyển xuống dưới"
            className="p-1.5 rounded-md text-gray-500 hover:text-gray-900 hover:bg-gray-100 border border-transparent hover:border-gray-200 disabled:opacity-25 disabled:cursor-not-allowed transition-colors"
          >
            <svg
              className="w-4 h-4"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              xmlns="http://www.w3.org/2000/svg"
            >
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
            </svg>
          </button>
        </div>
      </div>

      {/* Main Content Area */}
      <div className="flex-1 min-w-0 space-y-2 w-full">
        <div className="flex flex-wrap items-center gap-2">
          {step.title ? (
            <h4
              data-testid={`step-title-${index}`}
              className="text-base font-semibold text-gray-900 break-words"
            >
              {step.title}
            </h4>
          ) : (
            <h4 className="text-sm font-medium text-gray-400 italic">
              Bước {formattedNumber}
            </h4>
          )}

          {step.timerMinutes !== null && step.timerMinutes !== undefined && step.timerMinutes > 0 && (
            <span
              data-testid={`step-timer-${index}`}
              className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-700 border border-gray-200"
            >
              <svg className="w-3.5 h-3.5 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <circle cx="12" cy="12" r="10" strokeWidth="2" />
                <polyline points="12 6 12 12 16 14" strokeWidth="2" />
              </svg>
              {step.timerMinutes} phút
            </span>
          )}
        </div>

        <p
          data-testid={`step-description-${index}`}
          className="text-sm text-gray-700 whitespace-pre-line leading-relaxed break-words"
        >
          {step.description}
        </p>

        {step.imageUrl && (
          <div className="mt-2.5 overflow-hidden rounded-lg border border-gray-200 max-w-xs bg-gray-50">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={step.imageUrl}
              alt={step.title || `Ảnh minh họa bước ${formattedNumber}`}
              className="w-full h-32 object-cover transition-opacity hover:opacity-95"
              onError={(e) => {
                (e.currentTarget as HTMLImageElement).style.display = 'none';
              }}
            />
          </div>
        )}
      </div>

      {/* Action Buttons: Edit & Delete */}
      <div className="flex items-center justify-end gap-1.5 shrink-0 w-full md:w-auto pt-2 md:pt-0 border-t md:border-t-0 border-gray-100">
        <button
          type="button"
          data-testid={`btn-edit-step-${index}`}
          disabled={disabled}
          onClick={() => onEdit(step)}
          className="inline-flex items-center gap-1 px-2.5 py-1.5 text-xs font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 hover:text-green-800 border border-gray-300 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
        >
          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
          </svg>
          Sửa
        </button>

        <button
          type="button"
          data-testid={`btn-delete-step-${index}`}
          disabled={disabled}
          onClick={() => onDeleteRequest(step)}
          className="inline-flex items-center gap-1 px-2.5 py-1.5 text-xs font-medium rounded-md text-gray-600 hover:text-red-600 bg-white hover:bg-red-50 border border-gray-200 hover:border-red-200 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
        >
          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
          </svg>
          Xóa
        </button>
      </div>
    </div>
  );
};
