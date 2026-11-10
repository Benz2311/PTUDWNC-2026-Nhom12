'use client';

import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import type { SearchResult } from '@/types/search';
import { searchRecipes } from '@/lib/api/search';
import { resolveImageUrl } from '@/lib/image-url';

export interface SearchBoxProps {
  initialQuery?: string;
  placeholder?: string;
  isLoading?: boolean;
  onSearch: (query: string) => void;
  className?: string;
  enableAutocomplete?: boolean;
  maxSuggestions?: number;
  debounceMs?: number;
  onSelectSuggestion?: (recipe: SearchResult) => void;
}

export function SearchBox({
  initialQuery = '',
  placeholder = 'Tìm kiếm công thức (vd: phở bò, bún chả, thịt kho...)',
  isLoading = false,
  onSearch,
  className = '',
  enableAutocomplete = true,
  maxSuggestions = 6,
  debounceMs = 300,
  onSelectSuggestion,
}: SearchBoxProps) {
  let router: any = null;
  try {
    // eslint-disable-next-line react-hooks/rules-of-hooks
    router = useRouter();
  } catch {
    router = null;
  }
  const [query, setQuery] = useState<string>(initialQuery);
  const [validationError, setValidationError] = useState<string | null>(null);

  // Trạng thái Autocomplete
  const [suggestions, setSuggestions] = useState<SearchResult[]>([]);
  const [isOpen, setIsOpen] = useState<boolean>(false);
  const [isFetchingSuggestions, setIsFetchingSuggestions] = useState<boolean>(false);
  const [selectedIndex, setSelectedIndex] = useState<number>(-1);
  const [hasSearchedSuggestions, setHasSearchedSuggestions] = useState<boolean>(false);

  const containerRef = useRef<HTMLDivElement>(null);
  const abortControllerRef = useRef<AbortController | null>(null);
  const debounceTimerRef = useRef<NodeJS.Timeout | null>(null);

  useEffect(() => {
    setQuery(initialQuery);
  }, [initialQuery]);

  // Click outside listener để đóng dropdown
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
        setSelectedIndex(-1);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Hủy debounce và pending request khi unmount
  useEffect(() => {
    return () => {
      if (debounceTimerRef.current) clearTimeout(debounceTimerRef.current);
      if (abortControllerRef.current) abortControllerRef.current.abort();
    };
  }, []);

  // Gọi API gợi ý với Debounce và AbortController chống race-condition
  const fetchSuggestions = useCallback(
    (searchKeyword: string) => {
      const trimmed = searchKeyword.trim();

      // Nếu từ khóa dưới 2 ký tự thì không gọi API
      if (trimmed.length < 2) {
        if (abortControllerRef.current) abortControllerRef.current.abort();
        setSuggestions([]);
        setIsOpen(false);
        setIsFetchingSuggestions(false);
        setHasSearchedSuggestions(false);
        return;
      }

      // Hủy request trước đó nếu chưa hoàn thành
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }

      const controller = new AbortController();
      abortControllerRef.current = controller;

      setIsFetchingSuggestions(true);
      setHasSearchedSuggestions(true);

      searchRecipes({
        q: trimmed,
        pageSize: maxSuggestions,
        signal: controller.signal,
      })
        .then((res) => {
          // Chỉ cập nhật nếu request này chưa bị abort
          if (!controller.signal.aborted) {
            setSuggestions(res.items || []);
            setIsOpen(true);
            setSelectedIndex(-1);
          }
        })
        .catch(() => {
          if (!controller.signal.aborted) {
            setSuggestions([]);
          }
        })
        .finally(() => {
          if (!controller.signal.aborted) {
            setIsFetchingSuggestions(false);
          }
        });
    },
    [maxSuggestions]
  );

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;
    setQuery(val);
    if (validationError) setValidationError(null);

    if (!enableAutocomplete) return;

    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current);
    }

    if (val.trim().length >= 2) {
      debounceTimerRef.current = setTimeout(() => {
        fetchSuggestions(val);
      }, debounceMs);
    } else {
      setSuggestions([]);
      setIsOpen(false);
      setIsFetchingSuggestions(false);
      setHasSearchedSuggestions(false);
    }
  };

  const handleSubmit = (e?: React.FormEvent) => {
    if (e) e.preventDefault();

    // Đóng dropdown khi submit
    setIsOpen(false);
    setSelectedIndex(-1);

    const trimmed = query.trim();

    if (trimmed.length === 0) {
      setValidationError('Vui lòng nhập từ khóa tìm kiếm.');
      return;
    }

    if (trimmed.length < 2) {
      setValidationError('Từ khóa tìm kiếm phải có ít nhất 2 ký tự.');
      return;
    }

    if (trimmed.length > 100) {
      setValidationError('Từ khóa tìm kiếm không được vượt quá 100 ký tự.');
      return;
    }

    setValidationError(null);
    onSearch(trimmed);
  };

  const handleClear = () => {
    setQuery('');
    setValidationError(null);
    setSuggestions([]);
    setIsOpen(false);
    setSelectedIndex(-1);
    setHasSearchedSuggestions(false);
    if (abortControllerRef.current) abortControllerRef.current.abort();
  };

  // Điều hướng bằng bàn phím (ArrowUp, ArrowDown, Enter, Escape)
  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (!isOpen || suggestions.length === 0) {
      if (e.key === 'ArrowDown' && suggestions.length > 0) {
        setIsOpen(true);
        setSelectedIndex(0);
        e.preventDefault();
      }
      return;
    }

    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setSelectedIndex((prev) => (prev + 1) % suggestions.length);
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setSelectedIndex((prev) => (prev - 1 + suggestions.length) % suggestions.length);
    } else if (e.key === 'Enter') {
      if (selectedIndex >= 0 && selectedIndex < suggestions.length) {
        e.preventDefault();
        const selected = suggestions[selectedIndex];
        handleSelectRecipe(selected);
      }
    } else if (e.key === 'Escape') {
      e.preventDefault();
      setIsOpen(false);
      setSelectedIndex(-1);
    }
  };

  const handleSelectRecipe = (recipe: SearchResult) => {
    setIsOpen(false);
    setSelectedIndex(-1);
    if (onSelectSuggestion) {
      onSelectSuggestion(recipe);
    } else if (router && typeof router.push === 'function') {
      router.push(`/recipes/${recipe.slug}`);
    } else if (typeof window !== 'undefined') {
      window.location.href = `/recipes/${recipe.slug}`;
    }
  };

  return (
    <div
      ref={containerRef}
      className={`relative w-full space-y-1.5 ${className}`}
      data-testid="search-box"
    >
      <form role="search" onSubmit={handleSubmit} className="relative flex items-center w-full">
        {/* Search Icon */}
        <div className="absolute left-4 top-1/2 -translate-y-1/2 pointer-events-none text-emerald-700" aria-hidden="true">
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
        </div>

        {/* Input Field with Combobox Accessibility */}
        <input
          type="text"
          data-testid="search-input"
          role="combobox"
          aria-autocomplete="list"
          aria-expanded={isOpen}
          aria-controls="search-autocomplete-list"
          aria-activedescendant={selectedIndex >= 0 ? `autocomplete-item-${selectedIndex}` : undefined}
          value={query}
          maxLength={100}
          disabled={isLoading}
          aria-label="Từ khóa tìm kiếm công thức"
          aria-invalid={Boolean(validationError)}
          aria-describedby={validationError ? 'search-validation-error' : undefined}
          onChange={handleInputChange}
          onKeyDown={handleKeyDown}
          onFocus={() => {
            if (enableAutocomplete && suggestions.length > 0) {
              setIsOpen(true);
            }
          }}
          placeholder={placeholder}
          className={`w-full pl-12 pr-32 py-3.5 text-sm sm:text-base rounded-2xl border bg-white text-slate-900 transition-all placeholder:text-slate-400 focus:outline-none focus:ring-4 shadow-xs ${
            validationError
              ? 'border-rose-300 focus:border-rose-500 focus:ring-rose-100'
              : 'border-emerald-200/90 focus:border-emerald-600 focus:ring-emerald-100 hover:border-emerald-400'
          }`}
        />

        {/* Clear Button */}
        {query && !isLoading && (
          <button
            type="button"
            data-testid="btn-clear-search"
            onClick={handleClear}
            className="absolute right-28 top-1/2 -translate-y-1/2 p-1.5 text-slate-400 hover:text-slate-600 rounded-full hover:bg-slate-100 transition-colors"
            title="Xóa từ khóa"
            aria-label="Xóa từ khóa"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        )}

        {/* Submit Button */}
        <button
          type="submit"
          data-testid="btn-submit-search"
          disabled={isLoading}
          className="absolute right-2 top-1/2 -translate-y-1/2 inline-flex items-center gap-1.5 px-4 sm:px-5 py-2.5 text-xs sm:text-sm font-semibold rounded-xl text-white bg-emerald-700 hover:bg-emerald-800 disabled:opacity-50 disabled:cursor-not-allowed transition-all shadow-sm hover:shadow"
        >
          {isLoading ? (
            <>
              <svg className="w-4 h-4 animate-spin text-emerald-200" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" strokeWidth="4" />
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
              </svg>
              <span>Tìm</span>
            </>
          ) : (
            <span>Tìm kiếm</span>
          )}
        </button>
      </form>

      {/* Validation Error Message */}
      {validationError && (
        <p
          id="search-validation-error"
          data-testid="search-validation-error"
          role="alert"
          className="text-xs text-rose-600 font-medium pl-3 flex items-center gap-1.5"
        >
          <svg className="w-3.5 h-3.5 shrink-0 text-rose-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <circle cx="12" cy="12" r="10" strokeWidth={2} />
            <line x1="12" y1="8" x2="12" y2="12" strokeWidth={2} strokeLinecap="round" />
            <line x1="12" y1="16" x2="12.01" y2="16" strokeWidth={2} strokeLinecap="round" />
          </svg>
          {validationError}
        </p>
      )}

      {/* 4. Autocomplete Dropdown Panel */}
      {enableAutocomplete && isOpen && (
        <div
          id="search-autocomplete-list"
          role="listbox"
          aria-label="Gợi ý công thức tìm kiếm"
          data-testid="search-autocomplete-dropdown"
          className="absolute left-0 right-0 top-full mt-2 z-50 bg-white rounded-2xl border border-emerald-100 shadow-xl overflow-hidden divide-y divide-slate-100 animate-in fade-in-50 duration-150"
        >
          {/* Header indicator */}
          <div className="px-4 py-2 bg-slate-50/80 flex items-center justify-between text-xs font-semibold text-slate-500">
            <span>Gợi ý công thức phù hợp</span>
            {isFetchingSuggestions && (
              <span className="flex items-center gap-1 text-emerald-700">
                <svg className="w-3 h-3 animate-spin" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                </svg>
                Đang tìm...
              </span>
            )}
          </div>

          {/* Skeletons while loading */}
          {isFetchingSuggestions && suggestions.length === 0 && (
            <div className="p-3 space-y-2" data-testid="autocomplete-loading">
              {[1, 2, 3].map((i) => (
                <div key={i} className="flex items-center gap-3 p-2 animate-pulse">
                  <div className="w-12 h-12 rounded-xl bg-slate-200 shrink-0" />
                  <div className="flex-1 space-y-1.5">
                    <div className="h-4 bg-slate-200 rounded-md w-3/4" />
                    <div className="h-3 bg-slate-100 rounded-md w-1/2" />
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* Empty State */}
          {!isFetchingSuggestions && suggestions.length === 0 && hasSearchedSuggestions && (
            <div data-testid="autocomplete-empty" className="p-6 text-center space-y-1">
              <p className="text-sm font-semibold text-slate-700">
                Không tìm thấy công thức phù hợp với &ldquo;{query.trim()}&rdquo;
              </p>
              <p className="text-xs text-slate-400">
                Nhấn Enter hoặc bấm nút &ldquo;Tìm kiếm&rdquo; để xem kết quả toàn diện.
              </p>
            </div>
          )}

          {/* List of Suggestions */}
          {suggestions.length > 0 && (
            <div className="max-h-80 overflow-y-auto">
              {suggestions.map((item, index) => {
                const isSelected = index === selectedIndex;
                const imgUrl = resolveImageUrl(item.primaryImageUrl);

                return (
                  <div
                    key={item.id}
                    id={`autocomplete-item-${index}`}
                    role="option"
                    tabIndex={-1}
                    aria-selected={isSelected}
                    data-testid={`autocomplete-item-${index}`}
                    onMouseEnter={() => setSelectedIndex(index)}
                    onClick={() => handleSelectRecipe(item)}
                    className={`flex items-center gap-3.5 p-3 sm:px-4 cursor-pointer transition-colors ${
                      isSelected
                        ? 'bg-emerald-50/90 text-emerald-950'
                        : 'hover:bg-slate-50 text-slate-800'
                    }`}
                  >
                    {/* Thumbnail */}
                    <div className="w-12 h-12 sm:w-14 sm:h-14 rounded-xl overflow-hidden bg-slate-100 border border-slate-200 shrink-0">
                      {imgUrl ? (
                        /* eslint-disable-next-line @next/next/no-img-element */
                        <img
                          src={imgUrl}
                          alt={item.title}
                          className="w-full h-full object-cover"
                          onError={(e) => {
                            (e.currentTarget as HTMLImageElement).src =
                              'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="60" height="60" viewBox="0 0 24 24" fill="none" stroke="%23059669" stroke-width="1.5"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="8.5" cy="8.5" r="1.5"/><path d="M21 15l-5-5L5 21"/></svg>';
                          }}
                        />
                      ) : (
                        <div className="w-full h-full flex items-center justify-center text-slate-300">
                          <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <rect x="3" y="3" width="18" height="18" rx="2" />
                            <circle cx="8.5" cy="8.5" r="1.5" />
                            <path d="M21 15l-5-5L5 21" />
                          </svg>
                        </div>
                      )}
                    </div>

                    {/* Metadata & Title */}
                    <div className="flex-1 min-w-0 space-y-1">
                      <div className="flex items-center gap-2 text-[11px]">
                        {item.categoryName && (
                          <span className="font-semibold text-emerald-700 truncate">
                            {item.categoryName}
                          </span>
                        )}
                        {item.cookTimeMinutes > 0 && (
                          <span className="text-slate-400">· {item.cookTimeMinutes} phút</span>
                        )}
                        {item.matchType === 'FullTextSearch' && (
                          <span className="hidden sm:inline-block px-1.5 py-0.2 rounded text-[10px] font-semibold bg-emerald-100 text-emerald-800">
                            FTS
                          </span>
                        )}
                      </div>

                      <h4
                        data-testid={`autocomplete-title-${index}`}
                        className="text-sm font-bold text-slate-900 truncate"
                      >
                        {item.title}
                      </h4>
                    </div>

                    {/* Arrow hint icon */}
                    <div className="text-slate-300 group-hover:text-emerald-700 shrink-0">
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                      </svg>
                    </div>
                  </div>
                );
              })}
            </div>
          )}

          {/* Footer view all link */}
          {suggestions.length > 0 && (
            <div className="p-2.5 bg-slate-50/90 text-center">
              <button
                type="button"
                onClick={() => handleSubmit()}
                className="text-xs font-semibold text-emerald-700 hover:text-emerald-900 transition-colors"
              >
                Xem tất cả kết quả tìm kiếm cho &ldquo;{query.trim()}&rdquo; →
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

export default SearchBox;
