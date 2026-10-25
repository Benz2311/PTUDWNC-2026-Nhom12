'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';
import type { RecipeDetail, RecipeStep } from '@/types/recipe';

type RecipeStepsProps = {
  slug: string;
  steps?: RecipeStep[];
};

export default function RecipeSteps({ slug, steps: initialSteps }: RecipeStepsProps) {
  const [steps, setSteps] = useState<RecipeStep[]>(initialSteps ?? []);

  useEffect(() => {
    if (initialSteps && initialSteps.length > 0) {
      setSteps(initialSteps);
      return;
    }

    let cancelled = false;
    apiFetch<RecipeDetail>(`/api/v1/recipes/${encodeURIComponent(slug)}`)
      .then((recipe) => {
        if (!cancelled) setSteps(recipe.steps);
      })
      .catch(() => {
        if (!cancelled) setSteps([]);
      });

    return () => { cancelled = true; };
  }, [slug, initialSteps]);

  if (steps.length === 0) {
    return (
      <section className="detail-section">
        <div className="section-heading step-guide-heading">
          <div>
            <h2>Các bước thực hiện</h2>
            <p>Làm theo từng bước để có kết quả hoàn hảo.</p>
          </div>
        </div>
        <div className="empty-state">
          <strong>Chưa có hướng dẫn chế biến.</strong>
        </div>
      </section>
    );
  }

  const sortedSteps = [...steps].sort((a, b) => a.stepNumber - b.stepNumber);

  return (
    <section className="detail-section">
      <div className="section-heading step-guide-heading">
        <div>
          <h2>Các bước thực hiện</h2>
          <p>Làm theo từng bước để có kết quả hoàn hảo.</p>
        </div>
        <a className="primary-button" href={`/recipes/${encodeURIComponent(slug)}/cook`}>
          Bắt đầu nấu →
        </a>
      </div>
      <div className="step-list">
        {sortedSteps.map((step) => (
          <article className="step-item" key={step.id}>
            <strong>{String(step.stepNumber).padStart(2, '0')}</strong>
            <div className="step-content">
              <div className="step-header">
                <h3>{step.title || `Bước ${step.stepNumber}`}</h3>
                {step.timerMinutes != null && step.timerMinutes > 0 ? (
                  <span className="step-timer-badge">⏱ {step.timerMinutes} phút</span>
                ) : null}
              </div>
              <p>{step.description}</p>
              {step.imageUrl ? (
                <div className="step-image-wrap">
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img src={step.imageUrl} alt={step.title || `Bước ${step.stepNumber}`} className="step-image" />
                </div>
              ) : null}
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}

