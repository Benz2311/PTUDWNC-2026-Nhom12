'use client';

import { useState } from 'react';
import Link from 'next/link';
import type { RecipeDetail } from '@/types/recipe';
import DifficultyBadge from './DifficultyBadge';
import RecipeMeta from './RecipeMeta';
import RecipeStatusBadge from './RecipeStatusBadge';
import RecipeTimeInfo from './RecipeTimeInfo';

const nutritionLabels = [
  ['calories', 'Calories', 'kcal'],
  ['protein', 'Protein', 'g'],
  ['carbohydrates', 'Carbohydrates', 'g'],
  ['fat', 'Chất béo', 'g'],
  ['fiber', 'Chất xơ', 'g'],
  ['sodium', 'Natri', 'mg'],
] as const;

export function RecipeDetailView({ recipe }: { recipe: RecipeDetail }) {
  const images = [...(recipe.images || [])].sort(
    (left, right) =>
      Number(Boolean(right.isPrimary)) - Number(Boolean(left.isPrimary)) ||
      (left.sortOrder ?? left.orderIndex ?? 0) - (right.sortOrder ?? right.orderIndex ?? 0)
  );
  const [activeImage, setActiveImage] = useState(0);
  const selectedImage = images[activeImage];
  const primaryCoverUrl =
    selectedImage?.url ??
    selectedImage?.originalUrl ??
    recipe.coverImageUrl ??
    '/sample-images/food-01.svg';

  const categoryName =
    typeof recipe.category === 'object' && recipe.category
      ? recipe.category.name
      : String(recipe.category || 'Danh mục');
  const categorySlug =
    typeof recipe.category === 'object' && recipe.category?.slug
      ? recipe.category.slug
      : encodeURIComponent(categoryName.toLowerCase().replace(/\s+/g, '-'));

  return (
    <article className="recipe-detail recipe-detail-view mx-auto max-w-6xl px-4 py-8">
      <nav aria-label="Breadcrumb" className="mb-6 flex flex-wrap items-center gap-2 text-sm text-slate-500">
        <Link href="/" className="hover:text-emerald-700">
          Trang chủ
        </Link>
        <span>/</span>
        <Link href={`/categories/${categorySlug}`} className="hover:text-emerald-700">
          {categoryName}
        </Link>
        <span>/</span>
        <span className="font-medium text-slate-800">{recipe.title}</span>
      </nav>

      <header className="recipe-detail-header mb-8">
        <div className="recipe-detail-badges flex flex-wrap items-center gap-2 mb-3">
          <RecipeStatusBadge status={String(recipe.status)} />
          <DifficultyBadge difficulty={String(recipe.difficulty)} />
        </div>
        <h1 className="flow-title text-3xl md:text-4xl font-bold text-slate-900 mb-3">{recipe.title}</h1>
        {recipe.description && <p className="detail-lead text-lg text-slate-600 mb-4">{recipe.description}</p>}
        <RecipeMeta category={recipe.category} author={recipe.author} publishedAt={recipe.publishedAt} />
        <RecipeTimeInfo
          prepTimeMinutes={recipe.prepTimeMinutes ?? recipe.prepTime}
          cookTimeMinutes={recipe.cookTimeMinutes ?? recipe.cookTime}
          servings={recipe.servings}
        />
      </header>

      <section className="recipe-detail-gallery mb-10" aria-label="Thư viện ảnh công thức">
        <div
          className="recipe-detail-cover h-72 md:h-96 w-full rounded-2xl bg-cover bg-center shadow-sm border border-slate-200"
          role="img"
          aria-label={selectedImage?.altText || recipe.title}
          style={{ backgroundImage: `url("${primaryCoverUrl}")` }}
        />
        {images.length > 1 && (
          <div className="recipe-detail-thumbnails flex gap-3 mt-4 overflow-x-auto pb-2" aria-label="Chọn ảnh công thức">
            {images.map((image, index) => (
              <button
                type="button"
                key={image.id}
                className={`recipe-detail-thumbnail h-20 w-28 shrink-0 rounded-xl bg-cover bg-center border-2 transition ${
                  index === activeImage ? 'border-emerald-600 ring-2 ring-emerald-200 is-active' : 'border-transparent opacity-75 hover:opacity-100'
                }`}
                onClick={() => setActiveImage(index)}
                aria-label={`Xem ảnh ${index + 1}`}
                aria-pressed={index === activeImage}
                style={{ backgroundImage: `url("${image.url ?? image.originalUrl}")` }}
              />
            ))}
          </div>
        )}
      </section>

      <div className="detail-columns recipe-detail-columns grid grid-cols-1 lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2 space-y-8">
          <section className="detail-section bg-white p-6 rounded-2xl border border-slate-200 shadow-xs">
            <h2 className="text-xl font-bold text-slate-900 mb-3">Giới thiệu</h2>
            <p className="detail-content text-slate-700 leading-relaxed whitespace-pre-line">
              {recipe.content || recipe.description || 'Chưa có nội dung mô tả chi tiết.'}
            </p>
          </section>

          <section className="detail-section bg-white p-6 rounded-2xl border border-slate-200 shadow-xs">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-xl font-bold text-slate-900">Nguyên liệu</h2>
              <span className="text-sm text-slate-500">{recipe.ingredients?.length ?? 0} mục</span>
            </div>
            {!recipe.ingredients || recipe.ingredients.length === 0 ? (
              <p className="detail-content text-slate-500">Chưa có nguyên liệu được cung cấp.</p>
            ) : (
              <ul className="ingredient-list divide-y divide-slate-100">
                {[...recipe.ingredients]
                  .sort(
                    (left, right) =>
                      (left.orderIndex ?? left.sortOrder ?? 0) - (right.orderIndex ?? right.sortOrder ?? 0)
                  )
                  .map((ingredient) => (
                    <li className="ingredient-item py-3 flex items-center justify-between gap-4" key={ingredient.id}>
                      <span className="text-slate-800 font-medium">
                        {ingredient.name}
                        {ingredient.notes && <small className="block text-xs text-slate-500">{ingredient.notes}</small>}
                      </span>
                      <strong className="text-emerald-700 font-semibold shrink-0">
                        {ingredient.quantity ?? '—'} {ingredient.unit ?? ''}
                      </strong>
                    </li>
                  ))}
              </ul>
            )}
          </section>

          <section className="detail-section bg-white p-6 rounded-2xl border border-slate-200 shadow-xs">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-xl font-bold text-slate-900">Các bước thực hiện</h2>
              <span className="text-sm text-slate-500">{recipe.steps?.length ?? 0} bước</span>
            </div>
            {!recipe.steps || recipe.steps.length === 0 ? (
              <p className="detail-content text-slate-500">Chưa có hướng dẫn chế biến.</p>
            ) : (
              <ol className="step-list space-y-6">
                {[...recipe.steps]
                  .sort((left, right) => left.stepNumber - right.stepNumber)
                  .map((step) => (
                    <li className="step-item flex gap-4 p-4 rounded-xl bg-slate-50 border border-slate-100" key={step.id}>
                      <strong className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-emerald-600 text-white text-sm font-bold">
                        {String(step.stepNumber).padStart(2, '0')}
                      </strong>
                      <div className="flex-1 space-y-2">
                        <div className="flex flex-wrap items-center justify-between gap-2">
                          <h3 className="font-bold text-slate-900">{step.title || `Bước ${step.stepNumber}`}</h3>
                          {step.timerMinutes != null && step.timerMinutes > 0 && (
                            <span className="inline-flex items-center rounded-full bg-amber-100 px-2.5 py-0.5 text-xs font-medium text-amber-800">
                              ⏱ {step.timerMinutes} phút
                            </span>
                          )}
                        </div>
                        <p className="text-slate-700 leading-relaxed">{step.description}</p>
                        {step.imageUrl && (
                          <div
                            className="mt-3 h-48 w-full max-w-md rounded-xl bg-cover bg-center border border-slate-200"
                            role="img"
                            aria-label={`Ảnh minh họa bước ${step.stepNumber}`}
                            style={{ backgroundImage: `url("${step.imageUrl}")` }}
                          />
                        )}
                      </div>
                    </li>
                  ))}
              </ol>
            )}
          </section>
        </div>

        <aside className="nutrition-panel bg-white p-6 rounded-2xl border border-slate-200 shadow-xs h-fit">
          <span className="panel-label text-xs font-semibold uppercase tracking-wider text-emerald-700">
            Thông tin dinh dưỡng
          </span>
          <h2 className="text-xl font-bold text-slate-900 mt-1 mb-4">Mỗi khẩu phần</h2>
          {recipe.nutrition ? (
            <dl className="nutrition-list divide-y divide-slate-100">
              {nutritionLabels.map(([key, label, unit]) => {
                const val = recipe.nutrition?.[key as keyof typeof recipe.nutrition];
                return (
                  <div key={key} className="py-2.5 flex items-center justify-between text-sm">
                    <dt className="text-slate-600">{label}</dt>
                    <dd className="font-semibold text-slate-900">
                      {val ?? '—'}
                      {val == null ? '' : ` ${unit}`}
                    </dd>
                  </div>
                );
              })}
            </dl>
          ) : (
            <p className="text-slate-500 text-sm">Chưa có thông tin dinh dưỡng.</p>
          )}
        </aside>
      </div>

      <div className="mt-8">
        <Link
          className="inline-flex items-center gap-2 rounded-xl border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-50"
          href="/search"
        >
          ← Khám phá thêm công thức
        </Link>
      </div>
    </article>
  );
}

export default RecipeDetailView;
