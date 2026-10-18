'use client';

import React, { useState, useEffect, useCallback, useMemo } from 'react';
import type { SearchResponse } from '@/types/search';
import { searchRecipes } from '@/lib/api/search';
import { SearchBox } from './SearchBox';
import { SearchResultCard } from './SearchResultCard';

export interface SearchContainerProps {
  initialQuery?: string;
  initialPage?: number;
  pageSize?: number;
  autoSearchOnMount?: boolean;
}

export const SearchContainer: React.FC<SearchContainerProps> = ({
  initialQuery = '',
  initialPage = 1,
  pageSize = 10,
  autoSearchOnMount = true,
}) => {
  const [activeQuery, setActiveQuery] = useState<string>(initialQuery);
  const [currentPage, setCurrentPage] = useState<number>(initialPage);
  const [searchResponse, setSearchResponse] = useState<SearchResponse | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [hasSearched, setHasSearched] = useState<boolean>(Boolean(initialQuery.trim().length >= 2));

  // Bộ lọc và sắp xếp
  const [difficultyFilter, setDifficultyFilter] = useState<string>('all');
  const [sortBy, setSortBy] = useState<'relevance' | 'newest' | 'cookTime'>('relevance');

  // Hàm gọi API tìm kiếm
  const executeSearch = useCallback(
    async (q: string, pageToFetch: number) => {
      const trimmed = q.trim();
      if (trimmed.length < 2) {
        setSearchResponse(null);
        setHasSearched(false);
        return;
      }

      setIsLoading(true);
      setErrorMessage(null);
      setHasSearched(true);
      setActiveQuery(trimmed);

      try {
        const result = await searchRecipes(trimmed, pageToFetch, pageSize);
        setSearchResponse(result);
        setCurrentPage(result.page);
      } catch (err) {
        setErrorMessage(
          err instanceof Error ? err.message : 'Không thể kết nối đến máy chủ tìm kiếm.'
        );
        setSearchResponse(null);
      } finally {
        setIsLoading(false);
      }
    },
    [pageSize]
  );

  // Khởi động tìm kiếm nếu có initialQuery
  useEffect(() => {
    if (autoSearchOnMount && initialQuery.trim().length >= 2) {
      executeSearch(initialQuery, initialPage);
    }
  }, [autoSearchOnMount, initialQuery, initialPage, executeSearch]);

  // Xử lý khi người dùng nhập từ khóa mới và submit
  const handleSearchSubmit = (newQuery: string) => {
    setCurrentPage(1);
    executeSearch(newQuery, 1);
  };

  // Chuyển trang
  const handlePageChange = (newPage: number) => {
    if (!searchResponse) return;
    if (newPage < 1 || newPage > searchResponse.totalPages) return;
    executeSearch(activeQuery, newPage);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  // Lọc và sắp xếp kết quả hiển thị trên client
  const filteredAndSortedItems = useMemo(() => {
    if (!searchResponse || !searchResponse.items) return [];

    let items = [...searchResponse.items];

    // Lọc theo độ khó nếu có chọn
    if (difficultyFilter !== 'all') {
      items = items.filter((item) => {
        const diffStr = String(item.difficulty || '').toLowerCase();
        return diffStr === difficultyFilter.toLowerCase();
      });
    }

    // Sắp xếp
    if (sortBy === 'newest') {
      items.sort((a, b) => {
        const dateA = a.publishedAt ? new Date(a.publishedAt).getTime() : 0;
        const dateB = b.publishedAt ? new Date(b.publishedAt).getTime() : 0;
        return dateB - dateA;
      });
    } else if (sortBy === 'cookTime') {
      items.sort((a, b) => (a.cookTimeMinutes ?? 0) - (b.cookTimeMinutes ?? 0));
    }

    return items;
  }, [searchResponse, difficultyFilter, sortBy]);

  return (
    <div className="w-full max-w-5xl mx-auto px-4 py-8 space-y-8" data-testid="search-container">
      {/* Header & SearchBox */}
      <div className="space-y-4 text-center max-w-2xl mx-auto">
        <div className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-800 shadow-2xs">
          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          Tìm kiếm thông minh FTS & Fuzzy Trigram
        </div>

        <h1 className="text-3xl sm:text-4xl font-extrabold text-slate-900 tracking-tight">
          Tìm kiếm công thức nấu ăn
        </h1>
        <p className="text-xs sm:text-sm text-slate-600 leading-relaxed">
          Khám phá hàng ngàn công thức thơm ngon với công nghệ tìm kiếm toàn văn FTS & gợi ý Trigram thông minh.
        </p>

        <div className="pt-2">
          <SearchBox
            initialQuery={activeQuery}
            isLoading={isLoading}
            onSearch={handleSearchSubmit}
          />
        </div>

        {/* Quick query tags */}
        <div className="flex flex-wrap items-center justify-center gap-2 pt-2">
          <span className="text-xs text-slate-400 font-medium">Gợi ý từ khóa:</span>
          {['Phở bò', 'Bún chả', 'Gà nướng', 'Thịt kho', 'Cá hồi'].map((tag) => (
            <button
              key={tag}
              type="button"
              onClick={() => handleSearchSubmit(tag)}
              className="text-xs px-3 py-1 rounded-full bg-emerald-50 text-emerald-800 border border-emerald-200/80 hover:bg-emerald-100 font-semibold transition-all shadow-2xs"
            >
              {tag}
            </button>
          ))}
        </div>
      </div>

      {/* Error banner */}
      {errorMessage && (
        <div
          data-testid="search-error-banner"
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
            <strong className="font-semibold block text-rose-900">Đã xảy ra sự cố:</strong> {errorMessage}
          </div>
          <button
            type="button"
            onClick={() => setErrorMessage(null)}
            className="text-rose-500 hover:text-rose-700 p-1"
            aria-label="Đóng thông báo lỗi"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      )}

      {/* Filter and Sort Toolbar (Chỉ hiển thị khi có kết quả) */}
      {searchResponse && searchResponse.totalCount > 0 && (
        <div
          data-testid="search-toolbar"
          className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 p-4 rounded-2xl bg-emerald-50/40 border border-emerald-100/90 text-xs sm:text-sm shadow-2xs"
        >
          <div data-testid="search-result-count" className="font-medium text-slate-700">
            Tìm thấy <strong className="text-emerald-800 font-bold">{searchResponse.totalCount}</strong> công thức cho từ khóa &ldquo;<span className="text-slate-900 font-semibold">{activeQuery}</span>&rdquo;
          </div>

          <div className="flex flex-wrap items-center gap-3">
            {/* Filter by Difficulty */}
            <div className="flex items-center gap-1.5">
              <label htmlFor="filter-difficulty" className="text-slate-600 text-xs font-semibold">Độ khó:</label>
              <select
                id="filter-difficulty"
                data-testid="select-difficulty"
                value={difficultyFilter}
                onChange={(e) => setDifficultyFilter(e.target.value)}
                className="px-3 py-1.5 rounded-xl border border-emerald-200 bg-white text-xs font-medium text-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-100 focus:border-emerald-600 shadow-2xs"
              >
                <option value="all">Tất cả</option>
                <option value="easy">Dễ</option>
                <option value="medium">Trung bình</option>
                <option value="hard">Khó</option>
              </select>
            </div>

            {/* Sort options */}
            <div className="flex items-center gap-1.5">
              <label htmlFor="sort-by" className="text-slate-600 text-xs font-semibold">Sắp xếp:</label>
              <select
                id="sort-by"
                data-testid="select-sort"
                value={sortBy}
                onChange={(e) => setSortBy(e.target.value as any)}
                className="px-3 py-1.5 rounded-xl border border-emerald-200 bg-white text-xs font-medium text-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-100 focus:border-emerald-600 shadow-2xs"
              >
                <option value="relevance">Độ liên quan (FTS Rank)</option>
                <option value="newest">Mới nhất</option>
                <option value="cookTime">Thời gian nấu nhanh nhất</option>
              </select>
            </div>
          </div>
        </div>
      )}

      {/* Main Results Section */}
      <div className="space-y-4">
        {isLoading ? (
          /* Loading Skeletons */
          <div data-testid="search-loading" className="space-y-4 animate-pulse">
            {[1, 2, 3].map((n) => (
              <div key={n} className="flex flex-col sm:flex-row bg-white rounded-2xl border border-emerald-100/80 overflow-hidden sm:h-44 shadow-xs">
                <div className="w-full sm:w-60 h-44 sm:h-full bg-slate-200/80 shrink-0" />
                <div className="p-5 flex-1 space-y-3 flex flex-col justify-center">
                  <div className="flex items-center gap-2">
                    <div className="h-5 bg-emerald-100/80 rounded-full w-24" />
                    <div className="h-5 bg-slate-200 rounded-full w-16" />
                  </div>
                  <div className="h-6 bg-slate-200 rounded-xl w-3/4" />
                  <div className="h-4 bg-slate-100 rounded-lg w-full" />
                  <div className="h-4 bg-slate-100 rounded-lg w-1/2" />
                </div>
              </div>
            ))}
          </div>
        ) : !hasSearched ? (
          /* Initial Empty State (Chưa tìm kiếm) */
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
              Nhập từ khóa bất kỳ để tìm kiếm công thức yêu thích của bạn. Hệ thống hỗ trợ tìm kiếm cả tiếng Việt có dấu và không dấu.
            </p>
          </div>
        ) : searchResponse && searchResponse.totalCount === 0 ? (
          /* No Results State */
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
              Không tìm thấy công thức nào cho từ khóa &ldquo;<span className="font-semibold text-slate-900">{activeQuery}</span>&rdquo;.
              Hãy thử kiểm tra lỗi chính tả hoặc tìm với từ khóa ngắn gọn hơn (vd: &ldquo;pho bo&rdquo;, &ldquo;thịt gà&rdquo;).
            </p>
          </div>
        ) : (
          /* Results List */
          <div data-testid="search-results-list" className="space-y-4">
            {filteredAndSortedItems.length === 0 ? (
              <div className="p-8 text-center text-sm text-slate-500 bg-white rounded-2xl border border-emerald-100 shadow-xs">
                Không có kết quả nào khớp với bộ lọc đã chọn.
              </div>
            ) : (
              filteredAndSortedItems.map((recipe, idx) => (
                <SearchResultCard key={recipe.id} recipe={recipe} index={idx} />
              ))
            )}
          </div>
        )}
      </div>

      {/* Pagination Controls */}
      {searchResponse && searchResponse.totalPages > 1 && !isLoading && (
        <div
          data-testid="search-pagination"
          className="flex items-center justify-center gap-3 pt-6 border-t border-emerald-100"
        >
          <button
            type="button"
            data-testid="btn-prev-page"
            disabled={!searchResponse.hasPreviousPage}
            onClick={() => handlePageChange(currentPage - 1)}
            className="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl border border-emerald-200 bg-white text-xs font-semibold text-slate-700 hover:bg-emerald-50 hover:text-emerald-900 disabled:opacity-40 disabled:cursor-not-allowed transition-all shadow-2xs"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
            </svg>
            Trang trước
          </button>

          <span data-testid="pagination-info" className="text-xs font-medium text-slate-600 px-3 py-1.5 rounded-xl bg-slate-100 font-mono">
            Trang <strong className="text-emerald-800">{searchResponse.page}</strong> / {searchResponse.totalPages}
          </span>

          <button
            type="button"
            data-testid="btn-next-page"
            disabled={!searchResponse.hasNextPage}
            onClick={() => handlePageChange(currentPage + 1)}
            className="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl border border-emerald-200 bg-white text-xs font-semibold text-slate-700 hover:bg-emerald-50 hover:text-emerald-900 disabled:opacity-40 disabled:cursor-not-allowed transition-all shadow-2xs"
          >
            Trang sau
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
            </svg>
          </button>
        </div>
      )}
    </div>
  );
};

export default SearchContainer;
