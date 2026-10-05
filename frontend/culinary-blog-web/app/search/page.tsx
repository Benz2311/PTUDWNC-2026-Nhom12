'use client';

import React, { Suspense } from 'react';
import { useSearchParams } from 'next/navigation';
import { SearchContainer } from '@/components/search/SearchContainer';

function SearchPageContent() {
  const searchParams = useSearchParams();
  const initialQuery = searchParams.get('q') || '';
  const initialPage = Number(searchParams.get('page')) || 1;

  return (
    <main className="min-h-screen bg-gray-50/30">
      <SearchContainer initialQuery={initialQuery} initialPage={initialPage} />
    </main>
  );
}

export default function SearchPage() {
  return (
    <Suspense
      fallback={
        <div className="flex items-center justify-center min-h-[50vh] text-sm text-gray-500">
          Đang tải trang tìm kiếm...
        </div>
      }
    >
      <SearchPageContent />
    </Suspense>
  );
}
