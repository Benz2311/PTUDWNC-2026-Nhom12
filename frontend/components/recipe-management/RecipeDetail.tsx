'use client';

import { useEffect, useState } from 'react';
import { getRecipeBySlug } from '@/lib/api/recipes';
import type { RecipeDetail as RecipeDetailData } from '@/types/recipe';
import RecipeDetailView from '@/components/recipes/RecipeDetailView';

export default function RecipeDetail({ slug }: { slug: string }) {
  const [recipe, setRecipe] = useState<RecipeDetailData | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    let cancelled = false;
    setRecipe(null);
    setError('');
    getRecipeBySlug(slug, false)
      .then(({ data }) => {
        if (!cancelled) setRecipe(data);
      })
      .catch((reason: Error) => {
        if (!cancelled) setError(reason.message || 'Không tìm thấy công thức.');
      });

    return () => { cancelled = true; };
  }, [slug]);

  if (error) {
    return (
      <div className="empty-state">
        <strong>{error}</strong>
        <a className="card-action" href="/recipes">Quay lại danh sách</a>
      </div>
    );
  }

  if (!recipe) return <div className="loading-state">Đang tải công thức...</div>;

  return <RecipeDetailView recipe={recipe} />;
}
