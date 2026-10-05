'use client';

import React, { useState } from 'react';

export interface SearchBoxProps {
  initialQuery?: string;
  placeholder?: string;
  isLoading?: boolean;
  onSearch: (query: string) => void;
  className?: string;
}

export const SearchBox: React.FC<SearchBoxProps> = ({
  initialQuery = '',
  placeholder = 'Tìm kiếm công thức (vd: phở bò, bún chả, thịt kho...)',
  isLoading = false,
  onSearch,
  className = '',
}) => {
  const [query, setQuery] = useState<string>(initialQuery);
  const [validationError, setValidationError] = useState<string | null>(null);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
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
  };

  return (
    <div className={`w-full space-y-1.5 ${className}`} data-testid="search-box">
      <form onSubmit={handleSubmit} className="relative flex items-center w-full">
        {/* Search Icon */}
        <div className="absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none text-gray-400">
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
        </div>

        {/* Input Field */}
        <input
          type="text"
          data-testid="search-input"
          value={query}
          maxLength={100}
          disabled={isLoading}
          onChange={(e) => {
            setQuery(e.target.value);
            if (validationError) setValidationError(null);
          }}
          placeholder={placeholder}
          className={`w-full pl-11 pr-28 py-3 text-sm sm:text-base rounded-xl border bg-white text-gray-900 transition-all placeholder:text-gray-400 focus:outline-none focus:ring-2 shadow-xs ${
            validationError
              ? 'border-red-300 focus:border-red-500 focus:ring-red-100'
              : 'border-gray-300 focus:border-green-700 focus:ring-green-100'
          }`}
        />

        {/* Clear Button */}
        {query && !isLoading && (
          <button
            type="button"
            data-testid="btn-clear-search"
            onClick={handleClear}
            className="absolute right-24 top-1/2 -translate-y-1/2 p-1 text-gray-400 hover:text-gray-600 rounded-full hover:bg-gray-100 transition-colors"
            title="Xóa từ khóa"
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
          className="absolute right-2 top-1/2 -translate-y-1/2 inline-flex items-center gap-1.5 px-4 py-2 text-xs sm:text-sm font-semibold rounded-lg text-white bg-green-800 hover:bg-green-900 disabled:opacity-50 disabled:cursor-not-allowed transition-colors shadow-xs"
        >
          {isLoading ? (
            <>
              <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
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
        <p data-testid="search-validation-error" className="text-xs text-red-600 font-medium pl-3 flex items-center gap-1">
          <svg className="w-3.5 h-3.5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <circle cx="12" cy="12" r="10" strokeWidth="2" />
            <line x1="12" y1="8" x2="12" y2="12" strokeWidth="2" strokeLinecap="round" />
            <line x1="12" y1="16" x2="12.01" y2="16" strokeWidth="2" strokeLinecap="round" />
          </svg>
          {validationError}
        </p>
      )}
    </div>
  );
};
