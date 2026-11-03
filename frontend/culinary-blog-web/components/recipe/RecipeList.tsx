import type { ReactNode } from 'react';
import type { RecipeSummary } from '@/types/recipe';
import RecipeCard from './RecipeCard';

type RecipeListProps = {
  recipes: RecipeSummary[];
  emptyMessage?: string;
  className?: string;
  showStatus?: boolean;
  renderActions?: (recipe: RecipeSummary) => ReactNode;
};

export default function RecipeList({
  recipes,
  emptyMessage = 'Chưa có công thức phù hợp.',
  className = '',
  showStatus = false,
  renderActions,
}: RecipeListProps) {
  if (recipes.length === 0) {
    return <div className="empty-state"><strong>{emptyMessage}</strong></div>;
  }

  return (
    <div className={`recipe-card-grid ${className}`.trim()}>
      {recipes.map((recipe) => (
        <RecipeCard
          key={recipe.id}
          recipe={recipe}
          showStatus={showStatus}
          actions={renderActions?.(recipe)}
        />
      ))}
    </div>
  );
}
