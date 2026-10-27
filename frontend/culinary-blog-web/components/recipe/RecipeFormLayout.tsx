import type { ReactNode } from 'react';
import type { RecipeStatus } from '@/types/recipe';
import RecipeStatusBadge from './RecipeStatusBadge';

type RecipeFormLayoutProps = {
  status: RecipeStatus;
  children: ReactNode;
};

export default function RecipeFormLayout({ status, children }: RecipeFormLayoutProps) {
  return (
    <div className="recipe-form-layout">
      <div className="recipe-form-status">
        <span>Trạng thái công thức</span>
        <RecipeStatusBadge status={status} />
      </div>
      {children}
    </div>
  );
}
