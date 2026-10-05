import { apiFetch, apiJson, ApiError } from '@/lib/api';
import type { RecipeImage, AddRecipeImageInput } from '@/types/image';

export class ImageApiError extends Error {
  status: number;
  errors?: Record<string, string[]>;

  constructor(message: string, status: number, errors?: Record<string, string[]>) {
    super(message);
    this.name = 'ImageApiError';
    this.status = status;
    this.errors = errors;
  }
}

/**
 * Chuyển đổi ApiError từ apiFetch/apiJson thành ImageApiError thân thiện với người dùng
 */
function handleImageError(error: unknown): never {
  if (error && typeof error === 'object' && 'status' in error) {
    const apiErr = error as ApiError;
    const status = apiErr.status ?? 500;
    let message = apiErr.message || 'Thao tác với hình ảnh công thức không thành công.';

    if (status === 401) {
      message = 'Bạn cần đăng nhập để quản lý hình ảnh của công thức.';
    } else if (status === 403) {
      message = 'Bạn không có quyền chỉnh sửa hình ảnh của công thức này.';
    } else if (status === 404) {
      message = 'Không tìm thấy công thức hoặc hình ảnh tương ứng.';
    } else if (status === 503) {
      message = 'Dịch vụ lưu trữ hình ảnh (MinIO / Object Storage) hiện không khả dụng (HTTP 503). Vui lòng thử lại sau.';
    }

    throw new ImageApiError(message, status, apiErr.errors);
  }

  if (error instanceof Error) {
    throw new ImageApiError(error.message, 500);
  }

  throw new ImageApiError('Đã xảy ra lỗi không xác định khi xử lý hình ảnh.', 500);
}

/**
 * Lấy danh sách ảnh của công thức qua endpoint chi tiết công thức
 */
export async function getRecipeImages(slugOrId: string): Promise<RecipeImage[]> {
  try {
    const data = await apiJson<{ images?: RecipeImage[] }>(`/api/v1/recipes/${slugOrId}`);
    const images = data?.images ?? [];
    return [...images].sort((a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0));
  } catch (error) {
    if (error && typeof error === 'object' && 'status' in error && (error as ApiError).status === 404) {
      return [];
    }
    return handleImageError(error);
  }
}

/**
 * Thêm hình ảnh mới cho công thức (Ảnh đầu tiên tự động thành Primary trên backend)
 */
export async function addRecipeImage(
  recipeId: string,
  input: AddRecipeImageInput
): Promise<RecipeImage> {
  try {
    return await apiFetch<RecipeImage>(`/api/v1/recipes/${recipeId}/images`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        originalUrl: input.originalUrl.trim(),
        mediumUrl: input.mediumUrl?.trim() || null,
        thumbnailUrl: input.thumbnailUrl?.trim() || null,
        altText: input.altText?.trim() || null,
        isPrimary: input.isPrimary ?? null,
        orderIndex: input.orderIndex ?? null,
      }),
    });
  } catch (error) {
    return handleImageError(error);
  }
}

/**
 * Đặt làm ảnh đại diện (Set Primary trong cùng transaction, backend tự bỏ primary của ảnh khác)
 */
export async function setPrimaryRecipeImage(
  recipeId: string,
  imageId: string
): Promise<RecipeImage> {
  try {
    return await apiFetch<RecipeImage>(`/api/v1/recipes/${recipeId}/images/${imageId}/primary`, {
      method: 'PATCH',
    });
  } catch (error) {
    return handleImageError(error);
  }
}

/**
 * Xóa mềm ảnh công thức (Backend tự động chuyển Primary cho ảnh có OrderIndex nhỏ nhất còn lại)
 */
export async function deleteRecipeImage(
  recipeId: string,
  imageId: string
): Promise<void> {
  try {
    await apiFetch<void>(`/api/v1/recipes/${recipeId}/images/${imageId}`, {
      method: 'DELETE',
    });
  } catch (error) {
    return handleImageError(error);
  }
}

/**
 * Tải file ảnh lên Object Storage / MinIO qua multipart/form-data
 */
export async function uploadImageFile(file: File): Promise<{ url: string }> {
  try {
    const formData = new FormData();
    formData.append('file', file);

    return await apiFetch<{ url: string }>('/api/v1/files/upload', {
      method: 'POST',
      body: formData,
    });
  } catch (error) {
    return handleImageError(error);
  }
}
