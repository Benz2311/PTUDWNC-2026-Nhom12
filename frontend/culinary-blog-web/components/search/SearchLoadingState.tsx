'use client';

import React from 'react';

export interface SearchLoadingStateProps {
  count?: number;
}

export function SearchLoadingState({ count = 3 }: SearchLoadingStateProps) {
  const items = Array.from({ length: count }, (_, i) => i);

  return (
    <div data-testid="search-loading" className="space-y-4 animate-pulse">
      {items.map((n) => (
        <div
          key={n}
          data-testid={`search-loading-skeleton-${n}`}
          className="flex flex-col sm:flex-row bg-white rounded-xl border border-gray-200 overflow-hidden sm:h-44"
        >
          {/* Image skeleton */}
          <div className="w-full sm:w-56 h-40 sm:h-full bg-gray-200 shrink-0" />

          {/* Text content skeleton */}
          <div className="p-4 sm:p-5 flex-1 space-y-3 flex flex-col justify-center">
            <div className="flex items-center gap-2">
              <div className="h-4 bg-gray-200 rounded-md w-24" />
              <div className="h-4 bg-gray-200 rounded-md w-16" />
            </div>
            <div className="h-6 bg-gray-200 rounded-md w-3/4" />
            <div className="h-4 bg-gray-200 rounded-md w-full" />
            <div className="h-4 bg-gray-200 rounded-md w-1/2" />
          </div>
        </div>
      ))}
    </div>
  );
}

export default SearchLoadingState;
