import { apiClient, unwrapData } from "./client";
import type {
  ActivityAnalyticsResponse,
  CategoryPerformanceAnalyticsResponse,
} from "@/types/analytics";
import type { ApiResponse } from "@/types/api";

export async function getActivityAnalytics(days: number = 7): Promise<ActivityAnalyticsResponse> {
  const response = await apiClient.get<ApiResponse<ActivityAnalyticsResponse>>("/admin/analytics/activity", {
    params: { days },
  });
  return unwrapData(response);
}

export async function getCategoryPerformanceAnalytics(
  days: number = 7
): Promise<CategoryPerformanceAnalyticsResponse> {
  const response = await apiClient.get<ApiResponse<CategoryPerformanceAnalyticsResponse>>(
    "/admin/analytics/category-performance",
    {
      params: { days },
    }
  );
  return unwrapData(response);
}
