'use client';

import { useCallback, useEffect, useState } from 'react';
import { getRecipeBySlug } from '@/lib/api/recipes';
import RecipeForm, { InitialRecipe } from './RecipeForm';

export default function RecipeEditor({ slug }: { slug: string }) {
  const [recipe, setRecipe] = useState<InitialRecipe | null>(null);
  const [etag, setEtag] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  const loadRecipe = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const { data, headers } = await getRecipeBySlug(slug);
      setRecipe(data);
      setEtag(headers.get('ETag') ?? '');
    } catch (reason: unknown) {
      setError(reason instanceof Error ? reason.message : 'Không thể tải công thức.');
    } finally {
      setLoading(false);
    }
  }, [slug]);

  useEffect(() => {
    void loadRecipe();
  }, [loadRecipe]);

  if (loading && !recipe) {
    return (
      <div className="loading-state" role="status">
        <span className="spinner" aria-hidden="true" /> Đang tải dữ liệu công thức...
      </div>
    );
  }

  if (error && !recipe) {
    return (
      <div className="empty-state error-state" role="alert">
        <strong>{error}</strong>
        <div style={{ display: 'flex', gap: 12, marginTop: 16 }}>
          <button className="primary-button" type="button" onClick={() => void loadRecipe()}>
            Thử lại
          </button>
          <a className="outline-button" href={`/recipes/${slug}`}>
            Quay lại chi tiết
          </a>
        </div>
      </div>
    );
  }

  if (!recipe) {
    return <div className="loading-state">Không tìm thấy công thức.</div>;
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
