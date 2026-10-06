import { apiClient } from "./client";
import type {
  AdminUserDto,
  AdminUserQuery,
  PagedUsersResponse,
  UpdateUserStatusRequest,
} from "@/types/admin";

/**
 * Admin API — tương ứng với AdminController (Backend):
 * GET   /api/v1/admin/users            Danh sách user (phân trang, lọc)
 * PATCH /api/v1/admin/users/{id}/status  Đổi trạng thái tài khoản
 */
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

    const { data } = await apiClient.get<PagedUsersResponse>(
      `/admin/users?${params.toString()}`
    );
    return data;
  },

  async updateUserStatus(
    id: string,
    isActive: boolean
  ): Promise<AdminUserDto> {
    const { data } = await apiClient.patch<AdminUserDto>(
      `/admin/users/${id}/status`,
      { isActive } satisfies UpdateUserStatusRequest
    );
    return data;
  },
};
