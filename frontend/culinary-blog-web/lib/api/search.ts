import { apiClient, unwrapPaged, ApiError } from './client';
import type { SearchRequest, SearchResponse, SearchResult } from '@/types/search';
import type { ApiResponse, PaginationMeta } from '@/types/api';

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
 * Validation SRS FR-SRCH-001: Từ khóa từ 2-100 ký tự. Ngoài phạm vi trả về rỗng không gửi request.
 */
export async function searchRecipes(
  queryOrParams: string | SearchRequest,
  page = 1,
  pageSize = 10,
  difficulty?: string,
  sortBy?: string
): Promise<SearchResponse> {
  let params: SearchRequest;

  if (typeof queryOrParams === 'string') {
    params = {
      q: queryOrParams,
      page,
      pageSize,
      difficulty,
      sortBy,
    };
  } else {
    params = { ...queryOrParams };
  }

  const trimmedQuery = (params.q ?? '').trim();

  // SRS FR-SRCH-001: Query dài 2-100 ký tự; ngoài phạm vi không gửi request, trả về danh sách rỗng
  if (trimmedQuery.length < 2 || trimmedQuery.length > 100) {
    return {
      items: [],
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 10,
      totalCount: 0,
      totalPages: 0,
      hasPreviousPage: false,
      hasNextPage: false,
    };
  }

  const queryParams: Record<string, string | number> = {
    q: trimmedQuery,
    page: params.page && params.page > 0 ? params.page : 1,
    pageSize: params.pageSize && params.pageSize > 0 && params.pageSize <= 100 ? params.pageSize : 10,
  };

  if (params.categoryId) queryParams.categoryId = params.categoryId;
  if (params.categorySlug) queryParams.categorySlug = params.categorySlug;
  if (params.difficulty) queryParams.difficulty = params.difficulty;
  if (params.maxCookTimeMinutes) queryParams.maxCookTimeMinutes = params.maxCookTimeMinutes;
  if (params.sortBy) queryParams.sortBy = params.sortBy;

  try {
    const response = await apiClient.get<ApiResponse<SearchResult[], PaginationMeta>>(
      '/recipes/search',
      {
        params: queryParams,
      }
    );

    const paged = unwrapPaged<SearchResult>(response);

    return {
      items: paged.items,
      page: paged.page,
      pageSize: paged.pageSize,
      totalCount: paged.totalCount,
      totalPages: paged.totalPages,
      hasPreviousPage: paged.hasPreviousPage,
      hasNextPage: paged.hasNextPage,
    };
  } catch (error) {
    if (error instanceof ApiError) {
      throw new SearchApiError(
        error.message || 'Không thể tìm kiếm công thức vào lúc này.',
        error.status || 500
      );
    }
    if (error instanceof Error) {
      throw new SearchApiError(error.message, 500);
    }
    throw new SearchApiError('Đã xảy ra lỗi không xác định khi tìm kiếm.', 500);
  }
}
