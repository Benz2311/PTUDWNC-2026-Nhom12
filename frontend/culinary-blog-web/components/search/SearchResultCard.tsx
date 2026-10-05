'use client';

import React from 'react';
import type { RecipeSearchResult } from '@/types/search';

interface SearchResultCardProps {
  recipe: RecipeSearchResult;
  index: number;
}

export const SearchResultCard: React.FC<SearchResultCardProps> = ({ recipe, index }) => {
  const formatDifficulty = (diff?: string | number | null) => {
    if (diff === null || diff === undefined) return null;
    const str = String(diff).toLowerCase();
    if (str === 'easy' || str === '1') return { label: 'Dễ', color: 'bg-emerald-50 text-emerald-700 border-emerald-200' };
    if (str === 'medium' || str === '2') return { label: 'Trung bình', color: 'bg-amber-50 text-amber-700 border-amber-200' };
    if (str === 'hard' || str === '3') return { label: 'Khó', color: 'bg-rose-50 text-rose-700 border-rose-200' };
    return { label: String(diff), color: 'bg-gray-50 text-gray-700 border-gray-200' };
  };

  const difficultyInfo = formatDifficulty(recipe.difficulty);
  const totalTime = (recipe.prepTimeMinutes ?? 0) + (recipe.cookTimeMinutes ?? 0);

  return (
    <article
      data-testid={`search-card-${index}`}
      className="group flex flex-col sm:flex-row bg-white rounded-xl border border-gray-200 overflow-hidden hover:border-gray-300 hover:shadow-xs transition-all"
    >
      {/* Thumbnail */}
      <div className="relative sm:w-56 aspect-video sm:aspect-auto shrink-0 bg-gray-100 overflow-hidden">
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
          <div className="w-full h-full flex items-center justify-center text-gray-300">
            <svg className="w-10 h-10" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
          </div>
        )}

        {/* Match Type Badge */}
        {recipe.matchType && (
          <div className="absolute top-2.5 left-2.5">
            {recipe.matchType === 'FullTextSearch' ? (
              <span
                data-testid={`badge-fts-${index}`}
                className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[11px] font-semibold bg-green-800 text-white shadow-xs"
              >
                <svg className="w-3 h-3 text-green-200" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                </svg>
                Khớp toàn văn (FTS)
              </span>
            ) : recipe.matchType === 'FuzzyTrigram' ? (
              <span
                data-testid={`badge-fuzzy-${index}`}
                className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[11px] font-semibold bg-amber-600 text-white shadow-xs"
              >
                <svg className="w-3 h-3 text-amber-200" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                </svg>
                Gợi ý tương đồng (Fuzzy)
              </span>
            ) : null}
          </div>
        )}
      </div>

      {/* Details */}
      <div className="p-4 sm:p-5 flex-1 flex flex-col justify-between gap-3">
        <div className="space-y-2">
          {/* Category & Badges */}
          <div className="flex flex-wrap items-center gap-2">
            {recipe.categoryName && (
              <span className="text-xs font-semibold text-green-800 bg-green-50 px-2.5 py-0.5 rounded-md border border-green-200">
                {recipe.categoryName}
              </span>
            )}

            {difficultyInfo && (
              <span className={`text-[11px] font-medium px-2 py-0.5 rounded-md border ${difficultyInfo.color}`}>
                {difficultyInfo.label}
              </span>
            )}

            {totalTime > 0 && (
              <span className="inline-flex items-center gap-1 text-[11px] font-medium text-gray-500 bg-gray-50 px-2 py-0.5 rounded-md border border-gray-200">
                <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <circle cx="12" cy="12" r="10" strokeWidth="2" />
                  <path d="M12 6v6l4 2" strokeWidth="2" strokeLinecap="round" />
                </svg>
                {totalTime} phút
              </span>
            )}
          </div>

          {/* Title */}
          <h3
            data-testid={`recipe-title-${index}`}
            className="text-base sm:text-lg font-bold text-gray-900 group-hover:text-green-800 transition-colors line-clamp-1"
          >
            {recipe.title}
          </h3>

          {/* Description */}
          {recipe.description && (
            <p className="text-xs sm:text-sm text-gray-600 line-clamp-2 leading-relaxed">
              {recipe.description}
            </p>
          )}
        </div>

        {/* Footer info: Author & Date */}
        <div className="flex items-center justify-between pt-2 border-t border-gray-100 text-[11px] text-gray-500">
          <div className="flex items-center gap-1.5">
            <svg className="w-3.5 h-3.5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
            </svg>
            <span className="font-medium text-gray-700">{recipe.authorName || 'Bếp trưởng'}</span>
          </div>

          <a
            href={`/recipes/${recipe.slug}`}
            className="inline-flex items-center gap-1 font-semibold text-green-800 hover:text-green-900 hover:underline"
          >
            Xem chi tiết
            <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
            </svg>
          </a>
        </div>
      </div>
    </article>
  );
};
