'use client';

import React, { Suspense } from 'react';
import { useSearchParams } from 'next/navigation';
import { SearchContainer } from '@/components/search/SearchContainer';

function SearchPageContent() {
  const searchParams = useSearchParams();
  const initialQuery = searchParams.get('q') || '';

  return (
    <div className="container mx-auto max-w-5xl px-4 py-8">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900 tracking-tight">
          Tìm kiếm công thức nấu ăn
        </h1>
        <p className="text-sm text-gray-500 mt-1">
          Khám phá hàng ngàn công thức hấp dẫn từ cộng đồng ẩm thực
        </p>
      </div>

      <SearchContainer
        initialQuery={initialQuery}
        autoSearchOnMount={Boolean(initialQuery.trim().length >= 2)}
      />
    </div>
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
