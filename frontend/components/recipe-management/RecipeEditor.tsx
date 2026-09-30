'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';
import RecipeForm, { InitialRecipe } from './RecipeForm';

export default function RecipeEditor({ slug }: { slug: string }) {
  const [recipe, setRecipe] = useState<InitialRecipe | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    apiFetch<InitialRecipe>(`/api/v1/recipes/${slug}`)
      .then(setRecipe)
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

  return <RecipeForm recipeId={recipe.id} initialRecipe={recipe} />;
}
