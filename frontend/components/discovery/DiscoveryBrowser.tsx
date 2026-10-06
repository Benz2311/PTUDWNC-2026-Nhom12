'use client';

import { FormEvent, useEffect, useMemo, useState } from 'react';
import { useRouter } from 'next/navigation';
import { apiFetch } from '@/lib/api';

type DiscoveryBrowserProps = { mode?: 'explore' | 'search' };
type Recipe = {
  id: string;
  title: string;
  slug: string;
  description: string | null;
  category: string;
  author: string;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  difficulty: string;
  primaryImageUrl: string | null;
};
type Category = { id: string; name: string; slug: string; recipeCount: number };
type PageResponse = { items: Recipe[]; page: number; pageSize: number; totalCount: number };
const fallbackImages = ['/sample-images/food-01.svg', '/sample-images/food-02.svg', '/sample-images/food-03.svg'];

export default function DiscoveryBrowser({ mode = 'explore' }: DiscoveryBrowserProps) {
  const router = useRouter();
  const [query, setQuery] = useState('');
  const [submittedQuery, setSubmittedQuery] = useState('');
  const [categories, setCategories] = useState<Category[]>([]);
  const [categoryId, setCategoryId] = useState('');
  const [recipes, setRecipes] = useState<Recipe[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    apiFetch<Category[]>('/api/v1/categories', {}, false)
      .then(setCategories)
      .catch((reason: Error) => setError(reason.message));
    if (mode === 'search') {
      const initialQuery = new URLSearchParams(window.location.search).get('q') ?? '';
      setQuery(initialQuery);
      setSubmittedQuery(initialQuery);
    }
  }, [mode]);

  useEffect(() => {
    let cancelled = false;
    if (mode === 'search' && submittedQuery.trim().length < 2) {
      setRecipes([]);
      setTotalCount(0);
      setLoading(false);
      setError('');
      return () => { cancelled = true; };
    }

    setLoading(true);
    setError('');
    const category = categoryId ? `&categoryId=${encodeURIComponent(categoryId)}` : '';
    const request = mode === 'search'
      ? apiFetch<PageResponse>(`/api/v1/recipes/search?query=${encodeURIComponent(submittedQuery.trim())}&page=${page}&pageSize=12${category}`, {}, false)
      : apiFetch<PageResponse>(`/api/v1/recipes?page=${page}&pageSize=12${category}`, {}, false);
    request.then((result) => {
      if (cancelled) return;
      setRecipes(result.items);
      setTotalCount(result.totalCount);
    }).catch((reason: Error) => {
      if (cancelled) return;
      setError(reason.message || 'Không tải được công thức.');
      setRecipes([]);
      setTotalCount(0);
    }).finally(() => {
      if (!cancelled) setLoading(false);
    });
    return () => { cancelled = true; };
  }, [categoryId, mode, page, submittedQuery]);

  const currentCategory = useMemo(() => categories.find((category) => category.id === categoryId), [categories, categoryId]);
  const totalPages = Math.max(1, Math.ceil(totalCount / 12));

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const value = query.trim();
    if (value.length < 2) {
      setError('Nhập ít nhất 2 ký tự để tìm công thức.');
      return;
    }
    setPage(1);
    setSubmittedQuery(value);
    if (mode === 'search') router.replace(`/search?q=${encodeURIComponent(value)}`);
  }

  return (
    <div className="recipe-workspace-layout discovery-layout">
      <aside className="recipe-filter-panel">
        <h2>Bộ lọc</h2>
        {mode === 'search' && (
          <form className="discovery-search-form" onSubmit={submit}>
            <label className="field"><span>Từ khóa</span><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Tên món, nguyên liệu..." /></label>
            <button className="primary-button" type="submit">Tìm công thức</button>
          </form>
        )}
        <fieldset>
          <legend>Danh mục</legend>
          <label><input type="radio" name="category" checked={!categoryId} onChange={() => { setCategoryId(''); setPage(1); }} /> Tất cả danh mục</label>
          {categories.map((category) => (
            <label key={category.id}><input type="radio" name="category" checked={categoryId === category.id} onChange={() => { setCategoryId(category.id); setPage(1); }} /> {category.name} <span className="filter-count">{category.recipeCount}</span></label>
          ))}
        </fieldset>
        <p className="filter-note">Chỉ hiển thị công thức đã xuất bản.</p>
      </aside>
      <section className="recipe-results">
        <div className="results-heading">
          <div>
            <span className="flow-kicker">{mode === 'search' ? 'Kết quả tìm kiếm' : 'Bếp nhà cộng đồng'}</span>
            <h2>{mode === 'search' ? (submittedQuery ? `Kết quả cho “${submittedQuery}”` : 'Bắt đầu tìm món bạn thích') : currentCategory?.name ?? 'Khám phá công thức'}</h2>
            <p>{mode === 'search' && submittedQuery.length < 2 ? 'Tìm theo tên công thức hoặc mô tả món ăn.' : `${totalCount} công thức phù hợp với bạn`}</p>
          </div>
          <a className="outline-button" href="/recipes/new">+ Chia sẻ công thức</a>
        </div>
        {error && <p className="form-error" role="alert">{error}</p>}
        {loading ? <div className="loading-state">Đang tìm công thức phù hợp...</div> : recipes.length === 0 ? (
          <div className="empty-state"><strong>{mode === 'search' && submittedQuery.length < 2 ? 'Nhập từ khóa để tìm kiếm' : 'Chưa có công thức phù hợp'}</strong><span>Thử từ khóa khác hoặc bỏ bớt bộ lọc.</span></div>
        ) : (
          <div className="recipe-card-grid">
            {recipes.map((recipe, index) => (
              <a className="recipe-card" href={`/recipes/${encodeURIComponent(recipe.slug)}`} key={recipe.id}>
                <div className="recipe-image" role="img" aria-label={`Ảnh món ${recipe.title}`} style={{ backgroundImage: `url("${recipe.primaryImageUrl || fallbackImages[index % fallbackImages.length]}")` }}>
                  <span className="recipe-tag">{recipe.category}</span>
                </div>
                <div className="recipe-body">
                  <h3>{recipe.title}</h3>
                  <p>{recipe.description || 'Một công thức ngon đang chờ bạn khám phá.'}</p>
                  <div className="recipe-meta"><span>◷ {recipe.prepTimeMinutes + recipe.cookTimeMinutes} phút</span><span>{recipe.difficulty}</span></div>
                  <span className="recipe-card-link">Xem công thức →</span>
                </div>
              </a>
            ))}
          </div>
        )}
        {totalPages > 1 && (
          <div className="pagination">
            <button type="button" onClick={() => setPage((current) => Math.max(1, current - 1))} disabled={page === 1}>← Trước</button>
            <span>Trang {page} / {totalPages}</span>
            <button type="button" onClick={() => setPage((current) => Math.min(totalPages, current + 1))} disabled={page === totalPages}>Tiếp theo →</button>
          </div>
        )}
      </section>
    </div>
  );
}
