import type { PagedResult } from "./api";

export interface AdminUserDto {
  id: string;
  userName: string;
  email: string;
  displayName: string;
  avatarUrl?: string | null;
  bio?: string | null;
  roles: string[];
  isActive: boolean;
  createdAt: string;
}

export interface AdminUserQuery {
  page: number;
  pageSize: number;
  keyword?: string;
  isActive?: boolean;
}

export type PagedUsersResponse = PagedResult<AdminUserDto>;

export interface UpdateUserStatusRequest {
  isActive: boolean;
}
