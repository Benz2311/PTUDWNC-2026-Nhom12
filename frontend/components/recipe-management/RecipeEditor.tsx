'use client';

import { useEffect, useState } from 'react';
import { apiFetchWithResponse } from '@/lib/api';
import RecipeForm, { InitialRecipe } from './RecipeForm';

export default function RecipeEditor({ slug }: { slug: string }) {
  const [recipe, setRecipe] = useState<InitialRecipe | null>(null);
  const [etag, setEtag] = useState('');
  const [error, setError] = useState('');

  useEffect(() => {
    apiFetchWithResponse<InitialRecipe>(`/api/v1/recipes/${slug}`)
      .then(({ data, headers }) => {
        setRecipe(data);
        setEtag(headers.get('ETag') ?? '');
      })
      .catch((reason: Error) => setError(reason.message));
  }, [slug]);

  if (error) {
    return (
      <div className="empty-state">
        <strong>{error}</strong>
        <a className="card-action" href={`/recipes/${slug}`}>
          Quay lại
        </a>
      </div>
    );
  }

  if (!recipe) {
    return <div className="loading-state">Đang tải công thức...</div>;
  }

  return <RecipeForm recipeId={recipe.id} initialRecipe={recipe} etag={etag} />;
}
