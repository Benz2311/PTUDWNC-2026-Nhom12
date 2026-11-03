'use client';

import { useState, useCallback, useEffect } from 'react';
import type { RecipeStep, CreateStepInput, UpdateStepInput } from '@/types/step';
import {
  getRecipeSteps,
  createRecipeStep,
  updateRecipeStep,
  deleteRecipeStep,
  reorderRecipeSteps,
} from '@/lib/api/steps';

export interface UseRecipeStepsOptions {
  recipeId?: string;
  initialSteps?: RecipeStep[];
  onStepsChange?: (steps: RecipeStep[]) => void;
}

export function useRecipeSteps({
  recipeId,
  initialSteps = [],
  onStepsChange,
}: UseRecipeStepsOptions = {}) {
  const isDirectApi = Boolean(recipeId && recipeId.trim() !== '');

  const [steps, setSteps] = useState<RecipeStep[]>(() =>
    [...initialSteps].sort((a, b) => (a.stepNumber ?? 0) - (b.stepNumber ?? 0))
  );
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Sync internal state to parent callback
  const updateStepsState = useCallback(
    (newSteps: RecipeStep[]) => {
      const sorted = [...newSteps].sort((a, b) => (a.stepNumber ?? 0) - (b.stepNumber ?? 0));
      setSteps(sorted);
      onStepsChange?.(sorted);
    },
    [onStepsChange]
  );

  // Load steps from backend when recipeId changes
  useEffect(() => {
    let cancelled = false;

    if (isDirectApi && initialSteps.length === 0) {
      setIsLoading(true);
      setError(null);

      getRecipeSteps(recipeId!)
        .then((fetched) => {
          if (!cancelled) {
            updateStepsState(fetched);
          }
        })
        .catch((err) => {
          if (!cancelled) {
            setError(err instanceof Error ? err.message : 'Không tải được danh sách bước thực hiện.');
          }
        })
        .finally(() => {
          if (!cancelled) {
            setIsLoading(false);
          }
        });
    }

    return () => {
      cancelled = true;
    };
  }, [isDirectApi, recipeId, initialSteps.length, updateStepsState]);

  const addStep = useCallback(
    async (input: CreateStepInput): Promise<RecipeStep> => {
      setIsLoading(true);
      setError(null);
      try {
        if (isDirectApi && recipeId) {
          const created = await createRecipeStep(recipeId, input);
          const next = [...steps, created];
          updateStepsState(next);
          return created;
        }

        // Controlled local mode
        const localStepNumber = steps.length > 0 ? Math.max(...steps.map((s) => s.stepNumber ?? 0)) + 1 : 1;
        const newStep: RecipeStep = {
          id: `temp-step-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
          stepNumber: localStepNumber,
          title: input.title?.trim() || null,
          description: input.description.trim(),
          timerMinutes: input.timerMinutes ?? null,
          imageUrl: input.imageUrl?.trim() || null,
          createdAt: new Date().toISOString(),
        };

        const next = [...steps, newStep];
        updateStepsState(next);
        return newStep;
      } catch (err) {
        const msg = err instanceof Error ? err.message : 'Lỗi khi thêm bước thực hiện.';
        setError(msg);
        throw err;
      } finally {
        setIsLoading(false);
      }
    },
    [isDirectApi, recipeId, steps, updateStepsState]
  );

  const updateStep = useCallback(
    async (stepId: string, input: UpdateStepInput): Promise<RecipeStep> => {
      setIsLoading(true);
      setError(null);
      try {
        if (isDirectApi && recipeId) {
          const updated = await updateRecipeStep(recipeId, stepId, input);
          const next = steps.map((s) => (s.id === stepId ? updated : s));
          updateStepsState(next);
          return updated;
        }

        // Controlled local mode
        const existing = steps.find((s) => s.id === stepId);
        if (!existing) {
          throw new Error('Không tìm thấy bước thực hiện cần sửa.');
        }

        const updated: RecipeStep = {
          ...existing,
          title: input.title?.trim() || null,
          description: input.description.trim(),
          timerMinutes: input.timerMinutes ?? null,
          imageUrl: input.imageUrl?.trim() || null,
        };

        const next = steps.map((s) => (s.id === stepId ? updated : s));
        updateStepsState(next);
        return updated;
      } catch (err) {
        const msg = err instanceof Error ? err.message : 'Lỗi khi cập nhật bước thực hiện.';
        setError(msg);
        throw err;
      } finally {
        setIsLoading(false);
      }
    },
    [isDirectApi, recipeId, steps, updateStepsState]
  );

  const deleteStep = useCallback(
    async (stepId: string): Promise<void> => {
      setIsLoading(true);
      setError(null);
      try {
        if (isDirectApi && recipeId) {
          await deleteRecipeStep(recipeId, stepId);
          // Backend renumbers automatically, re-fetch or adjust local sequence
          const remaining = steps
            .filter((s) => s.id !== stepId)
            .sort((a, b) => (a.stepNumber ?? 0) - (b.stepNumber ?? 0))
            .map((s, idx) => ({ ...s, stepNumber: idx + 1 }));
          updateStepsState(remaining);
          return;
        }

        // Controlled local mode: filter and renumber 1..N
        const remaining = steps
          .filter((s) => s.id !== stepId)
          .sort((a, b) => (a.stepNumber ?? 0) - (b.stepNumber ?? 0))
          .map((s, idx) => ({ ...s, stepNumber: idx + 1 }));

        updateStepsState(remaining);
      } catch (err) {
        const msg = err instanceof Error ? err.message : 'Lỗi khi xóa bước thực hiện.';
        setError(msg);
        throw err;
      } finally {
        setIsLoading(false);
      }
    },
    [isDirectApi, recipeId, steps, updateStepsState]
  );

  const reorderSteps = useCallback(
    async (direction: 'up' | 'down', index: number): Promise<void> => {
      const targetIndex = direction === 'up' ? index - 1 : index + 1;
      if (targetIndex < 0 || targetIndex >= steps.length) return;

      const newSteps = [...steps];
      const temp = newSteps[index];
      newSteps[index] = newSteps[targetIndex];
      newSteps[targetIndex] = temp;

      // Renumber locally
      const renumbered = newSteps.map((step, idx) => ({
        ...step,
        stepNumber: idx + 1,
      }));

      if (isDirectApi && recipeId) {
        setIsLoading(true);
        setError(null);
        try {
          const stepIds = renumbered.map((s) => s.id);
          const serverReordered = await reorderRecipeSteps(recipeId, stepIds);
          updateStepsState(serverReordered);
        } catch (err) {
          const msg = err instanceof Error ? err.message : 'Lỗi khi sắp xếp lại bước thực hiện.';
          setError(msg);
          throw err;
        } finally {
          setIsLoading(false);
        }
      } else {
        updateStepsState(renumbered);
      }
    },
    [isDirectApi, recipeId, steps, updateStepsState]
  );

  return {
    steps,
    isLoading,
    error,
    setError,
    setSteps: updateStepsState,
    addStep,
    updateStep,
    deleteStep,
    reorderSteps,
  };
}
