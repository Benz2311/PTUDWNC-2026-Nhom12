'use client';

import { useEffect, useState } from 'react';
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
  const recipeImages = [...(recipe.images || [])].sort(
    (left, right) =>
      Number(Boolean(right.isPrimary)) - Number(Boolean(left.isPrimary)) ||
      (left.sortOrder ?? left.orderIndex ?? 0) - (right.sortOrder ?? right.orderIndex ?? 0)
  );
  const images = recipeImages.length > 0 || !recipe.coverImageUrl
    ? recipeImages
    : [{
        id: `${recipe.id}-cover`,
        originalUrl: recipe.coverImageUrl,
        mediumUrl: recipe.coverImageUrl,
        thumbnailUrl: recipe.coverImageUrl,
        altText: recipe.title,
        isPrimary: true,
        orderIndex: 0,
        url: recipe.coverImageUrl,
      }];
  const [activeImage, setActiveImage] = useState(0);
  const selectedImage = images[activeImage];

  useEffect(() => {
    setActiveImage(0);
  }, [recipe.id]);

  return (
    <article className="recipe-detail recipe-detail-view rounded-2xl border border-slate-200 bg-white p-5 shadow-sm md:p-8">
      <header className="recipe-detail-header text-center">
        <div className="recipe-detail-badges">
          <RecipeStatusBadge status={String(recipe.status)} />
          <DifficultyBadge difficulty={String(recipe.difficulty)} />
        </div>
        <h1 className="flow-title mt-4 text-3xl font-bold text-slate-950 md:text-5xl">{recipe.title}</h1>
        {recipe.description && <p className="detail-lead mx-auto mt-3 max-w-3xl text-slate-600">{recipe.description}</p>}
        <RecipeMeta category={recipe.category} author={recipe.author} publishedAt={recipe.publishedAt} />
        <RecipeTimeInfo
          prepTimeMinutes={recipe.prepTimeMinutes ?? recipe.prepTime}
          cookTimeMinutes={recipe.cookTimeMinutes ?? recipe.cookTime}
          servings={recipe.servings}
        />
      </header>

      <section className="recipe-detail-gallery" aria-label="Thư viện ảnh công thức">
        {selectedImage ? (
          <div
            className="recipe-detail-cover"
            role="img"
            aria-label={selectedImage.altText || recipe.title}
            style={{
              backgroundImage: `url("${selectedImage.url ?? selectedImage.mediumUrl ?? selectedImage.originalUrl}")`,
            }}
          />
        ) : (
          <div className="recipe-detail-cover recipe-detail-cover-empty" role="img" aria-label="Công thức chưa có ảnh">
            <span>Chưa có ảnh công thức</span>
          </div>
        )}
        {images.length > 1 && (
          <div className="recipe-detail-thumbnails" aria-label="Chọn ảnh công thức">
            {images.map((image, index) => (
              <button
                type="button"
                key={image.id}
                className={`recipe-detail-thumbnail${index === activeImage ? ' is-active' : ''}`}
                onClick={() => setActiveImage(index)}
                aria-label={`Xem ảnh ${index + 1}`}
                aria-pressed={index === activeImage}
                style={{ backgroundImage: `url("${image.url ?? image.originalUrl}")` }}
              />
            ))}
          </div>
        )}
      </section>

      <div className="detail-columns recipe-detail-columns mt-8 grid gap-8 md:grid-cols-[minmax(0,1fr)_280px]">
        <div>
          <section className="detail-section">
            <h2 className="text-xl font-bold text-slate-900">Giới thiệu</h2>
            <p className="detail-content">{recipe.content}</p>
          </section>

          <section className="detail-section mt-8">
            <h2 className="text-xl font-bold text-slate-900">Nguyên liệu</h2>
            {!recipe.ingredients || recipe.ingredients.length === 0 ? (
              <p className="detail-content">Chưa có nguyên liệu được cung cấp.</p>
            ) : (
              <ul className="ingredient-list mt-4 grid gap-2">
                {[...recipe.ingredients]
                  .sort(
                    (left, right) =>
                      (left.sortOrder ?? left.orderIndex ?? 0) - (right.sortOrder ?? right.orderIndex ?? 0)
                  )
                  .map((ingredient) => (
                    <li className="ingredient-item flex items-start justify-between gap-4 rounded-xl border border-slate-200 bg-slate-50 px-4 py-3" key={ingredient.id}>
                      <span>
                        {ingredient.name}
                        {ingredient.notes && <small>{ingredient.notes}</small>}
                      </span>
                      <strong>
                        {ingredient.quantity ?? '—'} {ingredient.unit ?? ''}
                      </strong>
                    </li>
                  ))}
              </ul>
            )}
          </section>

          <section className="detail-section mt-8">
            <h2 className="text-xl font-bold text-slate-900">Các bước thực hiện</h2>
            {!recipe.steps || recipe.steps.length === 0 ? (
              <p className="detail-content">Chưa có hướng dẫn chế biến.</p>
            ) : (
              <ol className="step-list mt-4 grid gap-3">
                {[...recipe.steps]
                  .sort((left, right) => left.stepNumber - right.stepNumber)
                  .map((step) => (
                    <li className="step-item grid gap-4 rounded-xl border border-slate-200 bg-slate-50 p-4 sm:grid-cols-[40px_minmax(0,1fr)]" key={step.id}>
                      <strong>{String(step.stepNumber).padStart(2, '0')}</strong>
                      <div>
                        <h3>{step.title || `Bước ${step.stepNumber}`}</h3>
                        <p>{step.description}</p>
                        {step.timerMinutes != null && step.timerMinutes > 0 && (
                          <span className="step-timer inline-flex rounded-full bg-amber-50 px-2.5 py-1 text-xs font-semibold text-amber-800" aria-label={`Thời gian bước ${step.stepNumber}`}>
                            ◷ {step.timerMinutes} phút
                          </span>
                        )}
                        {step.imageUrl && (
                          // eslint-disable-next-line @next/next/no-img-element
                          <img
                            src={step.imageUrl}
                            alt={step.title || `Ảnh bước ${step.stepNumber}`}
                            className="step-image mt-3 max-h-72 w-full rounded-xl border border-slate-200 object-cover"
                            loading="lazy"
                          />
                        )}
                      </div>
                    </li>
                  ))}
              </ol>
            )}
          </section>
        </div>

        <aside className="nutrition-panel h-fit rounded-2xl bg-emerald-50 p-5">
          <span className="panel-label">Thông tin dinh dưỡng</span>
          <h2>Mỗi khẩu phần</h2>
          {recipe.nutrition ? (
            <dl className="nutrition-list">
              {nutritionLabels.map(([key, label, unit]) => {
                const val = recipe.nutrition?.[key as keyof typeof recipe.nutrition];
                return (
                  <div key={key}>
                    <dt>{label}</dt>
                    <dd>
                      {val ?? '—'}
                      {val == null ? '' : unit}
                    </dd>
                  </div>
                );
              })}
            </dl>
          ) : (
            <p>Chưa có thông tin dinh dưỡng.</p>
          )}
        </aside>
      </div>

      <Link className="outline-button detail-back" href="/search">
        ← Danh sách công thức
      </Link>
    </article>
  );
}

export default RecipeDetailView;
