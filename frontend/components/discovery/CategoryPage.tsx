'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';
import { getRecipes } from '@/lib/api/recipes';
import type { RecipeSummary } from '@/types/recipe';
import RecipeList from '@/components/recipes/RecipeList';

type Category = { id: string; name: string; slug: string; description: string | null; recipeCount?: number };

export default function CategoryPage({ slug }: { slug: string }) {
  const [category, setCategory] = useState<Category | null>(null);
  const [recipes, setRecipes] = useState<RecipeSummary[]>([]);
  const [error, setError] = useState('');

  useEffect(() => {
    let cancelled = false;
    apiFetch<Record<string, unknown>>(`/api/v1/categories/${encodeURIComponent(slug)}`, {}, false)
      .then(async (raw) => {
        const catObj = ((raw.category ?? raw.Category ?? raw) as Record<string, unknown>) || {};
        const normalizedCategory: Category = {
          id: String(catObj.id ?? catObj.Id ?? ''),
          name: String(catObj.name ?? catObj.Name ?? ''),
          slug: String(catObj.slug ?? catObj.Slug ?? slug),
          description: catObj.description ? String(catObj.description) : (catObj.Description ? String(catObj.Description) : null),
        };

        if (cancelled) return;
        setCategory(normalizedCategory);

        if (normalizedCategory.id) {
          try {
            const page = await getRecipes({ categoryId: normalizedCategory.id, pageSize: 50 }, false);
            if (!cancelled) {
              setRecipes(page.items.filter((recipe) => recipe.status === 'Published'));
            }
          } catch {
            const rawRecipes = (raw.recipes ?? raw.Recipes) as Record<string, unknown> | undefined;
            const items = (rawRecipes?.items ?? rawRecipes?.Items ?? []) as RecipeSummary[];
            if (!cancelled) {
              setRecipes(items);
            }
          }
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
        <RecipeList recipes={recipes} emptyMessage="Chưa có công thức nào trong danh mục này." />
      </section>
    </>
  );
}
