import { apiClient, unwrapPaged } from "./client";
import type {
  AdminUserDto,
  AdminUserQuery,
  PagedUsersResponse,
  UpdateUserStatusRequest,
} from "@/types/admin";
import type { ApiResponse, PaginationMeta } from "@/types/api";

export const adminApi = {
  async getUsers(query: AdminUserQuery): Promise<PagedUsersResponse> {
    const params = new URLSearchParams();
    params.set("page", String(query.page));
    params.set("pageSize", String(query.pageSize));
    if (query.keyword) {
      params.set("keyword", query.keyword);
    }
    if (query.isActive !== undefined) {
      params.set("isActive", String(query.isActive));
    }

    const response = await apiClient.get<ApiResponse<AdminUserDto[], PaginationMeta>>(
      `/admin/users?${params.toString()}`
    );
    return unwrapPaged<AdminUserDto>(response);
  },

  async updateUserStatus(
    id: string,
    isActive: boolean
  ): Promise<void> {
    await apiClient.patch(
      `/admin/users/${id}/status`,
      { isActive } satisfies UpdateUserStatusRequest
    );
  },
};
