'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';

type Recipe = {
  id: string;
  title: string;
  slug: string;
  description: string | null;
  category: string;
  author: string;
  cookTimeMinutes: number;
  difficulty: string;
  status: string;
  primaryImageUrl: string | null;
};

type Category = { id: string; name: string; slug: string; recipeCount: number };
type PageResponse = { items: Recipe[]; page: number; pageSize: number; totalCount: number };

export default function RecipeList() {
  const [recipes, setRecipes] = useState<PageResponse | null>(null);
  const [categories, setCategories] = useState<Category[]>([]);
  const [search, setSearch] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [sort, setSort] = useState('newest');
  const [page, setPage] = useState(1);
  const [error, setError] = useState('');

  async function deleteRecipe(id: string, title: string) {
    if (!window.confirm(`Công thức "${title}" sẽ bị xóa vĩnh viễn cùng nguyên liệu và dinh dưỡng. Bạn có chắc không?`)) {
      return;
    }

    try {
      await apiFetch(`/api/v1/recipes/${id}`, { method: 'DELETE' });
      setRecipes((current) =>
        current
          ? {
              ...current,
              items: current.items.filter((recipe) => recipe.id !== id),
              totalCount: current.totalCount - 1,
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
    const params = new URLSearchParams({ page: String(page), pageSize: '6', sort });
    if (search.trim()) params.set('search', search.trim());
    if (categoryId) params.set('categoryId', categoryId);

    apiFetch<PageResponse>(`/api/v1/recipes?${params}`)
      .then(setRecipes)
      .catch((reason: Error) => setError(reason.message));
  }, [search, categoryId, sort, page]);

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
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span>Sắp xếp</span>
          <select
            value={sort}
            onChange={(event) => {
              setSort(event.target.value);
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
        <a className="primary-button" href="/recipes/new">
          + Tạo công thức
        </a>
      </div>
      <div className="recipe-admin-grid">
        {recipes?.items.map((recipe) => (
          <article className="recipe-admin-card" key={recipe.id}>
            {recipe.primaryImageUrl && (
              <div
                className="recipe-admin-image"
                style={{ backgroundImage: `url(${recipe.primaryImageUrl})` }}
                aria-label={`Ảnh ${recipe.title}`}
              />
            )}
            <div className="recipe-admin-card-top">
              <span className={`status status-${recipe.status.toLowerCase()}`}>{recipe.status}</span>
              <span>{recipe.category}</span>
            </div>
            <h2>{recipe.title}</h2>
            <p>{recipe.description}</p>
            <div className="recipe-admin-meta">
              <span>{recipe.cookTimeMinutes} phút</span>
              <span>{recipe.difficulty}</span>
              <span>{recipe.author}</span>
            </div>
            <div className="card-actions">
              <a className="card-action" href={`/recipes/${recipe.slug}`}>
                Xem chi tiết <span aria-hidden="true">→</span>
              </a>
              <button className="delete-action" type="button" onClick={() => deleteRecipe(recipe.id, recipe.title)}>
                Xóa
              </button>
            </div>
          </article>
        ))}
      </div>
      {recipes && recipes.items.length === 0 && (
        <div className="empty-state">
          <strong>Chưa có công thức phù hợp</strong>
          <span>Thử đổi từ khóa hoặc bộ lọc.</span>
        </div>
      )}
      <div className="pagination">
        <button type="button" onClick={() => setPage((current) => Math.max(1, current - 1))} disabled={page === 1}>
          ← Trước
        </button>
        <span>
          Trang {page} / {totalPages}
        </span>
        <button
          type="button"
          onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
          disabled={page >= totalPages}
        >
          Sau →
        </button>
      </div>
    </section>
  );
}
