import Link from 'next/link';
import type { ReactNode } from 'react';
import type { RecipeListItem, RecipeSummary } from '@/types/recipe';
import DifficultyBadge from './DifficultyBadge';
import RecipeStatusBadge from './RecipeStatusBadge';

const fallbackImages = [
  '/sample-images/food-01.svg',
  '/sample-images/food-02.svg',
  '/sample-images/food-03.svg',
  '/sample-images/food-04.svg',
  '/sample-images/food-05.svg',
];

type RecipeCardProps = {
  recipe: RecipeSummary | RecipeListItem;
  className?: string;
  showStatus?: boolean;
  actions?: ReactNode;
};

export default function RecipeCard({ recipe, className = '', showStatus = false, actions }: RecipeCardProps) {
  const imageUrl =
    recipe.primaryImageUrl ||
    recipe.coverImageUrl ||
    recipe.imageUrl ||
    fallbackImages[Math.abs(recipe.title.length) % fallbackImages.length];

  const categoryName =
    typeof recipe.category === 'object' && recipe.category
      ? recipe.category.name
      : (recipe.category || recipe.categoryName || 'Ẩm thực');

  const authorName =
    typeof recipe.author === 'object' && recipe.author
      ? recipe.author.displayName || (recipe.author as any).name || 'Đầu bếp'
      : (recipe.author || 'Đầu bếp');

  const prepTime = recipe.prepTimeMinutes ?? recipe.prepTime ?? 0;
  const cookTime = recipe.cookTimeMinutes ?? recipe.cookTime ?? 0;
  const totalMinutes = prepTime + cookTime;

  return (
    <article
      data-testid="recipe-card"
      className={`group relative flex flex-col bg-white rounded-2xl border border-gray-100 overflow-hidden shadow-xs hover:shadow-xl hover:border-emerald-300 hover:-translate-y-1 transition-all duration-300 recipe-card ${className}`.trim()}
    >
      <Link
        className="flex flex-col h-full block w-full focus:outline-none focus:ring-2 focus:ring-emerald-500 rounded-2xl recipe-card-link-wrapper cursor-pointer"
        href={`/recipes/${encodeURIComponent(recipe.slug)}`}
        aria-label={`Xem chi tiết công thức ${recipe.title}`}
      >
        {/* 1. Thumbnail Container (Tỷ lệ chuẩn 16:9) */}
        <div className="relative w-full aspect-[16/9] bg-gradient-to-br from-emerald-50 via-slate-50 to-slate-100 overflow-hidden recipe-image recipe-shared-image">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src={imageUrl}
            alt={recipe.title}
            className="w-full h-full object-cover transition-transform duration-500 group-hover:scale-105"
            loading="lazy"
            onError={(e) => {
              (e.currentTarget as HTMLImageElement).src = fallbackImages[0];
            }}
          />

          {/* Category Tag overlay góc trên bên trái */}
          <div className="absolute top-3 left-3 flex items-center gap-1.5 z-10">
            <span className="px-3 py-1 rounded-full text-xs font-bold bg-white/95 text-emerald-800 shadow-sm backdrop-blur-xs border border-white/60 recipe-tag">
              {categoryName}
            </span>
          </div>

          {/* Difficulty & Status Badges góc trên bên phải */}
          <div className="absolute top-3 right-3 flex items-center gap-1.5 z-10 recipe-card-badges">
            <DifficultyBadge difficulty={recipe.difficulty} />
            {showStatus && <RecipeStatusBadge status={recipe.status} />}
          </div>
        </div>

        {/* 2. Card Body Content */}
        <div className="p-5 flex-1 flex flex-col justify-between gap-3 bg-white recipe-body">
          <div className="space-y-2">
            <h3 className="text-base sm:text-lg font-bold text-gray-900 group-hover:text-emerald-700 transition-colors line-clamp-1 tracking-tight">
              {recipe.title}
            </h3>
            <p className="text-xs sm:text-sm text-gray-500 line-clamp-2 leading-relaxed">
              {recipe.description || 'Khám phá công thức nấu ăn thơm ngon, dễ làm chuẩn vị ngay tại nhà.'}
            </p>
          </div>

          {/* 3. Card Meta: Tác giả & Thời gian nấu */}
          <div className="pt-3 border-t border-gray-100 flex items-center justify-between text-xs text-gray-500">
            <div className="flex items-center gap-1.5 truncate max-w-[60%]">
              <span className="truncate font-medium text-gray-700">
                Tác giả: {authorName}
              </span>
            </div>
            <div className="flex items-center gap-1 text-emerald-700 font-semibold shrink-0">
              <svg className="w-3.5 h-3.5 text-amber-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <circle cx="12" cy="12" r="10" strokeWidth="2" />
                <polyline points="12 6 12 12 16 14" strokeWidth="2" />
              </svg>
              <span>{totalMinutes > 0 ? `${totalMinutes} phút` : '30 phút'}</span>
            </div>
          </div>
        </div>
      </Link>

      {actions && (
        <div className="p-3 border-t border-gray-100 bg-gray-50 card-actions recipe-card-actions">
          {actions}
        </div>
      )}
    </article>
  );
}
