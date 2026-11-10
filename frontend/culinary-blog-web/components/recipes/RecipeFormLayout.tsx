import type { ReactNode } from 'react';
import type { RecipeStatus } from '@/types/recipe';
import RecipeStatusBadge from './RecipeStatusBadge';

type RecipeFormLayoutProps = {
  status: RecipeStatus;
  children: ReactNode;
};

export default function RecipeFormLayout({ status, children }: RecipeFormLayoutProps) {
  return (
    <div className="recipe-form-layout space-y-8">
      <div className="recipe-form-status flex items-center justify-between p-4 bg-emerald-50/70 border border-emerald-200/80 rounded-2xl mb-6 shadow-2xs">
        <span className="text-xs sm:text-sm font-semibold text-emerald-900 flex items-center gap-2">
          <span className="w-2.5 h-2.5 rounded-full bg-emerald-600 animate-pulse" />
          Trạng thái bài viết:
        </span>
        <RecipeStatusBadge status={status} />
      </div>
      {children}
    </div>
  );
}
