'use client';

import { useState, useCallback, useEffect } from 'react';
import type { SearchRequest, SearchResponse, SearchFilterState } from '@/types/search';
import { searchRecipes } from '@/lib/api/search';

export interface UseRecipeSearchOptions {
  initialQuery?: string;
  initialPage?: number;
  initialPageSize?: number;
  initialFilters?: SearchFilterState;
  autoSearchOnMount?: boolean;
}

export function useRecipeSearch({
  initialQuery = '',
  initialPage = 1,
  initialPageSize = 10,
  initialFilters = {},
  autoSearchOnMount = false,
}: UseRecipeSearchOptions = {}) {
  const [query, setQuery] = useState(initialQuery);
  const [activeQuery, setActiveQuery] = useState(initialQuery);
  const [page, setPage] = useState(initialPage);
  const [pageSize, setPageSize] = useState(initialPageSize);
  const [filters, setFilters] = useState<SearchFilterState>(initialFilters);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hasSearched, setHasSearched] = useState(false);
  const [response, setResponse] = useState<SearchResponse | null>(null);

  const executeSearch = useCallback(
    async (
      overrideQuery?: string,
      overridePage?: number,
      overrideFilters?: SearchFilterState
    ) => {
      const q = (overrideQuery !== undefined ? overrideQuery : query).trim();
      const p = overridePage !== undefined ? overridePage : page;
      const f = overrideFilters !== undefined ? overrideFilters : filters;

      // Validation 2-100 ký tự (SRS FR-SRCH-001)
      if (q.length < 2 || q.length > 100) {
        setResponse({
          items: [],
          page: p,
          pageSize,
          totalCount: 0,
          totalPages: 0,
          hasPreviousPage: false,
          hasNextPage: false,
        });
        setActiveQuery(q);
        setHasSearched(true);
        return;
      }

      setIsLoading(true);
      setError(null);
      setActiveQuery(q);
      setHasSearched(true);

      const hasCustomFilters = Boolean(
        f.categorySlug ||
        (f.difficulty && f.difficulty !== 'all') ||
        (f.sortBy && f.sortBy !== 'relevance')
      );

      try {
        let res: SearchResponse;
        if (hasCustomFilters) {
          const searchParams: SearchRequest = {
            q,
            page: p,
            pageSize,
            categorySlug: f.categorySlug || undefined,
            difficulty: f.difficulty && f.difficulty !== 'all' ? f.difficulty : undefined,
            sortBy: f.sortBy || undefined,
          };
          res = await searchRecipes(searchParams);
        } else {
          res = await searchRecipes(q, p, pageSize);
        }
        setResponse(res);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Không thể thực hiện tìm kiếm.');
        setResponse(null);
      } finally {
        setIsLoading(false);
      }
    },
    [query, page, pageSize, filters]
  );

  const clearSearch = useCallback(() => {
    setQuery('');
    setActiveQuery('');
    setHasSearched(false);
    setError(null);
    setResponse(null);
    setPage(1);
  }, []);

  const handlePageChange = useCallback(
    (newPage: number) => {
      setPage(newPage);
      executeSearch(activeQuery, newPage);
    },
    [activeQuery, executeSearch]
  );

  useEffect(() => {
    if (autoSearchOnMount && initialQuery.trim().length >= 2) {
      executeSearch(initialQuery);
    }
  }, [autoSearchOnMount, initialQuery, executeSearch]);

  return {
    query,
    setQuery,
    activeQuery,
    page,
    pageSize,
    setPageSize,
    filters,
    setFilters,
    isLoading,
    error,
    setError,
    hasSearched,
    response,
    results: response?.items ?? [],
    executeSearch,
    clearSearch,
    handlePageChange,
  };
}
