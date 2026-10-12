export interface SearchRequest {
  q?: string;
  page?: number;
  pageSize?: number;
  categoryId?: string;
  categorySlug?: string;
  difficulty?: string;
  maxCookTimeMinutes?: number;
  sortBy?: string;
}

export interface SearchResult {
  id: string;
  title: string;
  slug: string;
  description: string;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  difficulty: number | string;
  publishedAt?: string | null;
  primaryImageUrl?: string | null;
  authorName: string;
  categoryName: string;
  categorySlug: string;
  matchType?: string;
}

export type RecipeSearchResult = SearchResult;

export interface Pagination {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface SearchResponse extends Pagination {
  items: SearchResult[];
}

export interface SearchFilterState {
  categorySlug?: string;
  difficulty?: string;
  sortBy?: 'relevance' | 'newest' | 'cookTime';
}
