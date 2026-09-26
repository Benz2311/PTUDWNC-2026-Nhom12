'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';

type RecipeStep = { id: string; stepNumber: number; title: string; description: string };

export default function RecipeSteps({ slug }: { slug: string }) {
  const [steps, setSteps] = useState<RecipeStep[]>([]);

  useEffect(() => {
    apiFetch<{ steps: RecipeStep[] }>(`/api/v1/recipes/${slug}`)
      .then((recipe) => setSteps(recipe.steps || []))
      .catch(() => setSteps([]));
  }, [slug]);

  if (steps.length === 0) return null;

  return (
    <section className="detail-section">
      <h2>Các bước thực hiện</h2>
      <div className="step-list">
        {steps.map((step) => (
          <article className="step-item" key={step.id}>
            <strong>{String(step.stepNumber).padStart(2, '0')}</strong>
            <div>
              <h3>{step.title || `Bước ${step.stepNumber}`}</h3>
              <p>{step.description}</p>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
