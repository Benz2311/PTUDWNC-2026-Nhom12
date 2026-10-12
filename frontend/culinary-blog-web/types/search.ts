export interface RecipeSearchResult {
  id: string;
  title: string;
  slug: string;
  description?: string | null;
  prepTimeMinutes?: number | null;
  cookTimeMinutes?: number | null;
  servings?: number | null;
  difficulty?: string | number | null;
  publishedAt?: string | null;
  primaryImageUrl?: string | null;
  authorName?: string | null;
  categoryName?: string | null;
  categorySlug?: string | null;
  matchType?: 'FullTextSearch' | 'FuzzyTrigram' | string | null;
}

export interface SearchResponse {
  items: RecipeSearchResult[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface SearchFilterState {
  categorySlug?: string;
  difficulty?: string;
  sortBy?: 'relevance' | 'newest' | 'cookTime';
}
