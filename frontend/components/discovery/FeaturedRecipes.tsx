'use client';

import { useEffect, useMemo, useState } from 'react';
import { apiFetch } from '@/lib/api';

type Recipe = {
  id: string;
  title: string;
  slug: string;
  description: string | null;
  category: string;
  cookTimeMinutes: number;
  primaryImageUrl: string | null;
};

type RecipePage = { items: Recipe[] };

const tabs = ['Tất cả', 'Món Việt', 'Món Âu', 'Món Chay', 'Tráng Miệng', 'Đồ uống'];
const fallbackImages = [
  '/sample-images/food-01.svg',
  '/sample-images/food-02.svg',
  '/sample-images/food-03.svg',
];

export default function FeaturedRecipes() {
  const [recipes, setRecipes] = useState<Recipe[]>([]);
  const [activeTab, setActiveTab] = useState('Tất cả');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(() => {
    let cancelled = false;
    apiFetch<RecipePage>('/api/v1/recipes?page=1&pageSize=50&sort=newest', {}, false)
      .then((page) => {
        if (!cancelled) setRecipes(page.items);
      })
      .catch(() => {
        if (!cancelled) setError(true);
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => { cancelled = true; };
  }, []);

  const visibleRecipes = useMemo(() => recipes
    .filter((recipe) => activeTab === 'Tất cả' || recipe.category === activeTab)
    .slice(0, 3), [activeTab, recipes]);

  return (
    <>
      <div className="featured-tabs" role="group" aria-label="Lọc công thức nổi bật">
        {tabs.map((tab) => (
          <button
            className={`featured-tab${activeTab === tab ? ' is-active' : ''}`}
            key={tab}
            type="button"
            aria-pressed={activeTab === tab}
            onClick={() => setActiveTab(tab)}
          >
            {tab}
          </button>
        ))}
      </div>

      {isLoading ? (
        <div className="featured-message" role="status">Đang tải công thức nổi bật...</div>
      ) : error ? (
        <div className="featured-message" role="status">Chưa thể tải công thức. Vui lòng thử lại sau.</div>
      ) : visibleRecipes.length === 0 ? (
        <div className="featured-message" role="status">Chưa có công thức trong chủ đề này.</div>
      ) : (
        <div className="recipe-grid">
          {visibleRecipes.map((recipe, index) => {
            const imageUrl = recipe.primaryImageUrl || fallbackImages[index % fallbackImages.length];
            return (
              <a className="recipe-card" href={`/recipes/${encodeURIComponent(recipe.slug)}`} key={recipe.id}>
                <div
                  className="recipe-image"
                  role="img"
                  aria-label={`Ảnh món ${recipe.title}`}
                  style={{ backgroundImage: `url("${imageUrl}")` }}
                >
                  <span className="recipe-tag">{recipe.category}</span>
                  <span className="recipe-image-icon" aria-hidden="true">↗</span>
                </div>
                <div className="recipe-body">
                  <h3>{recipe.title}</h3>
                  <p>{recipe.description || 'Khám phá hương vị hấp dẫn trong công thức này.'}</p>
                  <div className="recipe-meta">
                    <span>◷ {recipe.cookTimeMinutes} phút</span>
                    <span className="recipe-card-link">Xem công thức <span aria-hidden="true">→</span></span>
                  </div>
                </div>
              </a>
            );
          })}
        </div>
      )}
    </>
  );
}
