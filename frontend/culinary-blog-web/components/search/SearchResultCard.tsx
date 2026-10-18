 

'use client';

import React from 'react';
import Link from 'next/link';
import type { SearchResult } from '@/types/search';

export interface SearchResultCardProps {
  recipe: SearchResult;
  index: number;
}

export function SearchResultCard({ recipe, index }: SearchResultCardProps) {
  // Format difficulty badge label & color
  const getDifficultyMeta = (difficulty: string | number) => {
    const raw = String(difficulty).toLowerCase();
    if (raw === '0' || raw === 'easy' || raw === 'dễ') {
      return { label: 'Dễ', color: 'bg-green-50 text-green-700 border-green-200' };
    }
    if (raw === '1' || raw === 'medium' || raw === 'trung bình') {
      return { label: 'Trung bình', color: 'bg-amber-50 text-amber-700 border-amber-200' };
    }
    return { label: 'Khó', color: 'bg-red-50 text-red-700 border-red-200' };
  };

  const diffMeta = getDifficultyMeta(recipe.difficulty);
  const totalMinutes = (recipe.prepTimeMinutes || 0) + (recipe.cookTimeMinutes || 0);

  return (
    <article
      data-testid={`search-card-${index}`}
      className="group flex flex-col sm:flex-row bg-white rounded-xl border border-gray-200 overflow-hidden hover:border-gray-300 hover:shadow-xs transition-all"
    >
      {/* Thumbnail */}
      <div className="relative w-full sm:w-56 aspect-video sm:aspect-auto bg-gray-100 shrink-0 overflow-hidden">
        <Link href={`/recipes/${recipe.slug}`} className="block w-full h-full">
          {recipe.primaryImageUrl ? (
            /* eslint-disable-next-line @next/next/no-img-element */
            <img
              src={recipe.primaryImageUrl}
              alt={recipe.title}
              className="w-full h-full object-cover transition-transform duration-300 group-hover:scale-102"
              onError={(e) => {
                (e.currentTarget as HTMLImageElement).src =
                  'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100" viewBox="0 0 24 24" fill="none" stroke="%239ca3af" stroke-width="1.5"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="8.5" cy="8.5" r="1.5"/><path d="M21 15l-5-5L5 21"/></svg>';
              }}
            />
          ) : (
            <div className="w-full h-full min-h-[140px] flex items-center justify-center text-gray-300 bg-gray-50">
              <svg className="w-10 h-10" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <rect x="3" y="3" width="18" height="18" rx="2" />
                <circle cx="8.5" cy="8.5" r="1.5" />
                <path d="M21 15l-5-5L5 21" />
              </svg>
            </div>
          )}
        </Link>

        {/* Category tag overlaid on mobile */}
        {recipe.categoryName && (
          <span className="sm:hidden absolute top-2.5 left-2.5 px-2 py-0.5 rounded text-[11px] font-semibold bg-black/60 text-white backdrop-blur-xs">
            {recipe.categoryName}
          </span>
        )}
      </div>

      {/* Main Content */}
      <div className="p-4 sm:p-5 flex-1 flex flex-col justify-between gap-3">
        <div className="space-y-1.5">
          {/* Metadata Bar */}
          <div className="flex flex-wrap items-center gap-2 text-xs">
            {recipe.categoryName && (
              <span className="hidden sm:inline-block font-semibold text-green-800">
                {recipe.categoryName}
              </span>
            )}

            <span className={`inline-flex items-center px-2 py-0.5 rounded text-[11px] font-medium border ${diffMeta.color}`}>
              {diffMeta.label}
            </span>

            {/* FTS or Fuzzy match badge */}
            {recipe.matchType === 'FullTextSearch' && (
              <span
                data-testid={`badge-fts-${index}`}
                className="inline-flex items-center px-2 py-0.5 rounded text-[11px] font-medium bg-emerald-50 text-emerald-800 border border-emerald-200"
              >
                Khớp toàn văn (FTS)
              </span>
            )}

            {recipe.matchType === 'FuzzyTrigram' && (
              <span
                data-testid={`badge-fuzzy-${index}`}
                className="inline-flex items-center px-2 py-0.5 rounded text-[11px] font-medium bg-amber-50 text-amber-800 border border-amber-200"
              >
                Gợi ý tương đồng (Fuzzy)
              </span>
            )}

            {totalMinutes > 0 && (
              <span className="inline-flex items-center gap-1 text-gray-500">
                <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <circle cx="12" cy="12" r="10" strokeWidth="2" />
                  <polyline points="12 6 12 12 16 14" strokeWidth="2" />
                </svg>
                {totalMinutes} phút
              </span>
            )}

            {recipe.servings > 0 && (
              <span className="inline-flex items-center gap-1 text-gray-500">
                <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
                </svg>
                {recipe.servings} phần
              </span>
            )}
          </div>

          {/* Title */}
          <h3
            data-testid={`recipe-title-${index}`}
            className="text-base sm:text-lg font-bold text-gray-900 group-hover:text-green-800 transition-colors line-clamp-1"
          >
            <Link href={`/recipes/${recipe.slug}`}>
              {recipe.title}
            </Link>
          </h3>

          {/* Description snippet */}
          <p className="text-xs sm:text-sm text-gray-600 line-clamp-2 leading-relaxed">
            {recipe.description}
          </p>
        </div>

        {/* Author & Footer */}
        <div className="flex items-center justify-between text-xs text-gray-500 pt-2 border-t border-gray-100">
          <span className="truncate">
            Bởi <strong className="text-gray-700 font-medium">{recipe.authorName || 'Đầu bếp'}</strong>
          </span>

          <Link
            href={`/recipes/${recipe.slug}`}
            className="inline-flex items-center gap-1 text-green-800 font-semibold hover:underline"
          >
            <span>Xem chi tiết</span>
            <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
            </svg>
          </Link>
        </div>
      </div>
    </article>
  );
}

export default SearchResultCard;
