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
        <h2>{recipes.length} công thức đã xuất bản</h2>
        {recipes.length === 0 ? <p>Chưa có công thức nào trong danh mục này.</p> : (
          <ul className="list-plain">
            {recipes.map((recipe) => (
              <li key={recipe.id}>
                <a className="card-action" href={`/recipes/${recipe.slug}`}>{recipe.title}</a>
                {recipe.description && <p>{recipe.description}</p>}
              </li>
            ))}
          </ul>
        )}
      </section>
    </>
  );
}
