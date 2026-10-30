'use client';

import React, { useState, useEffect, useCallback, useMemo } from 'react';
import type { SearchResponse } from '@/types/search';
import { searchRecipes } from '@/lib/api/search';
import { getCategories } from '@/lib/api/categories';
import { SearchBox } from './SearchBox';
import { SearchResultCard } from './SearchResultCard';
import { SearchLoadingState } from './SearchLoadingState';
import { SearchEmptyState } from './SearchEmptyState';
import { SearchErrorState } from './SearchErrorState';

export interface SearchContainerProps {
  initialQuery?: string;
  initialPage?: number;
  initialCategory?: string;
  initialDifficulty?: string;
  initialMaxCookTime?: number;
  initialSort?: 'relevance' | 'newest' | 'cookTime';
  pageSize?: number;
  autoSearchOnMount?: boolean;
  syncUrl?: boolean;
}

export const SearchContainer: React.FC<SearchContainerProps> = ({
  initialQuery = '',
  initialPage = 1,
  initialCategory = 'all',
  initialDifficulty = 'all',
  initialMaxCookTime = 0,
  initialSort = 'relevance',
  pageSize = 10,
  autoSearchOnMount = true,
  syncUrl = true,
}) => {
  const [activeQuery, setActiveQuery] = useState<string>(initialQuery);
  const [currentPage, setCurrentPage] = useState<number>(initialPage);
  const [searchResponse, setSearchResponse] = useState<SearchResponse | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [hasSearched, setHasSearched] = useState<boolean>(Boolean(initialQuery.trim().length >= 2));

  // Đồng bộ activeQuery khi initialQuery prop thay đổi
  useEffect(() => {
    if (initialQuery !== undefined) {
      setActiveQuery(initialQuery);
    }
  }, [initialQuery]);

  // Bộ lọc và sắp xếp
  const [categoryFilter, setCategoryFilter] = useState<string>(initialCategory);
  const [difficultyFilter, setDifficultyFilter] = useState<string>(initialDifficulty);
  const [maxCookTimeFilter, setMaxCookTimeFilter] = useState<number>(initialMaxCookTime);
  const [sortBy, setSortBy] = useState<'relevance' | 'newest' | 'cookTime'>(initialSort);

  // Danh mục lấy từ API thật
  const [categories, setCategories] = useState<{ slug: string; name: string }[]>([]);

  useEffect(() => {
    let isMounted = true;
    getCategories()
      .then((data) => {
        if (isMounted && Array.isArray(data) && data.length > 0) {
          setCategories(data.map((c) => ({ slug: c.slug, name: c.name })));
        }
      })
      .catch(() => {
        // Fallback an toàn nếu API danh mục tạm ngắt
      });
    return () => {
      isMounted = false;
    };
  }, []);

  // Hàm đồng bộ URL query parameters
  const updateUrlParams = useCallback(
    (
      q: string,
      pageToFetch: number,
      diff: string,
      sort: 'relevance' | 'newest' | 'cookTime',
      cat: string,
      cookTime: number
    ) => {
      if (!syncUrl || typeof window === 'undefined') return;

      const params = new URLSearchParams();
      if (q.trim()) params.set('q', q.trim());
      if (pageToFetch > 1) params.set('page', String(pageToFetch));
      if (cat && cat !== 'all') params.set('category', cat);
      if (diff && diff !== 'all') params.set('difficulty', diff);
      if (cookTime > 0) params.set('maxCookTime', String(cookTime));
      if (sort !== 'relevance') params.set('sort', sort);

      const qs = params.toString();
      const targetUrl = qs ? `/search?${qs}` : '/search';
      if (window.location.pathname + window.location.search !== targetUrl) {
        window.history.pushState(
          { q, page: pageToFetch, diff, sort, cat, cookTime },
          '',
          targetUrl
        );
      }
    },
    [syncUrl]
  );

  // Hàm gọi API tìm kiếm
  const executeSearch = useCallback(
    async (
      q: string,
      pageToFetch: number,
      filterDiff: string = difficultyFilter,
      sortOption: 'relevance' | 'newest' | 'cookTime' = sortBy,
      filterCat: string = categoryFilter,
      filterCookTime: number = maxCookTimeFilter,
      shouldUpdateUrl: boolean = true
    ) => {
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
        const diffParam = filterDiff !== 'all' ? filterDiff : undefined;
        const sortParam =
          sortOption === 'newest'
            ? 'publishedAt'
            : sortOption === 'cookTime'
            ? 'cookTime'
            : 'relevance';
        const catParam = filterCat !== 'all' && filterCat ? filterCat : undefined;
        const cookParam = filterCookTime > 0 ? filterCookTime : undefined;

        let result: SearchResponse;
        if (catParam || cookParam) {
          result = await searchRecipes({
            q: trimmed,
            page: pageToFetch,
            pageSize,
            categorySlug: catParam,
            difficulty: diffParam,
            maxCookTimeMinutes: cookParam,
            sortBy: sortParam,
          });
        } else if (diffParam || sortParam !== 'relevance') {
          result = await searchRecipes(trimmed, pageToFetch, pageSize, diffParam, sortParam);
        } else {
          result = await searchRecipes(trimmed, pageToFetch, pageSize);
        }

        setSearchResponse(result);
        setCurrentPage(result.page);

        if (shouldUpdateUrl) {
          updateUrlParams(trimmed, pageToFetch, filterDiff, sortOption, filterCat, filterCookTime);
        }
      } catch (err) {
        setErrorMessage(
          err instanceof Error ? err.message : 'Không thể kết nối đến máy chủ tìm kiếm.'
        );
        setSearchResponse(null);
      } finally {
        setIsLoading(false);
      }
    },
    [pageSize, difficultyFilter, sortBy, categoryFilter, maxCookTimeFilter, updateUrlParams]
  );

  // Khởi động tìm kiếm nếu có initialQuery
  useEffect(() => {
    if (autoSearchOnMount && initialQuery.trim().length >= 2) {
      executeSearch(
        initialQuery,
        initialPage,
        initialDifficulty,
        initialSort,
        initialCategory,
        initialMaxCookTime,
        false
      );
    }
  }, [
    autoSearchOnMount,
    initialQuery,
    initialPage,
    initialDifficulty,
    initialSort,
    initialCategory,
    initialMaxCookTime,
    executeSearch,
  ]);

  // Lắng nghe sự kiện popstate để hỗ trợ Back / Forward của trình duyệt
  useEffect(() => {
    if (!syncUrl || typeof window === 'undefined') return;

    const handlePopState = () => {
      const sp = new URLSearchParams(window.location.search);
      const urlQ = sp.get('q') || '';
      const urlPage = parseInt(sp.get('page') || '1', 10) || 1;
      const urlCat = sp.get('category') || 'all';
      const urlDiff = sp.get('difficulty') || 'all';
      const urlCook = parseInt(sp.get('maxCookTime') || '0', 10) || 0;
      const urlSort = (sp.get('sort') as any) || 'relevance';

      setActiveQuery(urlQ);
      setCurrentPage(urlPage);
      setCategoryFilter(urlCat);
      setDifficultyFilter(urlDiff);
      setMaxCookTimeFilter(urlCook);
      setSortBy(urlSort);

      if (urlQ.trim().length >= 2) {
        executeSearch(urlQ, urlPage, urlDiff, urlSort, urlCat, urlCook, false);
      } else {
        setSearchResponse(null);
        setHasSearched(false);
      }
    };

    window.addEventListener('popstate', handlePopState);
    return () => window.removeEventListener('popstate', handlePopState);
  }, [syncUrl, executeSearch]);

  // Xử lý khi người dùng nhập từ khóa mới và submit
  const handleSearchSubmit = (newQuery: string) => {
    setCurrentPage(1);
    executeSearch(newQuery, 1, difficultyFilter, sortBy, categoryFilter, maxCookTimeFilter);
  };

  // Thay đổi bộ lọc danh mục
  const handleCategoryChange = (newCategory: string) => {
    setCategoryFilter(newCategory);
    setCurrentPage(1);
    if (activeQuery.trim().length >= 2) {
      executeSearch(activeQuery, 1, difficultyFilter, sortBy, newCategory, maxCookTimeFilter);
    }
  };

  // Thay đổi bộ lọc độ khó (gọi lại API và reset trang 1)
  const handleDifficultyChange = (newDifficulty: string) => {
    setDifficultyFilter(newDifficulty);
    setCurrentPage(1);
    if (activeQuery.trim().length >= 2) {
      executeSearch(activeQuery, 1, newDifficulty, sortBy, categoryFilter, maxCookTimeFilter);
    }
  };

  // Thay đổi bộ lọc thời gian nấu tối đa
  const handleMaxCookTimeChange = (newCookTimeStr: string) => {
    const newCookTime = parseInt(newCookTimeStr, 10) || 0;
    setMaxCookTimeFilter(newCookTime);
    setCurrentPage(1);
    if (activeQuery.trim().length >= 2) {
      executeSearch(activeQuery, 1, difficultyFilter, sortBy, categoryFilter, newCookTime);
    }
  };

  // Thay đổi tiêu chí sắp xếp (gọi lại API và reset trang 1)
  const handleSortChange = (newSort: 'relevance' | 'newest' | 'cookTime') => {
    setSortBy(newSort);
    setCurrentPage(1);
    if (activeQuery.trim().length >= 2) {
      executeSearch(activeQuery, 1, difficultyFilter, newSort, categoryFilter, maxCookTimeFilter);
    }
  };

  // Reset toàn bộ bộ lọc
  const handleResetFilters = () => {
    setCategoryFilter('all');
    setDifficultyFilter('all');
    setMaxCookTimeFilter(0);
    setSortBy('relevance');
    setCurrentPage(1);
    if (activeQuery.trim().length >= 2) {
      executeSearch(activeQuery, 1, 'all', 'relevance', 'all', 0);
    }
  };

  // Chuyển trang
  const handlePageChange = (newPage: number) => {
    if (!searchResponse) return;
    if (newPage < 1 || newPage > searchResponse.totalPages) return;
    executeSearch(activeQuery, newPage, difficultyFilter, sortBy, categoryFilter, maxCookTimeFilter);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const handleClearSearch = () => {
    setActiveQuery('');
    setSearchResponse(null);
    setHasSearched(false);
    setErrorMessage(null);
    setCurrentPage(1);
    if (syncUrl && typeof window !== 'undefined') {
      window.history.pushState({}, '', '/search');
    }
  };

  // Lọc và sắp xếp kết quả hiển thị trên client
  const filteredAndSortedItems = useMemo(() => {
    if (!searchResponse || !searchResponse.items) return [];

    let items = [...searchResponse.items];

    // Lọc theo độ khó nếu có chọn (fallback an toàn trên client)
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

  const hasActiveFilters =
    categoryFilter !== 'all' ||
    difficultyFilter !== 'all' ||
    maxCookTimeFilter > 0 ||
    sortBy !== 'relevance';

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
          Khám phá hàng ngàn công thức thơm ngon với công nghệ tìm kiếm toàn văn PostgreSQL FTS & gợi ý Trigram thông minh.
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
              className="text-xs px-3 py-1 rounded-full bg-emerald-50 text-emerald-800 border border-emerald-200/80 hover:bg-emerald-100 font-semibold transition-all shadow-2xs cursor-pointer"
            >
              {tag}
            </button>
          ))}
        </div>
      </div>

      {/* Error banner */}
      {errorMessage && (
        <SearchErrorState
          message={errorMessage}
          onRetry={() =>
            executeSearch(
              activeQuery,
              currentPage,
              difficultyFilter,
              sortBy,
              categoryFilter,
              maxCookTimeFilter
            )
          }
        />
      )}

      {/* Filter and Sort Toolbar (Chỉ hiển thị khi có kết quả hoặc đã tìm kiếm) */}
      {searchResponse && searchResponse.totalCount > 0 && (
        <div
          data-testid="search-toolbar"
          className="flex flex-col gap-3.5 p-4 rounded-2xl bg-emerald-50/40 border border-emerald-100/90 text-xs sm:text-sm shadow-2xs"
        >
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
            <div data-testid="search-result-count" className="font-medium text-slate-700">
              Tìm thấy <strong className="text-emerald-800 font-bold">{searchResponse.totalCount}</strong> công thức cho từ khóa &ldquo;<span className="text-slate-900 font-semibold">{activeQuery}</span>&rdquo;
            </div>

            {hasActiveFilters && (
              <button
                type="button"
                data-testid="btn-reset-filters"
                onClick={handleResetFilters}
                className="self-start sm:self-auto text-xs text-emerald-700 hover:text-emerald-900 font-semibold underline underline-offset-2 transition-colors cursor-pointer"
              >
                Đặt lại bộ lọc
              </button>
            )}
          </div>

          <div className="flex flex-wrap items-center gap-3 pt-1 border-t border-emerald-100/60">
            {/* Filter by Category */}
            <div className="flex items-center gap-1.5">
              <label htmlFor="filter-category" className="text-slate-600 text-xs font-semibold">Danh mục:</label>
              <select
                id="filter-category"
                data-testid="select-category"
                value={categoryFilter}
                onChange={(e) => handleCategoryChange(e.target.value)}
                className="px-3 py-1.5 rounded-xl border border-emerald-200 bg-white text-xs font-medium text-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-100 focus:border-emerald-600 shadow-2xs"
              >
                <option value="all">Tất cả danh mục</option>
                {categories.map((c) => (
                  <option key={c.slug} value={c.slug}>
                    {c.name}
                  </option>
                ))}
              </select>
            </div>

            {/* Filter by Difficulty */}
            <div className="flex items-center gap-1.5">
              <label htmlFor="filter-difficulty" className="text-slate-600 text-xs font-semibold">Độ khó:</label>
              <select
                id="filter-difficulty"
                data-testid="select-difficulty"
                value={difficultyFilter}
                onChange={(e) => handleDifficultyChange(e.target.value)}
                className="px-3 py-1.5 rounded-xl border border-emerald-200 bg-white text-xs font-medium text-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-100 focus:border-emerald-600 shadow-2xs"
              >
                <option value="all">Tất cả</option>
                <option value="easy">Dễ</option>
                <option value="medium">Trung bình</option>
                <option value="hard">Khó</option>
              </select>
            </div>

            {/* Filter by Max Cook Time */}
            <div className="flex items-center gap-1.5">
              <label htmlFor="filter-cook-time" className="text-slate-600 text-xs font-semibold">Thời gian nấu:</label>
              <select
                id="filter-cook-time"
                data-testid="select-max-cook-time"
                value={String(maxCookTimeFilter)}
                onChange={(e) => handleMaxCookTimeChange(e.target.value)}
                className="px-3 py-1.5 rounded-xl border border-emerald-200 bg-white text-xs font-medium text-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-100 focus:border-emerald-600 shadow-2xs"
              >
                <option value="0">Tất cả thời gian</option>
                <option value="15">Dưới 15 phút</option>
                <option value="30">Dưới 30 phút</option>
                <option value="60">Dưới 60 phút</option>
                <option value="120">Dưới 120 phút</option>
              </select>
            </div>

            {/* Sort options */}
            <div className="flex items-center gap-1.5 ml-auto">
              <label htmlFor="sort-by" className="text-slate-600 text-xs font-semibold">Sắp xếp:</label>
              <select
                id="sort-by"
                data-testid="select-sort"
                value={sortBy}
                onChange={(e) => handleSortChange(e.target.value as any)}
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
          <SearchLoadingState count={3} />
        ) : !hasSearched ? (
          <SearchEmptyState />
        ) : searchResponse && searchResponse.totalCount === 0 ? (
          <SearchEmptyState query={activeQuery} onClearQuery={handleClearSearch} />
        ) : (
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
            className="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl border border-emerald-200 bg-white text-xs font-semibold text-slate-700 hover:bg-emerald-50 hover:text-emerald-900 disabled:opacity-40 disabled:cursor-not-allowed transition-all shadow-2xs cursor-pointer"
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
            className="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl border border-emerald-200 bg-white text-xs font-semibold text-slate-700 hover:bg-emerald-50 hover:text-emerald-900 disabled:opacity-40 disabled:cursor-not-allowed transition-all shadow-2xs cursor-pointer"
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
