'use client';

import { useState } from 'react';
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
  const images = [...(recipe.images || [])].sort((left, right) =>
    Number(Boolean(right.isPrimary)) - Number(Boolean(left.isPrimary)) ||
    ((left.sortOrder ?? left.orderIndex ?? 0) - (right.sortOrder ?? right.orderIndex ?? 0)));
  const [activeImage, setActiveImage] = useState(0);
  const selectedImage = images[activeImage];

  return (
    <article className="recipe-detail recipe-detail-view">
      <header className="recipe-detail-header">
        <div className="recipe-detail-badges">
          <RecipeStatusBadge status={recipe.status} />
          <DifficultyBadge difficulty={recipe.difficulty} />
        </div>
        <h1 className="flow-title">{recipe.title}</h1>
        {recipe.description && <p className="detail-lead">{recipe.description}</p>}
        <RecipeMeta category={recipe.category} author={recipe.author} publishedAt={recipe.publishedAt} />
        <RecipeTimeInfo
          prepTimeMinutes={recipe.prepTimeMinutes ?? recipe.prepTime}
          cookTimeMinutes={recipe.cookTimeMinutes ?? recipe.cookTime}
          servings={recipe.servings}
        />
      </header>

      <section className="recipe-detail-gallery" aria-label="Thư viện ảnh công thức">
        <div
          className="recipe-detail-cover"
          role="img"
          aria-label={selectedImage?.altText || recipe.title}
          style={{ backgroundImage: `url("${selectedImage?.url ?? selectedImage?.originalUrl ?? '/sample-images/food-01.svg'}")` }}
        />
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

      <div className="detail-columns recipe-detail-columns">
        <div>
          <section className="detail-section">
            <h2>Giới thiệu</h2>
            <p className="detail-content">{recipe.content}</p>
          </section>

          <section className="detail-section">
            <h2>Nguyên liệu</h2>
            {(!recipe.ingredients || recipe.ingredients.length === 0) ? (
              <p className="detail-content">Chưa có nguyên liệu được cung cấp.</p>
            ) : (
              <ul className="ingredient-list">
                {[...recipe.ingredients]
                  .sort((left, right) => (left.sortOrder ?? left.orderIndex ?? 0) - (right.sortOrder ?? right.orderIndex ?? 0))
                  .map((ingredient) => (
                    <li className="ingredient-item" key={ingredient.id}>
                      <span>{ingredient.name}{ingredient.notes && <small>{ingredient.notes}</small>}</span>
                      <strong>{ingredient.quantity ?? '—'} {ingredient.unit ?? ''}</strong>
                    </li>
                  ))}
              </ul>
            )}
          </section>

          <section className="detail-section">
            <h2>Các bước thực hiện</h2>
            {(!recipe.steps || recipe.steps.length === 0) ? (
              <p className="detail-content">Chưa có hướng dẫn chế biến.</p>
            ) : (
              <ol className="step-list">
                {[...recipe.steps]
                  .sort((left, right) => left.stepNumber - right.stepNumber)
                  .map((step) => (
                    <li className="step-item" key={step.id}>
                      <strong>{String(step.stepNumber).padStart(2, '0')}</strong>
                      <div>
                        <h3>{step.title || `Bước ${step.stepNumber}`}</h3>
                        <p>{step.description}</p>
                      </div>
                    </li>
                  ))}
              </ol>
            )}
          </section>
        </div>

        <aside className="nutrition-panel">
          <span className="panel-label">Thông tin dinh dưỡng</span>
          <h2>Mỗi khẩu phần</h2>
          {recipe.nutrition ? (
            <dl className="nutrition-list">
              {nutritionLabels.map(([key, label, unit]) => (
                <div key={key}>
                  <dt>{label}</dt>
                  <dd>{(recipe.nutrition as any)?.[key] ?? '—'}{(recipe.nutrition as any)?.[key] == null ? '' : unit}</dd>
                </div>
              ))}
            </dl>
          ) : <p>Chưa có thông tin dinh dưỡng.</p>}
        </aside>
      </div>

      <a className="outline-button detail-back" href="/recipes">← Danh sách công thức</a>
    </article>
  );
}

export default RecipeDetailView;
