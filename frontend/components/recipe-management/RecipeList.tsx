'use client';

import { useEffect, useState } from 'react';
import { apiFetch, apiFetchWithResponse } from '@/lib/api';

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

export default function RecipeList({ admin = false }: { admin?: boolean }) {
  const [recipes, setRecipes] = useState<PageResponse | null>(null);
  const [categories, setCategories] = useState<Category[]>([]);
  const [search, setSearch] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [sort, setSort] = useState('newest');
  const [difficulty, setDifficulty] = useState('');
  const [maximumMinutes, setMaximumMinutes] = useState('');
  const [status, setStatus] = useState(admin ? 'Published' : '');
  const [page, setPage] = useState(1);
  const [error, setError] = useState('');

  async function deleteRecipe(id: string, title: string) {
    if (!window.confirm(`Chuyển công thức "${title}" vào thùng rác? Quản trị viên có thể khôi phục trong thời hạn lưu trữ.`)) {
      return;
    }

    try {
      const recipe = recipes?.items.find((item) => item.id === id);
      if (!recipe) return;
      const { headers } = await apiFetchWithResponse(`/api/v1/recipes/${recipe.slug}`);
      const etag = headers.get('ETag');
      if (!etag) {
        throw new Error('Không nhận được phiên bản công thức. Hãy tải lại trang rồi thử lại.');
      }
      await apiFetch(`/api/v1/recipes/${id}`, { method: 'DELETE', headers: { 'If-Match': etag } });
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
    const params = new URLSearchParams({ page: '1', pageSize: '50', sort });
    if (search.trim()) params.set('search', search.trim());
    if (categoryId) params.set('categoryId', categoryId);
    if (admin && status) params.set('status', status);

    apiFetch<PageResponse>(`/api/v1/recipes?${params}`)
      .then(setRecipes)
      .catch((reason: Error) => setError(reason.message));
  }, [search, categoryId, sort, page, admin, status]);

  const filteredItems = (recipes?.items ?? []).filter((recipe) =>
    (!difficulty || recipe.difficulty === difficulty) &&
    (!maximumMinutes || recipe.cookTimeMinutes <= Number(maximumMinutes)));
  const pageSize = 6;
  const totalPages = Math.max(1, Math.ceil(filteredItems.length / pageSize));
  const visibleItems = filteredItems.slice((page - 1) * pageSize, page * pageSize);

  return (
    <section className="recipe-workspace recipe-workspace-layout">
      <aside className="recipe-filter-panel">
        <h2>{admin ? 'Quản lý nội dung' : 'Lọc công thức'}</h2>
        {admin && (
          <label className="field">
            <span>Trạng thái</span>
            <select value={status} onChange={(event) => { setStatus(event.target.value); setPage(1); }}>
              <option value="">Tất cả trạng thái</option><option value="Published">Đã xuất bản</option><option value="Draft">Bản nháp</option><option value="Archived">Đã lưu trữ</option>
            </select>
          </label>
        )}
        <label className="field field-search">
          <span>Từ khóa</span>
          <input
            value={search}
            onChange={(event) => {
              setSearch(event.target.value);
              setPage(1);
            }}
            placeholder="Phở, mì, món chay..."
          />
        </label>
        <fieldset>
            <legend>Danh mục</legend>
            <label><input type="radio" name="category-filter" checked={!categoryId} onChange={() => { setCategoryId(''); setPage(1); }} /> Tất cả</label>
            {categories.map((category) => (
              <label key={category.id}><input type="radio" name="category-filter" checked={categoryId === category.id} onChange={() => { setCategoryId(category.id); setPage(1); }} /> {category.name}</label>
            ))}
        </fieldset>
        <fieldset>
            <legend>Độ khó</legend>
            <label><input type="radio" name="difficulty-filter" checked={!difficulty} onChange={() => { setDifficulty(''); setPage(1); }} /> Tất cả</label>
            {['Easy', 'Medium', 'Hard', 'Expert'].map((level) => (
              <label key={level}><input type="radio" name="difficulty-filter" checked={difficulty === level} onChange={() => { setDifficulty(level); setPage(1); }} /> {level}</label>
            ))}
        </fieldset>
        <label className="field">
            <span>Thời gian nấu tối đa</span>
            <select value={maximumMinutes} onChange={(event) => { setMaximumMinutes(event.target.value); setPage(1); }}>
              <option value="">Bất kỳ</option><option value="15">Dưới 15 phút</option><option value="30">Dưới 30 phút</option><option value="60">Dưới 60 phút</option>
            </select>
        </label>
      </aside>
      <div className="recipe-results">
      <div className="recipe-toolbar">
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
          <strong>{filteredItems.length}</strong> công thức phù hợp
        </div>
        <a className="primary-button" href="/recipes/new">+ Tạo công thức</a>
      </div>
      <div className="recipe-admin-grid">
        {visibleItems.map((recipe) => (
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
              <a className="card-action" href={`/recipes/${recipe.slug}/edit`}>Chỉnh sửa</a>
              <button className="delete-action" type="button" onClick={() => deleteRecipe(recipe.id, recipe.title)}>
                Xóa
              </button>
            </div>
          </article>
        ))}
      </div>
      {recipes && visibleItems.length === 0 && (
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
      </div>
    </section>
  );
}
