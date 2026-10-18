'use client';

import React from 'react';

export interface SearchEmptyStateProps {
  /**
   * Nếu có query: Trạng thái không tìm thấy kết quả cho từ khóa đó.
   * Nếu không có query: Trạng thái ban đầu trước khi tìm kiếm.
   */
  query?: string;
  onClearQuery?: () => void;
}

export function SearchEmptyState({ query, onClearQuery }: SearchEmptyStateProps) {
  const hasQuery = Boolean(query && query.trim().length > 0);

  if (!hasQuery) {
    return (
      <div
        data-testid="search-initial-state"
        className="flex flex-col items-center justify-center p-12 rounded-3xl bg-emerald-50/30 border-2 border-dashed border-emerald-200 text-center space-y-3"
      >
        <div className="w-16 h-16 rounded-2xl bg-emerald-100 flex items-center justify-center text-emerald-800 shadow-2xs">
          <svg className="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
        </div>
        <h3 className="text-base sm:text-lg font-bold text-slate-900">Sẵn sàng khám phá ẩm thực</h3>
        <p className="text-xs sm:text-sm text-slate-500 max-w-md leading-relaxed">
          Nhập từ khóa bất kỳ để tìm kiếm công thức yêu thích của bạn. Hệ thống hỗ trợ tìm kiếm cả tiếng Việt có dấu và không dấu (ví dụ: &ldquo;pho bo&rdquo;, &ldquo;bún chả&rdquo;).
        </p>
      </div>
    );
  }

  return (
    <div
      data-testid="search-no-results"
      className="flex flex-col items-center justify-center p-12 rounded-3xl bg-amber-50/20 border border-amber-200 text-center space-y-3 shadow-xs"
    >
      <div className="w-16 h-16 rounded-2xl bg-amber-100 flex items-center justify-center text-amber-800 shadow-2xs">
        <svg className="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      </div>
      <h3 className="text-base sm:text-lg font-bold text-slate-900">Không tìm thấy công thức phù hợp</h3>
      <p className="text-xs sm:text-sm text-slate-600 max-w-md leading-relaxed">
        Không tìm thấy công thức nào cho từ khóa &ldquo;<span className="font-semibold text-slate-900">{query}</span>&rdquo;.
        Hãy thử kiểm tra lỗi chính tả hoặc tìm với từ khóa ngắn gọn hơn (vd: &ldquo;phở&rdquo;, &ldquo;thịt kho&rdquo;).
      </p>

      {onClearQuery && (
        <button
          type="button"
          onClick={onClearQuery}
          className="mt-2 inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold rounded-xl text-emerald-800 bg-emerald-50 hover:bg-emerald-100 border border-emerald-200 transition-colors shadow-2xs"
        >
          Xóa tìm kiếm và thử lại
        </button>
      )}
    </div>
  );
}

export default SearchEmptyState;
