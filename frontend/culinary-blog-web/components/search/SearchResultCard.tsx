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
      return { label: 'Dễ', color: 'bg-emerald-50 text-emerald-800 border-emerald-200' };
    }
    if (raw === '1' || raw === 'medium' || raw === 'trung bình') {
      return { label: 'Trung bình', color: 'bg-amber-50 text-amber-800 border-amber-200' };
    }
    return { label: 'Khó', color: 'bg-rose-50 text-rose-800 border-rose-200' };
  };

  const diffMeta = getDifficultyMeta(recipe.difficulty);
  const totalMinutes = (recipe.prepTimeMinutes || 0) + (recipe.cookTimeMinutes || 0);

  return (
    <article
      data-testid={`search-card-${index}`}
      className="group flex flex-col sm:flex-row bg-white rounded-2xl border border-emerald-100/90 overflow-hidden hover:border-emerald-300 hover:shadow-md transition-all duration-300 shadow-xs"
    >
      {/* Thumbnail */}
      <div className="relative w-full sm:w-60 aspect-video sm:aspect-auto bg-slate-100 shrink-0 overflow-hidden">
        <Link href={`/recipes/${recipe.slug}`} className="block w-full h-full">
          {recipe.primaryImageUrl ? (
            /* eslint-disable-next-line @next/next/no-img-element */
            <img
              src={recipe.primaryImageUrl}
              alt={recipe.title}
              className="w-full h-full object-cover transition-transform duration-500 group-hover:scale-105"
              onError={(e) => {
                (e.currentTarget as HTMLImageElement).src =
                  'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100" viewBox="0 0 24 24" fill="none" stroke="%23059669" stroke-width="1.5"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="8.5" cy="8.5" r="1.5"/><path d="M21 15l-5-5L5 21"/></svg>';
              }}
            />
          ) : (
            <div className="w-full h-full min-h-[150px] flex items-center justify-center text-slate-300 bg-slate-50">
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
          <span className="sm:hidden absolute top-2.5 left-2.5 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-950/70 text-white backdrop-blur-xs">
            {recipe.categoryName}
          </span>
        )}
      </div>

      {/* Main Content */}
      <div className="p-4 sm:p-5 flex-1 flex flex-col justify-between gap-3">
        <div className="space-y-2">
          {/* Metadata Bar */}
          <div className="flex flex-wrap items-center gap-2 text-xs">
            {recipe.categoryName && (
              <span className="hidden sm:inline-block font-semibold px-2.5 py-0.5 rounded-full bg-emerald-100 text-emerald-800 text-[11px]">
                {recipe.categoryName}
              </span>
            )}

            <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold border ${diffMeta.color}`}>
              {diffMeta.label}
            </span>

            {/* FTS or Fuzzy match badge */}
            {recipe.matchType === 'FullTextSearch' && (
              <span
                data-testid={`badge-fts-${index}`}
                className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-100 text-emerald-800 border border-emerald-200"
              >
                <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" />
                Khớp toàn văn (FTS)
              </span>
            )}

            {recipe.matchType === 'FuzzyTrigram' && (
              <span
                data-testid={`badge-fuzzy-${index}`}
                className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-amber-100 text-amber-800 border border-amber-200"
              >
                <span className="w-1.5 h-1.5 rounded-full bg-amber-500" />
                Gợi ý tương đồng (Fuzzy)
              </span>
            )}

            {totalMinutes > 0 && (
              <span className="inline-flex items-center gap-1 text-slate-500 text-[11px]">
                <svg className="w-3.5 h-3.5 text-emerald-700" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <circle cx="12" cy="12" r="10" strokeWidth="2" />
                  <polyline points="12 6 12 12 16 14" strokeWidth="2" />
                </svg>
                {totalMinutes} phút
              </span>
            )}

            {recipe.servings > 0 && (
              <span className="inline-flex items-center gap-1 text-slate-500 text-[11px]">
                <svg className="w-3.5 h-3.5 text-emerald-700" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
                </svg>
                {recipe.servings} phần
              </span>
            )}
          </div>

          {/* Title */}
          <h3
            data-testid={`recipe-title-${index}`}
            className="text-base sm:text-lg font-bold text-slate-900 group-hover:text-emerald-700 transition-colors line-clamp-1"
          >
            <Link href={`/recipes/${recipe.slug}`}>
              {recipe.title}
            </Link>
          </h3>

          {/* Description snippet */}
          <p className="text-xs sm:text-sm text-slate-600 line-clamp-2 leading-relaxed">
            {recipe.description}
          </p>
        </div>

        {/* Author & Footer */}
        <div className="flex items-center justify-between text-xs text-slate-500 pt-3 border-t border-emerald-50">
          <span className="truncate">
            Bởi <strong className="text-slate-800 font-semibold">{recipe.authorName || 'Đầu bếp'}</strong>
          </span>

          <Link
            href={`/recipes/${recipe.slug}`}
            className="inline-flex items-center gap-1 text-emerald-700 font-semibold hover:text-emerald-900 transition-colors"
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
