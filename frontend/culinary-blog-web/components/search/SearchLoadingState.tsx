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
          className="flex flex-col sm:flex-row bg-white rounded-2xl border border-emerald-100/80 overflow-hidden sm:h-44 shadow-xs"
        >
          {/* Image skeleton */}
          <div className="w-full sm:w-60 h-44 sm:h-full bg-slate-200/80 shrink-0" />

          {/* Text content skeleton */}
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
  );
}

export default SearchLoadingState;
