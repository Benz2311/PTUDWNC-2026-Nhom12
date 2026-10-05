'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';

type Recipe = {
  id: string;
  title: string;
  slug: string;
  description: string;
  content: string;
  category: string;
  author: string;
  cookTimeMinutes: number;
  difficulty: string;
  status: string;
  ingredients: { id: string; name: string; quantity: string | number | null; unit: string | null; sortOrder: number }[];
  nutrition: { calories: number; protein: number; carbohydrates: number; fat: number; fiber: number } | null;
  images: { id: string; url: string; altText: string | null; sortOrder: number }[];
};

export default function RecipeDetail({ slug }: { slug: string }) {
  const [recipe, setRecipe] = useState<Recipe | null>(null);
  const [error, setError] = useState('');
  const [slide, setSlide] = useState(0);

  useEffect(() => {
    apiFetch<Recipe>(`/api/v1/recipes/${slug}`)
      .then(setRecipe)
      .catch((reason: Error) => setError(reason.message || 'Không tìm thấy công thức.'));
  }, [slug]);

  useEffect(() => {
    if (!recipe || !recipe.images || recipe.images.length < 2) return;
    const timer = window.setInterval(() => setSlide((current) => (current + 1) % recipe.images.length), 4500);
    return () => window.clearInterval(timer);
  }, [recipe]);

  if (error) {
    return (
      <div className="empty-state">
        <strong>{error}</strong>
        <a className="card-action" href="/recipes">
          Quay lại danh sách
        </a>
      </div>
    );
  }

  if (!recipe) {
    return <div className="loading-state">Đang tải công thức...</div>;
  }

  return (
    <article className="recipe-detail">
      <div className="detail-topline">
        <span className="status status-published">{recipe.status}</span>
        <span>{recipe.category}</span>
        <span>{recipe.author}</span>
      </div>
      <h1 className="flow-title">{recipe.title}</h1>
      <p className="detail-lead">{recipe.description}</p>
      <div className="detail-meta">
        <span>◷ {recipe.cookTimeMinutes} phút</span>
        <span>Độ khó: {recipe.difficulty}</span>
      </div>

      {recipe.images && recipe.images.length > 0 && (
        <section className="recipe-slideshow" aria-label="Trình chiếu ảnh món ăn">
          <img src={recipe.images[slide]?.url} alt={recipe.images[slide]?.altText ?? recipe.title} />
          {recipe.images.length > 1 && (
            <>
              <button
                type="button"
                className="slide-control slide-prev"
                onClick={() => setSlide((current) => (current - 1 + recipe.images.length) % recipe.images.length)}
                aria-label="Ảnh trước"
              >
                ‹
              </button>
              <button
                type="button"
                className="slide-control slide-next"
                onClick={() => setSlide((current) => (current + 1) % recipe.images.length)}
                aria-label="Ảnh tiếp theo"
              >
                ›
              </button>
              <div className="slide-dots">
                {recipe.images.map((image, index) => (
                  <button
                    type="button"
                    key={image.id || index}
                    className={index === slide ? 'active' : ''}
                    onClick={() => setSlide(index)}
                    aria-label={`Chọn ảnh ${index + 1}`}
                  />
                ))}
              </div>
            </>
          )}
        </section>
      )}

      <div className="detail-columns">
        <div>
          <section className="detail-section">
            <h2>Cách thực hiện</h2>
            <p className="detail-content">{recipe.content}</p>
          </section>
          <section className="detail-section">
            <h2>Nguyên liệu</h2>
            <div className="ingredient-list">
              {recipe.ingredients?.map((ingredient) => (
                <div className="ingredient-item" key={ingredient.id}>
                  <span>{ingredient.name}</span>
                  <strong>
                    {ingredient.quantity ?? ''} {ingredient.unit ?? ''}
                  </strong>
                </div>
              ))}
            </div>
          </section>
        </div>

        <aside className="nutrition-panel">
          <span className="panel-label">Dinh dưỡng</span>
          <h2>Mỗi phần ăn</h2>
          {recipe.nutrition && (
            <div className="nutrition-list">
              <div>
                <strong>{recipe.nutrition.calories ?? 0}</strong>
                <span>Calories</span>
              </div>
              <div>
                <strong>{recipe.nutrition.protein ?? 0}g</strong>
                <span>Protein</span>
              </div>
              <div>
                <strong>{recipe.nutrition.carbohydrates ?? 0}g</strong>
                <span>Carbs</span>
              </div>
              <div>
                <strong>{recipe.nutrition.fat ?? 0}g</strong>
                <span>Fat</span>
              </div>
              <div>
                <strong>{recipe.nutrition.fiber ?? 0}g</strong>
                <span>Fiber</span>
              </div>
            </div>
          )}
        </aside>
      </div>

      <a className="outline-button detail-back" href="/recipes">
        ← Danh sách công thức
      </a>
    </article>
  );
}
