import { apiClient, unwrapData, ApiError } from './client';
import type {
  RecipeImage,
  AddRecipeImageInput,
  UpdateRecipeImageInput,
} from '@/types/image';
import type { ApiResponse } from '@/types/api';

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
 * Chuyển đổi lỗi từ apiClient thành ImageApiError thân thiện với người dùng
 */
function handleImageError(error: unknown): never {
  if (error instanceof ApiError) {
    const status = error.status ?? 500;
    let message = error.message || 'Thao tác với hình ảnh công thức không thành công.';

    if (status === 401) {
      message = 'Bạn cần đăng nhập để quản lý hình ảnh của công thức.';
    } else if (status === 403) {
      message = 'Bạn không có quyền chỉnh sửa hình ảnh của công thức này.';
    } else if (status === 404) {
      message = 'Không tìm thấy công thức hoặc hình ảnh tương ứng.';
    } else if (status === 503) {
      message = 'Dịch vụ lưu trữ hình ảnh (MinIO / Object Storage) hiện không khả dụng (HTTP 503). Vui lòng thử lại sau.';
    }

    throw new ImageApiError(message, status, error.errors);
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
    const response = await apiClient.get<ApiResponse<{ images?: RecipeImage[] }>>(
      `/recipes/${encodeURIComponent(slugOrId)}`
    );
    const data = unwrapData(response);
    const images = data?.images ?? [];
    return [...images].sort((a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0));
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      return [];
    }
    return handleImageError(error);
  }
}

/**
 * Thêm hình ảnh mới cho công thức bằng URL đã có (Ảnh đầu tiên tự động thành Primary trên backend)
 */
export async function addRecipeImage(
  recipeId: string,
  input: AddRecipeImageInput
): Promise<RecipeImage> {
  try {
    const payload = {
      originalUrl: input.originalUrl.trim(),
      mediumUrl: input.mediumUrl?.trim() || null,
      thumbnailUrl: input.thumbnailUrl?.trim() || null,
      altText: input.altText?.trim() || null,
      isPrimary: input.isPrimary ?? null,
      orderIndex: input.orderIndex ?? null,
    };

    const response = await apiClient.post<ApiResponse<RecipeImage>>(
      `/recipes/${recipeId}/images`,
      payload
    );
    return unwrapData(response);
  } catch (error) {
    return handleImageError(error);
  }
}

/**
 * Tải file ảnh trực tiếp lên Recipe qua multipart/form-data
 * KHÔNG hard-code boundary: Browser/Axios tự động tạo Content-Type multipart boundary
 */
export async function uploadRecipeImageFile(
  recipeId: string,
  file: File,
  altText?: string,
  isPrimary?: boolean,
  onUploadProgress?: (progressEvent: { loaded: number; total?: number }) => void
): Promise<RecipeImage> {
  try {
    const formData = new FormData();
    formData.append('file', file);
    if (altText) {
      formData.append('altText', altText.trim());
    }
    if (isPrimary !== undefined) {
      formData.append('isPrimary', String(isPrimary));
    }

    const response = await apiClient.post<ApiResponse<RecipeImage>>(
      `/recipes/${recipeId}/images`,
      formData,
      {
        onUploadProgress,
      }
    );
    return unwrapData(response);
  } catch (error) {
    return handleImageError(error);
  }
}

/**
 * Cập nhật thông tin ảnh (PATCH: altText, orderIndex, isPrimary theo SRS v1.2.0 Mục 8.3)
 */
export async function updateRecipeImage(
  recipeId: string,
  imageId: string,
  input: UpdateRecipeImageInput
): Promise<RecipeImage> {
  try {
    const response = await apiClient.patch<ApiResponse<RecipeImage>>(
      `/recipes/${recipeId}/images/${imageId}`,
      input
    );
    return unwrapData(response);
  } catch (error) {
    return handleImageError(error);
  }
}

/**
 * Đặt làm ảnh đại diện theo hợp đồng chuẩn SRS v1.2.0 Mục 8.3:
 * PATCH /api/v1/recipes/{id}/images/{imageId} với body { isPrimary: true }
 */
export async function setPrimaryRecipeImage(
  recipeId: string,
  imageId: string
): Promise<RecipeImage> {
  try {
    const response = await apiClient.patch<ApiResponse<RecipeImage>>(
      `/recipes/${recipeId}/images/${imageId}`,
      { isPrimary: true }
    );
    return unwrapData(response);
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
    await apiClient.delete(`/recipes/${recipeId}/images/${imageId}`);
  } catch (error) {
    handleImageError(error);
  }
}

/**
 * Tải file ảnh lên Object Storage / MinIO qua multipart/form-data
 */
export async function uploadImageFile(file: File): Promise<{ url: string }> {
  try {
    const formData = new FormData();
    formData.append('file', file);

    const response = await apiClient.post<ApiResponse<{ url: string }>>(
      '/files/upload',
      formData
    );
    return unwrapData(response);
  } catch (error) {
    return handleImageError(error);
  }
}

