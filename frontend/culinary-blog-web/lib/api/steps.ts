import { apiClient, unwrapData, ApiError } from './client';
import type {
  RecipeStep,
  CreateStepInput,
  UpdateStepInput,
} from '@/types/step';
import type { ApiResponse } from '@/types/api';

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
 * Chuyển đổi lỗi từ API client chung thành StepApiError thân thiện với người dùng
 */
function handleStepError(error: unknown): never {
  if (error instanceof ApiError) {
    const status = error.status ?? 500;
    let message = error.message || 'Thao tác với bước thực hiện không thành công.';

    if (status === 401) {
      message = 'Bạn cần đăng nhập để thực hiện thao tác này.';
    } else if (status === 403) {
      message = 'Bạn không có quyền chỉnh sửa bước của công thức này.';
    } else if (status === 404) {
      message = 'Không tìm thấy công thức hoặc bước thực hiện tương ứng.';
    }

    throw new StepApiError(message, status, error.errors);
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
    const response = await apiClient.get<ApiResponse<{ steps?: RecipeStep[] }>>(
      `/recipes/${encodeURIComponent(slugOrId)}`
    );
    const data = unwrapData(response);
    const steps = data?.steps ?? [];
    return [...steps].sort((a, b) => (a.stepNumber ?? 0) - (b.stepNumber ?? 0));
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
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
    const payload = {
      title: input.title?.trim() || null,
      description: input.description.trim(),
      timerMinutes: input.timerMinutes ?? null,
      imageUrl: input.imageUrl?.trim() || null,
    };

    const response = await apiClient.post<ApiResponse<RecipeStep>>(
      `/recipes/${recipeId}/steps`,
      payload
    );
    return unwrapData(response);
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
    const payload = {
      title: input.title?.trim() || null,
      description: input.description.trim(),
      timerMinutes: input.timerMinutes ?? null,
      imageUrl: input.imageUrl?.trim() || null,
    };

    const response = await apiClient.put<ApiResponse<RecipeStep>>(
      `/recipes/${recipeId}/steps/${stepId}`,
      payload
    );
    return unwrapData(response);
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
    await apiClient.delete(`/recipes/${recipeId}/steps/${stepId}`);
  } catch (error) {
    handleStepError(error);
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
    const response = await apiClient.put<ApiResponse<RecipeStep[]>>(
      `/recipes/${recipeId}/steps/reorder`,
      { stepIds }
    );
    return unwrapData(response);
  } catch (error) {
    return handleStepError(error);
  }
}

/**
 * Tải lên tệp ảnh cho bước thực hiện (Multipart Form Data - Browser/Axios tự tạo boundary)
 */
export async function uploadStepImageFile(
  recipeId: string,
  file: File
): Promise<{ url: string }> {
  try {
    const formData = new FormData();
    formData.append('file', file);

    const response = await apiClient.post<ApiResponse<{ url: string }>>(
      `/recipes/${recipeId}/steps/images`,
      formData,
      {
        headers: {
          'Content-Type': undefined,
        },
      }
    );
    return unwrapData(response);
  } catch (error) {
    return handleStepError(error);
  }
}
