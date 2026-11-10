'use client';

import React, { useState, useEffect, useCallback, useMemo } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import type { RecipeDetail, RecipeStep } from '@/types/recipe';
import { resolveImageUrl } from '@/lib/image-url';
import { useCookingTimer } from '@/hooks/useCookingTimer';
import { CookingTimer } from './CookingTimer';

export interface CookingModeProps {
  recipe: RecipeDetail;
}

export function CookingMode({ recipe }: CookingModeProps) {
  const router = useRouter();

  // Sắp xếp các bước theo stepNumber tăng dần và lọc bỏ các bước không hợp lệ
  const steps = useMemo(() => {
    if (!recipe.steps || !Array.isArray(recipe.steps)) return [];
    return [...recipe.steps]
      .filter((s): s is RecipeStep => Boolean(s && typeof s.stepNumber === 'number'))
      .sort((a, b) => a.stepNumber - b.stepNumber);
  }, [recipe.steps]);

  const [currentStepIndex, setCurrentStepIndex] = useState(0);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [showIngredients, setShowIngredients] = useState(false);
  const [checkedIngredients, setCheckedIngredients] = useState<string[]>([]);

  // Hook quản lý timers độc lập cho từng bước
  const { timers, startTimer, pauseTimer, resumeTimer, resetTimer, adjustTimer } = useCookingTimer(steps);

  const totalSteps = steps.length;
  const currentStep = steps[currentStepIndex];
  const progressPercent = totalSteps > 0 ? ((currentStepIndex + 1) / totalSteps) * 100 : 0;

  // Xử lý chuyển bước với clamp an toàn
  const goToNextStep = useCallback(() => {
    setCurrentStepIndex((prev) => Math.min(totalSteps - 1, prev + 1));
  }, [totalSteps]);

  const goToPrevStep = useCallback(() => {
    setCurrentStepIndex((prev) => Math.max(0, prev - 1));
  }, []);

  const goToStep = useCallback(
    (index: number) => {
      if (index >= 0 && index < totalSteps) {
        setCurrentStepIndex(index);
      }
    },
    [totalSteps]
  );

  // Xử lý phím tắt (Left: Bước trước, Right: Tiếp theo, F: Fullscreen)
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Bỏ qua nếu đang gõ vào input
      if (['INPUT', 'TEXTAREA'].includes((e.target as HTMLElement)?.tagName)) return;

      if (e.key === 'ArrowRight') {
        goToNextStep();
      } else if (e.key === 'ArrowLeft') {
        goToPrevStep();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [goToNextStep, goToPrevStep]);

  // Fullscreen API
  const toggleFullscreen = useCallback(async () => {
    try {
      if (!document.fullscreenElement) {
        await document.documentElement.requestFullscreen?.();
        setIsFullscreen(true);
      } else {
        await document.exitFullscreen?.();
        setIsFullscreen(false);
      }
    } catch {
      // Fullscreen bị từ chối hoặc không hỗ trợ
    }
  }, []);

  useEffect(() => {
    const handleFsChange = () => {
      setIsFullscreen(Boolean(document.fullscreenElement));
    };
    document.addEventListener('fullscreenchange', handleFsChange);
    return () => document.removeEventListener('fullscreenchange', handleFsChange);
  }, []);

  const toggleIngredientCheck = (id: string) => {
    setCheckedIngredients((prev) =>
      prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id]
    );
  };

  // Trường hợp công thức không có bước nào
  if (totalSteps === 0) {
    return (
      <div className="max-w-3xl mx-auto py-16 px-4 text-center">
        <div className="w-16 h-16 mx-auto mb-4 rounded-2xl bg-amber-50 text-amber-600 flex items-center justify-center border border-amber-200">
          <svg className="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
          </svg>
        </div>
        <h2 className="text-2xl font-bold text-slate-900 mb-2">Công thức chưa có bước nấu</h2>
        <p className="text-slate-600 mb-6">Món ăn này chưa được cập nhật danh sách các bước chế biến chi tiết.</p>
        <Link
          href={`/recipes/${recipe.slug}`}
          className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl font-semibold text-white bg-emerald-700 hover:bg-emerald-800 transition-colors shadow-sm"
        >
          ← Quay lại công thức
        </Link>
      </div>
    );
  }

  const currentTimer = timers[currentStep.stepNumber];
  const stepImageUrl = resolveImageUrl(currentStep.imageUrl);

  return (
    <div
      data-testid="cooking-mode-container"
      className="min-h-screen bg-slate-50 flex flex-col justify-between select-none"
    >
      {/* 1. Header Bar: Tiêu đề món, counter & controls */}
      <header className="sticky top-0 z-30 bg-white/95 backdrop-blur-md border-b border-slate-200 shadow-xs px-4 sm:px-8 py-3.5 flex items-center justify-between gap-4">
        {/* Left: Exit button & Recipe Title */}
        <div className="flex items-center gap-3 min-w-0">
          <Link
            href={`/recipes/${recipe.slug}`}
            data-testid="btn-exit-cooking"
            className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs sm:text-sm font-semibold text-slate-700 bg-slate-100 hover:bg-slate-200 hover:text-slate-900 transition-colors shrink-0"
            title="Thoát chế độ nấu ăn"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            <span className="hidden sm:inline">Thoát</span>
          </Link>

          <div className="min-w-0">
            <span className="text-[11px] font-bold uppercase tracking-wider text-emerald-700 block truncate">
              Chế độ nấu ăn
            </span>
            <h1 className="text-base sm:text-lg font-extrabold text-slate-900 truncate">
              {recipe.title}
            </h1>
          </div>
        </div>

        {/* Right: Step Counter, Ingredients Drawer Toggle & Fullscreen */}
        <div className="flex items-center gap-2 sm:gap-3 shrink-0">
          {/* Step Counter Badge */}
          <span
            data-testid="step-counter"
            className="text-xs sm:text-sm font-bold px-3 py-1 rounded-full bg-emerald-100 text-emerald-800 border border-emerald-200"
          >
            Bước {currentStepIndex + 1} / {totalSteps}
          </span>

          {/* Toggle Ingredients Checklist */}
          {recipe.ingredients && recipe.ingredients.length > 0 && (
            <button
              type="button"
              data-testid="btn-toggle-ingredients"
              onClick={() => setShowIngredients(!showIngredients)}
              className={`p-2 rounded-xl text-xs sm:text-sm font-semibold border transition-colors flex items-center gap-1.5 ${
                showIngredients
                  ? 'bg-emerald-700 text-white border-emerald-800 shadow-inner'
                  : 'bg-white text-slate-700 hover:bg-slate-100 border-slate-200'
              }`}
              title="Danh sách nguyên liệu"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
              </svg>
              <span className="hidden md:inline">Nguyên liệu</span>
              <span className="text-[11px] font-mono bg-emerald-900/10 px-1.5 py-0.2 rounded-md">
                {checkedIngredients.length}/{recipe.ingredients.length}
              </span>
            </button>
          )}

          {/* Fullscreen Toggle */}
          <button
            type="button"
            data-testid="btn-toggle-fullscreen"
            onClick={toggleFullscreen}
            className="p-2 rounded-xl bg-white text-slate-600 hover:text-slate-900 hover:bg-slate-100 border border-slate-200 transition-colors"
            title={isFullscreen ? 'Thu nhỏ' : 'Toàn màn hình'}
            aria-label={isFullscreen ? 'Thu nhỏ' : 'Toàn màn hình'}
          >
            {isFullscreen ? (
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            ) : (
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4" />
              </svg>
            )}
          </button>
        </div>
      </header>

      {/* 2. Visual Progress Bar */}
      <div className="w-full h-1.5 bg-slate-200 overflow-hidden" role="progressbar" aria-valuenow={progressPercent} aria-valuemin={0} aria-valuemax={100}>
        <div
          data-testid="cooking-progress-bar"
          className="h-full bg-emerald-700 transition-all duration-300 ease-out"
          style={{ width: `${progressPercent}%` }}
        />
      </div>

      {/* 3. Main Stage Content Area */}
      <main className="flex-1 max-w-5xl w-full mx-auto p-4 sm:p-6 lg:p-8 flex flex-col justify-center">
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
          {/* Step Detail Card (Left / Main) */}
          <div className="lg:col-span-12 space-y-6">
            <article
              data-testid="current-step-card"
              className="bg-white rounded-3xl border border-slate-200/90 shadow-sm p-6 sm:p-8 space-y-6 transition-all"
            >
              {/* Step number badge & heading */}
              <div className="flex items-start justify-between gap-4">
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <span className="px-3 py-1 rounded-xl bg-emerald-700 text-white font-extrabold text-sm tracking-wide">
                      BƯỚC {String(currentStep.stepNumber).padStart(2, '0')}
                    </span>
                    {currentStep.timerMinutes && currentStep.timerMinutes > 0 ? (
                      <span className="text-xs font-semibold px-2.5 py-1 rounded-lg bg-emerald-50 text-emerald-800 border border-emerald-200 flex items-center gap-1">
                        <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <circle cx="12" cy="12" r="10" strokeWidth="2" />
                          <polyline points="12 6 12 12 16 14" strokeWidth="2" />
                        </svg>
                        {currentStep.timerMinutes} phút
                      </span>
                    ) : null}
                  </div>
                  <h2
                    data-testid="step-title"
                    className="text-2xl sm:text-3xl font-extrabold text-slate-900 pt-2"
                  >
                    {currentStep.title || `Bước ${currentStep.stepNumber}`}
                  </h2>
                </div>
              </div>

              {/* Step Illustration Image if available */}
              {stepImageUrl && (
                <div className="relative w-full aspect-video sm:aspect-[21/9] max-h-96 rounded-2xl overflow-hidden bg-slate-100 border border-slate-200 shadow-inner">
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img
                    src={stepImageUrl}
                    alt={currentStep.title || `Bước ${currentStep.stepNumber}`}
                    className="w-full h-full object-cover"
                    onError={(e) => {
                      (e.currentTarget as HTMLImageElement).style.display = 'none';
                    }}
                  />
                </div>
              )}

              {/* Step Description with high legibility */}
              <div className="bg-slate-50/70 p-5 sm:p-6 rounded-2xl border border-slate-200/70">
                <p
                  data-testid="step-description"
                  className="text-slate-800 text-base sm:text-xl leading-relaxed whitespace-pre-line font-medium"
                >
                  {currentStep.description}
                </p>
              </div>

              {/* Smart Cooking Timer Integration */}
              <div className="pt-2">
                <CookingTimer
                  timer={currentTimer}
                  stepNumber={currentStep.stepNumber}
                  onStart={() => startTimer(currentStep.stepNumber)}
                  onPause={() => pauseTimer(currentStep.stepNumber)}
                  onResume={() => resumeTimer(currentStep.stepNumber)}
                  onReset={() => resetTimer(currentStep.stepNumber)}
                  onAdjust={(delta) => adjustTimer(currentStep.stepNumber, delta)}
                />
              </div>
            </article>
          </div>
        </div>
      </main>

      {/* 4. Ingredients Floating Drawer / Modal */}
      {showIngredients && recipe.ingredients && (
        <div
          data-testid="ingredients-drawer"
          className="fixed inset-y-0 right-0 z-50 w-full sm:w-96 bg-white shadow-2xl border-l border-slate-200 p-6 flex flex-col justify-between animate-in slide-in-from-right duration-200"
        >
          <div>
            <div className="flex items-center justify-between pb-4 border-b border-slate-100 mb-4">
              <div>
                <h3 className="text-lg font-bold text-slate-900">Danh sách nguyên liệu</h3>
                <p className="text-xs text-slate-500">
                  {checkedIngredients.length}/{recipe.ingredients.length} nguyên liệu đã chuẩn bị
                </p>
              </div>
              <button
                type="button"
                data-testid="btn-close-ingredients"
                onClick={() => setShowIngredients(false)}
                className="p-2 rounded-xl text-slate-400 hover:text-slate-700 hover:bg-slate-100"
              >
                ✕
              </button>
            </div>

            <ul className="space-y-2.5 max-h-[70vh] overflow-y-auto pr-1 divide-y divide-slate-100">
              {recipe.ingredients.map((ing) => {
                const isChecked = checkedIngredients.includes(ing.id);
                return (
                  <li key={ing.id} className="pt-2.5 first:pt-0">
                    <label className="flex items-start gap-3 cursor-pointer group">
                      <input
                        type="checkbox"
                        checked={isChecked}
                        onChange={() => toggleIngredientCheck(ing.id)}
                        className="mt-1 w-4 h-4 rounded text-emerald-700 focus:ring-emerald-500 border-slate-300 cursor-pointer"
                      />
                      <div className="flex-1 text-sm">
                        <span
                          className={`font-medium transition-colors ${
                            isChecked ? 'line-through text-slate-400' : 'text-slate-800'
                          }`}
                        >
                          {ing.name}
                        </span>
                        {ing.notes && (
                          <span className="block text-xs text-slate-400">{ing.notes}</span>
                        )}
                      </div>
                      <span className="text-xs font-semibold text-emerald-700 shrink-0">
                        {ing.quantity ?? ''} {ing.unit ?? ''}
                      </span>
                    </label>
                  </li>
                );
              })}
            </ul>
          </div>

          <button
            type="button"
            onClick={() => setShowIngredients(false)}
            className="w-full mt-4 py-2.5 rounded-xl font-semibold text-sm bg-slate-100 hover:bg-slate-200 text-slate-800 transition-colors"
          >
            Đóng bảng nguyên liệu
          </button>
        </div>
      )}

      {/* 5. Bottom Navigation Bar: Quick pills & Action buttons */}
      <footer className="sticky bottom-0 z-30 bg-white/95 backdrop-blur-md border-t border-slate-200 px-4 sm:px-8 py-4 shadow-lg">
        <div className="max-w-5xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-4">
          {/* Quick jump pills */}
          <nav
            data-testid="cooking-step-nav"
            className="flex items-center gap-1.5 overflow-x-auto max-w-full pb-1 sm:pb-0 scrollbar-none"
            aria-label="Điều hướng nhanh các bước"
          >
            {steps.map((s, idx) => {
              const isActive = idx === currentStepIndex;
              const isPast = idx < currentStepIndex;
              const sTimer = timers[s.stepNumber];
              const timerRunning = sTimer?.status === 'running';
              const timerDone = sTimer?.status === 'completed';

              return (
                <button
                  type="button"
                  key={s.id || s.stepNumber}
                  data-testid={`btn-jump-step-${s.stepNumber}`}
                  onClick={() => goToStep(idx)}
                  className={`relative min-w-[36px] h-9 px-2 rounded-xl text-xs font-bold transition-all flex items-center justify-center gap-1 ${
                    isActive
                      ? 'bg-emerald-700 text-white shadow-sm ring-2 ring-emerald-500/30'
                      : isPast
                      ? 'bg-emerald-100 text-emerald-900 hover:bg-emerald-200'
                      : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                  }`}
                  aria-label={`Đi tới bước ${s.stepNumber}`}
                >
                  <span>{s.stepNumber}</span>
                  {timerRunning && (
                    <span className="w-1.5 h-1.5 rounded-full bg-amber-400 animate-ping" />
                  )}
                  {timerDone && <span className="text-[10px]">✓</span>}
                </button>
              );
            })}
          </nav>

          {/* Prev / Next Controls */}
          <div className="flex items-center gap-3 w-full sm:w-auto justify-between sm:justify-end">
            <button
              type="button"
              data-testid="btn-prev-step"
              onClick={goToPrevStep}
              disabled={currentStepIndex === 0}
              className="inline-flex items-center gap-1.5 px-5 py-3 rounded-2xl text-sm font-bold text-slate-700 bg-slate-100 hover:bg-slate-200 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" d="M15 19l-7-7 7-7" />
              </svg>
              <span>Bước trước</span>
            </button>

            {currentStepIndex < totalSteps - 1 ? (
              <button
                type="button"
                data-testid="btn-next-step"
                onClick={goToNextStep}
                className="inline-flex items-center gap-1.5 px-6 py-3 rounded-2xl text-sm font-bold text-white bg-emerald-700 hover:bg-emerald-800 transition-all shadow-md hover:shadow-lg hover:-translate-y-0.5"
              >
                <span>Tiếp theo</span>
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" d="M9 5l7 7-7 7" />
                </svg>
              </button>
            ) : (
              <button
                type="button"
                data-testid="btn-finish-cooking"
                onClick={() => router.push(`/recipes/${recipe.slug}`)}
                className="inline-flex items-center gap-2 px-6 py-3 rounded-2xl text-sm font-bold text-white bg-gradient-to-r from-emerald-600 to-teal-700 hover:from-emerald-700 hover:to-teal-800 transition-all shadow-md hover:shadow-lg hover:-translate-y-0.5 animate-pulse"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" d="M5 13l4 4L19 7" />
                </svg>
                <span>Hoàn thành ✓</span>
              </button>
            )}
          </div>
        </div>
      </footer>
    </div>
  );
}

export default CookingMode;
