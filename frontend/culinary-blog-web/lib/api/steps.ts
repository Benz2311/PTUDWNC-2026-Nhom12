import { apiFetch, apiJson, ApiError } from '@/lib/api';
import type {
  RecipeStep,
  CreateStepInput,
  UpdateStepInput,
} from '@/types/step';

export class StepApiError extends Error {
  status: number;
  errors?: Record<string, string[]>;

  constructor(message: string, status: number, errors?: Record<string, string[]>) {
    super(message);
    this.name = 'StepApiError';
    this.status = status;
    this.errors = errors;
  }
}

/**
 * Chuyển đổi ApiError từ apiFetch/apiJson thành StepApiError thân thiện với người dùng
 */
function handleStepError(error: unknown): never {
  if (error && typeof error === 'object' && 'status' in error) {
    const apiErr = error as ApiError;
    const status = apiErr.status ?? 500;
    let message = apiErr.message || 'Thao tác với bước thực hiện không thành công.';

    if (status === 401) {
      message = 'Bạn cần đăng nhập để thực hiện thao tác này.';
    } else if (status === 403) {
      message = 'Bạn không có quyền chỉnh sửa bước của công thức này.';
    } else if (status === 404) {
      message = 'Không tìm thấy công thức hoặc bước thực hiện tương ứng.';
    }

    throw new StepApiError(message, status, apiErr.errors);
  }

  if (error instanceof Error) {
    throw new StepApiError(error.message, 500);
  }

  throw new StepApiError('Đã xảy ra lỗi không xác định.', 500);
}

/**
 * Lấy danh sách bước của công thức qua endpoint chi tiết công thức
 */
export async function getRecipeSteps(slugOrId: string): Promise<RecipeStep[]> {
  try {
    const data = await apiJson<{ steps?: RecipeStep[] }>(`/api/v1/recipes/${slugOrId}`);
    return data?.steps ?? [];
  } catch (error) {
    if (error && typeof error === 'object' && 'status' in error && (error as ApiError).status === 404) {
      return [];
    }
    return handleStepError(error);
  }
}

/**
 * Thêm bước mới cho công thức (Server tự động cấp StepNumber = N + 1)
 */
export async function createRecipeStep(
  recipeId: string,
  input: CreateStepInput
): Promise<RecipeStep> {
  try {
    return await apiFetch<RecipeStep>(`/api/v1/recipes/${recipeId}/steps`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        title: input.title?.trim() || null,
        description: input.description.trim(),
        timerMinutes: input.timerMinutes ?? null,
        imageUrl: input.imageUrl?.trim() || null,
      }),
    });
  } catch (error) {
    return handleStepError(error);
  }
}

/**
 * Cập nhật bước thực hiện (Giữ nguyên StepNumber)
 */
export async function updateRecipeStep(
  recipeId: string,
  stepId: string,
  input: UpdateStepInput
): Promise<RecipeStep> {
  try {
    return await apiFetch<RecipeStep>(`/api/v1/recipes/${recipeId}/steps/${stepId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        title: input.title?.trim() || null,
        description: input.description.trim(),
        timerMinutes: input.timerMinutes ?? null,
        imageUrl: input.imageUrl?.trim() || null,
      }),
    });
  } catch (error) {
    return handleStepError(error);
  }
}

/**
 * Xóa mềm bước thực hiện (Backend tự động đánh số lại các bước còn lại tuần tự 1..N)
 */
export async function deleteRecipeStep(
  recipeId: string,
  stepId: string
): Promise<void> {
  try {
    await apiFetch<void>(`/api/v1/recipes/${recipeId}/steps/${stepId}`, {
      method: 'DELETE',
    });
  } catch (error) {
    return handleStepError(error);
  }
}

/**
 * Sắp xếp lại thứ tự các bước thực hiện
 */
export async function reorderRecipeSteps(
  recipeId: string,
  stepIds: string[]
): Promise<RecipeStep[]> {
  try {
    return await apiFetch<RecipeStep[]>(`/api/v1/recipes/${recipeId}/steps/reorder`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        stepIds,
      }),
    });
  } catch (error) {
    return handleStepError(error);
  }
}
