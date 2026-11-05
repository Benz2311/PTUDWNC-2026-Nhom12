import type { PaginationMeta } from "./api";

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

// Aliased to PaginationMeta to maintain single source of truth
export type Pagination = PaginationMeta;

export interface SearchResponse {
  items: SearchResult[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface SearchFilterState {
  categoryId?: string;
  categorySlug?: string;
  difficulty?: string;
  maxCookTimeMinutes?: number;
  sortBy?: 'relevance' | 'newest' | 'cookTime';
}
