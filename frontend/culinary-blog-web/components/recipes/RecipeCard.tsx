import Link from 'next/link';
import type { ReactNode } from 'react';
import type { RecipeListItem, RecipeSummary } from '@/types/recipe';
import DifficultyBadge from './DifficultyBadge';
import RecipeMeta from './RecipeMeta';
import RecipeStatusBadge from './RecipeStatusBadge';
import RecipeTimeInfo from './RecipeTimeInfo';

const fallbackImages = [
  '/sample-images/food-01.svg',
  '/sample-images/food-02.svg',
  '/sample-images/food-03.svg',
];

type RecipeCardProps = {
  recipe: RecipeSummary | RecipeListItem;
  className?: string;
  showStatus?: boolean;
  actions?: ReactNode;
};

export default function RecipeCard({ recipe, className = '', showStatus = false, actions }: RecipeCardProps) {
  const imageUrl =
    recipe.primaryImageUrl ||
    recipe.coverImageUrl ||
    recipe.imageUrl ||
    fallbackImages[recipe.title.length % fallbackImages.length];

  const categoryName =
    typeof recipe.category === 'object' && recipe.category
      ? recipe.category.name
      : (recipe.category || recipe.categoryName || 'Ẩm thực');

  return (
    <article className={`recipe-card ${className}`.trim()}>
      <Link
        className="recipe-card-link-wrapper"
        href={`/recipes/${encodeURIComponent(recipe.slug)}`}
        aria-label={`Xem công thức ${recipe.title}`}
      >
        <div
          className="recipe-image recipe-shared-image"
          role="img"
          aria-label={`Ảnh món ${recipe.title}`}
          style={{ backgroundImage: `url("${imageUrl}")` }}
        >
          <span className="recipe-tag">{categoryName}</span>
          <span className="recipe-image-icon" aria-hidden="true">↗</span>
        </div>
        <div className="recipe-body">
          <div className="recipe-card-badges">
            <DifficultyBadge difficulty={recipe.difficulty} />
            {showStatus && <RecipeStatusBadge status={recipe.status} />}
          </div>
          <h3>{recipe.title}</h3>
          <p>{recipe.description || 'Khám phá cách chế biến món ăn này.'}</p>
          <RecipeMeta category={recipe.category} author={recipe.author} publishedAt={recipe.publishedAt} />
          <RecipeTimeInfo
            prepTimeMinutes={recipe.prepTimeMinutes ?? recipe.prepTime}
            cookTimeMinutes={recipe.cookTimeMinutes ?? recipe.cookTime}
            compact
          />
        </div>
      </Link>
      {actions && <div className="card-actions recipe-card-actions">{actions}</div>}
    </article>
  );
}
