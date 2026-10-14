'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';
import { deleteRecipe as deleteRecipeRequest, getRecipeBySlug, getRecipes, type RecipeSort } from '@/lib/api/recipes';
import type { RecipePage } from '@/types/recipe';
import RecipeListView from '@/components/recipes/RecipeList';

type Category = { id: string; name: string; slug: string; recipeCount: number };

export default function RecipeList({ admin = false }: { admin?: boolean }) {
  const [recipes, setRecipes] = useState<RecipePage | null>(null);
  const [categories, setCategories] = useState<Category[]>([]);
  const [search, setSearch] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [sort, setSort] = useState<RecipeSort>('newest');
  const [page, setPage] = useState(1);
  const [error, setError] = useState('');

  async function deleteRecipe(id: string, slug: string, title: string) {
    if (!window.confirm(`Bạn chắc chắn muốn xóa công thức "${title}"?`)) {
      return;
    }

    try {
      const recipe = await getRecipeBySlug(slug);
      const etag = recipe.headers.get('ETag');
      if (!etag) throw new Error('Không nhận được ETag của công thức. Hãy tải lại trang.');
      await deleteRecipeRequest(id, etag);
      setRecipes((current) =>
        current
          ? {
              ...current,
              items: current.items.filter((recipe) => recipe.id !== id),
              totalCount: Math.max(0, current.totalCount - 1),
            }
          : current,
      );
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Không thể xóa công thức.');
    }
  }

  useEffect(() => {
    apiFetch<Category[]>('/api/v1/categories')
      .then(setCategories)
      .catch((reason: Error) => setError(reason.message));
  }, []);

  useEffect(() => {
    getRecipes({
      page,
      pageSize: 6,
      sort,
      search,
      categoryId: categoryId || undefined,
    })
      .then(setRecipes)
      .catch((reason: Error) => setError(reason.message));
  }, [search, categoryId, sort, page, admin]);

  const totalPages = recipes ? Math.max(1, Math.ceil(recipes.totalCount / recipes.pageSize)) : 1;

  return (
    <section className="recipe-workspace">
      <div className="recipe-toolbar">
        <label className="field field-search">
          <span>Tìm công thức</span>
          <input
            value={search}
            onChange={(event) => {
              setSearch(event.target.value);
              setPage(1);
            }}
            placeholder="Phở, mì, món chay..."
          />
        </label>
        <label className="field">
          <span>Danh mục</span>
          <select
            value={categoryId}
            onChange={(event) => {
              setCategoryId(event.target.value);
              setPage(1);
            }}
          >
            <option value="">Tất cả danh mục</option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>{category.name}</option>
            ))}
          </select>
        </label>
        <label className="field">
          <span>Sắp xếp</span>
          <select
            value={sort}
            onChange={(event) => {
              const value = event.target.value;
              if (value === 'newest' || value === 'oldest' || value === 'title' || value === 'cooktime') {
                setSort(value);
              }
              setPage(1);
            }}
          >
            <option value="newest">Mới nhất</option>
            <option value="oldest">Cũ nhất</option>
            <option value="title">Tên món</option>
            <option value="cooktime">Thời gian nấu</option>
          </select>
        </label>
      </div>

      {error && <p className="form-error">{error}</p>}
      <div className="recipe-list-heading">
        <div>
          <strong>{recipes?.totalCount ?? 0}</strong> công thức phù hợp
        </div>
        <a className="primary-button" href="/recipes/new">+ Tạo công thức</a>
      </div>

      {recipes && (
        <RecipeListView
          recipes={recipes.items}
          emptyMessage="Chưa có công thức phù hợp."
          showStatus
          renderActions={(recipe) => (
            <>
              <a className="card-action" href={`/recipes/${encodeURIComponent(recipe.slug)}`}>Xem chi tiết →</a>
              <a className="card-action" href={`/recipes/${encodeURIComponent(recipe.slug)}/edit`}>Chỉnh sửa</a>
              <button className="delete-action" type="button" onClick={() => deleteRecipe(recipe.id, recipe.slug, recipe.title)}>Xóa</button>
            </>
          )}
        />
      )}

      <div className="pagination">
        <button type="button" onClick={() => setPage((current) => Math.max(1, current - 1))} disabled={page === 1}>← Trước</button>
        <span>Trang {page} / {totalPages}</span>
        <button type="button" onClick={() => setPage((current) => Math.min(totalPages, current + 1))} disabled={page >= totalPages}>Sau →</button>
      </div>
    </section>
  );
}
