'use client';

import { useCallback, useEffect, useState } from 'react';
import Link from 'next/link';
import { apiFetchWithResponse } from '@/lib/api';
import type { RecipeDetail } from '@/types/recipe';
import RecipeForm from './RecipeForm';

export default function RecipeEditor({ slug }: { slug: string }) {
  const [recipe, setRecipe] = useState<RecipeDetail | null>(null);
  const [etag, setEtag] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  const loadRecipe = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const response = await apiFetchWithResponse<{ data: RecipeDetail } | RecipeDetail>(
        `/api/v1/recipes/${encodeURIComponent(slug)}`
      );
      const data = (response.data as { data?: RecipeDetail }).data ?? (response.data as RecipeDetail);
      setRecipe(data);
      setEtag(response.headers.get('ETag') ?? data?.version ?? '');
    } catch (reason: unknown) {
      setError(reason instanceof Error ? reason.message : 'Không thể tải công thức.');
    } finally {
      setLoading(false);
    }
  }, [slug]);

  useEffect(() => {
    loadRecipe().catch(() => {});
  }, [loadRecipe]);

  if (loading && !recipe) {
    return (
      <div className="loading-state p-8 text-center text-gray-500" role="status">
        <span className="spinner" aria-hidden="true" /> Đang tải dữ liệu công thức...
      </div>
    );
  }

  if (error && !recipe) {
    return (
      <div className="empty-state error-state p-8 text-center text-red-600 bg-red-50 rounded-xl border border-red-200" role="alert">
        <strong>{error}</strong>
        <div style={{ display: 'flex', gap: 12, marginTop: 16, justifyContent: 'center' }}>
          <button
            className="primary-button bg-emerald-700 text-white px-4 py-2 rounded-lg font-medium"
            type="button"
            onClick={() => {
              loadRecipe().catch(() => {});
            }}
          >
            Thử lại
          </button>
          <Link className="outline-button border border-gray-300 px-4 py-2 rounded-lg font-medium text-gray-700" href={`/recipes/${slug}`}>
            Quay lại chi tiết
          </Link>
        </div>
      </div>
    );
  }

  if (!recipe) {
    return <div className="loading-state p-8 text-center text-gray-500">Không tìm thấy công thức.</div>;
  }

  return (
    <RecipeForm
      recipeId={recipe.id}
      initialRecipe={recipe}
      initialEtag={etag}
      onReload={loadRecipe}
    />
  );
}

