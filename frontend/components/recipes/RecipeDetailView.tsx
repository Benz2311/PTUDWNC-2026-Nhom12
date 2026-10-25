'use client';

import { useState } from 'react';
import type { RecipeDetail } from '@/types/recipe';
import DifficultyBadge from './DifficultyBadge';
import RecipeStatusBadge from './RecipeStatusBadge';
import RecipeTimeInfo from './RecipeTimeInfo';
import RecipeSteps from '@/components/recipe-management/RecipeSteps';

const nutritionLabels = [
  ['calories', 'Calories', 'kcal'],
  ['protein', 'Protein', 'g'],
  ['carbohydrates', 'Carbohydrates', 'g'],
  ['fat', 'Chất béo', 'g'],
  ['fiber', 'Chất xơ', 'g'],
  ['sodium', 'Natri', 'mg'],
] as const;

function formatPublishedDate(value: string | null): string | null {
  if (!value) return null;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return null;
  return new Intl.DateTimeFormat('vi-VN', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  }).format(date);
}

export default function RecipeDetailView({ recipe }: { recipe: RecipeDetail }) {
  const images = [...recipe.images].sort((left, right) =>
    Number(right.isPrimary) - Number(left.isPrimary) || (left.sortOrder ?? 0) - (right.sortOrder ?? 0));
  const [activeImage, setActiveImage] = useState(0);
  const selectedImage = images[activeImage] ?? images[0];

  const sortedIngredients = [...recipe.ingredients].sort(
    (left, right) => (left.orderIndex ?? left.sortOrder ?? 0) - (right.orderIndex ?? right.sortOrder ?? 0),
  );

  const publishedDate = formatPublishedDate(recipe.publishedAt);
  const hasNutrition = recipe.nutrition != null && nutritionLabels.some(([key]) => recipe.nutrition?.[key] != null);

  return (
    <article className="recipe-detail recipe-detail-view" aria-label={`Chi tiết công thức: ${recipe.title}`}>
      <nav className="detail-breadcrumb" aria-label="Điều hướng liên kết">
        <a href="/">Trang chủ</a>
        <span aria-hidden="true" className="breadcrumb-separator">/</span>
        <a href="/recipes">Công thức</a>
        <span aria-hidden="true" className="breadcrumb-separator">/</span>
        <a href={`/recipes?categoryId=${encodeURIComponent(recipe.categoryId)}`}>{recipe.category}</a>
        <span aria-hidden="true" className="breadcrumb-separator">/</span>
        <span aria-current="page" className="breadcrumb-current">{recipe.title}</span>
      </nav>

      <header className="recipe-detail-header">
        <div className="recipe-detail-badges">
          <RecipeStatusBadge status={recipe.status} />
          <DifficultyBadge difficulty={recipe.difficulty} />
        </div>
        <h1 className="flow-title">{recipe.title}</h1>
        {recipe.description ? <p className="detail-lead">{recipe.description}</p> : null}

        <div className="recipe-card-meta recipe-detail-meta">
          <a
            href={`/recipes?categoryId=${encodeURIComponent(recipe.categoryId)}`}
            className="detail-meta-category"
            title={`Xem các công thức thuộc danh mục ${recipe.category}`}
          >
            🏷️ {recipe.category}
          </a>
          <span className="detail-meta-author">
            👨‍🍳 Tác giả: <strong>{recipe.author}</strong>
          </span>
          {publishedDate ? (
            <time dateTime={recipe.publishedAt ?? undefined} className="detail-meta-date">
              📅 Xuất bản ngày {publishedDate}
            </time>
          ) : (
            <span className="detail-meta-draft">📝 Bản nháp chưa xuất bản</span>
          )}
        </div>

        <RecipeTimeInfo
          prepTimeMinutes={recipe.prepTimeMinutes}
          cookTimeMinutes={recipe.cookTimeMinutes}
          servings={recipe.servings}
        />
      </header>

      <section className="recipe-detail-gallery" aria-label="Thư viện ảnh công thức">
        {images.length > 0 ? (
          <>
            <div
              className="recipe-detail-cover"
              role="img"
              aria-label={selectedImage?.altText || recipe.title}
              style={{ backgroundImage: `url("${selectedImage?.url}")` }}
            >
              {selectedImage?.isPrimary ? (
                <span className="primary-image-badge" aria-label="Ảnh đại diện chính của công thức">
                  ★ Ảnh chính
                </span>
              ) : null}
              {selectedImage?.altText ? (
                <span className="cover-caption">{selectedImage.altText}</span>
              ) : null}
            </div>
            {images.length > 1 ? (
              <div className="recipe-detail-thumbnails" aria-label="Bộ sưu tập ảnh phụ theo thứ tự">
                {images.map((image, index) => (
                  <button
                    type="button"
                    key={image.id || index}
                    className={`recipe-detail-thumbnail${index === activeImage ? ' is-active' : ''}`}
                    onClick={() => setActiveImage(index)}
                    aria-label={`Xem ảnh ${index + 1}${image.isPrimary ? ' (Ảnh chính)' : ''}`}
                    aria-pressed={index === activeImage}
                    style={{ backgroundImage: `url("${image.url}")` }}
                  >
                    {image.isPrimary ? <span className="thumbnail-primary-tag">Chính</span> : null}
                  </button>
                ))}
              </div>
            ) : null}
          </>
        ) : (
          <div className="recipe-detail-cover recipe-detail-cover-empty" role="img" aria-label="Chưa có ảnh món ăn">
            <div className="empty-cover-content">
              <span className="empty-cover-icon" aria-hidden="true">🍲</span>
              <strong>Công thức chưa có hình ảnh đại diện</strong>
              <p>Ảnh món ăn sẽ hiển thị tại đây khi tác giả cập nhật.</p>
            </div>
          </div>
        )}
      </section>

      <div className="detail-columns recipe-detail-columns">
        <div className="detail-primary-column">
          <section className="detail-section">
            <h2>Giới thiệu món ăn</h2>
            <p className="detail-content">{recipe.content}</p>
          </section>

          <section className="detail-section" aria-label="Danh sách nguyên liệu">
            <div className="section-heading">
              <div>
                <h2>Nguyên liệu</h2>
                <p>Khẩu phần {recipe.servings} người · Chuẩn bị theo thứ tự</p>
              </div>
            </div>
            {sortedIngredients.length === 0 ? (
              <div className="empty-state">
                <strong>Chưa có nguyên liệu nào được cung cấp.</strong>
              </div>
            ) : (
              <ul className="ingredient-list">
                {sortedIngredients.map((ingredient) => (
                  <li className="ingredient-item" key={ingredient.id}>
                    <span className="ingredient-bullet" aria-hidden="true">🥬</span>
                    <div className="ingredient-info">
                      <span className="ingredient-name">{ingredient.name}</span>
                      {ingredient.notes ? (
                        <small className="ingredient-notes">Ghi chú: {ingredient.notes}</small>
                      ) : null}
                    </div>
                    <strong className="ingredient-quantity">
                      {ingredient.quantity != null ? ingredient.quantity : '—'} {ingredient.unit ?? ''}
                    </strong>
                  </li>
                ))}
              </ul>
            )}
          </section>

          <RecipeSteps slug={recipe.slug} steps={recipe.steps} />
        </div>

        <aside className="nutrition-panel" aria-label="Thông tin dinh dưỡng">
          <span className="panel-label">Thông tin dinh dưỡng</span>
          <h2>Mỗi khẩu phần</h2>
          <p className="nutrition-sub">Giá trị tham khảo tính trên 1 phần ăn</p>
          {hasNutrition ? (
            <dl className="nutrition-list">
              {nutritionLabels.map(([key, label, unit]) => (
                <div key={key} className="nutrition-item">
                  <dt>{label}</dt>
                  <dd>
                    {recipe.nutrition?.[key] != null ? recipe.nutrition[key] : '—'}
                    {recipe.nutrition?.[key] != null ? <span className="nutrition-unit">{unit}</span> : null}
                  </dd>
                </div>
              ))}
            </dl>
          ) : (
            <div className="empty-state empty-inline">
              <p>Chưa có dữ liệu dinh dưỡng cho công thức này.</p>
            </div>
          )}
        </aside>
      </div>

      <footer className="recipe-detail-footer">
        <a className="outline-button detail-back" href="/recipes">
          ← Quay lại danh sách công thức
        </a>
        <a
          className="outline-button detail-category-link"
          href={`/recipes?categoryId=${encodeURIComponent(recipe.categoryId)}`}
        >
          Xem thêm công thức {recipe.category} →
        </a>
      </footer>
    </article>
  );
}
