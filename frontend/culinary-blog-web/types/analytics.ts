export interface DailyActivityStat {
  date: string;
  createdCount: number;
  publishedCount: number;
}

export interface ActivityComparison {
  totalCreatedCurrent: number;
  totalCreatedPrevious: number;
  createdGrowthRate: number;
  totalPublishedCurrent: number;
  totalPublishedPrevious: number;
  publishedGrowthRate: number;
}

export interface TopAuthorActivity {
  authorId: string;
  authorName: string;
  authorEmail: string;
  avatarUrl?: string | null;
  recipesCreated: number;
  recipesPublished: number;
}

export interface ActivityAnalyticsResponse {
  days: number;
  dailyStats: DailyActivityStat[];
  summary: ActivityComparison;
  topAuthors: TopAuthorActivity[];
}

export interface CategoryPerformanceItem {
  categoryId: string;
  categoryName: string;
  slug: string;
  newRecipesCount: number;
  publishedRecipesCount: number;
  previousPublishedCount: number;
  growthRate: number;
  totalViewsInPeriod: number;
}

export interface CategoryPerformanceAnalyticsResponse {
  days: number;
  categories: CategoryPerformanceItem[];
  fastestGrowingCategories: CategoryPerformanceItem[];
  lowActivityCategories: CategoryPerformanceItem[];
}

