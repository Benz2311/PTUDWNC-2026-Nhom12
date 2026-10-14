'use client';

import { useEffect, useMemo, useState } from 'react';
import { getRecipes } from '@/lib/api/recipes';
import type { RecipeSummary } from '@/types/recipe';
import RecipeList from '@/components/recipes/RecipeList';

const tabs = ['Tất cả', 'Món Việt', 'Món Âu', 'Món Chay', 'Tráng Miệng', 'Đồ uống'];

export default function FeaturedRecipes() {
  const [recipes, setRecipes] = useState<RecipeSummary[]>([]);
  const [activeTab, setActiveTab] = useState('Tất cả');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(() => {
    let cancelled = false;
    getRecipes({ page: 1, pageSize: 50, sort: 'newest' }, false)
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
        <RecipeList recipes={visibleRecipes} className="recipe-grid" />
      )}
    </>
  );
}
