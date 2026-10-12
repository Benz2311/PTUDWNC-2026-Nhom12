import { apiJson, ApiError } from '@/lib/api';
import type { SearchResponse } from '@/types/search';

export class SearchApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = 'SearchApiError';
    this.status = status;
  }
}

/**
 * Tìm kiếm công thức nấu ăn qua endpoint Full-Text Search & Fuzzy Trigram
 * Hỗ trợ từ khóa tiếng Việt có dấu và không dấu ("pho bo" -> "Phở bò")
 */
export async function searchRecipes(
  query: string,
  page = 1,
  pageSize = 10
): Promise<SearchResponse> {
  const trimmed = query.trim();

  // SRS FR-SRCH-001: Query dài 2-100 ký tự; ngoài phạm vi trả về danh sách rỗng
  if (trimmed.length < 2 || trimmed.length > 100) {
    return {
      items: [],
      page,
      pageSize,
      totalCount: 0,
      totalPages: 0,
      hasPreviousPage: false,
      hasNextPage: false,
    };
  }

  const queryParams = new URLSearchParams({
    q: trimmed,
    page: String(page > 0 ? page : 1),
    pageSize: String(pageSize > 0 && pageSize <= 100 ? pageSize : 10),
  });

  try {
    return await apiJson<SearchResponse>(`/api/v1/recipes/search?${queryParams.toString()}`);
  } catch (error) {
    if (error && typeof error === 'object' && 'status' in error) {
      const apiErr = error as ApiError;
      throw new SearchApiError(
        apiErr.message || 'Không thể tìm kiếm công thức vào lúc này.',
        apiErr.status || 500
      );
    }
    if (error instanceof Error) {
      throw new SearchApiError(error.message, 500);
    }
    throw new SearchApiError('Đã xảy ra lỗi không xác định khi tìm kiếm.', 500);
  }
}
