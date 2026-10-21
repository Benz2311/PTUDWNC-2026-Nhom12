'use client';

import React, { Suspense } from 'react';
import { useSearchParams } from 'next/navigation';
import { SearchContainer } from '@/components/search/SearchContainer';

function SearchPageContent() {
  const searchParams = useSearchParams();
  const initialQuery = searchParams.get('q') || '';

  return (
    <div className="min-h-screen bg-gradient-to-b from-emerald-50/60 via-slate-50 to-white py-8 px-4 sm:px-6">
      <div className="max-w-5xl mx-auto">
        <SearchContainer
          initialQuery={initialQuery}
          autoSearchOnMount={Boolean(initialQuery.trim().length >= 2)}
        />
      </div>
    </div>
  );
}

export default function SearchPage() {
  return (
    <Suspense
      fallback={
        <div className="flex items-center justify-center min-h-[60vh] text-sm text-emerald-800 font-medium">
          <svg className="w-5 h-5 animate-spin mr-2 text-emerald-600" fill="none" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
          </svg>
          Đang tải trang tìm kiếm...
        </div>
      }
    >
      <SearchPageContent />
    </Suspense>
  );
}
