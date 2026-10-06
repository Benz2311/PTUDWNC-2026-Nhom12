'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';

type Recipe = {
  title: string;
  servings: number;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  ingredients: { id: string; name: string; quantity: number | null; unit: string | null; notes: string | null }[];
  steps: { id: string; stepNumber: number; title: string; description: string }[];
};

export default function CookingGuide({ slug }: { slug: string }) {
  const [recipe, setRecipe] = useState<Recipe | null>(null);
  const [stepIndex, setStepIndex] = useState(0);
  const [checkedIngredients, setCheckedIngredients] = useState<string[]>([]);
  const [error, setError] = useState('');

  useEffect(() => {
    apiFetch<Recipe>(`/api/v1/recipes/${encodeURIComponent(slug)}`, {}, false)
      .then(setRecipe)
      .catch((reason: Error) => setError(reason.message || 'Không tải được công thức.'));
  }, [slug]);

  if (error) return <div className="empty-state"><strong>{error}</strong><a className="outline-button" href={`/recipes/${encodeURIComponent(slug)}`}>Quay lại công thức</a></div>;
  if (!recipe) return <div className="loading-state">Đang chuẩn bị hướng dẫn nấu...</div>;

  const currentStep = recipe.steps[stepIndex];
  const progress = recipe.steps.length > 0 ? (stepIndex + 1) / recipe.steps.length * 100 : 0;

  return (
    <div className="cooking-layout">
      <aside className="recipe-filter-panel cooking-ingredients">
        <span className="flow-kicker">Chuẩn bị</span>
        <h2>Nguyên liệu</h2>
        <p>{recipe.servings} khẩu phần · {recipe.prepTimeMinutes} phút chuẩn bị</p>
        <div className="cooking-checklist">
          {recipe.ingredients.map((ingredient) => (
            <label className={checkedIngredients.includes(ingredient.id) ? 'ingredient-checked' : ''} key={ingredient.id}>
              <input type="checkbox" checked={checkedIngredients.includes(ingredient.id)} onChange={() => setCheckedIngredients((current) => current.includes(ingredient.id) ? current.filter((id) => id !== ingredient.id) : [...current, ingredient.id])} />
              <span>{ingredient.name}{ingredient.notes ? <small>{ingredient.notes}</small> : null}</span>
              <strong>{ingredient.quantity ?? ''} {ingredient.unit ?? ''}</strong>
            </label>
          ))}
        </div>
        <a className="flow-panel-link" href={`/recipes/${encodeURIComponent(slug)}`}>← Xem công thức đầy đủ</a>
      </aside>
      <main className="cooking-main">
        <div className="cooking-title-row">
          <div><div className="flow-kicker">Chế độ nấu ăn</div><h1>{recipe.title}</h1></div>
          <span className="cooking-counter">Bước {stepIndex + 1} / {recipe.steps.length}</span>
        </div>
        <div className="cooking-progress"><span style={{ width: `${progress}%` }} /></div>
        {currentStep ? (
          <article className="cooking-step-card">
            <span className="cooking-step-number">{String(currentStep.stepNumber).padStart(2, '0')}</span>
            <span className="flow-kicker">Bước nấu</span>
            <h2>{currentStep.title || `Bước ${currentStep.stepNumber}`}</h2>
            <p>{currentStep.description}</p>
          </article>
        ) : <div className="empty-state"><strong>Hướng dẫn chưa có bước nào.</strong></div>}
        <div className="cooking-controls">
          <button className="outline-button" type="button" disabled={stepIndex === 0} onClick={() => setStepIndex((index) => Math.max(0, index - 1))}>← Bước trước</button>
          {stepIndex < recipe.steps.length - 1 ? (
            <button className="primary-button" type="button" onClick={() => setStepIndex((index) => Math.min(recipe.steps.length - 1, index + 1))}>Tiếp theo →</button>
          ) : <a className="primary-button" href={`/recipes/${encodeURIComponent(slug)}`}>Hoàn thành ✓</a>}
        </div>
        <nav className="cooking-step-nav" aria-label="Các bước nấu">
          {recipe.steps.map((step, index) => (
            <button className={index === stepIndex ? 'is-active' : index < stepIndex ? 'is-done' : ''} type="button" key={step.id} onClick={() => setStepIndex(index)} aria-label={`Đi tới bước ${step.stepNumber}`}>{step.stepNumber}</button>
          ))}
        </nav>
      </main>
    </div>
  );
}
