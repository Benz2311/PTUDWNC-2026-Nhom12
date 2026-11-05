'use client';

import { FormEvent, useEffect, useMemo, useState } from 'react';
import { useRouter } from 'next/navigation';
import { apiFetch } from '@/lib/api';
import { getRecipes } from '@/lib/api/recipes';
import { getRecipeSearchSuggestions, smartSearchRecipes, type SmartSearchFilters } from '@/lib/api/search';
import type { RecipeSummary } from '@/types/recipe';
import RecipeList from '@/components/recipes/RecipeList';

type DiscoveryBrowserProps = { mode?: 'explore' | 'search' };
type Category = { id: string; name: string; slug: string; recipeCount: number };

export default function DiscoveryBrowser({ mode = 'explore' }: DiscoveryBrowserProps) {
  const router = useRouter();
  const [query, setQuery] = useState('');
  const [submittedQuery, setSubmittedQuery] = useState('');
  const [categories, setCategories] = useState<Category[]>([]);
  const [categoryId, setCategoryId] = useState('');
  const [recipes, setRecipes] = useState<RecipeSummary[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [suggestions, setSuggestions] = useState<string[]>([]);
  const [searchFilters, setSearchFilters] = useState<Partial<SmartSearchFilters>>({});

  useEffect(() => {
    if (mode !== 'search' || query.trim().length < 2 || query.trim() === submittedQuery.trim()) {
      setSuggestions([]);
      return;
    }
    const timer = window.setTimeout(() => {
      void getRecipeSearchSuggestions(query)
        .then(setSuggestions)
        .catch(() => setSuggestions([]));
    }, 250);
    return () => window.clearTimeout(timer);
  }, [mode, query, submittedQuery]);

  useEffect(() => {
    apiFetch<unknown>('/api/v1/categories', {}, false)
      .then((data) => {
        const rawList = Array.isArray(data)
          ? data
          : Array.isArray((data as Record<string, unknown>)?.value)
            ? (data as { value: unknown[] }).value
            : Array.isArray((data as Record<string, unknown>)?.items)
              ? (data as { items: unknown[] }).items
              : Array.isArray((data as Record<string, unknown>)?.data)
                ? (data as { data: unknown[] }).data
                : [];

        const normalized: Category[] = rawList.map((item) => {
          const cat = item as Record<string, unknown>;
          return {
            id: String(cat.id ?? cat.Id ?? cat.categoryId ?? ''),
            name: String(cat.name ?? cat.Name ?? cat.categoryName ?? cat.title ?? ''),
            slug: String(cat.slug ?? cat.Slug ?? ''),
            recipeCount: Number(cat.recipeCount ?? cat.RecipeCount ?? cat.count ?? 0),
          };
        });

        setCategories(normalized);
      })
      .catch((reason: Error) => setError(reason.message));

    if (mode === 'search') {
      const params = new URLSearchParams(window.location.search);
      const initialQuery = params.get('q') ?? '';
      setQuery(initialQuery);
      setSubmittedQuery(initialQuery);
      setCategoryId(params.get('categoryId') ?? '');
      const filterKeys: Array<keyof SmartSearchFilters> = [
        'difficulty', 'maxCookTime', 'minCalories', 'maxCalories', 'minProtein', 'maxProtein',
        'minCarb', 'maxCarb', 'minFat', 'maxFat', 'minRating', 'includeIngredients', 'excludeIngredients', 'sortBy',
      ];
      setSearchFilters(Object.fromEntries(
        filterKeys.flatMap((key) => params.has(key) ? [[key, params.get(key) ?? '']] : []),
      ) as Partial<SmartSearchFilters>);
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
    const filters = { page, pageSize: 12, categoryId: categoryId || undefined };
    const request = mode === 'search'
      ? smartSearchRecipes({
          ...searchFilters,
          q: submittedQuery,
          page,
          pageSize: 12,
          categoryId: categoryId || undefined,
        })
      : getRecipes(filters, false);
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
  }, [categoryId, mode, page, searchFilters, submittedQuery]);

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
    if (mode === 'search') syncSearchUrl(value, categoryId, searchFilters);
  }

  function syncSearchUrl(
    q: string,
    category: string,
    filters: Partial<SmartSearchFilters>,
  ) {
    const params = new URLSearchParams();
    if (q) params.set('q', q);
    if (category) params.set('categoryId', category);
    Object.entries(filters).forEach(([key, value]) => {
      if (value) params.set(key, String(value));
    });
    router.replace(`/search${params.size ? `?${params.toString()}` : ''}`);
  }

  function updateSearchFilter(key: keyof SmartSearchFilters, value: string) {
    const next = { ...searchFilters, [key]: value || undefined };
    setSearchFilters(next);
    setPage(1);
    syncSearchUrl(query.trim(), categoryId, next);
  }

  return (
    <div className="recipe-workspace-layout discovery-layout">
      <aside className="recipe-filter-panel">
        <h2>Bộ lọc</h2>
        {mode === 'search' && (
          <form className="discovery-search-form" onSubmit={submit}>
            <label className="field" style={{ position: 'relative' }}>
              <span>Từ khóa</span>
              <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Tên món, nguyên liệu..." role="combobox" aria-autocomplete="list" />
              {suggestions.length > 0 ? (
                <ul role="listbox" style={{ background: 'var(--paper)', border: '1px solid var(--line)', borderRadius: 8, listStyle: 'none', margin: 0, padding: 6, position: 'absolute', top: '100%', width: '100%', zIndex: 2 }}>
                  {suggestions.map((suggestion) => (
                    <li key={suggestion}>
                      <button type="button" style={{ background: 'transparent', border: 0, padding: '7px 4px', textAlign: 'left', width: '100%' }} onClick={() => { setQuery(suggestion); setSubmittedQuery(suggestion); setSuggestions([]); router.replace(`/search?q=${encodeURIComponent(suggestion)}`); }}>
                        {suggestion}
                      </button>
                    </li>
                  ))}
                </ul>
              ) : null}
            </label>
            <button className="primary-button" type="submit">Tìm công thức</button>
          </form>
        )}
        <fieldset>
          <legend>Danh mục</legend>
          <label><input type="radio" name="category" checked={!categoryId} onChange={() => { setCategoryId(''); setPage(1); if (mode === 'search') syncSearchUrl(query.trim(), '', searchFilters); }} /> Tất cả danh mục</label>
          {categories.map((category, idx) => (
            <label key={category.id || `cat-${idx}`}>
              <input
                type="radio"
                name="category"
                checked={categoryId === category.id}
                onChange={() => { setCategoryId(category.id); setPage(1); if (mode === 'search') syncSearchUrl(query.trim(), category.id, searchFilters); }}
              />
              {category.name || 'Danh mục khác'}
              <span className="filter-count">{category.recipeCount}</span>
            </label>
          ))}
        </fieldset>
        {mode === 'search' ? (
          <fieldset className="advanced-search-filters">
            <legend>Bộ lọc nâng cao</legend>
            <label className="field">Độ khó
              <select value={searchFilters.difficulty ?? ''} onChange={(event) => updateSearchFilter('difficulty', event.target.value)}>
                <option value="">Tất cả</option><option value="Easy">Dễ</option><option value="Medium">Trung bình</option><option value="Hard">Khó</option>
              </select>
            </label>
            <label className="field">Thời gian nấu tối đa (phút)
              <input type="number" min="1" value={searchFilters.maxCookTime ?? ''} onChange={(event) => updateSearchFilter('maxCookTime', event.target.value)} />
            </label>
            <label className="field">Calories tối đa
              <input type="number" min="0" value={searchFilters.maxCalories ?? ''} onChange={(event) => updateSearchFilter('maxCalories', event.target.value)} />
            </label>
            <label className="field">Protein tối thiểu (g)
              <input type="number" min="0" value={searchFilters.minProtein ?? ''} onChange={(event) => updateSearchFilter('minProtein', event.target.value)} />
            </label>
            <label className="field">Đánh giá tối thiểu
              <input type="number" min="0" max="5" step="0.5" value={searchFilters.minRating ?? ''} onChange={(event) => updateSearchFilter('minRating', event.target.value)} />
            </label>
            <label className="field">Có nguyên liệu
              <input value={searchFilters.includeIngredients ?? ''} onChange={(event) => updateSearchFilter('includeIngredients', event.target.value)} placeholder="thịt bò, hành" />
            </label>
            <label className="field">Loại trừ nguyên liệu
              <input value={searchFilters.excludeIngredients ?? ''} onChange={(event) => updateSearchFilter('excludeIngredients', event.target.value)} placeholder="đậu phộng, tôm" />
            </label>
            <label className="field">Sắp xếp
              <select value={searchFilters.sortBy ?? 'relevance'} onChange={(event) => updateSearchFilter('sortBy', event.target.value)}>
                <option value="relevance">Liên quan</option><option value="rating">Đánh giá</option><option value="newest">Mới nhất</option><option value="cookTime">Thời gian nấu</option>
              </select>
            </label>
          </fieldset>
        ) : null}
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
        ) : <RecipeList recipes={recipes} />}
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
