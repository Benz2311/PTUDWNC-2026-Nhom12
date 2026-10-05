'use client';

import React, { useState, useEffect, useCallback, useMemo } from 'react';
import type { SearchResponse, RecipeSearchResult } from '@/types/search';
import { searchRecipes } from '@/lib/api/search';
import { SearchBox } from './SearchBox';
import { SearchResultCard } from './SearchResultCard';

export interface SearchContainerProps {
  initialQuery?: string;
  initialPage?: number;
  pageSize?: number;
}

export const SearchContainer: React.FC<SearchContainerProps> = ({
  initialQuery = '',
  initialPage = 1,
  pageSize = 10,
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
    if (initialQuery.trim().length >= 2) {
      executeSearch(initialQuery, initialPage);
    }
  }, [initialQuery, initialPage, executeSearch]);

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
        <h1 className="text-2xl sm:text-3xl font-extrabold text-gray-900 tracking-tight">
          Tìm kiếm công thức nấu ăn
        </h1>
        <p className="text-sm text-gray-600">
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
        <div className="flex flex-wrap items-center justify-center gap-2 pt-1">
          <span className="text-xs text-gray-400">Gợi ý từ khóa:</span>
          {['Phở bò', 'Bún chả', 'Gà nướng', 'Thịt kho', 'Cá hồi'].map((tag) => (
            <button
              key={tag}
              type="button"
              onClick={() => handleSearchSubmit(tag)}
              className="text-xs px-2.5 py-1 rounded-full bg-gray-100 text-gray-700 hover:bg-green-50 hover:text-green-800 transition-colors"
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
          className="p-4 rounded-xl bg-red-50 border border-red-200 text-sm text-red-800 flex items-start gap-3"
        >
          <svg className="w-5 h-5 text-red-600 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <circle cx="12" cy="12" r="10" strokeWidth="2" />
            <line x1="12" y1="8" x2="12" y2="12" strokeWidth="2" strokeLinecap="round" />
            <line x1="12" y1="16" x2="12.01" y2="16" strokeWidth="2" strokeLinecap="round" />
          </svg>
          <div className="flex-1">
            <strong className="font-semibold">Đã xảy ra sự cố:</strong> {errorMessage}
          </div>
          <button
            type="button"
            onClick={() => setErrorMessage(null)}
            className="text-red-500 hover:text-red-700 p-0.5"
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
          className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 p-4 rounded-xl bg-gray-50 border border-gray-200 text-xs sm:text-sm"
        >
          <div data-testid="search-result-count" className="font-medium text-gray-700">
            Tìm thấy <strong className="text-green-800 font-bold">{searchResponse.totalCount}</strong> công thức cho từ khóa &ldquo;<span className="text-gray-900 font-semibold">{activeQuery}</span>&rdquo;
          </div>

          <div className="flex flex-wrap items-center gap-3">
            {/* Filter by Difficulty */}
            <div className="flex items-center gap-1.5">
              <label htmlFor="filter-difficulty" className="text-gray-500 text-xs font-medium">Độ khó:</label>
              <select
                id="filter-difficulty"
                data-testid="select-difficulty"
                value={difficultyFilter}
                onChange={(e) => setDifficultyFilter(e.target.value)}
                className="px-2.5 py-1 rounded-lg border border-gray-300 bg-white text-xs text-gray-700 focus:outline-none focus:ring-1 focus:ring-green-700"
              >
                <option value="all">Tất cả</option>
                <option value="easy">Dễ</option>
                <option value="medium">Trung bình</option>
                <option value="hard">Khó</option>
              </select>
            </div>

            {/* Sort options */}
            <div className="flex items-center gap-1.5">
              <label htmlFor="sort-by" className="text-gray-500 text-xs font-medium">Sắp xếp:</label>
              <select
                id="sort-by"
                data-testid="select-sort"
                value={sortBy}
                onChange={(e) => setSortBy(e.target.value as any)}
                className="px-2.5 py-1 rounded-lg border border-gray-300 bg-white text-xs text-gray-700 focus:outline-none focus:ring-1 focus:ring-green-700"
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
              <div key={n} className="flex flex-col sm:flex-row bg-white rounded-xl border border-gray-200 overflow-hidden h-44">
                <div className="sm:w-56 bg-gray-200 shrink-0" />
                <div className="p-4 sm:p-5 flex-1 space-y-3">
                  <div className="h-4 bg-gray-200 rounded w-1/4" />
                  <div className="h-6 bg-gray-200 rounded w-3/4" />
                  <div className="h-4 bg-gray-200 rounded w-full" />
                </div>
              </div>
            ))}
          </div>
        ) : !hasSearched ? (
          /* Initial Empty State (Chưa tìm kiếm) */
          <div
            data-testid="search-initial-state"
            className="flex flex-col items-center justify-center p-12 rounded-xl bg-white border border-dashed border-gray-200 text-center space-y-3"
          >
            <div className="w-14 h-14 rounded-full bg-green-50 flex items-center justify-center text-green-800">
              <svg className="w-7 h-7" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </div>
            <h3 className="text-base font-bold text-gray-900">Sẵn sàng khám phá ẩm thực</h3>
            <p className="text-xs sm:text-sm text-gray-500 max-w-md">
              Nhập từ khóa bất kỳ để tìm kiếm công thức yêu thích của bạn. Hệ thống hỗ trợ tìm kiếm cả tiếng Việt có dấu và không dấu.
            </p>
          </div>
        ) : searchResponse && searchResponse.totalCount === 0 ? (
          /* No Results State */
          <div
            data-testid="search-no-results"
            className="flex flex-col items-center justify-center p-12 rounded-xl bg-white border border-gray-200 text-center space-y-3"
          >
            <div className="w-14 h-14 rounded-full bg-amber-50 flex items-center justify-center text-amber-700">
              <svg className="w-7 h-7" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <h3 className="text-base font-bold text-gray-900">Không tìm thấy công thức phù hợp</h3>
            <p className="text-xs sm:text-sm text-gray-500 max-w-md">
              Không tìm thấy công thức nào cho từ khóa &ldquo;<span className="font-semibold text-gray-700">{activeQuery}</span>&rdquo;.
              Hãy thử kiểm tra lỗi chính tả hoặc tìm với từ khóa ngắn gọn hơn (vd: &ldquo;pho bo&rdquo;, &ldquo;thịt gà&rdquo;).
            </p>
          </div>
        ) : (
          /* Results List */
          <div data-testid="search-results-list" className="space-y-4">
            {filteredAndSortedItems.length === 0 ? (
              <div className="p-8 text-center text-sm text-gray-500 bg-white rounded-xl border border-gray-200">
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
          className="flex items-center justify-center gap-3 pt-6 border-t border-gray-200"
        >
          <button
            type="button"
            data-testid="btn-prev-page"
            disabled={!searchResponse.hasPreviousPage}
            onClick={() => handlePageChange(currentPage - 1)}
            className="inline-flex items-center gap-1 px-3 py-1.5 rounded-lg border border-gray-300 bg-white text-xs font-semibold text-gray-700 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
            </svg>
            Trang trước
          </button>

          <span data-testid="pagination-info" className="text-xs font-medium text-gray-600">
            Trang <strong className="text-gray-900">{searchResponse.page}</strong> / {searchResponse.totalPages}
          </span>

          <button
            type="button"
            data-testid="btn-next-page"
            disabled={!searchResponse.hasNextPage}
            onClick={() => handlePageChange(currentPage + 1)}
            className="inline-flex items-center gap-1 px-3 py-1.5 rounded-lg border border-gray-300 bg-white text-xs font-semibold text-gray-700 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
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
