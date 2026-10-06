'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';

type Category = { id: string; name: string; slug: string; description: string | null; recipeCount: number };
type Recipe = { id: string; title: string; slug: string; description: string | null; category: string; author: string; prepTimeMinutes: number; cookTimeMinutes: number; servings: number; difficulty: string; status: string; createdAt: string; primaryImageUrl: string | null };
type PageResponse<T> = { items: T[]; page: number; pageSize: number; totalCount: number };

export default function CategoryPage({ slug }: { slug: string }) {
  const [category, setCategory] = useState<Category | null>(null);
  const [recipes, setRecipes] = useState<Recipe[]>([]);
  const [error, setError] = useState('');

  useEffect(() => {
    let cancelled = false;
    apiFetch<Category>(`/api/v1/categories/${encodeURIComponent(slug)}`, {}, false)
      .then(async (result) => {
        const page = await apiFetch<PageResponse<Recipe>>(`/api/v1/recipes?categoryId=${encodeURIComponent(result.id)}&pageSize=50`, {}, false);
        if (!cancelled) {
          setCategory(result);
          setRecipes(page.items.filter((recipe) => recipe.status === 'Published'));
        }
      })
      .catch((reason: Error) => {
        if (!cancelled) setError(reason.message || 'Không tải được danh mục.');
      });

    return () => { cancelled = true; };
  }, [slug]);

  if (error) return <p className="form-error" role="alert">{error}</p>;
  if (!category) return <div className="loading-state">Đang tải danh mục...</div>;

  return (
    <>
      <div className="flow-kicker">Danh mục công thức</div>
      <h1 className="flow-title">{category.name}</h1>
      {category.description && <p className="flow-description">{category.description}</p>}
      <section className="flow-panel">
        <div className="section-heading"><div><h2>{recipes.length} công thức đã xuất bản</h2><p>Khám phá món ngon trong danh mục {category.name}.</p></div></div>
        {recipes.length === 0 ? <p>Chưa có công thức nào trong danh mục này.</p> : (
          <div className="recipe-card-grid">
            {recipes.map((recipe) => (
              <a className="recipe-card" key={recipe.id} href={`/recipes/${encodeURIComponent(recipe.slug)}`}>
                <div className="recipe-image" role={recipe.primaryImageUrl ? 'img' : undefined} aria-label={recipe.primaryImageUrl ? `Ảnh món ${recipe.title}` : undefined} style={recipe.primaryImageUrl ? { backgroundImage: `url("${recipe.primaryImageUrl}")` } : undefined}><span className="recipe-tag">{category.name}</span></div>
                <div className="recipe-body"><h3>{recipe.title}</h3><p>{recipe.description || 'Món ăn hấp dẫn được cộng đồng chia sẻ.'}</p><div className="recipe-meta"><span>◷ {recipe.prepTimeMinutes + recipe.cookTimeMinutes} phút</span><span>{recipe.difficulty}</span></div><span className="recipe-card-link">Xem công thức →</span></div>
              </a>
            ))}
          </div>
        )}
      </section>
    </>
  );
}
