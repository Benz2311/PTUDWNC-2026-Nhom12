/**
 * Shared API contracts aligned with backend ASP.NET Core models.
 */

/** RFC 7807 ProblemDetails returned by Backend exception handling. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

/** Error payload representation for normalized client error handling. */
export interface ApiErrorResponse {
  message: string;
  status?: number;
  title?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

/** Pagination metadata aligned with backend PaginationMetaDto. */
export interface PaginationMeta {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

/** Generic paged result aligned with backend PagedResult<T> / PagedResultDto<T>. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

/** Standard envelope for API responses when data is wrapped. */
export type ApiResponse<T> = T;
