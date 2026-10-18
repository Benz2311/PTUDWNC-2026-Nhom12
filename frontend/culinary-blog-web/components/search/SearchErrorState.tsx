'use client';

import React from 'react';

export interface SearchErrorStateProps {
  message?: string;
  onRetry?: () => void;
}

export function SearchErrorState({
  message = 'Đã xảy ra lỗi trong quá trình tìm kiếm công thức.',
  onRetry,
}: SearchErrorStateProps) {
  return (
    <div
      data-testid="search-error-banner"
      role="alert"
      className="flex flex-col items-center justify-center p-8 sm:p-12 rounded-xl bg-red-50/50 border border-red-200 text-center space-y-3"
    >
      <div className="w-12 h-12 rounded-full bg-red-100 flex items-center justify-center text-red-600">
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
        </svg>
      </div>

      <h3 className="text-base font-bold text-gray-900">Tìm kiếm thất bại</h3>
      <p className="text-xs sm:text-sm text-gray-600 max-w-md">
        {message}
      </p>

      {onRetry && (
        <button
          type="button"
          data-testid="btn-retry-search"
          onClick={onRetry}
          className="mt-2 inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold rounded-lg text-white bg-red-600 hover:bg-red-700 transition-colors shadow-xs"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
          Thử lại
        </button>
      )}
    </div>
  );
}

export default SearchErrorState;
